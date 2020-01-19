using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;
using MCICommon;
using GlobalUtilities;

namespace DeviceServer
{
    public static class ServerAdvertiser
    {
        private static DataContractSerializer xmlSerializer = new DataContractSerializer(typeof(HostInfo));

        private static System.Timers.Timer refresh_timer = new System.Timers.Timer(2000);
        private static volatile bool refresh_active = false;

        private static int sync_broadcast_port;
        private static string host_guid;

        public static void Start()
        {
            DeviceServer.logger.AppendLog(DateTime.Now, "SA - Starting");

            var config = MCv2Persistance.Config;

            host_guid = config.GUID;
            sync_broadcast_port = config.DeviceServerPort;

            //refresh_timer.AutoReset = true;
            refresh_timer.Elapsed += async (a, b) =>
            {
                refresh_active = true;
                refresh_timer.Stop();

                await UDPAnnounce();

                refresh_timer.Start();
                refresh_active = false;
            };

            refresh_timer.Start();

            DeviceServer.logger.AppendLog(DateTime.Now, "SA - Started");
        }

        public static AutoResetEvent Stop()
        {
            DeviceServer.logger.AppendLog(DateTime.Now, "SA - Stopping");

            var are = new AutoResetEvent(false);

            Task.Run(async () =>
            {
                refresh_timer.Stop();

                do
                    await Task.Delay(100);
                while (refresh_active);

                are.Set();

                DeviceServer.logger.AppendLog(DateTime.Now, "SA - Stopped");
            });

            return are;
        }

        private static async Task UDPAnnounce()
        {
            UdpClient udp_client = null ;

            try
            {
                udp_client = new UdpClient();
                udp_client.EnableBroadcast = true;
            }
            catch (Exception ex)
            {
                if (udp_client != null)
                    udp_client.Close();

                DeviceServer.logger.AppendLog(DateTime.Now, "SA - An error occured while opening a broadcast client. Holding off for 1 minute.");
                await Task.Delay(60000);

                return;
            }

            string s;
            try
            {
                s = GetHostAddress();
            }
            catch (Exception ex) { return; }

            TCPConnectionProperties tcpconnprop = new TCPConnectionProperties("Device Server", s, sync_broadcast_port);

            HostInfo hi = new HostInfo();
            hi.HostGUID = host_guid;
            hi.TCPConnectionProperties = tcpconnprop;

            byte[] b;
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    xmlSerializer.WriteObject(ms, hi);
                }
                catch (Exception ex)
                {
                    DeviceServer.logger.AppendLog(DateTime.Now, "SA - Ann error occured during HostInfo serialization.");
                    return;
                }

                ms.Flush();
                b = ms.ToArray();
            }

            try
            {
                IPEndPoint local_endpoint = new IPEndPoint(IPAddress.Broadcast.Address, sync_broadcast_port);
                udp_client.Send(b, b.Length, local_endpoint);
            }
            catch (Exception ex)
            {
                if (udp_client != null)
                    udp_client.Close();

                DeviceServer.logger.AppendLog(DateTime.Now, "SA - An error occured while sending the HostInfo message. Holding off for 1 minute.");
                await Task.Delay(60000);
            }

            if(udp_client != null)
                udp_client.Close();
        }

        private static string GetHostAddress()
        {
            IPAddress[] host_addresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress address in host_addresses)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                    return address.ToString();

            throw new Exception("Host Has No Valid Address");
        }
    }
}
