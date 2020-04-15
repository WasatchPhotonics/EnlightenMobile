using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using EnlightenMobile.Models;
using System.Threading.Tasks;
using Telerik.XamarinForms;
using Telerik.XamarinForms.Chart;

namespace EnlightenMobile.ViewModels
{
    // This class provides all the business logic controlling the ScopeView. 
    public class ScopeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // A "clean-ish" way for the ViewModel to raise events in the View's
        // code-behind
        // https://stackoverflow.com/a/26038700/11615696
        public delegate void ScopeViewNotification(string msg);
        public event ScopeViewNotification scopeViewNotification;

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
            logger.debug("SVM: starting ctor");

            logger.debug("SVM: getting Spectrometer instance");
            spec = Spectrometer.getInstance();
            logger.debug("SVM: back from Spectrometer instance");

            // bind closures (method calls) to each Command
            acquireCmd = new Command(() => { _ = doAcquireAsync(); });
            refreshCmd = new Command(() => { _ = doAcquireAsync(); }); 
            saveCmd    = new Command(() => { _ = doSave        (); });

            logger.debug("SVM: instantiating XAxisOptions");
            xAxisOptions = new ObservableCollection<XAxisOption>()
            {
                // these names must match the fields in ChartDataPoint
                new XAxisOption() { name = "pixel", unit = "px" },
                new XAxisOption() { name = "wavelength", unit = "nm" },
                new XAxisOption() { name = "wavenumber", unit = "cm⁻¹" }
            };
            xAxisOption = xAxisOptions[0];

            chartData = new ObservableCollection<ChartDataPoint>();
            updateChart();

            logger.debug("SVM: finished ctor");
        } 

        ////////////////////////////////////////////////////////////////////////
        //
        //                          Bound Properties
        //
        ////////////////////////////////////////////////////////////////////////

        public string title
        {
            get => "Scope Mode";
        }

        ////////////////////////////////////////////////////////////////////////
        // X-Axis
        ////////////////////////////////////////////////////////////////////////

        public ObservableCollection<XAxisOption> xAxisOptions { get; set; }
        public XAxisOption xAxisOption
        {
            get => _xAxisOption;
            set
            {
                logger.debug($"xAxisOption -> {value}");
                _xAxisOption = value;
                updateChart();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(xAxisLabelFormat)));
            }
        }
        XAxisOption _xAxisOption;

        public double xAxisMinimum
        {
            get => _xAxisMinimum;
            set
            {
                _xAxisMinimum = value;
               PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(xAxisMinimum)));
            }
        }
        double _xAxisMinimum;

        public double xAxisMaximum
        {
            get => _xAxisMaximum;
            set
            {
                _xAxisMaximum = value;
               PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(xAxisMaximum)));
            }
        }
        double _xAxisMaximum;

        public string xAxisLabelFormat
        {
            get => xAxisOption.name == "pixel" ? "F0" : "F2";
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
                    spec.gainDb = 24;
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
        // Refresh
        ////////////////////////////////////////////////////////////////////////

        public bool isRefreshing
        {
            get => _isRefreshing;
            set 
            {
                logger.debug($"SVM: isRefreshing -> {value}");
                _isRefreshing = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(isRefreshing)));
            }
        }
        bool _isRefreshing;

        // invoked by ScopeView when the user pulls-down on the Scope Options grid
        // @todo consider whether this feature should user-configurable, as an 
        //       accidental acquisition could be destructive of both data and 
        //       health (as the laser could auto-fire)
        public Command refreshCmd { get; }

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

        public string batteryColor
        { 
            get => spec.battery.level > 20 ? "#eee" : "#f33";
        }

        ////////////////////////////////////////////////////////////////////////
        // Acquire Command
        ////////////////////////////////////////////////////////////////////////

        public string acquireButtonColor
        {
            get
            {
                // should somehow get this into the XAML itself, or perhaps the
                // code-behind (which could also set .IsEnabled)
                return spec.acquiring ? "#ba0a0a" : "#ccc";
            }
        }

        // invoked by ScopeView when the user clicks "Acquire" 
        public Command acquireCmd { get; }

        // the user clicked the "Acquire" button on the Scope View
        async Task<bool> doAcquireAsync()
        {
            if (spec.acquiring)
                return false;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(acquireButtonColor)));

            // take a fresh Measurement
            var startTime = DateTime.Now;
            var ok = await spec.takeOneAveragedAsync(showProgress);
            if (ok)
            {
                // info-level logging so we can QC timing w/o verbose logging
                var elapsedMS = (DateTime.Now - startTime).TotalMilliseconds;
                logger.info($"Completed acquisition in {elapsedMS} ms");

                updateChart();

                // later we could decide not to graph bad measurements, or not log
                // elapsed time, but this is fine for now
                _ = isGoodMeasurement();
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(acquireButtonColor)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(batteryState)));
            showProgress(0);
            isRefreshing = false;

            return ok;
        }

        void updateChart()
        {
            chartData = generateChartData();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(chartData)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(spectrumMax)));
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

        bool isGoodMeasurement()
        {
            Measurement m = spec.measurement;
            if (m is null || m.raw is null)
                return false;

            var allZero = true;                
            var allHigh = true;                
            for (int i = 0; i < m.raw.Length; i++)
            {
                if (m.raw[i] !=     0) allZero = false;
                if (m.raw[i] != 65535) allHigh = false;

                // no point checking beyond this point
                if (!allHigh && !allZero)
                    return true;
            }

            if (allZero)
                scopeViewNotification?.Invoke("ERROR: spectrum is all zero");
            else if (allHigh)
                scopeViewNotification?.Invoke("ERROR: spectrum is all 0xff");
            return !(allZero || allHigh);
        }

        ////////////////////////////////////////////////////////////////////////
        // Chart
        ////////////////////////////////////////////////////////////////////////

        public ObservableCollection<ChartDataPoint> chartData { get; set; }

        private ObservableCollection<ChartDataPoint> generateChartData()
        {
            // use last Measurement from the Spectrometer
            uint pixels = spec.pixels;
            double[] intensities = spec.measurement.processed;

            // pick our x-axis
            double[] xAxis = null;
            if (xAxisOption.name == "pixel")
            {
                xAxis = new double[pixels];
                for (int i = 0; i < spec.pixels; i++)
                    xAxis[i] = i;
            }
            else if (xAxisOption.name == "wavelength")
                xAxis = spec.wavelengths;
            else if (xAxisOption.name == "wavenumber")
                xAxis = spec.wavenumbers;

            if (intensities is null || xAxis is null)
                return null;

            ObservableCollection<ChartDataPoint> data = new ObservableCollection<ChartDataPoint>();
            for (int i = 0; i < pixels; i++)
                data.Add(new ChartDataPoint() { intensity = intensities[i], xValue = xAxis[i] });

            xAxisMinimum = xAxis[0];
            xAxisMaximum = xAxis[pixels-1];

            return data;
        }

        ////////////////////////////////////////////////////////////////////////
        // Save Command
        ////////////////////////////////////////////////////////////////////////

        // invoked by ScopeView when the user clicks "Save" 
        public Command saveCmd { get; }

        // the user clicked the "Save" button on the Scope View
        bool doSave()
        {
            var ok = spec.measurement.save();
            if (ok)
                scopeViewNotification?.Invoke($"saved {spec.measurement.filename}");
            return ok;
        }

        // This is required, but I don't remember how / why
        protected void OnPropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
    }
}
