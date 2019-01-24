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
    public partial class TextBoxDialog : Form
    {
        public TextBoxDialog(string labeltext)
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //ok
            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //cancel
            DialogResult = DialogResult.Cancel;
        }

        public string TextResult
        {
            get
            {
                return textBox1.Text.Trim();
            }
        }
    }
}
