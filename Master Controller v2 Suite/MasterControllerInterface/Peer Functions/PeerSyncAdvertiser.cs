/*
 * 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MasterControllerInterface
{
        public static class PeerSyncAdvertiser
        {
            private static Thread advertiser_thread = null;
            private static bool advertiser_running = false;

            private static int sync_broadcast_port = 10245;

            public static void Start()
            {
                if (advertiser_thread != null)
                    Stop();

                if (advertiser_thread == null)
                {
                    advertiser_thread = GenerateAdvertiserThread();
                    advertiser_running = true;
                    advertiser_thread.Start();
                }
            }

            public static void Stop()
            {
                advertiser_running = false;

                if(advertiser_thread != null)
                    while (advertiser_thread.IsAlive) ;

                advertiser_thread = null;
            }

            private static Thread GenerateAdvertiserThread()
            {
                return new Thread(delegate ()
                {
                    UdpClient udp_client = new UdpClient();
                    udp_client.EnableBroadcast = true;

                    IPEndPoint expander_endpoint = new IPEndPoint(IPAddress.Broadcast, sync_broadcast_port);

                    while (advertiser_running)
                    {
                        string s = GetHostAddress();
                        string[] split = s.Split('.');

                        byte[] b = {Convert.ToByte(split[0]), Convert.ToByte(split[1]) , Convert.ToByte(split[2]),
                        Convert.ToByte(split[3]), BitConverter.GetBytes(sync_broadcast_port)[1],
                        BitConverter.GetBytes(sync_broadcast_port)[0]};

                        udp_client.Send(b, b.Length, expander_endpoint);

                        Thread.Sleep(1000);
                    }
                });
            }

            private static string GetHostAddress()
            {
                IPAddress[] host_addresses = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (IPAddress address in host_addresses)
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                        return address.ToString();

                throw new Exception("Host Has No Valid Address");
            }

            private static byte[] GetBytes<T>(T t)
            {
                BinaryFormatter bf = new BinaryFormatter();

                using (MemoryStream ms = new MemoryStream())
                {
                    bf.Serialize(ms, t);
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }

            public static bool IsRunning
            {
                get { return advertiser_running; }
            }
        }
}
