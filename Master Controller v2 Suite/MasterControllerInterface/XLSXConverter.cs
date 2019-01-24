using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using Excel;
using MCICommon;

namespace MasterControllerInterface
{
    class XLSXConverter
    {
        public static string BeginConversion(string filename)
        {
            FileStream to_convert = new FileStream(filename, FileMode.Open);

            String output_file_name = Path.GetDirectoryName(filename) + "\\" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".csv";

            ConfigurationManager.SessionTempFiles.Add(output_file_name);

            using (StreamWriter csv_out = new StreamWriter(output_file_name))
            using (IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(to_convert))
            {
                DataSet result = excelReader.AsDataSet();

                foreach (DataTable table in result.Tables)
                {
                    for (int row_counter = 2; row_counter < table.Rows.Count; row_counter++)
                    {
                        String current_line = "";
                        for (int column_counter = 1; column_counter < table.Columns.Count && column_counter < 5; column_counter++)
                            if (table.Rows[row_counter].ItemArray[column_counter].ToString().Trim().Equals(""))
                            {
                                Console.WriteLine(table.Rows[row_counter].ItemArray[column_counter].ToString());

                                current_line = "";
                                break;
                            }
                            else
                            {
                                current_line += table.Rows[row_counter].ItemArray[column_counter];
                                if (column_counter < 4)
                                    current_line += ",";
                            }

                        if (!current_line.Equals(""))
                            csv_out.WriteLine(current_line);
                    }
                }
            }

            return output_file_name;
        }
    }
}
