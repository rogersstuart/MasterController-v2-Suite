using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCICommon;
using System.Threading;

namespace DeviceServer
{
    public static class ServiceLog
    {
        private static System.Timers.Timer log_write_timer = new System.Timers.Timer(1000);
        private static volatile bool timer_started = false;
        private static Object timer_lock = new Object();

        private static ConcurrentQueue<KeyValuePair<DateTime, string>> pending_log_entries = new ConcurrentQueue<KeyValuePair<DateTime, string>>();
        private static string log_path = System.AppDomain.CurrentDomain.BaseDirectory + "/mci_v2_device_server_log.txt";
        private static int max_log_entries = 10000;

        private static void StartTimer()
        {
            lock (timer_lock)
            {
                if (timer_started)
                    return;

                log_write_timer.Elapsed += TimerTask;
                log_write_timer.AutoReset = true;
                log_write_timer.Start();
                timer_started = true;
            }
        }

        public static void AppendLog(string line)
        {
            AppendLog(DateTime.Now, line);
        }

        public static void AppendLog(DateTime timestamp, string line)
        {
            if (!timer_started)
                Task.Run(() =>
                {
                    log_write_timer.Stop();
                    StartTimer();
                    log_write_timer.Start();
                });

            pending_log_entries.Enqueue(new KeyValuePair<DateTime, string>(timestamp, line));
        }

        private static void FileCheck(string[] paths)
        {
            List<Process> blocking_processes = new List<Process>();
            foreach (string path in paths)
                blocking_processes.AddRange(FileUtil.WhoIsLocking(path));

            if (blocking_processes.Count() > 0)
                foreach (Process p in blocking_processes)
                    p.Kill();
        }

        private static void TimerTask(object a, object b)
        {
            //Stopwatch sw = Stopwatch.StartNew();

            if (!pending_log_entries.IsEmpty)
            {
                if (File.Exists(log_path))
                {
                    FileCheck(new string[] { log_path });

                    string[] log_lines = File.ReadAllLines(log_path);

                    if (log_lines.Length > max_log_entries)
                        using (StreamWriter line_writer = new StreamWriter(File.OpenWrite(log_path)))
                            for (int i = 0; i < max_log_entries - 10; i++)
                                line_writer.WriteLine(log_lines[i]);
                }
                else
                    File.Create(log_path).Close();

                List<KeyValuePair<DateTime, string>> pending_entries_dump = new List<KeyValuePair<DateTime, string>>();

                KeyValuePair<DateTime, string> ret;
                while (pending_log_entries.TryDequeue(out ret))
                    pending_entries_dump.Add(ret);

                File.AppendAllLines(log_path, pending_entries_dump.Select(x => "[" + x.Key.ToString() + "] " + x.Value));

                //pending_entries_dump.Clear();
                //pending_entries_dump = null;
            }

            //sw.Stop();
        }
    }
}
