using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Threading;
using Newtonsoft.Json;

namespace MCICommon
{
    public static class DatabaseUtilities
    {

        public static Task DBBringup()
        {
            return null;
        }

        /*
        public static Task<AccessProperties> GetAccessProperties(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid)
        {
            return Task.Run(new Func<AccessProperties>(() =>
            {
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using (MySqlCommand cmdName = new MySqlCommand("select data from `" + table_name + "` where uid=" + uid, sqlconn))
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
        */
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
/*
        public static Task AddRow(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid) { return AddRow(dbconnprop, table_name, uid, null); }
        public static Task AddRow(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid, AccessProperties accessprop) { return AddRow(dbconnprop, table_name, uid, null, null, accessprop); }

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
*/
        public static Task DeleteRow(DatabaseConnectionProperties dbconnprop, string table_name, UInt64 uid)
        {
            return Task.Run(() =>
            {
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    using (MySqlCommand sqlcmd = new MySqlCommand("delete from `" + table_name + "` where uid=" + uid, sqlconn))
                        sqlcmd.ExecuteNonQuery();
                }
            });
        }

        public static Task DeleteDevicesRow(MySqlConnection sqlconn, UInt64 device_id)
        {
            return Task.Run(() =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("delete from `devices` where id=" + device_id, sqlconn))
                    sqlcmd.ExecuteNonQuery();
            });
        }

        public static async Task<List<string>> GetTableNames(MySqlConnection sqlconn)
        {
            return await Task.Run(new Func<Task<List<string>>>(async () =>
            {
                List<string> table_names = new List<string>();

                    using (MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn))
                    using (MySqlDataReader reader = cmdName.ExecuteReader())
                        while (await reader.ReadAsync())
                            table_names.Add(reader.GetString(0));

                return table_names;
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
                    using (MySqlCommand cmdName = new MySqlCommand("select uid from `" + table_name + "`", sqlconn))
                    using (MySqlDataReader reader = cmdName.ExecuteReader())
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

        public static Task CreateUserTable(DatabaseConnectionProperties dbconnprop, string table_name)
        {
            return Task.Run(() =>
            {
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    string cmdstr = "CREATE TABLE `accesscontrol`.`users` (`user_id` BIGINT UNSIGNED NOT NULL,`name` VARCHAR(255) NOT NULL,`description` VARCHAR(255) NULL,PRIMARY KEY (`user_id`),UNIQUE INDEX `user_id_UNIQUE` (`user_id` ASC));";

                    sqlconn.Open();

                    using (MySqlCommand sqlcmd = new MySqlCommand(cmdstr, sqlconn))
                        sqlcmd.ExecuteNonQuery();
                }
            });
        }

        public static Task CreateUserExtensionsTable()
        {
            return Task.Run(async () =>
            {
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                try
                {
                    string cmdstr = "CREATE TABLE `user_extensions` (`user_id` BIGINT UNSIGNED NOT NULL,`data` TEXT NOT NULL,PRIMARY KEY (`user_id`),UNIQUE INDEX `user_id_UNIQUE` (`user_id` ASC));";

                    using (MySqlCommand sqlcmd = new MySqlCommand(cmdstr, sqlconn.Connection))
                        sqlcmd.ExecuteNonQuery();
                }
                catch(Exception ex)
                {

                }


                ARDBConnectionManager.default_manager.CheckIn(sqlconn);
            });
        }

        public static Task<MCIUserExt> GetUserExtensions(ulong user_id)
        {
            return Task.Run(async () =>
            {
                MCIUserExt res = null;

                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                try
                {
                    using (MySqlCommand cmd = new MySqlCommand("select data from `user_extensions` where user_id=" + user_id + ";", sqlconn.Connection))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                            res = JsonConvert.DeserializeObject<MCIUserExt>((string)reader["data"]);
                    }
                }
                catch (Exception ex)
                {

                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                return res;
            });
        }

        public static Task SetUserExtensions(ulong user_id, MCIUserExt uext)
        {
            return Task.Run(async () =>
            {
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                try
                {
                    bool was_found = false;

                    using (MySqlCommand sqlcmd = new MySqlCommand("select user_id from user_extensions where user_id=@user_id", sqlconn.Connection))
                    {
                        sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                        using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                            while (await reader.ReadAsync())
                                was_found = true;
                    }

                    if(was_found)
                    {
                        using (MySqlCommand cmdName = new MySqlCommand("update `user_extensions` set data = @data where user_id=" + user_id, sqlconn.Connection))
                        {
                            cmdName.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(uext));
                            await cmdName.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        using (MySqlCommand sqlcmd = new MySqlCommand("insert into `user_extensions` (user_id, data) values (@user_id, @data)", sqlconn.Connection))
                        {
                            sqlcmd.Parameters.AddWithValue("@user_id", user_id);
                            sqlcmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(uext));

                            await sqlcmd.ExecuteNonQueryAsync();
                        }
                    }
                    
                }
                catch (Exception ex)
                {

                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                
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

                    using (MySqlCommand sqlcmd = new MySqlCommand("drop table `" + table_name + "`", sqlconn))
                        sqlcmd.ExecuteNonQuery();
                }
            });
        }

        public static async Task<UInt64> GenerateUniqueUserID(DatabaseConnectionProperties dbconnprop)
        {
            using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
            {
                await sqlconn.OpenAsync();

                return await GenerateUniqueUserID(sqlconn);
            }
        }

        public static async Task<UInt64> GenerateUniqueUserID(MySqlConnection sqlconn)
        {
            return await Task.Run(new Func<Task<UInt64>>(async () =>
            {
                List<UInt64> uids = new List<UInt64>();

                using (MySqlCommand cmdName = new MySqlCommand("select user_id from `users`", sqlconn))
                using (MySqlDataReader reader = cmdName.ExecuteReader())
                    while (await reader.ReadAsync())
                        uids.Add(reader.GetUInt64(0));

                Random r = new Random();
                byte[] uid_bytes = new byte[8];

                r.NextBytes(uid_bytes);

                while (uids.Contains(BitConverter.ToUInt64(uid_bytes, 0)))
                    r.NextBytes(uid_bytes);

                return BitConverter.ToUInt64(uid_bytes, 0);
            }));
        }

        public static async Task<UInt64> GenerateUniqueListUID(DatabaseConnectionProperties dbconnprop)
        {
            using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
            {
                sqlconn.Open();

                return await GenerateUniqueListUID(sqlconn);
            }
        }

        public static Task<UInt64> GenerateUniqueListUID(MySqlConnection sqlconn)
        {
            return Task.Run(new Func<UInt64>(() =>
            {
                List<UInt64> uids = new List<UInt64>();

                using (MySqlCommand cmdName = new MySqlCommand("select uid from `lists`", sqlconn))
                using (MySqlDataReader reader = cmdName.ExecuteReader())
                    while (reader.Read())
                        uids.Add(reader.GetUInt64(0));

                Random r = new Random();
                byte[] uid_bytes = new byte[8];

                r.NextBytes(uid_bytes);

                while (uids.Contains(BitConverter.ToUInt64(uid_bytes, 0)))
                    r.NextBytes(uid_bytes);

                return BitConverter.ToUInt64(uid_bytes, 0);
            }));
        }

        public static Task<UInt64> GenerateUniqueDeviceID(MySqlConnection sqlconn)
        {
            return Task.Run(new Func<UInt64>(() =>
            {
                List<UInt64> uids = new List<UInt64>();

                using (MySqlCommand cmdName = new MySqlCommand("select id from `devices`", sqlconn))
                using (MySqlDataReader reader = cmdName.ExecuteReader())
                    while (reader.Read())
                        uids.Add(reader.GetUInt64(0));

                Random r = new Random();
                byte[] uid_bytes = new byte[8];

                r.NextBytes(uid_bytes);

                while (uids.Contains(BitConverter.ToUInt64(uid_bytes, 0)))
                    r.NextBytes(uid_bytes);

                return BitConverter.ToUInt64(uid_bytes, 0);
            }));
        }

        public static async Task<List<UInt64>> GetAllUserIDS(MySqlConnection sqlconn)
        {
            return await Task.Run(new Func<Task<List<UInt64>>>(async () =>
            {
                List<UInt64> user_ids = new List<UInt64>();

                string cmd = "SELECT user_id FROM `users;";

                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                {
                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                        if (await reader.ReadAsync())
                            user_ids.Add(reader.GetUInt64(0));
                }

                return user_ids;
            }));
        }

        public static Task<List<ulong>> GetListUIDs(byte type = 0)
        {
            if (type != 0)
                throw new Exception("Only Type 0 is Implemented");

            return Task.Run(async () =>
            {
                List<ulong> list_uids = new List<ulong>();

                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                try
                {
                    string cmd = "SELECT * FROM `lists` WHERE  type=@type;";

                    using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn.Connection))
                    {
                        sqlcmd.Parameters.AddWithValue("@type", type);

                        using (MySqlDataReader reader = (MySqlDataReader)(await sqlcmd.ExecuteReaderAsync()))
                            while (await reader.ReadAsync())
                            {
                                string rstr = reader.GetString("alias").Trim();

                                var ruid = reader.GetUInt64("uid");

                                list_uids.Add(ruid);
                            }
                    }
                }
                catch(Exception ex)
                {

                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                return list_uids;
            });
        }

        public static async Task<Dictionary<UInt64, string>> GetDictionaryUniqueShortListDescriptionWithLimiter(MySqlConnection sqlconn, string limiter, byte type)
        {
            if (type != 0)
                throw new Exception("Only Type 0 is Implemented");

            return await Task.Run(new Func<Task<Dictionary<UInt64, string>>>(async () =>
            {
                Dictionary<UInt64, string> list_descriptions = new Dictionary<UInt64, string>();

                // Check if lists table exists
                var tables = await GetTableNames(sqlconn);
                if (!tables.Contains("lists"))
                {
                    return list_descriptions; // Return empty dictionary if table doesn't exist
                }

                string cmd = "";

                if (limiter != "")
                    cmd = "SELECT * FROM `lists` WHERE (uid like @limiter or alias like @limiter) and type=@type;";
                else
                    cmd = "SELECT * FROM `lists` WHERE type=@type;";

                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                {
                    if (limiter != "")
                        sqlcmd.Parameters.AddWithValue("@limiter", "%" + limiter + "%");

                    sqlcmd.Parameters.AddWithValue("@type", type);

                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                        while (await reader.ReadAsync())
                        {
                            string dstr = "";
                            string rstr = reader.GetString("alias").Trim();

                            if(rstr != null && rstr != "")
                                if(!list_descriptions.Values.Contains(rstr))
                                    dstr = rstr;

                            var ruid = reader.GetUInt64("uid");

                            if (dstr == "")
                                dstr = ruid.ToString();

                            list_descriptions.Add(ruid, dstr);
                        }   
                }

                return list_descriptions;
            }));
        }

        public static async Task<Dictionary<UInt64, string>> GetDictionaryUniqueShortDeviceDescriptionWithLimiter(MySqlConnection sqlconn, byte type_limiter)
        {
            return await Task.Run(new Func<Task<Dictionary<UInt64, string>>>(async () =>
            {
                Dictionary<UInt64, string> device_strings = new Dictionary<UInt64, string>();

                string cmd = "SELECT * FROM `devices` WHERE type=@type;";

                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@type", type_limiter);

                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                        while (await reader.ReadAsync())
                        {
                            Object[] entry = new Object[] { reader.GetValue(0), reader.GetValue(1), reader.GetValue(2), reader.GetValue(3), reader.GetValue(4) };

                            string entry_string = "";

                            if (((string)entry[4]).Trim() != "")
                                entry_string = (string)entry[4];
                            else
                                entry_string = (string)entry[2] + ":" + ((UInt32)entry[3]).ToString();

                            if (device_strings.Values.Contains(entry_string))
                                device_strings.Add((UInt64)entry[0], ((UInt64)entry[0]).ToString());
                            else
                                device_strings.Add((UInt64)entry[0], entry_string);
                        }
                }

                return device_strings;
            }));
        }

        public static async Task<List<ulong>> GetAllUsersInList(ulong list_uid)
        {
            List<ulong> usersinlist = new List<ulong>();

            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

            try
            {
                usersinlist.AddRange(await GetAllUsersInList(sqlconn.Connection, list_uid));
            }
            catch(Exception ex)
            {

            }

            ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            return usersinlist;
        }

        public static Task<List<ulong>> GetAllUsersInList(MySqlConnection sqlconn, ulong list_uid)
        {
            return Task.Run(async () =>
            {
                List<string> table_names = await GetTableNames(sqlconn);

                if (!table_names.Contains(list_uid.ToString()))
                    throw new Exception("The List Does Not Exist");

                List<UInt64> list_user_ids = new List<UInt64>();

                string cmd = "SELECT user_id FROM `"+ list_uid.ToString() +"`;";

                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                {
                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                        while (await reader.ReadAsync())
                            list_user_ids.Add(reader.GetUInt64(0));
                }

                return list_user_ids;
            });
        }

        public static async Task<List<Object>> GetV2ListEntry(UInt64 list_id, UInt64 user_id)
        {
            return await Task.Run(new Func<Task<List<Object>>>(async () =>
            {
                List<Object> list_entry = new List<Object>();

                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                try
                {
                    List<string> table_names = await GetTableNames(sqlconn.Connection);

                    if (!table_names.Contains(list_id.ToString()))
                        throw new Exception("The List Does Not Exist");

                    

                    string cmd = "SELECT * FROM `" + list_id.ToString() + "` WHERE user_id=@user_id;";

                    using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn.Connection))
                    {
                        sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                        using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                            if (await reader.ReadAsync())
                            {
                                list_entry.Add(await reader.GetFieldValueAsync<string>(1));
                                list_entry.Add(await reader.GetFieldValueAsync<string>(2));
                                list_entry.Add(await reader.GetFieldValueAsync<byte>(3));
                            }
                    }
                }
                catch(Exception ex) { list_entry = null; }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                return list_entry;
            }));
        }

        public static async Task<List<List<Object>>> GetDevicesFromDatabase(MySqlConnection sqlconn)
        {
            return await Task.Run(new Func<Task<List<List<Object>>>>(async () =>
            {
                await CheckIfDevicesTableExistsAndCreateIfNeeded(sqlconn);

                List<List<Object>> all_entries = new List<List<Object>>();

                string cmd = "SELECT * FROM `devices`;";

                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                {
                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                        while (await reader.ReadAsync())
                        {
                            List<Object> list_entry = new List<Object>();

                            list_entry.Add(await reader.GetFieldValueAsync<UInt64>(0)); //id
                            list_entry.Add(await reader.GetFieldValueAsync<byte>(1)); //type
                            list_entry.Add(await reader.GetFieldValueAsync<string>(2)); //address
                            list_entry.Add(await reader.GetFieldValueAsync<UInt32>(3)); //port
                            list_entry.Add(await reader.GetFieldValueAsync<string>(4)); //alias

                            all_entries.Add(list_entry);
                        }
                }

                return all_entries;
            }));
        }

        public static async Task<TCPConnectionProperties> GetDevicePropertiesFromDatabase(MySqlConnection sqlconn, UInt64 device_id)
        {
            return await Task.Run(new Func<Task<TCPConnectionProperties>>(async () =>
            {
                await CheckIfDevicesTableExistsAndCreateIfNeeded(sqlconn);

                List<List<Object>> all_entries = new List<List<Object>>();

                string cmd = "SELECT * FROM `devices` WHERE id=@id;";

                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@id", device_id);

                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                        if (await reader.ReadAsync())
                        {
                            return new TCPConnectionProperties(await reader.GetFieldValueAsync<string>(4),
                            await reader.GetFieldValueAsync<string>(2),
                            (int)await reader.GetFieldValueAsync<UInt32>(3));
                        }
                }

                throw new Exception("Device ID Was Not Found");
            }));
        }

        public static async Task<Dictionary<UInt64, string>> GetDictionaryDescriptionForAllUsersInListWithLimiter(MySqlConnection sqlconn, UInt64 list_uid, string limiter)
        {
            return await Task.Run(new Func<Task<Dictionary<UInt64, string>>>(async () =>
            {
                List<UInt64> users_in_list = null;
                try
                {
                    users_in_list = await GetAllUsersInList(sqlconn, list_uid);
                }
                catch(Exception ex)
                {
                    throw new Exception("Database Error. There is no access control list with that UID in the database.");
                }

                Dictionary<UInt64, string> user_descriptions = new Dictionary<UInt64, string>();

                string cmd = "";

                if (limiter != "")
                    cmd = "SELECT * FROM `users` WHERE user_id LIKE '%" + limiter + "%' or name LIKE '%" + limiter + "%' or description LIKE '%" + limiter + "%';";
                else
                    cmd = "SELECT * FROM `users`;";

                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                {
                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                        while (await reader.ReadAsync())
                        {
                            var ruid = reader.GetUInt64("user_id");

                            if (!users_in_list.Contains(ruid))
                                continue;

                            string dstr = "";
                            string rstr_name = reader.GetString("name").Trim();
                            string rstr_description = reader.GetString("description").Trim();

                            //string formats
                            //Name: x Description: y
                            //(uid)x
                            //Name: x
                            //(uid)x Description: y

                            string resstr = "";

                            if (rstr_name != null && rstr_name != "")
                            {
                                if (rstr_description != null && rstr_description != "")
                                    resstr = rstr_name + "; " + rstr_description;
                                else
                                    resstr = rstr_name;
                            }
                            else
                            {
                                if (rstr_description != null && rstr_description != "")
                                    resstr = ruid.ToString() + "; " + rstr_description;
                                else
                                    resstr = ruid.ToString();
                            }

                            if (user_descriptions.Values.Contains(resstr))
                                dstr = ruid.ToString();
                            else
                                dstr = resstr;

                            user_descriptions.Add(ruid, dstr);
                        }
                }

                return user_descriptions;
            }));
        }

        public static async Task SetV2ListRow(MySqlConnection sqlconn, UInt64 list_uid, UInt64 user_id, Object[] row_contents)
        {
            await Task.Run(new Func<Task>(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("update `" + list_uid.ToString() + "` set days=@days, times=@times, enabled=@enabled where user_id=@user_id;", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@days", (string)row_contents[0]);
                    sqlcmd.Parameters.AddWithValue("@times", (string)row_contents[1]);
                    sqlcmd.Parameters.AddWithValue("@enabled", (byte)row_contents[2]);
                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                    await sqlcmd.ExecuteNonQueryAsync();
                }
            }));
        }

        public static async Task SetDevicesRow(MySqlConnection sqlconn, Object[] row_contents)
        {
            await Task.Run(new Func<Task>(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("update `devices` set type=@type, address=@address, port=@port, alias=@alias where id=@device_id;", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@type", row_contents[1]);
                    sqlcmd.Parameters.AddWithValue("@address", row_contents[2]);
                    sqlcmd.Parameters.AddWithValue("@port", row_contents[3]);
                    sqlcmd.Parameters.AddWithValue("@alias", row_contents[4]);
                    sqlcmd.Parameters.AddWithValue("@device_id", row_contents[0]);

                    await sqlcmd.ExecuteNonQueryAsync();
                }
            }));
        }

        public static async Task<Dictionary<UInt64, string>> GetDictionaryDescriptionsAllUsersNotPresentInListWithLimiter(MySqlConnection sqlconn, UInt64 list_id, string limiter)
        {
            return await Task.Run(new Func<Task<Dictionary<UInt64, string>>>(async () =>
            {
                List<ulong> users_in_list = null;
                try
                {
                    users_in_list = await GetAllUsersInList(sqlconn, list_id);
                }
                catch(Exception ex)
                {
                    throw new Exception("Database Error. There is no access control list with that UID in the database.");
                }

                Dictionary<UInt64, string> user_descriptions = new Dictionary<UInt64, string>();

                string user_filter = "";

                string limiter_str = "user_id LIKE '%" + limiter + "%' OR name LIKE '%" + limiter + "%' OR description LIKE '%" + limiter + "%'";

                if (users_in_list.Count() > 0)
                {
                    for (int i = 0; i < users_in_list.Count(); i++)
                        user_filter += "user_id!=@user_id_" + i + " AND ";

                    user_filter = user_filter.Substring(0, user_filter.Length - 5);
                }

                string cmd = "";

                if (limiter != "")
                {
                    if (users_in_list.Count() > 0)
                        cmd = "SELECT * FROM `users` WHERE ((" + limiter_str + ") AND (" + user_filter + "))";
                    else
                        cmd = "SELECT * FROM `users` WHERE ((" + limiter_str + "))";
                }
                else
                {
                    if (users_in_list.Count() > 0)
                        cmd = "SELECT * FROM `users` WHERE (" + user_filter + ")";
                    else
                        cmd = "SELECT * FROM `users`";
                }

                cmd += ";";

                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                {
                    for (int i = 0; i < users_in_list.Count(); i++)
                        sqlcmd.Parameters.AddWithValue("@user_id_" + i, users_in_list[i]);

                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                        while (await reader.ReadAsync())
                        {
                            var ruid = reader.GetUInt64("user_id");

                            string dstr = "";
                            string rstr_name = reader.GetString("name").Trim();
                            string rstr_description = reader.GetString("description").Trim();

                            //string formats
                            //Name: x Description: y
                            //(uid)x
                            //Name: x
                            //(uid)x Description: y

                            string resstr = "";

                            if (rstr_name != null && rstr_name != "")
                            {
                                if (rstr_description != null && rstr_description != "")
                                    resstr = rstr_name + "; " + rstr_description;
                                else
                                    resstr = rstr_name;
                            }
                            else
                            {
                                if (rstr_description != null && rstr_description != "")
                                    resstr = ruid.ToString() + "; " + rstr_description;
                                else
                                    resstr = ruid.ToString();
                            }

                            if (user_descriptions.Values.Contains(resstr))
                                dstr = ruid.ToString();
                            else
                                dstr = resstr;

                            user_descriptions.Add(ruid, dstr);
                        }
                }

                return user_descriptions;
            }));
        }

        public static async Task CheckIfDevicesTableExistsAndCreateIfNeeded(MySqlConnection sqlconn)
        {
            await Task.Run(async () =>
            {
                List<string> table_names = new List<string>();

                using (MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn))
                using (MySqlDataReader reader = cmdName.ExecuteReader())
                    while (reader.Read())
                        table_names.Add(reader.GetString(0));

                //check to see if the cards table exists

                if (!table_names.Contains("devices"))
                {
                    string cmd = "CREATE TABLE `accesscontrol`.`devices` (`id` BIGINT UNSIGNED NOT NULL,`type` TINYINT UNSIGNED NOT NULL,`address` VARCHAR(255) NOT NULL,`port` INT UNSIGNED NOT NULL,`alias` VARCHAR(255) NULL,PRIMARY KEY (`id`),UNIQUE INDEX `id_UNIQUE` (`id` ASC));";

                    using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                        await sqlcmd.ExecuteNonQueryAsync();
                }
            });
        }

        public static async Task CheckIfUsersTableExistsAndCreateIfNeeded(MySqlConnection sqlconn)
        {
            await Task.Run(async () =>
            {
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
            });
        }

        public static Task AddDevicesRow(MySqlConnection sqlconn, Object[] row_contents)
        {
            return Task.Run(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("insert into `devices` (id, type, address, port, alias) values (@id, @type, @address, @port, @alias)", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@id", row_contents[0]);
                    sqlcmd.Parameters.AddWithValue("@type", row_contents[1]);
                    sqlcmd.Parameters.AddWithValue("@address", row_contents[2]);
                    sqlcmd.Parameters.AddWithValue("@port", row_contents[3]);
                    sqlcmd.Parameters.AddWithValue("@alias", row_contents[4]);

                    await sqlcmd.ExecuteNonQueryAsync();
                }
            });
        }

        public static async Task CheckIfCardsTableExistsAndCreateIfNeeded(MySqlConnection sqlconn)
        {
            await Task.Run(async () =>
            {
                List<string> table_names = new List<string>();

                using (MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn))
                using (MySqlDataReader reader = cmdName.ExecuteReader())
                    while (reader.Read())
                        table_names.Add(reader.GetString(0));

                //check to see if the cards table exists

                if (!table_names.Contains("cards"))
                {
                    string cmd = "CREATE TABLE `accesscontrol`.`cards` (`uid` BIGINT UNSIGNED NOT NULL,`user_id` BIGINT UNSIGNED NULL,PRIMARY KEY (`uid`),UNIQUE INDEX `uid_UNIQUE` (`uid` ASC));";

                    using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn))
                        await sqlcmd.ExecuteNonQueryAsync();
                }
            });
        }

        public static async Task AddNewUserToDatabase(MySqlConnection sqlconn, UInt64 user_id, string name, string desc)
        {
            if (user_id == 0)
                throw new Exception("The Supplied User ID Is Invalid");

            await Task.Run(async () =>
            { 
                using (MySqlCommand sqlcmd = new MySqlCommand("insert into `users` (user_id, name, description) values (@user_id, @name, @description)", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);
                    sqlcmd.Parameters.AddWithValue("@name", name);
                    sqlcmd.Parameters.AddWithValue("@description", desc);

                    await sqlcmd.ExecuteNonQueryAsync();
                }
            });
        }

        public static async Task EditUserInDatabase(MySqlConnection sqlconn, UInt64 user_id, string name, string desc)
        {
            if (user_id == 0)
                throw new Exception("The Supplied User ID Is Invalid");

            await Task.Run(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("update `users` set name=@name, description=@description where user_id=@user_id;", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);
                    sqlcmd.Parameters.AddWithValue("@name", name);
                    sqlcmd.Parameters.AddWithValue("@description", desc);

                    await sqlcmd.ExecuteNonQueryAsync();
                }
            });
        }

        public static async Task DropUser(MySqlConnection sqlconn, UInt64 user_id)
        {
            if (user_id == 0)
                throw new Exception("The Supplied User ID Is Invalid");

            await Task.Run(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("delete from `users` where user_id=@user_id;", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                    await sqlcmd.ExecuteNonQueryAsync();
                }
            });
        }

        public static async Task PurgeUserFromCardsTable(MySqlConnection sqlconn, UInt64 user_id)
        {
            if (user_id == 0)
                throw new Exception("The Supplied User ID Is Invalid");

            await Task.Run(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("update `cards` set user_id=0 where user_id=@user_id;", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                    await sqlcmd.ExecuteNonQueryAsync();
                }
            });
        }

        public static async Task AddCardToDatabase(MySqlConnection sqlconn, UInt64 card_uid)
        {
            if (card_uid == 0)
                throw new Exception("The Supplied Card NUID Is Invalid");

            await Task.Run(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("insert into `cards` (uid, user_id) values (@uid, @user_id)", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@uid", card_uid);
                    sqlcmd.Parameters.AddWithValue("@user_id", 0);

                    await sqlcmd.ExecuteNonQueryAsync();
                }
            });
        }

        public static async Task PurgeUserFromV2Lists(MySqlConnection sqlconn, UInt64 user_id)
        {
            if (user_id == 0)
                throw new Exception("The Supplied User ID Is Invalid");

            await Task.Run(async () =>
            {
                List<UInt64> v2_list_uids = new List<UInt64>();

                using (MySqlCommand sqlcmd = new MySqlCommand("select uid from `lists` where type=0", sqlconn))
                using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                    while (await reader.ReadAsync())
                        v2_list_uids.Add(reader.GetUInt64(0));

                foreach (var list_uid in v2_list_uids)
                {
                    List<UInt64> users_in_list = await GetAllUsersInList(sqlconn, list_uid);
                    if(users_in_list.Contains(user_id))
                        using (MySqlCommand sqlcmd = new MySqlCommand("delete from `"+ list_uid.ToString() +"` where user_id=@user_id;", sqlconn))
                        {
                            sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                            await sqlcmd.ExecuteNonQueryAsync();
                        }
                }
            });
        }

        public static async Task RemoveUserFromV2List(MySqlConnection sqlconn, UInt64 list_id, UInt64 user_id)
        {
            if (list_id == 0)
                throw new Exception("The Supplied List ID Is Invalid");

            if (user_id == 0)
                throw new Exception("The Supplied User ID Is Invalid");

            await Task.Run(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("delete from `" + list_id.ToString() + "` where user_id=@user_id;", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                    await sqlcmd.ExecuteNonQueryAsync();
                }
            });
        }

        public static async Task AddUserToV2List(MySqlConnection sqlconn, UInt64 list_id, UInt64 user_id)
        {
            if (list_id == 0)
                throw new Exception("The Supplied List ID Is Invalid");

            if (user_id == 0)
                throw new Exception("The Supplied User ID Is Invalid");

            await Task.Run(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("insert into `" + list_id + "` (user_id, days, times, enabled) values (@user_id, @days, @times, @enabled)", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);
                    sqlcmd.Parameters.AddWithValue("@days", "Sunday - Saturday");
                    sqlcmd.Parameters.AddWithValue("@times", "00:00 - 23:59");
                    sqlcmd.Parameters.AddWithValue("@enabled", 1);

                    await sqlcmd.ExecuteNonQueryAsync();
                }
            });
        }

        public static async Task CreateV2ACList(MySqlConnection sqlconn, UInt64 list_uid, string list_alias)
        {
            if (list_uid == 0)
                throw new Exception("The Supplied List ID Is Invalid");

            await Task.Run(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("insert into `lists` (uid, alias, type) values (@uid, @alias, @type)", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@uid", list_uid);
                    sqlcmd.Parameters.AddWithValue("@alias", list_alias);
                    sqlcmd.Parameters.AddWithValue("@type", 0);

                    await sqlcmd.ExecuteNonQueryAsync();
                }

                string sqlcmdstr = "CREATE TABLE `" + list_uid + "` (`user_id` BIGINT UNSIGNED NOT NULL,`days` VARCHAR(255) NOT NULL,`times` VARCHAR(255) NOT NULL,`enabled` TINYINT UNSIGNED NOT NULL,PRIMARY KEY(`user_id`),UNIQUE INDEX `user_id_UNIQUE` (`user_id` ASC));";

                using (MySqlCommand sqlcmd = new MySqlCommand(sqlcmdstr, sqlconn))
                    await sqlcmd.ExecuteNonQueryAsync();
            });
        }

        public static async Task SetV2ACListAlias(MySqlConnection sqlconn, UInt64 list_uid, string list_alias)
        {
            if (list_uid == 0)
                throw new Exception("The Supplied List ID Is Invalid");

            await Task.Run(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("UPDATE `lists` SET alias=@alias WHERE uid=@uid;", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@alias", list_alias);
                    sqlcmd.Parameters.AddWithValue("@uid", list_uid);

                    await sqlcmd.ExecuteNonQueryAsync();
                }
            });
        }

        public static async Task DeleteV2ACList(MySqlConnection sqlconn, UInt64 list_uid)
        {
            if (list_uid == 0)
                throw new Exception("The Supplied List ID Is Invalid");

            await Task.Run(async () =>
            {
                //drop table
                using (MySqlCommand sqlcmd = new MySqlCommand("drop table `" + list_uid.ToString() + "`", sqlconn))
                    await sqlcmd.ExecuteNonQueryAsync();

                //remove reference from lists table
                using (MySqlCommand sqlcmd = new MySqlCommand("delete from `lists` where uid=@uid;", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@uid", list_uid);

                    await sqlcmd.ExecuteNonQueryAsync();
                }

            });
        }

        public static async Task<List<DBCard>> GetAllCards(CancellationTokenSource lcts = null)
        {
            if (lcts == null)
                lcts = new CancellationTokenSource();

            List<DBCard> cards = new List<DBCard>();
            AutoRefreshDBConnection sqlconn = null;

            try
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                        using (MySqlCommand sqlcmd = new MySqlCommand("select * from cards", sqlconn.Connection))
                        {
                            using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                                while (await reader.ReadAsync())
                                    cards.Add(new DBCard(reader.GetUInt64(0), reader.GetUInt64(1)));
                        }
                    }
                    catch (Exception ex) { }

                }, lcts.Token);
            }
            catch(Exception ex) { }

            if (sqlconn != null)
                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            return cards;
        }

        public static async Task<ulong[]> GetAllCardsAssociatedWithUser(MySqlConnection sqlconn, UInt64 user_id)
        {
            if (user_id == 0)
                throw new Exception("The Supplied User ID Is Invalid");

            List<ulong> card_ids = new List<ulong>();

            await Task.Run(async () =>
            {
                using (MySqlCommand sqlcmd = new MySqlCommand("select uid from cards where user_id=@user_id", sqlconn))
                {
                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                        while (await reader.ReadAsync())
                            card_ids.Add(reader.GetUInt64(0));
                } 
            });

            return card_ids.ToArray();
        }

        public static async Task<ulong> MergeUsers(MySqlConnection sqlconn, List<UInt64> users_to_merge, string new_name, string new_desc)
        {
            if (users_to_merge.Contains(0))
                throw new Exception("The Supplied User ID Is Invalid");

            ulong new_user_id = 0;

            await Task.Run(async () =>
            {
                //locate which lists the users are present in
                List<UInt64> v2_list_uids = new List<UInt64>();
                List<UInt64> add_result_to = new List<UInt64>();

                using (MySqlCommand sqlcmd = new MySqlCommand("select uid from `lists` where type=0", sqlconn))
                using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                    while (await reader.ReadAsync())
                        v2_list_uids.Add(reader.GetUInt64(0));

                foreach (var list_uid in v2_list_uids)
                {
                    List<UInt64> users_in_list = await GetAllUsersInList(sqlconn, list_uid);
                    foreach (var user_id in users_to_merge)
                    {
                        if (users_in_list.Contains(user_id))
                            if (add_result_to.Contains(list_uid))
                                break;
                            else
                                add_result_to.Add(list_uid);
                    }           
                }

                List<ulong> card_ids = new List<ulong>();
                
                foreach (var user_id in users_to_merge)
                {
                    //get card associations
                    card_ids.AddRange(await GetAllCardsAssociatedWithUser(sqlconn, user_id));

                    //purge user
                    await PurgeUserFromV2Lists(sqlconn, user_id);
                    await PurgeUserFromCardsTable(sqlconn, user_id);
                    await DropUser(sqlconn, user_id);
                }

                //create new user
                new_user_id = await GenerateUniqueUserID(sqlconn);
                await AddNewUserToDatabase(sqlconn, new_user_id, new_name, new_desc);

                //add new user to lists
                foreach (var list_id in add_result_to)
                    await AddUserToV2List(sqlconn, list_id, new_user_id);

                //associate cards with new user
                foreach(var card_id in card_ids)
                    using (MySqlCommand sqlcmd = new MySqlCommand("update `cards` set user_id=@user_id where uid=@card_uid;", sqlconn))
                    {
                        sqlcmd.Parameters.AddWithValue("@user_id", new_user_id);
                        sqlcmd.Parameters.AddWithValue("@card_uid", card_id);

                        await sqlcmd.ExecuteNonQueryAsync();
                    }

            });

            return new_user_id;
        }

        public static async Task<string[]> BuildDenseAutocompleteSource(CancellationToken tolken)
        {
            List<string> lookup_strings = new List<string>();

            await Task.Run(async () =>
            {
                //build a lost of all user_ids, names, descriptions, and cards assigned to users
                var db_connection = await ARDBConnectionManager.default_manager.CheckOut();

                try
                {
                    using (MySqlCommand sqlcmd = new MySqlCommand("select * from users", db_connection.Connection))
                    {
                        using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                            while (await reader.ReadAsync())
                            {
                                lookup_strings.Add(reader.GetUInt64(0).ToString());
                                lookup_strings.Add(reader.GetString(1));
                                lookup_strings.Add(reader.GetString(2));
                            }
                    }

                    using (MySqlCommand sqlcmd = new MySqlCommand("select * from cards", db_connection.Connection))
                    {
                        using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                            while (await reader.ReadAsync())
                            {
                                ulong card_uid = reader.GetUInt64(0);

                                lookup_strings.Add(card_uid.ToString());
                                lookup_strings.Add(BaseConverter.EncodeFromBase10(card_uid));
                            }
                    }
                }
                catch (Exception ex) { }

                ARDBConnectionManager.default_manager.CheckIn(db_connection);
            }, tolken);

            return lookup_strings.ToArray();
        }
    }
}
