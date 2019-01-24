using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public class ExpanderEventArgs : EventArgs
    {
        ExpanderState expst;
        public ExpanderEventArgs(ExpanderState expst)
        {
            this.expst = expst;
        }

        public ExpanderState ExpanderState
        {
            get { return expst; }
        }
    }
}
