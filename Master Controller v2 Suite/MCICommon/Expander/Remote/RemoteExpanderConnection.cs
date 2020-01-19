using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization;
using System.Xml;
using System.Diagnostics;
using GlobalUtilities;

namespace MCICommon
{
    public class RemoteExpanderConnection
    {
        private ulong device_id;
        private HostInfo hi;

        public RemoteExpanderConnection(ulong device_id, HostInfo hi)
        {
            this.device_id = device_id;
            this.hi = hi;
        }

        public ExpanderCommandHandle EnqueueCommand(ExpanderCommand expcmd)
        {
            ExpanderCommandHandle cmdhdl = new ExpanderCommandHandle(new AutoResetEvent(false), expcmd);

            Task.Run(() =>
            {
                var sw = new Stopwatch();
                sw.Start();

                var t = new System.Timers.Timer(1000);
                t.AutoReset = true;
                t.Elapsed += (a, b) => { FileTextLogger.logger.AppendLog("REC, SCE - Waiting for simplex-command execution to complete. " + TimeSpan.FromTicks(sw.ElapsedTicks).ToString()); };
                t.Start();

                var ctc = new CommandTransactionContainer(device_id, new ExpanderCommand[] { expcmd });

                try
                {
                    FileTextLogger.logger.AppendLog("REC, SCE - connecting to = " + hi.TCPConnectionProperties.AddressString + " " + hi.TCPConnectionProperties.Port);

                    using (var client = new TcpClient())
                    {
                        client.NoDelay = true;
                        client.ReceiveTimeout = 5000;
                        client.SendTimeout = 5000;
                        client.Connect(hi.TCPConnectionProperties.AddressString, hi.TCPConnectionProperties.Port);

                        FileTextLogger.logger.AppendLog("REC, SCE - executing command");

                        try
                        {
                            XMLSerdes.SendPacket(client, ctc);
                        }
                        catch (Exception ex)
                        {
                            FileTextLogger.logger.AppendLog("REC, SCE - An error occured while serializing expander command");
                            FileTextLogger.logger.AppendLog(ex.Message);
                            throw;
                        }

                        try
                        {
                            ctc = (CommandTransactionContainer)XMLSerdes.Decode(XMLSerdes.ReceivePacket(client, typeof(CommandTransactionContainer)), typeof(CommandTransactionContainer));
                        }
                        catch (Exception ex)
                        {
                            FileTextLogger.logger.AppendLog("REC, SCE - An error occured while deserializing expander command");
                            FileTextLogger.logger.AppendLog(ex.Message);
                            throw;
                        }

                        FileTextLogger.logger.AppendLog("REC, SCE - Command Executed");
                    }  
                }
                catch(Exception ex)
                {
                    FileTextLogger.logger.AppendLog("REC, SCE - Simplex-command execution failed to complete. The operation has been aborted.");
                    FileTextLogger.logger.AppendLog(ex.Message);
                }

                sw.Stop();
                t.Stop();

                cmdhdl.Command = ctc.ExpanderCommands[0];

                cmdhdl.Handle.Set();
            });

            return cmdhdl;
        }

        public ExpanderCommandHandle[] EnqueueCommands(ExpanderCommand[] expcmds)
        {
            ExpanderCommandHandle[] cmdhdls = new ExpanderCommandHandle[expcmds.Length];
            for (int index_counter = 0; index_counter < expcmds.Length; index_counter++)
                cmdhdls[index_counter] = new ExpanderCommandHandle(new AutoResetEvent(false), expcmds[index_counter]);

            Task.Run(() =>
            {
                var sw = new Stopwatch();
                sw.Start();

                var t = new System.Timers.Timer(1000);
                t.AutoReset = true;
                t.Elapsed += (a, b) => { FileTextLogger.logger.AppendLog("REC, MCE - Waiting for multi-command execution to complete." + TimeSpan.FromTicks(sw.ElapsedTicks).ToString()); };
                t.Start();

                var ctc = new CommandTransactionContainer(device_id, cmdhdls.Select(x => x.Command).ToArray());

                try
                {
                    FileTextLogger.logger.AppendLog("REC, MCE - connecting to = " + hi.TCPConnectionProperties.AddressString + " " + hi.TCPConnectionProperties.Port);

                    using (var client = new TcpClient())
                    {
                        client.NoDelay = true;
                        client.ReceiveTimeout = 5000;
                        client.SendTimeout = 5000;
                        client.Connect(hi.TCPConnectionProperties.AddressString, hi.TCPConnectionProperties.Port);

                        FileTextLogger.logger.AppendLog("REC, MCE - executing command");

                        try
                        {
                            XMLSerdes.SendPacket(client, ctc);
                        }
                        catch (Exception ex)
                        {
                            FileTextLogger.logger.AppendLog("REC, MCE - An error occured while serializing expander command");
                            FileTextLogger.logger.AppendLog(ex.Message);
                            throw;
                        }

                        try
                        {
                            ctc = (CommandTransactionContainer)XMLSerdes.Decode(XMLSerdes.ReceivePacket(client, typeof(CommandTransactionContainer)), typeof(CommandTransactionContainer));
                        }
                        catch (Exception ex)
                        {
                            FileTextLogger.logger.AppendLog("REC, MCE - An error occured while deserializing expander command");
                            FileTextLogger.logger.AppendLog(ex.Message);
                            throw;
                        }

                        FileTextLogger.logger.AppendLog("REC, MCE - Commands Executed");
                    }
                }
                catch(Exception ex)
                {
                    FileTextLogger.logger.AppendLog("REC, MCE - Multi-command execution failed to complete. The operation has been aborted.");
                    FileTextLogger.logger.AppendLog(ex.Message);
                }

                sw.Stop();
                t.Stop();

                for (int i = 0; i < ctc.ExpanderCommands.Length; i++)
                {
                    cmdhdls[i].Command = ctc.ExpanderCommands[i];

                    cmdhdls[i].Handle.Set();
                }
            });

            return cmdhdls;
        }
    }
}