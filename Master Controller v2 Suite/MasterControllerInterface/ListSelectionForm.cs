using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MasterControllerInterface
{
    public partial class ListSelectionForm : Form
    {
        private string selected_option;

        public ListSelectionForm(string[] options)
        {
            InitializeComponent();

            if (options == null || options.Length == 0)
                throw new Exception("The source list must not be null and the length must be greater than zero.");
            else
                comboBox1.DataSource = options;

            int max_width = 0;
            foreach (string option in options)
            {
                int width = TextRenderer.MeasureText(option, comboBox1.Font).Width;
                if (width > max_width)
                    max_width = width;
            }

            comboBox1.DropDownWidth = max_width;
        }

        public string SelectedOption
        {
            get
            {
                return selected_option;
            }
        }

        //ok
        private void button1_Click(object sender, EventArgs e)
        {
            selected_option = (string)comboBox1.SelectedItem;

            DialogResult = DialogResult.OK;
        }

        //cancel
        private void button2_Click_1(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
