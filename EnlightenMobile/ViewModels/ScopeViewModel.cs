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

            chartData = new ObservableCollection<ChartDataPoint>();
            double halfMax = 50000.0 / 2.0;
            for (int i = 0; i < 1952; i++)
            {
                double intensity = halfMax + halfMax * Math.Sin(i * Math.PI * 2 / 1952.0);
                ChartDataPoint cdp = new ChartDataPoint() { intensity = intensity,
                                                            pixel = i,
                                                            wavelength = 800 + i/2.0,
                                                            wavenumber = i*1.1 };
                chartData.Add(cdp);
            }

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
            var ok = await spec.takeOneAveragedAsync(showProgress);
            if (ok)
            {
                chartData = generateChartData();

                logger.debug("sending updates on chartData and spectrumMax");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(chartData)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(spectrumMax)));
                logger.debug("updates sent");

                checkForBadMeasurement();
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(acquireButtonColor)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(batteryState)));
            showProgress(0);
            isRefreshing = false;

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
                scopeViewNotification?.Invoke("ERROR: spectrum is all zero");
            else if (allHigh)
                scopeViewNotification?.Invoke("ERROR: spectrum is all 0xff");
        }

        ////////////////////////////////////////////////////////////////////////
        // Chart
        ////////////////////////////////////////////////////////////////////////

        public ObservableCollection<ChartDataPoint> chartData { get; set; }

        private ObservableCollection<ChartDataPoint> generateChartData()
        {
            if (spec is null)
                return null;

            // get last-acquired spectrum
            Measurement m = spec.measurement;
            if (m is null)
                return null;

            if (m.processed is null || spec.wavelengths is null)
                return null;

            ObservableCollection<ChartDataPoint> data = new ObservableCollection<ChartDataPoint>();
            int count = m.processed.Length;
            var raman = spec.wavenumbers != null;
            for (int i = 0; i < count; i++)
            {
                var point = new ChartDataPoint() {
                    intensity = m.processed[i],
                    pixel = i,
                    wavelength = spec.wavelengths[i] };
                if (raman)
                    point.wavenumber = spec.wavenumbers[i];
                data.Add(point);
            }
            logger.debug($"replacing chartData with {chartData.Count} elements");
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
