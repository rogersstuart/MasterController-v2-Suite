using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCICommon;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;

namespace MCICommon
{
    public static class DeviceServerTracker
    {
        private static DataContractSerializer xmlSerializer = new DataContractSerializer(typeof(HostInfo));

        private static UdpClient udp_listener = null;
        private static IPEndPoint endpoint = null;

        private static System.Timers.Timer refresh_timer = new System.Timers.Timer(100);

        private static volatile bool refresh_active = false;

        private static Object hi_lock = new Object();
        private static HostInfo hi = null;
        private static TimeSpan host_info_retained_for = TimeSpan.FromMinutes(1);
        private static DateTime host_info_retained_at = DateTime.Now;

        private static bool refresh_enabled = false;

        public static void Start()
        {
            refresh_enabled = true;

            udp_listener = new UdpClient();

            udp_listener.Client.ReceiveTimeout = 2000;
            udp_listener.EnableBroadcast = true;

            endpoint = new IPEndPoint(IPAddress.Any, MCv2Persistance.Config.DeviceServerPort);

            udp_listener.Client.Bind(endpoint);

            Track();

            //refresh_timer.AutoReset = true;
            refresh_timer.Elapsed += (a, b) =>
            {
                refresh_active = true;
                refresh_timer.Stop();

                Track();

                if(refresh_enabled)
                refresh_timer.Start();
                refresh_active = false;
            };

            refresh_timer.Start();

        }

        private static void Track()
        { 
            byte[] rxdat = null;

            try
            {
                rxdat = udp_listener.Receive(ref endpoint);
            }
            catch (Exception ex){}

            if(rxdat != null)
            {
                using (MemoryStream ms = new MemoryStream(rxdat))
                using (XmlReader xmlReader = XmlReader.Create(ms, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true }))
                    lock (hi_lock)
                    {
                        hi = (HostInfo)xmlSerializer.ReadObject(xmlReader);
                        host_info_retained_at = DateTime.Now;
                    }  
            }
            else
                //no device server host info received
                lock (hi_lock)
                    if (DateTime.Now - host_info_retained_at >= host_info_retained_for)
                    {
                        host_info_retained_at = DateTime.Now;
                        hi = null;
                    }
        }

        public static void Stop()
        {
            refresh_enabled = false;
            refresh_timer.Stop();

            /*
            var are = new AutoResetEvent(false);

            Task.Run(async () =>
            {
                do
                    await Task.Delay(100);
                while (refresh_active);
                */

                if(udp_listener != null)
                {
                    udp_listener.Close();
                    udp_listener = null;
                    endpoint = null;
                }
                /*
                are.Set();
            });

            return are;
            */
        }

        public static HostInfo DeviceServerHostInfo
        {
            get
            {
                lock(hi_lock)
                    return hi;
            }
        }
    }
}
