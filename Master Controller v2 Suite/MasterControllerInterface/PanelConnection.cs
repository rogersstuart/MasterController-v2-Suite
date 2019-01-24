using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using MCICommon;

namespace MasterControllerInterface
{
    class PanelConnection
    {
        ConnectionProperties connprop;
        
        TcpClient tcpClient; NetworkStream netStream;

        private byte[] ack = { 0x1, 0x1, 0x9a };

        Object pending_command_queue_lock = new Object();
        Queue<PanelCommandHandle> pending_command_queue = new Queue<PanelCommandHandle>();

        Thread comm_thread;
        volatile bool comm_running;

        volatile bool comm_busy = false;

        const int MIN_COMMAND_BUF_SIZE = 2;
        const int MAX_COMMAND_BUF_SIZE = 2;
        const int BUFF_SIZE_INCREASE_RATE = 1;
        const int DELAY_PER_FAILURE = 20;
        int cur_command_buf_size = MIN_COMMAND_BUF_SIZE;
        
        public PanelConnection(ConnectionProperties connprop)
        {
            this.connprop = connprop;

            comm_thread = new Thread(delegate() { commLoop(); });
            comm_thread.Start();
        }

        void commLoop()
        {
            long total_errors = 0;
            comm_running = true;

            BlockingConnectionReset();
            while (comm_running)
            {
                int transmit_queue_size = 0;
                lock (pending_command_queue_lock)
                    transmit_queue_size = pending_command_queue.Count;

                if (transmit_queue_size > 0)
                {
                    comm_busy = true;

                    Queue<PanelCommandHandle> current_commands = new Queue<PanelCommandHandle>();

                    lock (pending_command_queue_lock)
                        for (int object_counter = 0; object_counter < cur_command_buf_size && (transmit_queue_size - object_counter) > 0; object_counter++)
                            current_commands.Enqueue(pending_command_queue.Dequeue());

                    int rep_counter = 0;

                    BlockingConnectionReset();

                    Action process_command = delegate
                    {
                        while (comm_running)
                        {
                            while (comm_running)
                            {
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
                                                {
                                                    Console.WriteLine("Failed CRC Check.");
                                                    break; //failure. restart loop
                                                }
                                            }
                                            else
                                                success_counter++; //return;
                                        }
                                        else
                                        {
                                            Console.WriteLine("expected ack. not received.");
                                            break; //failure. restart loop
                                        }
                                    }

                                    if (success_counter == current_commands.Count)
                                        return;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Process Command Failure ");
                                    //rep_counter++;
                                    break;
                                }
                            }

                            //if this code has been reached then an error has occured

                            Thread.Sleep((cur_command_buf_size + 1) * DELAY_PER_FAILURE);

                            cur_command_buf_size = MIN_COMMAND_BUF_SIZE; //set the buffer size to 1

                            //dequeue all but one command. there will always be at least one command in the current commands queue
                            if (current_commands.Count > 1)
                                lock (pending_command_queue_lock)
                                    while (current_commands.Count != MIN_COMMAND_BUF_SIZE)
                                    {
                                        //if (pending_command_queue.Count > 0)
                                        //{
                                            PanelCommandHandle[] pending_holding = pending_command_queue.ToArray();
                                            PanelCommandHandle[] current_holding = current_commands.ToArray();

                                            PanelCommandHandle[] merged_array = new PanelCommandHandle[pending_holding.Length + current_holding.Length];
                                            Array.Copy(current_holding, merged_array, current_holding.Length);
                                            Array.Copy(pending_holding, 0, merged_array, current_holding.Length, pending_holding.Length);
                                            
                                            pending_command_queue = new Queue<PanelCommandHandle>(merged_array);
                                            current_commands = new Queue<PanelCommandHandle>();

                                            current_commands.Enqueue(pending_command_queue.Dequeue());
                                        //}
                                    }

                            rep_counter++;

                            //netStream.Flush();
                            BlockingConnectionReset();
                        }
                    };
                    process_command();

                    //Console.WriteLine("Panel queue size:" + cur_command_buf_size);

                    if (rep_counter == 0)
                    {
                        if (cur_command_buf_size < MAX_COMMAND_BUF_SIZE)
                            cur_command_buf_size += BUFF_SIZE_INCREASE_RATE;
                    }
                    else
                        total_errors++;

                    //Console.WriteLine(cur_command_buf_size);

                    foreach (PanelCommandHandle cmdhdl in current_commands)
                        cmdhdl.Handle.Set();
                }
                else
                {
                    comm_busy = false;
                }
            }
        }

        private void BlockingConnectionReset()
        {
            while (true)
                if (netStream == null || CloseConnection())
                    break;
                else
                    Console.WriteLine("Failed To Close Connection");

            while (true)
                if (OpenConnection())
                    break;
                else
                    Console.WriteLine("Failed To Open Connection");
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

                return true;
            }
            catch (Exception ex)
            {
                //ErrorLogManager.AppendLog("Failed To Connect To Panel - \"" + connprop.ToString() + "\"", true);

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

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Closing Stream and Connection");

                return false;
            }
        }

        public PanelCommandHandle EnqueueCommand(PanelCommand expcmd)
        {
            PanelCommandHandle cmdhdl = new PanelCommandHandle(new AutoResetEvent(false), expcmd);
            
            Task.Run(() =>
            {           
                lock(pending_command_queue_lock)
                   pending_command_queue.Enqueue(cmdhdl);               
            });

            return cmdhdl;
        }

        public PanelCommandHandle[] EnqueueCommands(PanelCommand[] expcmds)
        {
            PanelCommandHandle[] cmdhdls = new PanelCommandHandle[expcmds.Length];
            for(int index_counter = 0; index_counter < expcmds.Length; index_counter++)
                cmdhdls[index_counter] = new PanelCommandHandle(new AutoResetEvent(false), expcmds[index_counter]);
            
            Task.Run(() =>
            {
                lock (pending_command_queue_lock)
                    for(int index_counter = 0; index_counter < expcmds.Length; index_counter++)
                        pending_command_queue.Enqueue(cmdhdls[index_counter]);
            });

            return cmdhdls;
        }

        public void Stop()
        {
            comm_running = false;
            while (comm_thread.IsAlive) ;
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
