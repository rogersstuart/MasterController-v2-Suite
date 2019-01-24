using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace MCICommon
{
    [DataContract]
    public class ExpanderCommand
    {
        public const byte WRITE_EXPANDERS_COMMAND = 2;
        public const byte READ_EXPANDERS_COMMAND = 3;
        public const byte READ_POWER_MONITOR_COMMAND = 4;
        public const byte READ_TEMPERATURES_COMMAND = 5;
        public const byte READ_FAN_SPEED_COMMAND = 6;
        public const byte READ_UPTIME_COMMAND = 7;
        public const byte RESET_COMMAND = 8;

        private byte[] tx_packet;
        private byte[] rx_packet;

        private bool response_expected = false;
        private int response_length = 0;

        public ExpanderCommand() { }

        public ExpanderCommand(byte command_to_transmit)
            : this(command_to_transmit, new byte[0])
        {
            //this.ExpanderCommand(command_to_transmit, new byte[0]);
        }

        public ExpanderCommand(byte command_to_transmit, byte[] data)
        {
            tx_packet = new byte[data.Length + 3]; //length + command + data + crc

            tx_packet[0] = Convert.ToByte(1 + data.Length);
            tx_packet[1] = command_to_transmit;

            if (data.Length > 0)
                Array.Copy(data, 0, tx_packet, 2, data.Length);

            tx_packet[tx_packet.Length - 1] = Utilities.CRC8(tx_packet, tx_packet.Length - 1);

            response_expected = true;

            switch (command_to_transmit)
            {
                case READ_EXPANDERS_COMMAND: response_length = 4; break;
                case READ_POWER_MONITOR_COMMAND: response_length = 8; break;
                case READ_TEMPERATURES_COMMAND: response_length = 12; break;
                case READ_FAN_SPEED_COMMAND: response_length = 2; break;
                case READ_UPTIME_COMMAND: response_length = 8; break;
                default: response_expected = false; break;
            }

            if (response_expected)
                response_length += 2;
        }

        [DataMember]
        public byte[] RxPacket
        {
            get
            {
                return rx_packet;
            }

            set
            {
                rx_packet = value;
            }
        }

        [DataMember]
        public byte[] TxPacket
        {
            get
            {
                return tx_packet;
            }

            set
            {
                tx_packet = value;
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
