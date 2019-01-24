using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MCICommon
{
    [Serializable]
    public class ExpanderState
    {
        private bool[] expander_0_state, expander_1_state;
        private float bus_power, bus_voltage, fan_temperature, board_0_temperature, board_1_temperature;
        private UInt16 fan_speed;
        private UInt64 uptime;

        private DateTime timestamp;

        public ExpanderState()
        {
            expander_0_state = new bool[16];
            expander_1_state = new bool[16];

            bus_power = 0; bus_voltage = 0; fan_temperature = 0; board_0_temperature = 0; board_1_temperature = 0;
            fan_speed = 0;
            uptime = 0;

            timestamp = DateTime.Now;
        }

        public ExpanderState(ExpanderState expst) //clone
        {
                expander_0_state = (bool[])expst.Expander0State.Clone();
                expander_1_state = (bool[])expst.Expander1State.Clone();

                bus_power = expst.BusPower;
                bus_voltage = expst.BusVoltage;
                fan_temperature = expst.FanTemperature;
                board_0_temperature = expst.Board0Temperature;
                board_1_temperature = expst.Board1Temperature;
                fan_speed = expst.FanSpeed;
                uptime = expst.Uptime;

                timestamp = expst.Timestamp;
        }

        public bool[] Expander0State
        {
            get
            {
                return expander_0_state;
            }
            set
            {
                expander_0_state = value;
            }
        }

        public DateTime Timestamp
        {
            get { return timestamp; }
        }

        public bool[] Expander1State
        {
            get
            {
                return expander_1_state;
            }
            set
            {
                expander_1_state = value;
            }
        }

        public float BusPower
        {
            get
            {
                return bus_power;
            }
            set
            {
                bus_power = value;
            }
        }

        public float BusVoltage
        {
            get
            {
                return bus_voltage;
            }
            set
            {
                bus_voltage = value;
            }
        }

        public float FanTemperature
        {
            get
            {
                return fan_temperature;
            }
            set
            {
                fan_temperature = value;
            }
        }

        public float Board0Temperature
        {
            get
            {
                return board_0_temperature;
            }
            set
            {
                board_0_temperature = value;
            }
        }

        public float Board1Temperature
        {
            get
            {
                return board_1_temperature;
            }
            set
            {
                board_1_temperature = value;
            }
        }

        public UInt16 FanSpeed
        {
            get
            {
                return fan_speed;
            }
            set
            {
                fan_speed = value;
            }
        }

        public UInt64 Uptime
        {
            get
            {
                return uptime;
            }
            set
            {
                uptime = value;
            }
        }
    }
}
