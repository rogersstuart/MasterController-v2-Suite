using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace GlobalUtilities
{
    public class FileTextLogger : IDisposable
    {
        private LoggerOptions options = null;

        private ConcurrentQueue<KeyValuePair<DateTime, string>> pending_log_entries = new ConcurrentQueue<KeyValuePair<DateTime, string>>();

        private Object task_locker = new Object();
        private Task log_writer_task = null;
        private bool log_writer_active = false;
        private bool log_writer_complete = false;

        private Object ingestion_rate_limit_lock = new Object();
        private Boolean ingestion_rate_limit_token = false;

        /*
        private System.Timers.Timer rate_limiter_timer = ((Func<Object, Boolean, System.Timers.Timer>)((lock_var, token_var) =>
        {
            var timer = new System.Timers.Timer();

            timer.Elapsed += (a,b) =>
            {
                lock (lock_var)
                    token_var = true;
            };

            return timer;
        }))(ingestion_rate_limit_lock, ingestion_rate_limit_token);
        */


        public FileTextLogger(LoggerOptions logging_options = null, bool autostart = true)
        {
            if (logging_options == null || logging_options.BaseOptions.LogName == null || logging_options.BaseOptions.LogName.Trim() == "" || logging_options.RetentionOptions.RetainNumLogEntries == 0)
                options = new LoggerOptions();
            else
                options = logging_options;

            if (autostart)
                Start();
        }

        public LoggerOptions LoggingOptions
        {
            get { return options; }
        }

        public void Start()
        {
            if (log_writer_active)
                throw new Exception("The logger has already been started.");

            lock (task_locker)
            {
                log_writer_task = GenerateLogWriterTask();
                log_writer_complete = false;
                log_writer_task.Start();
            }
        }

        public void Stop()
        {
            if (!log_writer_active)
                throw new Exception("The logger hasn't been started.");

            lock (task_locker)
            {
                log_writer_active = false;
                while (!log_writer_complete)
                    Thread.Sleep(1);
            }
        }

        private Task GenerateLogWriterTask()
        {
            return new Task(async () =>
            {
                log_writer_active = true;

                var last_check = DateTime.MinValue;

                //tracking var
                bool first_run = true;

                //enter log writer loop
                while (log_writer_active)
                {
                    try
                    {
                        //for those times when everything is going wrong
                        await Task.Delay(1);

                        //check for a block and try to resolve the situation if present
                        var start_time = DateTime.Now;

                        if(FileUtil.IsWindows) //if this isn't Windows just skip it for now and hope nothing bad happens
                            while (log_writer_active)
                            {
                                try
                                {
                                    if (IsBlocked(new string[] { options.BaseOptions.LogPath }))
                                    {
                                        if (options.ContentionOptions.KillBlockingProcess && (DateTime.Now - start_time >= TimeSpan.FromMilliseconds(options.ContentionOptions.OnBlockWaitMs)))
                                        {
                                            //try to end the contention

                                            KillBlockingProcesses(new string[] { options.BaseOptions.LogPath });

                                            break;
                                        }
                                        else
                                        {
                                            //we're blocked but not ready and/or configured to end the contention. make sure that the buffer is maintained then delay
                                            //for a bit before checking again if we didn't waste time dequeuing

                                            if (pending_log_entries.Count() > options.ContentionOptions.InBlockRetentionLimit)
                                            {
                                                KeyValuePair<DateTime, string> out_val;
                                                while (pending_log_entries.Count() > options.ContentionOptions.InBlockRetentionLimit)
                                                {
                                                    pending_log_entries.TryDequeue(out out_val);
                                                    Console.WriteLine(out_val.Value + " " + pending_log_entries.Count());
                                                }

                                            }
                                            else
                                                await Task.Delay(1);
                                        }

                                    }
                                    else
                                        break;
                                }
                                catch (Exception ex) { }
                            }

                        if (!log_writer_active)
                            break;

                        //try to create the filestream
                        using (FileStream log_stream = new FileStream(options.BaseOptions.LogPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                        {
                            using (StreamReader log_reader = new StreamReader(log_stream))
                            {
                                //start by reading all of the lines into an array if there's a retention limit
                                List<string> log_lines = new List<string>();

                                if (options.RetentionOptions.EnforceRetentionLimit)
                                {
                                    string log_line;
                                    while ((log_line = await log_reader.ReadLineAsync()) != null)
                                        log_lines.Add(log_line);
                                }

                                //start the writer loop
                                while (log_writer_active)
                                {
                                    //check to see if we should pull lines from the buffer and write them to file
                                    if ((DateTime.Now - last_check >= TimeSpan.FromMilliseconds(options.BaseOptions.WriteInterval)) && !pending_log_entries.IsEmpty)
                                    {
                                        last_check = DateTime.Now;

                                        //pull the records from the buffer, convert them to lines, and add them to the tracking array
                                        List<KeyValuePair<DateTime, string>> pending_entries_dump = new List<KeyValuePair<DateTime, string>>();

                                        KeyValuePair<DateTime, string> ret;
                                        while (pending_log_entries.TryDequeue(out ret))
                                            pending_entries_dump.Add(ret);

                                        var new_lines = pending_entries_dump.AsParallel().AsOrdered().Select(x => "[" + x.Key.ToString(options.LoggingOptions.TimeStampFormat) + "] " + x.Value).ToList();

                                        //only use the log lines list if there's a retention limit
                                        if (options.RetentionOptions.EnforceRetentionLimit)
                                            log_lines.AddRange(new_lines);
                                        //

                                        //check to see if a cull is needed
                                        bool cull_occured = false;
                                        if (options.RetentionOptions.EnforceRetentionLimit && log_lines.Count() > options.RetentionOptions.RetainNumLogEntries)
                                        {
                                            //a cull is needed so do it

                                            var log_lines_end_cull = (log_lines.Count() - options.RetentionOptions.RetainNumLogEntries) + options.PerformanceOptions.OnCullDropBackNum;
                                            //log_lines = log_lines.Where((x, i) => i >= log_lines_end_cull).ToList(); //I think this is slower
                                            log_lines = log_lines.GetRange((int)log_lines_end_cull, log_lines.Count() - (int)log_lines_end_cull);

                                            cull_occured = true;
                                        }

                                        //write the log lines to a memorystream to get a byte array, set the new file size, then write and flush
                                        using (MemoryStream ms = new MemoryStream())
                                        {
                                            using (StreamWriter sw = new StreamWriter(ms))
                                            {
                                                foreach (var line in (options.RetentionOptions.EnforceRetentionLimit ? (cull_occured || first_run ? log_lines : new_lines) : new_lines))
                                                    await sw.WriteLineAsync(line);

                                                await sw.FlushAsync();
                                            }

                                            var bytes = ms.ToArray();

                                            //set the new position based on the retention settings
                                            log_stream.Position = options.RetentionOptions.EnforceRetentionLimit ? (cull_occured || first_run ? 0 : log_stream.Length) : log_stream.Length;

                                            //set the new length based on the retention settings
                                            log_stream.SetLength(options.RetentionOptions.EnforceRetentionLimit ? (cull_occured || first_run ? bytes.LongLength : log_stream.Length + bytes.LongLength) : log_stream.Length + bytes.LongLength);

                                            await log_stream.WriteAsync(bytes, 0, bytes.Length);
                                            await log_stream.FlushAsync();

                                            Console.WriteLine("wrote " + bytes.Length + " bytes");
                                        }
                                    }
                                    else
                                        await Task.Delay(1);

                                    //set tracking var
                                    first_run = false;
                                }

                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //an exception occured. just loop back around and restart

                        //reset tracking var
                        first_run = true;
                    }
                }

                log_writer_complete = true;
            });
        }

        public Task AppendLogAsync(string line)
        {
            return AppendLogAsync(DateTime.Now, line);
        }

        public async Task AppendLogAsync(DateTime timestamp, string line)
        {
            /*
            if (options.PerformanceOptions.EnableIngestionRateLimit)
            {
                //try to take the token
                lock(ingestion_rate_limit_lock)
                {

                }
            }
            */

            if (options.PerformanceOptions.EnableBlockCallerAtBufferLimit)
                while (pending_log_entries.Count() > options.PerformanceOptions.BlockCallerAtBufferLimit)
                    await Task.Delay(1);

            pending_log_entries.Enqueue(new KeyValuePair<DateTime, string>(timestamp, line));
        }

        public void AppendLog(string line)
        {
            AppendLog(DateTime.Now, line);
        }

        public void AppendLog(DateTime timestamp, string line)
        {

            if (options.PerformanceOptions.EnableBlockCallerAtBufferLimit)
                while (pending_log_entries.Count() > options.PerformanceOptions.BlockCallerAtBufferLimit)
                    Thread.Sleep(1);

            pending_log_entries.Enqueue(new KeyValuePair<DateTime, string>(timestamp, line));
        }

        private bool IsBlocked(string[] file_paths)
        {
            List<Process> blocking_processes = new List<Process>();
            foreach (string path in file_paths)
                blocking_processes.AddRange(FileUtil.WhoIsLocking(path));

            if (blocking_processes.Count() > 0)
                return true;
            else
                return false;
        }

        private void KillBlockingProcesses(string[] file_paths)
        {
            List<Process> blocking_processes = new List<Process>();
            foreach (string path in file_paths)
                blocking_processes.AddRange(FileUtil.WhoIsLocking(path));

            if (blocking_processes.Count() > 0)
                foreach (Process p in blocking_processes)
                    p.Kill();
        }

        public void Dispose()
        {
            throw new NotImplementedException();

            //if disposed while the log writer is active...
        }
    }
}
