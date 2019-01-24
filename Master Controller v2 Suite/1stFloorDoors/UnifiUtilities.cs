using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.Threading;

namespace DoorControl
{
    public static class UnifiUtilities
    {
        private static string door_controller_mac = "18:fe:34:98:cb:54";

        private static CookieContainer Login()
        {
            var ubt_cookies = new CookieContainer();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mccsrv0:8443/api/login");
            request.ContentType = "application/json";
            request.Method = "POST";
            request.CookieContainer = ubt_cookies;

            byte[] buffer = Encoding.GetEncoding("UTF-8").GetBytes("{\"username\": \"criladmin\", \"password\": \"5700FloridaBlvd\"}");
            string result = Convert.ToBase64String(buffer);
            Stream reqstr = request.GetRequestStream();
            reqstr.Write(buffer, 0, buffer.Length);
            reqstr.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            response.Dispose();

            return ubt_cookies;
        }

        public static bool IsDoorControllerConnected()
        {
            try
            {
                var cookies = Login();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mccsrv0:8443/api/s/default/stat/sta/" + door_controller_mac.Replace(":", ""));
                request.ContentType = "application/json";
                request.Method = "POST";
                request.CookieContainer = cookies;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                response.Dispose();
            }
            catch(Exception ex)
            {
                return false;
            }

            return true;
        }

        public static void ReconnectDoorController()
        {
            var cookies = Login();

            JavaScriptSerializer jsonSer = new JavaScriptSerializer();
            Dictionary<String, object> jsonParams = new Dictionary<string, object>();

            jsonParams.Add("cmd", "kick-sta");
            jsonParams.Add("mac", "18:fe:34:98:cb:54");

            byte[] byteArray = Encoding.UTF8.GetBytes("json=" + jsonSer.Serialize(jsonParams));

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://mccsrv0:8443/api/s/default/cmd/stamgr");
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            request.CookieContainer = cookies;

            request.GetRequestStream().Write(byteArray, 0, byteArray.Length);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            response.Dispose();
        }
    }
}
