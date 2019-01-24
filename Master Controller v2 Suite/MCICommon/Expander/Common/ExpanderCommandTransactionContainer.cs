using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    [DataContract]
    public class CommandTransactionContainer
    {
        private ulong device_id;
        private ExpanderCommand[] expcmds;

        public CommandTransactionContainer()
        {
        }

        public CommandTransactionContainer(ulong device_id, ExpanderCommand[] expcmds)
        {
            this.device_id = device_id;
            this.expcmds = expcmds;
        }

        [DataMember]
        public ulong DeviceID
        {
            get { return device_id; }
            set { device_id = value; }
        }

        [DataMember]
        public ExpanderCommand[] ExpanderCommands
        {
            get { return expcmds; }
            set { expcmds = value; }
        }
    }
}
