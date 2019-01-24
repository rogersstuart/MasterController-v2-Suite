using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public class ControllerCommand
    {
        public const byte READ_LOG_COMMAND = (byte)'R';
        public const byte READ_LOG_PART = (byte)'B';
        public const byte ERASE_LOG = (byte)'Q';
        public const byte WRITE_LIST_COMMAND = (byte)'L';
        public const byte WRITE_LIST_PART = (byte)'S';
        public const byte READ_LIST_PART = (byte)'K';
        public const byte WRITE_RTC_COMMAND = (byte)'C';
        public const byte READ_TIME_COMMAND = (byte)'T';
        public const byte NULL_LIST_SEGMENT = (byte)'N';
        public const byte GET_DOOR_CONTROL_OUTPUTS = (byte)'Z';
        public const byte SET_DOOR_CONTROL_OUTPUTS = (byte)'A';

        private byte[] tx_data;
        private byte[] rx_data;

        private bool response_expected = false;
        private int response_length = 0;

        public ControllerCommand() { }

        public ControllerCommand(byte command_to_transmit)
            : this(command_to_transmit, new byte[0])
        {
            //this.ExpanderCommand(command_to_transmit, new byte[0]);
        }

        public ControllerCommand(byte command_to_transmit, byte[] data)
        {
            tx_data = new byte[data.Length + 3]; //length + command + data + crc

            tx_data[0] = Convert.ToByte(1 + data.Length);
            tx_data[1] = command_to_transmit;

            if (data.Length > 0)
                Array.Copy(data, 0, tx_data, 2, data.Length);

            tx_data[tx_data.Length - 1] = Utilities.CRC8(tx_data, tx_data.Length - 1);

            response_expected = true;

            /*
            switch (command_to_transmit)
            {
                case READ_EXPANDERS_COMMAND: response_length = 4; break;
                case READ_POWER_MONITOR_COMMAND: response_length = 8; break;
                case READ_TEMPERATURES_COMMAND: response_length = 12; break;
                case READ_FAN_SPEED_COMMAND: response_length = 2; break;
                case READ_UPTIME_COMMAND: response_length = 8; break;
                default: response_expected = false; break;
            }
            */
            if (response_expected)
                response_length += 2;
        }

        [DataMember]
        public byte[] RxPacket
        {
            get
            {
                return rx_data;
            }

            set
            {
                rx_data = value;
            }
        }

        [DataMember]
        public byte[] TxPacket
        {
            get
            {
                return tx_data;
            }

            set
            {
                tx_data = value;
            }
        }

        [DataMember]
        public bool ResponseExpected
        {
            get
            {
                return response_expected;
            }

            set
            {
                response_expected = value;
            }
        }

        [DataMember]
        public int ResponseLength
        {
            get
            {
                return response_length;
            }

            set
            {
                response_length = value;
            }
        }
    }
}
