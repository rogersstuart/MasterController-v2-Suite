using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace MCICommon
{
    [DataContract]
    public class HostInfo
    {
        private string host_guid;
        private TCPConnectionProperties tcpconnprop;

        public HostInfo() { }

        [DataMember]
        public string HostGUID
        {
            get { return host_guid; }
            set { host_guid = value; }
        }

        [DataMember]
        public TCPConnectionProperties TCPConnectionProperties
        {
            get { return tcpconnprop; }
            set { tcpconnprop = value; }
        }
    }
}
