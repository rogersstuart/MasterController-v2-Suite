using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public class BackupProperties
    {
        public DateTime Timestamp { get; set; }
        public DatabaseConnectionProperties Database { get; set; }
    }
}
