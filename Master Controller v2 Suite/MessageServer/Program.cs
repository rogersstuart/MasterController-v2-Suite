using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCICommon;
using System.Threading;
using MQTTnet.Client;
using MQTTnet;
using MQTTnet.Client.Options;
using GlobalUtilities;

namespace MessageServer
{
    class Program
    {
        private static FileTextLogger logger = new FileTextLogger();
        
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

                // Create a new MQTT client.
                var factory = new MqttFactory();
                var mqttClient = factory.CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                .WithTcpServer("192.168.0.31")
                .WithTls()
                .Build();

                //handle disconnection
                mqttClient.UseDisconnectedHandler(async e =>
                {
                    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    try
                    {
                        await mqttClient.ConnectAsync(options, CancellationToken.None); // Since 3.0.5 with CancellationToken
                    }
                    catch
                    {
                        Console.WriteLine("### RECONNECTING FAILED ###");
                    }
                });

                //connect to expander
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
                    Task.Run(async () =>
                    {
                        try
                        {
                            MqttApplicationMessage message;

                            //floor states
                            //Console.WriteLine("Publishing Message");
                            for (int car_val = 1; car_val < 3; car_val++)
                                for (int floor_val = 1; floor_val < 15; floor_val++)
                                {
                                    message = new MqttApplicationMessageBuilder()
                                    .WithTopic("access_control/elevator/car_" + car_val + "/floor_" + floor_val + "/get")
                                    .WithPayload(new byte[] {(car_val == 1 ? b.ExpanderState.Expander0State[floor_val-1] : b.ExpanderState.Expander1State[floor_val - 1]) ? (byte)'1' : (byte)'0' })
                                    .WithExactlyOnceQoS()
                                    .WithRetainFlag()
                                    .Build();

                                    await mqttClient.PublishAsync(message);
                                }

                            Queue<mqtt_container> mqtt_tx = new Queue<mqtt_container>();

                            //expander 0 temp
                            mqtt_tx.Enqueue(new mqtt_container
                            {
                                c_topic = "access_control/elevator/expander/operational_status/expander_0_temperature/get",
                                c_payload = Encoding.ASCII.GetBytes(b.ExpanderState.Board0Temperature + "")
                            });

                            //expander 1 temp
                            mqtt_tx.Enqueue(new mqtt_container
                            {
                                c_topic = "access_control/elevator/expander/operational_status/expander_1_temperature/get",
                                c_payload = Encoding.ASCII.GetBytes(b.ExpanderState.Board1Temperature + "")
                            });

                            //fan temp
                            mqtt_tx.Enqueue(new mqtt_container
                            {
                                c_topic = "access_control/elevator/expander/operational_status/fan_temperature/get",
                                c_payload = Encoding.ASCII.GetBytes(b.ExpanderState.FanTemperature + "")
                            });

                            //fan speed
                            mqtt_tx.Enqueue(new mqtt_container
                            {
                                c_topic = "access_control/elevator/expander/operational_status/fan_speed/get",
                                c_payload = Encoding.ASCII.GetBytes(b.ExpanderState.FanSpeed + "")
                            });

                            //bus power
                            mqtt_tx.Enqueue(new mqtt_container
                            {
                                c_topic = "access_control/elevator/expander/operational_status/bus_power/get",
                                c_payload = Encoding.ASCII.GetBytes(b.ExpanderState.BusPower + "")
                            });

                            //bus voltage
                            mqtt_tx.Enqueue(new mqtt_container
                            {
                                c_topic = "access_control/elevator/expander/operational_status/bus_voltage/get",
                                c_payload = Encoding.ASCII.GetBytes(b.ExpanderState.BusVoltage + "")
                            });

                            //uptime
                            mqtt_tx.Enqueue(new mqtt_container
                            {
                                c_topic = "access_control/elevator/expander/operational_status/uptime/get",
                                c_payload = Encoding.ASCII.GetBytes(b.ExpanderState.Uptime + "")
                            });

                            //expander state timestamp
                            mqtt_tx.Enqueue(new mqtt_container
                            {
                                c_topic = "access_control/elevator/expander/operational_status/last_state_timestamp/get",
                                c_payload = Encoding.ASCII.GetBytes(b.ExpanderState.Timestamp.ToString())
                            });

                            foreach(mqtt_container c in mqtt_tx)
                            {
                                message = new MqttApplicationMessageBuilder()
                                .WithTopic(c.c_topic)
                                .WithPayload(c.c_payload)
                                .WithExactlyOnceQoS()
                                .WithRetainFlag()
                                .Build();

                                await mqttClient.PublishAsync(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.AppendLog("MessageServer - An error occured while publishing status message.");
                            logger.AppendLog(ex.Message);
                        }
                    });
                };

                List<string> topics = new List<string>();
                for (int car_val = 1; car_val < 3; car_val++)
                    for (int floor_val = 1; floor_val < 15; floor_val++)
                        topics.Add("access_control/elevator/car_" + car_val + "/floor_" + floor_val + "/set");

                //subscribe to topics
                mqttClient.UseConnectedHandler(async e =>
                {
                    foreach (var topic in topics)
                        await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());
                });

                //received message handler
                mqttClient.UseApplicationMessageReceivedHandler(e =>
                {
                    Task.Run(() =>
                    {
                        if (topics.Contains(e.ApplicationMessage.Topic))
                        {
                            var substrs = e.ApplicationMessage.Topic.Split('/');

                            int car_val = Convert.ToInt32(substrs[2].Split('_')[1]) - 1;
                            int floor_val = Convert.ToInt32(substrs[3].Split('_')[1]) - 1;

                            RelaySet(e.ApplicationMessage.Payload[0] == '1', car_val, floor_val);
                        }
                    });

                    /*
                    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                    Console.WriteLine();
                    */
                });

                await mqttClient.ConnectAsync(options, CancellationToken.None);

                Console.WriteLine("Starting Remote Monitor");

                rem.Start();

                Console.WriteLine("Remote Monitor Started");
            });

            Thread.Sleep(-1);
        }

        struct mqtt_container
        {
            internal string c_topic;
            internal byte[] c_payload;

            internal mqtt_container(string c_topic, byte[] c_payload)
            {
                this.c_topic = c_topic;
                this.c_payload = c_payload;
            }
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
                logger.AppendLog("A fatal error occured while writing to the expanders. The operation was aborted.");
                logger.AppendLog(ex.Message);
            }
        }
    }
}
