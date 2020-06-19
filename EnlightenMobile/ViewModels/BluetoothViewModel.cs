using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using EnlightenMobile.Models;

namespace EnlightenMobile.ViewModels
{
    // Provides the backing logic and bound properties shown on the BluetoothView.
    // Arguably, much of BluetoothView.cs should be moved here. Jury's still out
    // on that one.
    public class BluetoothViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<BLEDevice> bleDeviceList = new ObservableCollection<BLEDevice>();

        Logger logger = Logger.getInstance();

        public BluetoothViewModel()
        {
            resetCmd = new Command(() => { _ = doResetAsync(); });
        }

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

        public bool bluetoothEnabled 
        {
            get 
            {
                // this lags :-(
                // var enabled = Util.bluetoothEnabled();
                return _bluetoothEnabled;
            }
        }
        bool _bluetoothEnabled = Util.bluetoothEnabled();

        public string buttonConnectText
        {
            get => paired ? "Disconnect" : "Connect";
        }

        public Command resetCmd { get; }

        // ideally, we should probably add some kind of callback hook to an
        // Android "onBluetoothEnabled" event, inside the PlatformService, and
        // float that update back here somehow, but...this will work for now
        public async Task<bool> doResetAsync()
        {
            logger.debug("attempting to disable Bluetooth");

            bleDeviceList.Clear();
            
            if (!Util.enableBluetooth(false))
                logger.error("Unable to disable Bluetooth");

            _bluetoothEnabled = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(bluetoothEnabled)));

            logger.debug("sleeping during Bluetooth restart");
            await Task.Delay(1000);

            logger.debug("attempting to re-enable Bluetooth");
            var ok = Util.enableBluetooth(true);

            logger.debug("sleeping AFTER Bluetooth restart");
            await Task.Delay(2000);

            _bluetoothEnabled = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(bluetoothEnabled)));

            if (!ok)
            {
                logger.error("Unable to re-enable Bluetooth");
                return false;
            }

            logger.info("Bluetooth reset");
            return true;
        }
    }
}
