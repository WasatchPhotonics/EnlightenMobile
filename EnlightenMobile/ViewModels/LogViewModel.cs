using System;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace EnlightenMobile.ViewModels
{
    // I'm probably making this more complicated than it needs to be, explicitly
    // passing a delegate down into the Logger.  There's probably a much simpler
    // way to use notification events to automatically trigger GUI updates.  If
    // you can simplify this, please show me how :-)
    public class LogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        Logger logger = Logger.getInstance();

        public LogViewModel()
        {
            // pass the StringBuilder into the Logger to acrue messages
            logger.history = history;

            // give the Logger a callback to let the ViewModel know when the
            // StringBuilder has been updated (probably easier way to do this)
            logger.logChangedDelegate = RaisePropertyChanged;
        } 

        // when the GUI receives a notification to redraw the LogView, it will
        // call this to render the StringBuilder
        public string LogText 
        {
            get => history.ToString();
        }

        // this is where Logger text is actually acrued (whether notifications
        // are sent to the GUI or not)
        StringBuilder history = new StringBuilder("Log data");

        protected void RaisePropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogText)));
        }
    }
}
