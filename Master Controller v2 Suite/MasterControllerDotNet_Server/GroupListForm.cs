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
using MCICommon;

namespace MasterControllerDotNet_Server
{
    internal partial class GroupListForm : Form
    {
        List<ExpanderMonitor> expmons;
        List<PanelMonitor> pnlmons;
        List<AccessControlGroup> acctlgp;
        DatabaseConnectionProperties dbconnprop;

        public GroupListForm(List<ExpanderMonitor> expmons, List<PanelMonitor> pnlmons, List<AccessControlGroup> acctlgp, DatabaseConnectionProperties dbconnprop)
        {
            InitializeComponent();

            this.expmons = expmons;
            this.pnlmons = pnlmons;
            this.acctlgp = acctlgp;
            this.dbconnprop = dbconnprop;

            listBox1.DataSource = acctlgp;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //add
            List<string> table_names = new List<string>();
            using(MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
            {
                sqlconn.Open();
                MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn);
                MySqlDataReader reader = cmdName.ExecuteReader();
                while (reader.Read())
                {
                    table_names.Add(reader.GetString(0));
                }
                reader.Close();
                sqlconn.Close();
            }

            GroupEditorForm grpcfm = new GroupEditorForm(table_names, expmons, pnlmons);
            grpcfm.ShowDialog(this);

            if (grpcfm.DialogResult == DialogResult.OK)
                acctlgp.Add(grpcfm.Group);

            grpcfm.Dispose();

            listBox1.DataSource = null;
            listBox1.DataSource = acctlgp;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //edit

            listBox1.DataSource = null;
            listBox1.DataSource = acctlgp;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //delete
            if (listBox1.SelectedIndex > -1)
            {
                acctlgp.RemoveAt(listBox1.SelectedIndex);

                listBox1.DataSource = null;
                listBox1.DataSource = acctlgp;
            }
        }
    }
}
