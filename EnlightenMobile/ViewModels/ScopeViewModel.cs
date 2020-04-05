using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Microcharts;
using Microcharts.Forms;
using EnlightenMobile.Models;
using System.Threading.Tasks;

namespace EnlightenMobile.ViewModels
{
    // This class provides all the business logic controlling the ScopeView. 
    public class ScopeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        ////////////////////////////////////////////////////////////////////////
        // Private attributes
        ////////////////////////////////////////////////////////////////////////

        Spectrometer spec = null;

        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public ScopeViewModel()
        {
            spec = Spectrometer.getInstance();

            // bind closures (method calls) to each Command
            acquireCmd = new Command(() => { _ = doAcquireAsync(); });
            saveCmd    = new Command(() => { _ = doSave        (); });
        } 

        ////////////////////////////////////////////////////////////////////////
        //
        //                          Bound Properties
        //
        ////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////
        // Status Bar
        ////////////////////////////////////////////////////////////////////////

        public string spectrumMax
        { 
            get => string.Format("Max: {0:f2}", spec.measurement.max);
        }

        public string batteryState 
        { 
            get => spec.battery.ToString();
        }

        ////////////////////////////////////////////////////////////////////////
        // Acquisition Parameters
        ////////////////////////////////////////////////////////////////////////

        public string integrationTimeMS 
        {
            get => spec.integrationTimeMS.ToString();
            set
            {
                ushort val = 0;
                if (UInt16.TryParse(value, out val))
                    spec.integrationTimeMS = val;
                else
                    spec.integrationTimeMS = 3;
            }
        }

        public string gainDb
        {
            get => spec.gainDb.ToString();
            set
            {
                ushort val = 0;
                if (UInt16.TryParse(value, out val))
                    spec.gainDb = val;
                else
                    spec.integrationTimeMS = 24;
            }
        }

        public string scansToAverage
        {
            get => spec.scansToAverage.ToString();
            set
            {
                ushort val = 0;
                if (UInt16.TryParse(value, out val))
                    spec.scansToAverage = val;
                else
                    spec.scansToAverage = 1;
            }
        }

        public string note
        {
            get => spec.note;
            set => spec.note = value;
        }

        public bool laserEnabled
        {
            get => spec.laserEnabled;
            set => spec.laserEnabled = value;
        }

        public bool darkEnabled
        {
            get => spec.dark != null;
            set => spec.toggleDark();
        }

        public bool alternatingEnabled
        {
            get => spec.alternatingEnabled;
            set => spec.alternatingEnabled = value;
        }

        ////////////////////////////////////////////////////////////////////////
        // Acquire Command
        ////////////////////////////////////////////////////////////////////////

        // invoked by ScopeView when the user clicks "Acquire" 
        public Command acquireCmd { get; }

        // the user clicked the "Acquire" button on the Scope View
        async Task<bool> doAcquireAsync()
        {
            // take a fresh Measurement
            var ok = await spec.takeOneAveragedAsync(showProgress);
            if (ok)
            {
                _chart = generateChart();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(chart)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(spectrumMax)));

                checkForBadMeasurement();
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(batteryState)));
            showProgress(0);

            return ok;
        }

        // This is a callback (delegate) passed down into Spectrometer so it can
        // update our acquisitionProgress property while reading BLE packets.
        void showProgress(double progress) => acquisitionProgress = progress; 

        // this is a floating-point "percentage completion" backing the 
        // ProgressBar on the ScopeView
        public double acquisitionProgress
        {
            get => _acquisitionProgress;
            set 
            {
                _acquisitionProgress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(acquisitionProgress)));
            }
        }
        double _acquisitionProgress;

        void checkForBadMeasurement()
        {
            Measurement m = spec.measurement;
            if (m is null || m.raw is null)
                return;

            var allZero = true;                
            var allHigh = true;                
            for (int i = 0; i < m.raw.Length; i++)
            {
                if (m.raw[i] !=     0) allZero = false;
                if (m.raw[i] != 65535) allHigh = false;
            }

            if (allZero)
                Util.toast("ERROR: spectrum is all zero");
            else if (allHigh)
                Util.toast("ERROR: spectrum is all 0xff");
        }

        ////////////////////////////////////////////////////////////////////////
        // Chart
        ////////////////////////////////////////////////////////////////////////

        // The private attribute will be populated asynchronously by the Acquire
        // Command. After the new chart is built, generateChart() will invoke a
        // PropertyChangedNotification with the name of this attribute, which
        // will cause the ScopeView to call this gettor and retrieve the new
        // chart for display.
        public Chart chart
        { 
            get => _chart;
        }
        Chart _chart;

        // convert the Spectrometer's latest Measurement into a Chart
        private Chart generateChart()
        {
            // get spectrum
            Measurement m = spec.measurement;

            // generate (x, y) datapoints
            List<Microcharts.Entry> entries = new List<Microcharts.Entry>();

            int count = m.processed.Length;
            for (int i = 0; i < count; i++)
            {
                // TODO: include X-axis
                var intensity = m.processed[i];
                entries.Add(new Microcharts.Entry((float)intensity) { Color=SkiaSharp.SKColors.Teal });
            }

            // instantiate chart from values
            LineChart lc = new LineChart() { Entries = entries.ToArray() };
            lc.PointMode = PointMode.Circle;
            lc.LineMode = LineMode.Straight;
            lc.LabelTextSize = 0;
            lc.BackgroundColor = SkiaSharp.SKColors.Black;

            return lc;
        }

        ////////////////////////////////////////////////////////////////////////
        // Save Command
        ////////////////////////////////////////////////////////////////////////

        // invoked by ScopeView when the user clicks "Save" 
        public Command saveCmd { get; }

        // the user clicked the "Save" button on the Scope View
        bool doSave()
        {
            return spec.measurement.save();
        }

        // This is required, but I don't remember how / why
        protected void OnPropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
    }
}
