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
            // Add global exception handlers for better debugging
            Application.ThreadException += (sender, e) =>
            {
                var ex = e.Exception;
                MessageBox.Show($"Thread Exception:\n{ex.GetType().Name}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                               "Unhandled Thread Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show($"Unhandled Exception:\n{ex?.GetType().Name}: {ex?.Message}\n\nStack Trace:\n{ex?.StackTrace}", 
                               "Unhandled Domain Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            while (true)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                try
                {
                    ConfigurationManager.ReadConfigurationFromFile();
                }
                catch (Exception configEx)
                {
                    MessageBox.Show($"Configuration Load Error:\n{configEx.GetType().Name}: {configEx.Message}\n\nStack Trace:\n{configEx.StackTrace}", 
                                   "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                try
                {
                    var config = MCv2Persistance.Instance.Config;
                    
                    // Ensure DatabaseConnectionProperties is properly initialized
                    if (config.DatabaseConfiguration?.DatabaseConnectionProperties == null)
                    {
                        config.DatabaseConfiguration = new DatabaseConfiguration();
                        config.DatabaseConfiguration.DatabaseConnectionProperties = new DatabaseConnectionProperties("", "", "", "");
                        MCv2Persistance.Instance.Config = config;
                    }

                    (new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "backups\\")).Directory.Create();

                    //needs to test credentials
                    if (config.AutoLogin)
                    {
                        // Try to access properties safely
                        string hostname = "", password = "", schema = "", uid = "";
                        try
                        {
                            hostname = config.DatabaseConfiguration.DatabaseConnectionProperties.Hostname ?? "";
                            password = config.DatabaseConfiguration.DatabaseConnectionProperties.Password ?? "";
                            schema = config.DatabaseConfiguration.DatabaseConnectionProperties.Schema ?? "";
                            uid = config.DatabaseConfiguration.DatabaseConnectionProperties.UID ?? "";
                        }
                        catch
                        {
                            // If accessing properties fails, force re-initialization
                            config.DatabaseConfiguration.DatabaseConnectionProperties = new DatabaseConnectionProperties("", "", "", "");
                        }
                        
                        if(!(hostname.Trim() != "" &&
                            password.Trim() != "" &&
                            schema.Trim() != "" &&
                            uid.Trim() != ""))
                        {
                            DatabaseConnectionEditor dbconneditor = new DatabaseConnectionEditor(config.DatabaseConfiguration.DatabaseConnectionProperties, false);
                            var res = dbconneditor.ShowDialog();
                            if (res == DialogResult.OK)
                            {
                                config.DatabaseConfiguration.DatabaseConnectionProperties = dbconneditor.DBConnProp;
                                MCv2Persistance.Instance.Config = config;
                            }
                            else if (res == DialogResult.Abort)
                                Environment.Exit(0);
                        }
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

                    DatabaseConnectionEditor dbconneditor = new DatabaseConnectionEditor(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties, false);
                    if (dbconneditor.ShowDialog() == DialogResult.OK)
                    {
                        MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties = dbconneditor.DBConnProp;
                        
                        // Force ARDBConnectionManager to restart with new settings
                        try
                        {
                            ARDBConnectionManager.default_manager.Stop();
                        }
                        catch { }
                    }
                    else
                        break;
                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    MessageBox.Show("Database connection error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    DatabaseConnectionEditor dbconneditor = new DatabaseConnectionEditor(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties, false);
                    if (dbconneditor.ShowDialog() == DialogResult.OK)
                    {
                        MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties = dbconneditor.DBConnProp;
                        
                        // Force ARDBConnectionManager to restart with new settings
                        try
                        {
                            ARDBConnectionManager.default_manager.Stop();
                        }
                        catch { }
                    }
                    else
                        break;
                }
                catch (System.Collections.Generic.KeyNotFoundException ex)
                {
                    MessageBox.Show("Database configuration error: The database character encoding is not supported by the MySQL driver.\n\n" +
                                   "Please verify your database configuration and character set settings.", 
                                   "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    DatabaseConnectionEditor dbconneditor = new DatabaseConnectionEditor(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties, false);
                    if (dbconneditor.ShowDialog() == DialogResult.OK)
                    {
                        MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties = dbconneditor.DBConnProp;
                        
                        // Force ARDBConnectionManager to restart with new settings
                        try
                        {
                            ARDBConnectionManager.default_manager.Stop();
                        }
                        catch { }
                    }
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
