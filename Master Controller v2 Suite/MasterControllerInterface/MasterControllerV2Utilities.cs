using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using MCICommon;
using UIElements;

namespace MasterControllerInterface
{
    public static class MasterControllerV2Utilities
    {
        private static int page_size = 128;

        public static async Task<object[]> VerifyList(ulong list_id, ulong device_id, ProgressDialog pgd)
        {
            int verification_index = 0;

            pgd.LabelText = "Compiling List From Database";

            byte[] compiled_list = await ListCompiler.GenerateGen2List(list_id);

            pgd.Step();
            await Task.Delay(100);

            pgd.Reset();
            pgd.LabelText = "Converting List Into Binary Format";

            byte[] binary_list = BinaryGenerator.GenerateIDList(compiled_list);

            pgd.Step();
            await Task.Delay(100);
            pgd.Reset();

            pgd.LabelText = "Acquiring Device Connection";

            ManagedStreamV2 stream_gen = await ManagedStreamV2.GenerateInstance(device_id);

            pgd.Step();
            await Task.Delay(100);
            pgd.Reset();

            int pages_available_to_compare = binary_list.Length / page_size;

            pgd.Maximum = pages_available_to_compare;
            pgd.LabelText = "Beginning List Verification";

            await Task.Run(async () =>
            {
                try
                {
                    for (; verification_index < pages_available_to_compare; verification_index++)
                    {
                        byte[] comparing_to = new byte[128];

                        if (verification_index < pages_available_to_compare)
                            comparing_to = binary_list.SubArray(verification_index * 128, 128);

                        if (await VerifyListPage(stream_gen, (ushort)verification_index, comparing_to))
                        {
                            double pdone = (verification_index + 1);
                            pdone /= pages_available_to_compare;
                            pdone *= 100;

                            pgd.LabelText = "Memory page comparison in progress. " + (int)pdone + "% Complete";
                            pgd.Step();
                        }
                        else
                            break;
                    }
                }
                catch (Exception ex) { }
            });

            if (verification_index >= pages_available_to_compare)
                return new object[] { true, "The list was verified. All pages match." };
            else
                return new object[] { false, "Verification failed. Page " + (verification_index + 1) + " in controller memory does not match the local copy." };
        }

        public static async Task<bool> UploadList(UInt64 list_id, ulong device_id, ProgressDialog pgd)
        {
            pgd.LabelText = "Compiling List From Database";

            byte[] compiled_list = await ListCompiler.GenerateGen2List(list_id);

            pgd.Step();
            await Task.Delay(100);

            pgd.Reset();
            pgd.LabelText = "Converting List Into Binary Format";

            byte[] binary_list = BinaryGenerator.GenerateIDList(compiled_list);

            pgd.Step();
            await Task.Delay(100);
            pgd.Reset();

            pgd.LabelText = "Acquiring Device Connection";

            ManagedStreamV2 stream_gen = await ManagedStreamV2.GenerateInstance(device_id);

            pgd.Step();
            await Task.Delay(100);
            pgd.Reset();

            pgd.LabelText = "Detecting Controller Revision";

            //each page is 128 bytes. 1000 pages can fit into memory.
            long pages_to_write = binary_list.Length / page_size;

            //progressBar1.Maximum = (int)pages_to_write;

            bool error_flag = false;
            //bool turbo_requires_reversal = false;

            //bool[] flags = new bool[2];

            int minor_rev = 0;

            await Task.Run(async () =>
            {
                //setLBLText(label1, "Determining Minor Version Support");

                //determine if the protocol version is 2 or 2.1
                try
                {
                    /*
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
                    */

                    (await stream_gen.GetStream()).WriteByte((byte)'?'); //enter the management interface

                    (await stream_gen.GetStream()).WriteByte((byte)'S');

                    (await stream_gen.GetStream()).ReadByte();

                    minor_rev = 1;

                    //setLBLText(label1, "Enhanced Features Are Supported By This Device");
                }
                catch (Exception ex)
                {
                    //if an exception was cought then the minor protocol version is 0.
                }

                if (minor_rev == 0)
                {
                    (await stream_gen.GetStream()).WriteByte((byte)'?'); //enter the management interface

                    await Task.Delay(500);

                    (await stream_gen.GetStream()).WriteByte((byte)'L');

                    await Task.Delay(250);
                }

                pgd.Step();
                pgd.LabelText = minor_rev == 0 ? "A Revision 0 Controller Has Been Detected" : "A Revision 1 Controller Has Been Detected";
                await Task.Delay(100);
                pgd.Reset();

                pgd.Maximum = (int)pages_to_write;

                try
                {
                    using (BinaryReader binaryliststream = new BinaryReader(new MemoryStream(binary_list)))
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
                                        pgd.LabelText = "List Uploading Is Now In Progress " + Math.Round(((float)page_counter / (minor_rev == 0 ? 1000 : pages_to_write)) * 100, 2) + "%";

                                        if ((byte)(await stream_gen.GetStream()).ReadByte() != (byte)'?') //read ?
                                            throw new Exception();

                                        if (minor_rev == 1)
                                            (await stream_gen.GetStream()).Write(BitConverter.GetBytes(page_counter).Reverse().ToArray(), 0, 2);
                                    }

                                    if (page_counter < pages_to_write)
                                    {
                                        (await stream_gen.GetStream()).Write(page_buffer, 0, page_size);

                                        if (minor_rev == 1)
                                        {
                                            (await stream_gen.GetStream()).WriteByte(CRC8(page_buffer, 128));

                                            if ((byte)(await stream_gen.GetStream()).ReadByte() != (byte)'!') //read ?
                                                throw new Exception(); //read ! (operation completion flag)

                                            if (await VerifyListPage(stream_gen, page_counter, page_buffer) == false)
                                                throw new Exception();

                                            if (page_counter + 1 < pages_to_write)
                                            {
                                                (await stream_gen.GetStream()).WriteByte((byte)'?'); //enter the management interface
                                                (await stream_gen.GetStream()).WriteByte((byte)'S'); //issue command

                                                if ((byte)(await stream_gen.GetStream()).ReadByte() != (byte)'?') //read ?
                                                    throw new Exception();
                                            }
                                        }
                                    }
                                    else
                                        if (minor_rev == 0)
                                        {
                                            byte[] null_buffer = new byte[128];
                                            (await stream_gen.GetStream()).Write(null_buffer, 0, page_size);
                                        }
                                        else
                                            if (minor_rev == 1)
                                            {
                                                pgd.Reset();
                                                pgd.LabelText = "Zeroing Out of Bounds List Memory";
                                                pgd.SetMarqueeStyle();

                                                (await stream_gen.GetStream()).WriteByte((byte)'?'); //enter the management interface
                                                (await stream_gen.GetStream()).WriteByte((byte)'N'); //issue command

                                                if ((byte)(await stream_gen.GetStream()).ReadByte() != (byte)'?') //read ?
                                                    throw new Exception();

                                                (await stream_gen.GetStream()).Write(BitConverter.GetBytes(page_counter).Reverse().ToArray(), 0, 2);
                                                (await stream_gen.GetStream()).Write(BitConverter.GetBytes((UInt16)999).Reverse().ToArray(), 0, 2);

                                                await Task.Delay((1000 - page_counter) * 10);

                                                if ((byte)(await stream_gen.GetStream()).ReadByte() != (byte)'!') //read !
                                                    throw new Exception();

                                                pgd.Reset();

                                                break;
                                            }

                                    pgd.Step();

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
                                    for (; i < 5; i++)
                                    {
                                        try
                                        {
                                            if (page_counter < pages_to_write)
                                            {
                                                (await stream_gen.GetStream()).WriteByte((byte)'?'); //enter the management interface
                                                (await stream_gen.GetStream()).WriteByte((byte)'S');

                                                if ((byte)(await stream_gen.GetStream()).ReadByte() != (byte)'?') //read ?
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

            var config = MCv2Persistance.Instance.Config;

            if (MCv2Persistance.Instance.Config.UIConfiguration.ShowDialogOnMCV2OfflineControllerInteractionFailure && error_flag)
                MessageBox.Show("An Error Occured While Uploading The List. The Operation Has Been Aborted.", "Error");
            else
            {
                if (config.SyncTimeAfterUploadsFlag)
                {
                    pgd.Reset();
                    pgd.LabelText = "Setting Controller Time";

                    await SetControllerTime(stream_gen);

                    pgd.Step();
                    await Task.Delay(100);
                    pgd.Reset();
                }

                pgd.LabelText = "The List Was Uploaded Successfully";
            }

            await Task.Delay(100);

            return error_flag;
        }

        private static async Task<bool> VerifyListPage(ManagedStreamV2 stream_gen, UInt16 page_index, byte[] compare_to)
        {
            try
            {
                byte[] remote_bin_buffer = new byte[128];

                while (true)
                {
                    try
                    {
                        (await stream_gen.GetStream()).WriteByte((byte)'?'); //enter the management interface
                        (await stream_gen.GetStream()).WriteByte((byte)'K');

                        if ((byte)(await stream_gen.GetStream()).ReadByte() != (byte)'?') //read ?
                            throw new Exception();

                        (await stream_gen.GetStream()).Write(BitConverter.GetBytes(page_index).Reverse().ToArray(), 0, 2);

                        if ((await stream_gen.GetStream()).Read(remote_bin_buffer, 0, page_size) < 128)
                            throw new Exception();

                        byte crc = (byte)(await stream_gen.GetStream()).ReadByte();

                        if (CRC8(remote_bin_buffer, 128) != crc)
                            throw new Exception();

                        if ((byte)(await stream_gen.GetStream()).ReadByte() != (byte)'?') //read ?
                            throw new Exception();
                        else
                            (await stream_gen.GetStream()).WriteByte((byte)'!');

                        return remote_bin_buffer.SequenceEqual(compare_to);
                    }
                    catch (Exception ex2)
                    {
                        int i = 0;
                        for (; i < 3; i++)
                        {
                            try
                            {
                                //setLBLText(label1, "Automatic Reconnection Attempt " + (i + 1) + " of 3");

                                await stream_gen.GetStream();

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

        private static byte CRC8(byte[] data, int len)
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

        public static void GetNewLogEntries(ulong device_id)
        {

        }

        public static Task<MCv2LogEntry> ReadLogEntry(ManagedStreamV2 stream_gen, uint entry_number)
        {
            return Task.Run(async () =>
            {
                byte[] rx_buffer = new byte[16];

                int retry_counter = 0;
                for (; retry_counter < 10; retry_counter++)
                {
                    try
                    {
                        (await stream_gen.GetStream()).WriteByte((byte)'?'); //enter the management interface
                        (await stream_gen.GetStream()).WriteByte((byte)'B');

                        if ((byte)(await stream_gen.GetStream()).ReadByte() != (byte)'?') //read ?
                            throw new Exception("Did not receive a request response from the controller.");

                        (await stream_gen.GetStream()).Write(BitConverter.GetBytes(entry_number).Reverse().ToArray(), 2, 2);

                        if ((await stream_gen.GetStream()).Read(rx_buffer, 0, 16) < 16)
                            throw new Exception("Unable to read log entry from stream.");

                        int crc = (await stream_gen.GetStream()).ReadByte();

                        if (crc == -1)
                            throw new Exception("The crc was invalid.");

                        if (CRC8(rx_buffer, 16) != (byte)crc)
                            throw new Exception("The crc was incorrect.");

                        (await stream_gen.GetStream()).WriteByte((byte)'!');

                        //string str = "";
                        //for(int i = 0; i < 16; i++)
                        //    str += rx_buffer[i] + " ";

                        //MessageBox.Show(String.Join(" ", BitConverter.GetBytes(entry_number).Reverse().ToArray().SelectMany(j => Utilities.GetBits(j)).Select(h => h + "")) + "");

                        break;
                    }
                    catch (Exception ex)
                    {
                        await Task.Delay(1000);
                    }
                }

                if(retry_counter >= 10)
                    throw new Exception("Unable to read log entry " + entry_number + ".");

                int offset_counter = 0;

                ulong card_id = ((ulong)rx_buffer[offset_counter] << 56) |
                                        ((ulong)rx_buffer[offset_counter + 1] << 48) |
                                        ((ulong)rx_buffer[offset_counter + 2] << 40) |
                                        ((ulong)rx_buffer[offset_counter + 3] << 32) |
                                        ((ulong)rx_buffer[offset_counter + 4] << 24) |
                                        ((ulong)rx_buffer[offset_counter + 5] << 16) |
                                        ((ulong)rx_buffer[offset_counter + 6] << 8) |
                                        ((ulong)rx_buffer[offset_counter + 7]);

                offset_counter += 8;

                int year = 2000 + (rx_buffer[offset_counter + 6] >> 4) * 10 + (rx_buffer[6 + offset_counter] & 0xF);
                int month = ((rx_buffer[offset_counter + 5] >> 4) & 1) * 10 + (rx_buffer[5 + offset_counter] & 0xF);
                int date = ((rx_buffer[offset_counter + 4] >> 4) & 3) * 10 + (rx_buffer[4 + offset_counter] & 0xF);
                int day = (rx_buffer[offset_counter + 3] & 7) - 1;

                int hour = ((rx_buffer[2 + offset_counter] >> 4) & 3) * 10 + (rx_buffer[2 + offset_counter] & 0xF);
                int minute = (rx_buffer[1 + offset_counter] >> 4) * 10 + (rx_buffer[1 + offset_counter] & 0xF);
                int second = (rx_buffer[offset_counter] >> 4) * 10 + (rx_buffer[offset_counter] & 0xF);

                int reader_id = rx_buffer[7 + offset_counter] >> 4;
                int auth_code = rx_buffer[7 + offset_counter] & 0xF;

                return new MCv2LogEntry(stream_gen.DeviceID, new DateTime(year, month, date, hour, minute, second), card_id, reader_id, auth_code);
            });
        }

        public static async Task<bool> SetControllerTime(ManagedStreamV2 stream_gen, ProgressDialog pgd = null, int num_retries = 10)
        {
           //issue serial commands to begin operation

            return await Task.Run(async () =>
            {
                if (pgd != null)
                {
                    //pgd.Show();
                    pgd.LabelText = "Beginning Command Execution";
                    await Task.Delay(100);
                }

                int i = 0;
                for (; i < num_retries; i++)
                {
                    try
                    {
                        if (pgd != null)
                            pgd.LabelText = "RTC Register Write";

                        (await stream_gen.GetStream()).WriteByte((byte)'?');
                        (await stream_gen.GetStream()).WriteByte((byte)'C');
                    
                        (await stream_gen.GetStream()).Write(BinaryGenerator.GenerateRTCBytes(), 0, 7);

                        if(pgd != null)
                        {
                            pgd.Step();
                            await Task.Delay(100);
                            pgd.Reset();
                        }

                        //read back time to verify
                        try
                        {
                            if (pgd != null)
                                pgd.LabelText = "Verifying Time";

                            DateTime controller_time = await ReadControllerTime(stream_gen);

                            Console.WriteLine(controller_time.ToString());

                            if (controller_time < (DateTime.Now - TimeSpan.FromSeconds(30)) || controller_time > (DateTime.Now + TimeSpan.FromSeconds(30)))
                                continue;
                            else
                            {
                                pgd.Step();
                                await Task.Delay(100);
                                pgd.Reset();

                                break;
                            }
                        }
                        catch (InvalidOperationException ex)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                     
                    }
                }

                if (i == num_retries)
                    return false;
                else
                    return true;
            });
        }

        public static async Task<DateTime> ReadControllerTime(ManagedStreamV2 stream_gen, int num_retries = 10)
        {
            byte[] rtc_bytes = new byte[7];

            for (int i = 0; i < num_retries; i++)
            {
                try
                {
                    (await stream_gen.GetStream()).WriteByte((byte)'?'); //enter the management interface
                    (await stream_gen.GetStream()).WriteByte((byte)'T');

                    if ((await stream_gen.GetStream()).Read(rtc_bytes, 0, 7) < 7)
                        throw new Exception();

                    int crc = (await stream_gen.GetStream()).ReadByte();

                    if (crc == -1)
                        throw new Exception();

                    if (CRC8(rtc_bytes, 7) != (byte)crc)
                        throw new Exception();

                    if ((byte)(await stream_gen.GetStream()).ReadByte() != (byte)'?') //read ?
                        throw new Exception();
                    else
                        (await stream_gen.GetStream()).WriteByte((byte)'!');

                    return BinaryTranslator.TranslateRTCBytes(rtc_bytes);
                }
                catch (Exception ex)
                {

                }
            }

            throw new InvalidOperationException("Read Controller Time - The Operation Was Unsuccessful");
        }
    }
}
