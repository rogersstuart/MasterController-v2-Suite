using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MCICommon
{
    public class ExpanderCommandHandle
    {
        private AutoResetEvent handle;
        private ExpanderCommand command;
        public ExpanderCommandHandle(AutoResetEvent handle, ExpanderCommand command)
        {
            this.handle = handle;
            this.command = command;
        }

        public ExpanderCommand Command
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
