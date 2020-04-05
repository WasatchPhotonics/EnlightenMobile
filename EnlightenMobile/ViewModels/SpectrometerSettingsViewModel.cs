using System.ComponentModel;
using System.Runtime.CompilerServices;
using EnlightenMobile.Models;

namespace EnlightenMobile.ViewModels
{
    // This class provides "transformation logic" to render the Model of the
    // EEPROM's ObservableList entries.  
    //
    // Not really; the ObservableList natively uses ViewableSetting objects, and 
    // this class does nothing except provide a "straight-through" copy of each 
    // ViewableSetting as it is rendered into a Cell of the ListView.  
    // 
    // This is the kind of verbose-yet-useless class that makes people hate MVVM.  
    // IF there's a way to obviate it, let me know.
    public class SpectrometerSettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        Logger logger = Logger.getInstance();

        public ViewableSetting ViewableSetting
        {
            get { return _viewableSetting; }
            set { _viewableSetting = value; }
        }
        ViewableSetting _viewableSetting;

        protected void OnPropertyChanged([CallerMemberName] string caller = "")
        {
            logger.debug("SSVM: OnPropertyChanged");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
    }
}
