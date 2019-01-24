using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceServer
{
    public partial class DeviceServer : ServiceBase
    {
        public DeviceServer()
        {
            InitializeComponent();

            var version_strings = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
            var version_string = "DeviceServer v" + version_strings[0] + "." + version_strings[1] + " Prerelease " + version_strings[2] + "." + version_strings[3];

            ServiceLog.AppendLog(DateTime.Now, version_string);
            ServiceLog.AppendLog(DateTime.Now, "Service - Starting");

            DeviceConnectionManager.Start();
            ServerAdvertiser.Start();
            ServerListener.Start();

            ServiceLog.AppendLog(DateTime.Now, "Service - Started");
        }

        protected override void OnStart(string[] args)
        {
            var version_strings = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
            var version_string = "DeviceServer v" + version_strings[0] + "." + version_strings[1] + " Prerelease " + version_strings[2] + "." + version_strings[3];

            ServiceLog.AppendLog(DateTime.Now, version_string);
            ServiceLog.AppendLog(DateTime.Now, "Service - Starting");

            DeviceConnectionManager.Start();
            ServerAdvertiser.Start();
            ServerListener.Start();

            ServiceLog.AppendLog(DateTime.Now, "Service - Started");
        }

        protected override void OnStop()
        {
            Task.Run(() =>
            { 
                var version_strings = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.');
                var version_string = "DeviceServer v" + version_strings[0] + "." + version_strings[1] + " Prerelease " + version_strings[2] + "." + version_strings[3];

                ServiceLog.AppendLog(DateTime.Now, version_string);
                ServiceLog.AppendLog(DateTime.Now, "Service - Stopping");

                var handle = ServerListener.Stop();
                handle.WaitOne();

                handle = ServerAdvertiser.Stop();
                handle.WaitOne();

                handle = DeviceConnectionManager.Stop();
                handle.WaitOne();

                ServiceLog.AppendLog(DateTime.Now, "Service - Stopped");
            });
        }
    }
}
