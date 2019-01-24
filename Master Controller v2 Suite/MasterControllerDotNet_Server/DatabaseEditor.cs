using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using MySql.Data.MySqlClient;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    internal partial class DatabaseEditor : Form
    {
        DatabaseConnectionProperties dbconnprop;
        public DatabaseEditor(DatabaseConnectionProperties dbconnprop)
        {
            InitializeComponent();

            this.dbconnprop = dbconnprop;
        }

        private async Task RefreshTableNames()
        {
                UseWaitCursor = true;
                foreach (Control ctl in Controls)
                    ctl.Enabled = false;

                string[] table_names = await DatabaseManager.GetTableNames(dbconnprop);

                comboBox1.DataSource = null;
                comboBox1.DataSource = table_names;

                foreach (Control ctl in Controls)
                    ctl.Enabled = true;
                UseWaitCursor = false;
        }

        private struct ComboName
        {
            public string description;
            public UInt64 uid;

            public ComboName(string description, UInt64 uid)
            {
                this.description = description;
                this.uid = uid;
            }

            public override string ToString()
            {
                if (description == null)
                    return uid + "";
                else
                    return description + " - " + uid;
            }
        }

        private async Task RefreshEntryNames()
        {
            if (comboBox1.SelectedIndex > -1)
            {
                string table_name = (string)comboBox1.SelectedItem;

                UseWaitCursor = true;
                foreach (Control ctl in Controls)
                    ctl.Enabled = false;

                UInt64[] uids = await DatabaseManager.GetUIDs(dbconnprop, table_name);
                string[] descriptions = await DatabaseManager.GetDescriptions(dbconnprop, table_name);

                ComboName[] combos = new ComboName[uids.Length];
                for (int index_counter = 0; index_counter < uids.Length; index_counter++)
                    combos[index_counter] = new ComboName(descriptions[index_counter], uids[index_counter]);

                listBox1.DataSource = null;
                listBox1.DataSource = combos;

                foreach (Control ctl in Controls)
                    ctl.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            //add
            TextBoxDialog tbd = new TextBoxDialog("Enter New Table Name");
            if (tbd.ShowDialog(this) == DialogResult.OK)
                if (tbd.TextResult.Length > 0)
                {
                    string textresult = tbd.TextResult;

                    await DatabaseManager.CreateTable(dbconnprop, textresult);
                    await RefreshTableNames();
                }

            tbd.Dispose();
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            //edit table name
            if (comboBox1.SelectedIndex > -1)
            {
                string original_table_name = (string)comboBox1.SelectedItem;

                TextBoxDialog tbd = new TextBoxDialog("Enter New Table Name");
                if (tbd.ShowDialog(this) == DialogResult.OK)
                    if (tbd.TextResult.Length > 0)
                    {
                        string new_table_name = tbd.TextResult;

                        await DatabaseManager.RenameTable(dbconnprop, original_table_name, new_table_name);

                        await RefreshTableNames();
                    }

                tbd.Dispose();
            }
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            //delete table
            if (comboBox1.SelectedIndex > -1)
            {
                string tablename = (string)comboBox1.SelectedItem;

                UseWaitCursor = true;
                foreach (Control ctl in Controls)
                    ctl.Enabled = false;

                await DatabaseManager.DeleteTable(dbconnprop, tablename);

                await RefreshTableNames(); //this will reenable everything
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //add entry to table
            if (comboBox1.SelectedIndex > -1)
            {
                TextBoxDialog tbd = new TextBoxDialog("Enter UID of New Entry");
                tbd.ShowDialog();
                if (tbd.DialogResult == DialogResult.OK && tbd.TextResult.Length > 0)
                {
                    await DatabaseManager.AddRow(dbconnprop, (string)comboBox1.SelectedItem, Convert.ToUInt64(tbd.TextResult));

                    AccessPropertiesEditor ented = new AccessPropertiesEditor(dbconnprop, (string)comboBox1.SelectedItem, Convert.ToUInt64(tbd.TextResult));
                    ented.ShowDialog(this);
                    ented.Dispose();

                    await RefreshEntryNames();
                }

                tbd.Dispose();
            }
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            //edit table entry
            if(comboBox1.SelectedIndex > -1 && listBox1.SelectedIndex > -1)
            {
                AccessPropertiesEditor ented = new AccessPropertiesEditor(dbconnprop, (string)comboBox1.SelectedItem, ((ComboName)listBox1.SelectedItem).uid);
                ented.ShowDialog(this);
                ented.Dispose();

                await RefreshEntryNames();
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            //delete table entry
            if (listBox1.SelectedIndex > -1 && comboBox1.SelectedIndex > -1)
            {
                await DatabaseManager.DeleteRow(dbconnprop, (string)comboBox1.SelectedItem, ((ComboName)listBox1.SelectedItem).uid);
                
                await RefreshEntryNames();
            }
        }

        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            await RefreshEntryNames();
        }

        private async void DatabaseEditor_Shown(object sender, EventArgs e)
        {
            await RefreshTableNames();
        }
    }
}
