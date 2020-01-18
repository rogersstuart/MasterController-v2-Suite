using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace MCICommon
{
    public class RemoteControllerMonitor
    {
        public event FreshStateAvailable freshstateevent;
        public delegate void FreshStateAvailable(RemoteControllerMonitor expmon, ControllerEventArgs expevargs);

        private Object monitor_lock = new Object();

        private Object expander_event_lock = new Object();
        private List<RemoteControllerEvent> expevents = new List<RemoteControllerEvent>();

        private Object expander_connection_lock = new Object();
        private RemoteControllerConnection expconn;

        private Object expander_state_lock = new Object();
        private ControllerState expst;

        private Object base_state_lock = new Object();
        private ControllerState base_state;

        private bool[] exp_0_default = { true, false, true, true, false, true, true, false, true, false, false, true, true, false, false, false };
        private bool[] exp_1_default = { true, false, true, true, false, true, true, false, true, false, false, true, true, false, false, false };

        private Object expander_write_lock = new Object();

        private HostInfo hi;
        private ulong device_id;

        private volatile bool monitor_stop;

        private Object hwconfig_lock = new Object();
        private HardwareConfiguration exphwconfig = null;

        private ConcurrentQueue<Task> write_tasks = new ConcurrentQueue<Task>();

        public RemoteControllerMonitor(ulong device_id)
        {
            hi = DeviceServerTracker.DeviceServerHostInfo;

            this.device_id = device_id;

            //base_state = new ControllerState();
            //base_state.Expander0State = exp_0_default;

            //exphwconfig = new HardwareConfiguration(exp_0_default, exp_1_default);
        }

        public void Stop(bool ignoreconn = false)
        {
            monitor_stop = true;

            Task.Delay(1000);

            if (expconn != null && !ignoreconn)
            {
                //while (expconn.Busy)
                //    await Task.Delay(100);
                //expconn.Stop();
                expconn = null;
            }

            //lock (expander_state_lock)
            //    expst = null;
        }

        //the ignore connection flag will prevent reinitilization of the connection if the connection is not null
        public void Start(bool ignoreconn = false)
        {
            /*
            DebugWriter.AppendLog("REM - Starting");

            if (expconn != null && !ignoreconn)
                Stop();

            if (expconn == null)
                expconn = new RemoteExpanderConnection(device_id, DeviceServerTracker.DeviceServerHostInfo);

            //write the base state
            ExpanderState bs_copy;
            lock (base_state_lock)
                bs_copy = new ControllerState(base_state);

            Task.Run(() =>
            {
                
                WriteExpanders(new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true },
                            new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true },
                            bs_copy.Expander0State,
                            bs_copy.Expander1State);
                            
            });


            GenerateWorkers();

            DebugWriter.AppendLog("REM - Started");
            */
        }

        private void GenerateWorkers()
        {
            monitor_stop = false;

            Task.Run(() =>
            {
                while (!monitor_stop)
                {
                    while (write_tasks.Count() > 0)
                    {
                        Task t = null;
                        write_tasks.TryDequeue(out t);
                        if (t != null)
                        {
                            t.Start();
                            t.Wait();
                        }
                    }

                    try
                    {
                        Refresh();
                    }
                    catch (Exception ex)
                    {
                        DebugWriter.AppendLog("REM, GEN - A fatal error occured during expander state refresh. The operation has been aborted");
                        DebugWriter.AppendLog(ex.Message);
                    }
                }
            });
        }

        private void Refresh()
        {
            /*
            DebugWriter.AppendLog("REM, GEN - Begin Refresh");

            ExpanderCommandHandle[] cmdhdls;
            ExpanderCommand[] expcmds = new ExpanderCommand[5];
            expcmds[0] = new ExpanderCommand(ExpanderCommand.READ_EXPANDERS_COMMAND);
            expcmds[1] = new ExpanderCommand(ExpanderCommand.READ_FAN_SPEED_COMMAND);
            expcmds[2] = new ExpanderCommand(ExpanderCommand.READ_POWER_MONITOR_COMMAND);
            expcmds[3] = new ExpanderCommand(ExpanderCommand.READ_TEMPERATURES_COMMAND);
            expcmds[4] = new ExpanderCommand(ExpanderCommand.READ_UPTIME_COMMAND);

            lock (expander_connection_lock)
                cmdhdls = expconn.EnqueueCommands(expcmds);

            DebugWriter.AppendLog("REM, GEN - Enqueued Commands");

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
            lock (hwconfig_lock)
                for (int index_counter = 0; index_counter < 16; index_counter++)
                {
                    new_state.Expander0State[index_counter] ^= exphwconfig.Expander0Configuration[index_counter];
                    new_state.Expander1State[index_counter] ^= exphwconfig.Expander1Configuration[index_counter];
                }
            DebugWriter.AppendLog("REM, GEN - Command 1 of 5 completed execution.");

            cmdhdls[1].Handle.WaitOne();
            buffer = new byte[2];
            Array.Copy(cmdhdls[1].Command.RxPacket, 1, buffer, 0, 2);
            new_state.FanSpeed = BitConverter.ToUInt16(buffer, 0);
            DebugWriter.AppendLog("REM, GEN - Command 2 of 5 completed execution.");

            cmdhdls[2].Handle.WaitOne();
            buffer = new byte[8];
            Array.Copy(cmdhdls[2].Command.RxPacket, 1, buffer, 0, 8);
            new_state.BusVoltage = BitConverter.ToSingle(buffer, 0);
            new_state.BusPower = BitConverter.ToSingle(buffer, 4);
            DebugWriter.AppendLog("REM, GEN - Command 3 of 5 completed execution.");

            cmdhdls[3].Handle.WaitOne();
            buffer = new byte[12];
            Array.Copy(cmdhdls[3].Command.RxPacket, 1, buffer, 0, 12);
            new_state.FanTemperature = BitConverter.ToSingle(buffer, 0);
            new_state.Board0Temperature = BitConverter.ToSingle(buffer, 4);
            new_state.Board1Temperature = BitConverter.ToSingle(buffer, 8);
            DebugWriter.AppendLog("REM, GEN - Command 4 of 5 completed execution.");

            cmdhdls[4].Handle.WaitOne();
            buffer = new byte[8];
            Array.Copy(cmdhdls[4].Command.RxPacket, 1, buffer, 0, 4);
            new_state.Uptime = BitConverter.ToUInt64(buffer, 0);
            DebugWriter.AppendLog("REM, GEN - Command 5 of 5 completed execution.");

            lock (expander_state_lock)
                expst = new ExpanderState(new_state);

            DebugWriter.AppendLog("REM, GEN - State Updated");

            if (freshstateevent != null)
                freshstateevent(this, new ControllerEventArgs(new_state));

            DebugWriter.AppendLog("REM, GEN - Event Generated");

            DebugWriter.AppendLog("REM, GEN - End Refresh");
            */
        }

        public override string ToString()
        {
            if (expconn != null)
                return "Expander (Active) " + hi.TCPConnectionProperties.AddressString + ":" + hi.TCPConnectionProperties.Port;
            else
                return "Expander (Inactive) " + hi.TCPConnectionProperties.AddressString + ":" + hi.TCPConnectionProperties.Port;
        }

        public void AddEvent(RemoteControllerEvent expev)
        {
            lock (expander_event_lock)
                expevents.Add(expev);
        }

        public List<RemoteControllerEvent> Events
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
