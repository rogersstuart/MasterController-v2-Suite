using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace MCICommon
{
    public static class Utilities
    {
        //CRC-8 - based on the CRC8 formulas by Dallas/Maxim
        //code released under the therms of the GNU GPL 3.0 license
        //Code converted to C# by Stuart on 7/23/2015
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

        public static byte[] ConvertBoolArrayToBytes(bool[] bools)
        {
            int bytes = bools.Length / 8;
            if ((bools.Length % 8) != 0) bytes++;
            byte[] arr2 = new byte[bytes];
            int bitIndex = 0, byteIndex = 0;
            for (int i = bools.Length - 1; i >= 0; i--)
            {
                if (bools[i])
                {
                    arr2[byteIndex] |= (byte)(((byte)1) << bitIndex);
                }
                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }
            return arr2;
        }

        public static IEnumerable<bool> GetBits(byte b)
        {
            for (int i = 0; i < 8; i++)
            {
                yield return (b & 0x80) != 0;
                b *= 2;
            }
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static byte[] ConvertObjectToByteArray<T>(T to_serialize)
        {
            byte[] data;
            using (var ms = new MemoryStream())
            {
                BinaryFormatter bFormatter = new BinaryFormatter();
                bFormatter.Serialize(ms, to_serialize);
                data = ms.ToArray();
            }
            return data;
        }

        public static T ConvertByteArrayToObject<T>(byte[] bytes)
        {
            T to_return;
            using (var ms = new MemoryStream())
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;

                BinaryFormatter bFormatter = new BinaryFormatter();
                to_return = (T)bFormatter.Deserialize(ms);
            }
            return to_return;
        }

        public static T ConvertStreamToObject<T>(Stream s)
        {
            BinaryFormatter bf = new BinaryFormatter();
            return (T)bf.Deserialize(s);
        }

        public static DateTime GetNextWeekday(DayOfWeek day)
        {
            DateTime result = DateTime.Now;
            while (result.DayOfWeek != day)
                result = result.AddDays(1);
            return result;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static byte[] GetHashOfBytes(byte[] bytes)
        {
            byte[] hash = new byte[] { };

            using (var md5 = MD5.Create())
                hash = md5.ComputeHash(bytes);

            return hash;
        }

        public static byte[] GetFileHashAsBytes(string filename)
        {
            byte[] hash = new byte[] { };

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    hash = md5.ComputeHash(stream);
                }
            }

            return hash;
        }

        public static string GetFileHashAsString(string filename)
        {
            string hash = "";

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                }
            }

            return hash;
        }

        public static bool CompareByteArrays(byte[] left, byte[] right)
        {
            if (left == null || right == null)
                return true;

            if (left.Length != right.Length)
                return false;

            for (int index = 0; index < left.Length; ++index)
            {
                if (left[index] != right[index])
                    return false;
            }

            return true;
        }
    }
}
