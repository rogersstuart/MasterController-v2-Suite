using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace DoorControl
{
    public partial class ProgressDialog : Form
    {
        Object step_lock = new Object();

        public ProgressDialog(string title)
        {
            InitializeComponent();

            progressBar1.Minimum = 0;
            progressBar1.Maximum = 1;
            progressBar1.Step = 1;

            label1.Text = "";

            Text = title;
        }

        public void Step()
        {
            Invoke((MethodInvoker)(() => { progressBar1.PerformStep(); Refresh(); }));
        }

        public void SetMarqueeStyle()
        {
            Invoke((MethodInvoker)(() => { progressBar1.Style = ProgressBarStyle.Marquee; Refresh(); }));
        }


        public void SyncStep()
        {
            Task.Run(() =>
            {
                Invoke((MethodInvoker)(() =>
                {
                    lock (step_lock)
                    {
                        progressBar1.PerformStep();
                        Refresh();
                    }
                }));
            });
        }

        public void Reset()
        {
            Invoke((MethodInvoker)(() =>
            {
                progressBar1.Style = ProgressBarStyle.Blocks;

                progressBar1.Minimum = 0;
                progressBar1.Maximum = 1;
                progressBar1.Step = 1;
                progressBar1.Value = 0;

                label1.Text = "";

                Refresh();
            }));
        }

        public int Minumum
        {
            get
            {
                return (int)Invoke(new Func<int>(() => { return progressBar1.Minimum; }));
            }

            set
            {
                Invoke((MethodInvoker)(() => { progressBar1.Minimum = value; }));
            }
        }

        public int Maximum
        {
            get
            {
                return (int)Invoke(new Func<int>(() => { return progressBar1.Maximum; }));
            }

            set
            {
                Invoke((MethodInvoker)(() => { progressBar1.Maximum = value; }));
            }
        }

        public string LabelText
        {
            get
            {
                return (string)Invoke(new Func<string>(() => { return label1.Text; }));
            }

            set
            {
                Invoke((MethodInvoker)(() => { label1.Text = value; Refresh(); }));
            }
        }
    }
}
