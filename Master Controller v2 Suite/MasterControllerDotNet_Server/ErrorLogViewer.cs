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

namespace MasterControllerDotNet_Server
{
    public partial class ErrorLogViewer : Form
    {
        FileSystemWatcher watcher;

        public ErrorLogViewer()
        {
            InitializeComponent();

            RefreshText();

            watcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory);
            watcher.Filter = "log.txt";
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += log_Changed;
            watcher.EnableRaisingEvents = true;
        }

        private void RefreshText()
        {
            richTextBox1.Lines = ErrorLogManager.ReadLog();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //refresh
            RefreshText();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //clear
            ErrorLogManager.ClearLog();
            RefreshText();
        }

        private void log_Changed(object source, FileSystemEventArgs e)
        {
            Invoke(new Action(() => RefreshText()));
        }

        private void ErrorLogViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            watcher.Changed -= log_Changed;
            watcher.Dispose();
        }
    }
}
