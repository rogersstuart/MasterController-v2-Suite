using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using MCICommon;
using uPLibrary.Networking.M2Mqtt;

namespace MasterControllerDotNet_Server
{
    public partial class ExpanderMonitorForm : Form
    {
        volatile bool moving_avg_flag = false;
        int elements_in_avg = 1;

        Queue<DateTime> record_time = new Queue<DateTime>();

        Queue<float> expander_0_temperature_queue = new Queue<float>();
        Queue<float> expander_1_temperature_queue = new Queue<float>();
        Queue<float> bus_voltage_queue = new Queue<float>();
        Queue<float> bus_power_queue = new Queue<float>();
        Queue<float> fan_temperature_queue = new Queue<float>();
        Queue<int> fan_speed_queue = new Queue<int>();

        private Object modLock = new Object();

        ApplyAverageToForm applyAverageToForm;

        Object expmon_lock = new Object();
        RemoteExpanderMonitor expmon;

        Button[] buttons;

        DateTime first_time, last_time;

        ExpanderState exp_sub;
        FloorStateTracker[] floor_tracker;

        public ExpanderMonitorForm(RemoteExpanderMonitor expmon = null)
        {
            InitializeComponent();

            buttons = new Button[]{button1, button2, button3, button4, button5, button6, button7, button8, button9, button10, button11, button12, button13, button14, button32, button30,
                          button28, button27, button26, button25, button24, button23, button22, button21, button20, button19, button18, button17, button16, button15, button34, button33};

            this.expmon = expmon;

            if(expmon == null)
            {
                button29.Enabled = false;

                exp_sub = new ExpanderState();

                floor_tracker = new FloorStateTracker[] { new FloorStateTracker(1), new FloorStateTracker(2)};

                floor_tracker[0].freshstateevent += a =>
                {
                    exp_sub.Expander0State[a.floor_number - 1] = a.state;

                    expmon_FreshStateAvailable(null, null);
                };

                floor_tracker[1].freshstateevent += a =>
                {
                    exp_sub.Expander1State[a.floor_number - 1] = a.state;

                    expmon_FreshStateAvailable(null, null);
                };
            }         
        }

        private void UpdateAverages(ExpanderState expst)
        {     
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();

                record_time.Enqueue(expst.Timestamp);
                while(record_time.Count() > elements_in_avg)
                    record_time.Dequeue();

                            //if enough values have been aquired to complete the average then do it
                            //if the requirement hasnt been met then continue without updating
                            float avg, exp0avg = 0, exp1avg = 0;

                            if (expander_0_temperature_queue.Count >= elements_in_avg)
                            {
                                foreach (float val in expander_0_temperature_queue)
                                    exp0avg += val;
                                exp0avg /= expander_0_temperature_queue.Count;

                                textBox1.Text = Math.Round(exp0avg, 3) + "°F";

                                while (expander_0_temperature_queue.Count >= elements_in_avg)
                                    expander_0_temperature_queue.Dequeue();
                            }
                            else
                            {
                                Text = elements_in_avg - expander_0_temperature_queue.Count + "";
                            }

                            expander_0_temperature_queue.Enqueue(expst.Board0Temperature);

                            ////

                            if (expander_1_temperature_queue.Count >= elements_in_avg)
                            {
                                foreach (float val in expander_1_temperature_queue)
                                    exp1avg += val;
                                exp1avg /= expander_1_temperature_queue.Count;

                                textBox2.Text = Math.Round(exp1avg, 3) + "°F";

                                while (expander_1_temperature_queue.Count >= elements_in_avg)
                                    expander_1_temperature_queue.Dequeue();
                            }

                            expander_1_temperature_queue.Enqueue(expst.Board1Temperature);

                            ////

                            if (bus_voltage_queue.Count >= elements_in_avg)
                            {
                                avg = 0;
                                foreach (float val in bus_voltage_queue)
                                    avg += val;
                                avg /= bus_voltage_queue.Count;

                                textBox3.Text = Math.Round(avg, 3) + "V";

                                while (bus_voltage_queue.Count >= elements_in_avg) //clear the excess
                                    bus_voltage_queue.Dequeue();
                            }

                            bus_voltage_queue.Enqueue(expst.BusVoltage);

                            //////

                            if (bus_power_queue.Count >= elements_in_avg)
                            {
                                avg = 0;
                                foreach (float val in bus_power_queue)
                                    avg += val;
                                avg /= bus_power_queue.Count;

                                textBox4.Text = Math.Round(avg, 3) + "W";

                                while (bus_power_queue.Count >= elements_in_avg) //clear the excess
                                    bus_power_queue.Dequeue();
                            }

                            bus_power_queue.Enqueue(expst.BusPower);

                            //////

                            if (fan_temperature_queue.Count >= elements_in_avg)
                            {
                                avg = 0;
                                foreach (float val in fan_temperature_queue)
                                    avg += val;
                                avg /= fan_temperature_queue.Count;

                                textBox5.Text = Math.Round(avg, 3) + "°F";

                                textBox7.Text = Math.Round(exp0avg - avg, 3) + "°F";
                                textBox8.Text = Math.Round(exp1avg - avg, 3) + "°F";

                                while (fan_temperature_queue.Count >= elements_in_avg) //clear the excess
                                    fan_temperature_queue.Dequeue();
                            }

                            fan_temperature_queue.Enqueue(expst.FanTemperature);

                            ////////////

                            if (fan_speed_queue.Count >= elements_in_avg)
                            {
                                avg = 0;
                                foreach (float val in fan_speed_queue)
                                    avg += val;
                                avg /= fan_speed_queue.Count;

                                avg = 780 - avg;
                                if (avg < 0)
                                    avg = 0;

                                progressBar1.Value = (int)Math.Round(avg);

                                if (avg > 0)
                                {
                                    avg /= 780;
                                    avg *= 100;
                                }

                                textBox6.Text = Math.Round(avg, 3) + "%";

                                while (fan_speed_queue.Count >= elements_in_avg) //clear the excess
                                    fan_speed_queue.Dequeue();
                            }

                            fan_speed_queue.Enqueue(expst.FanSpeed);
                        

                        if (applyAverageToForm != null)
                        {
                            if (applyAverageToForm.IsDisposed == false)
                                applyAverageToForm.RefreshData(expander_0_temperature_queue, expander_1_temperature_queue, bus_voltage_queue, bus_power_queue, fan_temperature_queue, fan_speed_queue);
                        }

                    stopwatch.Stop();
                    Console.WriteLine("gui " + stopwatch.ElapsedMilliseconds);
                    stopwatch.Reset();
        }

        private void UpdateButtonColors(ExpanderState expst)
        {
            for (int button_counter = 0; button_counter < buttons.Length/2; button_counter++)
            {
                if (expst.Expander0State[button_counter])
                    buttons[button_counter].BackColor = Color.Green;
                else
                    buttons[button_counter].BackColor = Color.Red;

                if (expst.Expander1State[button_counter])
                    buttons[button_counter + buttons.Length / 2].BackColor = Color.Green;
                else
                    buttons[button_counter + buttons.Length / 2].BackColor = Color.Red;
            } 
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 0);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 1);
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 2);
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 3);
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 4);
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 5);
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 6);
        }

        private async void button8_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 7);
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 8);
        }

        private async void button10_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 9);
        }

        private async void button11_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 10);
        }

        private async void button12_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 11);
        }

        private async void button13_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 12);
        }

        private async void button14_Click(object sender, EventArgs e)
        {
            await relayToggle(0, 13);
        }

        private async void button28_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 0);
        }

        private async void button27_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 1);
        }

        private async void button26_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 2);
        }

        private async void button25_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 3);
        }

        private async void button24_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 4);
        }

        private async void button23_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 5);
        }

        private async void button22_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 6);
        }

        private async void button21_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 7);
        }

        private async void button20_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 8);
        }

        private async void button19_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 9);
        }

        private async void button18_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 10);
        }

        private async void button17_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 11);
        }

        private async void button16_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 12);
        }

        private async void button15_Click(object sender, EventArgs e)
        {
            await relayToggle(1, 13);
        }

        private async void button29_Click(object sender, EventArgs e)
        {
            await Task.Run(delegate()
                {
                    lock (expmon_lock)
                        expmon.ResetExpander();
                });
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
                    if (checkBox1.Checked)
                    {
                        numericUpDown1.Enabled = true;
                        moving_avg_flag = true;
                    }
                    else
                    {
                        moving_avg_flag = false;
                        numericUpDown1.Enabled = false;

                        record_time.Clear();
                            expander_0_temperature_queue.Clear();
                            expander_1_temperature_queue.Clear();
                            bus_voltage_queue.Clear();
                            bus_power_queue.Clear();
                            fan_temperature_queue.Clear();
                            fan_speed_queue.Clear();
                    
                    }
        }

        private Task relayToggle(int expander_num, int relay_index)
        {
            return Task.Run(delegate()
            {
                if(expmon != null)
                {
                    //were directly issuing commands to the expander

                    lock (expmon_lock)
                    {
                        ExpanderState expst = expmon.ExpanderState;
                        ExpanderState base_state = expmon.DefaultState;

                        bool[] mask = new bool[16];
                        mask[relay_index] = true;

                        if (expander_num == 0)
                        {
                            base_state.Expander0State[relay_index] = !expst.Expander0State[relay_index];
                            expst.Expander0State[relay_index] = !expst.Expander0State[relay_index];

                            expmon.DefaultState = base_state;

                            expmon.WriteExpanders(mask, new bool[16], expst.Expander0State, expst.Expander1State);
                        }
                        else
                            if (expander_num == 1)
                        {
                            base_state.Expander1State[relay_index] = !expst.Expander1State[relay_index];
                            expst.Expander1State[relay_index] = !expst.Expander1State[relay_index];

                            expmon.DefaultState = base_state;

                            expmon.WriteExpanders(new bool[16], mask, expst.Expander0State, expst.Expander1State);
                        }
                    }
                }
                else
                {
                    //were using a mqtt relay

                    floor_tracker[expander_num].ToggleFloor(relay_index+1);
                }
                
            });
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            elements_in_avg = (int)numericUpDown1.Value;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (Control c in Controls)
                c.Enabled = false;

            UseWaitCursor = true;

            Refresh();

            e.Cancel = true;

            Task.Run(async () =>
            {
                if(expmon != null)
                    expmon.freshstateevent -= expmon_FreshStateAvailable;

                await Task.Delay(5000);

                Invoke(new Action(() => Dispose()));
            });
        }

        private void button31_Click(object sender, EventArgs e)
        {
            applyAverageToForm = new ApplyAverageToForm();
            applyAverageToForm.Show();
        }

        private void expmon_FreshStateAvailable(object sender, ExpanderEventArgs expevargs)
        {
            Invoke((MethodInvoker)(() =>
            {
                ExpanderState expst;

                if (expevargs != null)
                    expst = expevargs.ExpanderState;
                else
                    expst = exp_sub;


                UpdateButtonColors(expst);

                textBox9.Text = TimeSpan.FromMilliseconds(expst.Uptime).ToString();

                if (moving_avg_flag)
                {
                    UpdateAverages(expevargs.ExpanderState);

                    Text = (record_time.Last() - record_time.First()).ToString();

                    UInt64 base_ticks = (UInt64)record_time.First().Ticks;
                    UInt64 tick_avg = 0;
                    UInt64 last_tick = 0;
                    foreach (DateTime dt in record_time)
                    {
                        tick_avg += ((UInt64)dt.Ticks - base_ticks) - last_tick;
                        last_tick = ((UInt64)dt.Ticks - base_ticks);
                    }

                    tick_avg /= (UInt64)record_time.Count();
                    Console.WriteLine(record_time.Count());


                    Text += " Avg=" + TimeSpan.FromTicks((long)tick_avg).TotalMilliseconds;

                    if (record_time.Count() < elements_in_avg)
                        Text += " " + (elements_in_avg - record_time.Count()) + " " + TimeSpan.FromMilliseconds(TimeSpan.FromTicks((long)tick_avg).TotalMilliseconds * (elements_in_avg - record_time.Count())).ToString();
                }
                else
                {
                    textBox1.Text = Math.Round(expst.Board0Temperature, 3) + "°F";
                    textBox7.Text = Math.Round(expst.Board0Temperature - expst.FanTemperature, 3) + "°F";

                    textBox2.Text = Math.Round(expst.Board1Temperature, 3) + "°F";
                    textBox8.Text = Math.Round(expst.Board1Temperature - expst.FanTemperature, 3) + "°F";

                    textBox3.Text = Math.Round(expst.BusVoltage, 3) + "V";
                    textBox4.Text = Math.Round(expst.BusPower, 3) + "W";
                    textBox5.Text = Math.Round(expst.FanTemperature, 3) + "°F";

                    textBox6.Text = Math.Round(((783.0 - expst.FanSpeed) == 0 ? 0 : ((783.0 - expst.FanSpeed) / 783.0) * 100.0), 3) + " %";
                }
            }));
        }

        private void ExpanderMonitorForm_Shown(object sender, EventArgs e)
        {
            if(expmon != null)
                expmon.freshstateevent += expmon_FreshStateAvailable;
        }

        private async void button30_Click(object sender, EventArgs e)
        {
            //0-16
            await relayToggle(0, 15);
        }

        private async void button32_Click(object sender, EventArgs e)
        {
            //0-15
            await relayToggle(0, 14);
        }

        private async void button34_Click(object sender, EventArgs e)
        {
            //1-15
            await relayToggle(1, 14);
        }

        private async void button33_Click(object sender, EventArgs e)
        {
            //1-16
            await relayToggle(1, 15);
        }
    }
}
