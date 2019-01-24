using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon.Database
{
    public static class UserDBUtilities
    {
        public static Task<Dictionary<UInt64, string>> GetDictionaryDescriptionForAllUsers()
        {
            return Task.Run(new Func<Task<Dictionary<UInt64, string>>>(async () =>
            {
                Dictionary<UInt64, string> user_descriptions = new Dictionary<UInt64, string>();

                string cmd = "SELECT * FROM `users`;";

                var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn.Connection))
                {
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

                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                return user_descriptions;
            }));
        }
    }
}
