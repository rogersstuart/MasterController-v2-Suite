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
    internal partial class GroupEditorForm : Form
    {
        AccessControlGroup group;
        public GroupEditorForm(List<string> tables, List<ExpanderMonitor> expmons, List<PanelMonitor> pnlmons)
        {
            InitializeComponent();

            listBox1.DataSource = tables;
            listBox2.DataSource = expmons;
            checkedListBox1.DataSource = pnlmons;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //accept
            //check for errors

            //create group
            try
            {
                //List<PanelMonitor> pnlmons = new List<PanelMonitor>();
                //pnlmons.AddRange(checkedListBox1.CheckedItems<PanelMonitor>);
                
                group = new AccessControlGroup(textBox1.Text.Trim(),
                                               (string)listBox1.SelectedItem,
                                               (ExpanderMonitor)listBox2.SelectedItem,
                                               checkedListBox1.CheckedItems.Cast<PanelMonitor>().ToList());

                DialogResult = DialogResult.OK;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error Creating Group");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //cancel
            DialogResult = DialogResult.Cancel;
        }

        public AccessControlGroup Group
        {
            get
            {
                return group;
            }
        }
    }
}
