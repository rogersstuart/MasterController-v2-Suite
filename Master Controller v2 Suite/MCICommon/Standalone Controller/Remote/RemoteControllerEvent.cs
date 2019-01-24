using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    [Serializable]
    public class RemoteControllerEvent
    {
        DateTime event_datetime;

        bool[] exp0_mask, exp1_mask;
        bool[] exp0_vals, exp1_vals;

        bool triggered = false;

        public RemoteExpanderEvent(DateTime event_datetime, bool[] exp0_mask, bool[] exp1_mask, bool[] exp0_vals, bool[] exp1_vals)
        {
            this.event_datetime = event_datetime;
            this.exp0_mask = exp0_mask;
            this.exp1_mask = exp1_mask;
            this.exp0_vals = exp0_vals;
            this.exp1_vals = exp1_vals;
        }

        public bool IsActive
        {
            get
            {
                return !triggered;
            }
        }

        public async Task Process(RemoteExpanderMonitor expmon)
        {
            if (DateTime.Now >= event_datetime)
            {
                //apply values to base state, set that as the current base state, and write
                ExpanderState base_state = expmon.DefaultState;

                for (int index_counter = 0; index_counter < 16; index_counter++)
                {
                    base_state.Expander0State[index_counter] &= !exp0_mask[index_counter];
                    base_state.Expander1State[index_counter] &= !exp1_mask[index_counter];

                    base_state.Expander0State[index_counter] |= (exp0_mask[index_counter] & exp0_vals[index_counter]);
                    base_state.Expander1State[index_counter] |= (exp1_mask[index_counter] & exp1_vals[index_counter]);
                }

                expmon.DefaultState = base_state;

                expmon.WriteExpanders(exp0_mask, exp1_mask, exp0_vals, exp1_vals);

                triggered = true;
            }
        }

        public override string ToString()
        {
            return event_datetime.ToString();
        }
    }
}
