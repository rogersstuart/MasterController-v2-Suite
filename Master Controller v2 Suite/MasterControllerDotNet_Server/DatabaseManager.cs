using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    public static class DatabaseManager
    {
        public static Task<AccessProperties> GetAccessProperties(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid)
        {
            return Task.Run(new Func<AccessProperties>(() =>
            {
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using(MySqlCommand cmdName = new MySqlCommand("select data from `" + table_name + "` where uid=" + uid, sqlconn))
                        using (MySqlDataReader reader = cmdName.ExecuteReader())
                        {
                            reader.Read();

                            if (reader.IsDBNull(0))
                                return null;
                            else
                                return Utilities.ConvertByteArrayToObject<AccessProperties>((byte[])reader["data"]);
                        }
                }
            }));
        }

        public static Task SetAccessProperties(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid, AccessProperties accessprops)
        {
            return Task.Run(() =>
            { 
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using (MySqlCommand cmdName = new MySqlCommand("update `" + table_name + "` set data = @data where uid=" + uid, sqlconn))
                    {
                        cmdName.Parameters.AddWithValue("@data", accessprops == null ? null : Utilities.ConvertObjectToByteArray<AccessProperties>(accessprops));
                        cmdName.ExecuteNonQuery();
                    }
                }
            });
        }

        public static Task<string> GetDescription(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid)
        {
            return Task.Run(new Func<string>(() =>
            {
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using (MySqlCommand cmdName = new MySqlCommand("select description from `" + table_name + "` where uid=" + uid, sqlconn))
                        using (MySqlDataReader reader = cmdName.ExecuteReader())
                        {
                            reader.Read();

                            if (reader.IsDBNull(0))
                                return null;
                            else
                                return (string)reader["description"];
                        }
                }
            }));
        }

        public static Task SetDescription(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid, string description)
        {
            return Task.Run(() =>
            { 
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using (MySqlCommand cmdName = new MySqlCommand("update `" + table_name + "` set description = @description where uid=" + uid, sqlconn))
                    {
                        cmdName.Parameters.AddWithValue("@description", description);
                        cmdName.ExecuteNonQuery();
                    }
                }
            });
        }

        public static Task<string[]> GetNotes(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid)
        {
            return Task.Run(new Func<string[]>(() =>
            {
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using (MySqlCommand cmdName = new MySqlCommand("select notes from `" + table_name + "` where uid=" + uid, sqlconn))
                        using (MySqlDataReader reader = cmdName.ExecuteReader())
                        {
                            reader.Read();

                            if (reader.IsDBNull(0))
                                return null;
                            else
                                return Utilities.ConvertByteArrayToObject<string[]>((byte[])reader["notes"]);
                        }
                }
            }));
        }

        public static Task SetNotes(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid, string[] notes)
        {
            return Task.Run(() =>
            {
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using (MySqlCommand cmdName = new MySqlCommand("update `" + table_name + "` set notes = @notes where uid=" + uid, sqlconn))
                    {
                        cmdName.Parameters.AddWithValue("@notes", notes == null ? null : Utilities.ConvertObjectToByteArray<string[]>(notes));
                        cmdName.ExecuteNonQuery();
                    }
                }
            });
        }

        public static Task AddRow(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid) {return AddRow(dbconnprop, table_name, uid, null); }
        public static Task AddRow(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid, AccessProperties accessprop) {return AddRow(dbconnprop, table_name, uid, null, null, accessprop); }

        public static Task AddRow(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid, string description, string[] notes, AccessProperties accessprop)
        {
            return Task.Run(() =>
            { 
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using (MySqlCommand sqlcmd = new MySqlCommand("insert into `" + table_name + "` (uid, description, notes, data) values (@uid, @description, @notes, @data)", sqlconn))
                    {
                        sqlcmd.Parameters.AddWithValue("@uid", uid);
                        sqlcmd.Parameters.AddWithValue("@description", description);
                        sqlcmd.Parameters.AddWithValue("@notes", notes == null ? null : Utilities.ConvertObjectToByteArray<string[]>(notes));
                        sqlcmd.Parameters.AddWithValue("@data", accessprop == null ? null : Utilities.ConvertObjectToByteArray<AccessProperties>(accessprop));
                        sqlcmd.ExecuteNonQuery();
                    }
                }
            });
        }

        public static Task DeleteRow(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid)
        {
            return Task.Run(() =>
            { 
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using(MySqlCommand sqlcmd = new MySqlCommand("delete from `" + table_name + "` where uid=" + uid, sqlconn))
                        sqlcmd.ExecuteNonQuery();
                }
            });
        }

        public static Task<string[]> GetTableNames(DatabaseConnectionProperties dbconnprop)
        {
            return Task.Run(new Func<string[]>(() =>
            { 
                List<string> table_names = new List<string>();
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using(MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn))
                        using(MySqlDataReader reader = cmdName.ExecuteReader())
                            while (reader.Read())
                                table_names.Add(reader.GetString(0));
                }
                table_names.Remove("access_control_log");
                return table_names.ToArray();
            }));
        }

        public static Task<bool> ContainsUID(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid)
        {
            return Task.Run(() =>
            { 
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    return ContainsUID(sqlconn, table_name, uid);
                }
            });
        }

        public static Task<bool> ContainsUID(MySqlConnection sqlconn, string table_name, UInt64 uid)
        {
            return Task.Run(new Func<bool>(() =>
            { 
                List<UInt64> uids = new List<UInt64>();

                using (MySqlCommand cmdName = new MySqlCommand("select uid from `" + table_name + "`", sqlconn))
                using (MySqlDataReader reader = cmdName.ExecuteReader())
                    while (reader.Read())
                        uids.Add(reader.GetUInt64(0));

                return uids.Contains(uid);
            }));
        }

        public static Task<UInt64[]> GetUIDs(DatabaseConnectionProperties dbconnprop, string table_name)
        {
            return Task.Run(new Func<UInt64[]>(() =>
            {
                List<UInt64> uids = new List<UInt64>();
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using(MySqlCommand cmdName = new MySqlCommand("select uid from `" + table_name + "`", sqlconn))
                        using(MySqlDataReader reader = cmdName.ExecuteReader())
                            while (reader.Read())
                                uids.Add(reader.GetUInt64(0));
                }
                return uids.ToArray();
            }));
        }

        public static Task<string[]> GetDescriptions(DatabaseConnectionProperties dbconnprop, string table_name)
        {
            return Task.Run(new Func<string[]>(() =>
            {
                List<string> descriptions = new List<string>();
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using (MySqlCommand cmdName = new MySqlCommand("select description from `" + table_name + "`", sqlconn))
                    using (MySqlDataReader reader = cmdName.ExecuteReader())
                        while (reader.Read())
                            if (reader.IsDBNull(0))
                                descriptions.Add(null);
                            else
                                descriptions.Add(reader.GetString(0));
                }
                return descriptions.ToArray();
            }));
        }

        public static Task CreateTable(DatabaseConnectionProperties dbconnprop, string table_name)
        {
            return Task.Run(() =>
            { 
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    string cmdstr = "CREATE TABLE `accesscontrol`.`" + table_name + "` (`uid` BIGINT NOT NULL COMMENT '',`description` VARCHAR(255) NULL COMMENT '',`notes` BLOB NULL COMMENT '',`data` BLOB NULL COMMENT '',PRIMARY KEY (`uid`)  COMMENT '');";
                    sqlconn.Open();

                    using(MySqlCommand sqlcmd = new MySqlCommand(cmdstr, sqlconn))
                        sqlcmd.ExecuteNonQuery();
                }
            });
        }

        public static Task RenameTable(DatabaseConnectionProperties dbconnprop, string original_table_name, string new_table_name)
        {
            return Task.Run(() =>
            { 
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    string cmdstr = "RENAME TABLE `accesscontrol`.`" + original_table_name + "` TO `" + new_table_name + "`;";
                    sqlconn.Open();

                    using (MySqlCommand sqlcmd = new MySqlCommand(cmdstr, sqlconn))
                        sqlcmd.ExecuteNonQuery();
                }
            });
        }

        public static Task DeleteTable(DatabaseConnectionProperties dbconnprop, string table_name)
        {
            return Task.Run(() =>
            { 
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();

                    using(MySqlCommand sqlcmd = new MySqlCommand("drop table `" + table_name + "`", sqlconn))
                        sqlcmd.ExecuteNonQuery();

                    sqlconn.Close();
                }
            });
        }
    }
}
