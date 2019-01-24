//this needs to take new lists into account. what happens if no serials are present in the list?

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;

namespace MasterControllerInterface
{
    public static class ListUtilities
    {
        private static string[][] column_titles = new string[2][]{ new string[]{ "Serial", "UID", "Entry Times", "Entry Days", "Accessible Floors", "Assigned User", "Notes", "Type", "Card Canceled", "Card Lost", "Is NDEF Formatted", "Origin ID For Sequence" },
                                                                      new string[]{ "Serial", "UID", "Entry Times", "Entry Days", "Output Configuration", "Assigned User", "Notes", "Type", "Card Canceled", "Card Lost", "Is NDEF Formatted", "Origin ID For Sequence"} };

        //v1, v2 static, v2 active
        private static string[] config_values = new string[] {"1-14", "1", "1 : 10 : 13 : 14"};

        //returns row number where uid is found or -1 if it isnt found
        public static int FindUID(string filepath, UInt64 uid)
        {
            using (ExcelPackage p = new ExcelPackage(new FileInfo(filepath)))
            {
                ExcelWorkbook wb = p.Workbook;
                if(wb != null)
                {
                    ExcelWorksheet ws = wb.Worksheets.First();

                    if (ws.Dimension.Rows > 1) //continue if more than the column title row exists
                        for (int row_counter = 2; row_counter <= ws.Dimension.End.Row; row_counter++)
                        {
                            string cell_text = ws.Cells[row_counter, 2].Text;
                            if (cell_text.Trim() != "")
                                if (Convert.ToUInt64(cell_text) == uid)
                                    return row_counter;
                        }
                }
            }

            return -1;
        }

        public static void AddRow(string filename, string[] vars)
        {
            int uid_check = FindUID(filename, Convert.ToUInt64(vars[1]));
            if (uid_check < 0)
            {
                byte[] modified_file;
                using (ExcelPackage p = new ExcelPackage(new FileInfo(filename)))
                {
                    ExcelWorksheet ws = p.Workbook.Worksheets.First();
                    for (int column_counter = 1; column_counter <= vars.Length; column_counter++)
                        ws.Cells[ws.Dimension.End.Row+1, column_counter].Value = vars[column_counter-1];

                    modified_file = p.GetAsByteArray();
                }
                File.WriteAllBytes(filename, modified_file);
            }
            else
                throw new Exception("Row Exists");
        }

        public static void SetRow(string filename, int row, string[] vars)
        {
            byte[] modified_file;
            using (ExcelPackage p = new ExcelPackage(new FileInfo(filename)))
            {
                ExcelWorksheet ws = p.Workbook.Worksheets.First();

                ws.DeleteRow(row);
                ws.InsertRow(row, 1);

                for (int column_counter = 1; column_counter <= vars.Length; column_counter++)
                    ws.Cells[row, column_counter].Value = vars[column_counter - 1];

                modified_file = p.GetAsByteArray();
            }
            File.WriteAllBytes(filename, modified_file);
        }

        //public static string[] GetRow(string filename, int row)
        //{

        //}


        //broken: needs to add config options
        public static void CreateWorkbook(string file_name, int version)
        {
            using (ExcelPackage p = new ExcelPackage())
            {
                p.Workbook.Worksheets.Add("Access Control List");
                p.Workbook.Worksheets.First().InsertRow(1, 1);
                for (int column_counter = 1; column_counter < column_titles[version].Length; column_counter++)
                    p.Workbook.Worksheets.First().Cells[1, column_counter].Value = column_titles[version][column_counter-1];
                File.WriteAllBytes(file_name, p.GetAsByteArray());
            }
        }


        public static int GetSerialRange(string filename)
        {
            using (ExcelPackage p = new ExcelPackage(new FileInfo(filename)))
            {
                ExcelWorksheet ws = p.Workbook.Worksheets.First();
                if (ws.Dimension.End.Row > 1)
                    return Convert.ToInt32(ws.Cells[ws.Dimension.End.Row, 1].Text);
                else
                    return 0;
            }
        }

        public static int GetRowRange(string filename)
        {
            using (ExcelPackage p = new ExcelPackage(new FileInfo(filename)))
            {
                ExcelWorksheet ws = p.Workbook.Worksheets.First();
                return ws.Dimension.End.Row;
            }
        }

        public static async Task SyncListMembers(string[] filenames)
        {
            if (filenames.Length < 2)
                throw new Exception("More Than One List Is Required To Complete This Operation");

            List<ListMember[]> lol = new List<ListMember[]>();

            foreach(string filename in filenames)
                lol.Add(await GetListMembers(new string[]{filename}));
            
            //find the maximum serial number in all lists
            int max_serial = 1;
            foreach (ListMember[] lms in lol)
                foreach (ListMember lm in lms)
                    if (lm.Serial > max_serial)
                        max_serial = lm.Serial;
        }

        public static Task<ListMember[]> GetListMembers(string[] filenames)
        {
            return Task.Run(() =>
            {
                List<ListMember> members = new List<ListMember>();
                foreach(string filename in filenames)
                    using (ExcelPackage p = new ExcelPackage(new FileInfo(filename)))
                    {
                        ExcelWorksheet ws = p.Workbook.Worksheets.First();

                        for(int row_counter = 3; row_counter <= ws.Dimension.End.Row; row_counter++)
                        {
                            members.Add(new ListMember(
                                filename,
                                row_counter,
                                new Func<bool>(() =>
                                {
                                    bool to_return = true;
                                    to_return &= ws.Cells[row_counter, 2].Text.Trim() != "";
                                    to_return &= ws.Cells[row_counter, 3].Text.Trim() != "";
                                    to_return &= ws.Cells[row_counter, 4].Text.Trim() != "";
                                    to_return &= ws.Cells[row_counter, 5].Text.Trim() != "";

                                    return to_return;
                                }).Invoke(),
                                ws.Cells[row_counter, 1].Text.Trim() != "" ? Convert.ToInt32(ws.Cells[row_counter, 1].Text) : 0,
                                ws.Cells[row_counter, 2].Text.Trim() != "" ? Convert.ToUInt64(ws.Cells[row_counter, 2].Text) : 0,
                                ws.Cells[row_counter, 6].Text,
                                ws.Cells[row_counter, 7].Text,
                                ws.Cells[row_counter, 4].Text,
                                ws.Cells[row_counter, 3].Text
                                ));
                        }
                }

                return members.ToArray();
            });
        }

        public static void WriteMemberToList(ListMember member)
        {
            Object[] member_values = member.GetValues();

            string filename = (string)member_values[0];
            int row = Convert.ToInt32(member_values[1]);

            byte[] bytes;
            using (ExcelPackage p = new ExcelPackage(new FileInfo(filename)))
            {
                ExcelWorksheet ws = p.Workbook.Worksheets.First();

                ws.Cells[row, 1].Value = member_values[3]; //serial
                ws.Cells[row, 2].Value = member_values[4]; //uid
                ws.Cells[row, 3].Value = member_values[8]; //entry times
                ws.Cells[row, 4].Value = member_values[7]; //entry days
                ws.Cells[row, 5].Value = ws.Cells[row - 1, 5].Value; //copy down options
                ws.Cells[row, 6].Value = member_values[5]; //user
                ws.Cells[row, 7].Value = member_values[6]; //description

                bytes = p.GetAsByteArray();
            }

            File.WriteAllBytes(filename, bytes);
        }

        public static ListMember GenerateNewMember(string filename)
        {
            return new ListMember
                (
                    filename,
                    GetRowRange(filename)+1,
                    false,
                    GetSerialRange(filename)+1,
                    0,
                    "",
                    "",
                    "",
                    ""
                );
        }
    }
}
