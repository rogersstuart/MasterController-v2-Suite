using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MCICommon
{
    public class ControllerCommandHandle
    {
        private AutoResetEvent handle;
        private ControllerCommand command;
        public ControllerCommandHandle(AutoResetEvent handle, ControllerCommand command)
        {
            this.handle = handle;
            this.command = command;
        }

        public ControllerCommand Command
        {
            get
            {
                return command;
            }

            set
            {
                command = value;
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
