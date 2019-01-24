using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MasterControllerDotNet_Server
{
    public class AccessControlGroupMonitor
    {
        List<AccessControlGroup> accgrps;
        Thread monitor_thread;
        volatile bool monitor_active;

        public AccessControlGroupMonitor(List<AccessControlGroup> accgrps)
        {
            this.accgrps = accgrps;
        }

        public void Start()
        {
            monitor_active = true;
            monitor_thread = GenerateMonitorThread();
            monitor_thread.Start();
        }

        public void Stop()
        {
            if (monitor_thread != null)
            {
                monitor_active = false;
                while (monitor_thread.IsAlive) ;
                monitor_thread = null;
            }
        }

        private Thread GenerateMonitorThread()
        {
            return new Thread(delegate()
                {
                    while(monitor_active)
                    {
                        foreach (AccessControlGroup accgrp in accgrps)
                        {
                            accgrp.Process();
                            Thread.Sleep(1000/60);
                        }
                    }
                });
        }
    }
}
