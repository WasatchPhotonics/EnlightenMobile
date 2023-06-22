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

        Settings settings = Settings.getInstance();

        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public AboutViewModel()
        {
            OpenWebCommand = new Command(async () => await Browser.OpenAsync(settings.companyURL));
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
            get => String.Format("Enlighten Mobile version 1.1.0");
        }

        public ICommand OpenWebCommand { get; }
    }
} 
