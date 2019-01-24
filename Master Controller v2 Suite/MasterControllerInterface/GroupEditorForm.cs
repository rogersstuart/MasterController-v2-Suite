using MCICommon;
using MCICommon.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MasterControllerInterface
{
    public partial class GroupEditorForm : Form
    {
        ulong group_id;
        string group_name;

        List<ulong> group_users;
        Dictionary<ulong, string> all_users;

        Dictionary<ulong, string> displayed_group_users = new Dictionary<ulong, string>();
        Dictionary<ulong, string> displayed_unassociated_users = new Dictionary<ulong, string>();

        bool is_filtered = false;

        Dictionary<ulong, string> sub_displayed_group_users = new Dictionary<ulong, string>();
        Dictionary<ulong, string> sub_displayed_unassociated_users = new Dictionary<ulong, string>();

        public GroupEditorForm(ulong group_id, string group_name)
        {
            this.group_id = group_id;
            this.group_name = group_name.Trim();

            InitializeComponent();

            Text = "Editing: " + (group_name == "" ? group_id + "" : group_name);
            label1.Text = "Users In: " + (group_name == "" ? group_id + "" : group_name);
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private async void button5_Click(object sender, EventArgs e)
        {
            //remove from group

            var selected_indicies = listBox1.SelectedIndices.Cast<int>().ToArray();

            if (is_filtered)
            {
                var tmp = sub_displayed_group_users.AsParallel().Select((x, index) => new { x, index }).Where(y => selected_indicies.Contains(y.index)).Select(z => z.x.Key);

                selected_indicies = displayed_group_users.AsParallel().Select((x, index) => new { x, index })
                    .Where(y => tmp.Contains(y.x.Key)).Select(z => z.index).ToArray();
            }

            textBox1.Clear();
            is_filtered = false;

            await Task.Run(() =>
            {
                var selected_users = displayed_group_users.AsParallel().Select((x, index) => new { x, index })
                .Where(y => selected_indicies.Contains(y.index))
                .Select(z => z.x)
                .ToDictionary(k => k.Key, k => k.Value);

                foreach (var user in selected_users)
                {
                    displayed_group_users.Remove(user.Key);
                    displayed_unassociated_users.Add(user.Key, user.Value);
                }

                displayed_group_users = displayed_group_users.AsParallel().OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                displayed_unassociated_users = displayed_unassociated_users.AsParallel().OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                Invoke((MethodInvoker)(() =>
                {
                    listBox1.DataSource = displayed_group_users.Values.ToList();
                    listBox2.DataSource = displayed_unassociated_users.Values.ToList();
                }));

                var indicies_containing_selected_users = displayed_unassociated_users.AsParallel().Select((y, index) => new { y, index })
                .Where(z => selected_users.Keys.Contains(z.y.Key))
                .Select(m => m.index);

                Invoke((MethodInvoker)(() =>
                {
                    listBox2.ClearSelected();
                    foreach (var index in indicies_containing_selected_users)
                        listBox2.SetSelected(index, true);
                }));

            });
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            //add to group

            var selected_indicies = listBox2.SelectedIndices.Cast<int>().ToArray();

            if(is_filtered)
            {
                var tmp = sub_displayed_unassociated_users.AsParallel().Select((x, index) => new { x, index }).Where(y => selected_indicies.Contains(y.index)).Select(z => z.x.Key);

                selected_indicies = displayed_unassociated_users.AsParallel().Select((x, index) => new { x, index })
                    .Where(y => tmp.Contains(y.x.Key)).Select(z => z.index).ToArray();
            }

            textBox1.Clear();
            is_filtered = false;

            await Task.Run(() =>
            {
                var selected_users = displayed_unassociated_users.AsParallel().Select((x, index) => new { x, index })
                .Where(y => selected_indicies.Contains(y.index))
                .Select(z => z.x)
                .ToDictionary(k => k.Key, k => k.Value);

                foreach (var user in selected_users)
                {
                    displayed_unassociated_users.Remove(user.Key);
                    displayed_group_users.Add(user.Key, user.Value);
                }

                displayed_group_users = displayed_group_users.AsParallel().OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                displayed_unassociated_users = displayed_unassociated_users.AsParallel().OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                Invoke((MethodInvoker)(() =>
                {
                    listBox1.DataSource = displayed_group_users.Values.ToList();
                    listBox2.DataSource = displayed_unassociated_users.Values.ToList();
                }));

                var indicies_containing_selected_users = displayed_group_users.AsParallel().Select((y, index) => new { y, index})
                .Where(z => selected_users.Keys.Contains(z.y.Key))
                .Select(m => m.index);

                Invoke((MethodInvoker)(() =>
                {
                    listBox1.ClearSelected();
                    foreach (var index in indicies_containing_selected_users)
                        listBox1.SetSelected(index, true);
                }));

            });
        }

        private async void GroupEditorForm_Shown(object sender, EventArgs e)
        {
            //form shown

            ProgressDialog pgd = new ProgressDialog("Loading");
            pgd.Show();
            pgd.SetMarqueeStyle();

            Task[] waiting = new Task[]{ Task.Run(async () => { group_users = await GroupDBUtilities.GetUsersInGroup(group_id); }),
                Task.Run(async () => { all_users = await UserDBUtilities.GetDictionaryDescriptionForAllUsers(); }) };

            await Task.WhenAll(waiting);

            waiting = new Task[]{ Task.Run(() =>
            {
                displayed_unassociated_users = all_users.AsParallel()
                .Where(y => !group_users.Contains(y.Key))
                .OrderBy(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
            }),

            Task.Run(() =>
            {
                displayed_group_users = all_users.AsParallel()
                .Where(y => group_users.Contains(y.Key))
                .OrderBy(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
            })};

            await Task.WhenAll(waiting);

            listBox1.DataSource = displayed_group_users.Values.ToList();
            listBox2.DataSource = displayed_unassociated_users.Values.ToList();

            pgd.Dispose();
        }

        private void listBox2_MouseEnter(object sender, EventArgs e)
        {
            listBox2.Focus();
        }

        private void listBox1_MouseEnter(object sender, EventArgs e)
        {
            listBox1.Focus();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //save button
 
            ProgressDialog pgd = new ProgressDialog("Saving Group");
            

            await GroupDBUtilities.SetUsersInGroup(group_id, displayed_group_users.Keys.ToArray(), pgd);
            //pgd.Dispose();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            //clear filter
            textBox1.Clear();
        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim() == "")
            {
                is_filtered = false;

                listBox1.DataSource = displayed_group_users.Values.ToList();
                listBox2.DataSource = displayed_unassociated_users.Values.ToList();
            }
            else
            {
                is_filtered = true;

                var text = textBox1.Text.Trim().ToLower();

                sub_displayed_group_users = displayed_group_users.Where(x => x.Value.ToLower().Contains(text)).OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                sub_displayed_unassociated_users = displayed_unassociated_users.Where(x => x.Value.ToLower().Contains(text)).OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                listBox1.DataSource = sub_displayed_group_users.Values.ToList();
                listBox2.DataSource = sub_displayed_unassociated_users.Values.ToList();
            }
        }

        private void textBox1_MouseEnter(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        
        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < listBox1.Items.Count; i++)
                listBox1.SetSelected(i, true);
        }

        private void listBox2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < listBox2.Items.Count; i++)
                listBox2.SetSelected(i, true);
        }
    }
}
