using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace MasterControllerDotNet_Server
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //try
            //{
                /*
                TcpServerChannel channel = new TcpServerChannel(9000);
                ChannelServices.RegisterChannel(channel, true);
                WellKnownServiceTypeEntry remObj = new WellKnownServiceTypeEntry(typeof(SharedServerProperties), "SharedServerProperties", WellKnownObjectMode.Singleton);
                RemotingConfiguration.RegisterWellKnownServiceType(remObj);
                 * */

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            //}
            //catch(Exception ex)
            //{
            //    ErrorLogManager.AppendLog("Fatal Error Occured", true);
            //    ErrorLogManager.AppendLog(ex.ToString(), false);
           // }
        }
    }
}
