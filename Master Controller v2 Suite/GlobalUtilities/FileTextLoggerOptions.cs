using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalUtilities
{
    public class LoggerOptions
    {
        public LoggerBaseOptions BaseOptions { get; set; }
        public LoggerLoggingOptions LoggingOptions { get; set; }
        public LoggerRetentionOptions RetentionOptions { get; set; }
        public LoggerContentionOptions ContentionOptions { get; set; }
        public LoggerPerformanceOptions PerformanceOptions { get; set; }

        public LoggerOptions(LoggerBaseOptions base_options = null, LoggerLoggingOptions logging_options = null, LoggerRetentionOptions retention_options = null, LoggerContentionOptions contention_options = null,
            LoggerPerformanceOptions performance_options = null)
        {
            if (base_options != null)
                BaseOptions = base_options;
            else
                BaseOptions = new LoggerBaseOptions();

            if (logging_options != null)
                LoggingOptions = logging_options;
            else
                LoggingOptions = new LoggerLoggingOptions();

            if (retention_options != null)
                RetentionOptions = retention_options;
            else
                RetentionOptions = new LoggerRetentionOptions();

            if (contention_options != null)
                ContentionOptions = contention_options;
            else
                ContentionOptions = new LoggerContentionOptions();

            if (performance_options != null)
                PerformanceOptions = performance_options;
            else
                PerformanceOptions = new LoggerPerformanceOptions();
        }
    }

    public class LoggerBaseOptions
    {
        public string LogName { get; set; }
        public uint WriteInterval { get; set; }

        public LoggerBaseOptions(string log_name = "log", uint write_interval = 0)
        {
            LogName = log_name;
            WriteInterval = write_interval;
        }

        public string LogPath
        {
            get
            {
                return System.AppDomain.CurrentDomain.BaseDirectory + LogName + ".txt";
            }
        }
    }

    public class LoggerRetentionOptions
    {
        public bool EnforceRetentionLimit { get; set; }
        public uint RetainNumLogEntries { get; set; }

        public LoggerRetentionOptions(bool enforce_retention_limit = true, uint retain_num_log_entries = 100000)
        {
            EnforceRetentionLimit = enforce_retention_limit;
            RetainNumLogEntries = retain_num_log_entries;
        }
    }

    public class LoggerContentionOptions
    {
        public bool KillBlockingProcess { get; set; }
        public uint OnBlockWaitMs { get; set; }
        public bool EnableInBlockRetentionLimit { get; set; }
        public uint InBlockRetentionLimit { get; set; }

        public LoggerContentionOptions(bool kill_blocking_process = true, uint on_block_wait_ms = 60000, bool enable_in_block_retention_limit = true, uint in_block_retention_limit = 10000)
        {
            KillBlockingProcess = kill_blocking_process;
            OnBlockWaitMs = on_block_wait_ms;
            EnableInBlockRetentionLimit = enable_in_block_retention_limit;
            InBlockRetentionLimit = in_block_retention_limit;
        }
    }

    public class LoggerPerformanceOptions
    {
        public uint OnCullDropBackNum { get; set; }
        public bool EnableBlockCallerAtBufferLimit { get; set; }
        public uint BlockCallerAtBufferLimit { get; set; }
        public bool EnableIngestionRateLimit { get; set; }
        public uint IngestionRateLimit { get; set; }

        public LoggerPerformanceOptions(uint on_cull_drop_back_num = 100, bool enable_block_caller_at_buffer_limit = true, uint block_caller_at_buffer_limit = 20,
            bool enable_ingestion_rate_limit = false, uint ingestion_rate_limit = 1)
        {
            OnCullDropBackNum = on_cull_drop_back_num;
            EnableBlockCallerAtBufferLimit = enable_block_caller_at_buffer_limit;
            BlockCallerAtBufferLimit = block_caller_at_buffer_limit;
            EnableIngestionRateLimit = enable_ingestion_rate_limit;
            IngestionRateLimit = ingestion_rate_limit;
        }
    }

    public class LoggerLoggingOptions
    {
        public string TimeStampFormat { get; set; }

        public LoggerLoggingOptions(string time_stamp_format = "MM/dd/yyyy hh:mm:ss.fff tt")
        {
            if (time_stamp_format != null && time_stamp_format.Trim() != "")
                TimeStampFormat = time_stamp_format;
            else
                TimeStampFormat = "MM/dd/yyyy hh:mm:ss.fff tt";
        }
    }
}
