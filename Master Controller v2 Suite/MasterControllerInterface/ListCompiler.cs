using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MySql.Data.MySqlClient;
using MCICommon;

namespace MasterControllerInterface
{
    public static class ListCompiler
    {
        private static readonly string flag_string = "1:10:13:14";

        public static async Task<byte[]> GenerateGen2List(UInt64 list_uid)
        {
            byte[] ms_contents = null;
            
            await Task.Run(async () =>
            { 
                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(ms))
                    {
                        var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                        try
                        {
                            //get user ids

                            List<UInt64> user_ids = new List<UInt64>();

                            using (MySqlCommand cmdName = new MySqlCommand("select user_id from `" + list_uid + "`", sqlconn.Connection))
                            using (MySqlDataReader reader = cmdName.ExecuteReader())
                                while (await reader.ReadAsync())
                                    user_ids.Add(reader.GetUInt64(0));

                            foreach (UInt64 user_id in user_ids)
                            {
                                List<UInt64> associated_cards = new List<UInt64>();

                                using (MySqlCommand sqlcmd = new MySqlCommand("select uid from `cards` where user_id=@user_id", sqlconn.Connection))
                                {
                                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                                        while (await reader.ReadAsync())
                                            associated_cards.Add(reader.GetUInt64(0));
                                }

                                if (associated_cards.Count() == 0)
                                    continue;

                                using (MySqlCommand sqlcmd = new MySqlCommand("select * from `" + list_uid + "` where user_id=@user_id", sqlconn.Connection))
                                {
                                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                                        if (await reader.ReadAsync())
                                            if ((byte)reader["enabled"] == 1)
                                                foreach (UInt64 card_uid in associated_cards)
                                                    await sw.WriteLineAsync(card_uid + "," + reader["times"] + "," + reader["days"] + "," + flag_string);
                                }
                            }
                        }
                        catch (Exception ex) { }

                        ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                        await sw.FlushAsync();

                        ms.Position = 0;

                        ms_contents = ms.ToArray();
                    }
                }
            });

            return ms_contents;  
        }
    }
}
