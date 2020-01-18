using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MCICommon;

namespace MasterControllerInterface
{
    public partial class HomeFloorsEditor : Form
    {
        private ulong[] users_to_edit;

        public HomeFloorsEditor(ulong[] users_to_edit)
        {
            InitializeComponent();

            this.users_to_edit = users_to_edit;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //save button clicked
            List<int> floors = new List<int>();
            foreach (var item in checkedListBox1.CheckedItems)
                floors.Add(Convert.ToInt32((string)item));
            foreach(var user in users_to_edit)
            {
                var ext = new MCIUserExt();
                ext.HomeFloors = floors.ToArray();

                await DatabaseUtilities.SetUserExtensions(user, ext);
            }

            DialogResult = DialogResult.OK;
        }

        private async void HomeFloorsEditor_Shown(object sender, EventArgs e)
        {
            //first shown

            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();

            var names = await DatabaseUtilities.GetTableNames(sqlconn.Connection);
            ARDBConnectionManager.default_manager.CheckIn(sqlconn);

            if (!names.Contains("user_extensions"))
                await DatabaseUtilities.CreateUserExtensionsTable();

            List<int> floors = new List<int>();
            foreach (var user in users_to_edit)
            {
                var ext = await DatabaseUtilities.GetUserExtensions(user);

                if(ext != null)
                    floors.AddRange(ext.HomeFloors);
            }

            floors = floors.Distinct().ToList() ;

            foreach (var floor in floors)
            {
                checkedListBox1.SetItemChecked(floor - 1, true);
            }

            //button1.Enabled = false;

            Refresh();
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
           
        }

        private void checkedListBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                List<int> floors = new List<int>();
                foreach (var item in checkedListBox1.CheckedItems)
                    floors.Add(Convert.ToInt32((string)item));

                List<int> floors2 = new List<int>();
                for (int i = 1; i <= 14; i++)
                    if (floors.Contains(i))
                        continue;
                    else
                        floors2.Add(i);

                for (int i = 1; i <= 14; i++)
                    checkedListBox1.SetItemChecked(i - 1, floors2.Contains(i));

                Refresh();
            }
        }
    }
}
