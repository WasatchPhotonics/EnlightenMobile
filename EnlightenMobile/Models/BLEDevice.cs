using System;
using System.ComponentModel;
using System.Text;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using EnlightenMobile.Common;

namespace EnlightenMobile.Models
{
    public class BLEDevice : INotifyPropertyChanged
    {
        public IDevice device;
        
        Logger logger = Logger.getInstance();

        public event PropertyChangedEventHandler PropertyChanged;

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
                    uuid = Util.formatUUID(rec.Data);
            }
            logger.debug($"UUID: {uuid}");
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

        public bool selected
        {
            get => _selected;
            set
            {
                if (_selected != value)
                { 
                    _selected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(backgroundColor)));
                }
            }
        }
        bool _selected;
    }
}
