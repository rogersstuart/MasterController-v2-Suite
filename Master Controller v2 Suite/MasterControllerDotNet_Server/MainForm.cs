using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    public partial class MainForm : Form
    {
        DatabaseConnectionProperties dbconnprop;

        List<ExpanderMonitor> expander_monitors = new List<ExpanderMonitor>();
        List<PanelMonitor> panel_monitors = new List<PanelMonitor>();

        List<AccessControlGroup> control_groups = new List<AccessControlGroup>();
        AccessControlGroupMonitor accgrpsmon;

        //testing code
        AltMonitor altmon;
        //

        Thread closing_thread;

        volatile bool server_active = false;

        public MainForm()
        {
            InitializeComponent();

            dbconnprop = new DatabaseConnectionProperties("mccsrv0", null , "accesscontrol", "MaMCaq9Jb3fvVr7d");

            try
            {
                if (File.Exists("appstate.bin"))
                {
                    FileStream fstream = new FileStream("appstate.bin", FileMode.Open);
                    BinaryFormatter bFormatter = new BinaryFormatter();

                    dbconnprop = (DatabaseConnectionProperties)bFormatter.Deserialize(fstream);
                    expander_monitors = (List<ExpanderMonitor>)bFormatter.Deserialize(fstream);
                    panel_monitors = (List<PanelMonitor>)bFormatter.Deserialize(fstream);

                    control_groups = (List<AccessControlGroup>)bFormatter.Deserialize(fstream);

                    fstream.Close();
                }
            }
            catch (Exception ex)
            {
                ErrorLogManager.AppendLog("Failed To Read Configuration From File", true);

                try{File.Delete("appstate.bin");}
                catch (Exception ex2){ErrorLogManager.AppendLog("Failed To Delete Configuration File", true);}
            }

            AccessControlLogManager.DatabaseConnectionProperties = dbconnprop;

            ErrorLogManager.AppendLog("Application Initialized", true);
        }

        private void databaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DatabaseConnectionForm dbconfm = new DatabaseConnectionForm(dbconnprop);
            dbconfm.ShowDialog(this);
            dbconnprop = dbconfm.DBConnProp;
            dbconfm.Dispose();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //toggle server state

            if(server_active)
            {
                //server is currently active

                Stop();

                button3.Text = "Start Server";
            }
            else
            {
                //server is currently inactive

                button3.Text = "Stop Server";

                Start();
            }
        }

        private void Start()
        {
            foreach (ExpanderMonitor expmon in expander_monitors)
                expmon.Start();

            foreach (PanelMonitor pnlmon in panel_monitors)
                pnlmon.Start();

            //accgrpsmon = new AccessControlGroupMonitor(control_groups);
            //accgrpsmon.Start();

            altmon = new AltMonitor(expander_monitors[0], panel_monitors, dbconnprop, "default");
            altmon.Start();

            //start other tasks here

            server_active = true;

            ErrorLogManager.AppendLog("Server Started", true);
        }

        private void Stop()
        {
            //stop other tasks here

            if (altmon != null)
            {
                altmon.Stop();
                altmon = null;
            }

            //accgrpsmon.Stop();

            foreach (ExpanderMonitor expmon in expander_monitors)
                expmon.Stop();

            foreach (PanelMonitor pnlmon in panel_monitors)
                pnlmon.Stop();

            server_active = false;

            ErrorLogManager.AppendLog("Server Stopped", true);
        }

        private void expandersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //expanders option in menu
            ExpanderConnectionsForm expconnfm = new ExpanderConnectionsForm(expander_monitors);
            expconnfm.ShowDialog(this);
            expconnfm.Dispose();
        }

        private void panelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PanelConnectionsForm pnlconnfm = new PanelConnectionsForm(panel_monitors);
            pnlconnfm.ShowDialog(this);
            pnlconnfm.Dispose();
        }

        private void groupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GroupListForm grpfm = new GroupListForm(expander_monitors, panel_monitors, control_groups, dbconnprop);
            grpfm.ShowDialog(this);
            grpfm.Dispose();
        }

        private void monitorToolStripMenuItem_Click_2(object sender, EventArgs e)
        {
            /*
            //launch selection form
            MonitorSelectionForm monselfm = new MonitorSelectionForm(expander_monitors, panel_monitors);
            monselfm.ShowDialog(this);

            //launch monitor gui
            if (monselfm.DialogResult == DialogResult.OK)
                if (monselfm.SelectedMonitor is ExpanderMonitor)
                    new ExpanderMonitorForm((ExpanderMonitor)monselfm.SelectedMonitor).Show(this);
                else
                    if (monselfm.SelectedMonitor is PanelMonitor)
                        new PanelMonitorForm((PanelMonitor)monselfm.SelectedMonitor).Show(this);

            monselfm.Dispose();
            */
        }

        private void connectionStatsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //connection stats
        }

        private void editorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //launch database editor
            //nn

        }

        private void eventsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //events
            ExpanderEventList expeventlst = new ExpanderEventList(expander_monitors);
            expeventlst.ShowDialog(this);
            expeventlst.Dispose();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;

            if(closing_thread == null || !closing_thread.IsAlive)
            {
                closing_thread = new Thread(delegate()
                {
                    Stop();

                    //someone has tried to close the form, save the settings
                    try
                    {
                        FileStream fstream = new FileStream("appstate.bin", FileMode.Create);
                        BinaryFormatter bFormatter = new BinaryFormatter();

                        bFormatter.Serialize(fstream, dbconnprop);
                        bFormatter.Serialize(fstream, expander_monitors);
                        bFormatter.Serialize(fstream, panel_monitors);
                        bFormatter.Serialize(fstream, control_groups);

                        fstream.Close();
                    }
                    catch(Exception ex)
                    {
                        ErrorLogManager.AppendLog("Error Writing Configuration To File", true);
                    }

                    Invoke(new Action(() => Dispose()));

                    ErrorLogManager.AppendLog("Application Closed", true);
                });
                closing_thread.Start();
            }
        }

        private void errorLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //show the error log viewer
            new ErrorLogViewer().Show(this);
        }

        private void simpleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //simple database editor
            new SimpleDatabaseEditor(dbconnprop).Show(this);
        }

        private void advancedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //advanced database editor
            DatabaseEditor dbedit = new DatabaseEditor(dbconnprop);
            dbedit.ShowDialog(this);
            dbedit.Dispose();
        }

        private void backupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //backup database
        }

        private void restoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //restore database
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //import
            ImportForm importForm = new ImportForm(dbconnprop);
            importForm.Show(this);
        }

        private void logViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AccessControlLogViewer(dbconnprop).ShowDialog();
        }
    }
}
