using MCICommon;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace OnlineController
{
    class Program
    {
        private static MqttClient client = new MqttClient(MCv2Persistance.Config.MQTTBroker.AddressString);

        private static readonly ulong elevator_list_id = MCv2Persistance.Config.OnlineControllerList;

        private static FloorStateTracker[] trackers = new FloorStateTracker[] { new FloorStateTracker(1), new FloorStateTracker(2)};

        static void Main(string[] args)
        {
            ARDBConnectionManager.default_manager.Start();

            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            var guid = Guid.NewGuid();
            string clientId = guid.ToString();

            client.Subscribe(new string[] { "acc/elev/0/rdr/tap" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            client.Subscribe(new string[] { "acc/elev/1/rdr/tap" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            client.Connect(clientId);

            Thread.Sleep(-1);
        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // handle message received 

            Task.Run(async () =>
            {
                var uid = BitConverter.ToUInt64(e.Message.Reverse().ToArray(), 0);

                Console.WriteLine(uid);

                //a uid of 0 is invalid
                if (uid == 0)
                    return;

                //get all cards and user associations from the database
                var cards = await GetAllCards();

                //find the user id of any users assigned this card
                var users = cards.AsParallel().Where(x => x.CardNUID == uid).Select(y => y.UserAssignment);

                //check to see if we need to continue
                if (users.Count() == 0)
                {
                    if (e.Topic == "acc/elev/0/rdr/tap")
                        client.Publish("acc/elev/0/rdr/tap/resp", new byte[] { 0 });
                    else
                        if (e.Topic == "acc/elev/1/rdr/tap")
                        client.Publish("acc/elev/1/rdr/tap/resp", new byte[] { 0 });

                    return;
                }

                foreach (var user in users)
                {
                    //get properties for the user from the database to determine if access will be granted and to where

                    if (await ProcessListEntry(user))
                    {
                        //check to see what floors the user should be granted access to
                        var ext = await GetUserExtensions(user);

                        if (ext == null || ext.HomeFloors.Count() == 0)
                        {
                            if (e.Topic == "acc/elev/0/rdr/tap")
                                client.Publish("acc/elev/0/rdr/tap/resp", new byte[] { 0 });
                            else
                            if (e.Topic == "acc/elev/1/rdr/tap")
                                client.Publish("acc/elev/1/rdr/tap/resp", new byte[] { 0 });

                            return;
                        }
                        else
                        {
                            int wait_ctr = 0;

                            Parallel.ForEach(ext.HomeFloors, floor =>
                            {
                                int car = -1;

                                if (e.Topic == "acc/elev/0/rdr/tap")
                                    car = 0;
                                else
                                if (e.Topic == "acc/elev/1/rdr/tap")
                                    car = 1;

                                FloorStateTracker tracker = trackers[car];

                                bool state = tracker.GetFloorState(floor);

                                if(!state)
                                    tracker.UnlockFloor(floor);

                                while (!tracker.GetFloorState(floor))
                                    Thread.Sleep(1);

                                if (!state)
                                    Task.Run(() =>
                                    {
                                        Thread.Sleep(5000);

                                        tracker.SetFloorState(floor, state);
                                    });

                                wait_ctr++;
                            });

                            while (wait_ctr < ext.HomeFloors.Count())
                                await Task.Delay(1);

                            if (e.Topic == "acc/elev/0/rdr/tap")
                                client.Publish("acc/elev/0/rdr/tap/resp", new byte[] { 1 });
                            else
                                if (e.Topic == "acc/elev/1/rdr/tap")
                                client.Publish("acc/elev/1/rdr/tap/resp", new byte[] { 1 });
                        }
                    }
                }
            });
        }

        private static async Task<MCIUserExt> GetUserExtensions(ulong user_id)
        {
            MCIUserExt res = null;

            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

            try
            {
                using (MySqlCommand cmd = new MySqlCommand("select data from `user_extensions` where user_id=" + user_id, sqlconn.Connection))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();

                    if (reader.IsDBNull(0))
                        res = null;
                    else
                        res = JsonConvert.DeserializeObject<MCIUserExt>((string)reader["data"]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while attempting to deserialize the user extensions object.");
            }

            ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            return res;
        }

        public static async Task<List<DBCard>> GetAllCards()
        {
            List<DBCard> cards = new List<DBCard>();
            AutoRefreshDBConnection sqlconn = null;

            try
            {
                sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                using (MySqlCommand sqlcmd = new MySqlCommand("select * from cards", sqlconn.Connection))
                {
                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                        while (reader.Read())
                            cards.Add(new DBCard(reader.GetUInt64(0), reader.GetUInt64(1)));
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            if (sqlconn != null)
                ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            return cards;
        }

        public static async Task<bool> ProcessListEntry(UInt64 user_id)
        {
            string sdays = "", stimes = "";
            byte enable = 0;

            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

            try
            {
                string cmd = "SELECT * FROM `" + elevator_list_id.ToString() + "` WHERE user_id=@user_id;";

                using (MySqlCommand sqlcmd = new MySqlCommand(cmd, sqlconn.Connection))
                {
                    sqlcmd.Parameters.AddWithValue("@user_id", user_id);

                    using (MySqlDataReader reader = sqlcmd.ExecuteReader())
                        if (reader.Read())
                        {
                            sdays = reader.GetFieldValue<string>(1);
                            stimes = reader.GetFieldValue<string>(2);
                            enable = reader.GetFieldValue<byte>(3);
                        }
                }
            }
            catch (Exception ex) { return false; }

            ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            if (enable == 0)
                return false;

            var spans = ListEntryUtilities.ConvertTimeStringToTimeSpans(stimes);
            var days = ListEntryUtilities.ConvertDOWStringToBools(sdays);

            var current_stamp = DateTime.Now;

            var day_of_week = current_stamp.DayOfWeek;

            if (!days[(int)day_of_week])
                return false;

            if (current_stamp.Hour >= spans[0].Hours && current_stamp.Hour <= spans[1].Hours)
                if (current_stamp.Minute >= spans[0].Minutes && current_stamp.Minute <= spans[1].Minutes)
                    return true;

            return false;
        }
    }
}
