using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;
using EnlightenMobile.Common;

namespace EnlightenMobile.Models
{
    // Allows the Spectrometer "model" to flow-up progress updates to the 
    // ScopeViewModel's acquireProgressBar.  Feels like cheating, but I didn't
    // know how else to do it.  Would probably be more elegant to give
    // Spectrometer an acquisitionProgress property and raise PropertyChanged 
    // events from it, to which the ScopeViewModel would be subscribed.  This
    // would probably be done by having Spectrometer implement INotifyPropertyChanged,
    // then it could notify observers of its acquisitionProgress value.
    public delegate void ProgressBarDelegate(double progress);

    // This more-or-less corresponds to WasatchNET.Spectrometer, or 
    // SiGDemo.Spectrometer.  Spectrometer state and logic should be 
    // encapsulated here.
    public class Spectrometer
    {
        // Singleton
        static Spectrometer instance = null;

        // BLE comms
        Dictionary<string, ICharacteristic> characteristicsByName = null;

        // hardware model
        public uint pixels;
        public float laserExcitationNM;
        public EEPROM eeprom = EEPROM.getInstance();
        public Battery battery = new Battery();

        // software state
        public double[] wavelengths;
        public double[] wavenumbers;

        public double[] lastSpectrum;
        public double[] dark;

        public Measurement measurement;
        public string note {get; set;} = "your text here";
        public bool acquiring;
        ushort lastCRC;
        const int MAX_RETRIES = 3;

        public uint scansToAverage {get; set;} = 1;
        uint totalPixelsToRead;
        uint totalPixelsRead;

        object bleLock = new object();

        // util
        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle 
        ////////////////////////////////////////////////////////////////////////

        static public Spectrometer getInstance()
        {
            if (instance is null)
                instance = new Spectrometer();
            return instance;
        }

        Spectrometer()
        {
            // Provide some test defaults so we can play with the chart etc while
            // disconnected.  These will all be overwritten when we read an EEPROM.
            pixels = 1952;
            laserExcitationNM = 785.0f;
            wavelengths = new double[pixels];
            for (int i = 0; i < pixels; i++)
                wavelengths[i] = laserExcitationNM + 15 + i / 10.0;
            wavenumbers = Util.wavelengthsToWavenumbers(laserExcitationNM, wavelengths);

            measurement = new Measurement(this);
        }

        public async Task<bool> initAsync(
                Dictionary<string, ICharacteristic> characteristicsByName, 
                ProgressBarDelegate showProgress)
        {
            logger.debug("Initializing Spectrometer");

            this.characteristicsByName = characteristicsByName;

            ////////////////////////////////////////////////////////////////////
            // read the raw EEPROM pages
            ////////////////////////////////////////////////////////////////////

            var eepromCmd = characteristicsByName["eepromCmd"];
            var eepromData = characteristicsByName["eepromData"];

            if (eepromCmd is null || eepromData is null)
            {
                logger.error("Can't read EEPROM w/o characteristics");                
                return false;
            }

            logger.debug("reading EEPROM");
            List<byte[]> pages = new List<byte[]>();
            for (int page = 0; page < EEPROM.MAX_PAGES; page++)
            {
                byte[] buf = new byte[EEPROM.PAGE_LENGTH];
                int pos = 0;
                for (int subpage = 0; subpage < EEPROM.SUBPAGE_COUNT; subpage++)
                {
                    byte[] request = ToBLEData.convert((byte)page, (byte)subpage);
                    logger.debug($"requestEEPROMSubpage: page {page}, subpage {subpage}");
                    bool ok = await eepromCmd.WriteAsync(request);
                    if (!ok)
                    {
                        logger.error($"Failed to write eepromCmd({page}, {subpage})");
                        return false;
                    } 

                    try
                    {
                        var response = await eepromData.ReadAsync();
                        // logger.hexdump(response, "response");

                        for (int i = 0; i < response.Length; i++)
                            buf[pos++] = response[i];
                    }
                    catch(Exception ex)
                    {
                        logger.error($"Caught exception when trying to read EEPROM characteristic: {ex}");
                        return false;
                    }
                }
                logger.hexdump(buf, "adding page: ");
                pages.Add(buf);
                showProgress(.15 + .85 * page/8.0);
            }

            ////////////////////////////////////////////////////////////////////
            // parse the EEPROM
            ////////////////////////////////////////////////////////////////////

            logger.debug("parsing EEPROM");
            if (!eeprom.parse(pages))
            {
                logger.error("Spectrometer.initAsync: failed to parse EEPROM");
                return false;
            }

            ////////////////////////////////////////////////////////////////////
            // post-process EEPROM
            ////////////////////////////////////////////////////////////////////

            pixels = eeprom.activePixelsHoriz;
            laserExcitationNM = eeprom.laserExcitationWavelengthNMFloat;

            logger.debug("computing wavecal");
            wavelengths = Util.generateWavelengths(pixels, eeprom.wavecalCoeffs);

            if (laserExcitationNM > 0)
                wavenumbers = Util.wavelengthsToWavenumbers(laserExcitationNM, wavelengths);
            else
                wavenumbers = null;


            ////////////////////////////////////////////////////////////////////
            // finish initializing Spectrometer 
            ////////////////////////////////////////////////////////////////////

            showProgress(1);

            
            logger.debug("finishing spectrometer initialization");
            pixels = eeprom.activePixelsHoriz;

            updateBatteryAsync();
            integrationTimeMS = (ushort)(eeprom.startupIntegrationTimeMS > 0 ? eeprom.startupIntegrationTimeMS : 400);
            gainDb = (byte)Math.Min(31, Math.Round(eeprom.detectorGain));

            logger.info($"initialized {eeprom.model} {eeprom.serialNumber}");
            logger.info($"  detector: {eeprom.detectorName}");
            logger.info($"  pixels: {pixels}");
            logger.info( "  excitation: {0:f2}nm", laserExcitationNM);
            logger.info( "  wavelengths: ({0:f2}, {1:f2})", wavelengths[0], wavelengths[pixels-1]);
            if (wavenumbers != null)
                logger.info("  wavenumbers: ({0:f2}, {1:f2})", wavenumbers[0], wavenumbers[pixels-1]);

            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        // integrationTimeMS
        ////////////////////////////////////////////////////////////////////////

        public ushort integrationTimeMS
        {
            get => _nextIntegrationTimeMS;
            set 
            { 
                _nextIntegrationTimeMS = value;
                logger.debug($"Spectrometer.integrationTimeMS: next = {value}");
                _ = syncIntegrationTimeMSAsync();
            }
        }
        ushort _nextIntegrationTimeMS = 3;
        ushort _lastIntegrationTimeMS = 9999;

        async Task<bool> syncIntegrationTimeMSAsync()
        {
            if (characteristicsByName is null)
                return false;

            if (_nextIntegrationTimeMS == _lastIntegrationTimeMS)
                return true;

            var characteristic = characteristicsByName["integrationTimeMS"];
            if (characteristic is null)
            {
                logger.error("can't find integrationTimeMS characteristic");
                return false;
            }

            ushort value = Math.Min((ushort)5000, Math.Max((ushort)3, (ushort)Math.Round((decimal)_nextIntegrationTimeMS)));
            byte[] request = ToBLEData.convert(value, len: 2);

            logger.info($"Spectrometer.syncIntegrationTimeMSAsync({value})");
            logger.hexdump(request, "data: ");

            var ok = await characteristic.WriteAsync(request);
            if (ok)
            { 
                _lastIntegrationTimeMS = _nextIntegrationTimeMS;
                pauseAsync();
            }
            else
                logger.error($"Failed to set integrationTimeMS {value}");

            return ok;
        }

        async void pauseAsync()
        {
            await Task.Delay(10);
        }

        ////////////////////////////////////////////////////////////////////////
        // gainDb
        ////////////////////////////////////////////////////////////////////////

        public ushort gainDb
        {
            get => _nextGainDb;
            set 
            { 
                _nextGainDb = value;
                logger.debug($"Spectrometer.gainDb: next = {value}");
                _ = syncGainDbAsync();

            }
        }
        ushort _nextGainDb = 24;
        ushort _lastGainDb = 99;

        async Task<bool> syncGainDbAsync()
        {
            if (characteristicsByName is null)
                return false;

            if (_nextGainDb == _lastGainDb)
                return true;
                            
            var characteristic = characteristicsByName["gainDb"];
            if (characteristic is null)
            {
                logger.error("gainDb characteristic not found");
                return false;
            }

            byte value = Math.Min((byte)31, (byte)Math.Round((decimal)_nextGainDb));
            byte[] request = ToBLEData.convert(value, len: 1);

            logger.info($"Spectrometer.syncGainDbAsync({value})"); 
            logger.hexdump(request, "data: ");

            var ok = await characteristic.WriteAsync(request);
            if (ok)
            {
                _lastGainDb = _nextGainDb;
                pauseAsync();
            }
            else
                logger.error($"Failed to set gainDb {value}");

            return ok;
        }

        ////////////////////////////////////////////////////////////////////////
        // laserEnabled
        ////////////////////////////////////////////////////////////////////////

        public bool laserEnabled
        {
            get => _laserEnabled;
            set => _ = setLaserEnabledAsync(value);
        }
        bool _laserEnabled = false;

        async Task<bool> setLaserEnabledAsync(bool flag)
        {
            if (characteristicsByName is null)
                return false;

            var characteristic = characteristicsByName["laserEnable"];
            if (characteristic is null)
            {
                logger.error("can't find laserEnable characteristic");
                return false;
            }
            byte[] request = ToBLEData.convert(flag);

            logger.info($"Spectrometer.setLaserEnabledAsync({flag})");
            logger.hexdump(request, "data: ");

            var ok = await characteristic.WriteAsync(request);
            if (ok)
            { 
                _laserEnabled = flag;
                pauseAsync();
            }
            else
                logger.error($"Failed to set laserEnabled {flag}");

            return ok;
        }

        ////////////////////////////////////////////////////////////////////////
        // alternatingEnabled
        ////////////////////////////////////////////////////////////////////////

        public bool alternatingEnabled { get; set; }

        ////////////////////////////////////////////////////////////////////////
        // battery
        ////////////////////////////////////////////////////////////////////////

        async public void updateBatteryAsync()
        {
            if (characteristicsByName is null)
                return;

            var characteristic = characteristicsByName["batteryStatus"];
            if (characteristic is null)
            {
                logger.error("can't find characteristic batteryStatus");
                return;
            }

            logger.info("reading battery status");
            var response = await characteristic.ReadAsync();
            if (response is null)
            {
                logger.error("failed reading battery");
                return;
            }
            logger.hexdump(response, "batteryStatus: ");
            battery.parse(response);
            pauseAsync();
        }

        ////////////////////////////////////////////////////////////////////////
        // dark
        ////////////////////////////////////////////////////////////////////////

        public void toggleDark()
        {
            if (dark is null)
                dark = lastSpectrum; 
            else
                dark = null;
            logger.debug("Spectrometer.dark -> {0}", dark is null);
        }

        ////////////////////////////////////////////////////////////////////////
        // spectra
        ////////////////////////////////////////////////////////////////////////

        // responsible for taking one fully-averaged measurement
        public async Task<bool> takeOneAveragedAsync(ProgressBarDelegate showProgress)
        {
            if (characteristicsByName is null)
                return false;

            // push-down any changed acquisition parameters
            if (! await syncIntegrationTimeMSAsync())
                return false;

            if (! await syncGainDbAsync())
                return false;

            // for progress bar
            totalPixelsToRead = pixels * scansToAverage;
            totalPixelsRead = 0;
            acquiring = true;

            // take the first spectrum
            double[] spectrum = await takeOneAsync(showProgress);
            if (spectrum is null)
                return acquiring = false;

            // if doing scan averaging (in software), take the rest
            for (int i = 1; i < scansToAverage; i++)
            { 
                double[] tmp = await takeOneAsync(showProgress);
                if (tmp is null || tmp.Length != spectrum.Length)
                    return acquiring = false;

                for (int j = 0; j < spectrum.Length; j++)
                    spectrum[j] += tmp[j];    
            }

            if (scansToAverage > 1)
                for (int j = 0; j < spectrum.Length; j++)
                    spectrum[j] /= scansToAverage;

            lastSpectrum = spectrum;
            measurement = new Measurement(this);
            if (alternatingEnabled)
                measurement.averageAlternating();

            logger.info($"acquired Measurement {measurement.measurementID}");
            updateBatteryAsync();

            acquiring = false;
            return true;
        }

        // Take one spectrum (of many, if doing scan averaging).  This is private,
        // callers are expected to use takeOneAveragedAsync().
        private async Task<double[]> takeOneAsync(ProgressBarDelegate showProgress)
        {
            if (characteristicsByName is null)
                return null;

            const int headerLen = 2;

            var acquireChar = characteristicsByName["acquireSpectrum"];
            if (acquireChar is null)
            {
                logger.error("can't find characteristic acquireSpectrum");
                return null;
            }

            var spectrumRequestChar = characteristicsByName["spectrumRequest"];
            if (spectrumRequestChar is null)
            {
                logger.error("can't find characteristic spectrumRequest");
                return null;
            }

            var spectrumChar = characteristicsByName["spectrum"];
            if (spectrumChar is null)
            {
                logger.error("can't find characteristic spectrum");
                return null;
            }

            // send acquire command
            byte[] request = ToBLEData.convert(true);
            if (! await acquireChar.WriteAsync(request))
            {
                logger.error("failed to send acquire");
                return null;
            }

            // wait for acquisition to complete
            await Task.Delay(integrationTimeMS);

            var spectrum = new double[pixels];
            UInt16 pixelsRead = 0;
            var retryCount = 0;
            bool requestRetry = false;

            while (pixelsRead < pixels)
            {
                if (requestRetry)
                {
                    retryCount++;
                    if (retryCount > MAX_RETRIES)
                    {
                        logger.error($"giving up after {MAX_RETRIES} retries");
                        return null;
                    }

                    int delayMS = (int)Math.Pow(5, retryCount);
                    logger.error($"Retry requested, so waiting for {delayMS}ms");
                    await Task.Delay(delayMS);

                    requestRetry = false;
                }

                logger.debug($"requesting spectrum packet starting at pixel {pixelsRead}");
                request = ToBLEData.convert(pixelsRead, len: 2);
                if (! await spectrumRequestChar.WriteAsync(request))
                {
                    logger.error($"failed to write spectrum request for pixel {pixelsRead}");
                    return null;
                }

                var response = await spectrumChar.ReadAsync();

                // make sure response length is even, and has both header and at least one pixel of data
                var responseLen = response.Length;
                if (responseLen < headerLen || responseLen % 2 != 0)
                {
                    logger.error($"received invalid response of {responseLen} bytes");
                    requestRetry = true;
                    continue;
                }

                // firstPixel is a big-endian UInt16
                short firstPixel = (short)((response[0] << 8) | response[1]);
                if (firstPixel > 2048 || firstPixel < 0)
                {
                    logger.error($"received NACK (firstPixel {firstPixel}, retrying");
                    requestRetry = true;
                    continue;
                }

                var pixelsInPacket = (responseLen - headerLen) / 2;

                logger.debug($"received spectrum packet starting at pixel {firstPixel} with {pixelsInPacket} pixels");
                logger.hexdump(response);

                var crc = Crc16.checksum(response);
                if (crc == lastCRC)
                {
                    logger.error($"received duplicate CRC 0x{crc:x4}, retrying");
                    requestRetry = true;
                    continue;
                }

                lastCRC = crc;

                for (int i = 0; i < pixelsInPacket; i++)
                {
                    // pixel intensities are little-endian UInt16
                    var offset = headerLen + i * 2;
                    ushort intensity = (ushort)((response[offset+1] << 8) | response[offset]);
                    spectrum[pixelsRead] = intensity;

                    pixelsRead++;
                    totalPixelsRead++;

                    if (pixelsRead == pixels)
                    {
                        logger.debug("read complete spectrum");
                        if (i + 1 != pixelsInPacket)
                            logger.error($"ignoring {pixelsInPacket - (i + 1)} trailing pixels");
                        break;
                    }
                }

                showProgress(((double)totalPixelsRead) / totalPixelsToRead);
            }

            // kludge: first four pixels are zero, so overwrite from 5th
            for (int i = 0; i < 4; i++)
                spectrum[i] = spectrum[4];

            return spectrum;
        }
    }
}
