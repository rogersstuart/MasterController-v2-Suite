﻿using MCICommon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UIElements;

namespace MasterControllerInterface
{
    public partial class BackupManagerForm : Form
    {
        private Dictionary<string, BackupProperties> backups = new Dictionary<string, BackupProperties>();

        public BackupManagerForm()
        {
            InitializeComponent();

            
        }

        private async void BackupManagerForm_Shown(object sender, EventArgs e)
        {
            dataGridView1.SelectionChanged += (a, b) =>
            {
                if (dataGridView1.SelectedRows.Count == 0 || backups.Count() == 0)
                {
                    button2.Enabled = false;
                    button3.Enabled = false;
                    button4.Enabled = true;
                    button5.Enabled = false;
                }
                else
                if (dataGridView1.SelectedRows.Count == 1)
                {
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    button5.Enabled = true;
                }
                else
                {
                    button2.Enabled = false;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    button5.Enabled = false;
                }
            };

            await RefreshUI();
        }

        private async Task LoadBackupInfo()
        {
            var backupDir = AppDomain.CurrentDomain.BaseDirectory + "backups\\";
            
            // Ensure the backup directory exists
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
                return;
            }
            
            var files = Directory.GetFiles(backupDir, "*.db2bak");

            backups.Clear();
            foreach(var file in files)
            {
                try
                {
                    var properties = await DBBackupAndRestore.GetBackupProperties(file);
                    backups.Add(file, properties);
                }
                catch (Exception ex)
                {
                    // Skip invalid backup files
                    Console.WriteLine($"Skipping invalid backup file {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            backups = backups.OrderBy(x => x.Value.Timestamp).Reverse().ToDictionary(x => x.Key, x => x.Value);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //close button
            Close();
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            //restore button
            List<bool> enable_states = new List<bool>();
            foreach(Control c in Controls)
            {
                enable_states.Add(c.Enabled);
                c.Enabled = false;
            }

            var cfg = MCv2Persistance.Instance.Config;

            ProgressDialog pgd = new ProgressDialog("Database Restore");
            pgd.Show();
            pgd.SetMarqueeStyle();

            try
            {
                var backup_filename = backups.Keys.ElementAt(dataGridView1.SelectedRows[0].Index);

                pgd.LabelText = "Restoring from " + new FileInfo(backup_filename).Name;

                await DBBackupAndRestore.Restore(backup_filename);
                
                MessageBox.Show("Database restored successfully!", "Success", 
                               MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restore database:\n\n{ex.Message}", 
                               "Restore Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                pgd.Dispose();

                for (int i = 0; i < enable_states.Count; i++)
                    Controls[i].Enabled = enable_states[i];
            }
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            //create button
            button4.Enabled = false;
            button5.Enabled = false;

            var cfg = MCv2Persistance.Instance.Config;

            ProgressDialog pgd = new ProgressDialog("Database Backup");
            
            pgd.Show();
            pgd.SetMarqueeStyle();

            var now_is = DateTime.Now;
            var backup_filename = cfg.DatabaseConfiguration.DatabaseConnectionProperties.Hostname.Replace('.', '_') + "_" + cfg.DatabaseConfiguration.DatabaseConnectionProperties.Schema + "_" + now_is.Year + "_" + now_is.Month + "_" + now_is.Day + "_" + now_is.ToFileTimeUtc().ToString();

            pgd.LabelText = "Saving backup to " + backup_filename + ".db2bak";

            await DBBackupAndRestore.Backup("./backups/" + backup_filename + ".db2bak");

            pgd.Dispose();

            await RefreshUI();
        }

        private void button4_MouseEnter(object sender, EventArgs e)
        {
            dataGridView1.Focus();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            //delete button

            var selected = dataGridView1.SelectedRows;
            for (int i = 0; i < selected.Count; i++)
                File.Delete(backups.Keys.ElementAt(selected[i].Index));

            await RefreshUI();
        }

        private async Task RefreshUI()
        {
            await LoadBackupInfo();

            dataGridView1.Rows.Clear();

            //fill datagridview
            foreach (var backup in backups)
                dataGridView1.Rows.Add(backup.Value.Timestamp, backup.Value.Database, new FileInfo(backup.Key).Name);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var selected = dataGridView1.SelectedRows;
            var bytes = File.ReadAllBytes(backups.Keys.ElementAt(selected[0].Index));

            string file_name;
            var cfg = MCv2Persistance.Instance.Config;
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Supported Extentions (*.db2bak)|*.db2bak";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var now_is = DateTime.Now;
                sfd.FileName = new FileInfo(backups.Keys.ElementAt(selected[0].Index)).Name;

                if (sfd.ShowDialog() == DialogResult.OK)
                    File.WriteAllBytes(sfd.FileName, bytes);
            }

            
        }

        private void dataGridView1_MouseHover(object sender, EventArgs e)
        {
            dataGridView1.Focus();
        }
    }
}
