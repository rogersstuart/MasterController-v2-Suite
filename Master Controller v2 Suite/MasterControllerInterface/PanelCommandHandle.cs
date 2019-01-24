using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MasterControllerInterface
{
    class PanelCommandHandle
    {
        private AutoResetEvent handle;
        private PanelCommand command;

        public PanelCommandHandle(AutoResetEvent handle, PanelCommand command)
        {
            this.handle = handle;
            this.command = command;
        }

        public PanelCommand Command
        {
            get
            {
                return command;
            }
        }

        public AutoResetEvent Handle
        {
            get
            {
                return handle;
            }
        }
    }
}
