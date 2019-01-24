using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterControllerInterface
{
    public class ListMember
    {
        string source_filename;
        int source_row;
        bool is_active;
        private int serial;
        private UInt64 uid;
        private string name;
        private string description;
        private string active_days;
        private string active_times;

        private string string_representation = "";

        public ListMember(string source_filename, int source_row, bool is_active, int serial, UInt64 uid, string name, string description, string active_days, string active_times)
        {
            this.source_filename = source_filename;
            this.source_row = source_row;
            this.is_active = is_active;
            this.serial = serial;
            this.uid = uid;
            this.name = name;
            this.description = description;
            this.active_days = active_days;
            this.active_times = active_times;

            string_representation = ToString().ToLower();
        }

        public bool Contains(string search_string)
        {
            search_string = search_string.ToLower();

            string[] comparison_strings = search_string.Split(' ');

            foreach (string substring in comparison_strings)
            {
                //string current_part = substring.Trim();

                if (string_representation.IndexOf(substring) == -1)
                    return false;
            }

            return true;
        }

        public Object[] GetValues()
        {
            //"Source List", "Source Row", "Serial", "UID", "User Name", "Description", "Active Days", "Active Times", key

            return new Object[10]
            {
                source_filename,
                source_row + "",
                is_active ? "True" : "False",
                serial + "",
                uid + "",
                name,
                description,
                active_days,
                active_times,
                null
            };
        }

        public override string ToString()
        {
            string return_string = serial + " " + uid + " " + name + " " + description + " ";
            return_string += active_days + " ";
            return_string += active_times;
            return return_string;
        }

        public int Serial
        {
            get { return serial; }
        }

        public UInt64 UID
        {
            get { return uid; }
        }

        public string ActiveDays
        {
            get { return active_days; }
            set { active_days = value; }
        }

        public string ActiveTimes
        {
            get { return active_times; }
            set { active_times = value; }
        }

        public string Name
        {
            get { return name; }
        }

        public string Description
        {
            get { return description; }
        }
    }
}
