using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerDotNet_Server
{
    [Serializable]
    public class ActivationProperties
    {
        DateTime range_start;
        DateTime range_end;

        bool[] exp_0_mask, exp_1_mask, exp_0_values, exp_1_values;

        //year, month, day of month, day of week, hour, minute, second
        bool[] comparison_flags;

        int revert_after;

        public ActivationProperties(DateTime range_start, DateTime range_end, bool[] exp_0_mask, bool[] exp_1_mask, bool[] exp_0_values, bool[] exp_1_values, bool[] comparison_flags, int revert_after)
        {
            this.range_start = range_start;
            this.range_end = range_end;
            this.exp_0_mask = exp_0_mask;
            this.exp_1_mask = exp_1_mask;
            this.exp_0_values = exp_0_values;
            this.exp_1_values = exp_1_values;
            this.comparison_flags = comparison_flags;
            this.revert_after = revert_after;
        }

        public bool Evaluate()
        {
            int num_comparisons = 0;
            int comparison_counter = 0;

            foreach (bool b in comparison_flags)
                if (b)
                    num_comparisons++;

            //year
            if (comparison_flags[0])
                if (range_start.Year <= DateTime.Now.Year)
                    if (DateTime.Now.Year <= range_end.Year)
                        comparison_counter++;

            //month
            if (comparison_flags[1])
                if (range_start.Month <= DateTime.Now.Month)
                    if (DateTime.Now.Month <= range_end.Month)
                        comparison_counter++;

            //day of month
            if (comparison_flags[2])
                if (range_start.Day <= DateTime.Now.Day)
                    if (DateTime.Now.Day <= range_end.Day)
                        comparison_counter++;

            //day of week
            if (comparison_flags[3])
                if (range_start.DayOfWeek <= DateTime.Now.DayOfWeek)
                    if (DateTime.Now.DayOfWeek <= range_end.DayOfWeek)
                        comparison_counter++;

            //hour
            if (comparison_flags[4])
                if (range_start.Hour <= DateTime.Now.Hour)
                    if (DateTime.Now.Hour <= range_end.Hour)
                        comparison_counter++;

            //minute
            if (comparison_flags[5])
                if (range_start.Minute <= DateTime.Now.Minute)
                    if (DateTime.Now.Minute <= range_end.Minute)
                        comparison_counter++;

            //second
            if (comparison_flags[6])
                if (range_start.Second <= DateTime.Now.Second)
                    if (DateTime.Now.Second <= range_end.Second)
                        comparison_counter++;

            if (num_comparisons == comparison_counter)
                return true;
            else
                return false;
        }

        public override string ToString()
        {
            //display values in a way that is appropriate based on the comparison options
            //if no comparison options are selected then use to string on the range

            int comp_count = 0;
            foreach (bool b in comparison_flags)
                if (b)
                    comp_count++;

            if(comp_count == 0)
                return "No Comparison " + range_start.ToString() + " - " + range_end.ToString();
            else
            {
                //comp ref
                //year, month, day of month, day of week, hour, minute, second

                string retstr = "";

                DateTime[] gen_source = {range_start, range_end};

                foreach (DateTime dt in gen_source)
                {
                    for (int comp_counter = 0; comp_counter < 7; comp_counter++)
                    {
                        if (comparison_flags[comp_counter])
                        {
                            switch (comp_counter)
                            {
                                case 0: retstr += dt.Year + "/";  break;
                                case 1: retstr += dt.Month + "/"; break;
                                case 2: retstr += dt.Day + " "; break;
                                case 3: retstr += dt.DayOfWeek.ToString() + " "; break;
                                case 4: retstr += dt.Hour + ":"; break;
                                case 5: retstr += dt.Minute + ":"; break;
                                case 6: retstr += dt.Second + " "; break;
                            }
                        }
                    }

                    retstr += " - ";
                }

                return retstr;
            }
        }

        public DateTime RangeStart
        {
            get { return range_start; }
        }

        public DateTime RangeEnd
        {
            get { return range_end; }
        }

        public int RevertAfter
        {
            get { return revert_after; }
        }

        public bool[] ComparisonFlags
        {
            get { return comparison_flags; }
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
    }
}
