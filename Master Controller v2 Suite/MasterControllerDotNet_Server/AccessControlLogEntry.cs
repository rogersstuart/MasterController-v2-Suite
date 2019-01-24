using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerDotNet_Server
{
    [Serializable]
    public class AccessControlLogEntry
    {
        private DateTime timestamp;
        private string description;
        private AccessControlCard accessctlcard;
        string expander;
        string panel;

        public AccessControlLogEntry(DateTime timestamp, string description, AccessControlCard accessctlcard, string expander, string panel)
        {
            this.timestamp = timestamp;
            this.description = description;
            this.accessctlcard = new AccessControlCard(accessctlcard);
            this.expander = expander;
            this.panel = panel;
        }

        public DateTime Timestamp
        {
            get { return timestamp; }
        }

        public string Description
        {
            get { return description; }
        }

        public AccessControlCard Card
        {
            get { return new AccessControlCard(accessctlcard); }
        }

        public string ExpanderInfo
        {
            get { return expander; }
        }

        public string PanelInfo
        {
            get { return panel; }
        }
    }
}
