using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerInterface
{
    public class PeerSyncStatus
    {
        private List<PeerInfo> all_peers = new List<PeerInfo>();
        private TimeSpan peer_timeout = TimeSpan.FromMinutes(10);

        private Object peer_info_lock = new Object();

        public PeerSyncStatus()
        {

        }

        public PeerSyncStatus(PeerSyncStatus source)
        {
            all_peers = new List<PeerInfo>(source.GetPeers());
        }

        public PeerInfo[] GetPeers()
        {
            lock (peer_info_lock)
                return all_peers.ToArray();
        }

        public void AddPeer(PeerInfo info)
        {
            lock(peer_info_lock)
            {
                List<int> peer_indicies = new List<int>();

                foreach (PeerInfo p in all_peers)
                    if (p.Equals(info))
                        peer_indicies.Add(all_peers.IndexOf(p));

                peer_indicies.Reverse();

                foreach (int i in peer_indicies)
                    all_peers.RemoveAt(i);

                all_peers.Add(info);
            }
        }

        public void Refresh()
        {
            
            List<int> cull_indicies = new List<int>();

            //check to make sure that peers in the list are still active
            lock (peer_info_lock)
            {
                foreach (PeerInfo p in all_peers)
                    if (DateTime.Now - p.PeerDiscoveryTime > peer_timeout)
                        cull_indicies.Add(all_peers.IndexOf(p));

                cull_indicies.Reverse();

                foreach (int i in cull_indicies)
                    all_peers.RemoveAt(i);
            }
            
        }

        public int PeerCount
        {
            get
            {
                lock (peer_info_lock)
                    return all_peers.Count();
            }
        }
    }
}
