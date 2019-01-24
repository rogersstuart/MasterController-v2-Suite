using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerDotNet_Server
{
    [Serializable]
    public class PanelState : MarshalByRefObject
    {
        private AccessControlCard card;
        private UInt64 uptime;
        private DateTime timestamp;

        //panels need and id. you need to implement that.

        public PanelState(PanelState state) : this(state.Card, state.Uptime, state.Timestamp){}

        public PanelState(AccessControlCard card, UInt64 uptime, DateTime timestamp)
        {
            if(card != null)
                this.card = new AccessControlCard(card);


            this.uptime = uptime;
            this.timestamp = timestamp;
        }

        public AccessControlCard Card
        {
            get
            {
                return new AccessControlCard(card);
            }
        }

        public UInt64 Uptime
        {
            get
            {
                return uptime;
            }
        }

        public DateTime Timestamp
        {
            get
            {
                return timestamp;
            }
        }
    }
}
