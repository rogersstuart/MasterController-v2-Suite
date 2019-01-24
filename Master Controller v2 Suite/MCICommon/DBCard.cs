using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public class DBCard
    {
        private ulong nuid, user;

        public DBCard(ulong nuid, ulong user)
        {
            this.nuid = nuid;
            this.user = user;
        }

        public ulong CardNUID
        { get { return nuid; } }

        public ulong UserAssignment
        { get { return user; } }
    }
}
