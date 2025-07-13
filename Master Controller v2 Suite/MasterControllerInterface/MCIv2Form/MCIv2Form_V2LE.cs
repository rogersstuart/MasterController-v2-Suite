using MCICommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MasterControllerInterface
{
    public partial class MCIv2Form
    {
        private void V2LE_DisableAllEntryFields()
        {

        }

        private void V2LE_ListEntryEditor_VerifyFields()
        {
            //check to see if the data is formatted correctly
            string tb6_text = textBox6.Text.Trim();
            string tb7_text = textBox7.Text.Trim();

            if (ListEntryUtilities.TryParseV2EntryDays(tb6_text) && ListEntryUtilities.TryParseV2EntryTime(tb7_text))
                button5.Enabled = true;
        }

        //to be called from a non ui thread
        //refreshes the displayed access control lists
        private async Task V2LE_RefreshListSelection(string filter)
        {
            V2LE_ListRefreshActive = true;

            string tb8_text = ""; //list filter

            await Task.Run(async () =>
            {
                //use all lists in the search
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();
                V2LE_ViewingLists = await DatabaseUtilities.GetDictionaryUniqueShortListDescriptionWithLimiter(sqlconn.Connection, tb8_text, 0);
                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                //V2LE_ViewingLists = (Dictionary<ulong, string>)V2LE_ViewingLists.OrderBy(x => x.Value).ToDictionary();

                lock (ui_access_lock)
                    Invoke((MethodInvoker)(() =>
                    {
                        if (V2LE_ViewingLists.Count() == 0)
                        {
                            listBox5.DataSource = null;
                            listBox5.Enabled = false;
                            V2LE_ResetUserAssignment();
                            ResetListEntryEditor();
                        }
                        else
                        {
                            listBox5.DataSource = V2LE_ViewingLists.Values.ToList();
                            listBox5.Enabled = true;
                        }

                        Refresh();
                    }));
            });

            V2LE_ListRefreshActive = false;
        }


        private void V2LE_SetTimesFromString(string time_string)
        {
            try
            {
                TimeSpan[] spans = ListEntryUtilities.ConvertTimeStringToTimeSpans(time_string);

                dateTimePicker1.Value = new DateTime(1970, 1, 1, spans[0].Hours, spans[0].Minutes, 0);
                dateTimePicker2.Value = new DateTime(1970, 1, 1, spans[1].Hours, spans[1].Minutes, 0);
            }
            catch (Exception ex)
            {
                dateTimePicker1.Value = new DateTime(1970, 1, 1, 0, 0, 0);
                dateTimePicker2.Value = new DateTime(1970, 1, 1, 0, 0, 0);
            }
        }

        //to be called from a non ui thread
        private async Task V2LE_PopulateDeviceTree()
        {
            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();
            V2LE_ViewingDevices = await DatabaseUtilities.GetDictionaryUniqueShortDeviceDescriptionWithLimiter(sqlconn.Connection, 0);
            ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            lock (ui_access_lock)
                Invoke((MethodInvoker)(() =>
                {
                    treeView1.Nodes.Clear();

                    TreeNode current_node = new TreeNode("MCv2 Offline Controllers");

                    if (V2LE_ViewingDevices != null && V2LE_ViewingDevices.Count() > 0)
                    {
                        foreach (string device_description in V2LE_ViewingDevices.Values)
                            current_node.Nodes.Add(new TreeNode(device_description));
                    }
                    else
                        current_node.Text = "No Devices Available";

                    treeView1.Nodes.Add(current_node);

                    treeView1.ExpandAll();

                    Refresh();
                }));
        }


        private async Task V2LE_RefreshListEntryEditor()
        {
            int lb5_index = listBox5.SelectedIndex;
            int lb3_index = listBox3.SelectedIndex;

            ResetListEntryEditor();

            // First ensure dictionaries are initialized
            if (V2LE_ViewingLists == null) 
                V2LE_ViewingLists = new Dictionary<UInt64, string>();
        
            if (V2LE_UsersInCurrentList == null)
                V2LE_UsersInCurrentList = new Dictionary<UInt64, string>();

            // Check bounds and valid state with better error handling
            bool validState = 
                lb3_index > -1 && lb5_index > -1 && 
                V2LE_UsersInCurrentList != null && V2LE_UsersInCurrentList.Count() > 0 && 
                V2LE_ViewingLists != null && V2LE_ViewingLists.Count() > 0 &&
                lb5_index < V2LE_ViewingLists.Count() &&
                lb3_index < V2LE_UsersInCurrentList.Count();

            if (!validState)
            {
                Debug.WriteLine("[V2LE_RefreshListEntryEditor] Invalid state detected, returning");
                return;
            }

            try
            {
                // Safe dictionary access with null checking
                var listEntry = V2LE_ViewingLists.ElementAtSafe(lb5_index);
                var userEntry = V2LE_UsersInCurrentList.ElementAtSafe(lb3_index);
        
                if (listEntry.Equals(default(KeyValuePair<UInt64, string>)) || 
                    userEntry.Equals(default(KeyValuePair<UInt64, string>)))
                {
                    Debug.WriteLine("[V2LE_RefreshListEntryEditor] Unable to get valid list or user entry");
                    return;
                }
        
                UInt64 selected_list_id = listEntry.Key;
                UInt64 selected_user_id = userEntry.Key;

                List<Object> list_row = await DatabaseUtilities.GetV2ListEntry(selected_list_id, selected_user_id);

                if (list_row == null)
                    return;

                string tb6_text = "";

                if (ListEntryUtilities.TryParseV2EntryDays((string)list_row[0]))
                    tb6_text = ListEntryUtilities.ConvertBoolToDOWString(ListEntryUtilities.ConvertDOWStringToBools((string)list_row[0]));
                else
                    tb6_text = "Sunday - Saturday";

                Invoke((MethodInvoker)(() =>
                {
                    textBox5.Text = selected_user_id.ToString();
                    textBox5.Enabled = true;

                    checkBox2.Checked = (byte)list_row[2] == 0 ? false : true;
                    checkBox2.Enabled = true;

                    textBox6.Text = tb6_text;

                    textBox6.Enabled = true;
                    checkedListBox1.Enabled = true;

                    textBox7.Text = ListEntryUtilities.TryParseV2EntryTime((string)list_row[1]) ? (string)list_row[1] : "00:00 - 23:59";

                    textBox7.Enabled = true;
                    dateTimePicker1.Enabled = true;
                    dateTimePicker2.Enabled = true;

                    Refresh();
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[V2LE_RefreshListEntryEditor] Exception: {ex.Message}");
                // Don't rethrow - just log and continue
            }
        }

        private async Task V2LE_FullRefresh()
        {
            string list_filter = "";

            var tasks = new Task[]{ new Task(async () => { await V2LE_RefreshListSelection(list_filter); }),
                                    new Task(async () => { await V2LE_PopulateDeviceTree(); })};

            foreach (Task t in tasks)
                t.Start();

            await Task.WhenAll(tasks);
        }


        //to be called by a ui thread
        private async Task V2LE_UA_ListUsers_Refresh()
        {
            if (listBox5.SelectedIndex == -1 || V2LE_ViewingLists == null || listBox5.SelectedIndex >= V2LE_ViewingLists.Count())
                return;

            ulong selected_list = V2LE_ViewingLists.ElementAt(listBox5.SelectedIndex).Key;
            string filter = textBox2.Text.Trim();

            await Task.Run(async () => { await V2LE_UA_ListUsers_Refresh(selected_list, filter); });
        }

        //to be called from a non ui thread
        private async Task V2LE_UA_ListUsers_Refresh(ulong selected_list, string list_user_filter)
        {
            if (selected_list != 0)
            {
                //use all lists in the search
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();
                V2LE_UsersInCurrentList = await DatabaseUtilities.GetDictionaryDescriptionForAllUsersInListWithLimiter(sqlconn.Connection, selected_list, list_user_filter);
                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                //V2LE_UsersInCurrentList = (Dictionary<ulong, string>)V2LE_UsersInCurrentList.OrderBy(x => x.Value); 

                lock (ui_access_lock)
                    Invoke((MethodInvoker)(() =>
                    {
                        if (V2LE_UsersInCurrentList == null || V2LE_UsersInCurrentList.Count() == 0)
                        {
                            listBox3.DataSource = null;
                            listBox3.Enabled = false;
                            textBox2.BackColor = Color.LightGray;
                        }
                        else
                        {
                            listBox3.DataSource = V2LE_UsersInCurrentList.Values.ToList();
                            listBox3.Enabled = true;
                            textBox2.BackColor = SystemColors.Window;

                        }

                        textBox2.Enabled = true;
                        button12.Enabled = true;

                        Refresh();
                    }));
            }
        }

        //to be called by a ui thread
        private async Task V2LE_UA_UserLookup_Refresh()
        {
            if (listBox5.SelectedIndex == -1 || V2LE_ViewingLists == null || listBox5.SelectedIndex >= V2LE_ViewingLists.Count())
                return;

            ulong selected_list = V2LE_ViewingLists.ElementAt(listBox5.SelectedIndex).Key;
            string filter = textBox2.Text.Trim();

            await Task.Run(async () => { await V2LE_UA_UserLookup_Refresh(selected_list, filter); });
        }

        //to be called from a non ui thread
        private async Task V2LE_UA_UserLookup_Refresh(ulong selected_list, string user_filter)
        {
            if (selected_list != 0)
            {
                //use all lists in the search
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();
                V2LE_UsersNotInCurrentList = await DatabaseUtilities.GetDictionaryDescriptionsAllUsersNotPresentInListWithLimiter(sqlconn.Connection, selected_list, user_filter);
                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                //V2LE_UsersNotInCurrentList = (Dictionary<ulong, string>)V2LE_UsersNotInCurrentList.OrderBy(x => x.Value);

                lock (ui_access_lock)
                    Invoke((MethodInvoker)(() =>
                    {
                        if (V2LE_UsersNotInCurrentList == null || V2LE_UsersNotInCurrentList.Count() == 0)
                        {
                            listBox4.DataSource = null;
                            listBox4.Enabled = false;
                        }
                        else
                        {
                            listBox4.DataSource = V2LE_UsersNotInCurrentList.Values.ToList();
                            listBox4.Enabled = true;
                        }

                        Refresh();
                    }));
            }
        }

        private void V2LE_ResetUserAssignment()
        {
            textBox2.Enabled = false;
            listBox3.Enabled = false;
            button3.Enabled = false;
            listBox4.Enabled = false;
            button4.Enabled = false;
            button12.Enabled = false;

            textBox2.Text = "";
            textBox2.BackColor = Color.LightGray;
            listBox3.DataSource = null;
            listBox4.DataSource = null;

            Refresh();
        }

        private void V2LE_ResetAll()
        {
            // Initialize dictionaries to prevent KeyNotFoundException errors
            V2LE_ViewingLists = new Dictionary<UInt64, string>();
            V2LE_UsersInCurrentList = new Dictionary<UInt64, string>();
            V2LE_UsersNotInCurrentList = new Dictionary<UInt64, string>();
            V2LE_ViewingDevices = new Dictionary<UInt64, string>();

            // Log the initialization
            Debug.WriteLine("[MCIv2Form] V2LE dictionaries initialized");

            // Preserve existing behavior
            V2LE_ResetListSelection();
            V2LE_ResetUserAssignment();
            ResetListEntryEditor();

            button6.Enabled = false;
            button20.Enabled = false;

            label7.Text = "No List Selected";
        }

        private void V2LE_ResetListSelection()
        {
            listBox5.Enabled = false;
            listBox5.DataSource = null;

            Refresh();
        }

        private async Task V2LE_RefreshListSelection()
        {
            string list_filter = "";

            await Task.Run(async () => { await V2LE_RefreshListSelection(list_filter); });
        }

        //call from a ui thread
        private async Task V2LE_RefreshUserAssignment()
        {
            V2LE_RefreshUserAssignment_Busy = true;

            //change edit button states
            if (listBox5.SelectedIndex > -1 && V2LE_ViewingLists != null && listBox5.SelectedIndex < V2LE_ViewingLists.Count())
            {
                button15.Enabled = true;
                button16.Enabled = true;
            }
            else
            {
                button15.Enabled = false;
                button16.Enabled = false;

                //everything should be invalidated here
                V2LE_RefreshUserAssignment_Busy = false;
                return;
            }

            ulong selected_list_uid = V2LE_ViewingLists.ElementAt(listBox5.SelectedIndex).Key;
            string selected_list_name = V2LE_ViewingLists.ElementAt(listBox5.SelectedIndex).Value;
            string list_user_filter = textBox2.Text.Trim();
            string user_assignment_filter = textBox2.Text.Trim();

            label7.Text = "Users In: " + selected_list_name;

            var tasks = new Task[] { new Task(async () => { await V2LE_UA_ListUsers_Refresh(selected_list_uid, list_user_filter); }) ,
                                     new Task(async () => { await V2LE_UA_UserLookup_Refresh(selected_list_uid, user_assignment_filter); }) };

            foreach (Task t in tasks)
                t.Start();

            await Task.WhenAll(tasks);

            V2LE_RefreshUserAssignment_Busy = false;
        }
    }
}
