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
    public partial class ErrorBox : Form
    {
        public ErrorBox(string message, string trace)
        {
            InitializeComponent();

            label3.Text = message;
            textBox1.Text = trace;
        }
    }
}
