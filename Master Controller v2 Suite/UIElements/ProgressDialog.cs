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
using MCICommon;

namespace UIElements
{
    public partial class ProgressDialog : Form, ProgressInterface
    {
        private Object step_lock = new Object();

        private int num_managed_steps = 0;
        private int _steps = 0;

        private System.Timers.Timer sync_step_refresh = new System.Timers.Timer(250);

        public ProgressDialog(string title)
        {
            InitializeComponent();

            progressBar1.Minimum = 0;
            progressBar1.Maximum = 1;
            progressBar1.Step = 1;

            label1.Text = "";

            Text = title;

            sync_step_refresh.Elapsed += (a, b) =>
            {
                Invoke((MethodInvoker)(() =>
                {
                    progressBar1.Value = _steps;
                    Refresh();
                }));
            };
        }

        public void SetTitle(string title)
        {
            Invoke((MethodInvoker)(() => { Text = title; Refresh(); }));
        }

        public void SetLabel(string text)
        {
            Invoke((MethodInvoker)(() => { label1.Text = text; Refresh(); }));
        }

        public void Step()
        {
            Invoke((MethodInvoker)(() => { progressBar1.PerformStep(); Refresh(); }));
        }

        public Task WaitStep()
        {
            return Task.Run(() =>
            {
                lock(step_lock)
                    Invoke((MethodInvoker)(() => { progressBar1.PerformStep(); Refresh();}));
            });

        }

        public void SetMarqueeStyle()
        {
            Invoke((MethodInvoker)(() => { progressBar1.Style = ProgressBarStyle.Marquee; Refresh(); }));
        }

        public void SyncStep()
        {
                lock (step_lock)
                    Step();
           
        }

        public void InitManagedStep(int num_steps)
        {
            num_managed_steps = num_steps;

            //monitor for exit condition
            Task.Run(async () =>
            {
                while (_steps < num_managed_steps)
                    await Task.Delay(100);

                sync_step_refresh.Stop();

                await Task.Delay(500);

                Invoke((MethodInvoker)(() => { Dispose(); }));
            });

            sync_step_refresh.Start();
        }

        public void ManagedStep()
        {
            Interlocked.Increment(ref _steps);
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

                //steps_remaining = 0;

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

        public void SetMaximum(int maximum)
        {
            Invoke((MethodInvoker)(() => { progressBar1.Maximum = maximum; Refresh(); }));
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
