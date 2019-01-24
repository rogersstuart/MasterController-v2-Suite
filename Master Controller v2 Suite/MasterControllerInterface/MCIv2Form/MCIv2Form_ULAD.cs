using MCICommon;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MasterControllerInterface
{
    public partial class MCIv2Form
    {
        private Dictionary<UInt64, string> ULAD_ViewingGroups = null;

        private async Task UserLookupAndDisplay(string query, bool force, CancellationTokenSource lcts = null)
        {
            int selected_index = -1;
            Invoke((MethodInvoker)(() => { selected_index = comboBox1.SelectedIndex; }));

            if (selected_index < 1)
                await UserLookupAndDisplay_NoGroups(query, force, lcts);
            else
                await UserLookupAndDisplay_Groups(query, force, lcts);
        }

        private async Task UserLookupAndDisplay_Groups(string query, bool force, CancellationTokenSource lcts = null)
        {
            if (query != previous_search || previous_search == null || force)
            {
                if (lcts == null)
                    lcts = new CancellationTokenSource();

                await Task.Run(async () =>
                {
                    //RemoveDataGridView1Events();

                    Invoke((MethodInvoker)(() =>
                    {
                        dataGridView1.Enabled = false;

                        //dataGridView1.DataSource = null;

                        Refresh();
                    }));

                    //begin a second task. we need to build the autocomplete source.
                    string[] autocomplete_strings = null;
                    Task auto_complete_source_builder = new Task(async () =>
                    {
                        autocomplete_strings = await DatabaseUtilities.BuildDenseAutocompleteSource(lcts.Token);
                    }, lcts.Token);
                    auto_complete_source_builder.Start();

                    previous_search = query;

                    AutoRefreshDBConnection sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                    //await DatabaseUtilities.CheckIfUsersTableExistsAndCreateIfNeeded(sqlconn);

                    int selected_index = 0;
                    Invoke((MethodInvoker)(() => { selected_index = comboBox1.SelectedIndex - 1; }));

                    var users_in_group = await GroupDBUtilities.GetUsersInGroup(ULAD_ViewingGroups.ElementAt(selected_index).Key);
                    var comp_str = "";
                    foreach (var user in users_in_group)
                        comp_str += user + ", ";
                    if (comp_str != "")
                        comp_str = comp_str.Substring(0, comp_str.Length - 2);

                    if (query == "")
                        mySqlDataAdapter = new MySqlDataAdapter("select * from users where user_id in (0, " + comp_str + ");", sqlconn.Connection);
                    else
                    {
                        List<string> substrs = query.Split(' ').ToList();

                        List<string> results = new List<string>();

                        if (cb2_seli == 1)
                            foreach (var substr in substrs)
                                if (BaseConverter.TryParseEncodedString(substr))
                                    results.Add(BaseConverter.DecodeFromString(substr).ToString());

                        //substrs.AddRange(result);

                        //string cmd = "SELECT * FROM `users` WHERE";

                        string cmd = "SELECT a.user_id, a.name, a.description FROM users a LEFT JOIN cards b ON a.user_id=b.user_id WHERE (";

                        foreach (string str in substrs)
                        {
                            string str_l = str;

                            str_l = str_l.Replace("\'", "\\'");

                            ulong res;

                            bool is_int = UInt64.TryParse(str_l, out res);

                            if (cb2_seli == 0)
                                is_int = false;

                            cmd += " (";

                            if (is_int)
                            {
                                cmd += " a.user_id=" + str_l + " OR ";
                                cmd += " b.uid=" + str_l + ") AND";
                            }
                            else
                            {
                                cmd += " a.name LIKE " + "'%" + str_l + "%'" + " OR ";
                                cmd += " a.description LIKE " + "'%" + str_l + "%'" + ") AND";
                            }

                            bool is_encoded = false;
                            string dec_str = "";

                            if (cb2_seli == 1)
                                if (is_encoded = BaseConverter.TryParseEncodedString(str_l))
                                    dec_str = BaseConverter.DecodeFromString(str_l).ToString();

                            if (is_encoded)
                            {
                                cmd = cmd.Substring(0, cmd.Length - 4);
                                cmd += " OR (";
                                cmd += " a.user_id=" + dec_str + " OR ";
                                cmd += " b.uid=" + dec_str + ") AND";
                            }
                        }

                        cmd = cmd.Substring(0, cmd.Length - 4);

                        cmd += ") AND a.user_id in (0, " + comp_str + ")" + " GROUP BY a.user_id;";

                        Console.WriteLine(cmd);

                        mySqlDataAdapter = new MySqlDataAdapter(cmd, sqlconn.Connection);
                    }

                    DataSet ds = new DataSet();

                    try
                    {
                        await mySqlDataAdapter.FillAsync(ds);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show("Query Error");
                    }

                    await auto_complete_source_builder;

                    Invoke((MethodInvoker)(() =>
                    {
                        if (autocomplete_strings != null)
                        {
                            AutoCompleteStringCollection acsc = new AutoCompleteStringCollection();
                            acsc.AddRange(autocomplete_strings);
                            textBox1.AutoCompleteCustomSource = acsc;
                        }

                        //dataGridView1.DataSource = null;
                        try
                        {
                            dataGridView1.DataSource = ds.Tables[0];
                        }
                        catch (Exception ex)
                        {
                            dataGridView1.DataSource = null;
                        }

                        dataGridView1.Enabled = true;

                        Refresh();
                    }));

                    ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                }, lcts.Token);
            }

            Invoke((MethodInvoker)(() =>
            {
                refreshToolStripMenuItem.BackColor = SystemColors.Control;
            }));
        }

        private async Task UserLookupAndDisplay_NoGroups(string query, bool force, CancellationTokenSource lcts = null)
        {
            if (query != previous_search || previous_search == null || force)
            {
                if (lcts == null)
                    lcts = new CancellationTokenSource();

                await Task.Run(async () =>
                {
                    //RemoveDataGridView1Events();

                    Invoke((MethodInvoker)(() =>
                    {
                        dataGridView1.Enabled = false;

                        //dataGridView1.DataSource = null;

                        Refresh();
                    }));

                    //begin a second task. we need to build the autocomplete source.
                    string[] autocomplete_strings = null;
                    Task auto_complete_source_builder = new Task(async () =>
                    {
                        autocomplete_strings = await DatabaseUtilities.BuildDenseAutocompleteSource(lcts.Token);
                    }, lcts.Token);
                    auto_complete_source_builder.Start();

                    previous_search = query;

                    AutoRefreshDBConnection sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                    //await DatabaseUtilities.CheckIfUsersTableExistsAndCreateIfNeeded(sqlconn);

                    if (query == "")
                        mySqlDataAdapter = new MySqlDataAdapter("select * from users", sqlconn.Connection);
                    else
                    {
                        List<string> substrs = query.Split(' ').ToList();

                        List<string> results = new List<string>();

                        if (cb2_seli == 1)
                            foreach (var substr in substrs)
                                if (BaseConverter.TryParseEncodedString(substr))
                                    results.Add(BaseConverter.DecodeFromString(substr).ToString());

                        //substrs.AddRange(result);

                        //string cmd = "SELECT * FROM `users` WHERE";

                        string cmd = "SELECT a.user_id, a.name, a.description FROM users a LEFT JOIN cards b ON a.user_id=b.user_id WHERE";

                        foreach (string str in substrs)
                        {
                            string str_l = str;

                            str_l = str_l.Replace("\'", "\\'");

                            ulong res;

                            bool is_int = UInt64.TryParse(str_l, out res);

                            if (cb2_seli == 0)
                                is_int = false;

                            cmd += " (";

                            if (is_int)
                            {
                                cmd += " a.user_id=" + str_l + " OR ";
                                cmd += " b.uid=" + str_l + ") AND";
                            }
                            else
                            {
                                cmd += " a.name LIKE " + "'%" + str_l + "%'" + " OR ";
                                cmd += " a.description LIKE " + "'%" + str_l + "%'" + ") AND";
                            }

                            bool is_encoded = false;
                            string dec_str = "";

                            if (cb2_seli == 1)
                                if (is_encoded = BaseConverter.TryParseEncodedString(str_l))
                                    dec_str = BaseConverter.DecodeFromString(str_l).ToString();

                            if (is_encoded)
                            {
                                cmd = cmd.Substring(0, cmd.Length - 4);
                                cmd += " OR (";
                                cmd += " a.user_id=" + dec_str + " OR ";
                                cmd += " b.uid=" + dec_str + ") AND";
                            }
                        }

                        cmd = cmd.Substring(0, cmd.Length - 4);

                        cmd += " GROUP BY a.user_id;";

                        Console.WriteLine(cmd);

                        mySqlDataAdapter = new MySqlDataAdapter(cmd, sqlconn.Connection);
                    }

                    DataSet ds = new DataSet();

                    try
                    {
                        await mySqlDataAdapter.FillAsync(ds);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show("Query Error");
                    }

                    await auto_complete_source_builder;

                    Invoke((MethodInvoker)(() =>
                    {
                        if (autocomplete_strings != null)
                        {
                            AutoCompleteStringCollection acsc = new AutoCompleteStringCollection();
                            acsc.AddRange(autocomplete_strings);
                            textBox1.AutoCompleteCustomSource = acsc;
                        }

                        //dataGridView1.DataSource = null;
                        dataGridView1.DataSource = ds.Tables[0];

                        dataGridView1.Enabled = true;

                        Refresh();
                    }));

                    ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                }, lcts.Token);
            }

            Invoke((MethodInvoker)(() =>
            {
                refreshToolStripMenuItem.BackColor = SystemColors.Control;
            }));
        }

        private async Task ULAD_UserGroupsRefresh(ulong to_select = 0)
        {
            ignore_cb1_event = true;

            await GroupDBUtilities.UserGroupsInit(); //make sure the user groups table exists

            ULAD_ViewingGroups = await GroupDBUtilities.GetDictionaryDescriptionForAllGroups();

            var l1 = new List<string>();
            l1.Add("All Users");
            l1.AddRange(ULAD_ViewingGroups.Values.ToArray());
            comboBox1.DataSource = (l1);

            if (to_select > 0)
            {
                var cfg = MCv2Persistance.Config;

                int index = -1;
                for (int i = 0; i < ULAD_ViewingGroups.Keys.Count(); i++)
                    if (ULAD_ViewingGroups.Keys.ElementAt(i) == to_select)
                        index = i;

                if (index > -1)
                {
                    comboBox1.SelectedIndex = index + 1;
                    cfg.UIConfiguration.SelectedGroup = to_select;
                }

                else
                {
                    comboBox1.SelectedIndex = 0;
                    cfg.UIConfiguration.SelectedGroup = 0;
                }

                MCv2Persistance.Config = cfg;
            }

            if (comboBox1.SelectedIndex > 0)
                button25.Enabled = true;
            else
                button25.Enabled = false;

            ignore_cb1_event = false;
        }

    }
}
