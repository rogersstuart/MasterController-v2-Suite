using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Security.Principal;
using System.Security.Permissions;
using MCICommon;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using UIElements;

namespace MasterControllerInterface
{
    public partial class MCIv2Form : Form
    {
        private MySqlDataAdapter mySqlDataAdapter;

        private DataTable dt = new DataTable("results");

        private string[] column_names = { "user_id", "name", "description" };

        string previous_search = null; //remember the last search to prevent spaces from causing an additional search

        string previous_card_search = null; //remember the last search to prevent spaces from causing an additional search

        private System.Timers.Timer update_holdoff_timer = new System.Timers.Timer(MCv2Persistance.Config.UIConfiguration.UserLookupHoldoff);

        private List<Control> card_lookup_controls = new List<Control>();

        private volatile bool display_uids_in_short_form = true;
        private volatile bool sync_time_after_uploads = true;

        private Dictionary<UInt64, string> V2LE_ViewingLists = null;
        private Dictionary<UInt64, string> V2LE_UsersInCurrentList = null;
        private Dictionary<UInt64, string> V2LE_UsersNotInCurrentList = null;
        private Dictionary<UInt64, string> V2LE_ViewingDevices = null;

        private volatile bool V2LE_RefreshUserAssignment_Busy = false;

        private int cb2_seli = 1;

        int last_selected = -1;

        //private UInt64 selected_user = 0;

        Object ui_access_lock = new Object();

        CancellationTokenSource rct = null;
        CancellationTokenSource dgv1_cts = null;

        private TableWatcher watcher;

        public MCIv2Form()
        {
            var cfg = MCv2Persistance.Config;
            var bcfg = cfg.BackupConfiguration;

            if (bcfg.EnableAutoBackup)
            {
                ProgressDialog pgd = new ProgressDialog("Database Backup");
                pgd.Show();
                pgd.SetMarqueeStyle();

                var now_is = DateTime.Now;
                var backup_filename = "auto_backup_" + cfg.DatabaseConfiguration.DatabaseConnectionProperties.Hostname.Replace('.', '_') + "_" + cfg.DatabaseConfiguration.DatabaseConnectionProperties.Schema + "_" + now_is.Year + "_" + now_is.Month + "_" + now_is.Day + "_" + now_is.ToFileTimeUtc().ToString();

                pgd.LabelText = "Saving backup to " + backup_filename + ".db2bak";

                DBBackupAndRestore.Backup("./backups/" + backup_filename + ".db2bak").Wait();

                pgd.Dispose();

                bcfg.LastAutoBackupTimestamp = DateTime.Now;
            }

            if (bcfg.EnableBackupNag && (DateTime.Now - bcfg.LastNagBackupTimestamp) > bcfg.BackupNagInterval)
            {
                var res = MessageBox.Show(this, "It's been " + (DateTime.Now - bcfg.LastNagBackupTimestamp).TotalDays + " days since the last local database backup." +Environment.NewLine
                    + "Would you like to perform a backup now?", "Warning", MessageBoxButtons.YesNo);

                if (res == DialogResult.Yes)
                {
                    //backup database
                    string file_name = null;

                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "Supported Extentions (*.db2bak)|*.db2bak";
                        sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        var now_is = DateTime.Now;
                        sfd.FileName = cfg.DatabaseConfiguration.DatabaseConnectionProperties.Hostname.Replace('.', '_') + "_" + cfg.DatabaseConfiguration.DatabaseConnectionProperties.Schema + "_" + now_is.Year + "_" + now_is.Month + "_" + now_is.Day + "_" + now_is.ToFileTimeUtc().ToString();

                        if (sfd.ShowDialog() == DialogResult.OK)
                            file_name = sfd.FileName;
                    }

                    if (file_name != null)
                    {
                        ProgressDialog pgd = new ProgressDialog("Database Backup");
                        pgd.Show();
                        pgd.SetMarqueeStyle();

                        pgd.LabelText = "Saving backup to " + file_name;
                        DBBackupAndRestore.Backup(file_name).Wait();

                        pgd.Dispose();

                        bcfg.LastNagBackupTimestamp = DateTime.Now;
                    }
                }
            }

            MCv2Persistance.Config = cfg;

            InitializeComponent();

            //context menu
            toolStripMenuItem2.Click += (a, b) => {
                Clipboard.SetText(new Func<string>(() =>
                {
                    var str = "";
                    var items = listBox2.Items.Cast<string>();
                    foreach (var item in items)
                        str += item + Environment.NewLine;
                    return str;
                }).Invoke());};

            DoubleBuffered = true;

            var version_strings = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
            var version_string = "MCI v" + version_strings[0] + "." + version_strings[1] + " Prerelease " + version_strings[2] + "." + version_strings[3];
            Text = version_string;

            //Set double buffering on the gridview using reflection and the bindingflags enum.
            typeof(DataGridView).InvokeMember("DoubleBuffered", BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.SetProperty, null,
            dataGridView1, new object[] { true });

            card_lookup_controls.Add(listBox1);
            card_lookup_controls.Add(textBox3);
            card_lookup_controls.Add(button1);
            card_lookup_controls.Add(listBox2);
            card_lookup_controls.Add(button2);
            card_lookup_controls.Add(button22);

            update_holdoff_timer.Elapsed += async (s, ea) =>
            {
                rct = new CancellationTokenSource();

                update_holdoff_timer.Stop();

                //try
                //{
                    await Task.Run(async () =>
                    {
                        string tb0 = "", tb1 = "";

                        Invoke((MethodInvoker)(() =>
                        {
                            tb0 = textBox1.Text.Trim();
                            tb1 = textBox3.Text.Trim();

                            textBox3.Text = "";

                            textBox1.BackColor = Color.LightCyan;

                            Refresh();
                        }));

                        //RemoveDataGridView1Events();
                        await UserLookupAndDisplay(tb0, true, rct);
                        //RestoreDataGridView1Events();

                        //if there are rows displayed then refresh card lookup and display

                        //var selected_users = ULAD_GetSelectedUsers();
                        //if(selected_users.Count() > 0)
                        //{
                        //await CardLookupAndDisplay(selected_user, tb1, true);
                        //}

                        Invoke((MethodInvoker)(() =>
                            {
                                textBox1.BackColor = SystemColors.Window;

                                Refresh();
                            }));

                    }, rct.Token);
                //}
                //catch (Exception ex) { }

                rct.Dispose();
                rct = null;
            };

            V2LE_ResetAll();
        }

        //helps to prevent flicker
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        
        private async Task CardLookupAndDisplay(ulong user_id, string query, bool force, CancellationTokenSource lcts = null)
        {
            await CardLookupAndDisplay(new List<ulong> { user_id }, query, force, lcts);
        }

        private async Task CardLookupAndDisplay(List<ulong> user_id, string query, bool force, CancellationTokenSource lcts = null)
        {
            if (lcts == null)
                lcts = new CancellationTokenSource();

            dgv1_cc = false;

            try
            {
                await Task.Run(async () =>
                {
                    //selected index description
                    //0 == unassigned cards
                    //1 == all cards
                    int selected_index = 0;

                    Invoke((MethodInvoker)(() =>
                    {
                        foreach (Control c in card_lookup_controls)
                            c.Enabled = false;

                        Refresh();
                    }));

                    if (display_uids_in_short_form)
                        {
                            if (query != "")
                                if (BaseConverter.TryParseEncodedString(query))
                                    query = BaseConverter.DecodeFromString(query).ToString();
                        }

                    var cards = await DatabaseUtilities.GetAllCards(lcts);

                    //unassigned cards filtered by query
                    List<ulong> filtered_uids = cards.Where(x => x.UserAssignment == 0 && (query == "" || x.CardNUID.ToString().Contains(query))).Select(y => y.CardNUID).ToList();
                        filtered_uids.Sort();
                        List<string> formatted_filtered_uids = filtered_uids.Select(x => display_uids_in_short_form ? BaseConverter.EncodeFromBase10(x) : x.ToString()).ToList();

                    //assigned cards filtered by query
                    List<ulong> assigned_uids = new List<ulong>();

                    foreach (var id in user_id)
                        assigned_uids.AddRange(cards.Where(x => x.UserAssignment == id).Select(y => y.CardNUID));

                    List<ulong> filtered_assigned_uids = assigned_uids.Where(x => query == "" || x.ToString().Contains(query)).ToList();
                    filtered_assigned_uids.Sort();
                    List<string> formatted_filtered_assigned_uids = filtered_assigned_uids.Select(x => display_uids_in_short_form ? BaseConverter.EncodeFromBase10(x) : x.ToString()).ToList();

                    

                    AutoCompleteStringCollection acsc_a = new AutoCompleteStringCollection();
                    acsc_a.AddRange(formatted_filtered_uids.ToArray());
                    acsc_a.AddRange(formatted_filtered_assigned_uids.ToArray());

                    Invoke((MethodInvoker)(() =>
                    {
                        listBox1.DataSource = formatted_filtered_uids;
                        textBox3.AutoCompleteCustomSource = acsc_a;

                        if (listBox1.Items.Count > 0)
                            button1.Enabled = true;

                        /////

                        listBox2.DataSource = formatted_filtered_assigned_uids;

                        if (listBox2.Items.Count > 0)
                            button2.Enabled = true;

                        ////

                        foreach (Control c in card_lookup_controls)
                                if (c.Name != "button1" && c.Name != "button2")
                                    c.Enabled = true;

                         Refresh();
                    }));

                    if (formatted_filtered_uids.Count() == 0 && formatted_filtered_assigned_uids.Count() == 0 && query != "" && cards.Select(x => x.CardNUID).Contains(Convert.ToUInt64(query)) )
                    {
                        var result = MessageBox.Show("The query had no results but the card was found. It's assigned to a user outside of the query parameters. Would you like to break the association and return the card to the pool?" +
                            " To determine the user association use the main search field to lookup the card. Clicking cancel will clear the card lookup field and refresh.", "Warning", MessageBoxButtons.OKCancel);
                        if(result == DialogResult.OK)
                        {
                            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();
                            
                            using (MySqlCommand cmdName = new MySqlCommand("update `cards` set user_id=@user_id where uid=@uid", sqlconn.Connection))
                            {
                                cmdName.Parameters.AddWithValue("@user_id", 0);
                                cmdName.Parameters.AddWithValue("@uid", Convert.ToUInt64(query));
                                await cmdName.ExecuteNonQueryAsync();
                            }

                            ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                            await CardLookupAndDisplay(user_id, textBox3.Text.Trim(), true, lcts);

                            return;
                        }
                    }

                }, lcts.Token);
            }
            catch (TaskCanceledException tcex) {  }

            dgv1_cc = true;
        }

       

        private async Task FullUIRefresh()
        {
            var config = MCv2Persistance.Config;

            encodeUIDsToolStripMenuItem.Checked = config.UIConfiguration.EncodeDisplayedUIDsFlag;
            //syncTimeAfterUploadsToolStripMenuItem.Checked = config.SyncTimeAfterUploadsFlag;

            display_uids_in_short_form = config.UIConfiguration.EncodeDisplayedUIDsFlag;
            sync_time_after_uploads = config.SyncTimeAfterUploadsFlag;

            string selected_tab = tabControl1.SelectedTab.Name;

            if (selected_tab == "tabPage2")
            {
                await ULAD_UserGroupsRefresh(MCv2Persistance.Config.UIConfiguration.SelectedGroup);
                //comboBox1.SelectedIndex = 0;
                //RemoveDataGridView1Events();
                await UserLookupAndDisplay(textBox1.Text.Trim(), true);
                //RestoreDataGridView1Events();
                //await CardLookupAndDisplay(selected_user, textBox3.Text.Trim(), true);
                if(IsHandleCreated)
                    Invoke((MethodInvoker)(() =>
                    {
                        dataGridView1.Focus();
                    }));
            }

            if (selected_tab == "tabPage1")
                await V2LE_FullRefresh();
        }

        bool ignore_cb1_event = false;

        

        

        private List<ulong> ULAD_GetSelectedUsers()
        {
            return dataGridView1.SelectedRows.Cast<DataGridViewRow>().Select(x => (ulong)x.Cells[0].Value).ToList();
        }

        private volatile bool dgv1_cc = false;

        

        
        private void RemoveDataGridView1Events()
        {
            dataGridView1.CurrentCellChanged -= dataGridView1_CurrentCellChanged;
            dataGridView1.SelectionChanged -= dataGridView1_SelectionChanged;
        }

        private void RestoreDataGridView1Events()
        {
            dataGridView1.CurrentCellChanged += dataGridView1_CurrentCellChanged;
            dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
        }

        private void RefreshV2ListEditor()
        {

        }

        
        

        private void DTPCheck0()
        {
            dateTimePicker2.Value = new DateTime(1970, 1, 1, (dateTimePicker1.Value + TimeSpan.FromMinutes(1)).Hour, (dateTimePicker1.Value + TimeSpan.FromMinutes(1)).Minute, 00);

            if (dateTimePicker1.Value > dateTimePicker2.Value || dateTimePicker1.Value == dateTimePicker2.Value)
            {
                if (dateTimePicker2.Value.Hour == 23 && dateTimePicker2.Value.Minute == 59)
                    dateTimePicker1.Value = new DateTime(1970, 1, 1, 23, 58, 0);
            }

            textBox7.Text = dateTimePicker1.Value.Hour.ToString("D2") + ":" + dateTimePicker1.Value.Minute.ToString("D2") + " - " + dateTimePicker2.Value.Hour.ToString("D2") + ":" + dateTimePicker2.Value.Minute.ToString("D2");
        }

        

        private void DTPCheck1()
        {
            dateTimePicker1.Value = new DateTime(1970, 1, 1, (dateTimePicker2.Value - TimeSpan.FromMinutes(1)).Hour, (dateTimePicker2.Value - TimeSpan.FromMinutes(1)).Minute, 00);

            if (dateTimePicker2.Value < dateTimePicker1.Value || dateTimePicker1.Value == dateTimePicker2.Value)
            {
                if (dateTimePicker1.Value.Hour == 0 && dateTimePicker1.Value.Minute == 0)
                    dateTimePicker2.Value = new DateTime(1970, 1, 1, 0, 1, 00);
            }

            textBox7.Text = dateTimePicker1.Value.Hour.ToString("D2") + ":" + dateTimePicker1.Value.Minute.ToString("D2") + " - " + dateTimePicker2.Value.Hour.ToString("D2") + ":" + dateTimePicker2.Value.Minute.ToString("D2");
        }

        private async void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {

        }

        private void RemoveCardLookupEvents()
        {
            //comboBox1.SelectedIndexChanged -= comboBox1_SelectedIndexChanged;
            textBox3.TextChanged -= textBox3_TextChanged;
            textBox3.KeyDown -= textBox3_KeyDown;
            listBox1.SelectedIndexChanged -= listBox1_SelectedIndexChanged;
        }

        private void RestoreCardLookupEvents()
        {
            //comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            textBox3.TextChanged += textBox3_TextChanged;
            textBox3.KeyDown += textBox3_KeyDown;
            listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
        }

        

        private volatile bool V2LE_ListRefreshActive = false;

        
        
        
        

        private void ResetListEntryEditor()
        {
            textBox5.Enabled = false;
            checkBox2.Enabled = false;
            checkedListBox1.Enabled = false;
            textBox6.Enabled = false;
            dateTimePicker1.Enabled = false;
            dateTimePicker2.Enabled = false;
            textBox7.Enabled = false;
            button5.Enabled = false;

            textBox5.Text = "";
            textBox5.BackColor = Color.LightGray;
            checkBox2.Checked = false;
            checkedListBox1.ClearSelected();
            foreach (int index in checkedListBox1.CheckedIndices)
                checkedListBox1.SetItemCheckState(index, CheckState.Unchecked);
            textBox6.Text = "";
            textBox6.BackColor = Color.LightGray;
            dateTimePicker1.Value = new DateTime(1970, 1, 1, 0, 0, 0);
            dateTimePicker2.Value = new DateTime(1970, 1, 1, 0, 0, 0);
            textBox7.Text = "";
            textBox7.BackColor = Color.LightGray;


            Refresh();
        }

       

        

       

        
        private void DevicesMenuBuilder()
        {

        }

        private void devicesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //devices menu item

        }

        
       
        
        

        public bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
            /*
            var identity = WindowsIdentity.GetCurrent();
            if (identity == null) throw new InvalidOperationException("Couldn't get the current user identity");
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
            */
        }

        
        public void RunAsAdministrator()
        {
            // Restart program and run as admin
            var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
            startInfo.Verb = "runas";
            System.Diagnostics.Process.Start(startInfo);
            //Application.Exit();
            Close();
            return;
        }

        
        
       
        
    }
}
