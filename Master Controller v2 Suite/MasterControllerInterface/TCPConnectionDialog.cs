using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using MCICommon;

namespace MasterControllerInterface
{
    public partial class TCPConnectionDialog : Form
    {
        private TCPConnectionProperties connprop;

        public TCPConnectionDialog()
        {
            InitializeComponent();

            comboBox1.DataSource = new List<string>(ConfigurationManager.TCPConnectionHistory.Select(connprop => connprop.AddressString));
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //okay button

            if (comboBox1.Text.Trim() != "")
            {
                comboBox1.Enabled = false;
                numericUpDown1.Enabled = false;
                textBox1.Enabled = false;

                UseWaitCursor = true;

                try
                {
                    Console.WriteLine((await Dns.GetHostAddressesAsync(comboBox1.Text))[0].ToString());

                    connprop = new TCPConnectionProperties(textBox1.Text, comboBox1.Text, Convert.ToInt32(numericUpDown1.Value));
                }
                catch (Exception ex)
                {
                    numericUpDown1.Enabled = true;
                    comboBox1.Enabled = true;
                    textBox1.Enabled = true;

                    UseWaitCursor = false;

                    comboBox1.Focus();

                    return;
                }

                ConfigurationManager.AddTCPConnectionHistoryItem(connprop);

                DialogResult = DialogResult.OK;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //cancel button
            DialogResult = DialogResult.Cancel;
        }

        public TCPConnectionProperties ConnectionProperties
        {
            get
            {
                return connprop;
            }
        }

        private void TCPConnectionDialog_Shown(object sender, EventArgs e)
        {
            comboBox1.Focus();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            TCPConnectionProperties tcpconnprop = ConfigurationManager.TCPConnectionHistory[comboBox1.SelectedIndex];

            textBox1.Text = tcpconnprop.Alias;
            numericUpDown1.Value = tcpconnprop.Port;
        }
    }
}
