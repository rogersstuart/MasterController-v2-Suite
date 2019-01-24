using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;

namespace MCICommon
{
    public class ARDBConnectionManager
    {
        private static Object pool_lock = new Object();

        private static List<AutoRefreshDBConnection> db_connection_pool = new List<AutoRefreshDBConnection>();
        private static List<AutoRefreshDBConnection> db_connections_checked_out = new List<AutoRefreshDBConnection>();

        private static string db_connection_string_cache = null;

        public static ARDBConnectionManager default_manager = new ARDBConnectionManager();

        private ProgressInterface progi = null;

        public static ProgressInterface default_progress_interface = null;

        public ARDBConnectionManager(ProgressInterface progi = null)
        {
            if (progi != null)
                default_progress_interface = progi;
            else
                this.progi = default_progress_interface;
        }
        
        public void Start()
        {
            int maintain_num_connections = MCv2Persistance.Config.DatabaseConfiguration.NumCachedDBConnections;

            progi = default_progress_interface;

            if (progi != null)
            {
                progi.SetTitle("Filling Connection Cache");
                progi.Show();
                progi.SetMaximum(maintain_num_connections + 1);
            }

            foreach (AutoRefreshDBConnection sqlconn in db_connections_checked_out)
                if (sqlconn != null)
                    sqlconn.Dispose();

            db_connections_checked_out.Clear();

            foreach (AutoRefreshDBConnection sqlconn in db_connection_pool)
                if (sqlconn != null)
                    sqlconn.Dispose();

            db_connection_pool.Clear();

            db_connection_string_cache = MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString;

            //ready
            if (progi != null)
                progi.Step();

            Object step_locker = new Object();

            var connhold = new ConcurrentBag<AutoRefreshDBConnection>();
            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = -1; // -1 is for unlimited. 1 is for sequential.
            options.TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            Parallel.For(0, maintain_num_connections, options, async i =>
            {
                connhold.Add(await GenerateNewConnection());

                if (progi != null)
                    lock (step_locker)
                        progi.Step();
            });

            db_connection_pool.AddRange(connhold);

            if (progi != null)
                progi.Dispose();
        }

        public void Stop()
        {
            lock (pool_lock)
            {
                foreach (AutoRefreshDBConnection sqlconn in db_connections_checked_out)
                    if (sqlconn != null)
                        sqlconn.Dispose();

                db_connections_checked_out.Clear();

                foreach (AutoRefreshDBConnection sqlconn in db_connection_pool)
                    if (sqlconn != null)
                        sqlconn.Dispose();

                db_connection_pool.Clear();

                //progi = null;
            }
        }

        public int NumCheckedOut
        {
            get { lock(pool_lock){ return db_connections_checked_out.Count(); } }
        }

        public Task<AutoRefreshDBConnection> CheckOut()
        {
            return Task.Run(() =>
            {
                AutoRefreshDBConnection sqlconn = null;

                lock (pool_lock)
                {
                    if (db_connection_string_cache != MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString)
                    {
                        //the connection string has changed. all connections are now invalid.

                        foreach (AutoRefreshDBConnection sqlconn_b in db_connections_checked_out)
                            if (sqlconn_b != null)
                                sqlconn_b.Dispose();

                        db_connections_checked_out.Clear();

                        foreach (AutoRefreshDBConnection sqlconn_b in db_connection_pool)
                            if (sqlconn_b != null)
                                sqlconn_b.Dispose();

                        db_connection_pool.Clear();

                        db_connection_string_cache = MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString;
                    }

                    if (db_connection_pool.Count() > 0)
                    {
                        sqlconn = db_connection_pool.First();
                        db_connection_pool.Remove(sqlconn);
                        db_connections_checked_out.Add(sqlconn);
                    }
                    else
                    {
                        sqlconn = new Func<AutoRefreshDBConnection>(() =>
                        {
                            var x = GenerateNewConnection();
                            x.Wait();
                            return x.Result;
                        }).Invoke();
                        db_connections_checked_out.Add(sqlconn);
                    }

                }

                return sqlconn;
            });
            
        }

        public void CheckIn(AutoRefreshDBConnection sqlconn)
        {
            lock (pool_lock)
            {
                int maintain_num_connections = MCv2Persistance.Config.DatabaseConfiguration.NumCachedDBConnections;

                db_connections_checked_out.Remove(sqlconn);

                if (db_connection_pool.Count() >= maintain_num_connections)
                    sqlconn.Dispose();
                else
                    db_connection_pool.Add(sqlconn);
            }
        }

        private async Task<AutoRefreshDBConnection> GenerateNewConnection()
        {
            AutoRefreshDBConnection sqlconn = new AutoRefreshDBConnection();
            await sqlconn.Start();
            return sqlconn;
        }
    }
}
