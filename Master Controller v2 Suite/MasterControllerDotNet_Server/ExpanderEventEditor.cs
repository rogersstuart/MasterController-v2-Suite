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
    internal partial class ExpanderEventEditor : Form
    {
        ExpanderEvent expevent;
        public ExpanderEventEditor(List<ExpanderMonitor> expmons)
        {
            InitializeComponent();

            listBox1.DataSource = expmons;
        }

        public ExpanderEvent Event
        {
            get
            {
                return expevent;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //apply
            DateTime dt = dateTimePicker1.Value.Date + dateTimePicker2.Value.TimeOfDay;

            bool[] mask0 = new bool[16];
            bool[] mask1 = new bool[16];
            bool[] value0 = new bool[16];
            bool[] value1 = new bool[16];

            foreach(int index in checkedListBox1.CheckedIndices)//mask exp0
                mask0[index] = true;

            foreach(int index in checkedListBox2.CheckedIndices)//values exp0
                value0[index] = true;

            foreach(int index in checkedListBox3.CheckedIndices)//mask exp1
                mask1[index] = true;

            foreach(int index in checkedListBox4.CheckedIndices)//values exp1
                value1[index] = true;

            ((ExpanderMonitor)listBox1.SelectedItem).Events.Add(new ExpanderEvent(dt, mask0, mask1, value0, value1));

            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //cancel
            DialogResult = DialogResult.Cancel;
        }
    }
}
