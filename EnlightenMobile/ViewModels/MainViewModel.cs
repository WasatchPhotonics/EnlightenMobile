using System;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using EnlightenMobile.Models;

namespace EnlightenMobile.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        AppSettings appSettings = AppSettings.getInstance();

        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public MainViewModel()
        {
            OpenWebCommand = new Command(async () => await Browser.OpenAsync(appSettings.companyURL));
        }

        ////////////////////////////////////////////////////////////////////////
        // Public Properties
        ////////////////////////////////////////////////////////////////////////

        public string title 
        {
            get => String.Format("About {0}", App.appProjectSetting);
        }

        public string version
        {
            get => String.Format("{0} Mobile {1}", App.appProjectSetting, AppSettings.getInstance().version);
        }

        public ICommand OpenWebCommand { get; }
    }
} 
