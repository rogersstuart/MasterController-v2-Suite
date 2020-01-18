using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;
using MCICommon;
using UIElements;

namespace MasterControllerInterface
{
    public class Program
    {
        //private static DBConnectionManager dbconnman = new DBConnectionManager();
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            while (true)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                //MCIv2Persistance.RestoreFromFile();

                ConfigurationManager.ReadConfigurationFromFile();

                //PeerSyncListener.Start();
                //PeerSyncAdvertiser.Start();
                //PeerSyncDiscovery.Start();

                //Application.ThreadExit += (e, b) => { ApplicationClosing(); };

                try
                {
                    var config = MCv2Persistance.Config;

                    (new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "backups\\")).Directory.Create();

                    //needs to test credentials
                    if (config.AutoLogin)
                        if(!(config.DatabaseConfiguration.DatabaseConnectionProperties.Hostname.Trim() != "" &&
                            config.DatabaseConfiguration.DatabaseConnectionProperties.Password.Trim() != "" &&
                            config.DatabaseConfiguration.DatabaseConnectionProperties.Schema.Trim() != "" &&
                            config.DatabaseConfiguration.DatabaseConnectionProperties.UID.Trim() != ""))
                        {
                            DatabaseConnectionEditor dbconneditor = new DatabaseConnectionEditor(config.DatabaseConfiguration.DatabaseConnectionProperties, false);
                            var res = dbconneditor.ShowDialog();
                            if (res == DialogResult.OK)
                            {
                                config.DatabaseConfiguration.DatabaseConnectionProperties = dbconneditor.DBConnProp;

                                MCv2Persistance.Config = config;

                                //MCv2Persistance.SaveToFile();
                            }
                            else
                                if (res == DialogResult.Abort)
                                Environment.Exit(0);
                        }

                    ProgressDialog pgd = new ProgressDialog("");
                    ARDBConnectionManager.default_progress_interface = pgd;
                    pgd.Show();
                    ARDBConnectionManager.default_manager.Start();

                    DeviceServerTracker.Stop();
                    DeviceServerTracker.Start();

                    Application.Run(new MCIv2Form());

                    DeviceServerTracker.Stop();

                    ARDBConnectionManager.default_manager.Stop();
                    pgd.Dispose();

                    break;
                }
                catch (System.Reflection.TargetInvocationException ex)
                {
                    MessageBox.Show("A database error has occured." + Environment.NewLine + ex.Message);

                    DatabaseConnectionEditor dbconneditor = new DatabaseConnectionEditor(MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties, false);
                    if (dbconneditor.ShowDialog() == DialogResult.OK)
                        MCv2Persistance.Config.DatabaseConfiguration.DatabaseConnectionProperties = dbconneditor.DBConnProp;
                    else
                        break;
                }
                catch (Exception ex)
                {
                    new ErrorBox(ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : ""), ex.ToString()).ShowDialog();
                    break;
                }
            }

            //MCIv2Persistance.SaveToFile();

            //PeerSyncDiscovery.Stop();
            //PeerSyncAdvertiser.Stop();
            //PeerSyncListener.Stop();
        }

        static void ApplicationClosing()
        {
           

            
        }
    }
}
