using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    public partial class AccessControlLogViewer : Form
    {
        private DatabaseConnectionProperties dbconnprop;

        public AccessControlLogViewer(DatabaseConnectionProperties dbconnprop)
        {
            InitializeComponent();

            this.dbconnprop = dbconnprop;
        }

        private new async Task Refresh()
        {
            UseWaitCursor = true;
            
            foreach (Control control in Controls)
                control.Enabled = false;

            treeView1.Nodes.Clear();

            Dictionary<UInt64, List<AccessControlLogEntry>> entries = await AccessControlLogManager.GetAccessControlLogEntries();

            TreeNode allnode = new TreeNode("All");
            treeView1.Nodes.Add(allnode);

            treeView1.AfterSelect += (x, y) =>
            {
                if (y.Node.Level == 0)
                {
                    dataGridView1.Rows.Clear();

                    if (y.Node == allnode)
                    {
                        foreach (var key in entries.Keys)
                            foreach (var entry in entries[key])
                                AddRow(entry);
                    }
                    else
                    {
                        foreach (var entry in entries[Convert.ToUInt64(y.Node.Text)])
                            AddRow(entry);
                    }
                }
            };

            foreach (var key in entries.Keys)
            {
                TreeNode keyroot = new TreeNode(key + "");

                string[] table_names = await DatabaseManager.GetTableNames(dbconnprop);

                foreach (string table_name in table_names)
                {
                    TreeNode table_node = new TreeNode(table_name);

                    keyroot.Nodes.Add(table_node);
                }

                treeView1.Nodes.Add(keyroot);
            }

            foreach (Control control in Controls)
                control.Enabled = true;

            UseWaitCursor = false;
        }

        private void AddRow(AccessControlLogEntry entry)
        {
            //timestamp, description, card details, modified expander, source panel

            dataGridView1.Rows.Add(entry.Timestamp, entry.Description, entry.Card.ToString(), entry.ExpanderInfo, entry.PanelInfo);
        }

        private async void AccessControlLogViewer_Shown(object sender, EventArgs e)
        {
            await Refresh();
        }
    }
}
