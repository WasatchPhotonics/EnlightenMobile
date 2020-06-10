using EnlightenMobile.Models;
using EnlightenMobile.ViewModels;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using Xamarin.Forms.Xaml;
using Xamarin.Forms;

namespace EnlightenMobile.Views
{
    // Arguably, most of this should go into BluetoothViewModel, if we changed 
    // the Button.OnClick events to Button.Command bindings. It's not GUI-related
    // and spends a lot of time talking to the Spectrometer Model.
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BluetoothView : ContentPage
    {
        IBluetoothLE ble;
        IAdapter adapter;

        ObservableCollection<BLEDevice> bleDeviceList;
        BLEDevice bleDevice;

        IService service;

        Dictionary<string, Guid> guidByName = new Dictionary<string, Guid>();
        Dictionary<Guid, string> nameByGuid = new Dictionary<Guid, string>();
        Dictionary<string, ICharacteristic> characteristicsByName = new Dictionary<string, ICharacteristic>();

        Guid primaryServiceId;


        Spectrometer spec = Spectrometer.getInstance();
        BluetoothViewModel bvm;

        Logger logger = Logger.getInstance();

        ////////////////////////////////////////////////////////////////////////
        // Lifecycle
        ////////////////////////////////////////////////////////////////////////

        public BluetoothView()
        {
            InitializeComponent();

            bvm = (BluetoothViewModel)BindingContext;

            logger.debug("BluetoothView: initializing BLE stuff");

            // this crashes on iOS if you don't follow add plist entries per
            // https://stackoverflow.com/a/59998233/11615696
            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            bleDeviceList = new ObservableCollection<BLEDevice>();

            listView.ItemsSource = bleDeviceList;

            adapter.DeviceDiscovered += _bleAdapterDeviceDiscovered;

            primaryServiceId = _makeGuid("ff00");

            // characteristics
            logger.debug("BluetoothView: initializing characteristic GUIDs");

            // ENG-0120
            guidByName["integrationTimeMS"] = _makeGuid("ff01");
            guidByName["gainDb"]            = _makeGuid("ff02"); 
            guidByName["laserState"]        = _makeGuid("ff03"); 
            guidByName["acquireSpectrum"]   = _makeGuid("ff04"); 
            guidByName["spectrumRequest"]   = _makeGuid("ff05"); 
            guidByName["readSpectrum"]      = _makeGuid("ff06");
            guidByName["eepromCmd"]         = _makeGuid("ff07");
            guidByName["eepromData"]        = _makeGuid("ff08");
            guidByName["batteryStatus"]     = _makeGuid("ff09");
            guidByName["roi"]               = _makeGuid("ff0a");

            foreach (var pair in guidByName)
                nameByGuid[pair.Value] = pair.Key;

            btnConnect.IsEnabled = false;
        }

        ////////////////////////////////////////////////////////////////////////
        // BLE Connection Sequence
        ////////////////////////////////////////////////////////////////////////

        // Step 1: user clicked "Scan"
        private async void btnScan_Clicked(object sender, EventArgs e)
        {
            try
            {
                // resolve Location permission
                var success = await _requestPermissionsAsync();
                if (!success)
                {
                    logger.error("can't obtain Location permission");
                    _ = DisplayAlert("Error", "Can't obtain Location permission", "OK");
                    return;
                }

                bleDeviceList.Clear();
                if (!ble.Adapter.IsScanning)
                {
                    logger.debug("starting scan");
                    await adapter.StartScanningForDevicesAsync();

                    // Step 2: As each device is added to the list, the 
                    // adapter.DeviceDiscovered event will call 
                    // _bleAdapterDeviceDiscovered and add it to the listView.
                }
            }
            catch (Exception ex)
            {
                _ = DisplayAlert("Exception", ex.Message.ToString(), "OK");
                logger.error("caught exception during scan button event: {0}", ex.Message);
            }
            logger.debug("scan complete");
        }

        // Step 3: the user has explicitly selected a device from the listView
        private void listView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (listView.SelectedItem == null)
            {
                bleDevice = null;
                service = null;
                btnConnect.IsEnabled = false;
                return;
            }

            bleDevice = listView.SelectedItem as BLEDevice;
            logger.debug($"selected device {bleDevice.name}");

            // update color, because ListView doesn't have a cross-platform
            // SelectedBackgroundColor property :-(
            foreach (var dev in bleDeviceList)
                dev.selected = dev.device.Id == bleDevice.device.Id;

            btnConnect.IsEnabled = true;
        }

        void initProgress() => Util.updateProgressBar(progressBarConnect, 0);
        void showProgress(double progress) => Util.updateProgressBar(progressBarConnect, progress);

        // Step 4: the user clicked "Connect"
        async void btnConnect_Clicked(object sender, EventArgs e)
        {
            if (bvm.paired)
            {
                _ = await doDisconnectAsync();
                bvm.paired = false;
            }
            else
            { 
                bvm.paired = await doConnectAsync();
                btnConnect.IsEnabled = true;
                if (bvm.paired)
                    PageNav.getInstance().select("Scope");
            }
            initProgress();
        }

        async Task<bool> doDisconnectAsync()
        {
            logger.debug("attempting to disconnect");

            if (bleDevice is null)
            {
                logger.error("attempt to disconnect without bleDevice");
                return false;
            }

            await adapter.DisconnectDeviceAsync(bleDevice.device);
            bvm.paired = false;
            spec.reset();
            return true;
        }

        async Task<bool> doConnectAsync()
        {
            logger.debug("attempting to connect");

            initProgress();
            if (bleDevice is null)
            {
                logger.error("must select a device before connecting");
                return false;
            }

            // recommended to help avoid GattCallback error 133
            if (ble.Adapter.IsScanning)
            {
                logger.debug("stopping scan");
                await adapter.StopScanningForDevicesAsync();
            }

            logger.debug($"attempting connection to {bleDevice.name}");
            btnConnect.IsEnabled = false;
            var success = false;
            try
            {
                // Step 5: actually try to connect
                await adapter.ConnectToDeviceAsync(bleDevice.device);

                // Step 5a: verify connection
                foreach (var d in adapter.ConnectedDevices)
                {
                    if (d == bleDevice.device)
                    {
                        success = true;
                        break;
                    }
                }
            }
            catch (DeviceConnectionException ex)
            {
                logger.error("exception connecting to device ({0})", ex.Message);
                _ = DisplayAlert("Notice", ex.Message.ToString(), "OK");
                return false;
            }

            if (!success)
                return logger.error($"failed connection to {bleDevice.name}");

            logger.info($"successfully connected to {bleDevice.name}");
            showProgress(.05);

            // Step 6: connect to primary service
            logger.debug($"connecting to primary service {primaryServiceId}");
            service = await bleDevice.device.GetServiceAsync(primaryServiceId);
            if (service is null)
                return logger.error($"did not find primary service {primaryServiceId}");

            logger.debug($"found primary service {service}");

            // Step 7: read characteristics
            logger.debug("reading characteristics of service {0} ({1})", service.Name, service.Id);
            characteristicsByName = new Dictionary<string, ICharacteristic>();
            var list = await service.GetCharacteristicsAsync();
            foreach (var c in list)
            {
                // match it with an "expected" UUID
                string name = null;
                foreach (var pair in guidByName)
                { 
                    if (pair.Value == new Guid(c.Uuid))
                    {
                        name = pair.Key;
                        break;
                    }
                }

                if (name is null)
                {
                    logger.error($"ignoring unrecognized characteristic {c.Uuid}");
                    continue;
                }

                // store it by friendly name
                characteristicsByName.Add(name, c);
            }

            logger.debug("Registered characteristics:");
            foreach (var pair in characteristicsByName)
            {
                var name = pair.Key;
                var c = pair.Value;

                logger.debug($"  {c.Uuid} {name}");

                if (c.CanUpdate)
                    logger.debug("    (supports notifications)");

                // Step 7a: read characteristic descriptors
                // logger.debug($"    WriteType = {c.WriteType}");
                // var descriptors = await c.GetDescriptorsAsync();
                // foreach (var d in descriptors)
                //     logger.debug($"    descriptor {d.Name} = {d.Value}");
            }
            showProgress(.15);

            logger.debug("polling device for other services");
            BLEDeviceInfo bdi = new BLEDeviceInfo();
            var allServices = await bleDevice.device.GetServicesAsync();
            foreach (var thisService in allServices)
            {
                logger.debug($"examining service {thisService.Name} (ID {thisService.Id})");
                if (thisService.Id == primaryServiceId)
                {
                    logger.debug("skipping primary service");
                    continue;
                }

                var characteristics = await thisService.GetCharacteristicsAsync();
                foreach (var c in characteristics)
                {
                    logger.debug($"reading {c.Name}");
                    var data = await c.ReadAsync();
                    if (data is null)
                    {
                        logger.debug($"can't read {c.Uuid} ({c.Name})");
                    }
                    else
                    {
                        logger.hexdump(data, prefix: $"  {c.Uuid}: {c.Name} = ");
                        bdi.add(c.Name, Util.toASCII(data));
                    }
                }
            }

            // populate Spectrometer
            logger.debug("initializing spectrometer");
            spec.bleDeviceInfo = bdi;
            _ = await spec.initAsync(characteristicsByName, showProgress);

            // start notifications
            foreach (var pair in characteristicsByName)
            {
                var name = pair.Key;
                var c = pair.Value;

                if (c.CanUpdate && (name == "batteryStatus" || name == "laserState"))
                {
                    c.ValueUpdated -= _characteristicUpdated;
                    c.ValueUpdated += _characteristicUpdated;
                    _ = c.StartUpdatesAsync();
                }
            }

            ////////////////////////////////////////////////////////////////////
            // all done
            ////////////////////////////////////////////////////////////////////

            logger.debug("btnConnect_clicked done");
            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        // Utility methods
        ////////////////////////////////////////////////////////////////////////

        // @todo test
        private void _characteristicUpdated(object sender, CharacteristicUpdatedEventArgs characteristicUpdatedEventArgs)
        {
            var c = characteristicUpdatedEventArgs.Characteristic;

            // faster way to do this using nameByGuid?
            string name = null;
            foreach (var pair in characteristicsByName)
                if (pair.Value.Uuid == c.Uuid)
                    name = pair.Key;

            if (name is null)
            {
                logger.error($"Received update from unknown characteristic ({c.Uuid})"); 
                return;
            }

            if (name == "batteryStatus")
            {
                spec.processBatteryNotification(c.Value);
            }
            else if (name == "laserState")
            {
                spec.processLaserStateNotification(c.Value);
            }
        }

        Guid _makeGuid(string id)
        {
            // All Wasatch Photonics SiG Characteristic UUIDs follow this format
            const string prefix = "D1A7";
            const string suffix = "-AF78-4449-A34F-4DA1AFAF51BC";
            return new Guid(string.Format($"{prefix}{id}{suffix}"));
        }

        // Step 2a: during a Scan, we've discovered a new BLE device, so add it 
        // to the listView (by adding it to the deviceList serving as the listView's
        // Model).
        void _bleAdapterDeviceDiscovered(object sender, DeviceEventArgs e)
        {
            var device = e.Device; // an IDevice
            
            // ignore anything without a name
            if (device.Name is null || device.Name.Length == 0)
                return;

            // ignore anything that doesn't have "WP" or "SiG" in the name
            var nameLC = device.Name.ToLower();
            if (!nameLC.Contains("wp") && !nameLC.Contains("sig"))
                return;
       
            BLEDevice bd = new BLEDevice(device);
            logger.debug($"discovered {bd.name} (RSSI {bd.rssi} Id {device.Id})");
            bleDeviceList.Add(bd);
        }

        async Task<bool> _requestPermissionsAsync()
		{ 
            // Recent versions of Android require either coarse- or fine-grained 
            // Location access in order to perform a BLE scan.
            if (!await _requestPermissionAsync<LocationPermission>(Permission.Location, "Location", 
                "Bluetooth requires access to the 'Location' service in order to function."))
                return false;

            if (!await _requestPermissionAsync<StoragePermission>(Permission.Storage, "Storage", 
                "App needs to be able to save spectra to local filesystem."))
                return false;

            return true;
        }

        // @todo figure out why generic CheckPermissionStatusAsync doesn't compile
        async Task<bool> _requestPermissionAsync<T>(Permission perm, string name, string reason) 
            where T : BasePermission
        {
			try
			{
                // do we already have permission?

                // WHY DOESN'T THIS WORK?!?
                // I almost feel like I'm working from a cached version of the Plugin.Permissions package...
                // like if I fully flushed and reinstalled that package, this would be okay.
		  	  //var status = await CrossPermissions.Current.CheckPermissionStatusAsync<T>(); 
	            var status = await CrossPermissions.Current.CheckPermissionStatusAsync(perm);

                // if not, prompt the user to authorize it
				if (status != PermissionStatus.Granted)
				{
					if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(perm))
					{
						await DisplayAlert($"{name} Permission", reason, "OK");
					}

					var result = await CrossPermissions.Current.RequestPermissionsAsync(perm);
					status = result[perm];
				}

                // do we have it now?
				if (status == PermissionStatus.Granted)
				{
					logger.debug($"{name} permission granted");
                    return true;
				}
				else if (status != PermissionStatus.Unknown)
				{
					return logger.error($"{name} permission denied");
				}
			}
			catch (Exception ex)
			{
                return logger.error($"Exception obtaining {name} permission: {ex}");
			}
            return false;
		}
    }
}