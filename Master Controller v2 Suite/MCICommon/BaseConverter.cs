using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public class BaseConverter
    {
        private static string encoding_chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static int encoding_base = encoding_chars.Length;

        public static string EncodeFromBase10(ulong i)
        {
            string retstr = "";

            if (i == 0)
            {
                return "0";
            }

            while (i > 0)
            {
                int rem = (int)(i % (ulong)encoding_base);
                i = i / (ulong)encoding_base;

                retstr += encoding_chars[(int)rem];
            }

            return new String(retstr.Reverse().ToArray());
        }

        public static ulong DecodeFromString(string to_decode)
        {
            to_decode = to_decode.Trim().ToUpper();

            char[] chars = to_decode.ToCharArray();
            chars = chars.Reverse().ToArray();

            ulong to_return = 0;

            //for(int i = chars.Length-1; i >= 0; i--)
            for(int i = 0; i < chars.Length; i++)
                to_return += (ulong)encoding_chars.IndexOf(chars[i]) * (ulong)Math.Pow(encoding_base, i);

            return to_return;
        }

        public static bool TryParseEncodedString(string to_test)
        {
            try
            {
                DecodeFromString(to_test);
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
    }
}
