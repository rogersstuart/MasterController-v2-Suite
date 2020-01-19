using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace PanelSim
{
    public partial class Form1 : Form
    {
        private static MqttClient client = new MqttClient("mccsrv1");
        private System.Timers.Timer car1timer = new System.Timers.Timer();
        private System.Timers.Timer car2timer = new System.Timers.Timer();

        private Color[] colors = {Color.Red, Color.Blue, Color.Green };

        public Form1()
        {
            InitializeComponent();

            textBox1.Text = "3449006277";
            button1.BackColor = Color.Gray;
            button2.BackColor = Color.Gray;

            car1timer.Interval = 4000;
            car2timer.Interval = 4000;

            car1timer.Elapsed += (a,b) =>
            {
                Invoke((MethodInvoker)(() =>
                {
                    button1.BackColor = Color.Gray;
                    button1.Enabled = true;
                }));  
            };
            
            car2timer.Elapsed += (a, b) =>
            {
                Invoke((MethodInvoker)(() =>
                {
                    button2.BackColor = Color.Gray;
                    button2.Enabled = true;
                }));
            };
        }

        
        private async void button1_Click(object sender, EventArgs e)
        {
            //car 1

            button1.Enabled = false;
            button1.BackColor = Color.Yellow;

            //convert text to uint64
            var uid = Convert.ToUInt64(textBox1.Text.Trim());
            var uid_bytes = BitConverter.GetBytes(uid);
            uid_bytes = uid_bytes.Reverse().ToArray();

            //publish
            client.Publish("acc/elev/0/rdr/tap", uid_bytes);

            car1timer.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //car 2

            button2.Enabled = false;
            button2.BackColor = Color.Yellow;

            //convert text to uint64
            var uid = Convert.ToUInt64(textBox1.Text.Trim());
            var uid_bytes = BitConverter.GetBytes(uid);
            uid_bytes = uid_bytes.Reverse().ToArray();

            //publish
            client.Publish("acc/elev/1/rdr/tap", uid_bytes);

            car2timer.Start();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            //shown

            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            var guid = Guid.NewGuid();
            string clientId = guid.ToString();

            client.Connect(clientId);
            client.Subscribe(new string[] { "acc/elev/0/rdr/tap/resp" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            client.Subscribe(new string[] { "acc/elev/1/rdr/tap/resp" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // handle message received 

            if (e.Topic == "acc/elev/0/rdr/tap/resp")
            {
                car1timer.Stop();
                Invoke((MethodInvoker)(() => { button1.BackColor = colors[(int)e.Message[0]]; }));
                car1timer.Start();
            } 
            else
                if (e.Topic == "acc/elev/1/rdr/tap/resp")
                {
                    car2timer.Stop();
                    Invoke((MethodInvoker)(() => { button2.BackColor = colors[(int)e.Message[0]]; }));
                    car2timer.Start();
            }
        }
    }
}
