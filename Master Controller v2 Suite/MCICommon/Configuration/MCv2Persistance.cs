using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlobalUtilities;
using static MCICommon.DatabaseConfiguration;

namespace MCICommon
{
    public sealed class MCv2Persistance : PersistanceBase<MCv2Configuration>
    {
        private static MCv2Persistance instance = new MCv2Persistance();

        private MCv2Persistance() : base(System.AppDomain.CurrentDomain.BaseDirectory + "/mci_config.json") { }

        public static MCv2Persistance Instance
        {
            get { return instance; }
        }
    }
}
