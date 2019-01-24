using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Timers;
using MCICommon;

namespace MasterControllerInterface
{
    public class ManagedStreamV2
    {
        private TcpClient client = null;

        private ulong device_id;
        private string ip_or_host;
        private int port;

        public ManagedStreamV2(ulong device_id, string ip_or_host, int port)
        {
            this.device_id = device_id;
            this.ip_or_host = ip_or_host;
            this.port = port;
        }

        public static async Task<ManagedStreamV2> GenerateInstance(ulong device_id)
        {
            ManagedStreamV2 stream_gen = null;

            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

            try
            {
                TCPConnectionProperties tcpconnprop = await DatabaseUtilities.GetDevicePropertiesFromDatabase(sqlconn.Connection, device_id);

                stream_gen = new ManagedStreamV2(device_id, tcpconnprop.AddressString, tcpconnprop.Port);

                await stream_gen.InitConnection();
            }
            catch (Exception ex) { }

            ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            return stream_gen;
        }

        private async Task InitConnection()
        {
            client = new TcpClient();
            client.ReceiveTimeout = 2000;
            client.SendTimeout = 2000;

            IPEndPoint end_point = new IPEndPoint((await Dns.GetHostAddressesAsync(ip_or_host))[0], port);

            await client.ConnectAsync(end_point.Address, end_point.Port);

            if (client == null || !client.Connected)
                throw new Exception("Unable to open TCP connection.");

        }

        public void CleanupConnection()
        {
            //close connection
            if (client != null && client.Connected)
                client.Close();
            client = null;
        }

        public Task<Stream> GetStream()
        {
            return GetStream(5);
        }

        public async Task<Stream> GetStream(int num_attempts)
        {
            return await Task.Run(new Func<Task<Stream>>(async () =>
            {
                for (int i = 0; i < num_attempts; i++)
                    try
                    {
                        if (client == null)
                            await InitConnection();
                        if (!client.Connected)
                        {
                            CleanupConnection();
                            await InitConnection();
                        }
                        return client.GetStream();
                    }
                    catch (Exception ex) { }

                throw new Exception("Failed to Obtain Stream");
            }));
        }

        public ulong DeviceID
        {
            get { return device_id; }
        }
    }
}
