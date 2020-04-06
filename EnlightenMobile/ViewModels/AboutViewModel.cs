using System;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace EnlightenMobile.ViewModels
{
    public class AboutViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        Logger logger = Logger.getInstance();

        public string Title = "About";

        public ICommand OpenWebCommand { get; }

        public AboutViewModel()
        {
            OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://xamarin.com"));
        }

    }
} 