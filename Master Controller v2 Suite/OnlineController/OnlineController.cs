using MCICommon;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineController
{
    public class OnlineController
    {
        private static MqttClient client = new MqttClient(MCv2Persistance.Instance.Config.MQTTBroker.AddressString);
        private static readonly ulong elevator_list_id = MCv2Persistance.Instance.Config.OnlineControllerList;
        private static FloorStateTracker[] trackers = new FloorStateTracker[] { new FloorStateTracker(1), new FloorStateTracker(2) };

        public OnlineController()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            Start();
        }

        private void Start()
        {
            ARDBConnectionManager.default_manager.Start();

            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            var guid = Guid.NewGuid();
            string clientId = guid.ToString();

            client.Connect(clientId);
            client.Subscribe(new string[] { "acc/elev/0/rdr/tap" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            client.Subscribe(new string[] { "acc/elev/1/rdr/tap" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

            Console.WriteLine("started");
        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // handle message received 

            Task.Run(async () =>
            {
                //determine car number
                int car = -1;

                if (e.Topic == "acc/elev/0/rdr/tap")
                    car = 0;
                else
                if (e.Topic == "acc/elev/1/rdr/tap")
                    car = 1;

                //convert uid
                var uid = BitConverter.ToUInt64(e.Message.Reverse().ToArray(), 0);

                Console.WriteLine(uid);

                //a uid of 0 is invalid so return without doing anything. if the uid actually came from the panel this will
                //result in a blinking red light
                if (uid == 0)
                    return;

                //get all cards from the database
                var cards = await GetAllCards();

                Console.WriteLine("found " + cards.Count() + " cards");

                //find the user id of any users assigned this card
                var users = cards.AsParallel().Where(x => x.CardNUID == uid).Select(y => y.UserAssignment);

                Console.WriteLine(users.Count() + " user(s) are assigned this card");

                //check to see if we need to continue
                if (users.Count() == 0)
                {
                    //commented out to prevent a confirmation denial

                    /*
                    if (car == 0)
                        client.Publish("acc/elev/0/rdr/tap/resp", new byte[] { 0 });
                    else
                        if (car == 1)
                            client.Publish("acc/elev/1/rdr/tap/resp", new byte[] { 0 });
                    */

                    Console.WriteLine("Denied; No Confirmation");

                    return;
                }

                foreach (var user in users)
                {
                    Console.WriteLine("processing...");

                    //get properties for the user from the database to determine if access will be granted and to where

                    if (await ProcessListEntry(user))
                    {
                        //if we're here the user has been approved but not yet granted entry
                        
                        //check to see what floors the user should be granted access to
                        var ext = await GetUserExtensions(user);

                        if (ext == null || ext.HomeFloors.Count() == 0)
                        {
                            //if the user extensions object was null or the user isn't assigned to any floors
                            //respond with a denial signal

                            if (car == 0)
                                client.Publish("acc/elev/0/rdr/tap/resp", new byte[] { 0 });
                            else
                            if (car == 1)
                                client.Publish("acc/elev/1/rdr/tap/resp", new byte[] { 0 });

                            Console.WriteLine("Denied; Confirmed");

                            return;
                        }
                        else
                        {
                            //if we're here then the user has been granted entry

                            int wait_ctr = 0;

                            wait_ctr = ext.HomeFloors.Count();

                            //respond early if there will be an extended delay
                            bool report_early = false;
                            if (report_early = (ext.HomeFloors.Count() > 1))
                            {
                                if (car == 0)
                                    client.Publish("acc/elev/0/rdr/tap/resp", new byte[] { 1 });
                                else
                                if (car == 1)
                                    client.Publish("acc/elev/1/rdr/tap/resp", new byte[] { 1 });

                                Console.WriteLine("Approved Early; Confirmed");
                            }


                            Parallel.ForEach(ext.HomeFloors, floor =>
                            {
                                FloorStateTracker tracker = trackers[car];

                                bool state = tracker.GetFloorState(floor);

                                if (state == false)
                                    tracker.UnlockFloor(floor);

                                while (tracker.GetFloorState(floor) == false)
                                    Thread.Sleep(1);

                                if(state == false)
                                Task.Run(() =>
                                {
                                    Thread.Sleep(5000);

                                    tracker.SetFloorState(floor, state);
                                });

                                wait_ctr++;
                            });
                            

                            while (wait_ctr < ext.HomeFloors.Count())
                                await Task.Delay(1);

                            if(report_early == false)
                            {
                                if (car == 0)
                                    client.Publish("acc/elev/0/rdr/tap/resp", new byte[] { 1 });
                                else
                                    if (car == 1)
                                        client.Publish("acc/elev/1/rdr/tap/resp", new byte[] { 1 });

                                Console.WriteLine("Approved Late; Confirmed");
                            }

                            return;
                        }
                    }
                    else
                    {
                        //the user was denied entry based on the day of week or time of day. respond with a confirmation denial signal

                        if (car == 0)
                            client.Publish("acc/elev/0/rdr/tap/resp", new byte[] { 0 });
                        else
                            if (car == 1)
                            client.Publish("acc/elev/1/rdr/tap/resp", new byte[] { 0 });

                        Console.WriteLine("Denied; Confirmed");

                        return;
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

            if (days[(int)day_of_week] == false)
                return false;

            if (((((current_stamp.Hour == spans[0].Hours) && (current_stamp.Minute >= spans[0].Minutes)) || ((current_stamp.Hour == spans[1].Hours) && current_stamp.Minute <= spans[1].Minutes))) ||
                              ((current_stamp.Hour > spans[0].Hours) && (current_stamp.Hour < spans[1].Hours)))
                    return true;

            return false;
        }
    }
}
