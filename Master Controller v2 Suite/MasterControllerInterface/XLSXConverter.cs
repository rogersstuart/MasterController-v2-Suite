using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using MCICommon;
using OfficeOpenXml;

namespace MasterControllerInterface
{
    class XLSXConverter
    {
        public static string BeginConversion(string filename)
        {
            String output_file_name = Path.GetDirectoryName(filename) + "\\" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".csv";

            ConfigurationManager.SessionTempFiles.Add(output_file_name);

            using (StreamWriter csv_out = new StreamWriter(output_file_name))
            using (ExcelPackage package = new ExcelPackage(new FileInfo(filename)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

                if (worksheet.Dimension != null)
                {
                    for (int row_counter = 2; row_counter <= worksheet.Dimension.End.Row; row_counter++)
                    {
                        String current_line = "";
                        for (int column_counter = 1; column_counter <= worksheet.Dimension.End.Column && column_counter < 5; column_counter++)
                        {
                            string cellValue = worksheet.Cells[row_counter, column_counter].Text?.Trim() ?? "";
                            
                            if (cellValue.Equals(""))
                            {
                                Console.WriteLine(cellValue);
                                current_line = "";
                                break;
                            }
                            else
                            {
                                current_line += cellValue;
                                if (column_counter < 4)
                                    current_line += ",";
                            }
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
