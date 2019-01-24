using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public class AutoRefreshDBConnection : IDisposable
    {
        private Object connection_lock = new Object();
        private MySqlConnection sqlconn;

        private TimeSpan refresh_interval;

        private Task refresh_task;

        private Random rand = new Random();

        private volatile bool isRunning = false;
        private volatile bool isRefreshing = false;
        private volatile bool isLocked = false;

        private Guid _guid = Guid.NewGuid();
       
        public AutoRefreshDBConnection(){}

        private void UpdateRefreshConfig()
        {
            var config = MCv2Persistance.Config.DatabaseConfiguration;

            refresh_interval = config.DBCacheRefreshInterval;

            if (config.UseInterleaving)
                refresh_interval += TimeSpan.FromMilliseconds(rand.Next(config.InterleavingDynamicRange.Milliseconds));
        }

        public async Task Start()
        {
            sqlconn = await GenerateNewConnection();
            UpdateRefreshConfig();

            refresh_task = Task.Run(async () =>
            {
                isRunning = true;

                DateTime last_refresh_occured = DateTime.Now;

                while(isRunning)
                {
                    if (!isLocked && (DateTime.Now - last_refresh_occured > refresh_interval))
                    {
                        lock (connection_lock)
                        {
                            Console.WriteLine("refresh occuring " + _guid.ToString());

                            sqlconn.Close();
                            sqlconn.Open();

                            last_refresh_occured = DateTime.Now;
                        }
                    }
                    else
                        await Task.Delay(10000);
                }

                sqlconn.Close();
                isRunning = false;
            });
        }

        public MySqlConnection Connection
        {
            get
            {
                lock(connection_lock)
                {
                    return sqlconn;
                }
            }
        }

        public void Dispose()
        {
            //lock (connection_lock)
            //{
                isRunning = false;
                //refresh_task.Wait();

            //}
        }

        public bool IsLocked
        {
            get { return isLocked; }
            set { isLocked = value; }
        }

        private async Task<MySqlConnection> GenerateNewConnection()
        {
            MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString);
            await sqlconn.OpenAsync();
            return sqlconn;
        }
    }
}
