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

namespace MasterControllerDotNet_Server
{
    internal partial class PanelConnectionsForm : Form
    {
        List<PanelMonitor> pnlmons;
        public PanelConnectionsForm(List<PanelMonitor> pnlmons)
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

            if(connproped.DialogResult == DialogResult.OK)
                pnlmons.Add(new PanelMonitor(connproped.ConnectionProperties));

            connproped.Dispose();

            listBox1.DataSource = null;
            listBox1.DataSource = pnlmons;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //remove
            pnlmons.ElementAt(listBox1.SelectedIndex).Stop();
            pnlmons.RemoveAt(listBox1.SelectedIndex);

            listBox1.DataSource = null;
            listBox1.DataSource = pnlmons;
        }

        //car 0 hack
        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
                ((PanelMonitor)listBox1.SelectedItem).AssociatedCar = 0;
        }

        //car 1 hack
        private void button4_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
                ((PanelMonitor)listBox1.SelectedItem).AssociatedCar = 1;
        }
    }
}
