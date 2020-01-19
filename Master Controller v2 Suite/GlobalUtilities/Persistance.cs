using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace GlobalUtilities
{
    public class PersistanceBase<T> where T : new()
    {   
        private string persistance_file = System.AppDomain.CurrentDomain.BaseDirectory + "/config.json";
        private bool needs_init = true;
        private string persistance_serialization = JsonConvert.SerializeObject(new T());
        private Object config_lock = new Object();

        public PersistanceBase(string persistance_file)
        {
            this.persistance_file = persistance_file;
        }

        private void RestoreFromFile()
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

        public Task<T> GetConfiguration()
        {
            return Task.Run(() =>
            {
                lock (config_lock)
                {
                    if (needs_init)
                    {
                        try
                        {
                            RestoreFromFile();

                            needs_init = false;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("An error occured while restoring from the persistance file.");
                        }
                    }

                    return JsonConvert.DeserializeObject<T>(persistance_serialization);
                }
            }); 
        }

        public Task SetConfiguration(T config)
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

        public T Config
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

        public string PersistanceFile
        {
            get { return persistance_file; }
        }
    }
}
