using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using MCICommon;
using UIElements;

namespace LabelPrinting
{
    public partial class PrintLabelSetForm : Form
    {
        public PrintLabelSetForm()
        {
            InitializeComponent();

            textBox1.Text = 0.ToString();
            textBox2.Text = 0.ToString();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
                label4.Text = ofd.FileName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //print
            string[] lines = File.ReadAllLines(label4.Text).Select(t => t.Trim()).Where(t => t != "" && t != "0").ToArray();

            LabelPrinter printer = new LabelPrinter();

            ProgressDialog pgd = new ProgressDialog("Printing Labels");
            pgd.Show();
            pgd.Maximum = lines.Length - 1;

            for (int i = 0; i < lines.Length;)
            {
                pgd.LabelText = "Printing Label " + (i+1) + " of " + lines.Length;

                    if ((i + 1) < lines.Length)
                        printer.PrintTwoLabel(BaseConverter.EncodeFromBase10(Convert.ToUInt64(lines[i])), BaseConverter.EncodeFromBase10(Convert.ToUInt64(lines[i + 1])));
                    else
                        printer.PrintLabel(BaseConverter.EncodeFromBase10(Convert.ToUInt64(lines[i])));

                if ((i + 1) < lines.Length)
                {
                    i += 2;
                    pgd.Step();
                    pgd.Step();
                }
                else
                {
                    i++;
                    pgd.Step();
                }
            }
        }
    }
}
