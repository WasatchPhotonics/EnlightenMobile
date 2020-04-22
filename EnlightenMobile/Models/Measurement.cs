using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EnlightenMobile.Models
{

    // corresponds to ENLIGHTEN and WasatchNET's Measurement class
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

        Logger logger = Logger.getInstance();

        public Measurement(Spectrometer spec)
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

            processed = (double[]) raw.Clone();

            dark = spec.dark;
            applyDark();
           
            var serialNumber = spec is null ? "sim" : spec.eeprom.serialNumber;
            measurementID = string.Format("enlighten-{0}-{1}", 
                timestamp.ToString("yyyyMMdd-HHmmss-ffffff"), 
                serialNumber);
            filename = measurementID + ".csv";
        }

        public double max => processed is null ? 0 : processed.Max();

        void applyDark()
        {
            if (dark is null || raw is null || dark.Length != raw.Length)
                return;

            for (int i = 0; i < raw.Length; i++)
                processed[i] -= dark[i];
        }

        public void averageAlternating()
        {
            // just do the processed array
            averageAlternatingArray(processed);
        }

        // average-over odd pixels from adjascent even neighbors
        void averageAlternatingArray(double[] a)
        {
            if (a is null)
            {
                logger.error("can't average null array");
                return;
            }
            for (int i = 1; i + 1 < a.Length; i += 2)
                a[i] = (a[i - 1] + a[i + 1]) / 2.0;
        }

        /// <returns>true on success</returns>
        /// <todo>
        /// - support full ENLIGHTEN metadata
        /// - support SaveOptions (selectable output fields)
        /// </todo>
        public bool save()
        {
            logger.debug("Measurement.save: starting");

            if (processed is null || raw is null)
            {
                logger.error("saveAsync: nothing to save");
                return false;
            }

            AppSettings appSettings = AppSettings.getInstance();
            string savePath = appSettings.getSavePath();
            if (savePath is null)
            {
                logger.error("saveAsync: can't get savePath");
                return false;
            }

            string pathname = string.Format($"{savePath}/{filename}");
            logger.debug($"Measurement.saveAsync: creating {pathname}");

            using (StreamWriter sw = new StreamWriter(pathname))  
            {  
                writeMetadata(sw);
                sw.WriteLine();
                writeSpectra(sw);
            }

            return true;
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
            sw.WriteLine("Laser Enable, {0}", spec.laserEnabled);
            sw.WriteLine("Laser Wavelength, {0}", spec.eeprom.laserExcitationWavelengthNMFloat);
            sw.WriteLine("Note, {0}", spec.note);
            sw.WriteLine("Pixel Count, {0}", spec.eeprom.activePixelsHoriz);
            sw.WriteLine("Host Description, {0}", appSettings.hostDescription);
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

            if (appSettings.savePixel     ) headers.Add("Pixel");
            if (appSettings.saveWavelength) headers.Add("Wavelength");
            if (appSettings.saveWavenumber) headers.Add("Wavenumber");
                                            headers.Add("Processed");
            if (appSettings.saveRaw       ) headers.Add("Raw");
            if (appSettings.saveDark      ) headers.Add("Dark");
            if (appSettings.saveReference ) headers.Add("Reference");

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
