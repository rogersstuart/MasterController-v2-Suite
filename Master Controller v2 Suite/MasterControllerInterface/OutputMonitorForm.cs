using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using MCICommon;

namespace MasterControllerInterface
{
    public partial class OutputMonitorForm : Form
    {
        private bool[] lock_override_flags = new bool[2];

        private System.Timers.Timer polling_timer = null;


        private Dictionary<DateTime, double> polling_op_durations = new Dictionary<DateTime, double>();

        private volatile bool comm_busy = false;
        private volatile bool pending_override_change = false;

        private int last_ext0_state = 1;
        private DateTime ext0_active_motion_event_start;

        private Dictionary<DateTime, TimeSpan> ext0_motion_events = new Dictionary<DateTime, TimeSpan>();

        private int last_ext1_state = 1;
        private DateTime ext1_active_motion_event_start;

        private Dictionary<DateTime, TimeSpan> ext1_motion_events = new Dictionary<DateTime, TimeSpan>();

        public OutputMonitorForm()
        {
            InitializeComponent();

            label6.Text = "Init";

            GenerateTimer((double)numericUpDown1.Value);
        }

        private void GenerateTimer(double interval)
        {
            if (polling_timer != null)
            {
                polling_timer.Stop();
                polling_timer.Dispose();
                polling_timer = null;
            }

            polling_timer = new System.Timers.Timer(interval);
            polling_timer.AutoReset = true;
            polling_timer.Elapsed += async (sender, e) => await SyncStates();

            polling_timer.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //main entrance button
            lock_override_flags[0] = !lock_override_flags[0];
            pending_override_change = true;

            button1.Text += " *";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //side entrance button
            lock_override_flags[1] = !lock_override_flags[1];
            pending_override_change = true;

            button2.Text += " *";
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            //polling interval

            GenerateTimer((double)numericUpDown1.Value);
        }

        int missed = 0;

        private async Task SyncStates()
        {
            //get door control outputs command 'Z'
            if (comm_busy)
            {
                missed++;
                return;
            }
            else
            {
                missed = 0;
                comm_busy = true;
            }

            try
            {
                if (pending_override_change)
                {
                    await SetOverrides();
                    pending_override_change = false;
                }

                Stopwatch polling_duration_stopwatch = Stopwatch.StartNew();

                (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface
                (await ManagedStream.GetStream()).WriteByte((byte)'Z');

                int response = (await ManagedStream.GetStream()).ReadByte();

                if (response == -1)
                    throw new Exception("No Response");
                else
                {
                    
                        int ext0 = response & 1;

                        if(ext0 == 0 && ext0 != last_ext0_state)
                        {
                            //a new motion event has began
                            last_ext0_state = ext0;
                            ext0_active_motion_event_start = DateTime.Now;
                        }
                        else
                            if (ext0 == 1 && ext0 != last_ext0_state)
                            {
                                last_ext0_state = ext0;
                                ext0_motion_events.Add(ext0_active_motion_event_start, DateTime.Now-ext0_active_motion_event_start);
                            }

                    int ext1 = (response >> 1) & 1;

                    if (ext1 == 0 && ext1 != last_ext1_state)
                    {
                        //a new motion event has began
                        last_ext1_state = ext1;
                        ext1_active_motion_event_start = DateTime.Now;
                    }
                    else
                        if (ext1 == 1 && ext1 != last_ext1_state)
                    {
                        last_ext1_state = ext1;
                        ext1_motion_events.Add(ext1_active_motion_event_start, DateTime.Now - ext1_active_motion_event_start);
                    }

                    Func<Dictionary<DateTime, TimeSpan>, Task<string>> motion_stats_gen = new Func<Dictionary<DateTime, TimeSpan>, Task<string>>((dict) =>
                    {
                        return Task.Run<string>(() =>
                        {

                            string motion_stats = "";

                            TimeSpan record_span = dict.Last().Key - dict.First().Key;

                            if (record_span >= TimeSpan.FromMinutes(1))
                            {
                                double seconds_of_motion_during_last_minute = dict.Where(x => x.Key >= DateTime.Now - TimeSpan.FromMinutes(1)).Select(x => x.Value.TotalMilliseconds).Sum();
                                if (seconds_of_motion_during_last_minute > 0)
                                {
                                    seconds_of_motion_during_last_minute /= 1000;
                                    motion_stats += "1m:" + Math.Round((seconds_of_motion_during_last_minute / (60)) * 100, 2) + "%,";
                                }
                            }

                            double seconds_of_motion_during_last_five_minutes = dict.Where(x => x.Key >= DateTime.Now - TimeSpan.FromMinutes(5)).Select(x => x.Value.TotalMilliseconds).Sum();
                            if (seconds_of_motion_during_last_five_minutes > 0)
                            {
                                seconds_of_motion_during_last_five_minutes /= 1000;
                                motion_stats += "5m:" + Math.Round((seconds_of_motion_during_last_five_minutes / (60 * 5)) * 100, 2) + "%,";
                            }

                            double seconds_of_motion_during_last_ten_minutes = dict.Where(x => x.Key >= DateTime.Now - TimeSpan.FromMinutes(10)).Select(x => x.Value.TotalMilliseconds).Sum();
                            if (seconds_of_motion_during_last_ten_minutes > 0)
                            {
                                seconds_of_motion_during_last_ten_minutes /= 1000;
                                motion_stats += "10m:" + Math.Round((seconds_of_motion_during_last_ten_minutes / (60 * 10)) * 100, 2) + "%,";
                            }

                            double seconds_of_motion_during_last_thirty_minutes = dict.Where(x => x.Key >= DateTime.Now - TimeSpan.FromMinutes(30)).Select(x => x.Value.TotalMilliseconds).Sum();
                            if (seconds_of_motion_during_last_thirty_minutes > 0)
                            {
                                seconds_of_motion_during_last_thirty_minutes /= 1000;
                                motion_stats += "30m:" + Math.Round((seconds_of_motion_during_last_thirty_minutes / (60 * 30)) * 100, 2) + "%,";
                            }

                            double seconds_of_motion_during_last_hour = dict.Where(x => x.Key >= DateTime.Now - TimeSpan.FromMinutes(60)).Select(x => x.Value.TotalMilliseconds).Sum();
                            if (seconds_of_motion_during_last_hour > 0)
                            {
                                seconds_of_motion_during_last_hour /= 1000;
                                motion_stats += "1h:" + Math.Round((seconds_of_motion_during_last_hour / (60 * 60)) * 100, 2) + "%,";
                            }

                            double seconds_of_motion_during_last_day = dict.Where(x => x.Key >= DateTime.Now - TimeSpan.FromHours(24)).Select(x => x.Value.TotalMilliseconds).Sum();
                            if (seconds_of_motion_during_last_day > 0)
                            {
                                seconds_of_motion_during_last_day /= 1000;
                                motion_stats += "1d:" + Math.Round((seconds_of_motion_during_last_day / (60 * 60 * 24)) * 100, 2) + "%";
                            }

                            if (motion_stats.Last() == ',')
                                motion_stats = motion_stats.Substring(0, motion_stats.Length-1);

                            return motion_stats;
                        });
                        });

                    Invoke((MethodInvoker)(async () =>
                    {
                        label4.Text = ext0_motion_events.Count() > 0 ? await motion_stats_gen(ext0_motion_events) : "No Motion Records";
                        label5.Text = ext1_motion_events.Count() > 0 ? await motion_stats_gen(ext1_motion_events): "No Motion Records";
                    }));

                        

                    if (!pending_override_change)
                    {
                        lock_override_flags[0] = Convert.ToBoolean((response >> 2) & 1);
                        lock_override_flags[1] = Convert.ToBoolean((response >> 3) & 1);
                    }

                    Invoke((MethodInvoker)(() =>
                    {
                        if (ext0 == 1 && !lock_override_flags[0] && !TimeCheck())
                        {
                            button1.Text = "Locked";
                            button1.BackColor = Color.Red;
                        }
                        else
                        {
                            button1.Text = "Unlocked (";
                            if (ext0 == 0)
                                button1.Text += "Motion, ";
                            if (lock_override_flags[0])
                                button1.Text += "Override, ";
                            if (TimeCheck())
                                button1.Text += "Time, ";

                            if (button1.Text.Last() == ' ')
                                button1.Text = button1.Text.Substring(0, button1.Text.Length - 2) + ")";
                            else
                                if (button1.Text.Last() == '(')
                                    button1.Text = button1.Text.Substring(0, button1.Text.Length - 2);

                            button1.BackColor = Color.Green;
                        }

                        if (ext1 == 1 && !lock_override_flags[1] && !TimeCheck())
                        {
                            button2.Text = "Locked";
                            button2.BackColor = Color.Red;
                        }
                        else
                        {
                            button2.Text = "Unlocked (";
                            if (ext1 == 0)
                                button2.Text += "Motion, ";
                            if (lock_override_flags[1])
                                button2.Text += "Override, ";
                            if (TimeCheck())
                                button2.Text += "Time, ";

                            if (button2.Text.Last() == ' ')
                                button2.Text = button2.Text.Substring(0, button2.Text.Length - 2) + ")";
                            else
                                if (button2.Text.Last() == '(')
                                button2.Text = button2.Text.Substring(0, button2.Text.Length - 2);

                            button2.BackColor = Color.Green;
                        }
                    }));
                }

                polling_duration_stopwatch.Stop();

                DateTime now = DateTime.Now;

                polling_op_durations.Add(now, (double)polling_duration_stopwatch.Elapsed.Ticks/10000);

                polling_op_durations = polling_op_durations.Where(x => x.Key >= (now-TimeSpan.FromMinutes(1))).ToDictionary(x => x.Key, x => x.Value); //filter the records based on time

                double average_polling_op_duration = polling_op_durations.Select(x => x.Value).Average();
                double max_polling_op_duration = polling_op_durations.Select(x => x.Value).Max();
                double min_polling_op_duration = polling_op_durations.Select(x => x.Value).Min();

                Invoke((MethodInvoker) (() =>
                {
                    label6.Text = "Operation Execution Time (ms): {" + Math.Round(max_polling_op_duration, 4) + ", " + Math.Round(min_polling_op_duration, 4) + ", " + Math.Round(average_polling_op_duration, 4) + "}, "
                        + "RPS: {" + Math.Round(1000 / max_polling_op_duration, 4) + ", " + Math.Round(1000 / min_polling_op_duration, 4) + ", " + Math.Round(1000 / average_polling_op_duration, 4) + "}, " + "Missed: " + missed;

                    if ((decimal)average_polling_op_duration > numericUpDown1.Value)
                        numericUpDown1.BackColor = Color.Red;
                    else
                        numericUpDown1.BackColor = Color.LightGreen;
                }));
            }
            catch(Exception ex)
            {
                Invoke((MethodInvoker)(() => { label6.Text += " *"; }));
            }

            comm_busy = false;
        }

        private bool TimeCheck()
        {
            if (DateTime.Now.DayOfWeek > DayOfWeek.Sunday && DateTime.Now.DayOfWeek < DayOfWeek.Saturday)
                if (DateTime.Now.TimeOfDay >= new TimeSpan(7, 45, 0) && DateTime.Now.TimeOfDay <= new TimeSpan(17, 45, 0))
                    return true;

            return false;
        }

        private async Task SetOverrides()
        {
            //set door control outputs command 'A'
            int i = 0;
            for(; i < 3; i++)
            {
                try
                {
                    (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface
                    (await ManagedStream.GetStream()).WriteByte((byte)'A');

                    if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                        throw new Exception();

                    (await ManagedStream.GetStream()).WriteByte(Convert.ToByte((lock_override_flags[0] ? 1 : 0) | ((lock_override_flags[1] ? 1 : 0) << 1)));

                    if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'!') //read !
                        throw new Exception();

                    break;
                }
                catch (Exception ex)
                {

                }
            }

            if (i == 3)
                throw new Exception();
        }

        private void OutputMonitorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            UseWaitCursor = true;

            foreach (Control c in Controls)
                c.Enabled = false;

            e.Cancel = true;

            Task.Run(async () =>
            { 
                polling_timer.Stop();
                polling_timer.Dispose();

                await Task.Delay(5000);

                Invoke(new Action(() => Dispose()));
            });
        }
    }
}
