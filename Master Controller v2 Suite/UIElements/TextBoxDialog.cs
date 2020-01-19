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
    public partial class TextBoxDialog : Form
    {
        private string entered_text = "";

        public TextBoxDialog(string title, string message)
        {
            InitializeComponent();

            Text = title;

            label1.Text = message;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //okay button
            entered_text = textBox1.Text.Trim();

            if (entered_text != "")
                DialogResult = DialogResult.OK;
            else
                DialogResult = DialogResult.Cancel;
        }

        public string TextBoxText
        {
            get
            {
                if (IsDisposed)
                    return entered_text;
                else
                    return textBox1.Text.Trim();
            }

            set
            {
                textBox1.Text = value;
            }
        }

        private void textBox1_MouseEnter(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        private void TextBoxDialog_Shown(object sender, EventArgs e)
        {
            textBox1.Focus();
        }
    }
}
