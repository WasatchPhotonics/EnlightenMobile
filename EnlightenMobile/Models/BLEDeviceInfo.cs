using System;
namespace EnlightenMobile.Models
{
    public class BLEDeviceInfo
    {
        // These get populated in BluetoothView code-behind from the Device Info
        // service (not Primary Service).
        public string manufacturerName { get; set; }
        public string softwareRevision { get; set; }
        public string firmwareRevision { get; set; }
        public string hardwareRevision { get; set; }

        public BLEDeviceInfo()
        {
        }

        public void dump()
        {
            Logger logger = Logger.getInstance();
            logger.debug($"manufacturerName = {manufacturerName}");
            logger.debug($"softwareRevision = {softwareRevision}");
            logger.debug($"firmwareRevision = {firmwareRevision}");
            logger.debug($"hardwareRevision = {hardwareRevision}");
        }
    }
}
