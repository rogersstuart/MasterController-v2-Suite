using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using MCICommon;

namespace MasterControllerDotNet_Server
{
    public partial class ImportForm : Form
    {
        DatabaseConnectionProperties dbconnprop;
        private string[] days_of_the_week = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        public ImportForm(DatabaseConnectionProperties dbconnprop)
        {
            InitializeComponent();

            this.dbconnprop = dbconnprop;

            RefreshTableNames();
        }

        private void RefreshTableNames()
        {
            new Thread(delegate()
            {
                List<string> table_names = new List<string>();
                using (MySqlConnection sqlconn = new MySqlConnection(dbconnprop.ConnectionString))
                {
                    sqlconn.Open();
                    MySqlCommand cmdName = new MySqlCommand("show tables", sqlconn);
                    MySqlDataReader reader = cmdName.ExecuteReader();
                    while (reader.Read())
                    {
                        table_names.Add(reader.GetString(0));
                    }
                    reader.Close();
                    sqlconn.Close();
                }

                Invoke(new Action(() => comboBox1.DataSource = null));
                Invoke(new Action(() => comboBox1.DataSource = table_names));
            }).Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            comboBox1.Enabled = false;

            try
            {
                //select file to import
                if (comboBox1.SelectedIndex > -1)
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Filter = "Supported Extentions (*.xlsx;*.csv;*.bin)|*.xlsx;*.csv;*.bin";
                    ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    ofd.FileName = "";
                    if (ofd.ShowDialog() == DialogResult.OK)
                        if (ofd.SafeFileName.Contains(".csv"))
                            ProcessCSV(ofd.FileName, (string)comboBox1.SelectedItem);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, ex.ToString());
            }

            button1.Enabled = true;
            comboBox1.Enabled = true;
        }

        private void ProcessCSV(string filename, string tablename)
        {
            using(StreamReader csv_reader = new StreamReader(filename))
                using (MySqlConnection mysqlconnection = new MySqlConnection())
                {
                    mysqlconnection.ConnectionString = dbconnprop.ConnectionString;
                    mysqlconnection.Open();

                    while (true)
                    {
                        string line_to_parse = csv_reader.ReadLine();

                        if (line_to_parse != null)
                        {
                            UInt64 card_id;
                            string entry_times, entry_days, available_floors;

                            //get the values
                            card_id = UInt64.Parse(line_to_parse.Substring(0, line_to_parse.IndexOf(',')).Trim());
                            line_to_parse = line_to_parse.Substring(line_to_parse.IndexOf(',') + 1);

                            entry_times = line_to_parse.Substring(0, line_to_parse.IndexOf(',')).Trim();
                            line_to_parse = line_to_parse.Substring(line_to_parse.IndexOf(',') + 1);

                            entry_days = line_to_parse.Substring(0, line_to_parse.IndexOf(',')).Trim();
                            line_to_parse = line_to_parse.Substring(line_to_parse.IndexOf(',') + 1);

                            available_floors = line_to_parse.Trim();
                            ////

                            //parse entry times
                            uint start_hour, start_minute, end_hour, end_minute;

                            start_hour = uint.Parse(entry_times.Substring(0, entry_times.IndexOf(':')).Trim());
                            entry_times = entry_times.Substring(entry_times.IndexOf(':') + 1);

                            start_minute = uint.Parse(entry_times.Substring(0, entry_times.IndexOf('-')).Trim());
                            entry_times = entry_times.Substring(entry_times.IndexOf('-') + 1);

                            end_hour = uint.Parse(entry_times.Substring(0, entry_times.IndexOf(':')).Trim());
                            entry_times = entry_times.Substring(entry_times.IndexOf(':') + 1);

                            end_minute = uint.Parse(entry_times.Trim());
                            ////

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
                            ////

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
                                            //got it. colon. ;)
                                            converted_available_floors |= floorToUintPos(uint.Parse(available_floors.Substring(0, available_floors.IndexOf(':'))));
                                            available_floors = available_floors.Substring(available_floors.IndexOf(':') + 1);
                                        }
                            }
                            ////

                            //done with the conversions for this line. add it to the database now

                            //generate the accessproperties object
                            DateTime begin_enable = DateTime.Now;
                            DateTime end_enable = DateTime.Now.AddYears(1);

                            List<ActivationProperties> actiprops = new List<ActivationProperties>();

                            //only compare the day of week
                            bool[] comparison_flags = { false, false, false, true, false, false, false };
                            int activation_duration = 6;

                            bool[] exp0mask = new bool[16];
                            bool[] exp0val = new bool[16];
                            bool[] exp1mask = new bool[16];
                            bool[] exp1val = new bool[16];

                            uint floor_mask = 8192;
                            for(int bit_counter = 0; bit_counter < 14; bit_counter++)
                            {
                                if((converted_available_floors & (floor_mask >> bit_counter)) > 0)
                                {
                                    exp0mask[13 - bit_counter] = true;
                                    exp0val[13 - bit_counter] = true;
                                    exp1mask[13 - bit_counter] = true;
                                    exp1val[13 - bit_counter] = true;
                                }
                            }
                            exp0mask[0] = false;
                            exp1mask[0] = false;

                            List<DateTime> activation_days = new List<DateTime>();
                            byte mask = 64;
                            for (int bit_counter = 0; bit_counter < 7; bit_counter++)
                                if ((converted_entry_days & (mask >> bit_counter)) > 0)
                                    activation_days.Add(GetNextWeekday((DayOfWeek)bit_counter));

                            foreach (DateTime dt in activation_days)
                                actiprops.Add(new ActivationProperties(dt, dt, exp0mask, exp1mask, exp0val, exp1val, comparison_flags, activation_duration));

                            AccessProperties accessprop = new AccessProperties(begin_enable, end_enable, actiprops, false, false);

                            //add the entry
                            byte[] data;
                            using (var ms = new MemoryStream())
                            {
                                BinaryFormatter bFormatter = new BinaryFormatter();
                                bFormatter.Serialize(ms, accessprop);
                                data = ms.ToArray();

                                using (MySqlCommand sqlcmd = new MySqlCommand("insert into `" + tablename + "` (uid, data) values (@uid, @data)", mysqlconnection))
                                {
                                    sqlcmd.Parameters.AddWithValue("@uid", card_id);
                                    sqlcmd.Parameters.AddWithValue("@data", data);
                                    sqlcmd.ExecuteNonQuery();
                                }
                            }
                        }
                        else
                            break;
                    }
                }
        }

        private DateTime GetNextWeekday(DayOfWeek day)
        {
            DateTime result = DateTime.Now;
            while (result.DayOfWeek != day)
                result = result.AddDays(1);
            return result;
        }

        //returns a byte with a bit set at the position specified by the following equation
        //6 - (index of day_to_convert in the days_of_the_week array)
        //the first day of the week (Sunday) is the msb
        private byte dayOfWeekToBytePos(string day_to_convert)
        {
            int day_index = getDayOfWeekPos(day_to_convert);
            if (day_index > -1)
                return (byte)Math.Pow(2, 6 - day_index);
            else
                return 0;
        }

        //returns the index in the days_of_the_week array where day_to_convert can be found
        private int getDayOfWeekPos(string day_to_convert)
        {
            for (int index_counter = 0; index_counter < 7; index_counter++)
                if (days_of_the_week[index_counter].ToLower() == day_to_convert.ToLower())
                    return index_counter;
            return -1;
        }

        //returns a unsigned integer with a bit set at the position given by the following equation
        //14 - floor_to_convert
        //the first floor is the lsb
        private uint floorToUintPos(uint floor_to_convert)
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
