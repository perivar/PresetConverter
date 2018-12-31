using System;
using System.Text;

namespace CommonUtils.Audio
{
    public static class FourCC
    {
        public static string FromFourCC(int fourCCInt)
        {
            byte[] bytes = BitConverter.GetBytes(fourCCInt);
            return Encoding.ASCII.GetString(bytes);
        }

        public static int ToFourCC(string fourCCString)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(fourCCString);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static int ToFourCC(char[] fourCC)
        {
            if (fourCC.Length != 4)
            {
                throw new Exception("FourCC char arrays must be 4 characters long " + new string(fourCC));
            }

            int result = ((int)fourCC[3]) << 24
                        | ((int)fourCC[2]) << 16
                        | ((int)fourCC[1]) << 8
                        | ((int)fourCC[0]);

            return result;
        }

        public static int ToFourCC(char c0, char c1, char c2, char c3)
        {
            int result = ((int)c3) << 24
                        | ((int)c2) << 16
                        | ((int)c1) << 8
                        | ((int)c0);

            return result;
        }
    }
}