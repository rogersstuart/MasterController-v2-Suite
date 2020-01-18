using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using MCICommon;

namespace MasterControllerInterface
{
    public static class PeerSyncListener
    {
        private static Thread manager_thread = null;
        private static bool manager_running = false;

        public static void Start()
        {
            if (manager_thread != null)
                Stop();

            if (manager_thread == null)
            {
                manager_thread = GenerateDiscoveryThread();
                manager_running = true;
                manager_thread.Start();
            }
        }

        public static void Stop()
        {

            manager_running = false;

            if (manager_thread != null)
                while (manager_thread.IsAlive) ;

            manager_thread = null;
        }

        public static bool IsRunning
        {
            get { return manager_running; }
        }

        private static Thread GenerateDiscoveryThread()
        {
            return new Thread(delegate ()
            {
                Int32 listen_port = 10245;
                IPAddress local_addr = IPAddress.Parse("127.0.0.1");
                TcpListener server = new TcpListener(local_addr, listen_port);

                server.Server.ReceiveTimeout = 2000;
                server.Server.SendTimeout = 2000;

                server.Start();

                while (manager_running)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            TcpClient client = server.AcceptTcpClient();
                            NetworkStream stream = client.GetStream();

                            int cmd = stream.ReadByte();
                            while (cmd > -1)
                            {
                                if (cmd == '0') //request instance guid
                                {
                                    byte[] instance_id = ConfigurationManager.InstanceID;
                                    stream.Write(instance_id, 0, instance_id.Length);
                                }
                            }
                        }
                        catch (Exception ex)
                        { Console.WriteLine("A PeerSysnc error occured while processing commands."); }
                    });

                    

                    Thread.Sleep(100);
                }

                server.Stop();
            });
        }
    }
}
