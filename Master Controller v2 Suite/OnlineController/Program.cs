using MCICommon;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace OnlineController
{
    class Program
    {

        static void Main(string[] args)
        {
            new OnlineController();

            Thread.Sleep(-1);
        }

    }
}
