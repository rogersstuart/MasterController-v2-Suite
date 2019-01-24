using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MasterControllerDotNet_Server
{
    internal partial class ActivationPropertiesEditor : Form
    {
        private bool[] mask0 = new bool[16];
        private bool[] mask1 = new bool[16];
        private bool[] value0 = new bool[16];
        private bool[] value1 = new bool[16];

        //year, month, day of month, day of week, hour, minute, second
        private bool[] comp_options = new bool[7];

        private string[] option_strings = {"Year","Month","Day of Month","Day of Week","Hour","Minute","Second"};

        public ActivationPropertiesEditor()
        {
            InitializeComponent();

            listBox1.DataSource = new List<string>();
        }

        public ActivationPropertiesEditor(ActivationProperties actprop)
        {
            InitializeComponent();
            
            dateTimePicker8.Value = actprop.RangeStart;
            dateTimePicker7.Value = actprop.RangeStart;
            dateTimePicker6.Value = actprop.RangeEnd;
            dateTimePicker5.Value = actprop.RangeEnd;

            List<string> ds = new List<string>();
            for (int index_counter = 0; index_counter < 7; index_counter++)
                if (actprop.ComparisonFlags[index_counter])
                    ds.Add(option_strings[index_counter]);

            listBox1.DataSource = null;
            listBox1.DataSource = ds;

            numericUpDown1.Value = actprop.RevertAfter;

            for (int index_counter = 0; index_counter < 16; index_counter++)
            {
                if(actprop.Exp0Mask[index_counter])

                    checkedListBox1.SetItemChecked(index_counter, true);
                if(actprop.Exp1Mask[index_counter])

                    checkedListBox3.SetItemChecked(index_counter, true);
                if(actprop.Exp0Values[index_counter])

                    checkedListBox2.SetItemChecked(index_counter, true);
                if(actprop.Exp1Values[index_counter])
                    checkedListBox4.SetItemChecked(index_counter, true);
            }
        }

        private void ConvertListsToBoolArray()
        {
            foreach (int index in checkedListBox1.CheckedIndices)//mask exp0
                mask0[index] = true;

            foreach (int index in checkedListBox2.CheckedIndices)//values exp0
                value0[index] = true;

            foreach (int index in checkedListBox3.CheckedIndices)//mask exp1
                mask1[index] = true;

            foreach (int index in checkedListBox4.CheckedIndices)//values exp1
                value1[index] = true;

            List<string> ds = (List<string>)listBox1.DataSource;
            foreach(string str in ds)
            {
                switch(str)
                {
                    case "All": comp_options = new bool[]{true, true, true, true, true, true, true}; break;
                    case "All Date": comp_options[0] = true; comp_options[1] = true; comp_options[2] = true; break;
                    case "All Time": comp_options[4] = true; comp_options[5] = true; comp_options[6] = true; break;
                    case "Year": comp_options[0] = true; break;
                    case "Month": comp_options[1] = true; break;
                    case "Day of Month": comp_options[2] = true; break;
                    case "Day of Week": comp_options[3] = true; break;
                    case "Hour": comp_options[4] = true; break;
                    case "Minute": comp_options[5] = true; break;
                    case "Second": comp_options[6] = true; break;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //add comparison option
            if(comboBox1.SelectedIndex > -1)
            {
                List<string> ds = (List<string>)listBox1.DataSource;
                ds.Add((string)comboBox1.SelectedItem);
                listBox1.DataSource = null;
                listBox1.DataSource = ds;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //remove comparison option
            if(listBox1.SelectedIndex > -1)
            {
                List<string> ds = (List<string>)listBox1.DataSource;
                ds.RemoveAt(listBox1.SelectedIndex);
                listBox1.DataSource = null;
                listBox1.DataSource = ds;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
                button2.Enabled = true;
            else
                button2.Enabled = false;
        }

        public ActivationProperties ActivationProperties
        {
            get
            {
                ConvertListsToBoolArray();

                DateTime range_start = new DateTime(dateTimePicker8.Value.Year, dateTimePicker8.Value.Month, dateTimePicker8.Value.Day,
                    dateTimePicker7.Value.Hour, dateTimePicker7.Value.Minute, dateTimePicker7.Value.Second);
                DateTime range_end = new DateTime(dateTimePicker6.Value.Year, dateTimePicker6.Value.Month, dateTimePicker6.Value.Day,
                    dateTimePicker5.Value.Hour, dateTimePicker5.Value.Minute, dateTimePicker5.Value.Second);

                return new ActivationProperties(range_start, range_end, mask0, mask1, value0, value1, comp_options, Convert.ToInt32(numericUpDown1.Value));
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //save button
            DialogResult = DialogResult.OK;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //cancel button
            DialogResult = DialogResult.Cancel;
        }
    }
}
