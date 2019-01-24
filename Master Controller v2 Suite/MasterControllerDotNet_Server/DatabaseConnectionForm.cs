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
using System.Threading;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    internal partial class DatabaseConnectionForm : Form
    {
        DatabaseConnectionProperties dbprop;
        public DatabaseConnectionForm(DatabaseConnectionProperties dbprop)
        {
            InitializeComponent();

            this.dbprop = new DatabaseConnectionProperties(dbprop);

            textBox1.Text = dbprop.Hostname;
            textBox2.Text = dbprop.UID;
            textBox3.Text = dbprop.Password;
        }

        private void DatabaseConnectionForm_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //try to open a connection and display the results
            SyncProp();

            new Thread(delegate()
            {
                try
                {
                    MySqlConnection connection = new MySqlConnection(dbprop.ConnectionString);
                    connection.Open();
                    connection.Close();

                    MessageBox.Show("Successfully Opened Connection");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed To Open Connection " + ex.ToString());
                }
            }).Start();
        }

        private void DatabaseConnectionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SyncProp();
        }

        private void SyncProp()
        {
            dbprop.Hostname = textBox1.Text.Trim();
            dbprop.UID = textBox2.Text.Trim();
            dbprop.Password = textBox3.Text.Trim();
        }

        public DatabaseConnectionProperties DBConnProp
        {
            get
            {
                return new DatabaseConnectionProperties(dbprop);
            }
        }
    }
}
