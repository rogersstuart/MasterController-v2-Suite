using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MCICommon;
using MasterControllerInterface;

namespace DeviceServer
{
    public static class DeviceConnectionManager
    {
        private static System.Timers.Timer refresh_timer = new System.Timers.Timer(60000);

        private static volatile bool refresh_active = false;

        private static Dictionary<ulong, TCPConnectionProperties> expcons_connprops = new Dictionary<ulong, TCPConnectionProperties>();
        private static Dictionary<ulong, LocalExpanderConnection> expcons = new Dictionary<ulong, LocalExpanderConnection>();

        private static Object conn_lock = new Object();

        public static void Start()
        {
            DeviceServer.logger.AppendLog(DateTime.Now, "DCM - Starting");

            RefreshDevices();

            //refresh_timer.AutoReset = true;
            refresh_timer.Elapsed += (a ,b) =>
            {
                refresh_active = true;
                refresh_timer.Stop();

                RefreshDevices();

                refresh_timer.Start();
                refresh_active = false;
            };

            refresh_timer.Start();

            DeviceServer.logger.AppendLog(DateTime.Now, "DCM - Started");
        }

        public static AutoResetEvent Stop()
        {
            DeviceServer.logger.AppendLog(DateTime.Now, "DCM - Stopping");

            var are = new AutoResetEvent(false);

            Task.Run(async () =>
            {
                refresh_timer.Stop();

                do
                    await Task.Delay(100);
                while (refresh_active);

                are.Set();

                DeviceServer.logger.AppendLog(DateTime.Now, "DCM - Stopped");
            });

            return are;
        }

        private static void RefreshDevices()
        {
            lock(conn_lock)
            Task.Run(async () =>
            {
                MySqlConnection sqlconn = null;

                try
                {
                    sqlconn = new MySqlConnection(MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString);
                    await sqlconn.OpenAsync();

                    //the following will return a list of lists containing the following fields
                    ////id, type, address, port, alias

                    var devices = await DatabaseUtilities.GetDevicesFromDatabase(sqlconn);

                    //create a list of expander ids in the database
                    var expander_ids = devices.Where(x => Convert.ToInt32(x[1]) == 1).Select(y => (ulong)y[0]);

                    //create a list of active expanders whos ids arent in the database
                    var exp_ids_not_in_db = expcons.Where(x => !expander_ids.Contains(x.Key)).Select(y => y.Key);

                    //dispose of expanders that werent in the database
                    foreach (var exp_id in exp_ids_not_in_db)
                    {
                        expcons[exp_id].Stop();
                        expcons_connprops.Remove(exp_id);
                        expcons.Remove(exp_id);

                        DeviceServer.logger.AppendLog(DateTime.Now, "DCM - Dropped " + exp_id);
                    }

                    //create a list of new expander ids
                    var new_exp_ids = expander_ids.Where(x => !expcons.Keys.Contains(x));

                    //create monitors for the new expanders and add them to the list of active devices
                    foreach (var exp_id in new_exp_ids)
                    {
                        var connection_info = await DatabaseUtilities.GetDevicePropertiesFromDatabase(sqlconn, exp_id);

                        var endpoint = await connection_info.GetIPEndPointAsync();
                        var ip_string = endpoint.Address.ToString();

                        var expcon = new LocalExpanderConnection(new ConnectionProperties(ip_string, connection_info.Port));
                        //await expcon.Start();

                        expcons_connprops.Add(exp_id, connection_info);
                        expcons.Add(exp_id, expcon);

                        DeviceServer.logger.AppendLog(DateTime.Now, "DCM - Added " + exp_id);
                    }

                    //create a list of expanders that were already active and are still present in the database
                    var same_exp_ids = new_exp_ids.Where(x => !expander_ids.Contains(x));

                    //check to see if the connection properties have changed. if so remove the old instance and add a new one
                    foreach (var exp_id in same_exp_ids)
                    {
                        var current_connection_info = await DatabaseUtilities.GetDevicePropertiesFromDatabase(sqlconn, exp_id);
                        var old_connection_info = expcons_connprops[exp_id];

                        if (current_connection_info.AddressString != old_connection_info.AddressString ||
                            current_connection_info.Port != old_connection_info.Port)
                        {
                            expcons[exp_id].Stop();
                            expcons[exp_id] = new LocalExpanderConnection(new ConnectionProperties((await current_connection_info.GetIPEndPointAsync()).ToString(), current_connection_info.Port));

                            expcons_connprops[exp_id] = current_connection_info;

                            //await expcons[exp_id].Start();

                            DeviceServer.logger.AppendLog(DateTime.Now, "DCM - Modified " + exp_id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DeviceServer.logger.AppendLog(DateTime.Now, "DCM - A fatal error occured while attempting to refresh devices");
                }

                if (sqlconn != null)
                    await sqlconn.CloseAsync();

            }).Wait();

            
        }

        public static LocalExpanderConnection GetConnecion(ulong device_id)
        {
            lock(conn_lock)
                return expcons[device_id];
        }
    }
}
