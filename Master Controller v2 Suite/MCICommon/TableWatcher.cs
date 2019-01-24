using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MCICommon
{
    public class TableWatcher : IDisposable
    {
        public event TableChangeOccured tablechangeevent;
        public delegate void TableChangeOccured(string table_name, long checksum);

        private string table_name;
        private Task table_watcher_task;
        private bool watcher_running;
        private TimeSpan check_interval = TimeSpan.FromSeconds(4);
        private long last_check_occured_at = DateTime.MinValue.Ticks;
        private long last_checksum = -1;

        public TableWatcher(string table_name)
        {
            this.table_name = table_name;

            table_watcher_task = new Task(async () =>
            {
                watcher_running = true;

                while(watcher_running)
                {
                    if (DateTime.Now.Ticks - last_check_occured_at >= check_interval.Ticks)
                    {
                        

                        last_check_occured_at = DateTime.Now.Ticks;

                        long new_checksum = -1;
                        var cmd_str = "CHECKSUM TABLE " + table_name + ";";
                        var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

                        try
                        {
                            using (MySqlCommand sqlcmd = new MySqlCommand(cmd_str, sqlconn.Connection))
                                using (MySqlDataReader reader = (MySqlDataReader)(await sqlcmd.ExecuteReaderAsync()))
                                    if(await reader.ReadAsync())
                                    {
                                        new_checksum = reader.GetUInt32(1);
                                        //Console.WriteLine("checksum " + new_checksum);
                                    }
                                    
                        }
                        catch(Exception ex)
                        {

                        }

                        ARDBConnectionManager.default_manager.CheckIn(sqlconn);

                        if (new_checksum != last_checksum)
                            Task.Run(() => { tablechangeevent(table_name, new_checksum); });

                        last_checksum = new_checksum;
                    }
                    else
                        await Task.Delay(250);
                }
            });
        }

        public void Begin()
        {
            table_watcher_task.Start();
        }

        public void Reset()
        {
            Interlocked.Exchange(ref last_check_occured_at, DateTime.Now.Ticks);
        }

        public void Dispose()
        {
            watcher_running = false;
            table_watcher_task.Wait();
        }
    }
}
