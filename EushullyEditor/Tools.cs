using System;

namespace VNX.EushullyEditor
{ 
    class Tools
    {      
        public static byte[] GenDWOffet(int value)
        {
            byte[] result = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result, 0, result.Length);
            return result;
        }
        public static int GetDWOffset(byte[] script, int pos)
        {
            byte[] Arr = new byte[4];
            Array.Copy(script, pos, Arr, 0, Arr.Length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(Arr, 0, Arr.Length);
            return BitConverter.ToInt32(Arr, 0);
        }

        public static bool CompareAt(byte[] script, int At, byte[] Check)
        {
            for (int index = 0; index < Check.Length; index++)
                if (Check[index] != script[index + At])
                    return false;
            return true;
        }

    }
}
