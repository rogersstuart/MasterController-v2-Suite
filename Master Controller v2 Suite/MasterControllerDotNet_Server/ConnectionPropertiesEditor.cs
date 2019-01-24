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
using MCICommon;

namespace MasterControllerDotNet_Server
{
    internal partial class ConnectionPropertiesEditor : Form
    {
        ConnectionProperties connprop;
        public ConnectionPropertiesEditor() : this(null) { }
        public ConnectionPropertiesEditor(ConnectionProperties connprop)
        {
            InitializeComponent();

            if(connprop != null)
            {
                textBox1.Text = connprop.IPAddress;
                textBox2.Text = connprop.TCPPort + "";
            }
        }

        private void ConnectionPropertiesEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        public ConnectionProperties ConnectionProperties
        {
            get
            {
                return connprop;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //accept button
            connprop = new ConnectionProperties(textBox1.Text.Trim(), Convert.ToInt32(textBox2.Text));

            DialogResult = DialogResult.OK;
            return;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //cancel button
            DialogResult = DialogResult.Cancel;

            return;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //test button
            ConnectionProperties localprop = new ConnectionProperties(textBox1.Text.Trim(), Convert.ToInt32(textBox2.Text));

            try
            {
                using (TcpClient tcpclient = new TcpClient())
                {
                    tcpclient.Connect(localprop.IPAddress, localprop.TCPPort);
                    tcpclient.Close();

                    MessageBox.Show("Connection Successful");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Connection Failed");
            }
        }
    }
}
