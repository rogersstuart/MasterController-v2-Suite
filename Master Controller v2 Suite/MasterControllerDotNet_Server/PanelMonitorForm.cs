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
using System.IO;

namespace MasterControllerDotNet_Server
{
    internal partial class PanelMonitorForm : Form
    {
        Object monitor_lock = new Object();
        PanelMonitor pnlmon;

        public PanelMonitorForm(PanelMonitor pnlmon)
        {
            InitializeComponent();

            this.pnlmon = pnlmon;

            
        }

        private void PanelMonitorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            pnlmon.newstate -= pnlmon_NewState;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //reset
            lock(monitor_lock)
                pnlmon.ResetPanel();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //ani blank
            lock(monitor_lock)
                pnlmon.AnimateLED(PanelCommand.ANIMATION_BLANK);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //ani red
            lock (monitor_lock)
                pnlmon.AnimateLED(PanelCommand.ANIMATION_DECLINE);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //ani green
            lock (monitor_lock)
                pnlmon.AnimateLED(PanelCommand.ANIMATION_ALT_APPROVAL);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //ani blue
            lock (monitor_lock)
                pnlmon.AnimateLED(PanelCommand.ANIMATION_APPROVAL);
        }

        private void pnlmon_NewState(object sender, PanelEventArgs pnlevargs)
        {
            //new panelstate available
            if (pnlevargs.PanelState != null)
            {
                Invoke(new Action(() => {
                    textBox1.Text = pnlevargs.PanelState.Card.UID + "";
                    textBox3.Text = pnlevargs.PanelState.Card.Timestamp.ToString();
                    textBox2.Text = TimeSpan.FromMilliseconds(pnlevargs.PanelState.Uptime).ToString();
                    textBox4.Text = pnlevargs.PanelState.Timestamp.ToString() + " " + pnlevargs.PanelState.Timestamp.Ticks;
                }));

                if (File.Exists("cards.txt"))
                {
                    if (!File.ReadAllLines("cards.txt").Contains(pnlevargs.PanelState.Card.UID + ""))
                        File.AppendAllText("cards.txt", pnlevargs.PanelState.Card.UID + Environment.NewLine);
                }
                else
                    File.AppendAllText("cards.txt", pnlevargs.PanelState.Card.UID + Environment.NewLine);
            }
        }

        private void PanelMonitorForm_Shown(object sender, EventArgs e)
        {
            pnlmon.newstate += pnlmon_NewState;
        }
    }
}
