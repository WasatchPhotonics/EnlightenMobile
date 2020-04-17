using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Essentials;
using EnlightenMobile.Services;

namespace EnlightenMobile.Models
{
    // This class represents application-wide settings.  It currently corresponds 
    // to ENLIGHTEN's Configuration (enlighten.ini) and SaveOptions classes, and
    // a bit of FileManager and common.py.
    public class AppSettings : INotifyPropertyChanged
    {
        static AppSettings instance = null;

        // so it can send out notifications that authentication has changed, to
        // anyone interested in authentication status
        public event PropertyChangedEventHandler PropertyChanged;

        // where to save spectra on the internet
        public string saveURL;

        // if provided, an override directing where to save spectra on the filesystem (else use default path)
        public string savePath;

        // todo: move to SaveOptions
        public bool savePixel = true;
        public bool saveWavelength = true;
        public bool saveWavenumber = true;
        public bool saveRaw = true;
        public bool saveDark = true;
        public bool saveReference = false;

        // todo: prompt to auto-connect this device if found on scan
        // public Guid lastConnectedGuid;

        public string version
        {
            get => $"version {VersionTracking.CurrentVersion}";
        }

        public string companyURL = "https://wasatchphotonics.com";

        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        static public AppSettings getInstance()
        {
            if (instance is null)
                instance = new AppSettings();
            return instance;
        }

        AppSettings()
        {
        }

        ////////////////////////////////////////////////////////////////////////
        // Device / Platform
        ////////////////////////////////////////////////////////////////////////

        public string os
        {
            get => Device.RuntimePlatform.ToString();
        }

        public string hostDescription
        {
            get
            {
                var model = DeviceInfo.Model; // SMG-950U, iPhone10,6 etc
                var manuf = DeviceInfo.Manufacturer; // Samsung, Apple etc
                var name = DeviceInfo.Name; // "Mark's iPhone" etc
                var version = DeviceInfo.VersionString; // 7.0 etc
                var os = DeviceInfo.Platform; // Android, iOS etc
                // var idiom = DeviceInfo.Idiom; // Phone, Tablet, Watch, TV etc
                return $"{name} ({manuf} {model} running {os} {version})";
            }
        }

        ////////////////////////////////////////////////////////////////////////
        // SaveOptions / FileManager
        ////////////////////////////////////////////////////////////////////////

        public string getSavePath()
        {
            IPlatformUtil platformUtil = DependencyService.Get<IPlatformUtil>();
            return platformUtil.getSavePath();
        }

        ////////////////////////////////////////////////////////////////////////
        // Authentication
        ////////////////////////////////////////////////////////////////////////

        // This exposes Production Quality Control (test/verification) operations
        // normally not exposed to the end-user.
        //
        // @warning This mode increases opportunity for laser eye injury due to
        //          operator error.  Do not enable without cause and appropriate
        //          Personal Protective Equipment.
        public bool authenticated
        {
            get => _authenticated;
            set
            {
                _authenticated = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(authenticated)));
            }
        }
        bool _authenticated;

        // Obviously this is not a way to conceal genuinely dangerous
        // functionality in an open-source project.  Programmers can access the
        // full BLE or USB API all they want.  This is meant to keep casual
        // users from accidentally enabling dangerous test-mode behaviors by
        // simply clicking the wrong button.
        public void authenticate(string password)
        {
            const string EXPECTED_PASSWORD = "DangerMan";
            authenticated = password == EXPECTED_PASSWORD;
        }
    }
}
