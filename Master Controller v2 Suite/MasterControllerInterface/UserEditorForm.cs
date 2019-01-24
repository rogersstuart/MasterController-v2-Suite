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
    public partial class UserEditorForm : Form
    {
        public UserEditorForm(string title) : this(title, "", ""){}

        public UserEditorForm(string title, string name, string desc)
        {
            InitializeComponent();

            Text = title;
            textBox1.Text = name;
            textBox2.Text = desc;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //save button

            DialogResult = DialogResult.OK;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //name field changed

            button1.Enabled = textBox1.Text.Trim() != "";

            Refresh();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            //description field changed
        }

        public string NameText
        {
            get
            {
                return textBox1.Text.Trim();
            }
        }

        public string DescriptionText
        {
            get
            {
                return textBox2.Text.Trim();
            }
        }

        private void UserEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (textBox1.Text.Trim() == "")
                e.Cancel = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;

            Dispose();
        }

        private void UserEditorForm_Shown(object sender, EventArgs e)
        {
            textBox1.Focus();
        }
    }
}
