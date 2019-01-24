using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public class BuildingFloor
    {
        private Object access_lock = new Object();

        public int floor_number { get; }
        public string description { get; }
        public bool last_reported_state = false;

        public List<FloorStateModifier> modifiers = new List<FloorStateModifier>();

        public Func<DateTime, bool> system_modifier_activation_condition = default_activation_condition;
        public Func<DateTime, bool> system_modifier_result = default_modifier_result;

        public Func<DateTime, bool> arbitration_function = default_arbitration_function;

        public static readonly Func<DateTime, bool> default_arbitration_function = new Func<DateTime, bool>((time) => { return true; });
        public static readonly Func<DateTime, bool> default_activation_condition = new Func<DateTime, bool>((time) => { return true; });
        public static readonly Func<DateTime, bool> default_modifier_result = new Func<DateTime, bool>((time) => { return true; });

        public BuildingFloor(int floor_number)
        {
            this.floor_number = floor_number;
        }
    }
}
