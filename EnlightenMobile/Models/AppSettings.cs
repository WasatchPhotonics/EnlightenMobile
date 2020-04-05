using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using EnlightenMobile.Services;

namespace EnlightenMobile.Models
{
    // This class represents application-wide settings.  It currently corresponds 
    // to ENLIGHTEN's Configuration (enlighten.ini) and SaveOptions classes, and
    // a bit of FileManager and common.py.
    public class AppSettings
    {
        static AppSettings instance = null;

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

        public string version => $"version {VersionTracking.CurrentVersion}";

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
        // SaveOptions / FileManager
        ////////////////////////////////////////////////////////////////////////

        public string getSavePath()
        {
            IPlatformUtil platformUtil = DependencyService.Get<IPlatformUtil>();
            return platformUtil.getSavePath();
        }
    }
}
