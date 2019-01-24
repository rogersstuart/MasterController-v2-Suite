using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using MySql.Data.MySqlClient;
using MCICommon;

namespace MasterControllerInterface
{
    public partial class UserEditorForm_Dep : Form
    {
        private MySqlDataAdapter mySqlDataAdapter;

        private DataTable dt = new DataTable("results");

        private string[] column_names = {"user_id", "name", "description"};

        string previous_search = null; //remember the last search to prevent spaces from causing an additional search

        private System.Timers.Timer update_holdoff_timer = new System.Timers.Timer(500);

        public UserEditorForm_Dep()
        {
            InitializeComponent();

            //Set Double buffering on the Grid using reflection and the bindingflags enum.
            typeof(DataGridView).InvokeMember("DoubleBuffered", BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.SetProperty, null,
            dataGridView1, new object[] { true });

            update_holdoff_timer.Elapsed += async (sender, e) =>  await LookupAndDisplay(textBox1.Text.Trim());
            update_holdoff_timer.SynchronizingObject = this;
        }

        private async Task LookupAndDisplay(string query)
        {
            if (query != previous_search || previous_search == null)
            {
                dataGridView1.Enabled = false;

                previous_search = query;

                using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                {
                    await sqlconn.OpenAsync();

                    List<string> table_names = new List<string>();

                    using (MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn))
                    using (MySqlDataReader reader = cmdName.ExecuteReader())
                        while (reader.Read())
                            table_names.Add(reader.GetString(0));

                    if (!table_names.Contains("users"))
                    {
                        string cmdstr = "CREATE TABLE `accesscontrol`.`users` (`user_id` BIGINT UNSIGNED NOT NULL,`name` VARCHAR(255) NOT NULL,`description` VARCHAR(255) NULL,PRIMARY KEY (`user_id`),UNIQUE INDEX `user_id_UNIQUE` (`user_id` ASC));";

                        using (MySqlCommand sqlcmd = new MySqlCommand(cmdstr, sqlconn))
                            await sqlcmd.ExecuteNonQueryAsync();
                    }

                    //

                    if (query != "")
                    {
                        if (query.Length > 1)
                        {
                            if (query[0] == '*' && query[1] == 'h')
                            {
                                try
                                {
                                    query = UInt64.Parse(query.Substring(2), System.Globalization.NumberStyles.HexNumber) + "";
                                }
                                catch (Exception ex)
                                {
                                    dataGridView1.DataSource = null;

                                    return;
                                }
                            }
                        }
                        else
                            if (query[0] == '*')
                            query = "";
                    }

                    if (query == "")
                        mySqlDataAdapter = new MySqlDataAdapter("select * from users", sqlconn);
                    else
                    {
                        string[] substrs = query.Split(' ');

                        string cmd = "SELECT * FROM users WHERE ";

                        foreach (string str in substrs)
                            for (int col = 1; col < column_names.Length; col++)
                                cmd += column_names[col] + " LIKE " + "'%" + str + "%'" + " OR ";

                        cmd = cmd.Substring(0, cmd.Length - 3);

                        mySqlDataAdapter = new MySqlDataAdapter(cmd, sqlconn);
                    }

                    DataSet ds = new DataSet();

                    await mySqlDataAdapter.FillAsync(ds);

                    dataGridView1.DataSource = null;
                    dataGridView1.DataSource = ds.Tables[0];

                    dataGridView1.Columns[0].ReadOnly = true;
                    //dataGridView1.Columns[0].Visible = false;

                    dataGridView1.Enabled = true;
                }
            }
        }

        

        private async void dataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            dataGridView1.Enabled = false;

            e.Row.Cells[0].Value = await DatabaseUtilities.GenerateUniqueUserID(MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties);

            dataGridView1.Enabled = true;

            dataGridView1.Focus();
        }

        private void dataGridView1_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            DataTable changes = ((DataTable)dataGridView1.DataSource).GetChanges();

            if (changes != null)
            {
                MySqlCommandBuilder mcb = new MySqlCommandBuilder(mySqlDataAdapter);
                mySqlDataAdapter.UpdateCommand = mcb.GetUpdateCommand();
                mySqlDataAdapter.Update(changes);
                ((DataTable)dataGridView1.DataSource).AcceptChanges();
            }
        }

        private void dataGridView1_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            DataTable changes = ((DataTable)dataGridView1.DataSource).GetChanges();

            if (changes != null)
            {
                MySqlCommandBuilder mcb = new MySqlCommandBuilder(mySqlDataAdapter);
                mySqlDataAdapter.UpdateCommand = mcb.GetUpdateCommand();
                mySqlDataAdapter.Update(changes);
                ((DataTable)dataGridView1.DataSource).AcceptChanges();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            update_holdoff_timer.Stop();
            update_holdoff_timer.Start();
        }

        private async void UserEditorForm_Load(object sender, EventArgs e)
        {
            await LookupAndDisplay("");
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {

        }
    }
}
