using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public class FloorStateModifier
    {
        //true results in an expiration
        public Func<DateTime, bool> modifier_expiration_condition;

        //true results in an activation
        public Func<DateTime, bool> modifier_activation_condition;

        //voting
        public Func<DateTime, bool> modifier_result;

        public int precidence = 0;

        public FloorStateModifier(
            int precidence,
            Func<DateTime, bool> modifier_activation_condition,
            Func<DateTime, bool> modifier_expiration_condition,
            Func<DateTime, bool> modifier_result)
        {
            this.precidence = precidence;
            this.modifier_activation_condition = modifier_activation_condition;
            this.modifier_expiration_condition = modifier_expiration_condition;
            this.modifier_result = modifier_result;
        }
    }
}
