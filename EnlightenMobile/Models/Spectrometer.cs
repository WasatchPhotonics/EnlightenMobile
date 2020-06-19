using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class Spectrometer : INotifyPropertyChanged
    {
        // Singleton
        static Spectrometer instance = null;

        // BLE comms
        Dictionary<string, ICharacteristic> characteristicsByName;

        // hardware model
        public uint pixels;
        public float laserExcitationNM;
        public EEPROM eeprom = EEPROM.getInstance();
        public Battery battery;

        public BLEDeviceInfo bleDeviceInfo = new BLEDeviceInfo();

        // software state
        public double[] wavelengths;
        public double[] wavenumbers;
        public double[] xAxisPixels;

        public double[] lastSpectrum;
        public double[] dark;

        public Measurement measurement;
        public string note { get; set; }

        public bool acquiring;
        ushort lastCRC;
        const int MAX_RETRIES = 4;
        const int THROWAWAY_SPECTRA = 6;

        public uint scansToAverage { get; set; } = 1;
        uint totalPixelsToRead;
        uint totalPixelsRead;

        // util
        WhereAmI whereAmI;
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
            reset();
            battery = new Battery();
        }

        public void reset()
        { 
            // Provide some test defaults so we can play with the chart etc while
            // disconnected.  These will all be overwritten when we read an EEPROM.
            pixels = 1952;
            laserExcitationNM = 785.0f;
            wavelengths = new double[pixels];
            for (int i = 0; i < pixels; i++)
                wavelengths[i] = laserExcitationNM + 15 + i / 10.0;
            wavenumbers = Util.wavelengthsToWavenumbers(laserExcitationNM, wavelengths);
            generatePixelAxis();

            if (measurement is null)
                measurement = new Measurement();
            measurement.reset();
            measurement.reload(this);

            characteristicsByName = null;
            note = "your text here";
            acquiring = false;
            lastCRC = 0;
            scansToAverage = 1;
        }

        void generatePixelAxis()
        {
            xAxisPixels = new double[pixels];
            for (int i = 0; i < pixels; i++)
                xAxisPixels[i] = i;
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
                        logger.debug("reading eepromData");
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

            generatePixelAxis();

            ////////////////////////////////////////////////////////////////////
            // finish initializing Spectrometer 
            ////////////////////////////////////////////////////////////////////

            showProgress(1);
            
            logger.debug("finishing spectrometer initialization");
            pixels = eeprom.activePixelsHoriz;

            _ = await updateBatteryAsync();
            integrationTimeMS = (ushort)(eeprom.startupIntegrationTimeMS > 0 && eeprom.startupIntegrationTimeMS < 5000 ? eeprom.startupIntegrationTimeMS : 3);
            gainDb = eeprom.detectorGain;

            verticalROIStartLine = eeprom.ROIVertRegionStart[0];
            verticalROIStopLine = eeprom.ROIVertRegionEnd[0];

            logger.info($"initialized {eeprom.model} {eeprom.serialNumber}");
            logger.info($"  detector: {eeprom.detectorName}");
            logger.info($"  pixels: {pixels}");
            logger.info($"  verticalROI: ({verticalROIStartLine}, {verticalROIStopLine})");
            logger.info( "  excitation: {0:f2}nm", laserExcitationNM);
            logger.info( "  wavelengths: ({0:f2}, {1:f2})", wavelengths[0], wavelengths[pixels-1]);
            if (wavenumbers != null)
                logger.info("  wavenumbers: ({0:f2}, {1:f2})", wavenumbers[0], wavenumbers[pixels-1]);

            // I'm honestly not sure where we should initialize location, but it 
            // should probably happen after we've successfully connected to a
            // spectrometer and are ready to take measurements.  Significantly,
            // at this point we know the user has already granted location privs.
            whereAmI = WhereAmI.getInstance();

            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        // integrationTimeMS
        ////////////////////////////////////////////////////////////////////////

        public uint integrationTimeMS
        {
            get => _nextIntegrationTimeMS;
            set 
            { 
                _nextIntegrationTimeMS = value;
                logger.debug($"Spectrometer.integrationTimeMS: next = {value}");
                _ = syncIntegrationTimeMSAsync();
            }
        }
        uint _nextIntegrationTimeMS = 3;
        uint _lastIntegrationTimeMS = 9999;

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
            byte[] request = ToBLEData.convert(value, len: 4);

            logger.info($"Spectrometer.syncIntegrationTimeMSAsync({value})");
            logger.hexdump(request, "data: ");

            var ok = await characteristic.WriteAsync(request);
            if (ok)
            { 
                _lastIntegrationTimeMS = _nextIntegrationTimeMS;
                await pauseAsync();
            }
            else
                logger.error($"Failed to set integrationTimeMS {value}");

            return ok;
        }

        // I no longer remember exactly why this method is here
        async Task<bool> pauseAsync()
        {
            await Task.Delay(10);

            // logger.debug("pauseAsync: running GC");
            // GC.Collect();
            // logger.debug("pauseAsync: back from GC");

            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        // gainDb
        ////////////////////////////////////////////////////////////////////////

        // for documentation on the unsigned bfloat16 datatype used by gain, see
        // https://github.com/WasatchPhotonics/Wasatch.NET/blob/master/WasatchNET/FunkyFloat.cs

        public float gainDb
        {
            get => _nextGainDb;
            set 
            { 
                if (value > 0 && value < 256)
                {
                    _nextGainDb = value;
                    logger.debug($"Spectrometer.gainDb: next = {value}");
                    _ = syncGainDbAsync();
                }
                else
                {
                    logger.error($"ignoring out-of-range gainDb {value}");
                }
            }
        }
        float _nextGainDb = 24.0f;
        float _lastGainDb = 99.0f;

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

            byte msb = (byte)Math.Floor(_nextGainDb);
            byte lsb = (byte)(((byte)Math.Round( (_nextGainDb - msb) * 256.0)) & 0xff);

            ushort value = (ushort)((msb << 8) | lsb);
            ushort len = 2;

            byte[] request = ToBLEData.convert(value, len: len);

            logger.debug($"converting gain {_nextGainDb:f4} to msb 0x{msb:x2}, lsb 0x{lsb:x2}, value 0x{value:x4}, request {request}");

            logger.info($"Spectrometer.syncGainDbAsync({_nextGainDb})"); 
            logger.hexdump(request, "data: ");

            var ok = await characteristic.WriteAsync(request);
            if (ok)
            {
                _lastGainDb = _nextGainDb;
                await pauseAsync();
            }
            else
                logger.error($"Failed to set gainDb {value:x4}");

            // kludge
            if (!ok)
            {
                logger.error("KLUDGE: ignoring gainDb failure");
                ok = true;
                _lastGainDb = _nextGainDb;
            }

            return ok;
        }

        ////////////////////////////////////////////////////////////////////////
        // Vertical ROI Start/Stop
        ////////////////////////////////////////////////////////////////////////

        public ushort verticalROIStartLine
        {
            get => _nextVerticalROIStartLine;
            set 
            { 
                if (value > 0 && value < eeprom.activePixelsVert)
                {
                    _nextVerticalROIStartLine = value;
                    logger.debug($"Spectrometer.verticalROIStartLine -> {value}");
                    _ = syncROI();
                }
                else
                {
                    logger.error($"ignoring out-of-range start line {value}");
                }
            }
        }
        ushort _nextVerticalROIStartLine = 200;
        ushort _lastVerticalROIStartLine = 0;

        public ushort verticalROIStopLine
        {
            get => _nextVerticalROIStopLine;
            set 
            { 
                if (value > 0 && value < eeprom.activePixelsVert)
                {
                    _nextVerticalROIStopLine = value;
                    logger.debug($"Spectrometer.verticalROIStopLine -> {value}");
                    _ = syncROI();
                }
                else
                {
                    logger.error($"ignoring out-of-range stop line {value}");
                }
            }
        }
        ushort _nextVerticalROIStopLine = 800;
        ushort _lastVerticalROIStopLine = 0;

        async Task<bool> syncROI()
        {
            if (characteristicsByName is null)
                return false;

            var characteristic = characteristicsByName["roi"];
            if (characteristic is null)
            {
                logger.error("ROI characteristic not found");
                return false;
            }

            // noop
            if (_nextVerticalROIStartLine == _lastVerticalROIStartLine &&
                _nextVerticalROIStopLine == _lastVerticalROIStopLine)
                return false;

            // force ordering
            var start = verticalROIStartLine;
            var stop = verticalROIStopLine;
            if (stop < start)
                Util.swap(ref start, ref stop);

            byte[] startData = ToBLEData.convert(start, len: 2);
            byte[] stopData = ToBLEData.convert(stop, len: 2);
            byte[] request = new byte[4];
            Array.Copy(startData, request, 2);
            Array.Copy(stopData, 0, request, 2, 2);

            logger.info($"Spectrometer.syncROI({verticalROIStartLine}, {verticalROIStopLine})"); 
            logger.hexdump(request, "data: ");

            var ok = await characteristic.WriteAsync(request);
            if (ok)
            {
                _lastVerticalROIStartLine = _nextVerticalROIStartLine;
                _lastVerticalROIStopLine = _nextVerticalROIStopLine;
                await pauseAsync();
            }
            else
                logger.error($"Failed to set ROI ({verticalROIStartLine}, {verticalROIStopLine})");

            return ok;
        }

        ////////////////////////////////////////////////////////////////////////
        // laserState
        ////////////////////////////////////////////////////////////////////////

        LaserState laserState = new LaserState();

        public bool ramanModeEnabled
        {
            get => laserState.mode == LaserMode.RAMAN;
            set
            {
                laserState.mode = value ? LaserMode.RAMAN : LaserMode.MANUAL;
                logger.debug($"Spectrometer.ramanModeEnabled: laserState.mode = {laserState.mode}");

                _ = syncLaserStateAsync();
            }
        }

        public byte laserWatchdogSec
        {
            get => laserState.watchdogSec;
            set
            {
                laserState.watchdogSec = value;
                _ = syncLaserStateAsync();
            }
        }

        public bool laserEnabled
        {
            get => laserState.enabled;
            set
            {
                laserState.enabled = value;
                _ = syncLaserStateAsync();
            }
        }

        public ushort laserDelayMS
        {
            get => laserState.laserDelayMS;
            set
            {
                laserState.laserDelayMS = value;
                _ = syncLaserStateAsync();
            }
        }

        bool laserSyncEnabled = true;

        async Task<bool> syncLaserStateAsync()
        {
            logger.debug("syncLaserStateAsync: start");
            if (!laserSyncEnabled)
            {
                logger.debug("syncLaserState: skipping");
                return false;
            }

            laserState.dump();

            if (characteristicsByName is null)
                return false;

            var characteristic = characteristicsByName["laserState"];
            if (characteristic is null)
            {
                logger.error("laserState characteristic not found");
                return false;
            }

            byte[] request = laserState.serialize();
            logger.hexdump(request, "Spectrometer.syncLaserStateAsync: ");

            var ok = await characteristic.WriteAsync(request);
            if (ok)
                await pauseAsync();
            else
                logger.error($"Failed to set laserState");

            return ok;
        }

        ////////////////////////////////////////////////////////////////////////
        // battery
        ////////////////////////////////////////////////////////////////////////

        async public Task<bool> updateBatteryAsync()
        {
            if (battery.level > 0)
            {
                logger.debug("updateBatteryAsync: skipping after first read (waiting for notifications)");
                return false;
            }

            logger.debug("updateBatteryAsync: starting");

            if (!battery.isExpired)
            {
                logger.debug("battery state still valid, skipping");
                return false;
            }

            if (characteristicsByName is null)
                return false;

            var characteristic = characteristicsByName["batteryStatus"];
            if (characteristic is null)
            {
                logger.error("can't find characteristic batteryStatus");
                return false;
            }

            logger.info("reading battery status");
            var response = await characteristic.ReadAsync();
            if (response is null)
            {
                logger.error("failed reading battery");
                return false;
            }
            logger.hexdump(response, "batteryStatus: ");
            battery.parse(response);
            await pauseAsync();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("batteryStatus"));

            logger.debug("updateBatteryAsync: done");
            return true;
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
            // keep this positive for loop sanity
            scansToAverage = Math.Min(1, scansToAverage);

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

            // TODO: integrate laserDelayMS into showProgress
            var swRamanMode = laserState.mode == LaserMode.RAMAN && LaserState.SW_RAMAN_MODE;
            if (swRamanMode)
            {
                var sec = (byte)Math.Max(5, (uint)((integrationTimeMS * scansToAverage / 1000.0) + 3));
                logger.debug($"takeOneAveragedAsync: setting laserWatchdogSec -> {sec}");

                // since we're going to sync the laser state immediately after to turn on the laser,
                // skip this sync
                laserSyncEnabled = false;
                laserWatchdogSec = (byte)Math.Min(5, (uint)((integrationTimeMS * scansToAverage / 1000.0) + 3));
                laserSyncEnabled = true;

                logger.debug("takeOneAveragedAsync: setting laserEnabled = true");
                laserEnabled = true;

                logger.debug($"takeOneAveragedAsync: waiting {laserState.laserDelayMS}ms");
                await Task.Delay(laserState.laserDelayMS);
            }

            double[] spectrum = null;
            for (int spectrumCount = 0; spectrumCount < scansToAverage; spectrumCount++)
            { 
                bool disableLaserAfterFirstPacket = swRamanMode && spectrumCount + 1 == scansToAverage;

                double[] tmp = await takeOneAsync(showProgress, disableLaserAfterFirstPacket);
                logger.debug("takeOneAveragedAsync: back from takeOneAsync");

                if (tmp is null || (spectrum != null && tmp.Length != spectrum.Length))
                {
                    if (tmp is null)
                        logger.error("takeOneAveragedAsnc: tmp is null");
                    else if (spectrum != null && tmp.Length != spectrum.Length)
                        logger.error($"takeOneAveragedAsnc: length changed ({tmp.Length} != {spectrum.Length})");

                    if (swRamanMode)
                        laserEnabled = false;

                    logger.error("takeOneAveragedAsnc: giving up");
                    return acquiring = false;
                }

                if (spectrum is null)
                    spectrum = tmp;
                else
                    for (int i = 0; i < spectrum.Length; i++)
                        spectrum[i] += tmp[i];    
            }

            if (scansToAverage > 1)
                for (int i = 0; i < spectrum.Length; i++)
                    spectrum[i] /= scansToAverage;

            // MZ: memory leak? [update: no]
            lastSpectrum = spectrum;
            measurement.reset();
            measurement.reload(this);
            logger.info($"acquired Measurement {measurement.measurementID}");

            // update battery if needed
            _ = await updateBatteryAsync();

            // logger.debug($"calling GC.Collect({GC.MaxGeneration})");
            // GC.Collect(GC.MaxGeneration);

            logger.debug("takeOneAveragedAsync: done");
            acquiring = false;
            return true;
        }

        // Take one spectrum (of many, if doing scan averaging).  This is private,
        // callers are expected to use takeOneAveragedAsync().
        // 
        // There is no need to disable the laser if returning NULL, as the caller
        // will do so anyway.
        private async Task<double[]> takeOneAsync(
            ProgressBarDelegate showProgress,
            bool disableLaserAfterFirstPacket)
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

            var spectrumChar = characteristicsByName["readSpectrum"];
            if (spectrumChar is null)
            {
                logger.error("can't find characteristic spectrum");
                return null;
            }

            // send acquire command
            logger.debug("takeOneAsync: sending SPECTRUM_ACQUIRE");
            byte[] request = ToBLEData.convert(true);
            if (! await acquireChar.WriteAsync(request))
            {
                logger.error("failed to send acquire");
                return null;
            }
            request = null;

            // YOU ARE HERE -- when laserState.laserEnable is true, the app
            // just hangs here.  We don't get the above "failed to send acquire",
            // yet nor do we get the following "waiting" message.  
            //
            // This is when laserState was last sent as "00 00 01 03 00 00" 
            // (type normal, mode manual, enabled on, watchdog 3s, delay 0ms)
            //
            // Apparently it DOESN'T freeze when laserDelayMS is >0 (e.g. 500ms)

            // wait for acquisition to complete
            logger.debug($"takeOneAsync: waiting {integrationTimeMS}ms");
            await Task.Delay((int)integrationTimeMS);

            var spectrum = new double[pixels];
            UInt16 pixelsRead = 0;
            var retryCount = 0;
            bool requestRetry = false;
            bool haveDisabledLaser = false;

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

                    // if this is the first retry, assume that the sensor was
                    // powered-down, and we need to wait for some throwaway
                    // spectra 
                    if (retryCount == 1)
                        delayMS = (int)(integrationTimeMS * THROWAWAY_SPECTRA);

                    logger.error($"Retry requested, so waiting for {delayMS}ms");
                    await Task.Delay(delayMS);

                    requestRetry = false;
                }

                logger.debug($"takeOneAsync: requesting spectrum packet starting at pixel {pixelsRead}");
                request = ToBLEData.convert(pixelsRead, len: 2);
                if (! await spectrumRequestChar.WriteAsync(request))
                {
                    logger.error($"failed to write spectrum request for pixel {pixelsRead}");
                    return null;
                }

                logger.debug("reading spectrumChar");
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
                // logger.hexdump(response);

                var crc = Crc16.checksum(response);
                if (crc == lastCRC)
                {
                    logger.error($"received duplicate CRC 0x{crc:x4}, retrying");
                    requestRetry = true;
                    continue;
                }

                lastCRC = crc;

                // YOU ARE HERE:
                // This was the original SW Raman Mode design, seems to freeze
                // the acquisition -- maybe BLE doesn't like being interrupted 
                // in the middle of a spectrum readout?
                if (false && disableLaserAfterFirstPacket && !haveDisabledLaser && pixelsInPacket > 0)
                {
                    logger.debug("disabling laser after first valid spectrum packet received");
                    haveDisabledLaser = true;
                    laserEnabled = false;
                }

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
                response = null;

                showProgress(((double)totalPixelsRead) / totalPixelsToRead);
            }

            // YOU ARE HERE: kludge at end
            if (disableLaserAfterFirstPacket && !haveDisabledLaser)
            {
                logger.debug("disabling laser after complete spectrum received");
                laserEnabled = false;
                logger.debug("continuing end-of-spectrum processing after triggering laser disable");
            }

            // kludge: first four pixels are zero, so overwrite from 5th
            for (int i = 0; i < 4; i++)
                spectrum[i] = spectrum[4];

            // kludge: last pixel seems to be 0xff, so re-write from previous
            spectrum[pixels-1] = spectrum[pixels-2];

            // apply 2x2 binning
            if (eeprom.featureMask.bin2x2)            {                var smoothed = new double[spectrum.Length];                for (int i = 0; i < spectrum.Length - 1; i++)                    smoothed[i] = (spectrum[i] + spectrum[i + 1]) / 2.0;                smoothed[spectrum.Length - 1] = spectrum[spectrum.Length - 1];                spectrum = smoothed;            }

            logger.debug("Spectrometer.takeOneAsync: returning completed spectrum");
            return spectrum;
        }

        ////////////////////////////////////////////////////////////////////////
        // Notifications
        ////////////////////////////////////////////////////////////////////////

        public event PropertyChangedEventHandler PropertyChanged;

        public void processBatteryNotification(byte[] data)
        {
            if (data is null)
                return;

            logger.hexdump(data, "batteryStatus: ");
            battery.parse(data);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("batteryStatus"));
        }

        public void processLaserStateNotification(byte[] data)
        {
            if (data is null)
                return;

            logger.hexdump(data, "laserState: ");
            laserState.parse(data);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("laserState"));
        }
    }
}
