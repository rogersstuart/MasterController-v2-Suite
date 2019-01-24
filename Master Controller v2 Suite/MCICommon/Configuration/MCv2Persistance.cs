using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
//using System.Runtime.Serialization;
using Newtonsoft.Json;

using static MCICommon.DatabaseConfiguration;

namespace MCICommon
{
    public static class MCv2Persistance
    {
        public static readonly string persistance_file = System.AppDomain.CurrentDomain.BaseDirectory + "/mci_config.xml";
       // private static DataContractSerializer xmlSerializer = new DataContractSerializer(typeof(MCv2Configuration));

        private static bool needs_init = true;

        private static string persistance_serialization = JsonConvert.SerializeObject(new MCv2Configuration());

        private static Object config_lock = new Object();

        private static void RestoreFromFile()
        {
            if (!File.Exists(persistance_file))
                return;

            bool error_flag = false;

            try
            {
                var persistance_serialization_holding = File.ReadAllText(persistance_file);

                JsonConvert.DeserializeObject(persistance_serialization_holding); //test

                persistance_serialization = persistance_serialization_holding;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while restoring from the persistance file.");

                error_flag = true;      
            }

            if(error_flag)
            {
                try
                {
                    File.Delete(persistance_file);
                }
                catch(Exception ex){}
            }
        }

        public static Task<MCv2Configuration> GetConfiguration()
        {
            return Task.Run(() =>
            {
                lock (config_lock)
                {
                    if (needs_init)
                    {
                        try
                        {
                            //persistance_serialization = File.ReadAllBytes(persistance_file);

                            RestoreFromFile();

                            needs_init = false;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("An error occured while restoring from the persistance file.");
                        }
                    }

                    return JsonConvert.DeserializeObject<MCv2Configuration>(persistance_serialization);
                }
            });
            
        }

        public static Task SetConfiguration(MCv2Configuration config)
        {
            return Task.Run(() =>
            {
                lock (config_lock)
                {
                    persistance_serialization = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);

                    //save to file // todo: add a timer
                    Task.Run(() =>
                    {
                        try
                        {
                            File.WriteAllText(persistance_file, persistance_serialization);
                        }
                        catch (Exception ex) { }
                    });                    
                }
            });
            
        }

        public static MCv2Configuration Config
        {
            get
            {
                var t = GetConfiguration();
                t.Wait();
                return t.Result;
            }

            set
            {
                var t = SetConfiguration(value);
                t.Wait();
            }
        }
    }
}
