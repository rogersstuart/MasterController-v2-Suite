using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    internal partial class AccessPropertiesEditor : Form
    {
        DatabaseConnectionProperties dbconnprop;
        string table_name;
        UInt64 uid;

        public AccessPropertiesEditor(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid)
        {
            InitializeComponent();

            this.dbconnprop = dbconnprop;
            this.table_name = table_name;
            this.uid = uid;

            this.Text = table_name + " - " + uid;
        }

        private async Task Populate()
        {
                UseWaitCursor = true;
                foreach (Control ctl in Controls)
                    ctl.Enabled = false;
            
                AccessProperties accessprops = await GetAccessPropertiesFromDatabase();

                if(accessprops != null)
                {
                    string description = await DatabaseManager.GetDescription(dbconnprop, table_name, uid);

                    textBox1.Text = description;
                
                    checkBox1.Checked = accessprops.ForceEnable;
                    checkBox2.Checked = accessprops.ForceDisable;

                    dateTimePicker1.Value = accessprops.EnabledFrom;
                    dateTimePicker2.Value = accessprops.EnabledFrom;
                    dateTimePicker3.Value = accessprops.EnabledTo;
                    dateTimePicker4.Value = accessprops.EnabledTo;

                    listBox1.DataSource = null;
                    listBox1.DataSource = accessprops.ActivationProperties;
                }

                foreach (Control ctl in Controls)
                    ctl.Enabled = true;
                UseWaitCursor = false;
        }

        private async Task<AccessProperties> GetAccessPropertiesFromDatabase()
        {
            AccessProperties accessprops = await DatabaseManager.GetAccessProperties(dbconnprop, table_name, uid);

            if (accessprops == null)
            { 
                accessprops = new AccessProperties(DateTime.Now, DateTime.Now, new List<ActivationProperties>(), false, false);
                await WriteAccessPropertiesToDatabase(accessprops);
            }

            return await DatabaseManager.GetAccessProperties(dbconnprop, table_name, uid);
        }

        private Task WriteAccessPropertiesToDatabase(AccessProperties accessprop)
        {
            return DatabaseManager.SetAccessProperties(dbconnprop, table_name, uid, accessprop);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //add activation property
            ActivationPropertiesEditor actiproped = new ActivationPropertiesEditor();
            actiproped.ShowDialog();
            if(actiproped.DialogResult == DialogResult.OK)
            {
                AccessProperties accessprop = await GetAccessPropertiesFromDatabase();
                accessprop.ActivationProperties.Add(actiproped.ActivationProperties);

                await WriteAccessPropertiesToDatabase(accessprop);

                await Populate();
            }

            actiproped.Dispose();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            //edit activation property
            int selected_index = listBox1.SelectedIndex;
            if (selected_index > -1)
            {
                ActivationPropertiesEditor actiproped = new ActivationPropertiesEditor((ActivationProperties)listBox1.SelectedItem);
                actiproped.ShowDialog();

                if (actiproped.DialogResult == DialogResult.OK)
                {
                    AccessProperties accessprop = await GetAccessPropertiesFromDatabase();
                    accessprop.ActivationProperties[selected_index] = actiproped.ActivationProperties;

                    WriteAccessPropertiesToDatabase(accessprop);

                    await Populate();
                }

                actiproped.Dispose();
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            //delete activation property
            if(listBox1.SelectedIndex > -1)
            {
                AccessProperties accessprop = await GetAccessPropertiesFromDatabase();
                accessprop.ActivationProperties.RemoveAt(listBox1.SelectedIndex);

                await WriteAccessPropertiesToDatabase(accessprop);

                await Populate();
            }
        }

        private void AccessPropertiesEditor_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            //notes button
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            //save
                UseWaitCursor = true;
                foreach (Control ctl in Controls)
                    ctl.Enabled = false;

                await DatabaseManager.SetDescription(dbconnprop, table_name, uid, textBox1.Text.Trim() == "" ? null : textBox1.Text.Trim());

                AccessProperties accessprop = await GetAccessPropertiesFromDatabase();

                accessprop.ForceEnable = checkBox1.Checked;
                accessprop.ForceDisable = checkBox2.Checked;

                accessprop.EnabledFrom = new DateTime(dateTimePicker1.Value.Year, dateTimePicker1.Value.Month, dateTimePicker1.Value.Day, dateTimePicker2.Value.Hour, dateTimePicker2.Value.Minute, dateTimePicker2.Value.Second);
                accessprop.EnabledTo = new DateTime(dateTimePicker3.Value.Year, dateTimePicker3.Value.Month, dateTimePicker3.Value.Day, dateTimePicker4.Value.Hour, dateTimePicker4.Value.Minute, dateTimePicker4.Value.Second);

                await WriteAccessPropertiesToDatabase(accessprop);

                DialogResult = DialogResult.OK;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void AccessPropertiesEditor_Shown(object sender, EventArgs e)
        {
            await Populate();
        }
    }
}
