using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public static class ListEntryUtilities
    {
        private static readonly string[] days_of_the_week = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        public static bool TryParseV2EntryTime(string _time_string)
        {
            string time_string = _time_string.Trim();

            if (time_string == "")
                return false;

            try
            {
                //parse entry times
                uint stored_start_hour, stored_start_minute, stored_end_hour, stored_end_minute;

                stored_start_hour = uint.Parse(time_string.Substring(0, time_string.IndexOf(':')).Trim());
                time_string = time_string.Substring(time_string.IndexOf(':') + 1);

                stored_start_minute = uint.Parse(time_string.Substring(0, time_string.IndexOf('-')).Trim());
                time_string = time_string.Substring(time_string.IndexOf('-') + 1);

                stored_end_hour = uint.Parse(time_string.Substring(0, time_string.IndexOf(':')).Trim());
                time_string = time_string.Substring(time_string.IndexOf(':') + 1);

                stored_end_minute = uint.Parse(time_string.Trim());

                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public static TimeSpan[] ConvertTimeStringToTimeSpans(string time_string)
        {
            time_string = time_string.Trim();

            uint stored_start_hour, stored_start_minute, stored_end_hour, stored_end_minute;

            stored_start_hour = uint.Parse(time_string.Substring(0, time_string.IndexOf(':')).Trim());
            time_string = time_string.Substring(time_string.IndexOf(':') + 1);

            stored_start_minute = uint.Parse(time_string.Substring(0, time_string.IndexOf('-')).Trim());
            time_string = time_string.Substring(time_string.IndexOf('-') + 1);

            stored_end_hour = uint.Parse(time_string.Substring(0, time_string.IndexOf(':')).Trim());
            time_string = time_string.Substring(time_string.IndexOf(':') + 1);

            stored_end_minute = uint.Parse(time_string.Trim());

            return new TimeSpan[] { new TimeSpan((int)stored_start_hour, (int)stored_start_minute, 0), new TimeSpan((int)stored_end_hour, (int)stored_end_minute, 0)};
        }

        public static bool TryParseV2EntryDays(string days_string)
        {
            days_string = days_string.Trim();

            if (days_string == "")
                return false;

            try
            {
                var result = ConvertDaysString(days_string);

                if (result == 0 || result == 0xFF)
                    return false;

                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public static byte ConvertDaysString(string entry_days)
        {
            entry_days = entry_days.Trim();

            byte converted_entry_days = 0;

            while (true)
            {
                if (entry_days.IndexOf('-') == -1 && entry_days.IndexOf(':') == -1)
                {
                    converted_entry_days |= BinaryGenerator.dayOfWeekToBytePos(entry_days.Trim());
                    break; //done :)
                }
                else
                {
                    if (entry_days.IndexOf('-') > -1 && entry_days.IndexOf(':') > -1)
                    {
                        if (entry_days.IndexOf('-') > entry_days.IndexOf(':'))
                        {
                            //so... looks like the colon is closer
                            converted_entry_days |= BinaryGenerator.dayOfWeekToBytePos(entry_days.Substring(0, entry_days.IndexOf(':')).Trim());
                            entry_days = entry_days.Substring(entry_days.IndexOf(':') + 1).Trim();
                        }
                        else
                        {
                            //it seems like the dash is closer
                            int first_day, second_day;

                            first_day = BinaryGenerator.getDayOfWeekPos(entry_days.Substring(0, entry_days.IndexOf('-')).Trim());

                            entry_days = entry_days.Substring(entry_days.IndexOf('-') + 1).Trim();

                            //there must be a colon coming up so we can use that
                            second_day = BinaryGenerator.getDayOfWeekPos(entry_days.Substring(0, entry_days.IndexOf(':')).Trim());

                            entry_days = entry_days.Substring(entry_days.IndexOf(':') + 1).Trim();

                            for (; first_day <= second_day; first_day++)
                                converted_entry_days |= BinaryGenerator.dayOfWeekToBytePos(days_of_the_week[first_day]);
                        }
                    }
                    else
                        if (entry_days.IndexOf('-') > -1)
                    {
                        //there's a dash but no colon
                        int first_day, second_day;

                        first_day = BinaryGenerator.getDayOfWeekPos(entry_days.Substring(0, entry_days.IndexOf('-')).Trim());

                        entry_days = entry_days.Substring(entry_days.IndexOf('-') + 1).Trim();

                        //you shouldn't have another dash after this one and there is no colon
                        second_day = BinaryGenerator.getDayOfWeekPos(entry_days.Trim());

                        for (; first_day <= second_day; first_day++)
                            converted_entry_days |= BinaryGenerator.dayOfWeekToBytePos(days_of_the_week[first_day]);

                    }
                    else
                    {
                        //so if we're here there's a colon but no dash
                        while (entry_days.IndexOf(':') > -1)
                        {
                            converted_entry_days |= BinaryGenerator.dayOfWeekToBytePos(entry_days.Substring(0, entry_days.IndexOf(':')).Trim());
                            entry_days = entry_days.Substring(entry_days.IndexOf(':') + 1).Trim();
                        }
                    }

                }
            }

            return converted_entry_days;
        }

        public static bool[] ConvertDOWStringToBools(string days_string)
        {
            byte days_conv = ConvertDaysString(days_string);

            bool[] to_return = new bool[7];

            for (int i = 0; i < 7; i++)
                to_return[i] = ((days_conv >> (6 - i)) & 1) == 1;

            return to_return;
        }

        public static string ConvertBoolToDOWString(bool[] dows)
        {
            string dow_string = "";

            for(int i = 0; i < 7; i++)
            {
                if (dows[i])
                {
                    int series_end = i;

                    if (i != 6)
                        for (; series_end < 6; series_end++)
                            if (!dows[series_end+1])
                                break;

                    
                    if (series_end > i && dow_string != "")
                        dow_string += " : " + days_of_the_week[i] + " - " + days_of_the_week[series_end];
                    else
                        if(series_end > i)
                            dow_string += days_of_the_week[i] + " - " + days_of_the_week[series_end];
                        else
                            if (series_end == i && dow_string != "")
                                dow_string += " : " + days_of_the_week[i];
                            else
                                if (series_end == i)
                                    dow_string += days_of_the_week[i];

                    if (series_end > i && series_end == 6)
                        break;
                    else
                        i = series_end+1;
                }
            }

            return dow_string;  
        }

        private static bool AreDOWsNeighboring(string dow0, string dow1)
        {
            int convered = BinaryGenerator.getDayOfWeekPos(dow0.Trim()) - BinaryGenerator.getDayOfWeekPos(dow1.Trim());
            if (convered == 1 || convered == -1)
                return true;
            else
                return false;
        }
    }
}
