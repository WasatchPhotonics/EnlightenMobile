using System;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using EnlightenMobile.Models;

namespace EnlightenMobile.ViewModels
{
    public class AboutViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        AppSettings appSettings = AppSettings.getInstance();

        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public AboutViewModel()
        {
            OpenWebCommand = new Command(async () => await Browser.OpenAsync(appSettings.companyURL));
        }

        ////////////////////////////////////////////////////////////////////////
        // Public Properties
        ////////////////////////////////////////////////////////////////////////

        public string title 
        {
            get => "About ENLIGHTEN™";
        }

        public string version
        {
            get => AppSettings.getInstance().version;
        }

        public ICommand OpenWebCommand { get; }
    }
} 
