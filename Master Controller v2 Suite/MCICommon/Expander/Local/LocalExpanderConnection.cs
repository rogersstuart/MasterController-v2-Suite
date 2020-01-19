using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using GlobalUtilities;

namespace MCICommon
{
    public class LocalExpanderConnection
    {
        private ConnectionProperties connprop;

        private TcpClient tcpClient; NetworkStream netStream;

        private byte[] ack = { 0x1, 0x1, 0x9a };

        private Object pending_command_queue_lock = new Object();
        private Queue<ExpanderCommandHandle> pending_command_queue = new Queue<ExpanderCommandHandle>();

        //Object completed_command_queue_lock = new Object();
        //Queue<ExpanderCommand> completed_command_queue = new Queue<ExpanderCommand>();

        //Object wait_handle_queue_lock = new Object();
        //Queue<AutoResetEvent> wait_handle_queue = new Queue<AutoResetEvent>();

        private Task comm_thread;
        private volatile bool comm_running = true;

        private volatile bool comm_busy = false;

        private const int MIN_COMMAND_BUF_SIZE = 5;
        private const int MAX_COMMAND_BUF_SIZE = 6;
        private const int DELAY_PER_FAILURE = 150;
        private int cur_command_buf_size = MIN_COMMAND_BUF_SIZE;
        
        public LocalExpanderConnection(ConnectionProperties connprop)
        {
            this.connprop = connprop;

            comm_thread = commLoop();
        }

        private Task commLoop()
        {
            return Task.Run(async () =>
            {
                FileTextLogger.logger.AppendLog("LEC, COM - Started Communications Loop");

                long total_errors = 0;

                BlockingConnectionReset();

                while (comm_running)
                {
                    int transmit_queue_size = 0;
                    lock (pending_command_queue_lock)
                        transmit_queue_size = pending_command_queue.Count;

                    if (transmit_queue_size > 0)
                    {
                        comm_busy = true;
                        FileTextLogger.logger.AppendLog("LEC, COM - Transmit Queue Size = " + transmit_queue_size);
                        FileTextLogger.logger.AppendLog("LEC, COM - Buffer Size = " + cur_command_buf_size);
                        //Console.WriteLine(transmit_queue_size);
                        //Console.WriteLine("buf size =" + cur_command_buf_size);

                        Queue<ExpanderCommandHandle> current_commands = new Queue<ExpanderCommandHandle>();

                        lock (pending_command_queue_lock)
                            for (int object_counter = 0; object_counter < cur_command_buf_size && (transmit_queue_size - object_counter) > 0; object_counter++)
                                current_commands.Enqueue(pending_command_queue.Dequeue());

                        int rep_counter = 0;

                        Func<Task> process_command = async delegate
                        {
                            //BlockingConnectionReset();
                            while (comm_running)
                                while (comm_running)
                                {
                                    //Console.WriteLine("rep_counter " + rep_counter);
                                    FileTextLogger.logger.AppendLog("LEC, COM - rep_counter = " + rep_counter);

                                    //BlockingConnectionReset();

                                    try
                                    {
                                        for (int object_counter = 0; object_counter < current_commands.Count; object_counter++)
                                            netStream.Write(current_commands.ElementAt(object_counter).Command.TxPacket, 0, current_commands.ElementAt(object_counter).Command.TxPacket.Length); //write current commands

                                        int success_counter = 0;
                                        for (int object_counter = 0; object_counter < current_commands.Count; object_counter++)
                                        {
                                            byte[] resp_buffer = new byte[3];
                                            netStream.Read(resp_buffer, 0, 3); //read ack

                                            if (ack.SequenceEqual(resp_buffer))
                                            {
                                                if (current_commands.ElementAt(object_counter).Command.ResponseExpected)
                                                {
                                                    resp_buffer = new byte[current_commands.ElementAt(object_counter).Command.ResponseLength];
                                                    netStream.Read(resp_buffer, 0, current_commands.ElementAt(object_counter).Command.ResponseLength);

                                                    if (Utilities.CRC8(resp_buffer, current_commands.ElementAt(object_counter).Command.ResponseLength - 1) == resp_buffer[current_commands.ElementAt(object_counter).Command.ResponseLength - 1])
                                                    {
                                                        current_commands.ElementAt(object_counter).Command.RxPacket = resp_buffer;
                                                        //return;
                                                        success_counter++;
                                                    }
                                                    else
                                                        break; //failure. restart loop
                                                }
                                                else
                                                    success_counter++; //return;
                                            }
                                            else
                                                break; //failure. restart loop
                                        }

                                        if (success_counter == current_commands.Count)
                                            return;
                                    }
                                    catch (Exception ex)
                                    {
                                        FileTextLogger.logger.AppendLog("LEC, COM - Process Command Failure");
                                        //Console.WriteLine("Process Command Failure ");
                                        rep_counter++;
                                        break;
                                    }

                                    BlockingConnectionReset();

                                    //if this code has been reached then an error has occured
                                    await Task.Delay((cur_command_buf_size + 1) * DELAY_PER_FAILURE);

                                    cur_command_buf_size = MIN_COMMAND_BUF_SIZE; //set the buffer size to 1

                                    //dequeue all but one command. there will always be at least one command in the queue
                                    if (current_commands.Count > MIN_COMMAND_BUF_SIZE)
                                        lock (pending_command_queue_lock)
                                            while (current_commands.Count != MIN_COMMAND_BUF_SIZE)
                                                pending_command_queue.Enqueue(current_commands.Dequeue());

                                    rep_counter++;
                                }
                        };

                        await process_command();

                        //Console.WriteLine("Expander queue size:" + cur_command_buf_size);
                        FileTextLogger.logger.AppendLog("LEC, COM - Expander queue size = " + cur_command_buf_size);

                        if (rep_counter == 0)
                        {
                            if (cur_command_buf_size < MAX_COMMAND_BUF_SIZE)
                                cur_command_buf_size++;
                        }
                        else
                            total_errors++;

                        //Console.WriteLine("total errors " + total_errors);
                        FileTextLogger.logger.AppendLog("LEC, COM - total errors = " + total_errors);

                        foreach (ExpanderCommandHandle cmdhdl in current_commands)
                            cmdhdl.Handle.Set();

                        //int num_completed_commands = current_commands.Count;
                        //lock(completed_command_queue_lock)
                        //    for (int object_counter = 0; object_counter < current_commands.Count; object_counter++)
                        //    {
                        //        completed_command_queue.Enqueue(current_commands.ElementAt(object_counter));
                        //    }

                        //for(int completion_counter = 0; completion_counter < num_completed_commands; completion_counter++)
                        //    lock (wait_handle_queue_lock)
                        //        wait_handle_queue.Dequeue().Set();
                    }
                    else
                    {
                        comm_busy = false;
                    }
                }
            });
            
        }

        private void BlockingConnectionReset()
        {
            while (true)
                if (netStream == null || CloseConnection())
                    break;
                else
                    FileTextLogger.logger.AppendLog("LEC - Failed To Close Connection");
            //Console.WriteLine("Failed To Close Connection");

            while (true)
                if (OpenConnection())
                    break;
                else
                    FileTextLogger.logger.AppendLog("LEC - Failed To Open Connection");

            FileTextLogger.logger.AppendLog("LEC - Connection Reset Was Successful");
        }

        private bool OpenConnection()
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(connprop.IPAddress, connprop.TCPPort);

                netStream = tcpClient.GetStream();
                netStream.ReadTimeout = 2000;
                netStream.WriteTimeout = 200;

                FileTextLogger.logger.AppendLog("LEC - Connection Opened");

                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error Opening Connection and Stream");
                FileTextLogger.logger.AppendLog("Error Opening Connection and Stream");

                return false;
            }
        }

        private bool CloseConnection()
        {
            try
            {
                netStream = null; 

                if(tcpClient != null)
                    tcpClient.Close();

                FileTextLogger.logger.AppendLog("LEC - Connection Closed");

                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error Closing Stream and Connection");
                FileTextLogger.logger.AppendLog("Error Closing Stream and Connection");

                return false;
            }
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
                t.Elapsed += (a, b) => { FileTextLogger.logger.AppendLog("LEC - Waiting for simplex-command execution to complete. " + TimeSpan.FromTicks(sw.ElapsedTicks).ToString()); };
                t.Start();

                FileTextLogger.logger.AppendLog("LEC - connecting to = " + connprop.IPAddress + " " + connprop.TCPPort);
                FileTextLogger.logger.AppendLog("LEC - executing command");

                lock (pending_command_queue_lock)
                    pending_command_queue.Enqueue(cmdhdl);

                sw.Stop();
                t.Stop();

                FileTextLogger.logger.AppendLog("LEC, SCE - Command Enqueued");
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
                t.Elapsed += (a, b) => { FileTextLogger.logger.AppendLog("LEC - Waiting for multi-command execution to complete." + TimeSpan.FromTicks(sw.ElapsedTicks).ToString()); };
                t.Start();

                lock (pending_command_queue_lock)
                   for (int index_counter = 0; index_counter < expcmds.Length; index_counter++) 
                        pending_command_queue.Enqueue(cmdhdls[index_counter]);

                sw.Stop();
                t.Stop();

                FileTextLogger.logger.AppendLog("LEC, MCE - Commands Enqueued");
            });

            return cmdhdls;
        }

        public void Stop()
        {
            comm_running = false;
            while (comm_thread.Status == TaskStatus.Running) ;
            CloseConnection();
        }

        public bool Busy
        {
            get
            {
                return comm_busy;
            }
        }
    }
}