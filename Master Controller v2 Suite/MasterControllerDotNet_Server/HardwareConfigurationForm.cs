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
    public partial class HardwareConfigurationForm : Form
    {
        private string[] expander_names = {"Floor 1","Floor 2","Floor 3","Floor 4","Floor 5","Floor 6","Floor 7","Floor 8","Floor 9","Floor 10","Floor 11","Floor 12","Floor 13","Floor 14" };
        HardwareConfiguration hwconfig;

        public HardwareConfigurationForm(HardwareConfiguration hwconfig)
        {
            InitializeComponent();

            this.hwconfig = hwconfig;
            
            checkedListBox1.Items.AddRange(expander_names);
            checkedListBox2.Items.AddRange(expander_names);

            for (int index_counter = 0; index_counter < 14; index_counter++)
            {
                checkedListBox1.SetItemChecked(index_counter, hwconfig.Expander0Configuration[index_counter]);
                checkedListBox2.SetItemChecked(index_counter, hwconfig.Expander1Configuration[index_counter]);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //save
            for (int index_counter = 0; index_counter < 14; index_counter++)
            {
                hwconfig.Expander0Configuration[index_counter] = checkedListBox1.GetItemChecked(index_counter);
                hwconfig.Expander1Configuration[index_counter] = checkedListBox2.GetItemChecked(index_counter);
            }

            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //cancel
            DialogResult = DialogResult.Cancel;
        }
    }
}
