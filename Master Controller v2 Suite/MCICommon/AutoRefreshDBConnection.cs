using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var config = MCv2Persistance.Instance.Config.DatabaseConfiguration;

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
            try
            {
                // Use the enhanced connection string that includes charset
                var connectionString = MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString;
                var connection = new MySqlConnection(connectionString);
                
                await connection.OpenAsync();
                
                // Ensure proper character encoding is set for this session
                try
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        // Set UTF8MB4 encoding for the session to handle all Unicode characters
                        cmd.CommandText = "SET NAMES utf8mb4";
                        await cmd.ExecuteNonQueryAsync();
                        
                        // Set additional session parameters for compatibility
                        cmd.CommandText = "SET SESSION sql_mode='NO_ENGINE_SUBSTITUTION'";
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AutoRefreshDBConnection] Warning: Failed to set session encoding: {ex.Message}");
                    // Don't fail the connection if encoding setup fails
                }
                
                return connection;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AutoRefreshDBConnection] Failed to create connection: {ex.Message}");
                throw;
            }
        }
    }
}
