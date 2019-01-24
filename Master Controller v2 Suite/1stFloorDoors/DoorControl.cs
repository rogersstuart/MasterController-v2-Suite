using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading;
using System.Net.NetworkInformation;
using System.Net;

namespace DoorControl
{
    public partial class DoorControl : Form
    {
        private HttpClient client = null;
        private Button[] buttons;

        private Task polling_task;
        private volatile bool enable_polling = true;

        private volatile bool failure = false;
        private volatile bool retry_now = false;

        private volatile int error_counter = 0;
        private volatile int success_counter = 0;

        private Random r = new Random();

        public DoorControl()
        {
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

            InitializeComponent();

            buttons = new Button[]{button1, button2};

            Task.Run(() =>
            {
                while (!IsHandleCreated) ;

                polling_task = CreatePollingTask();
                polling_task.Start();
            });
        }

        private void Init()
        {
            enable_polling = true;
            error_counter = 0;
            success_counter = 0;
            retry_now = false;

            Invoke((MethodInvoker)(() =>
            { 
                checkBox1.Checked = true;
                numericUpDown1.Value = 10;
                foreach (Control c in Controls)
                    c.Enabled = true;
            }));

            if (client != null)
            {
                client.CancelPendingRequests();
                client.Dispose();
            }

            client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(4000);
        }

        private Task CreatePollingTask()
        {
            Init();

            return new Task(async () =>
            {
                while (enable_polling)
                {
                        if (!failure)
                        {
                            await (Task)Invoke(new Func<Task>(async () =>
                            {
                                if (!failure)
                                {
                                    await Task.Delay(250 + r.Next(751));

                                    await UpdateButtonText(0);

                                    if (!failure)
                                    {
                                        await Task.Delay(250 + r.Next(751));

                                        await UpdateButtonText(1);
                                    }
                                }
                            }));
                        }


                        if (failure)
                        {
                            client.CancelPendingRequests();

                            //check to make sure there is an active network connection
                            if (!IsNetworkAvailable())
                                AwaitNetworkConnection();

                            if (error_counter == 1)
                            {
                                error_counter = 0;
                                success_counter = 0;

                                Invoke(new Action(() =>
                                {
                                    Text = "Disconnected";

                                    foreach (Button b in buttons)
                                    {
                                        b.Text = "Unknown";
                                        b.BackColor = Color.LightGray;
                                    }

                                    button3.BackColor = Color.Red;
                                }));

                                retry_now = false;

                                int num_cycles = 64 + r.Next(193);
                                for (int sleep_counter = 1; (sleep_counter <= num_cycles) && enable_polling && !retry_now; sleep_counter++)
                                {
                                    Invoke(new Action(() => Text = "Disconnected - Retry in " + Math.Round((num_cycles - sleep_counter) / 4.0) + " second(s)"));

                                    await Task.Delay(250);
                                }

                                Invoke(new Action(() => Text = "Connecting"));
                            }
                            else
                            {
                                error_counter++;

                                Invoke(new Action(() =>
                                {
                                    if (success_counter > 0)
                                        Text = "Stale State";
                                    else
                                        Text = "Connecting";

                                    //string dots = " ";
                                    //for (int dot_counter = 0; dot_counter < error_counter; dot_counter++)
                                    //    dots += ".";

                                    //Text += dots;

                                    button3.BackColor = Color.LightSalmon;
                                }));

                                for (int sleep_counter = 0; (sleep_counter < (16 + r.Next(49))) && enable_polling; sleep_counter++)
                                    await Task.Delay(250);
                            }

                            failure = false;
                        }
                        else
                        {
                            Invoke(new Action(() => Text = "Connected"));

                            error_counter = 0;
                            success_counter = 1;

                            //for (int sleep_counter = 0; (sleep_counter < 2) && enable_polling; sleep_counter++)
                            //    await Task.Delay(250);
                        }
                    
                }

                client.CancelPendingRequests();
                client.Dispose();
                client = null;

                //disable and/or blank controls
                Invoke((MethodInvoker)(() => 
                {
                    foreach (Control c in Controls)
                        c.Enabled = false;
                    Text = "";
                    foreach (Button b in buttons)
                    {
                        b.Text = "";
                        b.BackColor = Color.LightGray;
                    }
                }));
            });
        }

        private void AwaitNetworkConnection()
        {
            enable_polling = false;

            Task.Run(async () =>
            {
                ProgressDialog pgd = new ProgressDialog("");

                Invoke(new Action(() =>
                {
                    pgd.Show(this);
                    pgd.Location = new Point(Location.X + Width / 2 - pgd.Width/2, Location.Y + Height / 2 - pgd.Height/2);
                    //Visible = false;
                }));

                pgd.LabelText = "Waiting for Network Connection";
                pgd.SetMarqueeStyle();

                await polling_task;

                while (!IsNetworkAvailable()) ;
                    //await Task.Delay(1000);

                Invoke(new Action(() =>
                {
                    pgd.Dispose();
                    //Visible = true;
                }));

                
                polling_task = CreatePollingTask();
                polling_task.Start();

            });
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (!failure)
            {
                Cursor.Current = Cursors.WaitCursor;

                await HandleToggle(0);

                if(!failure)
                    if (checkBox1.Checked && numericUpDown1.Value > 0)
                    {
                        var timer = new System.Windows.Forms.Timer();
                        timer.Interval = Convert.ToInt32(numericUpDown1.Value) * 1000;
                        timer.Tick += async (o, a) =>
                            {
                                timer.Stop();
                                await HandleToggle(0);
                            };
                        timer.Start();
                    }

                Cursor.Current = Cursors.Default;
            }
        }

        private async Task HandleToggle(int output_number)
        {
            if (!failure)
            {
                try
                {
                    button3.BackColor = Color.LightBlue;

                    string state = await client.GetStringAsync("http://192.168.0.17/get" + output_number);
                    //await Task.Delay(75 + r.Next(151));

                    if (state == "0")
                        await client.GetAsync("http://192.168.0.17/set" + output_number + "on");
                    else
                        if (state == "1")
                            await client.GetAsync("http://192.168.0.17/set" + output_number + "off");

                    //await Task.Delay(75 + r.Next(151));
                    await UpdateButtonText(output_number);
                }
                catch(Exception e)
                {
                    failure = true;
                }
            }
        }

        private async Task UpdateButtonText(int output_number)
        {
            if (!failure)
            {
                try
                {
                    button3.BackColor = Color.LightGreen;

                    string state = await client.GetStringAsync("http://192.168.0.17/get" + output_number);
                    if (state == "0")
                    {
                        buttons[output_number].Text = "Locked";
                        buttons[output_number].BackColor = Color.Red;
                    }
                    else
                        if (state == "1")
                        {
                            buttons[output_number].Text = "Unlocked";
                            buttons[output_number].BackColor = Color.Green;
                        }

                    button3.BackColor = Color.LightGray;
                }
                catch(Exception e)
                {
                    failure = true;
                }
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            if (!failure)
            {
                Cursor.Current = Cursors.WaitCursor;

                await HandleToggle(1);

                if(!failure)
                    if (checkBox1.Checked && numericUpDown1.Value > 0)
                    {
                        var timer = new System.Windows.Forms.Timer();
                        timer.Interval = Convert.ToInt32(numericUpDown1.Value) * 1000;
                        timer.Tick += async (o, a) =>
                        {
                            timer.Stop();
                            await HandleToggle(1);
                        };
                        timer.Start();
                    }

                Cursor.Current = Cursors.Default;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                numericUpDown1.Enabled = true;
            else
                numericUpDown1.Enabled = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UseWaitCursor = true;
            Refresh();

            e.Cancel = true;

            new Task(new Action(async () =>
            {
                enable_polling = false;
                await polling_task;

                await Task.Delay(5000);
                
                Invoke(new Action(() => Dispose()));

             })).Start();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            if(true)
            //if (failure || error_counter > 0)
            {
                var result = MessageBox.Show(this, "Launch Connection Repair?", "", MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.Yes)
                {
                    enable_polling = false;

                    await ConnectionDiagnostic();

                    await polling_task;
                    polling_task = CreatePollingTask();
                    polling_task.Start();
                }
                else
                    if (result == DialogResult.No)
                        retry_now = true;
            }
        }

        private async Task ConnectionDiagnostic()
        {
            ProgressDialog pgd = new ProgressDialog("Connection Repair");

            Invoke(new Action(() =>
            {
                pgd.Show(this);
                pgd.Location = new Point(Location.X + Width / 2 - pgd.Width / 2, Location.Y + Height / 2 - pgd.Height / 2);
                //Visible = false;
            }));

            pgd.LabelText = "Querying Controller";
            pgd.SetMarqueeStyle();

            bool isConnected = false;
            bool failure_flag = false;

            await Task.Run(() => { isConnected = UnifiUtilities.IsDoorControllerConnected(); });

            if (isConnected)
            {
                pgd.LabelText = "Forcing Device Reconnection";

                try
                {
                    await Task.Run(() => { UnifiUtilities.ReconnectDoorController(); }); 
                }
                catch(Exception ex)
                {
                    failure_flag = true;
                }
            }

            Invoke(new Action(() =>
            {
                pgd.Dispose();
                //Visible = true;
            }));

            if (failure_flag)
                MessageBox.Show(this, "The device appears to be online but an attempt to restore its connection state failed. \n " +
                    "To resore system functionality manually power cycle the unit.");
            else
                if (!isConnected)
                MessageBox.Show(this, "The device is offline. To restore system functionality manually power cycle the unit.");
            else
                MessageBox.Show(this, "The connection repair operation was successful.");
        }

        private bool IsNetworkAvailable(long minimumSpeed = 1000000)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // discard because of standard reasons
                if ((ni.OperationalStatus != OperationalStatus.Up) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
                    continue;

                // this allow to filter modems, serial, etc.
                // I use 10000000 as a minimum speed for most cases
                if (ni.Speed < minimumSpeed)
                    continue;

                // discard virtual cards (virtual box, virtual pc, etc.)
                if ((ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

                // discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
                if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
                    continue;

                return true;
            }
            return false;
        }
    }
}
