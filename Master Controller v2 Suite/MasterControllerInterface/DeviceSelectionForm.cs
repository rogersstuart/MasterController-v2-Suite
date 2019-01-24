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
    public partial class DeviceSelectionForm : Form
    {
        private List<List<Object>> displayed_devices = null;
        private List<int> device_filter = null;

        public DeviceSelectionForm() : this(null)
        {
            
        }

        public DeviceSelectionForm(List<int> filter_by)
        {
            InitializeComponent();

            device_filter = filter_by;  
        }

        private async void DeviceSelectionForm_Shown(object sender, EventArgs e)
        {
            UseWaitCursor = true;
            Refresh();
            
            //get list of devices
            var db_connection = await ARDBConnectionManager.default_manager.CheckOut();

            //the following will return a list of lists containing the following fields
            ////id, type, address, port, alias

            var devices = await DatabaseUtilities.GetDevicesFromDatabase(db_connection.Connection);

            ARDBConnectionManager.default_manager.CheckIn(db_connection);

            if (device_filter != null)
                devices = devices.Where(x => device_filter.Contains(Convert.ToInt32(x[1]))).ToList();

            displayed_devices = devices;

            if (displayed_devices.Count() > 0)
                listBox1.Enabled = true;

            var device_strings = new List<string>();
            foreach(var device_info in devices)
            {
                if (((string)device_info[4]).Trim() == "")
                    device_strings.Add(((ulong)device_info[0]).ToString());
                else
                    device_strings.Add(((string)device_info[4]));
            }

            listBox1.DataSource = device_strings;

            UseWaitCursor = false;
            Refresh();
        }

        public Object[] SelectedDeviceInfo
        {
            get
            {
                return displayed_devices[listBox1.SelectedIndex].ToArray();
            }
        }

        public ulong SelectedDeviceID
        {
            get
            {
                return (ulong)displayed_devices[listBox1.SelectedIndex][0];
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = (((ListBox)sender).SelectedIndex > -1);
        }
    }
}
