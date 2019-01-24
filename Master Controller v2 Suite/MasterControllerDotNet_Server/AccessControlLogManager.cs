using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    public static class AccessControlLogManager
    {
        private static string table_name = "access_control_log";
        private static DatabaseConnectionProperties dbconnprop = null;

        public static async Task AddAccessControlLogEntry(UInt64 uid, AccessControlLogEntry accessctllogent)
        {
            if (dbconnprop == null)
                throw new Exception();

            //make sure the table exists
            if (!LogTableExists())
                CreateAccessControlLogTable();
            
            var entries = await GetAccessControlLogEntries(uid);
            if (entries == null)
                entries = new List<AccessControlLogEntry>();

            entries.Add(accessctllogent);
            await SetAccessControlLogEntries(uid, entries);

            Console.WriteLine(uid + "   ---------------------------------------------------- " + entries.Count);
        }

        public static Task<Dictionary<UInt64, List<AccessControlLogEntry>>> GetAccessControlLogEntries()
        {
            return Task.Run(new Func<Dictionary<UInt64, List<AccessControlLogEntry>>>(() =>
            {
                if (dbconnprop == null)
                    throw new Exception();

                //make sure the table exists
                if (!LogTableExists())
                    CreateAccessControlLogTable();
                
                ConcurrentDictionary<UInt64, List<AccessControlLogEntry>> results =
                    new ConcurrentDictionary<UInt64, List<AccessControlLogEntry>>();

                Parallel.ForEach(GetUIDs().Result, uid =>
                {
                    List<AccessControlLogEntry> entries = GetAccessControlLogEntries(uid).Result;
                    results.TryAdd(uid, entries);
                });

                return results.ToDictionary(x => x.Key, x => x.Value);
            }));
        }

        public static Task<List<AccessControlLogEntry>> GetAccessControlLogEntries(UInt64 uid)
        {
            return Task.Run(new Func<Task<List<AccessControlLogEntry>>>(async () =>
            { 
                if (dbconnprop == null)
                    throw new Exception();

                //make sure the table exists
                if (!LogTableExists())
                    CreateAccessControlLogTable();

                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();

                    //if the table exists then check to see if the uid has been added before
                    if (!await DatabaseManager.ContainsUID(sqlconn, table_name, uid))
                        return null;

                    //if we've reached this point then the table exists and it contains a row with the uid
                    using (MySqlCommand cmdName = new MySqlCommand("select entries from `" + table_name + "` where uid=" + uid, sqlconn))
                        using (MySqlDataReader reader = cmdName.ExecuteReader())
                        {
                            reader.Read();
                            return Utilities.ConvertByteArrayToObject<List<AccessControlLogEntry>>((byte[])reader["entries"]);
                        }
                }
            }));
        }

        public static Task SetAccessControlLogEntries(UInt64 uid, List<AccessControlLogEntry> accessctllogents)
        {
            return Task.Run(async () =>
            {
                if (dbconnprop == null)
                    throw new Exception();

                //make sure the table exists
                if (!LogTableExists())
                    CreateAccessControlLogTable();

                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();

                    //if the table exists then check to see if the uid has been added before
                    if (!await DatabaseManager.ContainsUID(sqlconn, table_name, uid))
                    {
                        AddRow(sqlconn, uid, accessctllogents);
                        return;
                    }

                    //if we've reached this point then the table exists and the uid has been found in the table. replace the current list with the new list
                    using (MySqlCommand cmdName = new MySqlCommand("update `" + table_name + "` set entries = @entries where uid=" + uid, sqlconn))
                    {
                        cmdName.Parameters.AddWithValue("@entries", Utilities.ConvertObjectToByteArray<List<AccessControlLogEntry>>(accessctllogents));
                        cmdName.ExecuteNonQuery();
                    }
                }
            });
        }

        private static void AddRow(MySqlConnection sqlconn, UInt64 uid, List<AccessControlLogEntry> accessctllogents)
        {
            using (MySqlCommand sqlcmd = new MySqlCommand("insert into `" + table_name + "` (uid, entries) values (@uid, @entries)", sqlconn))
            {
                sqlcmd.Parameters.AddWithValue("@uid", uid);
                sqlcmd.Parameters.AddWithValue("@entries", Utilities.ConvertObjectToByteArray<List<AccessControlLogEntry>>(accessctllogents));
                sqlcmd.ExecuteNonQuery();
            }
        }

        private static void CreateAccessControlLogTable()
        {
            using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
            {
                sqlconn.Open();

                string cmdstr = "CREATE TABLE `accesscontrol`.`" + table_name + "` (`uid` BIGINT NOT NULL COMMENT '',`entries` LONGBLOB NULL COMMENT '',PRIMARY KEY (`uid`)  COMMENT '');";
                using (MySqlCommand sqlcmd = new MySqlCommand(cmdstr, sqlconn))
                    sqlcmd.ExecuteNonQuery();
            }
        }

        private static bool LogTableExists()
        {
            List<string> table_names = new List<string>();

            using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
            {
                sqlconn.Open();

                using (MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn))
                using (MySqlDataReader reader = cmdName.ExecuteReader())
                    while (reader.Read())
                        table_names.Add(reader.GetString(0));
            }

            return table_names.Contains(table_name);
        }

        public static Task<UInt64[]> GetUIDs()
        {
            return Task.Run(new Func<UInt64[]>(() =>
            {
                List<UInt64> uids = new List<UInt64>();
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using (MySqlCommand cmdName = new MySqlCommand("select uid from `" + table_name + "`", sqlconn))
                        using (MySqlDataReader reader = cmdName.ExecuteReader())
                            while (reader.Read())
                                uids.Add(reader.GetUInt64(0));
                }
                return uids.ToArray();
            }));
        }

        public static DatabaseConnectionProperties DatabaseConnectionProperties
        {
            set
            {
                dbconnprop = value;
            }
        }
    }
}
