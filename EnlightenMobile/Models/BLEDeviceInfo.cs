using System;
namespace EnlightenMobile.Models
{
    // This class provides a place to store string BLE characteristics found on
    // the Device Info and Generic Access services.  We don't bother capturing
    // all fields (currently ignoring Appearance and Preferred Connection Parameters)
    public class BLEDeviceInfo
    {
        // These get populated in BluetoothView code-behind from the Device Info
        // service (not Primary Service).
        public string deviceName { get; set; }
        public string manufacturerName { get; set; }
        public string softwareRevision { get; set; }
        public string firmwareRevision { get; set; }
        public string hardwareRevision { get; set; }

        Logger logger = Logger.getInstance();

        public BLEDeviceInfo()
        {
        }

        public bool add(string name, string value)
        {
            switch(name)
            {
                case "Device Name": deviceName = value; break;
                case "Manufacturer Name String": manufacturerName = value; break;
                case "Software Revision String": softwareRevision = value; break;
                case "Firmware Revision String": firmwareRevision = value; break;
                case "Hardware Revision String": hardwareRevision = value; break;
                default:
                    // logger.error($"unrecognized BLE Device Info: {name} = {value}");
                    return false;
            }
            logger.debug($"BLEDeviceInfo: adding {name} = {value}");
            return true;
        }

        public void dump()
        {
            Logger logger = Logger.getInstance();
            logger.debug($"deviceName       = {deviceName}");
            logger.debug($"manufacturerName = {manufacturerName}");
            logger.debug($"softwareRevision = {softwareRevision}");
            logger.debug($"firmwareRevision = {firmwareRevision}");
            logger.debug($"hardwareRevision = {hardwareRevision}");
        }
    }
}
