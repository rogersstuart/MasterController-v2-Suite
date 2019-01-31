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
    public class LoggerOptions
    {
        public LoggerBaseOptions BaseOptions { get; set; }
        public LoggerRetentionOptions RetentionOptions { get; set; }
        public LoggerBlockingOptions BlockingOptions { get; set; }

        public LoggerOptions(LoggerBaseOptions base_options = null, LoggerRetentionOptions retention_options = null, LoggerBlockingOptions blocking_options = null)
        {
            if (base_options != null)
                BaseOptions = base_options;
            else
                BaseOptions = new LoggerBaseOptions();

            if (retention_options != null)
                RetentionOptions = retention_options;
            else
                RetentionOptions = new LoggerRetentionOptions();

            if (blocking_options != null)
                BlockingOptions = blocking_options;
            else
                BlockingOptions = new LoggerBlockingOptions();
        }
    }

    public class LoggerBaseOptions
    {
        public string LogName { get; set; }
        public uint WriteInterval { get; set; }

        private readonly string base_path = System.AppDomain.CurrentDomain.BaseDirectory;

        public LoggerBaseOptions(string log_name = "log", uint write_interval = 1000)
        {
            LogName = log_name;
            WriteInterval = write_interval;
        }

        public string LogPath
        {
            get
            {
                return base_path + LogName + ".txt";
            }
        }
    }

    public class LoggerRetentionOptions
    {
        public uint BlockCallerAtBufferLimit { get; set; }
        public bool EnforceRetentionLimit { get; set; }
        public uint RetainNumLogEntries { get; set; }
        public uint OnCullDropBackNum { get; set; }

        public LoggerRetentionOptions(uint block_caller_at_buffer_limit = 10000, bool enforce_retention_limit = true, uint retain_num_log_entries = 1000000, uint on_cull_drop_back_num = 100)
        {
            BlockCallerAtBufferLimit = block_caller_at_buffer_limit;
            EnforceRetentionLimit = enforce_retention_limit;
            RetainNumLogEntries = retain_num_log_entries;
            OnCullDropBackNum = on_cull_drop_back_num;
        }
    }

    public class LoggerBlockingOptions
    {
        public bool KillBlockingProcess { get; set; }
        public uint OnBlockWaitMs { get; set; }

        public LoggerBlockingOptions(bool kill_blocking_process = true, uint on_block_wait_ms = 60000)
        {
            KillBlockingProcess = kill_blocking_process;
            OnBlockWaitMs = on_block_wait_ms;
        }
    }

    public class FileTextLogger
    {
        public LoggerOptions options = null;

        private ConcurrentQueue<KeyValuePair<DateTime, string>> pending_log_entries = new ConcurrentQueue<KeyValuePair<DateTime, string>>();

        private Object task_locker = new Object();
        private Task log_writer_task = null;
        private bool log_writer_active = false;

        private uint in_block_retention_limit = 10000;

        public FileTextLogger(LoggerOptions logging_options = null, bool autostart = true)
        {
            if (logging_options == null || logging_options.BaseOptions.LogName == null || logging_options.BaseOptions.LogName.Trim() == "" || logging_options.RetentionOptions.RetainNumLogEntries == 0)
                options = new LoggerOptions();
            else
                options = logging_options;

            if (autostart)
                Start();
        }

        public void Start()
        {
            lock(task_locker)
            {
                log_writer_task = GenerateLogWriterTask();
                log_writer_task.Start();
            }
        }

        public void Stop()
        {

        }

        private Task GenerateLogWriterTask()
        {
            return new Task(async () =>
            {
                log_writer_active = true;

                var last_check = DateTime.MinValue;

                //enter contention check
                while(log_writer_active)
                {
                    try
                    {
                        //check for a block and try to resolve the situation if present
                        if (IsBlocked(new string[] { options.BaseOptions.LogPath }))
                        {
                            if (options.BlockingOptions.KillBlockingProcess && (DateTime.Now - last_check >= TimeSpan.FromMilliseconds(options.BlockingOptions.OnBlockWaitMs)))
                            {
                                //try to end the contention

                                KillBlockingProcesses(new string[] { options.BaseOptions.LogPath });
                            }
                            else
                            {
                                //we're blocked but not ready or configured to end the contention. make sure that the buffer is maintained then delay
                                //for a bit before checking again if we didn't waste time dequeing

                                KeyValuePair<DateTime, string> out_val;

                                if (pending_log_entries.Count() > in_block_retention_limit)
                                {
                                    while (pending_log_entries.Count() > in_block_retention_limit)
                                        pending_log_entries.TryDequeue(out out_val);
                                }
                                else
                                    await Task.Delay(1);
                            }

                        }

                        //try to create the filestream
                        using (FileStream log_stream = new FileStream(options.BaseOptions.LogPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                        {
                            using (StreamReader log_reader = new StreamReader(log_stream))
                            {
                                //start by reading all of the lines into an array
                                List<string> log_lines = new List<string>();
                                string log_line;
                                while ((log_line = await log_reader.ReadLineAsync()) != null)
                                    log_lines.Add(log_line);

                                //start the writer loop
                                while (log_writer_active)
                                {
                                    //check to see if we should pull lines from the buffer and write them to file
                                    if ((DateTime.Now - last_check >= TimeSpan.FromMilliseconds(options.BaseOptions.WriteInterval)) && !pending_log_entries.IsEmpty)
                                    {
                                        //pull the records from the buffer, convert them to lines, and add them to the tracking array
                                        List<KeyValuePair<DateTime, string>> pending_entries_dump = new List<KeyValuePair<DateTime, string>>();

                                        KeyValuePair<DateTime, string> ret;
                                        while (pending_log_entries.TryDequeue(out ret))
                                            pending_entries_dump.Add(ret);

                                        log_lines.AddRange(pending_entries_dump.Select(x => "[" + x.Key.ToString() + "] " + x.Value));
                                        //

                                        //check to see if a cull is needed
                                        if (options.RetentionOptions.EnforceRetentionLimit && log_lines.Count() > options.RetentionOptions.RetainNumLogEntries)
                                        {
                                            //cull the log lines

                                            var log_lines_end_cull = log_lines.Count() - options.RetentionOptions.RetainNumLogEntries;
                                            log_lines = log_lines.Where((x, y) => y >= log_lines_end_cull+options.RetentionOptions.OnCullDropBackNum).ToList();
                                        }

                                        //write the log lines to a memorystream to get a byte array, set the new file size, and then write and flush
                                        using (MemoryStream ms = new MemoryStream())
                                        {
                                            using (StreamWriter sw = new StreamWriter(ms))
                                            {
                                                foreach (var line in log_lines)
                                                    await sw.WriteLineAsync(line);

                                                await sw.FlushAsync();
                                            }

                                            var bytes = ms.ToArray();

                                            log_stream.SetLength(bytes.Length);
                                            log_stream.Position = 0;
                                            await log_stream.WriteAsync(bytes, 0, bytes.Length);
                                            await log_stream.FlushAsync();
                                        }
                                        
                                    }
                                    else
                                        await Task.Delay(1);
                                }

                                break;
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        //an exception occured. just loop back around and restart
                    }
                }
            });
        }

        public Task AppendLogAsync(string line)
        {
            return AppendLogAsync(DateTime.Now, line);
        }

        public async Task AppendLogAsync(DateTime timestamp, string line)
        {
            while (pending_log_entries.Count() > options.RetentionOptions.BlockCallerAtBufferLimit)
                await Task.Delay(1);

            pending_log_entries.Enqueue(new KeyValuePair<DateTime, string>(timestamp, line));
        }

        public void AppendLog(string line)
        {
            AppendLog(DateTime.Now, line);
        }

        public void AppendLog(DateTime timestamp, string line)
        {
            while (pending_log_entries.Count() > options.RetentionOptions.BlockCallerAtBufferLimit)
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
    }
}
