using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerDotNet_Server
{
    public class PanelEventArgs : EventArgs
    {
        PanelState pnlst;

        public PanelEventArgs(PanelState pnlst)
        {
            this.pnlst = pnlst;
        }

        public PanelState PanelState
        {
            get { return pnlst; }
        }
    }
}
