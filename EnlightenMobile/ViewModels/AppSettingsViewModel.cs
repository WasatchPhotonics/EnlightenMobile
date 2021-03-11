using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Essentials;
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

        public void loadSettings()
        {
            bool savePixelValue = Preferences.Get("savePixel", false);
            bool saveWavelengthValue = Preferences.Get("saveWavelength", false);
            bool saveWavenumberValue = Preferences.Get("saveWavenumber", false);
            bool saveRawValue = Preferences.Get("saveRaw", false);
            bool saveDarkValue = Preferences.Get("saveDark", false);
            bool saveReferenceValue = Preferences.Get("saveReference", false);
            bool authValue = Preferences.Get("authenticated", false);

            appSettings.savePixel = savePixelValue;
            appSettings.saveWavelength = saveWavelengthValue;
            appSettings.saveWavenumber = saveWavenumberValue;
            appSettings.saveRaw = saveRawValue;
            appSettings.saveDark = saveDarkValue;
            appSettings.saveReference = saveReferenceValue;
            appSettings.authenticated = authValue;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(savePixel)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(saveWavelength)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(saveWavenumber)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(saveRaw)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(saveDark)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(saveReference)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(isAuthenticated)));
        }

        public string title
        {
            get => "Application Settings";
        }

        public bool savePixel 
        {
            get => appSettings.savePixel;
            set
            {
                appSettings.savePixel = value;
                Preferences.Set("savePixel", value);
                System.Console.WriteLine($"Changed save pixel to the following: {appSettings.savePixel.ToString()}");
            }
        }

        public bool saveWavelength
        {
            get => appSettings.saveWavelength;
            set
            {
                appSettings.saveWavelength = value;
                Preferences.Set("saveWavelength", value);
            }
        }

        public bool saveWavenumber 
        {
            get => appSettings.saveWavenumber;
            set
            {
                appSettings.saveWavenumber = value;
                Preferences.Set("saveWavenumber", value);
            }
        }

        public bool saveRaw 
        {
            get => appSettings.saveRaw;
            set
            {
                appSettings.saveRaw = value;
                Preferences.Set("saveRaw", value);
            }
        }

        public bool saveDark 
        {
            get => appSettings.saveDark;
            set
            {
                appSettings.saveDark = value;
                Preferences.Set("saveDark", value);
            }
        }

        public bool saveReference 
        {
            get => appSettings.saveReference;
            set
            {
                appSettings.saveReference = value;
                Preferences.Set("saveReference", value);
            }
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
            set
            {
                spec.laserWatchdogSec = value;
                Preferences.Set("laserWatchdog", value);
            }
        }

        public ushort laserDelayMS
        {
            get => spec.laserDelayMS;
            set
            {
                spec.laserDelayMS = value;
                Preferences.Set("laserDelay", value);
            }
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
                Preferences.Set("ROIStart", value);
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
                Preferences.Set("ROIStop", value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(verticalROIStopLine)));
        }
    }
}
