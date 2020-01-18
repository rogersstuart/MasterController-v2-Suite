using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Reflection;
using System.Diagnostics;
using MCICommon;

namespace MasterControllerInterface
{
    public delegate void ActiveListsChanged(object sender, string[] e);

    public partial class ListManagerForm : Form
    {
        public event ActiveListsChanged Changed;

        private ListMember[] members;

        private DataTable dt = new DataTable("results");

        string previous_search = ""; //remember the last search to prevent spaces from causing an additional search

        public ListManagerForm()
        {
            InitializeComponent();

            //Set Double buffering on the Grid using reflection and the bindingflags enum.
            typeof(DataGridView).InvokeMember("DoubleBuffered", BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.SetProperty, null,
            dataGridView1, new object[] { true });

            tabControl1.TabPages.Remove(tabPage3);

            string[] to_add = ConfigurationManager.ListPaths;
            bool[] to_add_check_state = ConfigurationManager.ListPathCheckStates;
            for(int index_counter = 0; index_counter < to_add.Length; index_counter++)
                checkedListBox1.SetItemChecked(checkedListBox1.Items.Add(to_add[index_counter]), to_add_check_state[index_counter]);

            GenerateTable();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //add card list
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Filter = "Supported Extentions (*.xlsx)|*.xlsx";
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                ofd.FileName = "";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filename in ofd.FileNames)
                        checkedListBox1.SetItemChecked(checkedListBox1.Items.Add(filename), true);

                    WriteStateToConfigurationManager();

                    //members = await ListUtilities.GetListMembers(checkedListBox1.CheckedItems.Cast<string>().ToArray());
                }
            }
        }

        private void WriteStateToConfigurationManager()
        {
            ConfigurationManager.ListPaths = checkedListBox1.Items.Cast<string>().ToArray();
            ConfigurationManager.ListPathCheckStates = GetCheckedListBox1ItemCheckStates();
        }

        private bool[] GetCheckedListBox1ItemCheckStates()
        {
            bool[] check_states = new bool[checkedListBox1.Items.Count];
            int[] checked_indicies = checkedListBox1.CheckedIndices.Cast<int>().ToArray();
            foreach (int index in checked_indicies)
                check_states[index] = true;

            return check_states;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //remove card list
            if (checkedListBox1.SelectedIndex > -1)
            {
                checkedListBox1.Items.RemoveAt(checkedListBox1.SelectedIndex);

                WriteStateToConfigurationManager();
            }
        }

        private void megeSelectedListsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //merge selected lists
        }

        private void v10ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //generate v1.0 list
            GenerateList(0);
        }

        private void v20ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //generate v2.0 list
            GenerateList(1);
        }

        private void GenerateList(int type)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Supported Extentions (*.xlsx)|*.xlsx";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ListUtilities.CreateWorkbook(sfd.FileName, type);
                    checkedListBox1.Items.Add(sfd.FileName);

                    if (MessageBox.Show(this, "Sync", "Sync Checked List Members With New List?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        SyncListMembers();
                }
            }
        }

        private void SyncListMembers()
        {
            string[] paths = checkedListBox1.CheckedItems.Cast<string>().ToArray();
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //add card
            using (ExcelPackage p = new ExcelPackage())
            {
                p.Workbook.Worksheets.Add("Access Control List");
                ExcelWorksheet es = p.Workbook.Worksheets[1];

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        //sync list members
        private void syncListSerialsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private async void textBox1_TextChanged(object sender, EventArgs e)
        {
            await SearchAndFill();
        }

        private async Task SearchAndFill()
        {
            string tbt = textBox1.Text.Trim();

            if (tbt != previous_search)
            {
                dataGridView1.Enabled = false;

                previous_search = tbt;

                if (tbt != "")
                {
                    if (tbt.Length > 1)
                    {
                        if (tbt[0] == '*' && tbt[1] == 'h')
                        {
                            try
                            {
                                tbt = UInt64.Parse(tbt.Substring(2), System.Globalization.NumberStyles.HexNumber) + "";
                            }
                            catch (Exception ex)
                            {
                                return;
                            }
                        }
                    }
                    else
                        if (tbt[0] == '*')
                            tbt = "";

                    DataTable local_dt = null;

                    await Task.Run(() =>
                    {
                        local_dt = dt.Clone();
                        local_dt.Clear();

                        for(int index_counter = 0; index_counter < members.Length; index_counter++)
                        {
                            ListMember lm = members[index_counter];
                            if (lm.Contains(tbt))
                            local_dt.Rows.Add(new Func<Object[]>(() =>
                            {
                                Object[] objects = lm.GetValues();
                                objects[objects.Length - 1] = index_counter;
                                return objects;
                            }).Invoke());
                        }
                    });

                    dataGridView1.DataSource = local_dt;
                    dt = local_dt;
                }
                else
                    dt.Clear();

                dataGridView1.Enabled = true;
            }
        }

        private void GenerateTable()
        {
            string[] column_names = new string[] { "Source List", "Source Row", "Active", "Serial", "UID", "Cardholder", "Description", "Active Days", "Active Times", "key"};
            DataColumn[] columns = new DataColumn[column_names.Length];

            for (int column_counter = 0; column_counter < column_names.Length; column_counter++)
            {
                columns[column_counter] = new DataColumn();

                columns[column_counter].ColumnName = column_names[column_counter];
                columns[column_counter].DataType = Type.GetType("System.String");

                dt.Columns.Add(columns[column_counter]);
            }

            dataGridView1.DataSource = dt;

            dataGridView1.Columns[dataGridView1.Columns.Count - 1].Visible = false;
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            int selected_member = Convert.ToInt32(dataGridView1[dataGridView1.Columns.Count - 1, e.RowIndex].Value);

            string[] member_properties = members[selected_member].GetValues().SubArray(0, 9).Cast<string>().ToArray();

            //fill the stuff in
            FillTabPage3(member_properties);

            tabControl1.TabPages.Add(tabPage3);
            tabControl1.SelectedTab = tabPage3;
        }

        private void FillTabPage3(string[] member_properties)
        {
            label10.Text = member_properties[0];
            label11.Text = member_properties[1];
            label12.Text = member_properties[2];

            textBox2.Text = member_properties[3];
            textBox3.Text = member_properties[4];
            textBox4.Text = member_properties[5];
            textBox5.Text = member_properties[6];
            textBox6.Text = member_properties[7];
            textBox7.Text = member_properties[8];
        }

        private void checkedListBox1_Click(object sender, EventArgs e)
        {
            
        }

        private async void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage2)
            {
                string[] list_paths = checkedListBox1.CheckedItems.Cast<string>().ToArray();

                //check to make sure none of the source lists are in use
                if (!FileChecks(list_paths))
                    return;
                
                //update the members list because they may have changed
                members = await ListUtilities.GetListMembers(list_paths);

                //force a search because the search items may have changed
                previous_search = "";
                await SearchAndFill();

                textBox1.Focus();
            }

            if (tabControl1.SelectedTab != tabPage3)
            {
                AcceptButton = null;

                if (tabControl1.TabPages.Contains(tabPage3))
                    tabControl1.TabPages.Remove(tabPage3);

                tabPage3.Name = "Edit";
            }
            else
                AcceptButton = button3;
        }

        private bool FileChecks(string path)
        {
            return FileChecks(new string[] { path});
        }

        private bool FileChecks(string[] paths)
        {
            List<Process> blocking_processes = new List<Process>();
            foreach (string path in paths)
                blocking_processes.AddRange(FileUtil.WhoIsLocking(path));

            if (blocking_processes.Count() > 0)
            {
                DialogResult result = MessageBox.Show(this, "One or more of the selected lists is currently in use by another process. Would you like to terminate the blocking processes to continue?", "Error",
                    MessageBoxButtons.OKCancel);

                if (result == DialogResult.OK)
                {
                    foreach (Process p in blocking_processes)
                        p.Kill();

                    return true;
                }
                else
                    if (result == DialogResult.Cancel)
                {
                    tabControl1.SelectedTab = tabPage1;
                    return false;
                }
            }

            return true;
        }

        //save the editor values
        private void button3_Click_1(object sender, EventArgs e)
        {
            if (!FileChecks(label10.Text))
                return;

            try
            {
                ListUtilities.WriteMemberToList(new ListMember
                    (
                        label10.Text,
                        Convert.ToInt32(label11.Text),
                        Convert.ToBoolean(label12.Text),
                        Convert.ToInt32(textBox2.Text.Trim()),

                        //Convert.ToUInt64(textBox3.Text.Trim()),
                        new Func<UInt64>(() =>
                        {
                            string tbt = textBox3.Text.Trim();
                            if (tbt.Length > 1)
                                if (tbt[0] == '*' && tbt[1] == 'h')
                                    tbt = UInt64.Parse(tbt.Substring(2), System.Globalization.NumberStyles.HexNumber) + "";

                            return Convert.ToUInt64(tbt);
                        }).Invoke(),

                        textBox4.Text.Trim(),
                        textBox5.Text.Trim(),
                        textBox6.Text.Trim(),
                        textBox7.Text.Trim()
                    ));
            }
            catch(Exception ex)
            {
                return;
            }

            tabControl1.TabPages.Remove(tabPage3);

            tabControl1.SelectedTab = tabPage2;
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (checkedListBox1.SelectedIndex > -1)
            {
                button2.Enabled = true;
                button4.Enabled = true;
            }
            else
            {
                button2.Enabled = false;
                button4.Enabled = false;
            }
        }

        //add new card to selected list
        private void button4_Click(object sender, EventArgs e)
        {
            ListMember member = ListUtilities.GenerateNewMember((string)checkedListBox1.SelectedItem);

            FillTabPage3(member.GetValues().SubArray(0, 9).Cast<string>().ToArray());

            tabPage3.Name = "Add";
            tabControl1.TabPages.Add(tabPage3);

            tabControl1.SelectedTab = tabPage3;
        }

        private void checkedListBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                foreach (int index in Enumerable.Range(0, checkedListBox1.Items.Count))
                    checkedListBox1.SetItemCheckState(index, checkedListBox1.GetItemChecked(index) ? CheckState.Unchecked : CheckState.Checked);

                ConfigurationManager.ListPathCheckStates = GetCheckedListBox1ItemCheckStates();
            }
        }

        private void ListManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        public string[] CheckedLists
        {
            get
            {
                return checkedListBox1.CheckedItems.Cast<string>().ToArray();
            }
        }

        private void ListManager_VisibleChanged(object sender, EventArgs e)
        {
            Changed(this, CheckedLists);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //sync list members
        }
    }
}
