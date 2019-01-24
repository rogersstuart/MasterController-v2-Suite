using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    [Serializable]
    public class AccessControlGroup
    {
        string group_name;

        string id_table;
        ExpanderMonitor expander_monitor;
        List<PanelMonitor> panel_monitors = new List<PanelMonitor>();

        public AccessControlGroup(string grpnm, string tblnm, ExpanderMonitor expmon, List<PanelMonitor> pnlmons)
        {
            group_name = grpnm;
            id_table = tblnm;
            expander_monitor = expmon;
            panel_monitors = pnlmons;
        }

        public void Process()
        {

        }

        public string GroupName
        {
            get
            {
                return group_name;
            }
        }

        public string TableName
        {
            get
            {
                return id_table;
            }
        }

        public ExpanderMonitor ExpanderMonitor
        {
            get
            {
                return expander_monitor;
            }
        }

        public List<PanelMonitor> PanelMonitors
        {
            get
            {
                return panel_monitors;
            }
        }

        public override string ToString()
        {
            return group_name + " " + id_table + " " + expander_monitor.ToString() + " " + panel_monitors.ToString();
        }
    }
}
