using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using MCICommon;

namespace MCICommon
{
    public static class ConfigurationManager
    {
        private static string configuration_file_path = "mci_config.bin";

        private static string program_version = "1.4 PRE 6";

        private static List<string> list_paths = new List<string>(); //default: empty
        private static List<bool> list_check_states = new List<bool>();//defualt: none

        //connection types, 0 = com, 1 = tcp
        private static int connection_type = 1; //default: tcp

        //protocol version, 0 = v1, 1 = v2
        //private static int protocol_version = 1; //default: v2.0

        private static string selected_com_port = ""; //default: none

        private static TCPConnectionProperties selected_tcp_connection = null; //default: none

        private static List<TCPConnectionProperties> tcp_connection_history = new List<TCPConnectionProperties>();

        private static List<string> session_temp_files = new List<string>();

        private static bool retain_temp_files = false; //default: don't retain

        private static ConcurrentQueue<KeyValuePair<DateTime, string>> pending_log_entries = new ConcurrentQueue<KeyValuePair<DateTime, string>>();
        private static Object log_lock = new Object();
        private static Object queue_lock = new Object();
        private static string log_path = "mci_app_log.txt";
        private static int max_log_entries = 1000;

        private static int cached_protocol_version = -1;

        private static Guid instance_id = Guid.NewGuid();

        public static void ReadConfigurationFromFile()
        {
            if (File.Exists(configuration_file_path))
            {
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(configuration_file_path)))
                {
                    BinaryFormatter bf = new BinaryFormatter();

                    if ((string)bf.Deserialize(ms) != program_version)
                    {
                        File.Delete(configuration_file_path);
                        WriteConfigurationToFile();
                    }
                    else
                    {
                        list_paths = (List<string>)bf.Deserialize(ms);
                        list_check_states = (List<bool>)bf.Deserialize(ms);
                        connection_type = (int)bf.Deserialize(ms);
                        //protocol_version = (int)bf.Deserialize(ms);
                        selected_com_port = (string)bf.Deserialize(ms);

                        long ms_pos = ms.Position;

                        try
                        {
                           //selected_tcp_connection = (TCPConnectionProperties)bf.Deserialize(ms);
                        }
                        catch(Exception ex)
                        {
                            //it's a string because it's null
                            ms.Position = ms_pos;
                            bf.Deserialize(ms);
                        }

                        //tcp_connection_history = (List<TCPConnectionProperties>)bf.Deserialize(ms);
                        //retain_temp_files = (bool)bf.Deserialize(ms);
                    }
                }
            }
            else
                WriteConfigurationToFile();
        }

        public static void WriteConfigurationToFile()
        {
            byte[] cfg_bytes;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();

                bf.Serialize(ms, program_version);
                bf.Serialize(ms, list_paths);
                bf.Serialize(ms, list_check_states);
                bf.Serialize(ms, connection_type);
                //bf.Serialize(ms, protocol_version);
                bf.Serialize(ms, selected_com_port);

                if (selected_tcp_connection == null)
                    bf.Serialize(ms, "");
                else
                    bf.Serialize(ms, selected_tcp_connection);

                bf.Serialize(ms, tcp_connection_history);
                bf.Serialize(ms, retain_temp_files);

                ms.Position = 0;
                cfg_bytes = ms.ToArray();
            }

            File.WriteAllBytes(configuration_file_path, cfg_bytes);
        }

        public static string ProgramVersion
        {
            get
            {
                return program_version;
            }
        }

        public static string[] ListPaths
        {
            get
            {
                return list_paths.ToArray();
            }

            set
            {
                list_paths = new List<string>(value);
                WriteConfigurationToFile();
            }
        }

        public static bool[] ListPathCheckStates
        {
            get
            {
                return list_check_states.ToArray();
            }

            set
            {
                list_check_states = new List<bool>(value);
                WriteConfigurationToFile();
            }
        }

        public static int ConnectionType
        {
            get
            {
                return connection_type;
            }

            set
            {
                connection_type = value;
                WriteConfigurationToFile();
            }
        }

        public static async Task<int> GetProtocolVersion()
        {
            return await Task.Run(new Func<Task<int>>(async () =>
            {
            if (connection_type == 1)
            {
                if (selected_tcp_connection != null)
                    return await selected_tcp_connection.ProtocolVersion;
            }
            else
                if (connection_type == 0)
                    if (selected_com_port != null)
                        return await ProtocolDetector.DetectProtocolVersion(selected_com_port);

            throw new Exception("Unable to retreive protocol version. Connection is null.");
            }));
        }

        public static string SelectedCOMPort
        {
            get
            {
                return selected_com_port;
            }

            set
            {
                selected_com_port = value;
                WriteConfigurationToFile();
            }
        }

        public static TCPConnectionProperties SelectedTCPConnection
        {
            get
            {
                return selected_tcp_connection;
            }

            set
            {
                selected_tcp_connection = value;
                WriteConfigurationToFile();
            }
        }

        public static TCPConnectionProperties[] TCPConnectionHistory
        {
            get
            {
                return tcp_connection_history.ToArray();
            }

            set
            {
                tcp_connection_history = new List<TCPConnectionProperties>(value);
                WriteConfigurationToFile();
            }
        }

        public static void ClearTCPConnectionHistory()
        {
            tcp_connection_history.Clear();
            WriteConfigurationToFile();
        }

        public static void AddTCPConnectionHistoryItem(TCPConnectionProperties tcpconnprop)
        {
            tcp_connection_history.Add(tcpconnprop);
            WriteConfigurationToFile();
        }

        public static List<string> SessionTempFiles
        {
            get
            {
                return session_temp_files;
            }
        }

        public static bool RetainTemporaryFiles
        {
            get
            {
                return retain_temp_files;
            }

            set
            {
                retain_temp_files = value;
                WriteConfigurationToFile();
            }
        }

        public static void EraseTemporaryFiles()
        {
            foreach (string filename in session_temp_files)
                File.Delete(filename);

            session_temp_files.Clear();
        }

        private static void FileCheck(string path)
        {
            FileCheck(new string[] { path});
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

        public static async Task<string[]> ReadAllLogLines()
        {
            return await Task.Run(() =>
            {
                lock(log_lock)
                {
                    if (File.Exists(log_path))
                    {
                        FileCheck(log_path);
                        return File.ReadAllLines(log_path);
                    }
                    else
                        return new string[0];
                }
            });
        }

        public static void AppendLog(DateTime timestamp, string line)
        {
            pending_log_entries.Enqueue(new KeyValuePair<DateTime, string>(timestamp, line));

            Task.Run(() =>
            {
                Stopwatch sw = Stopwatch.StartNew();

                if (pending_log_entries.Count > 0)
                {
                    List<KeyValuePair<DateTime, string>> pending_entries_dump = new List<KeyValuePair<DateTime, string>>();

                    lock (queue_lock)
                    {
                        KeyValuePair<DateTime, string> ret;
                        while (pending_log_entries.TryDequeue(out ret))
                            pending_entries_dump.Add(ret);


                        lock (log_lock)
                        {
                            if (File.Exists(log_path))
                            {
                                FileCheck(log_path);

                                string[] log_lines = File.ReadAllLines(log_path);

                                if (log_lines.Length >= max_log_entries)
                                using (StreamWriter line_writer = new StreamWriter(File.OpenWrite(log_path)))
                                 for (int i = pending_entries_dump.Count; i < log_lines.Length; i++)
                                   line_writer.WriteLine(log_lines[i]);

                                log_lines = null;
                            }
                            else
                                File.Create(log_path).Close();

                            File.AppendAllLines(log_path, pending_entries_dump.Select(x => "[" + x.Key.ToString() + "] " + x.Value));

                            pending_entries_dump.Clear();
                            pending_entries_dump = null;
                        }
                    }
                }

                sw.Stop();

                Console.WriteLine(sw.ElapsedMilliseconds);
            });
        }

        public static string LogPath
        {
            get
            {
                return new FileInfo(log_path).FullName;
            }
            set
            {
                lock(log_lock)
                {
                    if (File.Exists(log_path))
                    {
                        FileCheck(log_path);

                        string[] log_lines = File.ReadAllLines(log_path);
                        File.Delete(log_path);
                        if (log_lines.Length > 0)
                            File.WriteAllLines(value, log_lines);
                    }

                    log_path = value;
                }

                WriteConfigurationToFile();
            }
        }

        public static byte[] InstanceID
        {
            get
            {
                return instance_id.ToByteArray();
            }
        }
    }
}
