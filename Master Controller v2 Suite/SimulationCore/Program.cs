using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace SimulationCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new OfflineControllerServer(5000); // Choose a port
            server.Start();
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
            server.Stop();
        }
    }
}
