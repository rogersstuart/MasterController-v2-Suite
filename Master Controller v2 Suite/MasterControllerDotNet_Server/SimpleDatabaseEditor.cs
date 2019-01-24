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
        
        private DatabaseConnectionProperties dbconnprop;

        private PanelMonitor pnlmon = null;
        private ConnectionProperties pnlconnprop = null;

        private volatile bool autoaddflag = false;

        public SimpleDatabaseEditor(DatabaseConnectionProperties dbconnprop)
        {
            InitializeComponent();

            this.dbconnprop = dbconnprop;

       
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

        private async Task RefreshTablesList()
        {
            string[] table_names = await DatabaseManager.GetTableNames(dbconnprop);

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

        private async Task<bool> AddCard(UInt64 uid, string table_name, int default_option)
        {
            AccessProperties accessprop = GenerateDefaults(default_option);

            if (!await DatabaseManager.ContainsUID(dbconnprop, table_name, uid))
            {
                await DatabaseManager.AddRow(dbconnprop, table_name, uid, accessprop);
                MessageBox.Show(this, "Successfully added card to database.");
                return true;
            }
            else
                if(MessageBox.Show(this, "UID exists in database. Overwrite?", "Error", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    await DatabaseManager.SetAccessProperties(dbconnprop, table_name, uid, accessprop);
                    MessageBox.Show(this, "Successfully added card to database.");
                    return true;
                }

            return false;

        }

        private AccessProperties GenerateDefaults(int default_option)
        {
            bool force_enable = false;
            int activation_duration = 6;
            bool[] exp0mask = new bool[16];
            bool[] exp0vals = new bool[16];
            DayOfWeek[] active_days = null;
            int active_hour_start = 0, active_minute_start = 0, active_hour_end = 0, active_minute_end = 0;
            bool[] comparison_flags = { false, false, false, true, true, true, false };

            //refrence
            //"Full Access", "Building Employee", "School Employee", "Student", "3rd Floor Tenant", "4th Floor Tenant",
            //"6th Floor Tenant", "7th Floor Tenant", "9th Floor Tenant", "12th Floor Tenant", "13th Floor Tenant"

            switch(default_option)
            {
                //full access
                case 0: force_enable = true;
                        exp0mask = new bool[] { false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false};
                        exp0vals = new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false};
                        active_days = new DayOfWeek[] {DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday};
                        active_hour_start = 0; active_minute_start = 0; active_hour_end = 23; active_minute_end = 59;
                        break;
                
                //building employee
                case 1: exp0mask = new bool[] { false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false };
                        exp0vals = new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false };
                        active_days = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
                        active_hour_start = 5; active_minute_start = 0; active_hour_end = 22; active_minute_end = 00;
                        break;
                
                //school employee
                case 2: exp0mask = new bool[] { false, true, false, false, false, false, false, false, false, true, true, false, false, false, false, false };
                        exp0vals = new bool[] { true, true, false, false, false, false, false, false, false, true, true, false, false, false, false, false };
                        active_days = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
                        active_hour_start = 5; active_minute_start = 0; active_hour_end = 22; active_minute_end = 00;
                        break;
                
                //student
                case 3: exp0mask[9] = true;
                        exp0vals[9] = true;
                        active_days = new DayOfWeek[] {DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday};
                        active_hour_start = 7; active_minute_start = 0; active_hour_end = 18; active_minute_end = 00;
                        break;

                //3rd floor
                case 4: exp0mask[2] = true;
                        exp0vals[2] = true;
                        active_days = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
                        active_hour_start = 0; active_minute_start = 0; active_hour_end = 23; active_minute_end = 59;
                        break;

                //4th floor
                case 5: exp0mask[4] = true;
                        exp0vals[4] = true;
                        active_days = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
                        active_hour_start = 0; active_minute_start = 0; active_hour_end = 23; active_minute_end = 59;
                        break;

                //6th floor
                case 6: exp0mask[5] = true;
                        exp0vals[5] = true;
                        active_days = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
                        active_hour_start = 0; active_minute_start = 0; active_hour_end = 23; active_minute_end = 59;
                        break;

                //7th floor
                case 7: exp0mask[6] = true;
                        exp0vals[6] = true;
                        active_days = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
                        active_hour_start = 0; active_minute_start = 0; active_hour_end = 23; active_minute_end = 59;
                        break;

                //9th floor
                case 8: exp0mask[8] = true;
                        exp0vals[8] = true;
                        active_days = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
                        active_hour_start = 0; active_minute_start = 0; active_hour_end = 23; active_minute_end = 59;
                        break;

                //12th floor
                case 9: exp0mask[11] = true;
                        exp0vals[11] = true;
                        active_days = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
                        active_hour_start = 0; active_minute_start = 0; active_hour_end = 23; active_minute_end = 59;
                        break;

                //13th floor
                case 10: exp0mask[12] = true;
                         exp0vals[12] = true;
                         active_days = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
                         active_hour_start = 0; active_minute_start = 0; active_hour_end = 23; active_minute_end = 59;
                         break;
            }

            List<DateTime> converted_days = new List<DateTime>();
            foreach (DayOfWeek dow in active_days)
                converted_days.Add(Utilities.GetNextWeekday(dow));

            List<DateTime> activation_start = new List<DateTime>();
            List<DateTime> activation_end = new List<DateTime>();

            foreach(DateTime dt in converted_days)
            {
                activation_start.Add(new DateTime(dt.Year, dt.Month, dt.Day, active_hour_start, active_minute_start, 0));
                activation_end.Add(new DateTime(dt.Year, dt.Month, dt.Day, active_hour_end, active_minute_end, 0));
            }

            List<ActivationProperties> actiprops = new List<ActivationProperties>();
            for (int index_counter = 0; index_counter < activation_start.Count; index_counter++)
            {
                actiprops.Add(new ActivationProperties(activation_start.ElementAt(index_counter), activation_end.ElementAt(index_counter), exp0mask,
                    new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }, exp0vals,
                    new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
                    comparison_flags, activation_duration));
            }

            return new AccessProperties(DateTime.Now, DateTime.Now.AddYears(1), actiprops, force_enable, false);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //add button
            string table_name = GetSelectedTableName();
            if (table_name != null)
            {
                UInt64 uid = GetUIDFromTextBox();
                if (uid > 0)
                {
                    if(await AddCard(uid, table_name, selected_default))
                        textBox1.Text = "";
                }
                else
                    MessageBox.Show(this, "Invalid UID");
            }
            else
                MessageBox.Show(this, "A table must be selected to continue.");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //edit button
            string table_name = GetSelectedTableName();
            if (table_name != null)
            {
                UInt64 uid = GetUIDFromTextBox();
                if (uid > 0)
                {
                    textBox1.Text = "";
                }
                else
                    MessageBox.Show(this, "Invalid UID");
            }
            else
                MessageBox.Show(this, "A table must be selected to continue.");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //delete button
            string table_name = GetSelectedTableName();
            if (table_name != null)
            {
                UInt64 uid = GetUIDFromTextBox();
                if (uid > 0)
                {
                    textBox1.Text = "";
                }
                else
                    MessageBox.Show(this, "Invalid UID");
            }
            else
                MessageBox.Show(this, "A table must be selected to continue.");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //uid text changed
            Console.WriteLine("Text Changed");
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            //key pressed in uid textbox
            string table_name = GetSelectedTableName();
            if (table_name != null)
            {
                if(e.KeyChar == (char)Keys.Return)
                {
                    if (MessageBox.Show(this, "Are you sure that you want to add this card?", "Prompt", MessageBoxButtons.OKCancel) == DialogResult.OK)
                    {
                        UInt64 uid = GetUIDFromTextBox();
                        if (uid > 0)
                        {
                            textBox1.Text = "";
                        }
                        else
                            MessageBox.Show(this, "Invalid UID");
                    }
                }
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
                    string table_name = GetSelectedTableName();
                    if (table_name != null)
                    {
                        if (await AddCard(pnlevargs.PanelState.Card.UID, table_name, selected_default))
                            textBox1.Text = "";
                    }
                    else
                        MessageBox.Show(this, "A table must be selected to continue.");
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

        private async void SimpleDatabaseEditor_Shown(object sender, EventArgs e)
        {
            await RefreshTablesList();

            RefreshFormTitle();
        }
    }
}
