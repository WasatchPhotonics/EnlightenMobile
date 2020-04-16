using System;
namespace EnlightenMobile.Models
{
    public enum LaserMode { MANUAL=0, RAMAN=1, MAX_LASER_MODES=2 };
    public enum LaserType { NORMAL=0, MAX_LASER_TYPES=1 } // add others if/when implemented in FW

    public class LaserState
    {
        public LaserType type = LaserType.NORMAL;
        public LaserMode mode = LaserMode.MANUAL;
        public bool enabled;
        public byte watchdogSec;

        Logger logger = Logger.getInstance();

        public LaserState(byte[] data = null)
        {
            reset();
            if (data != null)
            {
                if (!parse(data))
                { 
                    logger.error("LaserState instantiated with invalid data; reverting to default values");
                    reset();
                }
            }
        }

        void reset()
        {
            type = LaserType.NORMAL;
            mode = LaserMode.MANUAL;
            enabled = false;
            watchdogSec = 10;
        }

        public byte[] serialize()
        {
            byte[] data = new byte[4];

            data[0] = (byte)type;
            data[1] = (byte)mode;
            data[2] = (byte)(enabled ? 1 : 0);
            data[3] = watchdogSec;

            return data;
        }

        public bool parse(byte[] data)
        {
            if (data.Length != 4)
            {
                logger.error($"rejecting LaserState with invalid payload length {data.Length}");
                return false;
            }

            ////////////////////////////////////////////////////////////////////
            // Laser Type
            ////////////////////////////////////////////////////////////////////

            LaserType newType = LaserType.NORMAL;
            byte value = data[0];
            if (value < (byte)LaserType.MAX_LASER_TYPES)
            {
                newType = (LaserType)value;
            }
            else
            {
                logger.error($"rejecting LaserState with invalid LaserType {value}");
                return false;
            }

            ////////////////////////////////////////////////////////////////////
            // Laser Mode
            ////////////////////////////////////////////////////////////////////

            LaserMode newMode = LaserMode.MANUAL;
            value = data[1];
            if (value < (byte)LaserMode.MAX_LASER_MODES)
            {
                newMode = (LaserMode)value;
            }
            else
            {
                logger.error($"rejecting LaserState with invalid LaserMode 0x{value:x2}");
                return false;
            }

            ////////////////////////////////////////////////////////////////////
            // Laser Enabled
            ////////////////////////////////////////////////////////////////////

            bool newEnabled = false;
            value = data[2];
            if (value < 0x02)
            {
                newEnabled = value == 0x01;
            }
            else
            {
                logger.error($"rejecting LaserState with invalid LaserEnabled 0x{value:x2}");
                return false;
            }

            ////////////////////////////////////////////////////////////////////
            // Laser Watchdog
            ////////////////////////////////////////////////////////////////////

            byte newWatchdog = 0;
            value = data[3];
            if (value < 0xff)
            {
                newWatchdog = value;
            }
            else
            {
                logger.error($"rejecting LaserState with invalid LaserWatchdog 0x{value:x2}");
                return false;
            }

            ////////////////////////////////////////////////////////////////////
            // all fields validated, accept new values
            ////////////////////////////////////////////////////////////////////

            type = newType;
            mode = newMode;
            enabled = newEnabled;
            watchdogSec = newWatchdog;

            return true;
        }
    }
}
