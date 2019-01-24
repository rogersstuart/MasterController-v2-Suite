using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerInterface
{
    public class MCv2LogEntry
    {
        private ulong source_device_id;
        private DateTime timestamp;
        private ulong card_id;
        private int panel_id;
        private int result_code;

        public static readonly string[] result_strings = {"Denial", "Disarm", "Forced Rearm"};

        public MCv2LogEntry(ulong source_device_id, DateTime timestamp, ulong card_id, int panel_id, int result_code)
        {
            this.source_device_id = source_device_id;
            this.timestamp = timestamp;
            this.card_id = card_id;
            this.panel_id = panel_id;
            this.result_code = result_code;
        }

        public override string ToString()
        {
            return timestamp + " " + source_device_id + " " + panel_id + " " + card_id + " " + result_strings[result_code];
        }
    }
}
