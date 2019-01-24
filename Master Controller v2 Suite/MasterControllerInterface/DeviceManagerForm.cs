using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Net.Sockets;
using MCICommon;

namespace MasterControllerInterface
{
    public partial class DeviceManagerForm : Form
    {
        private DatabaseConnectionProperties dbconnprop;
        private List<List<Object>> displayed_devices = null;

        private string[] device_type_strings = new string[] {"MCv2 Offline Controller", "Elevator Expander", "WiFi Door Controller", "IP Camera"};

        private bool is_adding = false;

        public DeviceManagerForm(DatabaseConnectionProperties dbconnprop)
        {
            this.dbconnprop = dbconnprop;

            InitializeComponent();
        }

        private async void DeviceManagerForm_Shown(object sender, EventArgs e)
        {
            //form show, populate

            await RefreshDeviceList();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //add button clicked

            if(is_adding)
            {
                //cancelation requested
                is_adding = false;
                await RefreshDeviceList();
            }
            else
            {
                //begin the process of adding a new device

                is_adding = true;

                button1.Text = "Cancel";

                listBox1.Enabled = false;
                button2.Enabled = false;
                button4.Enabled = false;

                textBox1.Enabled = true;
                textBox1.Text = "";
                textBox2.Enabled = true;
                textBox2.Text = "";
                numericUpDown1.Enabled = true;
                numericUpDown1.Value = 23;
                checkedListBox1.Enabled = true;
                foreach (var item in checkedListBox1.CheckedIndices)
                    checkedListBox1.SetItemCheckState((int)item, CheckState.Unchecked);
                //button3.Enabled = true;
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            //remove button clicked

            ulong selected_device_id = (ulong)displayed_devices[listBox1.SelectedIndex][0];

            ResetUI();

            using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
            {
                await sqlconn.OpenAsync();

                await DatabaseUtilities.DeleteDevicesRow(sqlconn, selected_device_id);
            }

            await RefreshDeviceList();
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            //test button clicked

            await TestConnection();
        }

        private async Task TestConnection()
        {
            //run a ping test and see if its possible to connect to the specified port

            int selected_index = listBox1.SelectedIndex;

            if(selected_index > -1)
            {
                string result_text = await TestTCPPortConnection((string)displayed_devices[selected_index][2], (int)(UInt32)displayed_devices[selected_index][3]) ? "The device is online and accepting connections on the specified port."
                    : "The device is either offline or not accepting connections on the specified port.";

                MessageBox.Show(this, result_text, "Test Result");
            }
        }

        private async Task<bool> TestTCPPortConnection(string hostname_or_ip, int port)
        {
            try
            {
                using (TcpClient tcpclient = new TcpClient())
                    await tcpclient.ConnectAsync(hostname_or_ip, port);

                return true;
            }
            catch (Exception ex)
            {

            }

            return false;
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            //save button clicked

            button1.Enabled = false;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            numericUpDown1.Enabled = false;
            checkedListBox1.Enabled = false;
            button3.Enabled = false;

            Refresh();

            ulong device_id = 0;
            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

            try
            {
                if (is_adding)
                {
                    device_id = await DatabaseUtilities.GenerateUniqueDeviceID(sqlconn.Connection);

                    await DatabaseUtilities.AddDevicesRow(sqlconn.Connection, new Object[] { device_id, (int)checkedListBox1.CheckedIndices[0], textBox2.Text.Trim(), numericUpDown1.Value, textBox1.Text.Trim() });
                }
                else
                {
                    device_id = (ulong)displayed_devices[listBox1.SelectedIndex][0];

                    await DatabaseUtilities.SetDevicesRow(sqlconn.Connection, new Object[] { device_id, (int)checkedListBox1.CheckedIndices[0], textBox2.Text.Trim(), numericUpDown1.Value, textBox1.Text.Trim() });
                }
            }
            catch(Exception ex)
            {

            }

            ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            if (is_adding)
                is_adding = false;

            await RefreshDeviceList();

            for (int i = 0; i < displayed_devices.Count(); i++)
                if ((UInt64)displayed_devices[i][0] == device_id)
                    checkedListBox1.SelectedIndex = i;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //selected device changed, populate fields
            int selected_index = listBox1.SelectedIndex;

            if (selected_index > -1)
            {
                textBox1.Text = (string)displayed_devices[selected_index][4]; //alias
                textBox2.Text = (string)displayed_devices[selected_index][2]; //address
                numericUpDown1.Value = (int)(UInt32)displayed_devices[selected_index][3]; //port

                foreach (int item in checkedListBox1.CheckedIndices)
                    checkedListBox1.SetItemCheckState(item, CheckState.Unchecked);

                checkedListBox1.SetItemCheckState((byte)displayed_devices[selected_index][1], CheckState.Checked); //type

                ConfigUI();
            }
        }

        private void ResetUI()
        {
            button1.Enabled = false;
            button1.Text = "Add";

            listBox1.Enabled = false;
            button2.Enabled = false;
            button4.Enabled = false;

            textBox1.Enabled = false;
            textBox1.Text = "";
            textBox2.Enabled = false;
            textBox2.Text = "";
            numericUpDown1.Enabled = false;
            numericUpDown1.Value = 23;
            checkedListBox1.Enabled = false;
            foreach (var item in checkedListBox1.CheckedIndices)
                checkedListBox1.SetItemCheckState((int)item, CheckState.Unchecked);
            button3.Enabled = false;

            Refresh();
        }

        private void ConfigUI()
        {
            button1.Enabled = true;

            if (listBox1.Items.Count > 0)
                listBox1.Enabled = true;

            if(listBox1.SelectedIndex > -1)
            {
                button2.Enabled = true;
                button4.Enabled = true;

                textBox1.Enabled = true;
                textBox2.Enabled = true;
                numericUpDown1.Enabled = true;
                checkedListBox1.Enabled = true;

                CheckSaveConditions();
            }
            else
            {
                listBox1.Enabled = false;
                button2.Enabled = false;
                button4.Enabled = false;

                textBox1.Enabled = false;
                textBox2.Enabled = false;
                numericUpDown1.Enabled = false;
                checkedListBox1.Enabled = false;
                button3.Enabled = false;
            }

            Refresh();
        }

        private async Task RefreshDeviceList()
        {
            //get list of devices from the database
            ResetUI();

            using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
            {
                await sqlconn.OpenAsync();

                displayed_devices = await DatabaseUtilities.GetDevicesFromDatabase(sqlconn);
            }

            if(displayed_devices == null)
            {
                ResetUI();
                return;
            }
            else
            {
                listBox1.DataSource = null;
                listBox1.DataSource = GetDeviceShortFormStrings();
            }

            ConfigUI();
        }

        private void RefreshDeviceDetails()
        {
            //if(listBox)
        }

        private string[] GetDeviceShortFormStrings()
        {
            List<string> device_strings = new List<string>();

            foreach (List<Object> entry in displayed_devices)
            {
                string entry_string = "";

                if (((string)entry[4]).Trim() != "")
                    entry_string = (string)entry[4] + " " + device_type_strings[(byte)entry[1]];
                else
                    entry_string = (string)entry[2] + ":" + ((UInt32)entry[3]).ToString() + " " + device_type_strings[(byte)entry[1]];

                if (device_strings.Contains(entry_string))
                    device_strings.Add(((UInt64)entry[0]).ToString() + " " + device_type_strings[(byte)entry[1]]);
                else
                    device_strings.Add(entry_string);
            }

            return device_strings.ToArray();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            //address text changed

            CheckSaveConditions();
        }

        private void CheckSaveConditions()
        {
            if (textBox2.Text.Trim() != "" && checkedListBox1.CheckedIndices.Count > 0)
                button3.Enabled = true;
            else
                button3.Enabled = false;

            Refresh();
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            bool will_be_checked = false;

            if (checkedListBox1.CheckedIndices.Count == 0)
            {
                if (e.NewValue == CheckState.Checked)
                    will_be_checked = true;
            }
            else
                if(checkedListBox1.CheckedIndices.Count == 1 && checkedListBox1.CheckedIndices[0] == e.Index && e.NewValue == CheckState.Unchecked)
                {
                    will_be_checked = false;
                }
                else
                    if(checkedListBox1.CheckedIndices.Count == 1 && checkedListBox1.CheckedIndices[0] != e.Index && e.NewValue == CheckState.Checked)
                    {
                        foreach (int index in checkedListBox1.CheckedIndices)
                            if (index != e.Index)
                                checkedListBox1.SetItemCheckState(index, CheckState.Unchecked);

                        will_be_checked = true;
                    }

            if (textBox2.Text.Trim() != "" && will_be_checked)
                button3.Enabled = true;
            else
                button3.Enabled = false;

            Refresh();
        }

        private async void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await RefreshDeviceList();
        }
    }
}
