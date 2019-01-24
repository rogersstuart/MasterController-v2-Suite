using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MCICommon;

namespace MasterControllerInterface
{
    class TCPConnectionHistoryMenuItem : ToolStripMenuItem
    {
        private TCPConnectionProperties tcpconnprop;

        public TCPConnectionHistoryMenuItem(TCPConnectionProperties tcpconnprop)
        {
            this.tcpconnprop = tcpconnprop;
            this.Text = tcpconnprop.ToString();
        }

        public TCPConnectionProperties TCPConnectionProperties
        {
            get
            {
                return tcpconnprop;
            }
        }
    }
}
