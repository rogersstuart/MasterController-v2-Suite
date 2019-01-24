using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    internal partial class MonitorSelectionForm : Form
    {
        public MonitorSelectionForm(List<ExpanderMonitor> expmons, List<PanelMonitor> pnlmons)
        {
            InitializeComponent();

            ArrayList dslist = new ArrayList();
            dslist.AddRange(expmons);
            dslist.AddRange(pnlmons);

            listBox1.DataSource = dslist;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        public Object SelectedMonitor
        {
            get
            {
                return listBox1.SelectedItem;
            }
        }
    }
}
