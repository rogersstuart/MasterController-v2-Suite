﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //ServiceBase[] ServicesToRun;
            //ServicesToRun = new ServiceBase[]
            //{  
                    new DeviceServer();
            Thread.Sleep(-1);
            //};
            //ServiceBase.Run(ServicesToRun);
        }
    }
}
