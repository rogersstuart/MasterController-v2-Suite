using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using MySql.Data.MySqlClient;
using System.IO;
using System.Reflection;
using ICSharpCode.SharpZipLib.Core;
using MCICommon;
using UIElements;

namespace MasterControllerInterface
{
    public partial class ExportForm : Form
    {
        public ExportForm()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == 0)
            {
                //create database backup

                SaveFileDialog sfd = new SaveFileDialog();

                sfd.Filter = "DB Backup Files (*.db2bak)|*.db2bak";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                sfd.FileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ProgressDialog pgd = new ProgressDialog("Exporting Database");
                    pgd.Show(this);

                    List<string> table_names = new List<string>();

                    using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                    {
                        pgd.LabelText = "Opening Database Connection";

                        await sqlconn.OpenAsync();

                        pgd.Step();
                        pgd.Reset();
                        pgd.LabelText = "Retrieving Table List";

                        using (MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn))
                        using (MySqlDataReader reader = cmdName.ExecuteReader())
                            while (await reader.ReadAsync())
                                table_names.Add(reader.GetString(0));

                        pgd.Step();

                    }

                    MemoryStream[] output_streams = new MemoryStream[table_names.Count()];
                    for (int i = 0; i < table_names.Count(); i++)
                        output_streams[i] = new MemoryStream();

                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 8
                    };

                    pgd.Reset();
                    pgd.LabelText = "Converting Tables";
                    pgd.Maximum = table_names.Count();

                    //generate csv files for all tables in parallel and write to an in memory stream
                    Parallel.For(0, table_names.Count(), parallelOptions, async i =>
                      {
                          MemoryStream local_stream = output_streams[i];
                          StreamWriter writer = new StreamWriter(local_stream);

                          string table_name = table_names[i];

                          List<string> column_names = new List<string>();

                          using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                          {
                              await sqlconn.OpenAsync();

                              using (MySqlCommand cmdName = new MySqlCommand("SHOW COLUMNS FROM `" + table_name + "`;", sqlconn))
                              {
                                  //cmdName.Parameters.AddWithValue("@table_name", table_name);

                                  using (MySqlDataReader reader = cmdName.ExecuteReader())
                                      while (await reader.ReadAsync())
                                          column_names.Add(reader.GetString(0));
                              }

                              //add names row
                              foreach (string column_name in column_names)
                                  await writer.WriteAsync(column_name + (column_names.Last() == column_name ? "," : ","));

                              await writer.WriteLineAsync();

                              using (MySqlCommand cmdName = new MySqlCommand("select * from `" + table_name + "`;", sqlconn))
                              {
                                  using (MySqlDataReader reader = cmdName.ExecuteReader())
                                  {
                                      MethodInfo genericFunction = reader.GetType().GetMethod("GetFieldValue");

                                      while (await reader.ReadAsync())
                                      {
                                          int field_index = 0;
                                          foreach (string column_name in column_names)
                                          {
                                              Type t = reader.GetFieldType(column_name);

                                              MethodInfo realFunction = genericFunction.MakeGenericMethod(t);
                                              var ret = realFunction.Invoke(reader, new object[] { field_index });

                                              await writer.WriteAsync(ret + ",");

                                              field_index++;
                                          }

                                          await writer.WriteLineAsync();
                                      }
                                  }
                              }

                              await writer.FlushAsync();
                          }

                          pgd.SyncStep();
                      });

                    pgd.Reset();
                    pgd.LabelText = "Compressing Tables in Memory";
                    pgd.Maximum = table_names.Count()-1;

                    using (MemoryStream zip_stream = new MemoryStream())
                    using (ZipOutputStream zos = new ZipOutputStream(zip_stream))
                    {

                        zos.SetLevel(9);

                        for (int i = 0; i < table_names.Count(); i++)
                        {
                            ZipEntry ze = new ZipEntry(table_names[i] + ".csv");

                            ze.DateTime = DateTime.Now;
                            ze.Size = output_streams[i].Length;

                            zos.PutNextEntry(ze);

                            output_streams[i].Position = 0;

                            StreamUtils.Copy(output_streams[i], zos, new byte[4096]);

                            zos.CloseEntry();

                            pgd.Step();
                        }

                        await zos.FlushAsync();

                        zos.Finish();

                        pgd.Reset();
                        pgd.LabelText = "Writing Database Backup Archive to Disk";

                        zip_stream.Position = 0;
                        File.WriteAllBytes(sfd.FileName, zip_stream.ToArray());

                        pgd.Step();
                    }

                    pgd.Dispose();
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
                button1.Enabled = true;
            else
                button1.Enabled = false;
        }
    }
 }
