using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCICommon;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MessageServer
{
    class Program
    {
        private static RemoteExpanderMonitor rem;

        private static Object tmr_mod_lock = new Object();
        private static System.Timers.Timer write_timer = new System.Timers.Timer(250);

        private static Object exp_mod_lock = new Object();
        private static bool[] exp_0_mask = new bool[16];
        private static bool[] exp_1_mask = new bool[16];
        private static bool[] exp_0_values = new bool[16];
        private static bool[] exp_1_values = new bool[16];

        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                DeviceServerTracker.Start();
                while (DeviceServerTracker.DeviceServerHostInfo == null)
                    await Task.Delay(100);

                //var client = new uPLibrary.Networking.M2Mqtt.MqttClient("127.0.0.1");
                //client.Connect(Guid.NewGuid().ToString(), "USER", "PASS");

                var client = new MqttClient("192.168.0.31");
                
                //DeviceServerTracker.Start();

                //while (DeviceServerTracker.DeviceServerHostInfo == null)
                //    await Task.Delay(100);

                rem = new RemoteExpanderMonitor(8230500780121598431);

                write_timer.AutoReset = false;
                write_timer.Elapsed += (a, b) =>
                {
                    write_timer.Stop();
                    Task.Run(() =>
                    {
                        lock (exp_mod_lock)
                        {
                            rem.WriteExpanders(exp_0_mask, exp_1_mask, exp_0_values, exp_1_values);

                            Parallel.For(0, 16, i =>
                            {
                                exp_0_mask[i] = false;
                                exp_1_mask[i] = false;
                                exp_0_values[i] = false;
                                exp_1_values[i] = false;
                            });
                        }
                    });
                    
                };

                rem.freshstateevent += (a, b) =>
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            //floor states
                            //Console.WriteLine("Publishing Message");
                            for (int car_val = 1; car_val < 3; car_val++)
                                for (int floor_val = 1; floor_val < 15; floor_val++)
                                    client.Publish("access_control/elevator/car_" + car_val + "/floor_" + floor_val + "/get",
                                                                            new byte[] {(car_val == 1 ?
                                                                        b.ExpanderState.Expander0State[floor_val-1] : b.ExpanderState.Expander1State[floor_val - 1]) ?
                                                                        (byte)'1' : (byte)'0' });

                            //expander 0 temp
                            client.Publish("access_control/elevator/expander/operational_status/expander_0_temperature/get", Encoding.ASCII.GetBytes(b.ExpanderState.Board0Temperature + ""));
                            //expander 1 temp
                            client.Publish("access_control/elevator/expander/operational_status/expander_1_temperature/get", Encoding.ASCII.GetBytes(b.ExpanderState.Board1Temperature + ""));

                            //fan temp
                            client.Publish("access_control/elevator/expander/operational_status/fan_temperature/get", Encoding.ASCII.GetBytes(b.ExpanderState.FanTemperature + ""));
                            //fan speed
                            client.Publish("access_control/elevator/expander/operational_status/fan_speed/get", Encoding.ASCII.GetBytes(b.ExpanderState.FanSpeed + ""));

                            //bus power
                            client.Publish("access_control/elevator/expander/operational_status/bus_power/get", Encoding.ASCII.GetBytes(b.ExpanderState.BusPower + ""));
                            //bus voltage
                            client.Publish("access_control/elevator/expander/operational_status/bus_voltage/get", Encoding.ASCII.GetBytes(b.ExpanderState.BusVoltage + ""));

                            //uptime
                            client.Publish("access_control/elevator/expander/operational_status/uptime/get", Encoding.ASCII.GetBytes(b.ExpanderState.Uptime + ""));


                            //expander state timestamp
                            client.Publish("access_control/elevator/expander/operational_status/last_state_timestamp/get", Encoding.ASCII.GetBytes(b.ExpanderState.Timestamp.ToString()));
                        }
                        catch (Exception ex)
                        {
                            DebugWriter.AppendLog("MessageServer - An error occured while publishing status message.");
                            DebugWriter.AppendLog(ex.Message);
                        }

                    });


                    //client.Disconnect();
                };


                List<string> topics = new List<string>();
                for (int car_val = 1; car_val < 3; car_val++)
                    for (int floor_val = 1; floor_val < 15; floor_val++)
                        topics.Add("access_control/elevator/car_" + car_val + "/floor_" + floor_val + "/set");

                foreach (var topic in topics)
                    client.Subscribe(new string[] { topic }, new byte[] { 0 });

                client.MqttMsgPublishReceived += (a, b) =>
                {
                    Task.Run(() =>
                    {
                        if (topics.Contains(b.Topic))
                        {
                            var substrs = b.Topic.Split('/');

                            int car_val = Convert.ToInt32(substrs[2].Split('_')[1]) - 1;
                            int floor_val = Convert.ToInt32(substrs[3].Split('_')[1]) - 1;

                            RelaySet(b.Message[0] == '1', car_val, floor_val);
                        }
                    });
                };

                client.Connect(Guid.NewGuid().ToString());

                Console.WriteLine("Starting Remote Monitor");

                rem.Start();

                Console.WriteLine("Remote Monitor Started");
            });

            Thread.Sleep(-1);
        }

        private static void RelaySet(bool val, int expander_num, int relay_index)
        {
            try
            {
                ExpanderState expst = rem.ExpanderState;
                ExpanderState base_state = rem.DefaultState;

                bool[] mask = new bool[16];
                mask[relay_index] = true;

                if (expander_num == 0)
                {
                    base_state.Expander0State[relay_index] = val;// !expst.Expander0State[relay_index];
                    expst.Expander0State[relay_index] = val;//!expst.Expander0State[relay_index];

                    rem.DefaultState = base_state;

                    //rem.WriteExpanders(mask, new bool[16], expst.Expander0State, expst.Expander1State);

                    lock (exp_mod_lock)
                    {
                        Parallel.For(0, 16, i =>
                        {
                            exp_0_mask[i] |= mask[i];
                            exp_1_mask[i] |= false;
                            exp_0_values[i] |= expst.Expander0State[i];
                            exp_1_values[i] |= expst.Expander1State[i];
                        });
                    }
                }
                else
                    if (expander_num == 1)
                {
                    base_state.Expander1State[relay_index] = val;//!expst.Expander1State[relay_index];
                    expst.Expander1State[relay_index] = val;//!expst.Expander1State[relay_index];

                    rem.DefaultState = base_state;

                    //rem.WriteExpanders(new bool[16], mask, expst.Expander0State, expst.Expander1State);

                    lock (exp_mod_lock)
                    {
                        Parallel.For(0, 16, i =>
                        {
                            exp_0_mask[i] |= false;
                            exp_1_mask[i] |= mask[i];
                            exp_0_values[i] |= expst.Expander0State[i];
                            exp_1_values[i] |= expst.Expander1State[i];
                        });
                    }

                    write_timer.Start();
                }
            }
            catch (Exception ex)
            {
                DebugWriter.AppendLog("A fatal error occured while writing to the expanders. The operation was aborted.");
                DebugWriter.AppendLog(ex.Message);
            }
        }
    }
}
