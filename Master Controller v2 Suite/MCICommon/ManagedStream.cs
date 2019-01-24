using System;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Timers;

namespace MCICommon
{
    public static class ManagedStream
    {
        private static TcpClient client = null;
        private static SerialPort serial = null;

        private static async Task InitConnection(string ip_or_host, int port)
        {
            client = new TcpClient();
            client.ReceiveTimeout = 2000;
            client.SendTimeout = 2000;

            IPEndPoint end_point = new IPEndPoint((await Dns.GetHostAddressesAsync(ip_or_host))[0], port);

            await client.ConnectAsync(end_point.Address, end_point.Port);

            if (client == null || !client.Connected)
                throw new Exception("Unable to open TCP connection.");

        }

        private static async Task InitConnection(string com_port)
        {
            serial = beginSerial(com_port);

            if (serial == null)
                throw new Exception("Unable to open selected COM port.");

        }

        private static async Task InitConnection()
        {
            switch (ConfigurationManager.ConnectionType)
            {
                case 0:
                    //opening com
                    serial = beginSerial(ConfigurationManager.SelectedCOMPort);

                    if (serial == null)
                        throw new Exception("Unable to open selected COM port.");

                    break;

                case 1:

                    client = new TcpClient();
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;

                    IPEndPoint end_point = await ConfigurationManager.SelectedTCPConnection.GetIPEndPointAsync();

                    await client.ConnectAsync(end_point.Address, end_point.Port);

                    if (client == null || !client.Connected)
                        throw new Exception("Unable to open TCP connection.");

                    break;

                default:
                    throw new Exception("Internal: Invalid Connection Type");
                    break;
            }
        }

        public static void CleanupConnection()
        {
            //close connection
            switch (ConfigurationManager.ConnectionType)
            {
                case 0:
                    if (serial != null && serial.IsOpen)
                        endSerial(serial);
                    serial = null;
                    break;

                case 1:
                    if (client != null && client.Connected)
                        client.Close();
                    client = null;
                    break;

                default:
                    throw new Exception("Internal: Invalid Connection Type");
                    break;
            }
        }

        private static SerialPort beginSerial(string port_name)
        {
            try
            {
                SerialPort com_connection = new SerialPort(port_name, 500000, Parity.None, 8, StopBits.One);
                com_connection.NewLine = "\r\n";
                com_connection.ReadTimeout = 2000;
                com_connection.WriteTimeout = 2000;

                com_connection.Open();

                return com_connection;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static void endSerial(SerialPort com_connection)
        {
            com_connection.Close();
        }

        public static async Task<Stream> GetStream(string ip_or_host, int port)
        {
            return await Task.Run(new Func<Task<Stream>>(async () =>
            {
                if (client == null)
                    await InitConnection(ip_or_host, port);
                if (!client.Connected)
                {
                    CleanupConnection();
                    await InitConnection(ip_or_host, port);
                }

                return client.GetStream();
            }));
        }

        public static async Task<Stream> GetStream(string com_port)
        {
            return await Task.Run(new Func<Task<Stream>>(async () =>
            {
                if (serial == null)
                    await InitConnection(com_port);
                if (!serial.IsOpen)
                {
                    CleanupConnection();
                    await InitConnection(com_port);
                }
                return serial.BaseStream;
            }));
        }

        public static Task<Stream> GetStream()
        {
            return GetStream(3);
        }

        public static async Task<Stream> GetStream(int num_attempts)
        {
            return await Task.Run(new Func<Task<Stream>>(async () =>
            {
                for(int i = 0; i < num_attempts; i++)
                    try
                    {
                        switch (ConfigurationManager.ConnectionType)
                        {
                            case 0:
                                if (serial == null)
                                    await InitConnection();
                                if (!serial.IsOpen)
                                {
                                    CleanupConnection();
                                    await InitConnection();
                                }
                                return serial.BaseStream;
                                break;
                            case 1:
                                if (client == null)
                                    await InitConnection();
                                if (!client.Connected)
                                {
                                    CleanupConnection();
                                    await InitConnection();
                                }
                                return client.GetStream();
                                break;
                            default:
                                throw new Exception("Internal: Invalid Connection Type");
                                break;

                        }
                    }
                    catch(Exception ex){}

                throw new Exception("Failed to Obtain Stream");
            }));
        }
    }
}
