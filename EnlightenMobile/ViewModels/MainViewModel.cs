using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using EnlightenMobile.Models;

namespace EnlightenMobile.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel() {}

        public string appVersion => AppSettings.getInstance().version;

        protected void RaisePropertyChanged ([CallerMemberName] string caller ="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
    }
}
