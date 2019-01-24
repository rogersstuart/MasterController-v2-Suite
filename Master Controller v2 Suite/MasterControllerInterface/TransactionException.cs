using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerInterface
{
    public class TransactionException : Exception
    {
        private int major_version, minor_version;

        public TransactionException() : base()
        {
        }

        public TransactionException(string message) : base(message)
        {
        }

        public TransactionException(string message, Exception inner) : base(message, inner)
        {
        }

        public TransactionException(int major_version, int minor_version) : base()
        {
            this.major_version = major_version;
            this.minor_version = minor_version;
        }

        public TransactionException(int major_version, int minor_version, string message, Exception inner) : base(message, inner)
        {
            this.major_version = major_version;
            this.minor_version = minor_version;
        }

        public int MajorProtocolVersion
        {
            get { return major_version; }
        }

        public int MinorProtocolVersion
        {
            get { return minor_version; }
        }
    }
}
