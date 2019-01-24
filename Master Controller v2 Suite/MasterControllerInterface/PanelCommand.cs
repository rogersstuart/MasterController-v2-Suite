using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCICommon;

namespace MasterControllerInterface
{
    class PanelCommand
    {
        public const byte POLL_CARD_PRESENCE = 2;
        public const byte CLEAR_CARD = 3;
        public const byte DISPLAY_ANIMATION = 4;
        public const byte GET_UPTIME = 5;
        public const byte RESTART = 6;

        public const byte ANIMATION_DECLINE = 0 ;
        public const byte ANIMATION_ALT_APPROVAL = 1;
        public const byte ANIMATION_APPROVAL = 2;
        public const byte ANIMATION_BLANK = 3;

        private byte[] tx_packet;
        private byte[] rx_packet;

        private bool response_expected;
        private int response_length = 0;

        public PanelCommand(byte command) : this(command, new byte[0]){}

        public PanelCommand(byte command, byte animation_id) : this(command, ((Func<byte[]>)(() => {byte[] b = {animation_id}; return b;}))()) {}

        public PanelCommand(byte command, byte[] data)
        {
            tx_packet = new byte[data.Length + 3]; //length + command + data + crc

            tx_packet[0] = Convert.ToByte(1 + data.Length);
            tx_packet[1] = command;

            if (data.Length > 0)
                Array.Copy(data, 0, tx_packet, 2, data.Length);

            tx_packet[tx_packet.Length - 1] = Utilities.CRC8(tx_packet, tx_packet.Length - 1);

            response_expected = true;

            switch (command)
            {
                case POLL_CARD_PRESENCE: response_length = 7; break;
                case GET_UPTIME: response_length = 8; break;
                default: response_expected = false; break;
            }

            if (response_expected)
                response_length += 2;
        }
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

        public byte[] TxPacket
        {
            get
            {
                return tx_packet;
            }
        }

        public bool ResponseExpected
        {
            get
            {
                return response_expected;
            }
        }

        public int ResponseLength
        {
            get
            {
                return response_length;
            }
        }
    }
}
