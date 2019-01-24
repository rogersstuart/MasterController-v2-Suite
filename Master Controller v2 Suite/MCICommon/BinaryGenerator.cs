using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MCICommon
{
    public static class BinaryGenerator
    {
        private static readonly string[] days_of_the_week = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        public static string GenerateIDList(string csvfilename)
        {
            bool completion_flag = false;

            string output_filename = Path.GetDirectoryName(csvfilename) + "\\" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".bin";

            ConfigurationManager.SessionTempFiles.Add(output_filename);

            using (StreamReader input_file = new StreamReader(csvfilename))
            using (BinaryWriter output_file = new BinaryWriter(new FileStream(output_filename, FileMode.Create)))
            {
                for (int page_counter = 0; page_counter < 1000 && !completion_flag; page_counter++)
                {
                    byte[] page_buffer = new byte[128];
                    for (int buffer_counter = 0; buffer_counter < 128;)
                    {
                        //card_id
                        //any integer number that will fit in 8 bytes
                        //
                        //entry_times - 24hr format - integer values
                        //hh:mm-hh:mm i.e start_time:end_time
                        //the times may not be equal and the end_time cannot be less than the start time
                        //
                        //entry_days - string values - Sunday to Saturday
                        //start day - end day or first start day - end of first day group : second start day - final end day
                        //overlap is permitted (but why do it?)
                        //a single day may be specified
                        //
                        //available_floors - integer values - 1 through 14
                        //first available floor - last available floor or 
                        //first floor of first sequence - last floor of first sequence : first floor of second sequence - last floor of second sequence
                        //a single floor may be specified

                        string line_to_parse = input_file.ReadLine();

                        if (line_to_parse != null && line_to_parse != "")
                        {
                            line_to_parse = line_to_parse.Trim();

                            ulong card_id;
                            string entry_times, entry_days, available_floors;

                            card_id = ulong.Parse(line_to_parse.Substring(0, line_to_parse.IndexOf(',')).Trim());
                            line_to_parse = line_to_parse.Substring(line_to_parse.IndexOf(',') + 1).Trim();

                            entry_times = line_to_parse.Substring(0, line_to_parse.IndexOf(',')).Trim();
                            line_to_parse = line_to_parse.Substring(line_to_parse.IndexOf(',') + 1).Trim();

                            entry_days = line_to_parse.Substring(0, line_to_parse.IndexOf(',')).Trim();
                            line_to_parse = line_to_parse.Substring(line_to_parse.IndexOf(',') + 1).Trim();

                            available_floors = line_to_parse.Trim();

                            //add card id to buffer
                            byte[] id_conversion_buffer = BitConverter.GetBytes(card_id);
                            for (int id_byte_counter = 0; id_byte_counter < 8; id_byte_counter++)
                                page_buffer[buffer_counter + id_byte_counter] = id_conversion_buffer[BitConverter.IsLittleEndian ? 7 - id_byte_counter : id_byte_counter];

                            buffer_counter += 8;

                            //parse entry times
                            uint start_hour, start_minute, end_hour, end_minute;

                            start_hour = uint.Parse(entry_times.Substring(0, entry_times.IndexOf(':')).Trim());
                            entry_times = entry_times.Substring(entry_times.IndexOf(':') + 1);

                            start_minute = uint.Parse(entry_times.Substring(0, entry_times.IndexOf('-')).Trim());
                            entry_times = entry_times.Substring(entry_times.IndexOf('-') + 1);

                            end_hour = uint.Parse(entry_times.Substring(0, entry_times.IndexOf(':')).Trim());
                            entry_times = entry_times.Substring(entry_times.IndexOf(':') + 1);

                            end_minute = uint.Parse(entry_times.Trim());


                            //add entry times to buffer as bcd. minute, hour, minute, hour

                            page_buffer[buffer_counter] = (byte)(((start_minute / 10) << 4) | (start_minute % 10));
                            page_buffer[buffer_counter + 1] = (byte)(((start_hour / 20)) << 5 | ((start_hour / 10) << 4) | (start_hour % 10));
                            page_buffer[buffer_counter + 2] = (byte)(((end_minute / 10) << 4) | (end_minute % 10));
                            page_buffer[buffer_counter + 3] = (byte)(((end_hour / 20)) << 5 | ((end_hour / 10) << 4) | (end_hour % 10));

                            buffer_counter += 4;

                            //parse entry days
                            byte converted_entry_days = 0;

                            while (true)
                            {
                                if (entry_days.IndexOf('-') == -1 && entry_days.IndexOf(':') == -1)
                                {
                                    converted_entry_days |= dayOfWeekToBytePos(entry_days);
                                    break; //done :)
                                }
                                else
                                {
                                    if (entry_days.IndexOf('-') > -1 && entry_days.IndexOf(':') > -1)
                                    {
                                        if (entry_days.IndexOf('-') > entry_days.IndexOf(':'))
                                        {
                                            //so... looks like the colon is closer
                                            converted_entry_days |= dayOfWeekToBytePos(entry_days.Substring(0, entry_days.IndexOf(':')));
                                            entry_days = entry_days.Substring(entry_days.IndexOf(':') + 1);
                                        }
                                        else
                                        {
                                            //it seems like the dash is closer
                                            int first_day, second_day;

                                            first_day = getDayOfWeekPos(entry_days.Substring(0, entry_days.IndexOf('-')).Trim());

                                            entry_days = entry_days.Substring(entry_days.IndexOf('-') + 1);

                                            //there must be a colon coming up so we can use that
                                            second_day = getDayOfWeekPos(entry_days.Substring(0, entry_days.IndexOf(':')).Trim());

                                            entry_days = entry_days.Substring(entry_days.IndexOf(':') + 1);

                                            for (; first_day <= second_day; first_day++)
                                                converted_entry_days |= dayOfWeekToBytePos(days_of_the_week[first_day]);
                                        }
                                    }
                                    else
                                        if (entry_days.IndexOf('-') > -1)
                                    {
                                        //there's a dash but no colon
                                        int first_day, second_day;

                                        first_day = getDayOfWeekPos(entry_days.Substring(0, entry_days.IndexOf('-')).Trim());

                                        entry_days = entry_days.Substring(entry_days.IndexOf('-') + 1);

                                        //you shouldn't have another dash after this one and there is no colon
                                        second_day = getDayOfWeekPos(entry_days.Trim());

                                        for (; first_day <= second_day; first_day++)
                                            converted_entry_days |= dayOfWeekToBytePos(days_of_the_week[first_day]);

                                    }
                                    else
                                    {
                                        //so if we're here there's a colon but no dash
                                        while (entry_days.IndexOf(':') > -1)
                                        {
                                            converted_entry_days |= dayOfWeekToBytePos(entry_days.Substring(0, entry_days.IndexOf(':')));
                                            entry_days = entry_days.Substring(entry_days.IndexOf(':') + 1);
                                        }
                                    }

                                }
                            }

                            //put it in da buffer

                            page_buffer[buffer_counter] = converted_entry_days;
                            buffer_counter++;

                            //parse floors
                            uint converted_available_floors = 0;
                            while (true)
                            {
                                if (available_floors.IndexOf('-') == -1 && available_floors.IndexOf(':') == -1)
                                {
                                    converted_available_floors |= floorToUintPos(uint.Parse(available_floors.Trim()));
                                    break;
                                }
                                else
                                    if (available_floors.IndexOf('-') > -1 && available_floors.IndexOf(':') > -1)
                                {
                                    if (available_floors.IndexOf('-') > available_floors.IndexOf(':'))
                                    {
                                        //so... looks like the colon is closer
                                        converted_available_floors |= floorToUintPos(uint.Parse(available_floors.Substring(0, available_floors.IndexOf(':'))));
                                        available_floors = available_floors.Substring(available_floors.IndexOf(':') + 1);
                                    }
                                    else
                                    {
                                        //the dash is closer...
                                        uint first_floor, second_floor;

                                        first_floor = uint.Parse(available_floors.Substring(0, available_floors.IndexOf('-')).Trim());

                                        available_floors = available_floors.Substring(available_floors.IndexOf('-') + 1);

                                        //there must be a colon coming up so we can use that
                                        second_floor = uint.Parse(available_floors.Substring(0, available_floors.IndexOf(':')).Trim());

                                        for (; first_floor <= second_floor; first_floor++)
                                            converted_available_floors |= floorToUintPos(first_floor);
                                    }
                                }
                                else
                                        if (available_floors.IndexOf('-') > -1)
                                {
                                    //there's a dash but no colon
                                    uint first_floor, second_floor;

                                    first_floor = uint.Parse(available_floors.Substring(0, available_floors.IndexOf('-')).Trim());

                                    available_floors = available_floors.Substring(available_floors.IndexOf('-') + 1);

                                    //you shouldn't have another dash after this one and there is no colon
                                    second_floor = uint.Parse(available_floors.Trim());

                                    for (; first_floor <= second_floor; first_floor++)
                                        converted_available_floors |= floorToUintPos(first_floor);
                                }
                                else
                                {
                                    //got it. colon. :)
                                    converted_available_floors |= floorToUintPos(uint.Parse(available_floors.Substring(0, available_floors.IndexOf(':'))));
                                    available_floors = available_floors.Substring(available_floors.IndexOf(':') + 1);
                                }
                            }

                            //yay! now put that in the buffer too!
                            page_buffer[buffer_counter] = (byte)(converted_available_floors >> 8);
                            page_buffer[buffer_counter + 1] = (byte)(converted_available_floors & 0xFF);

                            buffer_counter += 19;
                        }
                        else
                        {
                            completion_flag = true;
                            break;
                        }
                    }

                    output_file.Write(page_buffer, 0, 128);
                }

                output_file.Close();
            }

            return output_filename;
        }

        public static byte[] GenerateIDList(byte[] list_contents)
        {
            bool completion_flag = false;

            using (StreamReader input_file = new StreamReader(new MemoryStream(list_contents)))
            using (BinaryWriter output_file = new BinaryWriter(new MemoryStream()))
            {
                for (int page_counter = 0; page_counter < 1000 && !completion_flag; page_counter++)
                {
                    byte[] page_buffer = new byte[128];
                    for (int buffer_counter = 0; buffer_counter < 128;)
                    {
                        //card_id
                        //any integer number that will fit in 8 bytes
                        //
                        //entry_times - 24hr format - integer values
                        //hh:mm-hh:mm i.e start_time:end_time
                        //the times may not be equal and the end_time cannot be less than the start time
                        //
                        //entry_days - string values - Sunday to Saturday
                        //start day - end day or first start day - end of first day group : second start day - final end day
                        //overlap is permitted (but why do it?)
                        //a single day may be specified
                        //
                        //available_floors - integer values - 1 through 14
                        //first available floor - last available floor or 
                        //first floor of first sequence - last floor of first sequence : first floor of second sequence - last floor of second sequence
                        //a single floor may be specified

                        string line_to_parse = input_file.ReadLine();

                        if (line_to_parse != null && line_to_parse != "")
                        {
                            line_to_parse = line_to_parse.Trim();

                            ulong card_id;
                            string entry_times, entry_days, available_floors;

                            card_id = ulong.Parse(line_to_parse.Substring(0, line_to_parse.IndexOf(',')).Trim());
                            line_to_parse = line_to_parse.Substring(line_to_parse.IndexOf(',') + 1).Trim();

                            entry_times = line_to_parse.Substring(0, line_to_parse.IndexOf(',')).Trim();
                            line_to_parse = line_to_parse.Substring(line_to_parse.IndexOf(',') + 1).Trim();

                            entry_days = line_to_parse.Substring(0, line_to_parse.IndexOf(',')).Trim();
                            line_to_parse = line_to_parse.Substring(line_to_parse.IndexOf(',') + 1).Trim();

                            available_floors = line_to_parse.Trim();

                            //add card id to buffer
                            byte[] id_conversion_buffer = BitConverter.GetBytes(card_id);
                            for (int id_byte_counter = 0; id_byte_counter < 8; id_byte_counter++)
                                page_buffer[buffer_counter + id_byte_counter] = id_conversion_buffer[BitConverter.IsLittleEndian ? 7 - id_byte_counter : id_byte_counter];

                            buffer_counter += 8;

                            //parse entry times
                            uint start_hour, start_minute, end_hour, end_minute;

                            start_hour = uint.Parse(entry_times.Substring(0, entry_times.IndexOf(':')).Trim());
                            entry_times = entry_times.Substring(entry_times.IndexOf(':') + 1);

                            start_minute = uint.Parse(entry_times.Substring(0, entry_times.IndexOf('-')).Trim());
                            entry_times = entry_times.Substring(entry_times.IndexOf('-') + 1);

                            end_hour = uint.Parse(entry_times.Substring(0, entry_times.IndexOf(':')).Trim());
                            entry_times = entry_times.Substring(entry_times.IndexOf(':') + 1);

                            end_minute = uint.Parse(entry_times.Trim());


                            //add entry times to buffer as bcd. minute, hour, minute, hour

                            page_buffer[buffer_counter] = (byte)(((start_minute / 10) << 4) | (start_minute % 10));
                            page_buffer[buffer_counter + 1] = (byte)(((start_hour / 20)) << 5 | ((start_hour / 10) << 4) | (start_hour % 10));
                            page_buffer[buffer_counter + 2] = (byte)(((end_minute / 10) << 4) | (end_minute % 10));
                            page_buffer[buffer_counter + 3] = (byte)(((end_hour / 20)) << 5 | ((end_hour / 10) << 4) | (end_hour % 10));

                            buffer_counter += 4;

                            //parse entry days
                            byte converted_entry_days = 0;

                            while (true)
                            {
                                if (entry_days.IndexOf('-') == -1 && entry_days.IndexOf(':') == -1)
                                {
                                    converted_entry_days |= dayOfWeekToBytePos(entry_days);
                                    break; //done :)
                                }
                                else
                                {
                                    if (entry_days.IndexOf('-') > -1 && entry_days.IndexOf(':') > -1)
                                    {
                                        if (entry_days.IndexOf('-') > entry_days.IndexOf(':'))
                                        {
                                            //so... looks like the colon is closer
                                            converted_entry_days |= dayOfWeekToBytePos(entry_days.Substring(0, entry_days.IndexOf(':')));
                                            entry_days = entry_days.Substring(entry_days.IndexOf(':') + 1);
                                        }
                                        else
                                        {
                                            //it seems like the dash is closer
                                            int first_day, second_day;

                                            first_day = getDayOfWeekPos(entry_days.Substring(0, entry_days.IndexOf('-')).Trim());

                                            entry_days = entry_days.Substring(entry_days.IndexOf('-') + 1);

                                            //there must be a colon coming up so we can use that
                                            second_day = getDayOfWeekPos(entry_days.Substring(0, entry_days.IndexOf(':')).Trim());

                                            entry_days = entry_days.Substring(entry_days.IndexOf(':') + 1);

                                            for (; first_day <= second_day; first_day++)
                                                converted_entry_days |= dayOfWeekToBytePos(days_of_the_week[first_day]);
                                        }
                                    }
                                    else
                                        if (entry_days.IndexOf('-') > -1)
                                    {
                                        //there's a dash but no colon
                                        int first_day, second_day;

                                        first_day = getDayOfWeekPos(entry_days.Substring(0, entry_days.IndexOf('-')).Trim());

                                        entry_days = entry_days.Substring(entry_days.IndexOf('-') + 1);

                                        //you shouldn't have another dash after this one and there is no colon
                                        second_day = getDayOfWeekPos(entry_days.Trim());

                                        for (; first_day <= second_day; first_day++)
                                            converted_entry_days |= dayOfWeekToBytePos(days_of_the_week[first_day]);

                                    }
                                    else
                                    {
                                        //so if we're here there's a colon but no dash
                                        while (entry_days.IndexOf(':') > -1)
                                        {
                                            converted_entry_days |= dayOfWeekToBytePos(entry_days.Substring(0, entry_days.IndexOf(':')));
                                            entry_days = entry_days.Substring(entry_days.IndexOf(':') + 1);
                                        }
                                    }

                                }
                            }

                            //put it in da buffer

                            page_buffer[buffer_counter] = converted_entry_days;
                            buffer_counter++;

                            //parse floors
                            uint converted_available_floors = 0;
                            while (true)
                            {
                                if (available_floors.IndexOf('-') == -1 && available_floors.IndexOf(':') == -1)
                                {
                                    converted_available_floors |= floorToUintPos(uint.Parse(available_floors.Trim()));
                                    break;
                                }
                                else
                                    if (available_floors.IndexOf('-') > -1 && available_floors.IndexOf(':') > -1)
                                {
                                    if (available_floors.IndexOf('-') > available_floors.IndexOf(':'))
                                    {
                                        //so... looks like the colon is closer
                                        converted_available_floors |= floorToUintPos(uint.Parse(available_floors.Substring(0, available_floors.IndexOf(':'))));
                                        available_floors = available_floors.Substring(available_floors.IndexOf(':') + 1);
                                    }
                                    else
                                    {
                                        //the dash is closer...
                                        uint first_floor, second_floor;

                                        first_floor = uint.Parse(available_floors.Substring(0, available_floors.IndexOf('-')).Trim());

                                        available_floors = available_floors.Substring(available_floors.IndexOf('-') + 1);

                                        //there must be a colon coming up so we can use that
                                        second_floor = uint.Parse(available_floors.Substring(0, available_floors.IndexOf(':')).Trim());

                                        for (; first_floor <= second_floor; first_floor++)
                                            converted_available_floors |= floorToUintPos(first_floor);
                                    }
                                }
                                else
                                        if (available_floors.IndexOf('-') > -1)
                                {
                                    //there's a dash but no colon
                                    uint first_floor, second_floor;

                                    first_floor = uint.Parse(available_floors.Substring(0, available_floors.IndexOf('-')).Trim());

                                    available_floors = available_floors.Substring(available_floors.IndexOf('-') + 1);

                                    //you shouldn't have another dash after this one and there is no colon
                                    second_floor = uint.Parse(available_floors.Trim());

                                    for (; first_floor <= second_floor; first_floor++)
                                        converted_available_floors |= floorToUintPos(first_floor);
                                }
                                else
                                {
                                    //got it. colon. :)
                                    converted_available_floors |= floorToUintPos(uint.Parse(available_floors.Substring(0, available_floors.IndexOf(':'))));
                                    available_floors = available_floors.Substring(available_floors.IndexOf(':') + 1);
                                }
                            }

                            //yay! now put that in the buffer too!
                            page_buffer[buffer_counter] = (byte)(converted_available_floors >> 8);
                            page_buffer[buffer_counter + 1] = (byte)(converted_available_floors & 0xFF);

                            buffer_counter += 19;
                        }
                        else
                        {
                            completion_flag = true;
                            break;
                        }
                    }

                    output_file.Write(page_buffer, 0, 128);
                }

                output_file.Flush();

                return ((MemoryStream)output_file.BaseStream).ToArray();
            }
        }

        public static byte[] GenerateRTCBytes()
        {
            DateTime current_datetime = DateTime.Now;

            int current_day = getDayOfWeekPos(current_datetime.DayOfWeek.ToString()) + 1;

            byte[] rtc_register_buffer = new byte[7];
            rtc_register_buffer[0] = (byte)(((current_datetime.Second / 10) << 4) | (current_datetime.Second % 10));
            rtc_register_buffer[1] = (byte)(((current_datetime.Minute / 10) << 4) | (current_datetime.Minute % 10));
            rtc_register_buffer[2] = (byte)(((current_datetime.Hour / 10) << 4) | (current_datetime.Hour % 10));
            rtc_register_buffer[3] = (byte)current_day;
            rtc_register_buffer[4] = (byte)(((current_datetime.Day / 10) << 4) | (current_datetime.Day % 10));
            rtc_register_buffer[5] = (byte)(((current_datetime.Month / 10) << 4) | (current_datetime.Month % 10));
            rtc_register_buffer[6] = (byte)((((current_datetime.Year % 2000) / 10) << 4) | ((current_datetime.Year % 2000) % 10));

            return rtc_register_buffer;
        }

        //returns a byte with a bit set at the position specified by the following equation
        //6 - (index of day_to_convert in the days_of_the_week array)
        //the first day of the week (Sunday) is the msb-1
        public static byte dayOfWeekToBytePos(string day_to_convert)
        {
            int day_index = getDayOfWeekPos(day_to_convert);
            if (day_index > -1)
                return (byte)Math.Pow(2, 6 - day_index);
            else
                throw new Exception("Unable to convert dow string to byte position.");
        }

        //returns the index in the days_of_the_week array where day_to_convert can be found
        public static int getDayOfWeekPos(string day_to_convert)
        {
            day_to_convert = day_to_convert.Trim();

            for (int index_counter = 0; index_counter < 7; index_counter++)
                if (days_of_the_week[index_counter].ToLower() == day_to_convert.ToLower())
                    return index_counter;

            throw new Exception("Unable to convert dow string to dow int.");
        }

        //returns a unsigned integer with a bit set at the position given by the following equation
        //14 - floor_to_convert
        //the first floor is the lsb
        public static uint floorToUintPos(uint floor_to_convert)
        {
            if (floor_to_convert < 15)
            {
                if (floor_to_convert > 1)
                    return (uint)1 << (int)(floor_to_convert - 1);
                else
                    return 1;
            }
            else
                return 0;
        }
    }
}
