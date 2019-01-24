using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization;

namespace MCICommon
{
    [Serializable]
    [DataContract]
    public class TCPConnectionProperties
    {
        private string alias;
        private string hostname_or_ip;
        private int port;

        private int protocol_version = -1;

        public TCPConnectionProperties(string alias, string hostname_or_ip, int port)
        {
            this.alias = alias;
            this.hostname_or_ip = hostname_or_ip;
            this.port = port;
        }

        [DataMember]
        public string AddressString
        {
            get
            {
                return hostname_or_ip;
            }
            set { hostname_or_ip = value; }
        }

        [DataMember]
        public int Port
        {
            get
            {
                return port;
            }
            set { port = value; }
        }

        public async Task<IPEndPoint> GetIPEndPointAsync()
        {
            return new IPEndPoint((await Dns.GetHostAddressesAsync(hostname_or_ip))[0], port);
        }

        [DataMember]
        public string Alias
        {
            get
            {
                return alias;
            }

            set { alias = value; }
        }

        public Task<int> ProtocolVersion
        {
            get
            {
                return Task.Run<int>(async () =>
                {
                    if(protocol_version == -1)
                        protocol_version = await ProtocolDetector.DetectProtocolVersion(hostname_or_ip, port);

                    return protocol_version;
                });
            }
        }

        public override string ToString()
        {
            if (alias != null && alias.Trim() != "")
                return alias;
            else
                return hostname_or_ip + ":" + port;
        }

        public bool Equals(TCPConnectionProperties tcpconnprop)
        {
            if (AddressString == tcpconnprop.AddressString)
                if (Alias == tcpconnprop.Alias)
                    if (Port == tcpconnprop.Port)
                        return true;

            return false;
        }

        
    }
}
