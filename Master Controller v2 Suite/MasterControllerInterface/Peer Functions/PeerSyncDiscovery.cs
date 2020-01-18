using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Threading;
using MCICommon;

namespace MasterControllerInterface
{
    public static class PeerSyncDiscovery
    {
        private static Thread discovery_thread = null;
        private static bool discovery_running = false;

        private static int sync_broadcast_port = 10245;
        private static int listening_duration = 4000;

        private static PeerSyncStatus sync_status = new PeerSyncStatus();

        private static Object sync_status_lock = new Object();

        public static PeerInfo[] GetPeers()
        {
            lock(sync_status_lock)
            return sync_status.GetPeers();
        }

        public static int PeerCount()
        {
            lock(sync_status_lock)
            return sync_status.PeerCount;
        }

        public static void Start()
        {
            if (discovery_thread != null)
                Stop();

            if (discovery_thread == null)
            {
                discovery_thread = GenerateDiscoveryThread();
                discovery_running = true;
                discovery_thread.Start();
            }
        }

        public static void Stop()
        {
           
                discovery_running = false;

             if (discovery_thread != null)
                while (discovery_thread.IsAlive) ;

                discovery_thread = null;
        }

        public static bool IsRunning
        {
            get { return discovery_running; }
        }

        private static byte[] GetPeerInstanceID(Stream s)
        {
            s.WriteByte((byte)'0');

            //wait for 16 byte instance id

            byte[] remote_inst_id = new byte[16];

            s.Read(remote_inst_id, 0, remote_inst_id.Length);

            return remote_inst_id;
        }

        private static Thread GenerateDiscoveryThread()
        {
            return new Thread(delegate ()
            {
                BinaryFormatter bf = new BinaryFormatter();

                while (discovery_running)
                {
                    try
                    {
                        using (UdpClient udp_client = new UdpClient())
                        {
                            udp_client.Client.ReceiveTimeout = 2000;
                            udp_client.EnableBroadcast = true;

                            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, sync_broadcast_port);

                            udp_client.Client.Bind(endpoint);

                            while (discovery_running)
                            {
                                List<string> found_peers = new List<string>();

                                Stopwatch sw = Stopwatch.StartNew();

                                while (sw.ElapsedMilliseconds < listening_duration)
                                {
                                    try
                                    {
                                        //we should receive 6 bytes here
                                        byte[] rxdat = udp_client.Receive(ref endpoint);

                                        DateTime peer_discovery_time = DateTime.Now;

                                        //if we're here then we got some data
                                        string ip_address = rxdat[0] + "." + rxdat[1] + "." + rxdat[2] + "." + rxdat[3];
                                        int port = rxdat[4];
                                        port = (port << 8) | rxdat[5];

                                        //Console.WriteLine(ip_address + " " + port);

                                        //get detailed peer info and add peer to sync

                                        Task t = new Task(async () =>
                                        {
                                            try
                                            {
                                                ManagedStreamV2 mps = new ManagedStreamV2(0 , ip_address, port);

                                                Stream peer_stream = await mps.GetStream();

                                                byte[] remote_instance_id = GetPeerInstanceID(peer_stream);

                                                mps.CleanupConnection();

                                                lock (sync_status_lock)
                                                    sync_status.AddPeer(new PeerInfo(remote_instance_id, new ConnectionProperties(ip_address, port), peer_discovery_time));
                                            }
                                            catch(Exception ex)
                                            {
                                                Console.WriteLine("An exception occured while gathering detailed peer information.");
                                            }
                                        });
                                        t.Start();
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("An execption occured while searching for peers.");
                                        throw;
                                    }
                                }

                                sw.Stop();

                                lock(sync_status_lock)
                                sync_status.Refresh();

                                Thread.Sleep(500);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An execption occured. Peer sync is reinitalizing.");
                    }
                }
            });
        } 
    }
}
