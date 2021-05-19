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
            get => String.Format("About Enlighten");
        }

        public string version
        {
            get => String.Format("Enlighten Mobile version 1.0.1");
        }

        public ICommand OpenWebCommand { get; }
    }
} 
