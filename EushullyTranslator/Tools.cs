using System;
using System.Text;

namespace EushullyEditor
{ 
    class Tools
    {
        public static byte[] cutFile(byte[] file, int position)
        {
            if (position >= file.Length)
                return file;
            else
            {
                byte[] result = new byte[position-1];
                for (int pos = 0; pos < result.Length; pos++)
                {
                    result[pos] = file[pos];
                }
                return result;
            }
        }
        public static bool EndsWith(byte[] Array, byte[] subArray)
        {
            for (int pos = subArray.Length; pos > 0; pos--)
            {
                byte a = Array[Array.Length - pos];
                byte b = subArray[subArray.Length - pos];
                if (a != b)
                {
                    return false;
                }
            }
            return true;
        }
        public static byte[] OverWriteAt(byte[] File, byte[] NewData, int Position)
        {
            for (int pos = 0; pos < NewData.Length; pos++)
            {
                File[Position + pos] = NewData[pos];
            }
            return File;
        }
        public static byte[] GenDWOffet(int value)
        {
            byte[] result = new byte[4];
            string hex = DataTools.IntToHex(value);
            if (hex.Length % 2 != 0)
            {
                hex = "0" + hex;
            }
            byte[] off = DataTools.StringToByteArray(hex);
            switch (off.Length)
            {
                case 1:
                    result = new byte[] { off[0], 0x00, 0x00, 0x00 };
                    break;
                case 2:
                    result = new byte[] { off[1], off[0], 0x00, 0x00 };
                    break;
                case 3:
                    result = new byte[] { off[2], off[1], off[0], 0x00 };
                    break;
                case 4:
                    result = new byte[] { off[3], off[2], off[1], off[0] };
                    break;
            }
            return result;
        }

        public static byte[] AppendFile(byte[] Original, byte[] ContentToAppend)
        {
            byte[] result = new byte[Original.Length+ContentToAppend.Length];
            Original.CopyTo(result, 0);
            ContentToAppend.CopyTo(result, Original.Length);
            return result;
        }

        public static int GetDWOffset(byte[] script)
        {
            int pos = 0;
            return DataTools.ByteArrayToInt(new byte[] { script[pos + 3], script[pos + 2], script[pos + 1], script[pos] });
        }
        public static int GetDWOffset(byte[] script, int pos)
        {
            return DataTools.ByteArrayToInt(new byte[] { script[pos + 3], script[pos + 2], script[pos + 1], script[pos] });
        }

        public static bool CompareAt(byte[] script, int At, byte[] Check)
        {
            for (int index = 0; index < Check.Length; index++)
            {
                if (Check[index] != script[index + At])
                {
                    return false;
                }
            }
            return true;
        }

    }
    class DataTools
    {
        public static string IntToHex(int val)
        {
            return val.ToString("X");
        }
        public static int ByteArrayToInt(byte[] array)
        {
            return HexToInt(ByteArrayToString(array).Replace(@"-", ""));
        }
        public static string StringToHex(string _in)
        {
            string input = _in;
            char[] values = input.ToCharArray();
            string r = "";
            foreach (char letter in values)
            {
                int value = Convert.ToInt32(letter);
                string hexOutput = string.Format("{0:X}", value);
                if (value > 255)
                    return UnicodeStringToHex(input);
                r += value + " ";
            }
            string[] bytes = r.Split(' ');
            byte[] b = new byte[bytes.Length - 1];
            int index = 0;
            foreach (string val in bytes)
            {
                if (index == bytes.Length - 1)
                    break;
                if (int.Parse(val) > byte.MaxValue)
                {
                    b[index] = byte.Parse("0");
                }
                else
                    b[index] = byte.Parse(val);
                index++;
            }
            r = ByteArrayToString(b);
            return r.Replace("-", @" ");
        }
        public static string UnicodeStringToHex(string _in)
        {
            string input = _in;
            char[] values = Encoding.Unicode.GetChars(Encoding.Unicode.GetBytes(input.ToCharArray()));
            string r = "";
            foreach (char letter in values)
            {
                int value = Convert.ToInt32(letter);
                string hexOutput = System.String.Format("{0:X}", value);
                r += value + " ";
            }
            UnicodeEncoding unicode = new UnicodeEncoding();
            byte[] b = unicode.GetBytes(input);
            r = ByteArrayToString(b);
            return r.Replace("-", @" ");

        }
        public static string U8HexToString(string[] hex)
        {
            byte[] str = StringToByteArray(hex);
            UTF8Encoding encoder = new UTF8Encoding();
            return encoder.GetString(str);
        }
        public static string[] U8StringToHex(string text)
        {
            UTF8Encoding encoder = new UTF8Encoding();
            byte[] cnt = encoder.GetBytes(text.ToCharArray());
            return ByteArrayToString(cnt).Split('-');
        }

        public static string SJHexToString(string[] hex)
        {
            byte[] str = StringToByteArray(hex);
            Encoding encoder = Encoding.GetEncoding(932);
            return encoder.GetString(str);
        }
        public static string[] SJStringToHex(string text)
        {
            Encoding encoder = Encoding.GetEncoding(932);
            byte[] cnt = encoder.GetBytes(text.ToCharArray());
            return ByteArrayToString(cnt).Split('-');
        }
        public static byte[] StringToByteArray(string hex)
        {
            try
            {
                hex = hex.Replace(@" ", "");
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }
            catch { Console.Write("Invalid format file!"); return new byte[0]; }
        }
        public static byte[] StringToByteArray(string[] hex)
        {
            try
            {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars];
                for (int i = 0; i < NumberChars; i++)
                    bytes[i] = Convert.ToByte(hex[i], 16);
                return bytes;
            }
            catch { Console.Write("Invalid format file!"); return new byte[0]; }
        }
        public static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex;
        }

        public static int HexToInt(string hex)
        {
            int num = Int32.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return num;
        }

        public static string HexToString(string hex)
        {
            string[] hexValuesSplit = hex.Split(' ');
            string returnvar = "";
            foreach (string hexs in hexValuesSplit)
            {
                int value = Convert.ToInt32(hexs, 16);
                char charValue = (char)value;
                returnvar += charValue;
            }
            return returnvar;
        }

        public static string UnicodeHexToUnicodeString(string hex)
        {
            string hexString = hex.Replace(@" ", "");
            int length = hexString.Length;
            byte[] bytes = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return Encoding.Unicode.GetString(bytes);
        }

    }
}
