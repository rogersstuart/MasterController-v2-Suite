using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using MCICommon;

namespace MasterControllerInterface
{
    public partial class NUIDEncodingTranslatorForm : Form
    {
        public NUIDEncodingTranslatorForm()
        {
            InitializeComponent();

            textBox1.HideSelection = false; //base 10
            textBox2.HideSelection = false; //base 36
            textBox3.HideSelection = false; //base 16
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
            textBox2.TextChanged -= textBox2_TextChanged;
            textBox3.TextChanged -= textBox3_TextChanged;

            ////

            List<string> result_lines = new List<string>();
                bool error_occured = false;

                foreach (string line in textBox1.Lines.Select(x => x.Trim()))
                {
                    try
                    {
                        result_lines.Add(BaseConverter.EncodeFromBase10(Convert.ToUInt64(line)));
                    }
                    catch (Exception ex)
                    {
                        result_lines.Add("[null]");
                        error_occured = true;
                    }
                }

            textBox2.BackColor = SystemColors.Window;

            textBox2.Lines = result_lines.ToArray();

            //////

            result_lines = new List<string>();

            foreach (string line in textBox1.Lines.Select(x => x.Trim()))
            {
                try
                {
                    result_lines.Add(string.Format("{0:X}", Convert.ToUInt64(line)));
                }
                catch (Exception ex)
                {
                    result_lines.Add("[null]");
                    error_occured |= true;
                }
            }

            textBox3.BackColor = SystemColors.Window;

            textBox3.Lines = result_lines.ToArray();

            //////////

            if (error_occured)
                textBox1.BackColor = Color.LightSalmon;
            else
                textBox1.BackColor = SystemColors.Window;

            try
            {
                int selection_start = textBox2.GetFirstCharIndexFromLine(textBox1.GetLineFromCharIndex(textBox1.GetFirstCharIndexOfCurrentLine()));
                if (selection_start > -1)
                    textBox2.Select(selection_start, textBox2.Lines[textBox2.GetLineFromCharIndex(selection_start)].Length);

                selection_start = textBox3.GetFirstCharIndexFromLine(textBox1.GetLineFromCharIndex(textBox1.GetFirstCharIndexOfCurrentLine()));
                if (selection_start > -1)
                    textBox3.Select(selection_start, textBox3.Lines[textBox3.GetLineFromCharIndex(selection_start)].Length);
            }
            catch (Exception ex)
            { }

            textBox2.ScrollToCaret();
            textBox3.ScrollToCaret();

            Refresh();

            textBox2.TextChanged += textBox2_TextChanged;
            textBox3.TextChanged += textBox3_TextChanged;

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            textBox1.TextChanged -= textBox1_TextChanged;
            textBox3.TextChanged -= textBox3_TextChanged;

            List<string> result_lines = new List<string>();
                bool error_flag = false;

                string[] lines = textBox2.Lines.Select(x => x.Trim().ToUpper()).ToArray();

                for (int i = 0; i < lines.Length; i++)
                {
                    if (BaseConverter.TryParseEncodedString(lines[i]))
                    {
                        ulong conv_result = BaseConverter.DecodeFromString(lines[i]);

                        if (BaseConverter.EncodeFromBase10(Convert.ToUInt64(conv_result)) == lines[i])
                            result_lines.Add(conv_result.ToString());
                        else
                        {
                            error_flag = true;
                            result_lines.Add("[null]");
                        }
                    }
                    else
                    {
                        error_flag = true;
                        result_lines.Add("[null]");
                    }
                }

                textBox1.BackColor = SystemColors.Window;

                textBox1.Lines = result_lines.ToArray();

            //////

            result_lines = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (BaseConverter.TryParseEncodedString(lines[i]))
                {
                    ulong conv_result = BaseConverter.DecodeFromString(lines[i]);

                    if (BaseConverter.EncodeFromBase10(Convert.ToUInt64(conv_result)) == lines[i])
                        result_lines.Add(string.Format("{0:X}", conv_result));
                    else
                    {
                        error_flag = true;
                        result_lines.Add("[null]");
                    }
                }
                else
                {
                    error_flag |= true;
                    result_lines.Add("[null]");
                }
            }

            textBox3.BackColor = SystemColors.Window;

           

            textBox3.Lines = result_lines.ToArray();


            /////

            if (error_flag)
                textBox2.BackColor = Color.LightSalmon;
            else
                textBox2.BackColor = SystemColors.Window;

            try
            {
                int selection_start = textBox1.GetFirstCharIndexFromLine(textBox2.GetLineFromCharIndex(textBox2.GetFirstCharIndexOfCurrentLine()));
                if (selection_start > -1)
                    textBox1.Select(selection_start, textBox1.Lines[textBox1.GetLineFromCharIndex(selection_start)].Length);

                selection_start = textBox3.GetFirstCharIndexFromLine(textBox2.GetLineFromCharIndex(textBox2.GetFirstCharIndexOfCurrentLine()));
                if (selection_start > -1)
                    textBox3.Select(selection_start, textBox3.Lines[textBox3.GetLineFromCharIndex(selection_start)].Length);
            }
            catch (Exception ex) { }

            textBox1.ScrollToCaret();
            textBox3.ScrollToCaret();

            Refresh();

            textBox1.TextChanged += textBox1_TextChanged;
            textBox3.TextChanged += textBox3_TextChanged;
        }

        private void NUIDEncodingTranslatorForm_Shown(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            string[] file_names = ShowOFD();

            if (file_names == null)
                return;

            List<string> lines_from_files = new List<string>();
            foreach (string file_name in file_names)
                lines_from_files.AddRange(File.ReadAllLines(file_name));

            textBox1.Lines = new Func<string[]>(() =>
            {
                var list = textBox1.Lines.ToList();
                list.AddRange(lines_from_files);
                return list.ToArray();
            }).Invoke();
        }

        private string[] ShowOFD()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
                return ofd.FileNames;
            else
                return null;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string[] file_names = ShowOFD();

            if (file_names == null)
                return;

            List<string> lines_from_files = new List<string>();
            foreach (string file_name in file_names)
                lines_from_files.AddRange(File.ReadAllLines(file_name));

            textBox2.Lines = new Func<string[]>(() =>
            {
                var list = textBox2.Lines.ToList();
                list.AddRange(lines_from_files);
                return list.ToArray();
            }).Invoke();
        }

        private string ShowSFD()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
                return sfd.FileName;
            else
                return null;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string file_name = ShowSFD();

            if (file_name == null)
                return;

            File.WriteAllLines(file_name, textBox1.Lines);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string file_name = ShowSFD();

            if (file_name == null)
                return;

            File.WriteAllLines(file_name, textBox2.Lines);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //textBox1.TextChanged -= textBox1_TextChanged;
            //textBox2.TextChanged -= textBox2_TextChanged;

            textBox1.Clear();
            textBox2.Clear();
            textBox1.Focus();

            //textBox1.TextChanged += textBox1_TextChanged;
            //textBox2.TextChanged += textBox2_TextChanged;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string[] lines = textBox2.Lines.ToList().Select(x => new String(x.Trim().ToUpper().Reverse().ToArray())).ToArray();
            textBox2.Lines = lines;
        }

        private void textBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //mouse down tb1
            try
            {
                int line_num = textBox1.GetLineFromCharIndex(textBox1.GetCharIndexFromPosition(e.Location));

                int selection_start = textBox2.GetFirstCharIndexFromLine(line_num);
                if (line_num > -1)
                    textBox2.Select(selection_start, textBox2.Lines[line_num].Length);

                selection_start = textBox3.GetFirstCharIndexFromLine(line_num);
                if (line_num > -1)
                    textBox3.Select(selection_start, textBox3.Lines[line_num].Length);
            }
            catch (Exception ex) { }

            textBox2.ScrollToCaret();
            textBox3.ScrollToCaret();

            Refresh();
        }

        private void textBox2_MouseDown(object sender, MouseEventArgs e)
        {
            //mouse down tb2
            try
            {
                int line_num = textBox2.GetLineFromCharIndex(textBox2.GetCharIndexFromPosition(e.Location));

                int selection_start = textBox1.GetFirstCharIndexFromLine(line_num);
                if (line_num > -1)
                    textBox1.Select(selection_start, textBox1.Lines[line_num].Length);

                selection_start = textBox3.GetFirstCharIndexFromLine(line_num);
                if (line_num > -1)
                    textBox3.Select(selection_start, textBox3.Lines[line_num].Length);
            }
            catch (Exception ex) { }

            textBox1.ScrollToCaret();
            textBox3.ScrollToCaret();

            Refresh();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            //append from files hex

            string[] file_names = ShowOFD();

            if (file_names == null)
                return;

            List<string> lines_from_files = new List<string>();
            foreach (string file_name in file_names)
                lines_from_files.AddRange(File.ReadAllLines(file_name));

            textBox3.Lines = new Func<string[]>(() =>
            {
                var list = textBox3.Lines.ToList();
                list.AddRange(lines_from_files);
                return list.ToArray();
            }).Invoke();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            //write to file hex

            string file_name = ShowSFD();

            if (file_name == null)
                return;

            File.WriteAllLines(file_name, textBox3.Lines);
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            //hex text changed

            textBox1.TextChanged -= textBox1_TextChanged;
            textBox2.TextChanged -= textBox2_TextChanged;

            List<string> result_lines = new List<string>();
            bool error_flag = false;

            string[] lines = textBox3.Lines.Select(x => x.Trim().ToUpper()).ToArray();

            for (int i = 0; i < lines.Length; i++)
            {
                ulong parse = 0;
                bool success = false;

                try
                {
                    parse = ulong.Parse(lines[i], System.Globalization.NumberStyles.HexNumber);
                    success = true;
                }
                catch(Exception ex)
                {

                }

                if (success)
                    result_lines.Add(parse.ToString());
                else
                {
                    error_flag = true;
                    result_lines.Add("[null]");
                }
            }

            textBox1.BackColor = SystemColors.Window;
            textBox1.Lines = result_lines.ToArray();

            //////

            result_lines = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                ulong parse = 0;
                bool success = false;

                try
                {
                    parse = ulong.Parse(lines[i], System.Globalization.NumberStyles.HexNumber);
                    success = true;
                }
                catch (Exception ex)
                {

                }


                if (success)
                    result_lines.Add(BaseConverter.EncodeFromBase10(parse));
                else
                {
                    error_flag = true;
                    result_lines.Add("[null]");
                }
            }

            textBox2.BackColor = SystemColors.Window;
            textBox2.Lines = result_lines.ToArray();


            /////

            if (error_flag)
                textBox3.BackColor = Color.LightSalmon;
            else
                textBox3.BackColor = SystemColors.Window;

            try
            {
                int selection_start = textBox1.GetFirstCharIndexFromLine(textBox3.GetLineFromCharIndex(textBox3.GetFirstCharIndexOfCurrentLine()));
                if (selection_start > -1)
                    textBox1.Select(selection_start, textBox1.Lines[textBox1.GetLineFromCharIndex(selection_start)].Length);

                selection_start = textBox2.GetFirstCharIndexFromLine(textBox3.GetLineFromCharIndex(textBox3.GetFirstCharIndexOfCurrentLine()));
                if (selection_start > -1)
                    textBox2.Select(selection_start, textBox2.Lines[textBox2.GetLineFromCharIndex(selection_start)].Length);
            }
            catch (Exception ex) { }

            textBox1.ScrollToCaret();
            textBox2.ScrollToCaret();

            Refresh();

            textBox1.TextChanged += textBox1_TextChanged;
            textBox2.TextChanged += textBox2_TextChanged;
        }

        private void textBox3_MouseDown(object sender, MouseEventArgs e)
        {
            //hex mouse down

            try
            {
                int line_num = textBox3.GetLineFromCharIndex(textBox3.GetCharIndexFromPosition(e.Location));

                int selection_start = textBox1.GetFirstCharIndexFromLine(line_num);
                if (line_num > -1)
                    textBox1.Select(selection_start, textBox1.Lines[line_num].Length);

                selection_start = textBox2.GetFirstCharIndexFromLine(line_num);
                if (line_num > -1)
                    textBox2.Select(selection_start, textBox2.Lines[line_num].Length);
            }
            catch (Exception ex) { }

            textBox1.ScrollToCaret();
            textBox2.ScrollToCaret();

            Refresh();
        }
    }
}
