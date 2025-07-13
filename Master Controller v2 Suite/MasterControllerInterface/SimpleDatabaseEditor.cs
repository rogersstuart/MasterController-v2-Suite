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
using MCICommon;

namespace MasterControllerInterface
{
    internal partial class SimpleDatabaseEditor : Form
    {
        private const int FULL_ACCESS_CARD = 0;
        private const int BUILDING_EMPLOYEE_CARD = 1;
        private const int SCHOOL_EMPLOYEE_CARD = 2;
        private const int STUDENT_CARD = 3;
        private const int FLOOR_3_TENANT_CARD = 4;
        private const int FLOOR_4_TENANT_CARD = 5;
        private const int FLOOR_6_TENANT_CARD = 6;
        private const int FLOOR_7_TENANT_CARD = 7;
        private const int FLOOR_9_TENANT_CARD = 8;
        private const int FLOOR_12_TENANT_CARD = 9;
        private const int FLOOR_13_TENANT_CARD = 10;

        private readonly string[] DEFAULT_NAMES = {"Full Access", "Building Employee", "School Employee", "Student", "3rd Floor Tenant", "4th Floor Tenant", "6th Floor Tenant", "7th Floor Tenant", "9th Floor Tenant", "12th Floor Tenant", "13th Floor Tenant"};

        private int selected_default = 0;

        private PanelMonitor pnlmon = null;
        private ConnectionProperties pnlconnprop = null;

        private volatile bool autoaddflag = false;
        private bool prompt_if_exists = false;

        public SimpleDatabaseEditor()
        {
            InitializeComponent();
        }

        private void RefreshFormTitle()
        {
            string title_string = "";
            if (pnlmon != null)
                title_string += pnlmon.ToString() + "/";
            else
                title_string += "No Panel/";

            title_string += DEFAULT_NAMES[selected_default];

            this.Text = title_string;
        }

        private void RefreshTablesList()
        {
            string[] table_names = ConfigurationManager.ListPaths;

            toolStripComboBox1.ComboBox.DataSource = null;
            toolStripComboBox1.ComboBox.DataSource = table_names;
        }

        private UInt64 GetUIDFromTextBox()
        {
            string uid_to_convert = textBox1.Text.Trim();
            if (uid_to_convert == "")
                return 0;
            else
            {
                try
                {
                    UInt64 uid = Convert.ToUInt64(uid_to_convert);
                    return uid;
                }
                catch(Exception ex)
                {
                    return 0;
                }
            }
        }

        private string GetSelectedTableName()
        {
            if (toolStripComboBox1.ComboBox.SelectedIndex > -1)
                return (string)toolStripComboBox1.ComboBox.SelectedItem;
            else
                return null;
        }

        private async Task ModifyLM()
        {
            //public ListMember(string source_filename, int source_row, bool is_active, int serial, UInt64 uid, string name, string description, string active_days, string active_times)
            string table_name = GetSelectedTableName();

            if (table_name == null)
            {
                MessageBox.Show(this, "A table must be selected to continue.");
                return;
            }

            //if (MessageBox.Show(this, "Are you sure that you want to add this card?", "Prompt", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
            //    return;

            UInt64 uid = GetUIDFromTextBox();
            if (uid > 0)
                textBox1.Text = "";
            else
            {
                MessageBox.Show(this, "Invalid UID");
                return;
            }

            int uid_search_result = ListUtilities.FindUID(table_name, uid);

            if(uid_search_result > -1 && prompt_if_exists)
                if (MessageBox.Show(this, "The UID was found in the selected list. Do you want to continue?", "Prompt", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                    return;

            ListMember to_update = uid_search_result == -1 ? null : (await ListUtilities.GetListMembers(new string[] { table_name })).Where(x => x.UID.CompareTo(uid) == 1).First();

            string[] default_values = GenerateDefaults(selected_default);

            if (to_update != null)
            {
                to_update.ActiveDays = default_values[0];
                to_update.ActiveTimes = default_values[1];

                ListUtilities.WriteMemberToList(to_update);
            }
            else
            {
                int next_row_index = ListUtilities.GetRowRange(table_name) + 1;
                int next_list_serial = ListUtilities.GetSerialRange(table_name) + 1;

                ListUtilities.WriteMemberToList(new ListMember(
                    table_name,
                    next_row_index,
                    false,
                    next_list_serial,
                    uid,
                    "",
                    "",
                    default_values[0],
                    default_values[1]));
            }
        }

        private string[] GenerateDefaults(int default_option)
        {
            switch(default_option)
            {
                case 0:
                    return new string[] { "Sunday-Saturday", "00:00-23:59"};
                
                case 1:
                    return new string[] { "Sunday-Saturday", "00:00-23:59" };
                
                case 2:
                    return new string[] { "Sunday-Saturday", "00:00-23:59" };
                
                case 3:
                    return new string[] { "Sunday-Saturday", "00:00-23:59" };

                case 4:
                    return new string[] { "Sunday-Saturday", "00:00-23:59" };

                case 5:
                    return new string[] { "Sunday-Saturday", "00:00-23:59" };

                case 6:
                    return new string[] { "Sunday-Saturday", "00:00-23:59" };

                case 7:
                    return new string[] { "Sunday-Saturday", "00:00-23:59" };

                case 8:
                    return new string[] { "Sunday-Saturday", "00:00-23:59" };

                case 9:
                    return new string[] { "Sunday-Saturday", "00:00-23:59" };

                case 10:
                    return new string[] { "Sunday-Saturday", "00:00-23:59" };

                default:
                    throw new Exception("Invalid Default Selected");
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //add button
            await ModifyLM();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            //show in list manager
            await ModifyLM();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //delete button
            MessageBox.Show(this, "Sorry but this feature is currently unimplemented.");
            return; // Add this return statement to prevent unreachable code warning
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //uid text changed
            //Console.WriteLine("Text Changed");
        }

        private async void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            //key pressed in uid textbox
            string table_name = GetSelectedTableName();
            if (table_name != null)
            {
                if(e.KeyChar == (char)Keys.Return)
                    await ModifyLM();
            }
            else
                MessageBox.Show(this, "A table must be selected to continue.");
        }

        private void defaultValuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //default values editor option
        }

        private void panelConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //panel connection option

            if(pnlmon != null)
            {
                pnlmon.Stop();
                pnlmon.presented -= pnlmon_CardPresented;

                ConnectionPropertiesEditor connproped = pnlconnprop == null ? new ConnectionPropertiesEditor() : new ConnectionPropertiesEditor(pnlconnprop);
                if (connproped.ShowDialog(this) == DialogResult.OK)
                {
                    ConnectionProperties new_pnlconnprop = connproped.ConnectionProperties;
                    PanelMonitor new_pnlmon = null;
                    try
                    {
                        new_pnlmon = new PanelMonitor(new_pnlconnprop);
                        new_pnlmon.presented += pnlmon_CardPresented;
                        new_pnlmon.Start();
                    }
                    catch (Exception ex)
                    {
                        if(new_pnlmon != null)
                        {
                            try
                            {
                                new_pnlmon.Stop();
                                new_pnlmon.presented -= pnlmon_CardPresented;
                            }
                            catch (Exception ex2) { }
                        }

                        MessageBox.Show(this, "Error connecting to panel. The settings have been reverted.");

                        pnlmon.presented += pnlmon_CardPresented;
                        pnlmon.Start();
                    }

                    pnlconnprop = new_pnlconnprop;
                    pnlmon = new_pnlmon;
                }
                else
                {
                    pnlmon.presented += pnlmon_CardPresented;
                    pnlmon.Start();
                }

                connproped.Dispose();
            }
            else
            {
                ConnectionPropertiesEditor connproped = new ConnectionPropertiesEditor();
                if (connproped.ShowDialog(this) == DialogResult.OK)
                {
                    ConnectionProperties new_pnlconnprop = connproped.ConnectionProperties;
                    PanelMonitor new_pnlmon = null;
                    try
                    {
                        new_pnlmon = new PanelMonitor(new_pnlconnprop);
                        new_pnlmon.presented += pnlmon_CardPresented;
                        new_pnlmon.Start();
                    }
                    catch (Exception ex)
                    {
                        if (new_pnlmon != null)
                        {
                            try
                            {
                                new_pnlmon.Stop();
                                new_pnlmon.presented -= pnlmon_CardPresented;
                            }
                            catch (Exception ex2) { }
                        }

                        MessageBox.Show(this, "Error connecting to panel.");
                    }

                    pnlconnprop = new_pnlconnprop;
                    pnlmon = new_pnlmon;
                }
            }

            RefreshFormTitle();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            //auto add on read selection changed
            autoaddflag = checkBox1.Checked;
        }

        private void pnlmon_CardPresented(object sender, PanelEventArgs pnlevargs)
        {
            //a new card has been presented
            Invoke(new Action(() => textBox1.Text = pnlevargs.PanelState.Card.UID + ""));

            if (autoaddflag)
            {
                Invoke(new Action(async () =>
                {
                    await ModifyLM();
                }));
            }
        }

        private void SimpleDatabaseEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (pnlmon != null)
            {
                pnlmon.Stop();
                pnlmon.presented -= pnlmon_CardPresented;
                pnlmon = null;
            }
        }

        private void fullAccess247ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //full access card
            selected_default = FULL_ACCESS_CARD;
            RefreshFormTitle();
        }

        private void rdFloorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //3rd floor tenant
            selected_default = FLOOR_3_TENANT_CARD;
            RefreshFormTitle();
        }

        private void thFloorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //4th floor tenant
            selected_default = FLOOR_4_TENANT_CARD;
            RefreshFormTitle();
        }

        private void thFloorToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            //13th floor tenant
            selected_default = FLOOR_13_TENANT_CARD;
            RefreshFormTitle();
        }

        private void thFloorToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //6th floor tenant
            selected_default = FLOOR_6_TENANT_CARD;
            RefreshFormTitle();
        }

        private void thFloorToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            //7th floor tenant
            selected_default = FLOOR_7_TENANT_CARD;
            RefreshFormTitle();
        }

        private void thFloorToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            //9th floor tenant
            selected_default = FLOOR_9_TENANT_CARD;
            RefreshFormTitle();
        }

        private void thFloorToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            //12th floor tenant
            selected_default = FLOOR_12_TENANT_CARD;
            RefreshFormTitle();
        }

        private void studentCardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //student card
            selected_default = STUDENT_CARD;
            RefreshFormTitle();
        }

        private void schoolEmployeeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //school employee card
            selected_default = SCHOOL_EMPLOYEE_CARD;
            RefreshFormTitle();
        }

        private void buildingEmployeeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //building employee card
            selected_default = BUILDING_EMPLOYEE_CARD;
            RefreshFormTitle();
        }

        private void SimpleDatabaseEditor_Shown(object sender, EventArgs e)
        {
            RefreshTablesList();

            RefreshFormTitle();
        }
    }
}
