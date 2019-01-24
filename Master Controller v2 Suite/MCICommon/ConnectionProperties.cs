using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    [Serializable]
    public class ConnectionProperties
    {
        private string ip;
        private int port;

        public ConnectionProperties(string ip, int port)
        {
            this.ip = string.Copy(ip);
            this.port = port;
        }

        public override string ToString()
        {
            return ip + " " + port;
        }

        public string IPAddress
        {
            get
            {
                return string.Copy(ip);
            }
        }

        public int TCPPort
        {
            get
            {
                return port;
            }
        }

        public bool Equals(ConnectionProperties connprop)
        {
            if (connprop.IPAddress == IPAddress)
                if (connprop.TCPPort == TCPPort)
                    return true;

            return false;
        }
    }
}
