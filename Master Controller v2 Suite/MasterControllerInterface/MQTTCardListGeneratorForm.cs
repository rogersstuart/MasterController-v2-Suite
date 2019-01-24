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
using System.IO;

namespace MasterControllerInterface
{
    public partial class MQTTCardListGeneratorForm : Form
    {
        private MqttClient client;

        private ulong last_card_read;

        private List<ulong> card_ids = new List<ulong>();

        public MQTTCardListGeneratorForm()
        {
            InitializeComponent();

            
        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            ulong uid = BitConverter.ToUInt64(e.Message.Reverse().ToArray(), 0);

            if (uid != last_card_read)
            {
                last_card_read = uid;
                card_ids.Add(uid);
            }
            else
                return;

            

            Invoke(((MethodInvoker)(() =>
            {
                var loca = textBox1.Lines.ToList();
                loca.Add(uid.ToString());
                textBox1.Lines = loca.ToArray();
                Refresh();
            })));
            //RefreshUI();
        }

        private void RefreshUI()
        {

        }

        private void MQTTCardListGeneratorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            client.Disconnect();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //save button clicked

            client.Disconnect();

            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
                File.WriteAllLines(sfd.FileName, card_ids.Select(x => x + "").ToArray());

            Close();
        }

        private void MQTTCardListGeneratorForm_Shown(object sender, EventArgs e)
        {
            //first time the form is shown

            client = new MqttClient("mccsrv1");
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            var guid = Guid.NewGuid();
            string clientId = guid.ToString();
            client.Connect(clientId);
            client.Subscribe(new string[] { "acc/elev/0/rdr/tap" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            client.Subscribe(new string[] { "acc/elev/1/rdr/tap" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }
    }
}
