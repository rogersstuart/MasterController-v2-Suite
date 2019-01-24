using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using MySql.Data.MySqlClient;
using System.IO;
using System.Reflection;
using ICSharpCode.SharpZipLib.Core;
using MCICommon;

namespace MasterControllerInterface
{
    public partial class ImportForm : Form
    {
        private static readonly string[] days_of_the_week = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        public ImportForm()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == 0)
            {
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

                foreach(string path in files_to_import)
                    ImportDB2Bak(path);
            }
            else
            if (listBox1.SelectedIndex == 2) //import nuid set
            {
                List<string> to_import = new List<string>();

                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Multiselect = true;
                    ofd.Filter = "Supported Extentions (*.txt)|*.txt";
                    ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    ofd.FileName = "";

                    if (ofd.ShowDialog() == DialogResult.OK)
                        to_import.AddRange(ofd.FileNames);
                }

                if (to_import.Count() == 0)
                    return;

                using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                {
                    await sqlconn.OpenAsync();

                    foreach (string path in to_import)
                    {
                        UInt64[] nuids = File.ReadAllLines(path).Where(x => x.Trim() != "").Select(v => Convert.ToUInt64(BaseConverter.DecodeFromString(v.Trim()))).ToArray();
                        foreach (var nuid in nuids)
                            await DatabaseUtilities.AddCardToDatabase(sqlconn, nuid);
                    }
                }

            }
            else
            if (listBox1.SelectedIndex == 1)
            {
                foreach (Control c in Controls)
                    c.Enabled = false;

                UseWaitCursor = true;



                List<string> files_to_import = new List<string>();

                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Multiselect = true;
                    ofd.Filter = "Supported Extentions (*.xlsx)|*.xlsx";
                    ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    ofd.FileName = "";

                    if (ofd.ShowDialog() == DialogResult.OK)
                        files_to_import.AddRange(ofd.FileNames);
                }

                if (files_to_import.Count() == 0)
                    return;

                
                if (listBox1.SelectedIndex == 0) //import database backup
                {

                }
                else
                if (listBox1.SelectedIndex == 1) //import master controller list
                {
                    ProgressDialog pgd = new ProgressDialog("Importing List");
                    pgd.Show(this);

                    pgd.LabelText = "Importing " + files_to_import.Count() + " files.";

                    foreach (string file_name in files_to_import)
                    {
                        //import users

                        //load file

                        pgd.Reset();
                        pgd.LabelText = "Parsing list " + (files_to_import.IndexOf(file_name) + 1) + " of " + files_to_import.Count();

                        List<ListMember> members = (await ListUtilities.GetListMembers(new string[] { file_name })).ToList();

                        pgd.Step();

                        using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                        {
                            pgd.Reset();
                            pgd.LabelText = "Opening Database Connection";

                            await sqlconn.OpenAsync();

                            pgd.Step();

                            //check to make sure the user table exists
                            List<string> table_names = new List<string>();

                            using (MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn))
                            using (MySqlDataReader reader = cmdName.ExecuteReader())
                                while (await reader.ReadAsync())
                                    table_names.Add(reader.GetString(0));

                            if (!table_names.Contains("users"))
                            {
                                string cmdstr = "CREATE TABLE `accesscontrol`.`users` (`user_id` BIGINT UNSIGNED NOT NULL,`name` VARCHAR(255) NOT NULL,`description` VARCHAR(255) NULL,PRIMARY KEY (`user_id`),UNIQUE INDEX `user_id_UNIQUE` (`user_id` ASC));";

                                using (MySqlCommand sqlcmd = new MySqlCommand(cmdstr, sqlconn))
                                    await sqlcmd.ExecuteNonQueryAsync();
                            }

                            //generate subset of listmembers that dont exist in the current user list
                            //this will be based off of name and description matching

                            pgd.Reset();
                            pgd.LabelText = "Generating List of New Users";

                            if (members.Count() > 0)
                                pgd.Maximum = members.Count() - 1;

                            List<ListMember> new_users = new List<ListMember>();

                            foreach (ListMember lm in members)
                            {
                                if (lm.Name.Trim() == "")
                                {
                                    pgd.Step();
                                    continue;
                                }

                                string cmd = "SELECT * FROM `users` WHERE name=@name AND description=@description;";

                                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                                {
                                    sqlcmd.Parameters.AddWithValue("@name", lm.Name.Trim());
                                    sqlcmd.Parameters.AddWithValue("@description", lm.Description.Trim());

                                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                                        if (!(await reader.ReadAsync()))
                                            new_users.Add(lm);
                                }

                                pgd.Step();
                            }

                            pgd.Reset();
                            pgd.LabelText = "Adding New Users to the Database";

                            if (new_users.Count() > 0)
                                pgd.Maximum = new_users.Count() - 1;

                            foreach (ListMember lm in new_users)
                            {
                                using (MySqlCommand sqlcmd = new MySqlCommand("insert into `users` (user_id, name, description) values (@user_id, @name, @description)", sqlconn))
                                {
                                    sqlcmd.Parameters.AddWithValue("@user_id", await DatabaseUtilities.GenerateUniqueUserID(sqlconn));
                                    sqlcmd.Parameters.AddWithValue("@name", lm.Name.Trim());
                                    sqlcmd.Parameters.AddWithValue("@description", lm.Description.Trim());

                                    await sqlcmd.ExecuteNonQueryAsync();
                                }

                                pgd.Step();
                            }

                            //check to see if the cards table exists

                            if (!table_names.Contains("cards"))
                            {
                                string cmd = "CREATE TABLE `accesscontrol`.`cards` (`uid` BIGINT UNSIGNED NOT NULL,`user_id` BIGINT UNSIGNED NULL,PRIMARY KEY (`uid`),UNIQUE INDEX `uid_UNIQUE` (`uid` ASC));";

                                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                                    await sqlcmd.ExecuteNonQueryAsync();
                            }

                            //generate a list of new cards that will be added to the database

                            pgd.Reset();
                            pgd.LabelText = "Adding New Cards to the Database";

                            List<ListMember> new_cards = new List<ListMember>();

                            foreach (ListMember lm in members)
                            {
                                if (lm.UID == 0)
                                    continue;

                                string cmd = "SELECT uid FROM `cards` WHERE uid=@uid;";

                                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                                {
                                    sqlcmd.Parameters.AddWithValue("@uid", lm.UID);

                                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                                        if (!(await reader.ReadAsync()))
                                            new_cards.Add(lm);
                                }
                            }

                            if (new_cards.Count() > 0)
                                pgd.Maximum = new_cards.Count() - 1;

                            //add the new cards to the database

                            foreach (ListMember lm in new_cards)
                            {
                                using (MySqlCommand sqlcmd = new MySqlCommand("insert into `cards` (uid, user_id) values (@uid, @user_id)", sqlconn))
                                {
                                    sqlcmd.Parameters.AddWithValue("@uid", lm.UID);
                                    sqlcmd.Parameters.AddWithValue("@user_id", 0);

                                    await sqlcmd.ExecuteNonQueryAsync();
                                }

                                pgd.Step();
                            }

                            //associate card uids with user_ids

                            pgd.Reset();
                            pgd.LabelText = "Associating Cards With Users";
                            pgd.Maximum = members.Count() - 1;

                            foreach (ListMember lm in members)
                            {
                                if (lm.Name.Trim() == "" || lm.UID == 0)
                                {
                                    pgd.Step();
                                    continue;
                                }

                                //obtain user_id from database

                                UInt64 user_id = 0;

                                string cmd = "SELECT user_id FROM `users` WHERE name=@name AND description=@description;";

                                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                                {
                                    sqlcmd.Parameters.AddWithValue("@name", lm.Name.Trim());
                                    sqlcmd.Parameters.AddWithValue("@description", lm.Description.Trim());

                                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                                        if (await reader.ReadAsync())
                                            user_id = reader.GetUInt64(0);
                                }

                                //complete card association

                                using (MySqlCommand sqlcmd = new MySqlCommand("update `cards` set user_id=@user_id where uid=@uid;", sqlconn))
                                {
                                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);
                                    sqlcmd.Parameters.AddWithValue("@uid", lm.UID);

                                    await sqlcmd.ExecuteNonQueryAsync();
                                }

                                pgd.Step();
                            }

                            //generate access control list

                            //check to see if the lists table exists

                            if (!table_names.Contains("lists"))
                            {
                                string cmd = "CREATE TABLE `accesscontrol`.`lists` (`uid` BIGINT UNSIGNED NOT NULL,`alias` VARCHAR(255) NULL,`type` TINYINT UNSIGNED NOT NULL,PRIMARY KEY(`uid`),UNIQUE INDEX `uid_UNIQUE` (`uid` ASC));";

                                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                                    await sqlcmd.ExecuteNonQueryAsync();
                            }

                            //create a master list table entry

                            pgd.Reset();
                            pgd.LabelText = "Adding List to Master Table";

                            UInt64 list_uid = await DatabaseUtilities.GenerateUniqueListUID(sqlconn);

                            using (MySqlCommand sqlcmd = new MySqlCommand("insert into `lists` (uid, alias, type) values (@uid, @alias, @type)", sqlconn))
                            {
                                sqlcmd.Parameters.AddWithValue("@uid", list_uid);
                                sqlcmd.Parameters.AddWithValue("@alias", new FileInfo(file_name).Name.Split('.')[0]);
                                sqlcmd.Parameters.AddWithValue("@type", 0);

                                await sqlcmd.ExecuteNonQueryAsync();
                            }

                            pgd.Step();

                            //create list table

                            pgd.Reset();
                            pgd.LabelText = "Creating New List Table";

                            string sqlcmdstr = "CREATE TABLE `accesscontrol`.`" + list_uid + "` (`user_id` BIGINT UNSIGNED NOT NULL,`days` VARCHAR(255) NOT NULL,`times` VARCHAR(255) NOT NULL,`enabled` TINYINT UNSIGNED NOT NULL,PRIMARY KEY(`user_id`),UNIQUE INDEX `user_id_UNIQUE` (`user_id` ASC));";

                            using (MySqlCommand sqlcmd = new MySqlCommand(sqlcmdstr, sqlconn))
                                await sqlcmd.ExecuteNonQueryAsync();

                            pgd.Step();

                            //populate list table

                            pgd.Reset();
                            pgd.LabelText = "Populating New List";
                            pgd.Maximum = members.Count() - 1;

                            foreach (ListMember lm in members)
                            {
                                if (lm.Name.Trim() == "" || lm.UID == 0 || lm.ActiveDays.Trim() == "" || lm.ActiveTimes.Trim() == "")
                                {
                                    pgd.Step();
                                    continue;
                                }

                                UInt64 user_id = 0;

                                string cmd = "SELECT user_id FROM `users` WHERE name=@name AND description=@description;";

                                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                                {
                                    sqlcmd.Parameters.AddWithValue("@name", lm.Name.Trim());
                                    sqlcmd.Parameters.AddWithValue("@description", lm.Description.Trim());

                                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                                        if (await reader.ReadAsync())
                                            user_id = reader.GetUInt64(0);
                                }

                                if (user_id == 0)
                                    continue;

                                //check to see if the user is already present in the list and if necessary resolve the contention

                                cmd = "SELECT * FROM `" + list_uid + "` WHERE user_id=@user_id;";

                                bool contention_detected = false;
                                string resolved_access_days = "";
                                string resolved_access_times = "";

                                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                                {
                                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                                        if (await reader.ReadAsync())
                                        {
                                            contention_detected = true;

                                            //there were multiple occurences of the user in the source list. merge the access privileges.

                                            string stored_access_days = (string)reader["days"];
                                            string stored_access_times = (string)reader["times"];

                                            string contending_access_days = lm.ActiveDays;
                                            string contending_access_times = lm.ActiveTimes;

                                            //parse entry times
                                            uint stored_start_hour, stored_start_minute, stored_end_hour, stored_end_minute;
                                            uint contending_start_hour, contending_start_minute, contending_end_hour, contending_end_minute;

                                            stored_start_hour = uint.Parse(stored_access_times.Substring(0, stored_access_times.IndexOf(':')).Trim());
                                            stored_access_times = stored_access_times.Substring(stored_access_times.IndexOf(':') + 1);

                                            stored_start_minute = uint.Parse(stored_access_times.Substring(0, stored_access_times.IndexOf('-')).Trim());
                                            stored_access_times = stored_access_times.Substring(stored_access_times.IndexOf('-') + 1);

                                            stored_end_hour = uint.Parse(stored_access_times.Substring(0, stored_access_times.IndexOf(':')).Trim());
                                            stored_access_times = stored_access_times.Substring(stored_access_times.IndexOf(':') + 1);

                                            stored_end_minute = uint.Parse(stored_access_times.Trim());

                                            //

                                            contending_start_hour = uint.Parse(contending_access_times.Substring(0, contending_access_times.IndexOf(':')).Trim());
                                            contending_access_times = contending_access_times.Substring(contending_access_times.IndexOf(':') + 1);

                                            contending_start_minute = uint.Parse(contending_access_times.Substring(0, contending_access_times.IndexOf('-')).Trim());
                                            contending_access_times = contending_access_times.Substring(contending_access_times.IndexOf('-') + 1);

                                            contending_end_hour = uint.Parse(contending_access_times.Substring(0, contending_access_times.IndexOf(':')).Trim());
                                            contending_access_times = contending_access_times.Substring(contending_access_times.IndexOf(':') + 1);

                                            contending_end_minute = uint.Parse(contending_access_times.Trim());

                                            //complete the time conflict resolution

                                            if (stored_start_hour * 24 + stored_start_minute < contending_start_hour * 24 + contending_start_minute)
                                                resolved_access_times += stored_start_hour.ToString("00") + ":" + stored_start_minute.ToString("00") + "-";
                                            else
                                                resolved_access_times += contending_start_hour.ToString("00") + ":" + contending_start_minute.ToString("00") + "-";

                                            if (stored_end_hour * 24 + stored_end_minute > contending_end_hour * 24 + contending_end_minute)
                                                resolved_access_times += stored_end_hour.ToString("00") + ":" + stored_end_minute.ToString("00");
                                            else
                                                resolved_access_times += contending_end_hour.ToString("00") + ":" + contending_end_minute.ToString("00");

                                            //begin day of week conflict resolution

                                            byte merged_days = (byte)(ListEntryUtilities.ConvertDaysString(stored_access_days) | ListEntryUtilities.ConvertDaysString(contending_access_days));

                                            List<string> to_merge = new List<string>();

                                            for (int i = 6; i >= 0; i--)
                                                if (((byte)(merged_days >> i) & 1) == 1)
                                                    to_merge.Add(days_of_the_week[6 - i]);

                                            for (int i = 0; i < to_merge.Count(); i++)
                                            {
                                                resolved_access_days += to_merge[i];

                                                if (i + 1 != to_merge.Count())
                                                    resolved_access_days += ":";
                                            }
                                        }
                                }

                                if (contention_detected)
                                    using (MySqlCommand sqlcmd = new MySqlCommand("update `" + list_uid + "` set days=@days, times=@times where user_id=@user_id;", sqlconn))
                                    {
                                        sqlcmd.Parameters.AddWithValue("@days", resolved_access_days);
                                        sqlcmd.Parameters.AddWithValue("@times", resolved_access_times);
                                        sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                                        await sqlcmd.ExecuteNonQueryAsync();
                                    }
                                else
                                    using (MySqlCommand sqlcmd = new MySqlCommand("insert into `" + list_uid + "` (user_id, days, times, enabled) values (@user_id, @days, @times, @enabled)", sqlconn))
                                    {
                                        sqlcmd.Parameters.AddWithValue("@user_id", user_id);
                                        sqlcmd.Parameters.AddWithValue("@days", lm.ActiveDays);
                                        sqlcmd.Parameters.AddWithValue("@times", lm.ActiveTimes);
                                        sqlcmd.Parameters.AddWithValue("@enabled", 1);

                                        await sqlcmd.ExecuteNonQueryAsync();
                                    }

                                pgd.Step();
                            }
                        }
                    }

                    pgd.Dispose();
                }

                foreach (Control c in Controls)
                    c.Enabled = true;

                UseWaitCursor = false;
                Cursor = Cursors.Default;

                Refresh();
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
                button1.Enabled = true;
            else
                button1.Enabled = false;
        }

        private async Task ImportDB2Bak(string archive_path)
        {
            List<string> bak_table_names = new List<string>();
            List<string[]> bak_table_lines = new List<string[]>();

            using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(archive_path)))
            {
                ZipFile zf = new ZipFile(ms);
                zf.IsStreamOwner = false;

                foreach(ZipEntry ze in zf)
                {
                    bak_table_names.Add(Path.GetFileNameWithoutExtension(ze.Name));

                    byte[] buffer = new byte[4096];
                    using (MemoryStream zps = new MemoryStream())
                    {
                        StreamUtils.Copy(zf.GetInputStream(ze), zps, buffer);

                        List<string> lines = new List<string>(Encoding.UTF8.GetString(zps.ToArray()).Split(new string[] { Environment.NewLine }, StringSplitOptions.None));

                        lines.RemoveAll(x => x.Trim() == ""); //remove blank lines

                        bak_table_lines.Add(lines.Select(x => x.Substring(0, x.Length - 1)).ToArray()); //remove trailing commas and store
                    }      
                }
            }

            //get list of tables in the database
            List<string> db_table_names = new List<string>();

            using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
            {
                using (MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn))
                using (MySqlDataReader reader = cmdName.ExecuteReader())
                    while (await reader.ReadAsync())
                        db_table_names.Add(reader.GetString(0));


            }
        }
    }
}
