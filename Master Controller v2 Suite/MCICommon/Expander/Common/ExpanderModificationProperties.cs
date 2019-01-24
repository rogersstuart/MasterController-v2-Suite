using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public class ExpanderModificationProperties
    {
        bool[] exp_0_mask;
        bool[] exp_1_mask;
        bool[] exp_0_values;
        bool[] exp_1_values;

        DateTime revert_at;

        int stack_priority = 0;

        int associated_car = 0;

        public ExpanderModificationProperties(bool[] exp_0_mask, bool[] exp_1_mask, bool[] exp_0_values, bool[] exp_1_values, DateTime revert_at, int stack_priority)
        {
            this.exp_0_mask = exp_0_mask;
            this.exp_1_mask = exp_1_mask;
            this.exp_0_values = exp_0_values;
            this.exp_1_values = exp_1_values;
            this.revert_at = revert_at;
            this.stack_priority = stack_priority;
        }

        public int StackPriority
        {
            get { return stack_priority; }
        }

        public bool Evaluate()
        {
            if(DateTime.Now > revert_at)
                return false;
            else
                return true;
        }

        public bool[] Exp0Mask
        {
            get { return exp_0_mask; }
        }

        public bool[] Exp1Mask
        {
            get { return exp_1_mask; }
        }

        public bool[] Exp0Values
        {
            get { return exp_0_values; }
        }

        public bool[] Exp1Values
        {
            get { return exp_1_values; }
        }

        public int AssociatedCar
        {
            get { return associated_car; }
            set { associated_car = value; }
        }
    }
}
