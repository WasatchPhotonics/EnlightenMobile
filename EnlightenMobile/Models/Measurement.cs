using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Essentials;

namespace EnlightenMobile.Models
{
    // Mostly corresponds to ENLIGHTEN and WasatchNET's Measurement classes,
    // but right now we're re-using the existing measurement (via reload()) 
    // whilst tracking down a rogue memory leak.
    public class Measurement
    {
        public double[] raw = null;
        public double[] dark = null;
        public double[] reference = null;
        public double[] processed = null;

        Spectrometer spec;

        public DateTime timestamp = DateTime.Now;
        public string filename;
        public string measurementID;
        public Location location;

        Logger logger = Logger.getInstance();

        public void reset()
        {
            raw = dark = reference = processed = null;
            filename = measurementID = null;
            spec = null;
        }

        public Measurement()
        {
            reset();
        }

        public void reload(Spectrometer spec)
        {
            this.spec = spec;

            if (spec.lastSpectrum is null)
            {
                // for testing, default measurements with a sine-wave
                raw = new double[spec.pixels];
                double halfMax = 50000.0 / 2.0;
                for (int x = 0; x < raw.Length; x++)
                    raw[x] = halfMax + halfMax * Math.Sin(x * Math.PI * 2 / raw.Length);
            }
            else
            {
                raw = spec.lastSpectrum;
            }

            processed = (double[]) raw.Clone(); // MZ: needed?

            dark = spec.dark;
            applyDark();
           
            var serialNumber = spec is null ? "sim" : spec.eeprom.serialNumber;
            measurementID = string.Format("enlighten-{0}-{1}", 
                timestamp.ToString("yyyyMMdd-HHmmss-ffffff"), 
                serialNumber);
            filename = measurementID + ".csv";

            location = WhereAmI.getInstance().location;
        }

        public double max => processed is null ? 0 : processed.Max();

        void applyDark()
        {
            if (dark is null || raw is null || dark.Length != raw.Length)
                return;

            for (int i = 0; i < raw.Length; i++)
                processed[i] -= dark[i];
        }

        /// <returns>true on success</returns>
        /// <todo>
        /// - support full ENLIGHTEN metadata
        /// - support SaveOptions (selectable output fields)
        /// </todo>
        public string savePath;
        private bool needWriteMetaData = true;
        public bool save()
        {
            logger.debug("Measurement.save: starting");

            if (processed is null || raw is null || spec is null)
            {
                logger.error("saveAsync: nothing to save");
                return false;
            }

            AppSettings appSettings = AppSettings.getInstance();
            if (!appSettings.appendSpectra)
            {
                savePath = appSettings.getSavePath();
            }
            else
            {
                // the idea here is that if spectra is supposed to be appended and there is no prior save path get one
                // otherwise continue on and use what was perviously stored in savePath so that it will be appended
                if(savePath is null)
                {
                    savePath = appSettings.getSavePath();
                    needWriteMetaData = true;
                }
            }
            
            if (savePath is null)
            {
                logger.error("saveAsync: can't get savePath");
                return false;
            }

            string pathname = string.Format($"{savePath}/{filename}");
            logger.debug($"Measurement.saveAsync: creating {pathname}");

            using (StreamWriter sw = new StreamWriter(pathname))  
            {
                if (!appSettings.saveByRow)
                {
                    logger.info("saving by column");
                    writeMetadata(sw);
                    sw.WriteLine();
                    writeSpectra(sw);
                }
                else
                {
                    logger.info("saving by row");
                    if (needWriteMetaData)
                    {
                        writeRowMetadata(sw); //need to change to row version
                        needWriteMetaData = false;
                    }
                    writeRowData(sw);
                }
                
            }

            return true;
        }

        void writeRowMetadata(StreamWriter sw)
        {
            var appSettings = AppSettings.getInstance();
            //meta data for EnlightenMobile does not match with Enlighten Desktop
            //For consistency I kept the mobile metadata the same
            //If in the future there is the desire to have this the same it can reasonably be changed
            sw.Write("ENLIGHTEN Version,");
            sw.Write("Measurement ID,");
            sw.Write("Serial Number,");
            sw.Write("Model");
            sw.WriteLine();
            sw.Write($"Mobile {appSettings.version} for {appSettings.os},");
            sw.Write($"{measurementID},");
            sw.Write($"{spec.eeprom.serialNumber},`");
            sw.Write($"{spec.eeprom.model}");
            //The above meta data shouldn't change much but the rest likely will, which is the reason for the split
            sw.WriteLine();
            sw.Write("Integrtion Time,");
            sw.Write("Detector Gain,");
            sw.Write("Scan Averaging,");
            sw.Write("Laser Enable,");
            sw.Write("Laser Wavelength,");
            sw.Write("Timestamp,");
            sw.Write("Note,");
            sw.Write("Pixel Count");
            for(int i = 0; i < spec.eeprom.activePixelsHoriz; i++)
            {
                sw.Write($"{i},");
            }
            sw.WriteLine();
        }

        void writeRowData(StreamWriter sw)
        {
            logger.debug("writeSpectra: starting (by row)");
            AppSettings appSettings = AppSettings.getInstance();

            List<string> headers = new List<string>();

            double[] pix = new double[processed.Length];

            for(int i = 0; i < processed.Length; i++)
            {
                pix[i] = i;
            }

            Dictionary<string, double[]> headerMatch = new Dictionary<string, double[]>();

            if (appSettings.savePixel)
            { 
                headers.Add("Pixel"); 
                headerMatch.Add("Pixel",pix);
            }
            if (appSettings.saveWavelength)
            {
                headers.Add("Wavelength");
                headerMatch.Add("Wavelength", spec.wavelengths);
            }
            if (appSettings.saveWavenumber)
            {
                headers.Add("Wavenumber");
                headerMatch.Add("Wavenumber", spec.wavenumbers);
            }
            headers.Add("Processed");
            headerMatch.Add("Processed", processed);
            if (appSettings.saveRaw)
            {
                headers.Add("Raw");
                headerMatch.Add("Raw", raw);
            }
            if (appSettings.saveDark)
            {
                headers.Add("Dark");
                headerMatch.Add("Dark", dark);
            }
            if (appSettings.saveReference)
            {
                headers.Add("Reference");
                headerMatch.Add("Reference", reference);
            }
            foreach(string head in headers)
            {
                double[] headerValues = null;
                sw.Write($"{spec.eeprom.maxIntegrationTimeMS},");
                sw.Write($"{spec.gainDb},");
                sw.Write($"{spec.scansToAverage},");
                sw.Write($"{spec.laserEnabled || spec.ramanModeEnabled},");
                sw.Write($"{spec.eeprom.laserExcitationWavelengthNMFloat},");
                sw.Write($"{timestamp.ToString()},");
                sw.Write($"{head},");
                headerValues = headerMatch[head];
                logger.info($"Trying to access header match for {head}");
                if(!(headerValues is null))
                {
                    for(int i = 0; i < processed.Length; i++)
                    {
                        sw.Write($"{headerValues[i]},");
                    }
                }
            }

        }

        void writeMetadata(StreamWriter sw)
        { 
            var appSettings = AppSettings.getInstance();

            // not the full ENLIGHTEN set, but the key ones for now
            sw.WriteLine("ENLIGHTEN Version, Mobile {0} for {1}", appSettings.version, appSettings.os);
            sw.WriteLine("Measurement ID, {0}", measurementID);
            sw.WriteLine("Serial Number, {0}", spec.eeprom.serialNumber);
            sw.WriteLine("Model, {0}", spec.eeprom.model);
            sw.WriteLine("Integration Time, {0}", spec.integrationTimeMS);
            sw.WriteLine("Detector Gain, {0}", spec.gainDb);
            sw.WriteLine("Scan Averaging, {0}", spec.scansToAverage);
            sw.WriteLine("Laser Enable, {0}", spec.laserEnabled || spec.ramanModeEnabled);
            sw.WriteLine("Laser Wavelength, {0}", spec.eeprom.laserExcitationWavelengthNMFloat);
            sw.WriteLine("Timestamp, {0}", timestamp.ToString());
            sw.WriteLine("Note, {0}", spec.note);
            sw.WriteLine("Pixel Count, {0}", spec.eeprom.activePixelsHoriz);

            ////////////////////////////////////////////////////////////////////
            // a few that ENLIGHTEN doesn't have...
            ////////////////////////////////////////////////////////////////////

            sw.WriteLine("Host Description, {0}", appSettings.hostDescription);
            if (location != null)
                sw.WriteLine("Location, lat {0}, lon {1}", location.Latitude, location.Longitude);
        }

        string render(double[] a, int index, string format="f2")
        {
           if (a is null || index >= a.Length)
                return "";

           var fmt = "{0:" + format + "}";
           return string.Format(fmt, a[index]);
        }

        void writeSpectra(StreamWriter sw)
        { 
            logger.debug("writeSpectra: starting");
            AppSettings appSettings = AppSettings.getInstance();

            List<string> headers = new List<string>();

            // reference-based techniques should output higher precision
            string fmt = reference is null ? "f2" : "f5";

            sw.WriteLine(string.Join(", ", headers));

            for (int i = 0; i < processed.Length; i++)
            {
                List<string> values = new List<string>();

                if (appSettings.savePixel     ) values.Add(i.ToString());
                if (appSettings.saveWavelength) values.Add(render(spec.wavelengths, i));
                if (appSettings.saveWavenumber) values.Add(render(spec.wavenumbers, i));
                                                values.Add(render(processed, i, fmt));
                if (appSettings.saveRaw       ) values.Add(render(raw, i));
                if (appSettings.saveDark      ) values.Add(render(dark, i));
                if (appSettings.saveReference ) values.Add(render(reference, i));

                sw.WriteLine(string.Join(", ", values));
            }
            logger.debug("writeSpectra: done");
        }
    }
}
