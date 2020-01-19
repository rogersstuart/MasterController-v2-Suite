using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Reflection;
using ICSharpCode.SharpZipLib.Core;
using MCICommon;
using Newtonsoft.Json;

namespace MasterControllerInterface
{
    public static class DBBackupAndRestore
    {
        public static Task<BackupProperties> GetBackupProperties(string file_name)
        {
            return Task.Run(() =>
            {
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(file_name)))
                {
                    ZipFile zf = new ZipFile(ms);
                    zf.IsStreamOwner = false;

                    byte[] buffer = new byte[4096];
                    using (MemoryStream zps = new MemoryStream())
                    {
                        StreamUtils.Copy(zf.GetInputStream(zf[1]), zps, buffer);

                        return JsonConvert.DeserializeObject<BackupProperties>(Encoding.ASCII.GetString(zps.ToArray()));
                    }
                }
            });
        }

        public static Task Restore(string file_name, DatabaseConnectionProperties dbconnprop = null)
        {
            return Task.Run(async () =>
            {
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(file_name)))
                {
                    ZipFile zf = new ZipFile(ms);
                    zf.IsStreamOwner = false;

                    byte[] buffer = new byte[4096];
                    using (MemoryStream zps = new MemoryStream())
                    {
                        StreamUtils.Copy(zf.GetInputStream(zf[0]), zps, buffer);

                        string connection_string;
                        if (dbconnprop != null)
                            connection_string = dbconnprop.ConnectionString;
                        else
                            connection_string = MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString;

                        using (MySqlConnection sqlconn = new MySqlConnection(connection_string))
                        using (MySqlCommand cmd = new MySqlCommand())
                        using (MySqlBackup mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = sqlconn;
                            await sqlconn.OpenAsync();
                            mb.ImportFromMemoryStream(zps);
                        }
                    }
                }
            });
        }

        public static Task Backup(string file_name)
        {
            return Task.Run(async () =>
            {
                DateTime now_is = DateTime.Now;

                

                using (MemoryStream zip_stream = new MemoryStream())
                using (ZipOutputStream zos = new ZipOutputStream(zip_stream))
                {

                    zos.SetLevel(9);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                        using (MySqlCommand cmd = new MySqlCommand())
                        using (MySqlBackup mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = sqlconn;
                            await sqlconn.OpenAsync();
                            mb.ExportToMemoryStream(ms);
                        }

                        ZipEntry ze = new ZipEntry("dbbak.sql");

                        ze.DateTime = now_is;
                        ze.Size = ms.Length;

                        zos.PutNextEntry(ze);

                        ms.Position = 0;

                        StreamUtils.Copy(ms, zos, new byte[4096]);

                        zos.CloseEntry();
                        await zos.FlushAsync();

                    }

                //

                    using (MemoryStream ms = new MemoryStream())
                    {

                        var backprop = new BackupProperties();
                        backprop.Timestamp = now_is;
                        var corrected_var = MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties;
                        corrected_var.UID = "";
                        corrected_var.Password = "";
                        backprop.Database = corrected_var;

                        var bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(backprop));
                        ms.Write(bytes, 0, bytes.Length);

                        ZipEntry ze = new ZipEntry("properties.json");

                        ze.DateTime = DateTime.Now;
                        ze.Size = ms.Length;

                        zos.PutNextEntry(ze);

                        ms.Position = 0;

                        StreamUtils.Copy(ms, zos, new byte[4096]);


                        zos.CloseEntry();
                        await zos.FlushAsync();

                    }

                    

                    zos.Finish();

                    File.WriteAllBytes(file_name, zip_stream.ToArray());
                }
            });
        }

            /*
            public static async Task Backup(string file_name)
            {
                await Task.Run(async () =>
                {
                    List<string> table_names = new List<string>();
                    List<string> xml_strings = new List<string>();

                    using (MySqlConnection sqlconn = new MySqlConnection(MCIv2Persistance.Config.DBConnection.ConnectionString))
                    {
                        await sqlconn.OpenAsync();

                        using (MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn))
                        using (MySqlDataReader reader = cmdName.ExecuteReader())
                            while (reader.Read())
                                table_names.Add(reader.GetString(0));
                    }

                    MemoryStream[] streams = new MemoryStream[table_names.Count()];
                    for (int i = 0; i < streams.Length; i++)
                        streams[i] = new MemoryStream();

                    string connection_string = MCIv2Persistance.Config.DBConnection.ConnectionString;

                    Parallel.For(0, streams.Length, i =>
                    {
                        using (MySqlConnection sqlconn = new MySqlConnection(connection_string))
                        {
                            MemoryStream local_stream = streams[i];

                            MySqlDataAdapter da = new MySqlDataAdapter("select * from `" + table_names[i] + "`;", sqlconn);

                            DataSet ds = new DataSet();

                            da.Fill(ds);

                            ds.WriteXml(local_stream, XmlWriteMode.WriteSchema);
                        }
                    });

                    using (MemoryStream zip_stream = new MemoryStream())
                    using (ZipOutputStream zos = new ZipOutputStream(zip_stream))
                    {

                        zos.SetLevel(9);

                        for (int i = 0; i < table_names.Count(); i++)
                        {
                            ZipEntry ze = new ZipEntry(table_names[i] + ".xml");

                            ze.DateTime = DateTime.Now;
                            ze.Size = streams[i].Length;

                            zos.PutNextEntry(ze);

                            streams[i].Position = 0;

                            StreamUtils.Copy(streams[i], zos, new byte[4096]);

                            zos.CloseEntry();
                        }

                        await zos.FlushAsync();

                        zos.Finish();

                        zip_stream.Position = 0;
                        File.WriteAllBytes(file_name, zip_stream.ToArray());
                    }
                });
            }
            */
        }
}
