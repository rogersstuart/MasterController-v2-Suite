//note: on protocol version 2 systems the log can only be erased by reading the log. this functionality has been restored in the v2.1 revision.
// :( such hax

using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Runtime.InteropServices;
using MCICommon;

namespace MasterControllerInterface
{
    public partial class MasterControllerInterface : Form
    {
        private string[] button1_options = {"Connect","Disconnect"};
        private string[] days_of_the_week = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        private const int page_size = 128;

        private Button[] device_mgmt_buttons;

        private ListManagerForm lm;

        bool cancel_flag = false;

        FileSystemWatcher log_watcher = null;

        public MasterControllerInterface()
        {
            InitializeComponent();

            lm = new ListManagerForm();
            lm.Changed += ActiveListsChanged;

            device_mgmt_buttons = new Button[]{ button2, button3, button4, button5, button1, button6};

            retainTemporaryFilesToolStripMenuItem.Checked = ConfigurationManager.RetainTemporaryFiles;

            SetConnUIState();

            textBox1.GotFocus += (o, e) =>
            {
                HideCaret();
            };
            /*
            Task.Run(async () =>
            {
                UInt32 ctr = 0;

                while (true)
                {
                    ConfigurationManager.AppendLog(DateTime.Now, ctr + "");
                    ctr++;

                    await Task.Delay(100);
                }
            });
            */
        }

        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);
        public void HideCaret()
        {
            HideCaret(textBox1.Handle);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;    // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        private async Task InitLogView()
        {
            await PrintLogToForm();

            if (log_watcher != null)
            {
                log_watcher.Dispose();
                log_watcher = null;
            }

            log_watcher = new FileSystemWatcher();
            log_watcher.Path = new FileInfo(ConfigurationManager.LogPath).DirectoryName;
            log_watcher.NotifyFilter = NotifyFilters.LastWrite;
            log_watcher.Filter = new FileInfo(ConfigurationManager.LogPath).Name;

            log_watcher.Changed += new FileSystemEventHandler((o, e) =>
            {
                Invoke((MethodInvoker) (async () =>
                {
                    await PrintLogToForm();
                }));
            });

            log_watcher.EnableRaisingEvents = true;
        }

        private async Task PrintLogToForm()
        {
            textBox1.Lines = await ConfigurationManager.ReadAllLogLines();

            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.SelectionLength = 0;
            textBox1.ScrollToCaret();
        }

        private async Task BeginTransaction(Func<Func<Task<Stream>>, Action, Task> transaction)
        {
            SerialPort serial = null;
            TcpClient client = null;

            bool error_flag = false;

            //button1.Enabled = true;
            cancel_flag = false;

            menuStrip1.Enabled = false;
            UseWaitCursor = true;

            //begin operation
            Func<Task<Stream>> generate_stream = async () =>
            {
                switch (ConfigurationManager.ConnectionType)
                {
                    case 0:
                        setLBLText(label1,"Opening " + ConfigurationManager.SelectedCOMPort);
                        serial = beginSerial(ConfigurationManager.SelectedCOMPort);
                        if (serial != null)
                        {
                            setLBLText(label1, ConfigurationManager.SelectedCOMPort + " Opened");
                            return serial.BaseStream;
                        }
                        else
                            error_flag = true;
                        break;

                    case 1:
                        client = new TcpClient();
                        client.ReceiveTimeout = 2000;
                        client.SendTimeout = 2000;

                        SetConnUIState(false);

                        try
                        {
                            IPEndPoint end_point = await ConfigurationManager.SelectedTCPConnection.GetIPEndPointAsync();

                            setLBLText(label1, "Connecting To: " + ConfigurationManager.SelectedTCPConnection.ToString());
                            await client.ConnectAsync(end_point.Address, end_point.Port);
                            setLBLText(label1, "Connected To: " + ConfigurationManager.SelectedTCPConnection.ToString());
                        }
                        catch (Exception ex)
                        {
                            setLBLText(label1, "Failed To Connect To: " + ConfigurationManager.SelectedTCPConnection.ToString());

                            error_flag = true;
                        }

                        if (client != null && client.Connected)
                            return client.GetStream();
                        break;

                    default:
                        MessageBox.Show(this, "Internal: Invalid Connection Type");
                        error_flag = true;
                        break;
                }

                return null;
            };

            Action cleanup_connection = () =>
            {
                //close connection
                switch (ConfigurationManager.ConnectionType)
                {
                    case 0:
                        if (serial != null && serial.IsOpen)
                            endSerial(serial);
                        serial = null;
                        break;

                    case 1:
                        if (client != null && client.Connected)
                            client.Close();
                        client = null;
                        break;

                    default:
                        MessageBox.Show(this, "Internal: Invalid Connection Type");
                        error_flag = true;
                        break;
                }
            };

            if (!error_flag)
            try
            {
                await transaction(generate_stream, cleanup_connection);
            }
            catch(Exception ex)
            {
                //an error occured, pass it on
                error_flag = true;
            }

            cleanup_connection();

            if (!ConfigurationManager.RetainTemporaryFiles)
                ConfigurationManager.EraseTemporaryFiles();
            else
                ConfigurationManager.SessionTempFiles.Clear();

            //end operation

            UseWaitCursor = false;
            menuStrip1.Enabled = true;

            if (error_flag)
                throw new Exception();
        }
         
        //upload card list
        private async void button2_Click(object sender, EventArgs e)
        {
            SetConnUIState(false);

            try
            {
                await BeginTransaction(async (x, y) =>
                {
                    label1.Text = "Uploading Card List";
                    await CardListUploadAsync(x, y);
                    label1.Text = "The Card List Was Uploaded Successfully";
                });
            }
            catch(Exception ex)
            {
                label1.Text = "An Error Occured While Uploading The Card List";
            }

            SetConnUIState(true);
        }

        private void setLBLText(Label target, string text)
        {
            Invoke((MethodInvoker)( () => { target.Text = text; }));
        }

        //read log
        private async void button3_Click(object sender, EventArgs e)
        {
            SetConnUIState(false);

            try
            {
                await BeginTransaction(async (x, y) =>
                {
                    label1.Text = "Reading Log";
                    await ReadLogAsync(x, y);
                    label1.Text = "The Log Was Successfully Read";
                });
            }
            catch(Exception ex){label1.Text = "An Error Occured While Reading The Log";}

            SetConnUIState(true);
        }

        //set time
        private async void button4_Click(object sender, EventArgs e)
        {
            SetConnUIState(false);

            try
            {
                await BeginTransaction(async (x, y) =>
                {
                    label1.Text = "Setting Time";
                    await setTime(x, y);
                    label1.Text = "The Time Was Set Successfully";
                });
            }
            catch (Exception ex){label1.Text = "An Error Occured While Setting The Time";}

            SetConnUIState(true);
        }

        //erase log
        private async void button5_Click(object sender, EventArgs e)
        {
            SetConnUIState(false);

            try
            {
                await BeginTransaction(async (x, y) =>
                {
                    label1.Text = "Erasing Log";
                    await eraseLog(x, y);
                    label1.Text = "Log Erased";
                });
            }
            catch (Exception ex){label1.Text = "An Error Occured While Erasing The Log";}

            SetConnUIState(true);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //if(com_connection != null && com_connection.IsOpen)
            //    com_connection.Close();
        }

        private SerialPort beginSerial(string port_name)
        {
            try
            {
                SerialPort com_connection = new SerialPort(port_name, 115200, Parity.None, 8, StopBits.One);
                com_connection.NewLine = "\r\n";
                com_connection.ReadTimeout = 2000;
                com_connection.WriteTimeout = 2000;

                com_connection.Open();

                return com_connection;
            }
            catch(Exception ex)
            {
                label1.Text = "An Error Occured While Opening " + port_name;
                MessageBox.Show("Error Opening COM Port" + Environment.NewLine + ex.ToString());
                return null;
            }
        }

        private void endSerial(SerialPort com_connection)
        {
            com_connection.Close();

            SetConnUIState(false);
            progressBar1.Enabled = false;
        }

        private bool TimeCheck()
        {
            if (DateTime.Now.DayOfWeek > DayOfWeek.Sunday && DateTime.Now.DayOfWeek < DayOfWeek.Saturday)
                if (DateTime.Now.TimeOfDay >= new TimeSpan(7, 45, 0) && DateTime.Now.TimeOfDay <= new TimeSpan(17, 45, 0))
                    return true;

            return false;
        }

        private async Task eraseLog(Func<Task<Stream>> connection, Action cleanup)
        {
            await readAccessLog(connection, cleanup, "disposed_log.txt");
        }

        private async Task setTime(Func<Task<Stream>> stream_generator, Action cleanup)
        {
            progressBar1.Maximum = 2;

            //issue serial commands to begin operation

            await Task.Run(async () =>
            {
                int i = 0;
                for (; i < 3; i++)
                {
                    try
                    {
                        Invoke((MethodInvoker)(() => { progressBar1.Value = 0; }));

                        if (await ConfigurationManager.GetProtocolVersion() == 1)
                        {
                            (await ManagedStream.GetStream()).WriteByte((byte)'?');

                            await Task.Delay(500);

                            (await ManagedStream.GetStream()).WriteByte((byte)'C');
                        }
                        else
                            if (await ConfigurationManager.GetProtocolVersion() == 0)
                                {
                                    StreamWriter stw = new StreamWriter((await ManagedStream.GetStream()));

                                    stw.AutoFlush = true;
                                    stw.NewLine = "\r\n";

                                    stw.BaseStream.WriteByte((byte)'?');
                                    await Task.Delay(1000);
                                    stw.WriteLine("write");
                                    await Task.Delay(1000);
                                    stw.WriteLine("rtc");
                                    await Task.Delay(1000);
                                    stw.WriteLine("raw");
                                    await Task.Delay(500);
                                }

                        Invoke((MethodInvoker)(() => { progressBar1.PerformStep(); }));

                        await Task.Delay(500);

                        (await ManagedStream.GetStream()).Write(BinaryGenerator.GenerateRTCBytes(), 0, 7);

                        try
                        {
                            DateTime controller_time = await ReadTime();

                            Console.WriteLine(controller_time.ToString());

                            if (controller_time < (DateTime.Now - TimeSpan.FromSeconds(30)) || controller_time > (DateTime.Now + TimeSpan.FromSeconds(30)))
                                continue;
                            else
                                break;
                        }
                        catch (InvalidOperationException ex)
                        {
                            break;
                        }
                    }
                    catch(Exception ex)
                    {

                    }
                }

                if (i == 3)
                    throw new Exception("Unable To Set The Time");
            });

            progressBar1.PerformStep();

            await Task.Delay(200);

            progressBar1.Value = 0;
        }

        private async Task readAccessLog(Func<Task<Stream>> stream_generator, Action cleanup, string output_filename)
        {
            if (await ConfigurationManager.GetProtocolVersion() == 0)
                throw new Exception("This function is incompatible with protocol version 1.0.");

            byte[] log_buffer = new byte[128000]; //elements default to zero
            bool error_flag = false;

            //determine version compatibilty
            bool turbo_requires_reversal = false;

            bool[] flags = new bool[2];

            int minor_rev = 0;

            await Task.Run(async () =>
            {

                //if (await ConfigurationManager.GetProtocolVersion() == 1)
                if(true)
                {
                    setLBLText(label1, "Determining Minor Version Support");

                    //determine if the protocol version is 2 or 2.1
                    try
                    {
                        if (TimeCheck())
                        {
                            //the doors are unlocked so let's speed things up
                            flags = await GetOverrideFlags();
                            if (!flags[0])
                            {
                                flags[0] = true;
                                turbo_requires_reversal = true;
                            }

                            await SetOverrides(flags);

                            setLBLText(label1, "Turbo Enabled");
                        }

                        (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface

                        (await ManagedStream.GetStream()).WriteByte((byte)'S');

                        (await ManagedStream.GetStream()).ReadByte();

                        minor_rev = 1;

                        setLBLText(label1, "Enhanced Features Are Supported By This Device");

                        Invoke((MethodInvoker)(() => { progressBar1.Maximum = (128000 - 144) / 16; }));

                        Console.WriteLine("Beginning v2.1 log read.");
                    }
                    catch (Exception ex)
                    {
                        Invoke((MethodInvoker)(() => { progressBar1.Maximum = 1000; }));

                        setLBLText(label1, "Enhanced Features Are Unavailable On This Device");

                        //if an exception was cought then the minor protocol version is 0.

                        await Task.Delay(500);
                    }

                    if (minor_rev == 0)
                    {
                        (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface

                        await Task.Delay(500);

                        (await ManagedStream.GetStream()).WriteByte((byte)'R');
                    }
                }

                try
                {
                    if (minor_rev == 0)
                    {
                        for (int page_counter = 0; page_counter < 1000; page_counter++)
                        {
                            setLBLText(label1, "Reading Log " + page_counter + "/1000");

                            int err_ctr = 0;
                            for (; err_ctr < 3; err_ctr++)
                            {
                                try
                                {
                                    (await ManagedStream.GetStream()).Read(log_buffer, page_counter * page_size, page_size); //read in memory page
                                    break;
                                }
                                catch(Exception ex)
                                { }
                            }
                            if (err_ctr == 3)
                                throw new Exception("An error occured while reading the log");

                            await Task.Delay(10);

                            Invoke((MethodInvoker)(() => { progressBar1.PerformStep(); }));
                        }
                    }
                    else
                        if (minor_rev == 1)
                        {
                            for (int i = 0; i < (128000 - 144) / 16; i++)
                            {
                                setLBLText(label1, "Reading Log " + i + "/" + ((128000 - 144) / 16));

                                Array.Copy(await ReadLogEntry((ushort)i), 0, log_buffer, i * 16, 16);

                                Invoke((MethodInvoker)(() => { progressBar1.PerformStep(); }));
                            }
                        }
                }
                catch (Exception ex)
                {
                    error_flag = true;
                }

                if (turbo_requires_reversal)
                {
                    setLBLText(label1, "Disabling Turbo");

                    flags[0] = false;
                    await SetOverrides(flags);
                }

            });

            BinaryTranslator.ParseAccessLog(output_filename, log_buffer); //generate the text file

            progressBar1.Value = 0;

            if (error_flag)
                throw new Exception();
        }

        private async Task SetOverrides(bool[] lock_override_flags)
        {
            await Task.Run(async () =>
            {
                //set door control outputs command 'A'
                int i = 0;
                for (; i < 3; i++)
                {
                    try
                    {
                        (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface
                        (await ManagedStream.GetStream()).WriteByte((byte)'A');

                        if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                            throw new Exception();

                        (await ManagedStream.GetStream()).WriteByte(Convert.ToByte((lock_override_flags[0] ? 1 : 0) | ((lock_override_flags[1] ? 1 : 0) << 1)));

                        if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'!') //read !
                            throw new Exception();

                        break;
                    }
                    catch (Exception ex)
                    {
                        await Task.Delay(500);
                    }
                }

                if (i == 3)
                    throw new Exception();
            });
        }

        private async Task<bool[]> GetOverrideFlags()
        {
            return await Task.Run(new Func<Task<bool[]>>(async () =>
            { 
                //get door control outputs command 'Z'
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface
                        (await ManagedStream.GetStream()).WriteByte((byte)'Z');

                        int response = (await ManagedStream.GetStream()).ReadByte();

                        if (response == -1)
                            throw new Exception("No Response");
                        else
                            return new bool[] { Convert.ToBoolean((response >> 2) & 1), Convert.ToBoolean((response >> 3) & 1) };
                    }
                    catch (Exception ex)
                    {
                        await Task.Delay(500);
                    }
                }

                throw new Exception();
            }));
        }

        //mode, 0 = old version, 1 = new version
        private async Task writeIDList(Func<Task<Stream>> stream_generator, Action cleanup, string binarylist_filename)
        {
            //each page is 128 bytes. 1000 pages can fit into memory.
            long pages_to_write = new FileInfo(binarylist_filename).Length / page_size;

            progressBar1.Maximum = (int)pages_to_write;

            bool error_flag = false;
            bool turbo_requires_reversal = false;

            bool[] flags = new bool[2];

            //int minor_rev = 0;
            int minor_rev = 1;

            await Task.Run(async () =>
            {
                /*
                if (await ConfigurationManager.GetProtocolVersion() == 1)
                {
                    setLBLText(label1, "Determining Minor Version Support");

                    //determine if the protocol version is 2 or 2.1
                    try
                    {
                        if(TimeCheck())
                        {
                            //the doors are unlocked so let's speed things up
                            flags = await GetOverrideFlags();
                            if (!flags[0])
                            {
                                flags[0] = true;
                                turbo_requires_reversal = true;
                            }

                            await SetOverrides(flags);

                            setLBLText(label1, "Turbo Enabled");
                        }
                        
                        (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface

                        (await ManagedStream.GetStream()).WriteByte((byte)'S');

                        (await ManagedStream.GetStream()).ReadByte();

                        minor_rev = 1;

                        setLBLText(label1, "Enhanced Features Are Supported By This Device");
                    }
                    catch (Exception ex)
                    {
                        setLBLText(label1, "Enhanced Features Are Unavailable On This Device");

                        //if an exception was cought then the minor protocol version is 0.

                        await Task.Delay(500);
                    }

                    if (minor_rev == 0)
                    {
                        (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface

                        await Task.Delay(500);

                        (await ManagedStream.GetStream()).WriteByte((byte)'L');

                        await Task.Delay(250);
                    }
                }
                else
                    if (await ConfigurationManager.GetProtocolVersion() == 0)
                    {
                        StreamWriter conn_writer = new StreamWriter((await ManagedStream.GetStream()));

                        conn_writer.AutoFlush = true;
                        conn_writer.NewLine = "\r\n";

                        conn_writer.BaseStream.WriteByte((byte)'?');
                        await Task.Delay(1000);
                        conn_writer.WriteLine("write");
                        await Task.Delay(1000);
                        conn_writer.WriteLine("id eeprom");
                        await Task.Delay(1000);
                        conn_writer.WriteLine("raw");
                        await Task.Delay(1000);
                    }
                */
                try
                {
                    using (BinaryReader binaryliststream = new BinaryReader(new FileStream(binarylist_filename, FileMode.Open)))
                    {
                        byte[] page_buffer = new byte[128];

                        UInt16 page_counter = 0;
                        bool cycle_completion_flag = false;

                        while (true)
                        {
                            try
                            {
                                while (page_counter < 1000)
                                {
                                    if (page_counter < pages_to_write)
                                        if (!cycle_completion_flag)
                                        {
                                            binaryliststream.Read(page_buffer, 0, page_size);
                                            cycle_completion_flag = true;
                                        }

                                    if (minor_rev == 0 || page_counter < pages_to_write)
                                    {
                                        setLBLText(label1, "List Uploading Is Now In Progress " + ((float)page_counter / (minor_rev == 0 ? 1000 : pages_to_write)) * 100 + "%");

                                        if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                                            throw new Exception();

                                        if (minor_rev == 1)
                                            (await ManagedStream.GetStream()).Write(BitConverter.GetBytes(page_counter).Reverse().ToArray(), 0, 2);
                                    }

                                    if (page_counter < pages_to_write)
                                    {
                                        //(await ManagedStream.GetStream()).Write(page_buffer, 0, page_size);

                                        if (minor_rev == 1)
                                        {
                                            //(await ManagedStream.GetStream()).WriteByte(CRC8(page_buffer, 128));

                                            //if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'!') //read ?
                                            //    throw new Exception(); //read ! (operation completion flag)

                                            if (await VerifyListPage(null, stream_generator, cleanup, page_counter, page_buffer) == false)
                                                ConfigurationManager.AppendLog(DateTime.Now, "Difference on page " + page_counter);
                                                //throw new Exception();

                                            //if (page_counter + 1 < pages_to_write)
                                            //{
                                            //    (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface
                                            //    (await ManagedStream.GetStream()).WriteByte((byte)'S'); //issue command

                                            //    if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                                            //        throw new Exception();
                                            //}
                                        }
                                    }
                                    else
                                        if (minor_rev == 0)
                                    {
                                        byte[] null_buffer = new byte[128];
                                        (await ManagedStream.GetStream()).Write(null_buffer, 0, page_size);
                                    }
                                    /*
                                    else
                                            if (minor_rev == 1)
                                            {
                                                setLBLText(label1, "Zeroing Out of Bounds List Memory");

                                                (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface
                                                (await ManagedStream.GetStream()).WriteByte((byte)'N'); //issue command

                                                if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                                                    throw new Exception();

                                                (await ManagedStream.GetStream()).Write(BitConverter.GetBytes(page_counter).Reverse().ToArray(), 0, 2);
                                                (await ManagedStream.GetStream()).Write(BitConverter.GetBytes((UInt16)999).Reverse().ToArray(), 0, 2);

                                                await Task.Delay((1000 - page_counter) * 10);

                                                if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'!') //read !
                                                    throw new Exception();

                                                break;
                                            }
                                    */
                                    Invoke((MethodInvoker)(() => { progressBar1.PerformStep(); }));

                                    cycle_completion_flag = false;

                                    page_counter++;
                                }

                                break;
                            }
                            catch (Exception ex2)
                            {
                                if (minor_rev == 1)
                                {
                                    int i = 0;
                                    for (; i < 3; i++)
                                    {
                                        try
                                        {
                                            setLBLText(label1, "Automatic Reconnection Attempt " + (i + 1) + " of 3");

                                            Console.WriteLine("a");

                                            if (page_counter < pages_to_write)
                                            {
                                                (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface
                                                (await ManagedStream.GetStream()).WriteByte((byte)'S');

                                                if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                                                    throw new Exception();
                                            }

                                            break;
                                        }
                                        catch (Exception ex3) { }
                                    }

                                    if (i == 3)
                                        throw;
                                }
                                else
                                    throw;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    error_flag = true;
                }
            });

            if (turbo_requires_reversal)
            {
                setLBLText(label1, "Disabling Turbo");

                flags[0] = false;
                await SetOverrides(flags);
            }

            progressBar1.Value = 0;

            if (error_flag)
            {
                throw new Exception();
            }
        }

        private async Task<byte[]> ReadLogEntry(UInt16 entry_number)
        {
            return await Task.Run(new Func<Task<byte[]>>(async () =>
            {
                byte[] rx_buffer = new byte[16];

                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface
                        (await ManagedStream.GetStream()).WriteByte((byte)'B');

                        if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                            throw new Exception("Did not receive a request response from the controller.");

                        (await ManagedStream.GetStream()).Write(BitConverter.GetBytes(entry_number).Reverse().ToArray(), 0, 2);

                        if ((await ManagedStream.GetStream()).Read(rx_buffer, 0, 16) < 16)
                            throw new Exception("Unable to read log entry from stream.");

                        int crc = (await ManagedStream.GetStream()).ReadByte();

                        if (crc == -1)
                            throw new Exception("The crc was invalid.");

                        if (CRC8(rx_buffer, 16) != (byte)crc)
                            throw new Exception("The crc was incorrect.");

                        (await ManagedStream.GetStream()).WriteByte((byte)'!');

                        return rx_buffer;
                    }
                    catch (Exception ex)
                    {
                        await Task.Delay(1000);
                    }
                }

                throw new Exception("Unable to read log entry " + entry_number + ".");
            }));
        }

        private async Task<bool> VerifyListPage(Stream working_stream, Func<Task<Stream>> stream_generator, Action cleanup, UInt16 page_index, byte[] compare_to)
        {
            try
            {
                byte[] remote_bin_buffer = new byte[128];

                while (true)
                {
                    try
                    {
                        (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface
                        (await ManagedStream.GetStream()).WriteByte((byte)'K');

                        if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                            throw new Exception();

                        (await ManagedStream.GetStream()).Write(BitConverter.GetBytes(page_index).Reverse().ToArray(), 0, 2);

                        if ((await ManagedStream.GetStream()).Read(remote_bin_buffer, 0, page_size) < 128)
                            throw new Exception();

                        byte crc = (byte)(await ManagedStream.GetStream()).ReadByte();

                        if (CRC8(remote_bin_buffer, 128) != crc)
                            throw new Exception();

                        if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                            throw new Exception();
                        else
                            (await ManagedStream.GetStream()).WriteByte((byte)'!');

                        return remote_bin_buffer.SequenceEqual(compare_to);
                    }
                    catch (Exception ex2)
                    {
                        int i = 0;
                        for (; i < 3; i++)
                        {
                            try
                            {
                                setLBLText(label1, "Automatic Reconnection Attempt " + (i + 1) + " of 3");

                                Console.WriteLine("a2");

                                await ManagedStream.GetStream();

                                break;
                            }
                            catch (Exception ex3) { }
                        }

                        if (i == 3)
                            throw;
                    }
                }
            }
            catch (Exception ex)
            {
                
            }

            return false;
        }

        private async Task<DateTime> ReadTime()
        {
            byte[] rtc_bytes = new byte[7];

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    (await ManagedStream.GetStream()).WriteByte((byte)'?'); //enter the management interface
                    (await ManagedStream.GetStream()).WriteByte((byte)'T');

                    if ((await ManagedStream.GetStream()).Read(rtc_bytes, 0, 7) < 7)
                        throw new Exception();

                    int crc = (await ManagedStream.GetStream()).ReadByte();

                    if (crc == -1)
                        throw new Exception();

                    if (CRC8(rtc_bytes, 7) != (byte)crc)
                        throw new Exception();

                    if ((byte)(await ManagedStream.GetStream()).ReadByte() != (byte)'?') //read ?
                        throw new Exception();
                    else
                        (await ManagedStream.GetStream()).WriteByte((byte)'!');

                    return BinaryTranslator.TranslateRTCBytes(rtc_bytes);
                }
                catch (Exception ex)
                {
                    
                }
            }

            throw new InvalidOperationException("This Operation Isn't Supported");
        }

        private void ActiveListsChanged(object sender, string[] lists)
        {
            if (lists.Length == 0)
                button2.Enabled = false;
            else
                if (button4.Enabled == true)
                    button2.Enabled = true;
        }

        private async Task SetConnUIState()
        {
            bool en = false;

            if (ConfigurationManager.ConnectionType == 0 || ConfigurationManager.ConnectionType == 1)
                if (await ConfigurationManager.GetProtocolVersion() == 0 || await ConfigurationManager.GetProtocolVersion() == 1)
                    if (ConfigurationManager.SelectedCOMPort != "" || ConfigurationManager.SelectedTCPConnection != null)
                        en = true;

            SetConnUIState(en);
        }

        private async Task SetConnUIState(bool en)
        {
            bool activeListsExist = lm.CheckedLists.Length > 0 ? true : false;

            foreach (Button b in device_mgmt_buttons)
            {
                if (b == button2)
                    b.Enabled = activeListsExist ? en : false;
                else
                    b.Enabled = en;

                if (b == button3 || b == button5)
                    b.Enabled = await ConfigurationManager.GetProtocolVersion() == 0 ? false : en;
            }

            progressBar1.Enabled = en;

            //UseWaitCursor = en ? false : (com_connection != null && com_connection.IsOpen);
        }

        private void cOMPortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //com port connection, no options here
        }

        private async void tCPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //tcp port connection
            using (TCPConnectionDialog tcd = new TCPConnectionDialog())
            {
                if (tcd.ShowDialog() == DialogResult.OK)
                {
                    ConfigurationManager.SelectedTCPConnection = tcd.ConnectionProperties;
                    ConfigurationManager.ConnectionType = 1;

                    SetConnUIState(true);
                }
            }
        }

        private void COMPortSelected(object sender, EventArgs e)
        {
            ConfigurationManager.SelectedCOMPort = ((ToolStripMenuItem)sender).Text;
            ConfigurationManager.ConnectionType = 0;
            ConfigurationManager.SelectedTCPConnection = null;

            SetConnUIState(true);
        }

        private void TCPHistoryItemSelected(object sender, EventArgs e)
        {
            ConfigurationManager.SelectedTCPConnection = ((TCPConnectionHistoryMenuItem)sender).TCPConnectionProperties;
            ConfigurationManager.ConnectionType = 1;
            ConfigurationManager.SelectedCOMPort = "";

            SetConnUIState(true);
        }

        private async void cOMPortToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            //com port dropdown is opening, populate the list
            
        }

        private async Task ReadLogAsync(Func<Task<Stream>> connection, Action cleanup)
        {
            saveFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveFileDialog1.FileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                await readAccessLog(connection, cleanup, saveFileDialog1.FileName);
        }


        private async Task CardListUploadAsync(Func<Task<Stream>> connection, Action cleanup)
        {
            ListSelectionForm lsf = new ListSelectionForm(lm.CheckedLists);

            bool error_flag = false;
            if (lsf.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string file_name = "";

                    if (new FileInfo(lsf.SelectedOption).Name.Contains(".bin"))
                        file_name = lsf.SelectedOption;
                    else
                        if (new FileInfo(lsf.SelectedOption).Name.Contains(".xlsx"))
                        file_name = await Task.Run(() => BinaryGenerator.GenerateIDList(XLSXConverter.BeginConversion(lsf.SelectedOption)));
                    else
                            if (new FileInfo(lsf.SelectedOption).Name.Contains(".csv"))
                        file_name = await Task.Run(() => BinaryGenerator.GenerateIDList(lsf.SelectedOption));

                    while (true)
                        try
                        {
                            await writeIDList(connection, cleanup, file_name);
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (MessageBox.Show(this, "Failed To Upload Card List. Retry?", "Error", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                            {
                                error_flag = true;
                                break;
                            }

                            await Task.Delay(6000);
                        }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Reading File" + Environment.NewLine + ex.ToString());
                    error_flag = true;
                }
            }
            else
                error_flag = true;

            if (error_flag)
                throw new Exception();
        }

        private async void tCPToolStripMenuItem_MouseEnter(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                TCPConnectionProperties[] tcpconnprops = ConfigurationManager.TCPConnectionHistory;

                List<TCPConnectionHistoryMenuItem> options = new List<TCPConnectionHistoryMenuItem>();
                foreach (TCPConnectionProperties tcpconnprop in tcpconnprops)
                {
                    TCPConnectionHistoryMenuItem menu_item = new TCPConnectionHistoryMenuItem(tcpconnprop);

                    if (ConfigurationManager.SelectedTCPConnection != null && ConfigurationManager.SelectedTCPConnection.Equals(tcpconnprop))
                        menu_item.Checked = true;

                    menu_item.Click += TCPHistoryItemSelected;

                    options.Add(menu_item);
                }

                Invoke(new Action(() =>
                {
                    tCPToolStripMenuItem.DropDownItems.Clear();
                    tCPToolStripMenuItem.DropDownItems.AddRange(options.ToArray());
                }));
            });
        }

        private async void cOMPortToolStripMenuItem_MouseEnter(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                string[] port_names = SerialPort.GetPortNames();
                List<ToolStripMenuItem> port_options = new List<ToolStripMenuItem>();
                foreach (string port_name in port_names)
                {
                    ToolStripMenuItem menu_item = new ToolStripMenuItem(port_name);

                    if (ConfigurationManager.SelectedCOMPort != "" && ConfigurationManager.SelectedCOMPort == port_name)
                        menu_item.Checked = true;

                    using (SerialPort test_port = new SerialPort(port_name))
                        menu_item.Enabled = !test_port.IsOpen;

                    menu_item.Click += COMPortSelected;
                    port_options.Add(menu_item);
                }

                if (port_options.Count < 1)
                {
                    ToolStripMenuItem menu_item = new ToolStripMenuItem("None Available");
                    menu_item.Enabled = false;
                    port_options.Add(menu_item);
                }

                Invoke(new Action(() =>
                {
                    cOMPortToolStripMenuItem.DropDownItems.Clear();
                    cOMPortToolStripMenuItem.DropDownItems.AddRange(port_options.ToArray());
                }));
            });
        }

        private void connectToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem tsmi in connectToolStripMenuItem.DropDownItems)
                tsmi.Checked = false;

            if (ConfigurationManager.ConnectionType == 0 && ConfigurationManager.SelectedCOMPort != "")
            {
                if (SerialPort.GetPortNames().Contains(ConfigurationManager.SelectedCOMPort))
                    cOMPortToolStripMenuItem.Checked = true;
                else
                    ConfigurationManager.SelectedCOMPort = "";
            }
            else
                if (ConfigurationManager.ConnectionType == 1 && ConfigurationManager.SelectedTCPConnection != null)
                    tCPToolStripMenuItem.Checked = true;

            //preload the dropdown items, note: does this happen twice?
            cOMPortToolStripMenuItem_MouseEnter(null, EventArgs.Empty);
            tCPToolStripMenuItem_MouseEnter(null, EventArgs.Empty);
        }

        private void clearConnectionHistoryToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ConfigurationManager.ClearTCPConnectionHistory();

            ConfigurationManager.SelectedTCPConnection = null;
        }

        private void listManagerToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            lm.ShowDialog();
        }

        //cancel transaction
        private void button1_Click(object sender, EventArgs e)
        {
            cancel_flag = true;
        }

        private void retainTemporaryFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            retainTemporaryFilesToolStripMenuItem.Checked = !retainTemporaryFilesToolStripMenuItem.Checked;
            ConfigurationManager.RetainTemporaryFiles = retainTemporaryFilesToolStripMenuItem.Checked;
        }

        //about
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "Master Controller Interface v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()
                + Environment.NewLine + "Written by Stuart Rogers for Mid City Tower LLC", "About", MessageBoxButtons.OK);
        }

        public static byte CRC8(byte[] data, int len)
        {
            byte crc = 0x00;
            int data_index_counter = 0;
            while (len > 0)
            {
                len--;
                byte extract = data[data_index_counter];
                data_index_counter++;

                for (byte tempI = 8; tempI > 0; tempI--)
                {
                    byte sum = Convert.ToByte((crc ^ extract) & 1);
                    crc >>= 1;
                    if (sum > 0)
                    {
                        crc ^= 0x8C;
                    }
                    extract >>= 1;
                }
            }
            return crc;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //show output monitor form

            new OutputMonitorForm().ShowDialog();
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            DateTime controller_time = await ReadTime();

            MessageBox.Show(this, controller_time.ToString());
        }

        private void tbnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new SimpleDatabaseEditor().ShowDialog();
        }

        private async void MasterControllerInterface_Shown(object sender, EventArgs e)
        {
            await InitLogView();
        }

        private void userEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new UserEditorForm_Dep().ShowDialog();
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void importToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new ImportForm().ShowDialog(this);
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ExportForm().ShowDialog(this);
        }

        private void dBManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new MCIv2Form().ShowDialog(this);
        }
    }
}
