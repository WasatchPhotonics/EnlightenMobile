using System.ComponentModel;
using System.Runtime.CompilerServices;
using EnlightenMobile.Models;

namespace EnlightenMobile.ViewModels
{
    // Provides the backing logic and bound properties shown on the AppSettingsView.
    public class AppSettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        AppSettings appSettings = AppSettings.getInstance();

        Spectrometer spec = Spectrometer.getInstance();
        Logger logger = Logger.getInstance();

        public AppSettingsViewModel()
        {
            laserWatchdogTimeoutSec = spec.laserWatchdogSec;
            laserDelayMS = spec.laserDelayMS;
        }

        public string title
        {
            get => "Application Settings";
        }

        public bool savePixel 
        {
            get => appSettings.savePixel;
            set => appSettings.savePixel = value;
        }

        public bool saveWavelength
        {
            get => appSettings.saveWavelength;
            set => appSettings.saveWavelength = value;
        }

        public bool saveWavenumber 
        {
            get => appSettings.saveWavenumber;
            set => appSettings.saveWavenumber = value;
        }

        public bool saveRaw 
        {
            get => appSettings.saveRaw;
            set => appSettings.saveRaw = value;
        }

        public bool saveDark 
        {
            get => appSettings.saveDark;
            set => appSettings.saveDark = value;
        }

        public bool saveReference 
        {
            get => appSettings.saveReference;
            set => appSettings.saveReference = value;
        }

        ////////////////////////////////////////////////////////////////////////
        // Authentication 
        ////////////////////////////////////////////////////////////////////////

        public string password
        {
            get => AppSettings.stars;
            set
            {
                // We are not doing anything here, because we don't want to
                // process per-character input (which is what the Entry binding
                // gives us); instead, wait until they hit return, which will
                // trigger the View's Complete method.  That method will then
                // call the authenticate() method below.
            }
        }

        public bool isAuthenticated
        {
            get => appSettings.authenticated;
        }

        // the user entered a new password on the view, so authenticate it
        public void authenticate(string password)
        {
            appSettings.authenticate(password);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(isAuthenticated)));
        }

        ////////////////////////////////////////////////////////////////////////
        // Advanced Features
        ////////////////////////////////////////////////////////////////////////

        public byte laserWatchdogTimeoutSec
        {
            get => spec.laserWatchdogSec;
            set => spec.laserWatchdogSec = value;
        }

        public ushort laserDelayMS
        {
            get => spec.laserDelayMS;
            set => spec.laserDelayMS = value;
        }

        public string verticalROIStartLine
        {
            get => spec.verticalROIStartLine.ToString();
            set { ; }
        }

        // the View's code-behind has registered that a final value has
        // been entered into the Entry (hit return), so latch it
        public void setVerticalROIStartLine(string s)
        {
            if (ushort.TryParse(s, out ushort value))
                spec.verticalROIStartLine = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(verticalROIStartLine)));
        }

        public string verticalROIStopLine
        {
            get => spec.verticalROIStopLine.ToString();
            set { ; }
        }

        // the View's code-behind has registered that a final value has
        // been entered into the Entry (hit return), so latch it
        public void setVerticalROIStopLine(string s)
        {
            if (ushort.TryParse(s, out ushort value))
                spec.verticalROIStopLine = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(verticalROIStopLine)));
        }
    }
}
