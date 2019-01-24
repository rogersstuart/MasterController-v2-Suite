using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MasterControllerInterface
{
    class BinaryTranslator
    {
        private static string[] days_of_the_week = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        private static int page_size = 128;
        
        //converts the binary log file into text
        public static void ParseAccessLog(string log_filename, byte[] log_buffer)
        {
            using (StreamWriter output_file = new StreamWriter(new FileStream(log_filename, FileMode.Create)))
            {
                for (int page_counter = 0; page_counter < 1000; page_counter++)
                {
                    //each line in the resulting text file will contain the card id, time stamp, reader id, and authorization code
                    //the card id occupies the first 8 bytes (msb first), the time stamp occupies the next 7 bytes, and
                    //the last byte is composed of two values. the most significant nibble of the last byte is the reader id and the least significant nibble of the
                    //last byte is the authorization code.

                    for (int offset_counter = page_counter * page_size; offset_counter < (page_counter * page_size + page_size);)
                    {
                        ulong card_id = ((ulong)log_buffer[offset_counter] << 56) |
                                        ((ulong)log_buffer[offset_counter + 1] << 48) |
                                        ((ulong)log_buffer[offset_counter + 2] << 40) |
                                        ((ulong)log_buffer[offset_counter + 3] << 32) |
                                        ((ulong)log_buffer[offset_counter + 4] << 24) |
                                        ((ulong)log_buffer[offset_counter + 5] << 16) |
                                        ((ulong)log_buffer[offset_counter + 6] << 8) |
                                        ((ulong)log_buffer[offset_counter + 7]);

                        offset_counter += 8;

                        int year = (log_buffer[offset_counter + 6] >> 4) * 10 + (log_buffer[6 + offset_counter] & 0xF);
                        int month = ((log_buffer[offset_counter + 5] >> 4) & 1) * 10 + (log_buffer[5 + offset_counter] & 0xF);
                        int date = ((log_buffer[offset_counter + 4] >> 4) & 3) * 10 + (log_buffer[4 + offset_counter] & 0xF);
                        int day = (log_buffer[offset_counter + 3] & 7) - 1;

                        int hour = ((log_buffer[2 + offset_counter] >> 4) & 3) * 10 + (log_buffer[2 + offset_counter] & 0xF);
                        int minute = (log_buffer[1 + offset_counter] >> 4) * 10 + (log_buffer[1 + offset_counter] & 0xF);
                        int second = (log_buffer[offset_counter] >> 4) * 10 + (log_buffer[offset_counter] & 0xF);

                        int reader_id = log_buffer[7 + offset_counter] >> 4;
                        int auth_code = log_buffer[7 + offset_counter] & 0xF;

                        if (day > -1)
                        {
                            output_file.Write(card_id + " ");
                            output_file.WriteLine(days_of_the_week[day] + ", " + year.ToString() + "/" + month.ToString() + "/" + date.ToString() + " " +
                                                  hour.ToString() + ":" + minute.ToString() + ":" + second.ToString() + " Reader ID:" + reader_id.ToString() +
                                                  " Code:" + auth_code);
                        }

                        offset_counter += 8;
                    }
                }

                output_file.Flush();
                output_file.Close();
            }
        }

        public static DateTime TranslateRTCBytes(byte[] rtc_bytes)
        {
            int year = 2000 + (rtc_bytes[6] >> 4) * 10 + (rtc_bytes[6] & 0xF);
            int month = (rtc_bytes[5] >> 4) * 10 + (rtc_bytes[5] & 0xF);
            int day = (rtc_bytes[4] >> 4) * 10 + (rtc_bytes[4] & 0xF);
            int hour = (rtc_bytes[2] >> 4) * 10 + (rtc_bytes[2] & 0xF);
            int minute = (rtc_bytes[1] >> 4) * 10 + (rtc_bytes[1] & 0xF);
            int second = (rtc_bytes[0] >> 4) * 10 + (rtc_bytes[0] & 0xF);

            return new DateTime(year, month, day, hour, minute, second);
        }
    }
}
