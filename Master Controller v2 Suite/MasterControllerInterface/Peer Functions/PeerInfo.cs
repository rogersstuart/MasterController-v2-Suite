using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCICommon;

namespace MasterControllerInterface
{
    //[Serializable]
    public class PeerInfo
    {
        private byte[] instance_id;
        private DateTime discovery_time;
        private ConnectionProperties peer_conn_prop;
        private bool is_master = false;

        public PeerInfo(byte[] instance_id, ConnectionProperties peer_conn_prop, DateTime discovery_time)
        {
            this.instance_id = instance_id;
            this.discovery_time = discovery_time;
            this.peer_conn_prop = peer_conn_prop;
        }

        public DateTime PeerDiscoveryTime
        {
            get
            {
                return discovery_time;
            }
        }

        public ConnectionProperties PeerConnectionProperties
        {
            get
            {
                return peer_conn_prop;
            }
        }

        public bool Equals(PeerInfo p)
        {
            //if (p.PeerDiscoveryTime == PeerDiscoveryTime)
                if (p.PeerConnectionProperties.Equals(PeerConnectionProperties))
                    return true;

            return false;
        }
    }
}
