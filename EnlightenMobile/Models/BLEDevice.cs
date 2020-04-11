using System;
using System.Text;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;

namespace EnlightenMobile.Models
{
    public class BLEDevice
    {
        public IDevice device;
        public bool selected;
        Logger logger = Logger.getInstance();
        
        public BLEDevice(IDevice device)
        {
            this.device = device;
            uuid = "unknown-uuid";

            logger.debug("BLEDevice advertisements:");
            foreach (var rec in device.AdvertisementRecords)
            {

                logger.debug($"{rec.Type}:");
                logger.hexdump(rec.Data, prefix: "  ");

                if (rec.Type == AdvertisementRecordType.UuidsComplete128Bit)
                    uuid = formatUUID(rec.Data);
            }
            logger.debug($"UUID: {uuid}");
        }

        // Format a 16-byte array like a standard UUID
        //
        // 00000000-0000-1000-8000-00805F9B34FB
        //  0 1 2 3  4 5  6 7  8 9  a b c d e f
        //
        // You'd think something like this would already be in Plugin.BLE, and
        // probably it is... *shrug*
        string formatUUID(byte[] data)
        {
            if (data.Length != 16)
                return "invalid-uuid";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                sb.Append(string.Format("{0:x2}", data[i]));
                if (i == 3 || i == 5 || i == 7 || i == 9)
                    sb.Append("-");
            }
            return sb.ToString();
        }

        public string uuid { get; private set; }
      
        public double rssi
        {
            get => device.Rssi;
        }

        public string name
        {
            get => device.Name;
        }

        public string id
        {
            get => device.Id.ToString();
        }

        public string backgroundColor
        {
            get => selected ? "#555" : "#444";
        }
    }
}
