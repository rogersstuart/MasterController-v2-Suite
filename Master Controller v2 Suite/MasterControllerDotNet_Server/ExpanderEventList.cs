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
    internal partial class ExpanderEventList : Form
    {
        List<ExpanderMonitor> expmons;
        //List<List<ExpanderEvent>> expeventsl = new List<List<ExpanderEvent>>();

        List<string> eventstrings;

        public ExpanderEventList(List<ExpanderMonitor> expmons)
        {
            InitializeComponent();

            this.expmons = expmons;

            GenerateStrings();

            listBox1.DataSource = eventstrings;
        }

        private void GenerateStrings()
        {
            eventstrings = new List<string>();
            foreach (ExpanderMonitor expmon in expmons)
            {
                //expeventsl.Add(expmon.Events);

                foreach(ExpanderEvent expevent in expmon.Events)
                    eventstrings.Add(expmon.ToString() + " - " + expevent.ToString());
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //add
            ExpanderEventEditor expevented = new ExpanderEventEditor(expmons);
            expevented.ShowDialog(this);
            expevented.Dispose();

            GenerateStrings(); //lazy way

            listBox1.DataSource = null;
            listBox1.DataSource = eventstrings;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //remove
            if(listBox1.SelectedIndex > -1)
            {
                int total = 0;
                foreach(ExpanderMonitor expmon in expmons)
                {
                    total += expmon.Events.Count;
                    if(listBox1.SelectedIndex <= total)
                    {
                        total -= expmon.Events.Count;
                        int true_index = listBox1.SelectedIndex - total;

                        expmon.Events.RemoveAt(true_index);

                        break; //found it and removed it
                    }
                }

                eventstrings.RemoveAt(listBox1.SelectedIndex);

                listBox1.DataSource = null;
                listBox1.DataSource = eventstrings;
            }
        }
    }
}
