using MCICommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using GlobalUtilities;

namespace DeviceServer
{
    public static class ServerListener
    {
        public static Task listen_for_connections;
        private static volatile bool server_active = false;

        public static void Start()
        {
            DeviceServer.logger.AppendLog(DateTime.Now, "SL - Starting");

            server_active = true;
            listen_for_connections = GenerateListenerTask();
            listen_for_connections.Start();

            DeviceServer.logger.AppendLog(DateTime.Now, "SL - Started");
        }

        public static AutoResetEvent Stop()
        {
            DeviceServer.logger.AppendLog(DateTime.Now, "SL - Stopping");

            var are = new AutoResetEvent(false);

            Task.Run(async () =>
            {
                server_active = false;

                do
                     await Task.Delay(100);
                 while (listen_for_connections.Status == TaskStatus.Running);

                are.Set();

                DeviceServer.logger.AppendLog(DateTime.Now, "SL - Stopped");
            });

            return are;
        }

        public static Task GenerateListenerTask()
        {
            return new Task(async () =>
            {
                var config = MCv2Persistance.Instance.Config;

                var host_guid = config.GUID;
                var sync_port = config.DeviceServerPort;

                TcpListener server =  null;

                while (server_active)
                {
                    try
                    {
                        //IPAddress local_addr = IPAddress.Parse("127.0.0.1");
                        server = new TcpListener(IPAddress.Any, sync_port);

                        //server.Server.ReceiveTimeout = (int)TimeSpan.FromDays(30).TotalMilliseconds;
                        //server.Server.SendTimeout = (int)TimeSpan.FromDays(30).TotalMilliseconds;

                        server.Start();

                        while (server_active)
                            try
                            {
                                var client = server.AcceptTcpClient(); //blocks until connection
                                client.NoDelay = true;
                                HandleClient(client);
                            }
                            catch (Exception ex)
                            {
                                DeviceServer.logger.AppendLog("SL - An exception occured while attempting to accept a client.");
                                await Task.Delay(2000);
                            }
                    }
                    catch (Exception ex) { }

                    if(server != null)
                    {
                        try
                        {
                            server.Stop();
                        }
                        catch(Exception ex) { }
                        

                        server = null;
                    }
                }
            });
        }

        public static void HandleClient(TcpClient client)
        {
            Task.Run(() =>
            {
                DeviceServer.logger.AppendLog("SL - Client Connected");

                while (true)
                {
                    CommandTransactionContainer ctc = null;

                    try
                    {
                        ctc = (CommandTransactionContainer)XMLSerdes.Decode(XMLSerdes.ReceivePacket(client, typeof(CommandTransactionContainer)), typeof(CommandTransactionContainer));
                    }
                    catch (Exception ex)
                    {
                        DeviceServer.logger.AppendLog("SL - An exception occured while attempting to deserialize received container.");

                        DeviceServer.logger.AppendLog(ex.Message);

                        break;
                    }

                    if (ctc != null)
                    {
                        DeviceServer.logger.AppendLog("SL - Opening Device Connection");

                        var connection = DeviceConnectionManager.GetConnecion(ctc.DeviceID);

                        var handles = connection.EnqueueCommands(ctc.ExpanderCommands);

                        var sw = new Stopwatch();
                        sw.Start();
                        var t = new System.Timers.Timer(1000);
                        t.AutoReset = true;
                        t.Elapsed += (a, b) => { DeviceServer.logger.AppendLog("SL - Waiting for command execution to complete." + TimeSpan.FromTicks(sw.ElapsedTicks).ToString()); };
                        t.Start();
                        
                        foreach(var handle in handles)
                            handle.Handle.WaitOne();
                        
                        sw.Stop();
                        t.Stop();

                        DeviceServer.logger.AppendLog("SL - Command Execution Complete");

                        var commands = handles.Select(x => x.Command).ToArray();

                        var r_ctc = new CommandTransactionContainer(ctc.DeviceID, commands);

                        try
                        {
                            XMLSerdes.SendPacket(client, r_ctc);
                        }
                        catch (Exception ex)
                        {
                            DeviceServer.logger.AppendLog("SL - An exception occured while attempting to serialize transmit container.");

                            DeviceServer.logger.AppendLog(ex.Message);

                            break;
                        }
                    }

                    break;
                }

                try
                {
                    if (client != null && client.Connected)
                        client.Close();
                }
                catch(Exception ex) { }
            });
        }
    }
}
