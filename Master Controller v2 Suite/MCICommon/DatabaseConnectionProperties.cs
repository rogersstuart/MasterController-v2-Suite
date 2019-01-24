using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MCICommon
{
    [Serializable]
    //[DataContract]
    public class DatabaseConnectionProperties
    {
        private string hostname;
        private string schema = null;
        private string uid;
        private string password;

        public DatabaseConnectionProperties(DatabaseConnectionProperties dbconprop) : this(dbconprop.Hostname, dbconprop.Schema, dbconprop.UID, dbconprop.Password) { }

        public DatabaseConnectionProperties(string hostname, string schema, string uid, string password)
        {
            Hostname = hostname.Trim() ;
            Schema = schema.Trim();
            UID = uid.Trim();
            Password = password.Trim();
        }

        public DatabaseConnectionProperties() { }

        [DataMember]
        public string UID
        {
            get
            {
                return string.Copy(uid);
            }

            set
            {
                uid = string.Copy(value);
            }
        }

        [DataMember]
        public string Hostname
        {
            get
            {
                return string.Copy(hostname);
            }

            set
            {
                hostname = string.Copy(value);
            }
        }

        [DataMember]
        public string Schema
        {
            get
            {
                return string.Copy(schema);
            }

            set
            {
                schema = string.Copy(value);
            }
        }

        [DataMember]
        public string Password
        {
            get
            {
                return string.Copy(password);
            }

            set
            {
                password = string.Copy(value);
            }
        }

        [JsonIgnore]
        public string ConnectionString
        {
            get
            {
                if(schema != null && schema.Trim() != "")
                    return "Server=" + Dns.GetHostAddresses(hostname)[0].ToString() + ";Uid=" + uid + ";Pwd=" + password + ";Database=" + schema + "; Allow User Variables=True;";
                else
                    return "Server=" + Dns.GetHostAddresses(hostname)[0].ToString() + ";Uid=" + uid + ";Pwd=" + password + ";Database=" + "accesscontrol" + "; Allow User Variables=True;";
            }
        }

        public string[] ToArray()
        {
            return new string[] { hostname, schema, uid, password};
        }

        public static DatabaseConnectionProperties FromArray(string[] array)
        {
            return new DatabaseConnectionProperties(array[0], array[1], array[2], array[3]);
        }

        public override string ToString()
        {
            return hostname + "; " + schema;
        }
    }
}
