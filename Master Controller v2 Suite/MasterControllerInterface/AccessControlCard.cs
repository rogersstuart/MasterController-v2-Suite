using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerInterface
{
    [Serializable]
    public class AccessControlCard
    {
        public const int MIFARE_CLASSIC = 0;
        
        private UInt64 uid;
        private int type;
        private DateTime timestamp;

        public AccessControlCard(AccessControlCard card): this(card.UID, card.Type, card.Timestamp){}

        public AccessControlCard(UInt64 uid, int type, DateTime timestamp)
        {
            this.uid = uid;
            this.type = type;
            this.timestamp = timestamp;
        }

        public UInt64 UID
        {
            get
            {
                return uid;
            }
        }

        public int Type
        {
            get
            {
                return type;
            }
        }

        public DateTime Timestamp
        {
            get
            {
                return timestamp;
            }
        }

        public override string ToString()
        {
            return uid + "";
        }


    }
}
