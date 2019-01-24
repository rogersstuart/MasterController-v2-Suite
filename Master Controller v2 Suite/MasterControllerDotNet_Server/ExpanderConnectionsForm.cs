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
    internal partial class ExpanderConnectionsForm : Form
    {
        List<ExpanderMonitor> pnlmons;

        public ExpanderConnectionsForm(List<ExpanderMonitor> pnlmons)
        {
            InitializeComponent();

            this.pnlmons = pnlmons;

            listBox1.DataSource = pnlmons;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //add
            ConnectionPropertiesEditor connproped = new ConnectionPropertiesEditor();
            connproped.ShowDialog(this);

            if (connproped.DialogResult == DialogResult.OK)
                pnlmons.Add(new ExpanderMonitor(connproped.ConnectionProperties));

            connproped.Dispose();

            listBox1.DataSource = null;
            listBox1.DataSource = pnlmons;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //remove
            if (listBox1.SelectedIndex > -1)
            {
                pnlmons[listBox1.SelectedIndex].Stop();
                pnlmons.RemoveAt(listBox1.SelectedIndex);
            }

            listBox1.DataSource = null;
            listBox1.DataSource = pnlmons;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //edit hardware configuration
            if(listBox1.SelectedIndex > -1)
            {
                HardwareConfiguration hwconfig = ((ExpanderMonitor)listBox1.SelectedItem).HardwareConfiguration;
                HardwareConfigurationForm hwconfigfm = new HardwareConfigurationForm(hwconfig);
                hwconfigfm.ShowDialog(this);
                if(hwconfigfm.DialogResult == DialogResult.OK)
                    ((ExpanderMonitor)listBox1.SelectedItem).HardwareConfiguration = hwconfig;
                hwconfigfm.Dispose();
            }
        }
    }
}
