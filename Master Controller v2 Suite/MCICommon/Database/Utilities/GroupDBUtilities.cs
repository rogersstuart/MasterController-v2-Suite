using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public static class GroupDBUtilities
    {
        public static async Task UserGroupsInit()
        {
            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

            var tables = await DatabaseUtilities.GetTableNames(sqlconn.Connection);

            ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            if (!tables.Contains("user_groups"))
                await CreateGroupDescriptionTable();
        }

        public static Task<Dictionary<UInt64, string>> GetDictionaryDescriptionForAllGroups()
        {
            return Task.Run(async () =>
            {

                Dictionary<UInt64, string> user_groups_desc = new Dictionary<UInt64, string>();

                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                try
                {
                    using (MySqlCommand sqlcmd = new MySqlCommand("SELECT * FROM `user_groups`;", sqlconn.Connection))
                    using (MySqlDataReader reader = (MySqlDataReader)(await sqlcmd.ExecuteReaderAsync()))
                        while (await reader.ReadAsync())
                        {
                            ulong group_id = reader.GetUInt64("group_id");
                            string description = reader.GetString("description");

                            if (description.Trim() == "")
                                description = group_id + "";

                            user_groups_desc.Add(group_id, description);
                        }
                }
                catch(Exception ex)
                {

                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                return user_groups_desc;
            });
        }

        public static Task CreateGroupDescriptionTable()
        {
            return Task.Run(async () =>
            {
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                try
                {
                    string cmdstr = "CREATE TABLE `user_groups` (`group_id` BIGINT UNSIGNED NOT NULL, `description` VARCHAR(255) NULL,PRIMARY KEY (`group_id`),UNIQUE INDEX `group_id_UNIQUE` (`group_id` ASC));";

                    using (MySqlCommand sqlcmd = new MySqlCommand(cmdstr, sqlconn.Connection))
                        sqlcmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {

                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);
            });
        }

        public static Task CreateGroupTable(ulong group_id)
        {
            return Task.Run(async () =>
            {
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                try
                {
                    string cmdstr = "CREATE TABLE `" + group_id + "` (`user_id` BIGINT UNSIGNED NOT NULL, PRIMARY KEY (`user_id`),UNIQUE INDEX `user_id_UNIQUE` (`user_id` ASC));";

                    using (MySqlCommand sqlcmd = new MySqlCommand(cmdstr, sqlconn.Connection))
                        sqlcmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {

                }


                ARDBConnectionManager.default_manager.CheckIn(sqlconn);
            });
        }

        public static Task<UInt64> GenerateUniqueGroupID()
        {
            return Task.Run(new Func<Task<UInt64>>(async () =>
            {
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                List<UInt64> uids = new List<UInt64>();

                using (MySqlCommand cmdName = new MySqlCommand("select group_id from `user_groups`", sqlconn.Connection))
                using (MySqlDataReader reader = cmdName.ExecuteReader())
                    while (await reader.ReadAsync())
                        uids.Add(reader.GetUInt64(0));

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                Random r = new Random();
                byte[] uid_bytes = new byte[8];

                r.NextBytes(uid_bytes);

                while (uids.Contains(BitConverter.ToUInt64(uid_bytes, 0)))
                    r.NextBytes(uid_bytes);

                return BitConverter.ToUInt64(uid_bytes, 0);
            }));
        }

        public static Task<List<ulong>> GetGroupIDs()
        {
            return Task.Run(async () =>
            {
                var group_list = new List<ulong>();

                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                try
                {
                    using (MySqlCommand cmd = new MySqlCommand("select group_id from `user_groups`", sqlconn.Connection))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                            group_list.Add(reader.GetUInt64(0));
                    }
                }
                catch (Exception ex)
                {

                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                return group_list;
            });
            
        }

        public static async Task<List<ulong>> GetUsersInGroup(ulong group_id)
        {
            var group_list = await GetGroupIDs();

            if (!group_list.Contains(group_id))
                throw new Exception("User group " + group_id + "doesn't exist.");

            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

            var users = new List<ulong>();

            try
            {
                using (MySqlCommand cmd = new MySqlCommand("select user_id from `" + group_id + "`", sqlconn.Connection))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (await reader.ReadAsync())
                        users.Add(reader.GetUInt64(0));
                }
            }
            catch (Exception ex)
            {
                    
            }

            ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            return users;
        }

        public static async Task SetUsersInGroup(ulong group_id, ulong[] user_ids, ProgressInterface progi = null)
        {
            if(progi != null)
            {
                progi.Show();
                progi.SetLabel("Gathering Information");
                progi.SetMaximum(2);
            }

            var group_list = await GetGroupIDs();

            if (progi != null)
                progi.Step();

            if (!group_list.Contains(group_id))
                throw new Exception("User group " + group_id + "doesn't exist.");

            var users_in_group = await GetUsersInGroup(group_id);

            if (progi != null)
                progi.Step();

            var users_to_remove = users_in_group.Where(x => !user_ids.Contains(x));
            var users_to_add = user_ids.Where(x => !users_in_group.Contains(x));

            if(progi != null)
            {
                progi.Reset();
                progi.SetMaximum(2);
            }
            

            //remove users

            if(users_to_remove.Count() > 0)
            {
                var sqlconn2 = await ARDBConnectionManager.default_manager.CheckOut();

                string val = "";

                foreach (var user in users_to_remove)
                    val += user + ", ";

                val = val.Substring(0, val.Length - 2);

                using (MySqlCommand sqlcmd = new MySqlCommand("delete from `" + group_id + "` where user_id in (" + val + ");", sqlconn2.Connection))
                {
                    

                    //sqlcmd.Parameters.AddWithValue("@user_ids", val);

                    await sqlcmd.ExecuteNonQueryAsync();
                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn2);

                if (progi != null)
                    progi.Step();
            }

            //add users
            if(users_to_add.Count() > 0)
            {
                var sqlconn2 = await ARDBConnectionManager.default_manager.CheckOut();

                string val = "";

                foreach (var user in users_to_add)
                    val += "(" + user + "), ";

                val = val.Substring(0, val.Length - 2);

                using (MySqlCommand sqlcmd = new MySqlCommand("insert into `" + group_id + "` (user_id) values " + val + ";", sqlconn2.Connection))
                {
                    

                    //sqlcmd.Parameters.AddWithValue("@user_ids", val);

                    await sqlcmd.ExecuteNonQueryAsync();
                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn2);

                if (progi != null)
                    progi.Step();
            }

            progi.Dispose();
        }

        public static Task<ulong> AddUserGroup(string description)
        {
            return Task.Run(async () =>
            {
                var group_id = await GenerateUniqueGroupID();

                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                using (MySqlCommand sqlcmd = new MySqlCommand("insert into `user_groups` (group_id, description) values (@group_id, @description)", sqlconn.Connection))
                {
                    sqlcmd.Parameters.AddWithValue("@group_id", group_id);
                    sqlcmd.Parameters.AddWithValue("@description", description);
                    await sqlcmd.ExecuteNonQueryAsync();
                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                await CreateGroupTable(group_id);

                return group_id;
            });
        }

        public static Task RenameUserGroup(ulong group_id, string description)
        {
            return Task.Run(async () =>
            {
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                using (MySqlCommand cmdName = new MySqlCommand("update `user_groups` set description = @description where group_id=" + group_id, sqlconn.Connection))
                {
                    cmdName.Parameters.AddWithValue("@description", description);
                    await cmdName.ExecuteNonQueryAsync();
                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);
            });
        }

        public static Task DeleteUserGroup(ulong group_id)
        {
            return Task.Run(async () =>
            {
                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                using (MySqlCommand sqlcmd = new MySqlCommand("drop table `" + group_id + "`", sqlconn.Connection))
                    await sqlcmd.ExecuteNonQueryAsync();

                using (MySqlCommand sqlcmd = new MySqlCommand("delete from `user_groups` where group_id=" + group_id, sqlconn.Connection))
                    await sqlcmd.ExecuteNonQueryAsync();

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);
            });
        }







        public static Task<MCIUserExt> GetUserExtensions(ulong user_id)
        {
            return Task.Run(new Func<Task<MCIUserExt>>(async () =>
            {
                MCIUserExt res = null;

                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                try
                {
                    using (MySqlCommand cmd = new MySqlCommand("select data from `user_extensions` where user_id=" + user_id, sqlconn.Connection))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        await reader.ReadAsync();

                        if (reader.IsDBNull(0))
                            res = null;
                        //else
                        //    res = JsonConvert.DeserializeObject<MCIUserExt>((string)reader["data"]);
                    }
                }
                catch (Exception ex)
                {

                }

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                return res;
            }));
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

                    if (was_found)
                    {
                        using (MySqlCommand cmdName = new MySqlCommand("update `user_extensions` set data = @data where user_id=" + user_id, sqlconn.Connection))
                        {
                            //cmdName.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(uext));
                            //cmdName.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (MySqlCommand sqlcmd = new MySqlCommand("insert into `user_extensions` (user_id, data) values (@user_id, @data)", sqlconn.Connection))
                        {
                            //sqlcmd.Parameters.AddWithValue("@user_id", user_id);
                            //sqlcmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(uext));

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

    }
}
