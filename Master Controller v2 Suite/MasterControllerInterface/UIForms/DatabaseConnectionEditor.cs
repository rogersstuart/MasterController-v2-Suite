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
using System.Net.Sockets;
using System.IO;
using MCICommon;
using UIElements;

namespace MasterControllerInterface
{
    public partial class DatabaseConnectionEditor : Form
    {
        private DatabaseConnectionProperties dbconnprop = null;
        private TextBox[] tbs;

        private EventHandler text_change_handler;

        private System.Timers.Timer tb_check_timer = new System.Timers.Timer(2000);

        private volatile bool tb_bg_check_active = false;
        private TimeSpan tb_bg_check_ival = TimeSpan.FromSeconds(1);
        private Task tb_background_checker;

        public DatabaseConnectionEditor(DatabaseConnectionProperties dbconnprop, bool IsCancellable)
        {

            this.dbconnprop = dbconnprop;

            InitializeComponent();

            if (File.Exists("auto_backup.db2bak"))
                restoreDBFromAutosaveToolStripMenuItem.Enabled = true;

            if (!IsCancellable)
                button2.Enabled = false;

            tbs = new TextBox[] { textBox1, textBox4, textBox2, textBox3};

            text_change_handler = new EventHandler(HandleTextChanged);

            tb_background_checker = new Task(async () =>
            {
                tb_bg_check_active = true;

                await TestTextBoxValues();

                DateTime last_check = DateTime.Now;
                while (tb_bg_check_active)
                {
                    while (DateTime.Now - last_check < tb_bg_check_ival)
                    {
                        if (!tb_bg_check_active)
                            return;
                        else
                            await Task.Delay(250);
                    }                      

                    await TestTextBoxValues();

                    last_check = DateTime.Now;
                }
            });
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        private void FillTextBoxes()
        {
            if (dbconnprop != null)
            {
                for (int i = 0; i < tbs.Count(); i++)
                {
                    tbs[i].TextChanged -= text_change_handler;
                    tbs[i].Text = dbconnprop.ToArray()[i].ToString();
                    tbs[i].SelectionStart = tbs[i].Text.Length;
                    tbs[i].SelectionLength = 0;
                    tbs[i].TextChanged += text_change_handler;
                }
            }
        }

        private async Task TestTextBoxValues()
        {
            string[] text_box_strings = GetTextBoxStrings();

            DatabaseConnectionProperties dbconnprop = DatabaseConnectionProperties.FromArray(text_box_strings);

            //if the host name is blank or unreachable
            if (text_box_strings[0] == "")
            {
                if(IsHandleCreated)
                    Invoke((MethodInvoker)(() =>
                    {
                        tbs[0].BackColor = Color.LightSalmon;
                        tbs[1].BackColor = Color.GhostWhite;
                        tbs[2].BackColor = Color.GhostWhite;
                        tbs[3].BackColor = Color.GhostWhite;

                        Refresh();
                    }));

                return;
            }

            if(await TestHostnameOrIP(text_box_strings[0]))
            {
                if (IsHandleCreated)
                    Invoke((MethodInvoker)(() =>
                    {
                        tbs[0].BackColor = Color.LightGreen;
                    }));
            }
            else
            {
                if (IsHandleCreated)
                    Invoke((MethodInvoker)(() =>
                    {
                        tbs[0].BackColor = Color.LightSalmon;
                        tbs[1].BackColor = Color.GhostWhite;
                        tbs[2].BackColor = Color.GhostWhite;
                        tbs[3].BackColor = Color.GhostWhite;

                        Refresh();
                    }));

                return;
            }

            if (text_box_strings[1] == "" || text_box_strings[2] == "" || text_box_strings[2] == "")
            {
                if (IsHandleCreated)
                    Invoke((MethodInvoker)(() =>
                    {
                        tbs[1].BackColor = Color.LightSalmon;
                        tbs[2].BackColor = Color.LightSalmon;
                        tbs[3].BackColor = Color.LightSalmon;

                        Refresh();
                    }));

                return;
            }

            if (await TestDBConnProp(dbconnprop))
            {
                if (IsHandleCreated)
                    Invoke((MethodInvoker)(() =>
                    {
                        tbs[1].BackColor = Color.LightCyan;
                        tbs[2].BackColor = Color.LightCyan;
                        tbs[3].BackColor = Color.LightCyan;

                        button1.Enabled = true;
                        restoreToolStripMenuItem.Enabled = true;
                        saveToolStripMenuItem.Enabled = true;

                        Refresh();
                    }));

                return;
            }
            else
            {
                if (IsHandleCreated)
                    Invoke((MethodInvoker)(() =>
                    {
                        tbs[1].BackColor = Color.LightSalmon;
                        tbs[2].BackColor = Color.LightSalmon;
                        tbs[3].BackColor = Color.LightSalmon;

                        Refresh();
                    }));

                return;
            }
        }

        private string[] GetTextBoxStrings()
        {
            return tbs.Select(x => x.Text.Trim()).ToArray() ;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //accept button
            dbconnprop = DatabaseConnectionProperties.FromArray(GetTextBoxStrings());

            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //cancel button

            DialogResult = DialogResult.Cancel;
        }

        private void HandleTextChanged(object sender, EventArgs e)
        {
            button1.Enabled = false;
            restoreToolStripMenuItem.Enabled = false;
            saveToolStripMenuItem.Enabled = false;

            Refresh();

            tb_check_timer.Stop();
            tb_check_timer.Start();
        }

        private async Task<bool> TestHostnameOrIP(string hostname_or_ip)
        {
            try
            {
                using (TcpClient tcpclient = new TcpClient())
                    await tcpclient.ConnectAsync(hostname_or_ip, 3306);   

                return true;
            }
            catch (Exception ex)
            {
                
            }

            return false;
        }

        private async Task<bool> TestDBConnProp(DatabaseConnectionProperties dbconnprop)
        {
            bool res = false;

            await Task.Run(() =>
            {
                try
                {
                    using (var connection = new MySqlConnection(dbconnprop.ConnectionString))
                    {
                        connection.Open();
                        connection.Close();
                        res = true;
                    }
                }
                catch (Exception ex)
                {

                }
            });

            return res;
        }

        public DatabaseConnectionProperties DBConnProp
        {
            get
            {
                return dbconnprop;
            }

            set
            {
                dbconnprop = new DatabaseConnectionProperties(value);

                FillTextBoxes();
            }
        }

        private void DatabaseConnectionEditor_Shown(object sender, EventArgs e)
        {
            textBox1.TextChanged += text_change_handler;

            FillTextBoxes();

            tb_background_checker.Start();
        }

        private async void DatabaseConnectionEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            tb_bg_check_active = false;
            await tb_background_checker;
        }

        private void credentialFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> files_to_import = new List<string>();

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = false;
                ofd.Filter = "Supported Extentions (*.crdx)|*.crdx";
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                ofd.FileName = "";

                if (ofd.ShowDialog() == DialogResult.OK)
                    files_to_import.AddRange(ofd.FileNames);
            }

            if (files_to_import.Count() == 0)
                return;

            string[] lines = File.ReadAllLines(files_to_import[0]);

            int lctr = 0;
            foreach (var tbx in tbs)
                tbx.Text = lines[lctr++];
        }

        private void credentialFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Supported Extentions (*.crdx)|*.crdx";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                sfd.FileName = DateTime.Now.ToFileTime().ToString();

                if (sfd.ShowDialog() == DialogResult.OK)
                    File.WriteAllLines(sfd.FileName, GetTextBoxStrings());
            }
        }

        private async void databaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dbconnprop = DatabaseConnectionProperties.FromArray(GetTextBoxStrings());

            MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties = dbconnprop;

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

                try
                {
                    await DBBackupAndRestore.Restore(file, DatabaseConnectionProperties.FromArray(GetTextBoxStrings()));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occured while restoring from " + file + ".");
                }

            }

            pgd.Dispose();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Abort;
        }

        private async void restoreDBFromAutosaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var config = MCv2Persistance.Instance.Config;
            config.DatabaseConfiguration.DatabaseConnectionProperties = DBConnProp;
            MCv2Persistance.Instance.Config = config;

            new BackupManagerForm().ShowDialog(this);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            //
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            //
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            //
        }
    }
}
