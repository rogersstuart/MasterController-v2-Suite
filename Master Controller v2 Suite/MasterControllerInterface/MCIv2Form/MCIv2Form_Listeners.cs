using LabelPrinting;
using MCICommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UIElements;

namespace MasterControllerInterface
{
    public partial class MCIv2Form
    {
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var nodes = treeView1.Nodes[0].Nodes;

            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].IsSelected)
                {
                    button6.Enabled = true;
                    button20.Enabled = true;
                    return;
                }

            button6.Enabled = false;
            button20.Enabled = false;
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            //upload selected list to device

            if (listBox5.SelectedIndex == -1)
                return;

            var selected_list = V2LE_ViewingLists.Keys.ElementAt(listBox5.SelectedIndex);
            UInt64 selected_device = 0;

            var nodes = treeView1.Nodes[0].Nodes;

            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].IsSelected)
                {
                    selected_device = V2LE_ViewingDevices.Keys.ElementAt(i);
                    break;
                }

            ProgressDialog pgd = new ProgressDialog("Uploading List to " + V2LE_ViewingDevices[selected_device] + " Controller");
            pgd.Show(this);

            var res = await MasterControllerV2Utilities.UploadList(selected_list, selected_device, pgd);

            pgd.Dispose();

            if (MCv2Persistance.Instance.Config.UIConfiguration.ShowDialogOnMCV2OfflineControllerInteractionSuccess && !res)
                MessageBox.Show(V2LE_ViewingDevices[selected_device] + ": Upload Successful");
        }

        private async void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (rct != null)
            {
                rct.Cancel();
                while (rct != null)
                    await Task.Delay(100);
            }

            Invoke((MethodInvoker)(() => { textBox1.BackColor = Color.LightGoldenrodYellow; }));

            update_holdoff_timer.Stop();
            update_holdoff_timer.Start();
        }

        private async void connectionToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //system, database, connection

            Visible = false;

            DatabaseConnectionEditor dbconneditor = new DatabaseConnectionEditor(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties, true);
            var diag_result = dbconneditor.ShowDialog();

            if (diag_result == DialogResult.OK)
            {
                var config = MCv2Persistance.Instance.Config;

                config.DatabaseConfiguration.DatabaseConnectionProperties = dbconneditor.DBConnProp;

                MCv2Persistance.Instance.Config = config;

                ProgressDialog pgd = new ProgressDialog("");


                ARDBConnectionManager.default_manager.Stop();
                ARDBConnectionManager.default_manager = new ARDBConnectionManager();

                ARDBConnectionManager.default_progress_interface = pgd;
                pgd.Show();

                ARDBConnectionManager.default_manager.Start();

                var cfg = MCv2Persistance.Instance.Config;
                var bcfg = cfg.BackupConfiguration;

                if (bcfg.EnableAutoBackup)
                {
                    ProgressDialog pgd2 = new ProgressDialog("Database Backup");
                    pgd2.Show();
                    pgd2.SetMarqueeStyle();

                    var now_is = DateTime.Now;
                    var backup_filename = "auto_backup_" + cfg.DatabaseConfiguration.DatabaseConnectionProperties.Hostname.Replace('.', '_') + "_" + cfg.DatabaseConfiguration.DatabaseConnectionProperties.Schema + "_" + now_is.Year + "_" + now_is.Month + "_" + now_is.Day + "_" + now_is.ToFileTimeUtc().ToString();

                    pgd2.LabelText = "Saving backup to " + backup_filename + ".db2bak";

                    DBBackupAndRestore.Backup("./backups/" + backup_filename + ".db2bak").Wait();

                    pgd2.Dispose();

                    cfg.BackupConfiguration.LastAutoBackupTimestamp = DateTime.Now;
                }

                MCv2Persistance.Instance.Config = cfg;

                Visible = true;
                Refresh();

                await FullUIRefresh();
            }
            else
            {
                Visible = true;
                Refresh();
            }
        }

        private async void dropAllTablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //erase all tables in the database

            //verify the database password first
            TextBoxDialog tbd = new TextBoxDialog("Enter Password", "Please enter the current database password.");
            if (tbd.ShowDialog(this) == DialogResult.OK)
                if (tbd.TextBoxText == MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.Password)
                {
                    ProgressDialog pgd = new ProgressDialog("Reset Database");
                    pgd.Show();

                    pgd.LabelText = "Reading Table List";

                    using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                    {
                        await sqlconn.OpenAsync();

                        List<string> table_names = new List<string>();

                        using (MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn))
                        using (MySqlDataReader reader = cmdName.ExecuteReader())
                            while (await reader.ReadAsync())
                                table_names.Add(reader.GetString(0));

                        pgd.Step();
                        pgd.Reset();
                        pgd.LabelText = "Dropping Tables";

                        if (table_names.Count() > -1)
                            pgd.Maximum = table_names.Count();

                        foreach (string table_name in table_names)
                            using (MySqlCommand sqlcmd = new MySqlCommand("drop table `" + table_name + "`", sqlconn))
                            {
                                await sqlcmd.ExecuteNonQueryAsync();
                                pgd.Step();
                            }
                    }

                    pgd.Dispose();
                }

            await FullUIRefresh();
        }

        private void devicesToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "Master Controller Interface v" + Assembly.GetExecutingAssembly().GetName().Version.ToString()
                + Environment.NewLine + "Written by MST LLC", "About", MessageBoxButtons.OK);
        }

        private void dataGridView1_MouseEnter(object sender, EventArgs e)
        {
            dataGridView1.Focus();
        }

        private void listBox1_MouseEnter(object sender, EventArgs e)
        {
            listBox1.Focus();
        }

        private void listBox2_MouseEnter(object sender, EventArgs e)
        {
            listBox2.Focus();
        }

        private void listBox5_MouseEnter(object sender, EventArgs e)
        {
            listBox5.Focus();
        }

        private void listBox3_MouseEnter(object sender, EventArgs e)
        {
            listBox3.Focus();
        }

        private void listBox4_MouseEnter(object sender, EventArgs e)
        {
            listBox4.Focus();
        }

        private async void button22_Click(object sender, EventArgs e)
        {
            textBox3.Text = "";
            textBox3.Focus();

            await CardLookupAndDisplay(ULAD_GetSelectedUsers(), "", true);
        }

        private void treeView1_Enter(object sender, EventArgs e)
        {
            //devices tree gained focus

            var nodes = treeView1.Nodes[0].Nodes;

            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].IsSelected)
                {
                    button6.Enabled = true;
                    button20.Enabled = true;
                    return;
                }

            button6.Enabled = false;
            button20.Enabled = false;
        }

        private void treeView1_Leave(object sender, EventArgs e)
        {
            //devices tree lost focus

            //button6.Enabled = false;
            //button20.Enabled = false;
        }

        private void monitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //launch device selection form

            DeviceSelectionForm dsf = new DeviceSelectionForm();
            if (dsf.ShowDialog(this) == DialogResult.OK)
            {
                var device_info = dsf.SelectedDeviceInfo;

                switch ((byte)device_info[1])
                {
                    case 0:
                        if (MessageBox.Show(this, "This is a blocking monitor. Using it in conjunction with other MasterController network functions " +
                            "on the same device will result in system instabiltiy. Are you sure that you want to continue?", "Warning", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        {
                            ConfigurationManager.ConnectionType = 1;
                            ConfigurationManager.SelectedTCPConnection = new TCPConnectionProperties((string)device_info[4], (string)device_info[2], (int)((uint)device_info[3]));
                            new OutputMonitorForm().ShowDialog(this);
                            ManagedStream.CleanupConnection();
                        }
                        break;
                    case 1:
                        //if (MessageBox.Show(this, "This is a single user monitor. Using multiple monitors simultaneously " +
                        //    "will result in system instabiltiy. Are you sure that you want to continue?", "Warning", MessageBoxButtons.OKCancel) == DialogResult.OK)
                        //{
                        //var mon = new ExpanderMonitor((string)device_info[2], (int)((uint)device_info[3]));
                        var mon = new RemoteExpanderMonitor((ulong)device_info[0]);
                        mon.Start();
                        new MasterControllerDotNet_Server.ExpanderMonitorForm(mon).ShowDialog(this);
                        mon.Stop();
                        //}
                        break;
                    case 2:
                        new DoorControl.DoorControl().Show(this);
                        break;
                }
            }
            dsf.Dispose();
        }

        private async void writeDeviceLogToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "At this time logging is unsupported on some MasterController v2.x devices. Log reading may take more than an hour to complete. During the operation network functions on the selected device will be unavailable" +
                " and attempting to access them will result in system instabilty. Are you sure that you want to continue?", "Warning", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                DeviceSelectionForm dsf = new DeviceSelectionForm();
                if (dsf.ShowDialog(this) == DialogResult.OK)
                {
                    var device_info = dsf.SelectedDeviceInfo;

                    ManagedStreamV2 ms = await ManagedStreamV2.GenerateInstance((ulong)device_info[0]);

                    ProgressDialog pgd = new ProgressDialog("Log Reading Test");
                    pgd.Show(this);
                    pgd.Maximum = 7991;

                    double max = 0, min = 0, avg = 0;

                    Stopwatch st = new Stopwatch();
                    double freq = Stopwatch.Frequency;

                    for (int i = 0; i < 7991; i++)
                    {
                        st.Restart();

                        MCv2LogEntry log_entry = null;
                        try
                        {
                            log_entry = await MasterControllerV2Utilities.ReadLogEntry(ms, (uint)i);

                            if (log_entry == null)
                                throw new Exception();

                        }
                        catch (Exception ex)
                        {
                            st.Stop();

                            MessageBox.Show(this, "An error occured while reading the log. The operation has been aborted.", "Error", MessageBoxButtons.OK);

                            break;
                        }


                        st.Stop();


                        string log_string = log_entry.ToString();
                        File.AppendAllText("log_dump.txt", log_string + Environment.NewLine);

                        if (i == 0)
                        {
                            max = (st.ElapsedTicks / freq) * 1000;
                            min = (st.ElapsedTicks / freq) * 1000;
                            avg = (st.ElapsedTicks / freq) * 1000;
                        }
                        else
                        {
                            var ticks = (st.ElapsedTicks / freq) * 1000;
                            if (ticks > max)
                                max = ticks;
                            else
                                if (ticks < min)
                                min = ticks;

                            avg = (min + max + ticks) / 3;
                        }

                        pgd.LabelText = min + " " + max + " " + avg + Environment.NewLine + log_string;
                        pgd.Step();
                    }

                    ms.CleanupConnection();

                    pgd.Dispose();
                }
            }
        }

        private async void readTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //read the time from a device

            DeviceSelectionForm dsf = new DeviceSelectionForm(new List<int>(new int[] { 0 }));
            if (dsf.ShowDialog(this) == DialogResult.OK)
            {
                var device_info = dsf.SelectedDeviceInfo;

                ManagedStreamV2 ms = await ManagedStreamV2.GenerateInstance((ulong)device_info[0]);

                var dt = await MasterControllerV2Utilities.ReadControllerTime(ms);

                MessageBox.Show(dt.ToString());
            }
        }

        private async void setTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //set the time on a device

            DeviceSelectionForm dsf = new DeviceSelectionForm(new List<int>(new int[] { 0 }));

            if (dsf.ShowDialog(this) == DialogResult.OK)
            {
                var device_info = dsf.SelectedDeviceInfo;

                ProgressDialog pgd = new ProgressDialog("Setting Controller Time");
                pgd.Show();
                pgd.LabelText = "Opening Device Connection";

                ManagedStreamV2 ms = await ManagedStreamV2.GenerateInstance((ulong)device_info[0]);

                pgd.Step();
                await Task.Delay(100);
                pgd.Reset();

                bool result = await MasterControllerV2Utilities.SetControllerTime(ms, pgd);

                pgd.Dispose();

                if (result)
                    MessageBox.Show("The controller time was set successfully.");
                else
                    MessageBox.Show("An error occured while setting the controller time. " +
                        Environment.NewLine + " The operation was aborted.");
            }
        }

        /*
        private async void syncTimeAfterUploadsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //modify sync time after uploads flag

            if (syncTimeAfterUploadsToolStripMenuItem.Checked)
                syncTimeAfterUploadsToolStripMenuItem.Checked = false;
            else
                syncTimeAfterUploadsToolStripMenuItem.Checked = true;

            sync_time_after_uploads = syncTimeAfterUploadsToolStripMenuItem.Checked;

            var config = MCIv2Persistance.Config;
            config.SyncTimeAfterUploadsFlag = sync_time_after_uploads;
            MCIv2Persistance.Config = config;

            await FullUIRefresh();
        }
        */

        private void debugToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            debugToolStripMenuItem.DropDownItems.Clear();
            debugToolStripMenuItem.DropDownItems.Add(new ToolStripMenuItem("DB Cache Checked Out: " + ARDBConnectionManager.default_manager.NumCheckedOut));
            debugToolStripMenuItem.DropDownItems.Add(new ToolStripMenuItem("Device Server HostInfo Null?: " + (DeviceServerTracker.DeviceServerHostInfo == null ? "Yes" : "No")));
        }

        private async void auditToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
                await AuditingTools.GenerateListAudits(Path.GetDirectoryName(sfd.FileName));
        }

        private void deviceServerToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            //device server menu opening
            //check to see if the device server service is installed
            //display the installation and/or running state

            bool isAdmin = IsAdministrator();

            if (!isAdmin)
                installremoveToolStripMenuItem.Enabled = false;

            statusToolStripMenuItem.Enabled = false;

            statusToolStripMenuItem.DropDownItems.Clear();

            bool isInstalled = ServiceManager.isDeviceServerServiceInstalled();

            if (ServiceManager.isDeviceServerServiceInstalled())
                installremoveToolStripMenuItem.Text = "Uninstall";
            else
            {
                installremoveToolStripMenuItem.Text = "Install";
                statusToolStripMenuItem.Text = "Service Not Installed";

                return;
            }

            bool isRunning = ServiceManager.isDeviceServerServiceRunning();

            if (isAdmin)
            {
                var menu_item = new ToolStripMenuItem(isRunning ? "Stop" : "Start");
                menu_item.Click += devsrvstartstop_Clicked;

                statusToolStripMenuItem.DropDownItems.Add(menu_item);

                statusToolStripMenuItem.Enabled = true;
            }

            statusToolStripMenuItem.Text = "Service Installed - ";

            if (isRunning)
                statusToolStripMenuItem.Text += "Running";
            else
                statusToolStripMenuItem.Text += "Stopped";
        }

        private async void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            await FullUIRefresh();
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            //v2 list editor
            //dow string

            string text = textBox6.Text;

            checkedListBox1.Enabled = false;

            if (ListEntryUtilities.TryParseV2EntryDays(text))
            {
                textBox6.BackColor = Color.LightCyan;

                //set checks based on string
                bool[] check_states = ListEntryUtilities.ConvertDOWStringToBools(text);

                for (int i = 0; i < 7; i++)
                    checkedListBox1.SetItemCheckState(i, check_states[i] ? CheckState.Checked : CheckState.Unchecked);

                V2LE_ListEntryEditor_VerifyFields();
            }
            else
            {
                textBox6.BackColor = Color.LightSalmon;

                //clear clb checks
                for (int i = 0; i < 7; i++)
                    checkedListBox1.SetItemCheckState(i, CheckState.Unchecked);
            }

            checkedListBox1.Enabled = true;
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            //v2 list editor
            //time string

            string text = textBox7.Text;

            if (ListEntryUtilities.TryParseV2EntryTime(text))
            {
                textBox7.BackColor = Color.LightCyan;

                V2LE_SetTimesFromString(text);

                V2LE_ListEntryEditor_VerifyFields();
            }
            else
                textBox7.BackColor = Color.LightSalmon;
        }

        private void exportToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            //export

            //database export menu item
            new ExportForm().ShowDialog(this);
        }

        private async void devicesToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            new DeviceManagerForm(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties).ShowDialog(this);

            await FullUIRefresh();
        }

        private void mCISyncToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void mQTTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new MQTTCardListGeneratorForm().Show(this);
        }

        private async void button13_Click_1(object sender, EventArgs e)
        {
            //set home floors for selected users

            List<UInt64> selected_user_ids = new List<UInt64>();

            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                selected_user_ids.Add((UInt64)row.Cells[0].Value);

            HomeFloorsEditor hfe = new HomeFloorsEditor(selected_user_ids.ToArray<ulong>());
            var res = hfe.ShowDialog();

            //if(res == DialogResult.OK)
            //    await FullUIRefresh();
        }

        private async void button18_Click_1(object sender, EventArgs e)
        {
            //view in lists button

            //if user is only present in one list switch tabs immediately and insert user search term
            //if user is present in multiple lists display a list selection dialog

            List<UInt64> selected_user_ids = new List<UInt64>();

            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                selected_user_ids.Add((UInt64)row.Cells[0].Value);

            if ((selected_user_ids.Count() & 1) == 1)
            {
                var selected_user = selected_user_ids[0];
                var list_ids = await DatabaseUtilities.GetListUIDs();

                ConcurrentBag<ulong> user_present_in_lists = new ConcurrentBag<ulong>();

                Parallel.ForEach(list_ids, async a =>
                {
                    var ul = await DatabaseUtilities.GetAllUsersInList(a);

                    if (ul.Contains(selected_user))
                        user_present_in_lists.Add(a);
                });

                textBox2.Text = selected_user_ids[0] + "";



                var t1 = Task.Run(async () =>
                {
                    while (!V2LE_ListRefreshActive)
                        await Task.Delay(1);

                    while (V2LE_ListRefreshActive)
                        await Task.Delay(1);
                });

                tabControl1.SelectTab(1);

                await t1;

                var keys = V2LE_ViewingLists.Keys.ToArray();

                foreach (var id in user_present_in_lists)
                    Console.WriteLine(id);

                foreach (var id in keys)
                    Console.WriteLine(id);

                for (int i = 0; i < V2LE_ViewingLists.Count(); i++)
                    if (user_present_in_lists.Contains(keys[i]))
                    {
                        while (listBox5.Enabled == false)
                            await Task.Delay(100);

                        listBox5.SelectedIndex = i;
                        Refresh();
                        break;
                    }
            }
        }

        private async void button19_Click(object sender, EventArgs e)
        {
            //split selected user by cards

            var usel = ULAD_GetSelectedUsers()[0];

            var dbconn = await ARDBConnectionManager.default_manager.CheckOut();
            var cards = await DatabaseUtilities.GetAllCardsAssociatedWithUser(dbconn.Connection, usel);
            ARDBConnectionManager.default_manager.CheckIn(dbconn);

            if (cards.Length > 1)
            {
                if (MessageBox.Show(this, "This operation will generate " + cards.Length + " new user accounts from " + usel + ". Would you like to continue?", "Warning", MessageBoxButtons.OKCancel) == DialogResult.OK)
                    return;
                else
                    return;
            }
        }

        private async void launchEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //launch notepad to allow editing of the configuration file

            Visible = false;
            Refresh();

            bool changes = false;

            try
            {
                var hash_a = Utilities.GetFileHashAsBytes(MCv2Persistance.Instance.PersistanceFile);

                var notepad_proc = Process.Start("notepad.exe", MCv2Persistance.Instance.PersistanceFile);

                while (!notepad_proc.HasExited)
                    await Task.Delay(100);

                changes = !Utilities.CompareByteArrays(hash_a, Utilities.GetFileHashAsBytes(MCv2Persistance.Instance.PersistanceFile));


            }
            catch (Exception ex)
            {

            }

            if (changes)
                if (MessageBox.Show(this, "Would you like to reload the application with the modified options?", "Reload", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    // Restart program and run as admin
                    var exeName = Process.GetCurrentProcess().MainModule.FileName;
                    ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                    Process.Start(startInfo);
                    Close();
                    return;
                }

            Visible = true;
            Refresh();

        }

        private async void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            //main search control logic modifier drop down change


        }

        private async void useParsingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //enable parsing in search fields
            useParsingToolStripMenuItem.Checked = !useParsingToolStripMenuItem.Checked;

            cb2_seli = useParsingToolStripMenuItem.Checked ? 1 : 0;

            if (cb2_seli < 0)
                cb2_seli = 0;

            await FullUIRefresh();
        }

        private async void button21_Click(object sender, EventArgs e)
        {
            //add a new group

            TextBoxDialog tbd = new TextBoxDialog("Add New Group", "Description");
            var res = tbd.ShowDialog();
            if (res == DialogResult.OK)
            {
                var uuid = await GroupDBUtilities.AddUserGroup(tbd.TextBoxText);
                await ULAD_UserGroupsRefresh(uuid);
            }
        }

        private async void button23_Click(object sender, EventArgs e)
        {
            //edit an existing group
            if (comboBox1.SelectedIndex > 0)
            {
                var group_id = ULAD_ViewingGroups.Keys.ToArray()[comboBox1.SelectedIndex - 1];

                TextBoxDialog tbd = new TextBoxDialog("Edit Group", "Description");
                var res = tbd.ShowDialog();
                if (res == DialogResult.OK)
                {
                    await GroupDBUtilities.RenameUserGroup(group_id, tbd.TextBoxText);
                    await ULAD_UserGroupsRefresh(group_id);
                }
            }
        }

        private async void button24_Click(object sender, EventArgs e)
        {
            //delete a group
            var selected_index = comboBox1.SelectedIndex;

            if (selected_index > 0)
            {
                var group_id = ULAD_ViewingGroups.Keys.ToArray()[selected_index - 1];

                await GroupDBUtilities.DeleteUserGroup(group_id);

                if (selected_index > 1)
                    await ULAD_UserGroupsRefresh(ULAD_ViewingGroups.Keys.ToArray()[selected_index - 2]);
                else
                    await ULAD_UserGroupsRefresh();
            }
        }

        private async void button25_Click(object sender, EventArgs e)
        {
            //edit group contents
            var selected_index = comboBox1.SelectedIndex;

            if (selected_index > 0)
            {
                var group_id = ULAD_ViewingGroups.Keys.ToArray()[selected_index - 1];

                GroupEditorForm gef = new GroupEditorForm(group_id, ULAD_ViewingGroups[group_id]);
                var result = gef.ShowDialog(this);

                await FullUIRefresh();
            }
        }

        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //selected group index changed
            if (ignore_cb1_event)
                return;

            var current_selection = comboBox1.SelectedIndex;

            if (current_selection == last_selected)
                return;
            else
                last_selected = current_selection;

            var cfg = MCv2Persistance.Instance.Config;

            if (comboBox1.SelectedIndex < 1)
                cfg.UIConfiguration.SelectedGroup = 0;
            else
                cfg.UIConfiguration.SelectedGroup = ULAD_ViewingGroups.ElementAt(comboBox1.SelectedIndex - 1).Key;

            MCv2Persistance.Instance.Config = cfg;

            if (comboBox1.SelectedIndex > 0)
            {
                button23.Enabled = true;
                button24.Enabled = true;
                button25.Enabled = true;
            }
            else
            {
                button23.Enabled = false;
                button24.Enabled = false;
                button25.Enabled = false;
            }

            await FullUIRefresh();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
                button1.Enabled = true;
            else
                button1.Enabled = false;
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex > -1)
                button2.Enabled = true;
            else
                button2.Enabled = false;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            //remove from assigned cards

            button2.Enabled = false;

            using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
            {
                await sqlconn.OpenAsync();

                using (MySqlCommand cmdName = new MySqlCommand("update `cards` set user_id=@user_id where uid=@uid", sqlconn))
                {
                    cmdName.Parameters.AddWithValue("@user_id", 0);
                    cmdName.Parameters.AddWithValue("@uid", display_uids_in_short_form ? BaseConverter.DecodeFromString((string)listBox2.SelectedItem) : listBox2.SelectedItem);
                    await cmdName.ExecuteNonQueryAsync();
                }
            }

            await CardLookupAndDisplay(ULAD_GetSelectedUsers(), textBox3.Text.Trim(), true);
        }

        private async void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //database import menu item
            new ImportForm().ShowDialog();

            await FullUIRefresh();
        }

        private async void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //database export menu item
            new ExportForm().ShowDialog();
        }

        private void connectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //edit database connection properties
            DatabaseConnectionEditor dbconneditor = new DatabaseConnectionEditor(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties, true);
            var res = dbconneditor.ShowDialog();
            if (res == DialogResult.OK)
                MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties = dbconneditor.DBConnProp;
            else
                if (res == DialogResult.Abort)
            {
                Close();
            }

        }

        private async void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //erase all tables in the database

            //verify the database password first
            TextBoxDialog tbd = new TextBoxDialog("Enter Password", "Please enter the current database password.");
            if (tbd.ShowDialog(this) == DialogResult.OK)
                if (tbd.TextBoxText == MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.Password)
                {
                    ProgressDialog pgd = new ProgressDialog("Reset Database");
                    pgd.Show();

                    pgd.LabelText = "Reading Table List";

                    using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                    {
                        await sqlconn.OpenAsync();

                        List<string> table_names = new List<string>();

                        using (MySqlCommand sqlcmd = new MySqlCommand("show tables", sqlconn))
                        using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                            while (await reader.ReadAsync())
                                table_names.Add(reader.GetString(0));

                        pgd.Step();
                        pgd.Reset();
                        pgd.LabelText = "Dropping Tables";

                        if (table_names.Count() > -1)
                            pgd.Maximum = table_names.Count();

                        foreach (string table_name in table_names)
                            using (MySqlCommand sqlcmd = new MySqlCommand("drop table `" + table_name + "`", sqlconn))
                            {
                                await sqlcmd.ExecuteNonQueryAsync();
                                pgd.Step();
                            }


                    }

                    pgd.Dispose();
                }

            await FullUIRefresh();
        }

        private void checkedListBox1_KeyUp(object sender, KeyEventArgs e)
        {
            var checked_indicies = checkedListBox1.CheckedIndices;

            if (checked_indicies.Count == 0)
            {
                textBox6.Text = "";
                //checkBox1.Focus();
                return;
            }

            bool[] checked_states = new bool[7];
            foreach (int index in checked_indicies)
                checked_states[index] = true;

            textBox6.Text = ListEntryUtilities.ConvertBoolToDOWString(checked_states);

            textBox6.BackColor = Color.LightCyan;
        }


        private async void DatabaseManagerForm_Shown(object sender, EventArgs e)
        {
            watcher = new TableWatcher("users");
            watcher.tablechangeevent += (a, b) =>
            {
                Invoke((MethodInvoker)(() =>
                {
                    refreshToolStripMenuItem.BackColor = Color.AliceBlue;
                    Refresh();
                }));
            };
            watcher.Begin();

            if (MCv2Persistance.Instance.Config.UIConfiguration.WarnIfNotAdministrator && !IsAdministrator())
            {
                var res = MessageBox.Show(this, "Some program functions will be unavailable without administrative permissions." + Environment.NewLine
                    + "Would you like to reload with elevated permissions?", "Warning", MessageBoxButtons.YesNo);

                if (res == DialogResult.Yes)
                    RunAsAdministrator();
            }

            await FullUIRefresh();
        }

        private async void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (tabControl1.SelectedTab.Name == "tabPage2" && e.KeyCode == Keys.Enter)
            {
                string tb1 = textBox3.Text.Trim();

                await CardLookupAndDisplay(ULAD_GetSelectedUsers(), tb1, false);

                textBox3.Focus();
            }
        }

        private async void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //refresh menu item

            await FullUIRefresh();
        }


        private async void button1_Click(object sender, EventArgs e)
        {
            //assign card to selected user

            button1.Enabled = false;

            Refresh();

            var selected_users = ULAD_GetSelectedUsers();

            if (selected_users.Count() == 1)
            {
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                using (MySqlCommand cmdName = new MySqlCommand("update `cards` set user_id=@user_id where uid=@uid", sqlconn.Connection))
                {
                    cmdName.Parameters.AddWithValue("@user_id", selected_users[0]);
                    cmdName.Parameters.AddWithValue("@uid", display_uids_in_short_form ? BaseConverter.DecodeFromString((string)listBox1.SelectedItem) : listBox1.SelectedItem);
                    await cmdName.ExecuteNonQueryAsync();
                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                await CardLookupAndDisplay(selected_users[0], textBox3.Text.Trim(), true);
            }
        }

        private async void backupManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //show backup manager
            new BackupManagerForm().ShowDialog();

            await FullUIRefresh();
        }

        private void textBox3_MouseEnter(object sender, EventArgs e)
        {
            textBox3.Focus();
        }

        private void textBox1_MouseEnter(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        private void textBox2_MouseEnter(object sender, EventArgs e)
        {
            textBox2.Focus();
        }



        private void checkedListBox1_MouseUp(object sender, MouseEventArgs e)
        {
            var checked_indicies = checkedListBox1.CheckedIndices;

            if (checked_indicies.Count == 0)
            {
                textBox6.Text = "";
                //checkBox1.Focus();
                return;
            }

            bool[] checked_states = new bool[7];
            foreach (int index in checked_indicies)
                checked_states[index] = true;

            textBox6.Text = ListEntryUtilities.ConvertBoolToDOWString(checked_states);

            textBox6.BackColor = Color.LightCyan;
        }


        private async void devsrvstartstop_Clicked(object sender, EventArgs e)
        {
            if (ServiceManager.isDeviceServerServiceRunning())
                await ServiceManager.StopDeviceServerService();
            else
                await ServiceManager.StartDeviceServerService();
        }

        private void statusToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            //device server status dropdown opening
            //check to see if the service is installed and if so check to see if the service is running and give the
            //user an option to start or stop the service


        }


        private async void button7_Click(object sender, EventArgs e)
        {
            //add new user

            UserEditorForm uef = new UserEditorForm("Add New User");
            if (uef.ShowDialog(this) == DialogResult.OK)
            {
                string name = uef.NameText;
                string desc = uef.DescriptionText;
                UInt64 new_uid = 0;

                ProgressDialog pgd = new ProgressDialog("Adding New User");
                pgd.Show(this);

                pgd.Maximum = 3;

                pgd.LabelText = "Opening Database Connection";

                using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                {
                    await sqlconn.OpenAsync();

                    pgd.Step();
                    await Task.Delay(100);
                    pgd.LabelText = "Generating Unique User ID";

                    new_uid = await DatabaseUtilities.GenerateUniqueUserID(sqlconn);

                    pgd.Step();
                    await Task.Delay(100);
                    pgd.LabelText = "Inserting New User";

                    await DatabaseUtilities.AddNewUserToDatabase(sqlconn, new_uid, name, desc);

                    pgd.Step();
                    await Task.Delay(100);
                }

                pgd.Dispose();

                textBox1.Text = new_uid.ToString();
            }
        }

        private async void importToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            //import

            //database import menu item
            new ImportForm().ShowDialog(this);

            await FullUIRefresh();
        }


        private async void installremoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //device server install remove menuitem clicked

            if (ServiceManager.isDeviceServerServiceInstalled())
            {
                try
                {
                    await ServiceManager.StopDeviceServerService();
                }
                catch (Exception ex) { }

                await ServiceManager.UninstallDeviceServerService();
            }
            else
            {
                await ServiceManager.InstallDeviceServerService();

                try
                {
                    await ServiceManager.StartDeviceServerService();
                }
                catch (Exception ex) { }
            }
        }

        private async void button8_Click(object sender, EventArgs e)
        {
            //edit user

            var selected_users = ULAD_GetSelectedUsers();

            if (selected_users.Count() != 1)
                return;

            string sel_name = "";
            string sel_desc = "";

            DataTable l_dt = (DataTable)dataGridView1.DataSource;

            for (int i = 0; i < l_dt.Rows.Count; i++)
            {
                if (l_dt.Rows[i].Field<UInt64>("user_id") == selected_users[0])
                {
                    sel_name = l_dt.Rows[i].Field<string>("name");
                    sel_desc = l_dt.Rows[i].Field<string>("description");

                    break;
                }
            }

            UserEditorForm uef = new UserEditorForm("Editing: " + selected_users[0], sel_name, sel_desc);
            if (uef.ShowDialog(this) == DialogResult.OK)
            {
                string name = uef.NameText;
                string desc = uef.DescriptionText;

                ProgressDialog pgd = new ProgressDialog("Updating User Entry");
                pgd.Show(this);

                pgd.Maximum = 2;

                pgd.LabelText = "Opening Database Connection";

                using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                {
                    await sqlconn.OpenAsync();

                    pgd.Step();
                    await Task.Delay(100);

                    pgd.LabelText = "Updating Row";

                    await DatabaseUtilities.EditUserInDatabase(sqlconn, selected_users[0], name, desc);
                }

                pgd.Step();
                await Task.Delay(100);

                pgd.Dispose();

                textBox1.Text = selected_users[0].ToString();
            }
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            //delete user

            var selected_users = ULAD_GetSelectedUsers();

            if (selected_users.Count() != 1)
                return;

            ProgressDialog pgd = new ProgressDialog("Deleting User");
            pgd.Show(this);
            pgd.Maximum = 4;

            pgd.LabelText = "Opening Database Connection";

            using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
            {
                await sqlconn.OpenAsync();

                pgd.Step();
                await Task.Delay(100);

                pgd.LabelText = "Purging User From Access Control Lists";

                await DatabaseUtilities.PurgeUserFromV2Lists(sqlconn, selected_users[0]);

                pgd.Step();
                await Task.Delay(100);

                pgd.LabelText = "Removing User Card Associations";

                await DatabaseUtilities.PurgeUserFromCardsTable(sqlconn, selected_users[0]);

                pgd.Step();
                await Task.Delay(100);

                pgd.LabelText = "Deleting User Entry";

                await DatabaseUtilities.DropUser(sqlconn, selected_users[0]);

                pgd.Step();
                await Task.Delay(100);
            }

            pgd.Dispose();

            await UserLookupAndDisplay(textBox1.Text.Trim(), true);
        }

        private async void encodeUIDsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //set encode uids flag

            if (encodeUIDsToolStripMenuItem.Checked)
                encodeUIDsToolStripMenuItem.Checked = false;
            else
                encodeUIDsToolStripMenuItem.Checked = true;

            display_uids_in_short_form = encodeUIDsToolStripMenuItem.Checked;

            var config = MCv2Persistance.Instance.Config;
            config.UIConfiguration.EncodeDisplayedUIDsFlag = display_uids_in_short_form;
            MCv2Persistance.Instance.Config = config;

            await FullUIRefresh();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }

        private async void button11_Click(object sender, EventArgs e)
        {
            //merge button

            List<UInt64> selected_user_ids = new List<UInt64>();

            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                selected_user_ids.Add((UInt64)row.Cells[0].Value);

            UserEditorForm uef = new UserEditorForm("Merging " + selected_user_ids.Count + " User(s)",
                (string)dataGridView1.SelectedRows[0].Cells[1].Value,
                (string)dataGridView1.SelectedRows[0].Cells[2].Value);

            if (uef.ShowDialog(this) == DialogResult.OK)
            {
                string name = uef.NameText;
                string desc = uef.DescriptionText;

                ProgressDialog pgd = new ProgressDialog("Merging " + selected_user_ids.Count + " User(s)");
                pgd.Show(this);
                pgd.SetMarqueeStyle();


                ulong new_user_id = 0;
                using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                {
                    await sqlconn.OpenAsync();

                    new_user_id = await DatabaseUtilities.MergeUsers(sqlconn, selected_user_ids, name, desc);
                }

                pgd.Dispose();

                await FullUIRefresh();

                textBox1.Text = new_user_id.ToString();
            }
        }

        private async void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                textBox2.BackColor = Color.LightCyan;
                Refresh();

                ResetListEntryEditor();
                button3.Enabled = false;

                var tasks = new Task[] { V2LE_UA_ListUsers_Refresh(), V2LE_UA_UserLookup_Refresh() };
                await Task.WhenAll(tasks);

                textBox2.BackColor = SystemColors.Window;
                Refresh();
                textBox2.Focus();
            }
        }

        private async void button12_Click(object sender, EventArgs e)
        {
            bool requires_refresh = !(textBox2.Text.Trim() == "");

            textBox2.Text = "";

            if (requires_refresh)
            {
                textBox2.BackColor = Color.LightCyan;
                Refresh();

                ResetListEntryEditor();
                button3.Enabled = false;

                await V2LE_UA_ListUsers_Refresh();
                textBox2.BackColor = SystemColors.Window;
                Refresh();
                textBox2.Focus();
            }
        }

        private async void button13_Click(object sender, EventArgs e)
        {
            bool requires_refresh = !(textBox2.Text.Trim() == "");

            textBox2.Text = "";

            if (requires_refresh)
            {
                textBox2.BackColor = Color.LightCyan;
                Refresh();
                await V2LE_UA_UserLookup_Refresh();
                textBox2.BackColor = SystemColors.Window;
                Refresh();
                textBox2.Focus();
            }
        }

        private async void button14_Click(object sender, EventArgs e)
        {
            //v2le add list button
            using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
            {
                await sqlconn.OpenAsync();

                var new_list_uid = await DatabaseUtilities.GenerateUniqueListUID(sqlconn);

                await DatabaseUtilities.CreateV2ACList(sqlconn, new_list_uid, "_new_list");
            }

            await V2LE_FullRefresh();
        }

        private async void button15_Click(object sender, EventArgs e)
        {
            //v2le rename list button
            TextBoxDialog tbd = new TextBoxDialog("Editing:" + V2LE_ViewingLists.Keys.ElementAt(listBox5.SelectedIndex), "List Alias:");
            tbd.TextBoxText = (string)listBox5.SelectedItem;
            if (tbd.ShowDialog() == DialogResult.OK)
            {
                using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                {
                    await sqlconn.OpenAsync();

                    await DatabaseUtilities.SetV2ACListAlias(sqlconn, V2LE_ViewingLists.Keys.ElementAt(listBox5.SelectedIndex), tbd.TextBoxText);
                }

                await V2LE_RefreshListSelection();
            }
        }

        private async void button16_Click(object sender, EventArgs e)
        {
            //v2le delete list button

            using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
            {
                await sqlconn.OpenAsync();

                await DatabaseUtilities.DeleteV2ACList(sqlconn, V2LE_ViewingLists.Keys.ElementAt(listBox5.SelectedIndex));
            }

            await V2LE_FullRefresh();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            textBox1.Text = textBox5.Text;

            tabControl1.SelectedIndex = 0;
        }

        private void translatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new NUIDEncodingTranslatorForm().Show(this);
        }

        private void button18_Click(object sender, EventArgs e)
        {

        }

        private async void button20_Click(object sender, EventArgs e)
        {
            //verify vist

            if (listBox5.SelectedIndex == -1)
                return;



            var selected_list = V2LE_ViewingLists.Keys.ElementAt(listBox5.SelectedIndex);
            UInt64 selected_device = 0;

            var nodes = treeView1.Nodes[0].Nodes;

            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].IsSelected)
                {
                    selected_device = V2LE_ViewingDevices.Keys.ElementAt(i);
                    break;
                }

            TCPConnectionProperties tcpconnprop = null;

            using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
            {
                await sqlconn.OpenAsync();

                tcpconnprop = await DatabaseUtilities.GetDevicePropertiesFromDatabase(sqlconn, selected_device);
            }

            ProgressDialog pgd = new ProgressDialog("Verifying List on " + V2LE_ViewingDevices[selected_device] + " Controller");
            pgd.Show(this);

            var res = await MasterControllerV2Utilities.VerifyList(selected_list, selected_device, pgd);

            pgd.Dispose();

            if (((bool)res[0] && MCv2Persistance.Instance.Config.UIConfiguration.ShowDialogOnMCV2OfflineControllerInteractionSuccess) ||
                (!((bool)res[0]) && MCv2Persistance.Instance.Config.UIConfiguration.ShowDialogOnMCV2OfflineControllerInteractionFailure))
                MessageBox.Show(V2LE_ViewingDevices[selected_device] + ": " + (string)res[1]);
        }

        private async void backupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //backup database
            string file_name = null;

            var cfg = MCv2Persistance.Instance.Config;
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Supported Extentions (*.db2bak)|*.db2bak";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var now_is = DateTime.Now;

                sfd.FileName = cfg.DatabaseConfiguration.DatabaseConnectionProperties.Hostname.Replace('.', '_') +
                    "_" + cfg.DatabaseConfiguration.DatabaseConnectionProperties.Schema + "_" + now_is.Year + "_" +
                    now_is.Month + "_" + now_is.Day + "_" + now_is.ToFileTimeUtc().ToString();

                if (sfd.ShowDialog() == DialogResult.OK)
                    file_name = sfd.FileName;
            }

            if (file_name == null)
                return;

            ProgressDialog pgd = new ProgressDialog("Database Backup");
            pgd.Show();
            pgd.SetMarqueeStyle();

            pgd.LabelText = "Saving backup to " + file_name;
            await DBBackupAndRestore.Backup(file_name);

            pgd.Dispose();
        }

        private async void restoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //restore database
            List<string> files_to_import = new List<string>();

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Filter = "Supported Extentions (*.db2bak)|*.db2bak";
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                ofd.FileName = "";

                if (ofd.ShowDialog() == DialogResult.OK)
                    files_to_import.AddRange(ofd.FileNames);
            }

            if (files_to_import.Count() == 0)
                return;

            ProgressDialog pgd = new ProgressDialog("Restoring Database");
            pgd.Show();
            pgd.SetMarqueeStyle();

            foreach (var file in files_to_import)
            {
                pgd.LabelText = "Processing " + file;
                await DBBackupAndRestore.Restore(file);
            }

            pgd.Dispose();

            await FullUIRefresh();
        }


        private async void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv1_cts != null)
            {
                dgv1_cts.Cancel();

                while (dgv1_cc != true)
                    await Task.Delay(1);

                //dgv1_cts.Dispose();
                dgv1_cts = null;
                dgv1_cc = false;
            }

            dgv1_cts = new CancellationTokenSource();

            await Task.Run(async () =>
            {
                string tbt = "";
                int selected_row_count = dataGridView1.SelectedRows.Count;

                if (selected_row_count == 1)
                    await Task.Run(async () =>
                    {
                        var usel = ULAD_GetSelectedUsers()[0];

                        var dbconn = await ARDBConnectionManager.default_manager.CheckOut();
                        var cards = await DatabaseUtilities.GetAllCardsAssociatedWithUser(dbconn.Connection, usel);
                        ARDBConnectionManager.default_manager.CheckIn(dbconn);


                        Invoke((MethodInvoker)(() =>
                        {
                            if (cards.Length > 1)
                                button19.Enabled = true;
                            else
                                button19.Enabled = false;
                        }));
                    });

                Invoke((MethodInvoker)(() =>
                {

                    if (selected_row_count > 1)
                    {
                        //multiple rows selected

                        button11.Enabled = true;
                        button1.Enabled = false;
                        button2.Enabled = false;
                        button19.Enabled = false;
                        button13.Enabled = true;
                        button18.Enabled = false;

                        Refresh();

                        //return;
                    }
                    else
                        if (selected_row_count > 0)
                    {
                        //one row selected

                        button11.Enabled = false;
                        button13.Enabled = true;
                        button18.Enabled = true;


                    }
                    else
                    {
                        //no rows are selected

                        button11.Enabled = false;
                        button19.Enabled = false;
                        button13.Enabled = false;
                        button18.Enabled = false;
                    }

                    tbt = textBox3.Text.Trim();

                    foreach (Control c in card_lookup_controls)
                        c.Enabled = false;

                    Refresh();
                }));

                await CardLookupAndDisplay(ULAD_GetSelectedUsers(), tbt, true, dgv1_cts);

            }, dgv1_cts.Token);

            if (dgv1_cts != null)
            {
                dgv1_cts.Dispose();
                dgv1_cts = null;
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            //card_update_holdoff_timer.Stop();
            //card_update_holdoff_timer.Start();
        }

        private async void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            //await CardLookupAndDisplay(selected_user, textBox3.Text.Trim(), true);
        }

        private async void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            //await CardLookupAndDisplay(textBox3.Text.Trim());
        }

        private async void textBox8_TextChanged(object sender, EventArgs e)
        {
            await V2LE_RefreshListSelection();
        }

        //list selection changed
        private async void listBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            await V2LE_RefreshUserAssignment();
        }

        private async void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox3.SelectedIndex > -1 && listBox3.SelectedIndices.Count < 2)
            {
                button3.Enabled = true;

                Refresh();

                await V2LE_RefreshListEntryEditor();
            }
            else
            {
                if (listBox3.SelectedIndices.Count < 2)
                    button3.Enabled = false;
                else
                    button3.Enabled = true;

                Refresh();

                ResetListEntryEditor();
            }

            Invoke((MethodInvoker)(() => { Refresh(); }));
        }

        private void dateTimePicker1_KeyUp(object sender, KeyEventArgs e)
        {
            //start time
            DTPCheck0();
        }

        private void dateTimePicker1_MouseUp(object sender, MouseEventArgs e)
        {
            //start time
            DTPCheck0();
        }

        private void dateTimePicker2_KeyUp(object sender, KeyEventArgs e)
        {
            //end time
            DTPCheck1();
        }

        private void dateTimePicker2_MouseUp(object sender, MouseEventArgs e)
        {
            //end time
            DTPCheck1();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //v2le list entry editor apply button
            button5.Enabled = false;
            UInt64 list_id = V2LE_ViewingLists.ElementAt(listBox5.SelectedIndex).Key;
            UInt64 user_id = V2LE_UsersInCurrentList.ElementAt(listBox3.SelectedIndex).Key;
            byte check_box_state = (byte)(checkBox2.Checked ? 1 : 0);
            string entry_days = textBox6.Text.Trim();
            string entry_times = textBox7.Text.Trim();

            var pgd = new ProgressDialog("Applying Changes");
            pgd.Show();

            Task.Run(async () =>
            {
                pgd.LabelText = "Checking Out DB Connection";
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                pgd.Step();
                await Task.Delay(100);
                pgd.Reset();

                pgd.LabelText = "Updating List Row";
                await DatabaseUtilities.SetV2ListRow(sqlconn.Connection, list_id, user_id, new Object[] { entry_days, entry_times, check_box_state });
                pgd.Step();
                await Task.Delay(100);

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                Invoke((MethodInvoker)(() =>
                {
                    pgd.Dispose();
                    button5.Enabled = true;
                    Refresh();
                }));
            });
        }

        private void listBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            int lb4_index = listBox4.SelectedIndex;

            if (lb4_index > -1)
                button4.Enabled = true;
            else
                button4.Enabled = false;
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            //remove selected user from selected list

            UInt64 list_id = V2LE_ViewingLists.ElementAt(listBox5.SelectedIndex).Key;
            //UInt64 user_id = V2LE_UsersInCurrentList.ElementAt(listBox3.SelectedIndex).Key;

            UInt64[] selected_users = V2LE_UsersInCurrentList.Keys.Where(x => listBox3.SelectedIndices.Contains(V2LE_UsersInCurrentList.Keys.ToList().IndexOf(x))).ToArray();

            ProgressDialog pgd = null;
            pgd = new ProgressDialog("Removing Selected User(s) From List");
            pgd.Show(this);

            pgd.Maximum = selected_users.Length + 1;

            pgd.LabelText = "Opening Database Connection";

            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

            pgd.Step();

            foreach (var user_id in selected_users)
            {
                pgd.LabelText = "Removing User " + (selected_users.ToList().IndexOf(user_id) + 1) + " of " + selected_users.Length;

                await DatabaseUtilities.RemoveUserFromV2List(sqlconn.Connection, list_id, user_id);

                pgd.Step();
            }

            ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            await Task.Delay(100);

            pgd.Dispose();

            await V2LE_RefreshUserAssignment();

            await Task.Delay(100);
            listBox4.ClearSelected();
            await Task.Delay(900);
            listBox4.ClearSelected();


            Refresh();

            foreach (var user_id in selected_users)
                if (V2LE_UsersNotInCurrentList.Keys.ToList().Contains(user_id))
                    listBox4.SetSelected(V2LE_UsersNotInCurrentList.Keys.ToList().IndexOf(user_id), true);

            Refresh();
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            //add selected user(s) to selected list

            UInt64 list_id = V2LE_ViewingLists.ElementAt(listBox5.SelectedIndex).Key;
            //UInt64 user_id = V2LE_UsersNotInCurrentList.ElementAt(listBox4.SelectedIndex).Key;

            UInt64[] selected_users = V2LE_UsersNotInCurrentList.Keys.Where(x => listBox4.SelectedIndices.Contains(V2LE_UsersNotInCurrentList.Keys.ToList().IndexOf(x))).ToArray();

            //select user in lb3
            if (selected_users.Length > 1)
                listBox3.SelectedIndexChanged -= listBox3_SelectedIndexChanged;

            ProgressDialog pgd = null;
            pgd = new ProgressDialog("Adding Selected User(s) To List");
            pgd.Show(this);

            pgd.Maximum = selected_users.Length + 1;

            pgd.LabelText = "Opening Database Connection";

            using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
            {
                await sqlconn.OpenAsync();

                foreach (var user_id in selected_users)
                {
                    pgd.LabelText = "Adding User " + (selected_users.ToList().IndexOf(user_id) + 1) + " of " + selected_users.Length;

                    await DatabaseUtilities.AddUserToV2List(sqlconn, list_id, user_id);

                    pgd.Step();
                }
            }

            await Task.Delay(100);

            pgd.Dispose();

            await V2LE_RefreshUserAssignment();

            await Task.Delay(100);
            listBox3.ClearSelected();
            await Task.Delay(900);
            listBox3.ClearSelected();

            Refresh();

            foreach (var user_id in selected_users)
                if (V2LE_UsersInCurrentList.Keys.Contains(user_id))
                    listBox3.SetSelected(V2LE_UsersInCurrentList.Keys.ToList().IndexOf(user_id), true);

            if (selected_users.Length > 1)
            {
                listBox3.SelectedIndexChanged += listBox3_SelectedIndexChanged;
                V2LE_DisableAllEntryFields();
            }

            button3.Enabled = true;

            Refresh();
        }

        private void printLabelSetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new PrintLabelSetForm().ShowDialog();
        }

    }
}
