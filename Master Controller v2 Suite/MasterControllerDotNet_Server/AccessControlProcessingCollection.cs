using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerDotNet_Server
{
    public class AccessControlProcessingCollection
    {
        PanelMonitor pm;
        PanelState ps;
        AccessProperties ap;

        public AccessControlProcessingCollection()
        {

        }

        public PanelMonitor Monitor
        {
            get
            {
                return pm;
            }

            set
            {
                pm = value;
            }
        }

        public PanelState State
        {
            get
            {
                return ps;
            }

            set
            {
                ps = value;
            }
        }

        public AccessProperties Properties
        {
            get
            {
                return ap;
            }

            set
            {
                ap = value;
            }
        }
    }
}
