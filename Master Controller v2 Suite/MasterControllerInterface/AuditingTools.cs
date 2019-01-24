using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MCICommon;

namespace MasterControllerInterface
{
    public static class AuditingTools
    {
        public static async Task GenerateListAudits(string export_directory)
        {
            var dbconn = await ARDBConnectionManager.default_manager.CheckOut();

            var lists = await DatabaseUtilities.GetDictionaryUniqueShortListDescriptionWithLimiter(dbconn.Connection, "", 0);

            Console.WriteLine(lists.Count());

            for(int i = 0; i < lists.Count(); i++)
            {
                List<string> file_lines = new List<string>();

                var list_uid = lists.Keys.ElementAt(i);
                var list_description = lists.Values.ElementAt(i);

                file_lines.Add("List UID: " + list_uid);
                file_lines.Add("List Name: " + list_description);
                file_lines.Add("");
                file_lines.Add("");

                var list_users = await DatabaseUtilities.GetAllUsersInList(dbconn.Connection, list_uid);
                var user_descriptions = await DatabaseUtilities.GetDictionaryDescriptionForAllUsersInListWithLimiter(dbconn.Connection, list_uid, "");

                foreach(var list_user in list_users)
                {
                    string line = "";

                    line += list_user + " ";
                    line += user_descriptions[list_user] + ", ";

                    file_lines.Add(line);

                    var list_entry = await DatabaseUtilities.GetV2ListEntry(list_uid, list_user);

                    line = "Is Enabled? = " + (byte)list_entry[2] + ", ";
                    line += (string)list_entry[0] + ", ";
                    line += (string)list_entry[1] + ", ";

                    file_lines.Add(line);

                    line = "Assigned Cards: ";

                    var assigned_cards = await DatabaseUtilities.GetAllCardsAssociatedWithUser(dbconn.Connection, list_user);

                    foreach(var card in assigned_cards)
                        line += BaseConverter.EncodeFromBase10(card) + ", ";

                    file_lines.Add(line);
                    file_lines.Add("");
                }

                File.WriteAllLines(export_directory + "\\" + list_uid + ".txt", file_lines.ToArray());
            }
        }
    }
}
