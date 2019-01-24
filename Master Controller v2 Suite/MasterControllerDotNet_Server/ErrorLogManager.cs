using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace MasterControllerDotNet_Server
{
    internal static class ErrorLogManager
    {
        private static Object log_access_lock = new Object();

        public static void ClearLog()
        {
            if(File.Exists("log.txt"))
                lock(log_access_lock)
                    File.Delete("log.txt");
        }

        public static void AppendLog(string to_append, bool append_timestamp)
        {
            lock (log_access_lock)
                File.AppendAllText("log.txt", (append_timestamp ? DateTime.Now.ToString() + " " : "") + to_append + Environment.NewLine);
        }

        public static string[] ReadLog()
        {
            if (File.Exists("log.txt"))
            {
                lock (log_access_lock)
                    return File.ReadAllLines("log.txt");
            }
            else
                return new string[]{""};
        }
    }
}
