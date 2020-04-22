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

        // So the ScopeViewModel can float-up Toast events to the ScopeView.
        // This probably could be done using notifications, but I'm not sure I
        // want to make a "public string toastMessage" Property, and I'm not
        // sure what the "best practice" architecture would be.
        public delegate void ToastNotification(string msg);
        public event ToastNotification notifyToast;

        ////////////////////////////////////////////////////////////////////////
        // Private attributes
        ////////////////////////////////////////////////////////////////////////

        Spectrometer spec;
        AppSettings appSettings;
        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public ScopeViewModel()
        {
            spec = Spectrometer.getInstance();
            appSettings = AppSettings.getInstance();

            appSettings.PropertyChanged += handleAppSettingsChange;

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
        // integrationTimeMS
        ////////////////////////////////////////////////////////////////////////

        public string integrationTimeMS 
        {
            get => spec.integrationTimeMS.ToString();
        }

        // the ScopeView's code-behind has registered that a final value has
        // been entered into the Entry (hit return), so latch it
        public void setIntegrationTimeMS(string s)
        {
            if (UInt32.TryParse(s, out UInt32 value))
                spec.integrationTimeMS = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(integrationTimeMS)));
        }

        ////////////////////////////////////////////////////////////////////////
        // gainDb
        ////////////////////////////////////////////////////////////////////////

        public string gainDb
        {
            get => spec.gainDb.ToString();
        }

        // the ScopeView's code-behind has registered that a final value has
        // been entered into the Entry (hit return), so latch it
        public void setGainDb(string s)
        {
            if (float.TryParse(s, out float value))
                spec.gainDb = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(gainDb)));
        }

        ////////////////////////////////////////////////////////////////////////
        // scansToAverage
        ////////////////////////////////////////////////////////////////////////

        public string scansToAverage
        {
            get => spec.scansToAverage.ToString();
        }

        // the ScopeView's code-behind has registered that a final value has
        // been entered into the Entry (hit return), so latch it
        public void setScansToAverage(string s)
        {
            if (ushort.TryParse(s, out ushort value))
                spec.scansToAverage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(scansToAverage)));
        }

        ////////////////////////////////////////////////////////////////////////
        // misc acquisition parameters
        ////////////////////////////////////////////////////////////////////////

        public bool darkEnabled
        {
            get => spec.dark != null;
            set => spec.toggleDark();
        }

        // this can probably be deprecated
        public bool alternatingEnabled
        {
            get => spec.alternatingEnabled;
            set => spec.alternatingEnabled = value;
        }

        public string note
        {
            get => spec.note;
            set => spec.note = value;
        }

        ////////////////////////////////////////////////////////////////////////
        // Laser Shenanigans
        ////////////////////////////////////////////////////////////////////////

        public bool laserEnabled
        {
            get => spec.laserEnabled;
            set => spec.laserEnabled = value;
        }

        public bool ramanModeEnabled
        {
            get => spec.ramanModeEnabled;
            set => spec.ramanModeEnabled = value;
        }

        // Provided so the "Laser Enable" Switch is disabled if we're in Raman Mode.
        //
        // This probably looks like the most useless Property ever...in fact,
        // it's strangely messy to invert a boolean in XAML, so here we are.
        public bool ramanModeDisabled
        {
            get => !ramanModeEnabled;
        }

        // Provided so the View can only show/enable certain controls if we're
        // logged-in.
        public bool isAuthenticated
        {
            get => AppSettings.getInstance().authenticated;
        }

        // Provided so any changes to AppSettings.authenticated will immediately
        // take effect on our View.
        void handleAppSettingsChange(object sender, PropertyChangedEventArgs e)
        {
            logger.debug($"SVM.handleAppSettingsChange: received notification from {sender}, so refreshing isAuthenticated");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(isAuthenticated)));
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
            else
            {
                notifyToast?.Invoke("Error reading spectrum");
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
                notifyToast?.Invoke("ERROR: spectrum is all zero");
            else if (allHigh)
                notifyToast?.Invoke("ERROR: spectrum is all 0xff");
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
                notifyToast?.Invoke($"saved {spec.measurement.filename}");
            return ok;
        }

        // This is required, but I don't remember how / why
        protected void OnPropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
    }
}
