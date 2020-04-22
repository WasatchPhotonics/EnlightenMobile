using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EnlightenMobile.ViewModels
{
    // Provides the backing logic and bound properties shown on the BluetoothView.
    // Arguably, much of BluetoothView.cs should be moved here. Jury's still out
    // on that one.
    public class BluetoothViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // Public Properties
        ////////////////////////////////////////////////////////////////////////

        public string title
        {
            get => "Bluetooth Pairing";
        }

        public bool paired
        {
            get => _paired;
            set
            {
                _paired = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(buttonConnectText)));
            }
        }
        bool _paired;

        public string buttonConnectText
        {
            get => paired ? "Disconnect" : "Connect";
        }
    }
}
