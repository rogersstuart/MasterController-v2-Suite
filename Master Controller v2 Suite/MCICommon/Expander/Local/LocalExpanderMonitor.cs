using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MCICommon
{
    [Serializable]
    public class ExpanderMonitor : MarshalByRefObject
    {
        public event FreshStateAvailable freshstateevent;
        public delegate void FreshStateAvailable(ExpanderMonitor expmon, ExpanderEventArgs expevargs);

        ConnectionProperties connprop;

        List<ExpanderEvent> expevents = new List<ExpanderEvent>();
        
        Object expander_connection_lock = new Object();
        [NonSerialized] LocalExpanderConnection expconn;

        Object expander_state_lock = new Object();
        [NonSerialized]ExpanderState expst;

        Object monitor_lock = new Object();
        [NonSerialized] Task monitor_task;

        Object base_state_lock = new Object();
        private ExpanderState base_state;

        private bool[] exp_0_default = {true, false, true, true, false, true, true, false, true, false, false, true, true, false, false, false};
        private bool[] exp_1_default = {true, false, true, true, false, true, true, false, true, false, false, true, true, false, false, false};

        Object expander_write_lock = new Object();

        volatile bool monitor_stop;

        Object hwconfig_lock = new Object();
        HardwareConfiguration exphwconfig = null;

        public ExpanderMonitor(string ip, int port) : this(new ConnectionProperties(ip, port)) { }

        public ExpanderMonitor(ConnectionProperties connprop)
        {
            this.connprop = connprop;

            base_state = new ExpanderState();
            base_state.Expander0State = exp_0_default;
            base_state.Expander1State = exp_1_default;

            exphwconfig = new HardwareConfiguration(exp_0_default, exp_1_default);
        }

        public async Task Stop() { await Stop(false); }

        public async Task Stop(bool ignoreconn)
        {
            if (monitor_task != null)
            {
                monitor_stop = true;
                while (monitor_task.Status == TaskStatus.Running)
                    await Task.Delay(100);
            }

            if (expconn != null && !ignoreconn)
            {
                while (expconn.Busy)
                    await Task.Delay(100);
                expconn.Stop();
                expconn = null;
            }

            lock (expander_state_lock)
               expst = null;
        }

        public async Task Start(){await Start(false);}

        //the ignore connection flag will prevent reinitilization of the connection if the connection is not null
        public async Task Start(bool ignoreconn)
        {
            if (expconn != null && !ignoreconn)
                await Stop();

            if(expconn == null)
                expconn = new LocalExpanderConnection(connprop);

            //write the base state
            ExpanderState bs_copy;
            lock (base_state_lock)
                bs_copy = new ExpanderState(base_state);

            await WriteExpanders(new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true },
                            new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true },
                            bs_copy.Expander0State,
                            bs_copy.Expander1State);
            
            monitor_task = GenerateMonitor();
            monitor_task.Start();
        }

        private Task GenerateMonitor()
        {
            return new Task(async () =>
            {
                monitor_stop = false;
                while (!monitor_stop)
                {
                    await Refresh();

                    for (int index_counter = 0; index_counter < expevents.Count; index_counter++)
                    {
                        ExpanderEvent expevent = expevents.ElementAt(index_counter);
                        await expevent.Process(this);

                        if (!expevent.IsActive)
                        {
                            expevents.RemoveAt(index_counter);

                            if (index_counter > 0)
                                index_counter--;
                        }
                    }
                }
            });
        }

        private Task Refresh()
        {
            return Task.Run(() =>
            {
                ExpanderCommandHandle[] cmdhdls;
                ExpanderCommand[] expcmds = new ExpanderCommand[5];
                expcmds[0] = new ExpanderCommand(ExpanderCommand.READ_EXPANDERS_COMMAND);
                expcmds[1] = new ExpanderCommand(ExpanderCommand.READ_FAN_SPEED_COMMAND);
                expcmds[2] = new ExpanderCommand(ExpanderCommand.READ_POWER_MONITOR_COMMAND);
                expcmds[3] = new ExpanderCommand(ExpanderCommand.READ_TEMPERATURES_COMMAND);
                expcmds[4] = new ExpanderCommand(ExpanderCommand.READ_UPTIME_COMMAND);

                lock(expander_connection_lock)
                    cmdhdls = expconn.EnqueueCommands(expcmds);

                ExpanderState new_state = new ExpanderState();

                //fill expander state
                cmdhdls[0].Handle.WaitOne();
                byte[] buffer = new byte[4];
                Array.Copy(cmdhdls[0].Command.RxPacket, 1, buffer, 0, 4);

                bool[] converted = buffer.SelectMany(Utilities.GetBits).ToArray(); //resulting order 0: 1, 0 1: 1, 0
                Array.Copy(converted, 8, new_state.Expander0State, 0, 8);
                Array.Copy(converted, 0, new_state.Expander0State, 8, 8);
                Array.Copy(converted, 24, new_state.Expander1State, 0, 8);
                Array.Copy(converted, 16, new_state.Expander1State, 8, 8);

                //apply hardware configuration
                lock(hwconfig_lock)
                    for (int index_counter = 0; index_counter < 16; index_counter++ )
                    {
                        new_state.Expander0State[index_counter] ^= exphwconfig.Expander0Configuration[index_counter];
                        new_state.Expander1State[index_counter] ^= exphwconfig.Expander1Configuration[index_counter];
                    }

                cmdhdls[1].Handle.WaitOne();
                buffer = new byte[2];
                Array.Copy(cmdhdls[1].Command.RxPacket, 1, buffer, 0, 2);
                new_state.FanSpeed = BitConverter.ToUInt16(buffer, 0);

                cmdhdls[2].Handle.WaitOne();
                buffer = new byte[8];
                Array.Copy(cmdhdls[2].Command.RxPacket, 1, buffer, 0, 8);
                new_state.BusVoltage = BitConverter.ToSingle(buffer, 0);
                new_state.BusPower = BitConverter.ToSingle(buffer, 4);

                cmdhdls[3].Handle.WaitOne();
                buffer = new byte[12];
                Array.Copy(cmdhdls[3].Command.RxPacket, 1, buffer, 0, 12);
                new_state.FanTemperature = BitConverter.ToSingle(buffer, 0);
                new_state.Board0Temperature = BitConverter.ToSingle(buffer, 4);
                new_state.Board1Temperature = BitConverter.ToSingle(buffer, 8);

                cmdhdls[4].Handle.WaitOne();
                buffer = new byte[8];
                Array.Copy(cmdhdls[4].Command.RxPacket, 1, buffer, 0, 4);
                new_state.Uptime = BitConverter.ToUInt64(buffer, 0);

                lock (expander_state_lock)
                    expst = new ExpanderState(new_state);

                if(freshstateevent != null)
                    freshstateevent(this, new ExpanderEventArgs(new_state));
            });
        }

        public Task WriteExpanders(bool[] exp0_mask, bool[] exp1_mask, bool[] exp0_vals, bool[] exp1_vals)
        {
            return Task.Run(() =>
            {
                //apply hardware configuration
                lock (hwconfig_lock)
                   for (int index_counter = 0; index_counter < 16; index_counter++)
                    {
                       exp0_vals[index_counter] ^= exphwconfig.Expander0Configuration[index_counter];
                       exp1_vals[index_counter] ^= exphwconfig.Expander1Configuration[index_counter];
                   }

                byte[] mask = new byte[4];
                Array.Copy(Utilities.ConvertBoolArrayToBytes(exp0_mask), mask, 2);
                Array.Copy(Utilities.ConvertBoolArrayToBytes(exp1_mask), 0, mask, 2, 2);

                byte[] nbuffer = new byte[4];
                Array.Copy(Utilities.ConvertBoolArrayToBytes(exp0_vals), nbuffer, 2);
                Array.Copy(Utilities.ConvertBoolArrayToBytes(exp1_vals), 0, nbuffer, 2, 2);

                ExpanderCommandHandle cmdhdl;
                lock (expander_write_lock)
                {
                    //read in current expander values
                    lock (expander_connection_lock)
                        cmdhdl = expconn.EnqueueCommand(new ExpanderCommand(ExpanderCommand.READ_EXPANDERS_COMMAND));
                    cmdhdl.Handle.WaitOne();             
                    
                    byte[] write_buffer = new byte[4];
                    Array.Copy(cmdhdl.Command.RxPacket, 1, write_buffer, 0, 4);
                    
                    for (int index_counter = 0; index_counter < 4; index_counter++)
                    {
                        write_buffer[index_counter] &= (byte)~mask[index_counter]; //clear the bits that are changing
                        write_buffer[index_counter] |= (byte)(nbuffer[index_counter] & mask[index_counter]); //overlay the bits that are changing
                    }

                    lock (expander_connection_lock)
                        cmdhdl = expconn.EnqueueCommand(new ExpanderCommand(ExpanderCommand.WRITE_EXPANDERS_COMMAND, write_buffer));
                    cmdhdl.Handle.WaitOne();
                }

            });
        }


        public async Task ResetExpander()
        {
            if (expconn != null)
            {
                await Task.Run(() =>
                {
                    lock (monitor_lock)
                    {
                        Stop(true).Wait(); //this will stop the monitor thread and connection

                        lock (expander_connection_lock)
                        {
                            while (expconn.Busy) ; //wait for all commands to complete

                            expconn.EnqueueCommand(new ExpanderCommand(ExpanderCommand.RESET_COMMAND)).Handle.WaitOne();

                            Thread.Sleep(1000); //wait for transmit and execute
                        }

                        Start(true).Wait(); //this will generate a new connection and monitor thread
                    }
                });
            }
        }

        public ExpanderState ExpanderState
        {
            get
            {
                lock (expander_state_lock)
                    if (expst != null)
                        return new ExpanderState(expst);
                    else
                        return new ExpanderState();
            }
        }

        public ExpanderState DefaultState
        {
            get
            {
                lock(base_state_lock)
                    return new ExpanderState(base_state);
            }

            set
            {
                lock (base_state_lock)
                    base_state = new ExpanderState(value);
            }
        }

        public override string ToString()
        {
            if (expconn != null)
                return "Expander (Active) " + connprop.IPAddress + ":" + connprop.TCPPort;
            else
                return "Expander (Inactive) " + connprop.IPAddress + ":" + connprop.TCPPort;
        }

        public List<ExpanderEvent> Events
        {
            get
            {
                return expevents;
            }
        }

        public HardwareConfiguration HardwareConfiguration
        {
            get
            {
                lock (hwconfig_lock)
                    return new HardwareConfiguration(exphwconfig.Expander0Configuration, exphwconfig.Expander1Configuration);
            }

            set
            {
                lock (hwconfig_lock)
                    exphwconfig = new HardwareConfiguration(value.Expander0Configuration, value.Expander1Configuration);
            }
        }
    }
}
