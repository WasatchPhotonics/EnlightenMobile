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

        public string password
        {
            get => "••••••••";
            set
            {
                appSettings.authenticate(value);
            }
        }
    }
}
