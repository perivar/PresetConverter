using System.Runtime.InteropServices;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class BitProcess
    {
        public static byte nbits8(int n)
        {
            if (n > 8)
            {
                return 0xFF;
            }
            else if (n == 0)
            {
                return 0;
            }
            else
            {
                byte result = 1;
                for (int i = 2; i <= n; i++)
                {
                    result = (byte)(result << 1 + 1);
                }

                return result;
            }
        }

        public static ushort nbits16(int n)
        {
            if (n > 16)
            {
                return 0xFFFF;
            }
            else if (n == 0)
            {
                return 0;
            }
            else
            {
                ushort result = 1;
                for (int i = 2; i <= n; i++)
                {
                    result = (ushort)(result << 1 + 1);
                }

                return result;
            }
        }

        public static uint nbits32(int n)
        {
            // Calculates the bitmask for 'n' bits
            if (n > 32)
            {
                return 0xFFFFFFFF;
            }
            else if (n == 0)
            {
                return 0;
            }
            else
            {
                uint result = 1;
                for (int i = 2; i <= n; i++)
                {
                    result = (result << 1) + 1;
                }
                return result;
            }
        }

        public static int ChangeIntSign(int i, int sbit)
        {
            // Checks if the most significant bit of 'i' is set
            if ((i & (1 << (sbit - 1))) != 0)
            {
                uint dw = (uint)i;
                dw |= nbits32(32 - sbit) << sbit;
                // Changes the sign of 'dw' by applying a bitmask
                // to the upper bits based on the value of 'sbit'
                return (int)dw;
            }
            else
            {
                return i;
            }
        }

        public static void FillIntegers8(int n, IntPtr data, ref int[] ints, bool abs)
        {
            byte[] sourceArray = new byte[n * 4];
            Marshal.Copy(data, sourceArray, 0, n * 4);

            int start = abs ? 0 : 1;

            int sourceIndex = 0;

            for (int cur = start; cur < n; cur++)
            {
                byte tb = sourceArray[sourceIndex];
                sourceIndex++;

                int t = ChangeIntSign(tb, 8);
                ints[cur] = t;
            }

            if (!abs)
            {
                ints[0] = 0;
            }
        }

        public static void FillIntegers16(int n, IntPtr data, ref int[] ints, bool abs)
        {
            short[] sourceArray = new short[n];
            Marshal.Copy(data, sourceArray, 0, n);

            int start = abs ? 0 : 1;

            int sourceIndex = 0;

            for (int cur = start; cur < n; cur++)
            {
                short ts = sourceArray[sourceIndex];
                sourceIndex++;

                int t = ChangeIntSign(ts, 16);
                ints[cur] = t;
            }

            if (!abs)
            {
                ints[0] = 0;
            }
        }

        public static void FillIntegers24(int n, IntPtr data, ref int[] ints, bool abs)
        {
            byte[] sourceArray = new byte[n * 3];
            Marshal.Copy(data, sourceArray, 0, n * 3);

            int start = abs ? 0 : 1;

            int sourceIndex = 0;

            for (int cur = start; cur < n; cur++)
            {
                uint dw = (uint)((sourceArray[sourceIndex] & 0xFF) |
               ((sourceArray[sourceIndex + 1] & 0xFF) << 8) |
               ((sourceArray[sourceIndex + 2] & 0xFF) << 16));
                sourceIndex += 3;

                int t = ChangeIntSign((int)dw, 24);
                ints[cur] = t;
            }

            if (!abs)
            {
                ints[0] = 0;
            }
        }

        public static void FillIntegers32(int n, IntPtr data, ref int[] ints, bool abs)
        {
            int[] sourceArray = new int[n];
            Marshal.Copy(data, sourceArray, 0, n);

            int start = abs ? 0 : 1;

            int sourceIndex = 0;

            for (int cur = start; cur < n; cur++)
            {
                int ti = sourceArray[sourceIndex];
                sourceIndex++;

                int t = ChangeIntSign(ti, 32);
                ints[cur] = t;
            }

            if (!abs)
            {
                ints[0] = 0;
            }
        }

        // can be used for all bits: L8 = not divisible by 8
        public static void FillIntegersL8(int n, int bits, IntPtr data, ref int[] ints)
        {
            byte[] sourceArray = new byte[bits * 64];
            Marshal.Copy(data, sourceArray, 0, bits * 64);

            int bitsTotal = 0;
            int sourceIndex = 0;
            byte tb = sourceArray[sourceIndex];
            sourceIndex++;

            for (int cur = 1; cur < n; cur++)
            {
                uint dw = 0;
                for (int j = 0; j < bits; j++)
                {
                    // Extracts 'bits' number of bits from 'tb'
                    dw += (uint)(tb & 1) << j;
                    tb >>= 1;
                    bitsTotal++;
                    if (bitsTotal == 8)
                    {
                        tb = sourceArray[sourceIndex];
                        sourceIndex++;
                        bitsTotal = 0;
                    }
                }

                int t = ChangeIntSign((int)dw, bits);
                ints[cur] = t;
            }

            ints[0] = 0;
        }

        public static void FillBits8(int n, int[] ints, IntPtr data, bool abs)
        {
            short[] s = new short[n];
            Marshal.Copy(data, s, 0, n);

            int start = 0;
            for (int cur = start; cur < n; cur++)
            {
                s[cur] = (short)ints[cur];
            }
            Marshal.Copy(s, 0, data, n);
        }

        public static void FillBits16(int n, int[] ints, IntPtr data, bool abs)
        {
            short[] s = new short[n];
            Marshal.Copy(data, s, 0, n);

            int start = 0;
            for (int cur = start; cur < n; cur++)
            {
                s[cur] = (short)ints[cur];
            }
            Marshal.Copy(s, 0, data, n);
        }

        public static void FillBits24(int n, int[] ints, IntPtr data, bool abs)
        {
            byte[] b = new byte[n * 3];
            Marshal.Copy(data, b, 0, n * 3);

            int start = 0;
            for (int cur = start; cur < n; cur++)
            {
                int t = ints[cur];
                b[cur * 3] = (byte)(t & 0xFF);
                t >>= 8;
                b[cur * 3 + 1] = (byte)(t & 0xFF);
                t >>= 8;
                b[cur * 3 + 2] = (byte)(t & 0xFF);

                if (ints[cur] < 0)
                {
                    b[cur * 3 + 2] |= 0x80;
                }
            }
            Marshal.Copy(b, 0, data, n * 3);
        }

        public static void FillBits32(int n, int[] ints, IntPtr data, bool abs)
        {
            int[] ip = new int[n];
            Marshal.Copy(data, ip, 0, n);

            int start = 0;
            for (int cur = start; cur < n; cur++)
            {
                ip[cur] = ints[cur];
            }
            Marshal.Copy(ip, 0, data, n);
        }

        // can be used for all bits: L8 = not divisible by 8
        public static void FillBitsL8(int n, int bits, int[] ints, IntPtr data)
        {
            byte[] b = new byte[n];
            Marshal.Copy(data, b, 0, n);

            int tb = 0;
            int bitsWritten = 0;

            for (int cur = 0; cur < n; cur++)
            {
                uint dw = (uint)ints[cur];
                for (int j = 0; j < bits; j++)
                {
                    tb += (byte)((dw & 1) << bitsWritten);
                    dw >>= 1;
                    bitsWritten++;
                    if (bitsWritten == 8)
                    {
                        b[cur] = (byte)tb;
                        tb = 0;
                        bitsWritten = 0;
                        cur++;
                    }
                }
            }
            Marshal.Copy(b, 0, data, n);
        }

        public static void Encode8_8(int n, IntPtr source, IntPtr dest)
        {
            byte[] sourceArray = new byte[n];
            Marshal.Copy(source, sourceArray, 0, n);
            Marshal.Copy(sourceArray, 0, dest, n);
        }

        public static void Encode8_16(int n, IntPtr source, IntPtr dest)
        {
            short[] sourceArray = new short[n];
            Marshal.Copy(source, sourceArray, 0, n);

            byte[] destArray = new byte[n];

            for (int i = 0; i < n; i++)
            {
                destArray[i] = (byte)sourceArray[i];
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void Encode8_24(int n, IntPtr source, IntPtr dest)
        {
            int[] sourceArray = new int[n];
            Marshal.Copy(source, sourceArray, 0, n);

            byte[] destArray = new byte[n * 3];

            for (int i = 0; i < n; i++)
            {
                destArray[i * 3] = (byte)(sourceArray[i] & 0xFF);
                destArray[i * 3 + 1] = (byte)((sourceArray[i] >> 8) & 0xFF);
                destArray[i * 3 + 2] = (byte)((sourceArray[i] >> 16) & 0xFF);
            }

            Marshal.Copy(destArray, 0, dest, n * 3);
        }

        public static void Encode8_32(int n, IntPtr source, IntPtr dest)
        {
            int[] sourceArray = new int[n];
            Marshal.Copy(source, sourceArray, 0, n);

            byte[] destArray = new byte[n * 4];

            for (int i = 0; i < n; i++)
            {
                byte[] bytes = BitConverter.GetBytes(sourceArray[i]);
                Array.Copy(bytes, 0, destArray, i * 4, 4);
            }

            Marshal.Copy(destArray, 0, dest, n * 4);
        }

        public static void Encode16_16(int n, IntPtr source, IntPtr dest)
        {
            short[] sourceArray = new short[n];
            Marshal.Copy(source, sourceArray, 0, n);

            short[] destArray = new short[n];

            Array.Copy(sourceArray, destArray, n);

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void Encode16_24(int n, IntPtr source, IntPtr dest)
        {
            short[] sourceArray = new short[n];
            Marshal.Copy(source, sourceArray, 0, n);

            byte[] destArray = new byte[n * 3];

            for (int i = 0; i < n; i++)
            {
                int destIndex = i * 3;
                int sourceIndex = i * 2;

                destArray[destIndex] = (byte)(sourceArray[sourceIndex] & 0xFF);
                destArray[destIndex + 1] = (byte)((sourceArray[sourceIndex] >> 8) & 0xFF);
                destArray[destIndex + 2] = (byte)(sourceArray[sourceIndex + 1] & 0xFF);
            }

            Marshal.Copy(destArray, 0, dest, n * 3);
        }

        public static void Encode16_32(int n, IntPtr source, IntPtr dest)
        {
            short[] sourceArray = new short[n];
            Marshal.Copy(source, sourceArray, 0, n);

            int[] destArray = new int[n];

            for (int i = 0; i < n; i++)
            {
                destArray[i] = sourceArray[i];
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void Encode24_24(int n, IntPtr source, IntPtr dest)
        {
            byte[] sourceArray = new byte[n * 3];
            Marshal.Copy(source, sourceArray, 0, n * 3);

            byte[] destArray = new byte[n * 3];

            Array.Copy(sourceArray, destArray, n * 3);

            Marshal.Copy(destArray, 0, dest, n * 3);
        }

        public static void Encode24_32(int n, IntPtr source, IntPtr dest)
        {
            byte[] sourceArray = new byte[n * 3];
            Marshal.Copy(source, sourceArray, 0, n * 3);

            int[] destArray = new int[n];

            for (int i = 0; i < n; i++)
            {
                int sourceIndex = i * 3;

                destArray[i] = (sourceArray[sourceIndex] & 0xFF) |
                               ((sourceArray[sourceIndex + 1] & 0xFF) << 8) |
                               ((sourceArray[sourceIndex + 2] & 0xFF) << 16);
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void Encode32_32(int n, IntPtr source, IntPtr dest)
        {
            int[] sourceArray = new int[n];
            Marshal.Copy(source, sourceArray, 0, n);
            int[] destArray = new int[n];

            Array.Copy(sourceArray, destArray, n);

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void EncodeL_8(int n, int bits, IntPtr source, IntPtr dest)
        {
            byte[] sourceArray = new byte[n];
            Marshal.Copy(source, sourceArray, 0, n);

            byte[] destArray = new byte[n];

            byte b = 0;
            int bitsLeft = 8;
            int bitsWritten = 0;

            for (int cur = 0; cur < n; cur++)
            {
                byte db = sourceArray[cur];

                bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte tb = (byte)((db & ((0xFF >> (8 - (bits - bitsWritten))) << bitsWritten)) >> bitsWritten);
                        b |= (byte)(tb << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits;
                    }
                    else
                    {
                        byte tb = (byte)((db & ((0xFF >> (8 - bitsLeft)) << bitsWritten)) >> bitsWritten);
                        b |= (byte)(tb << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    if (bitsLeft == 0)
                    {
                        destArray[cur] = b;
                        b = 0;
                        bitsLeft = 8;
                        cur++;
                    }
                }
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void EncodeL_16(int n, int bits, IntPtr source, IntPtr dest)
        {
            short[] sourceArray = new short[n];
            Marshal.Copy(source, sourceArray, 0, n);

            byte[] destArray = new byte[n];

            byte b = 0;
            int bitsLeft = 8;
            int bitsWritten = 0;

            for (int cur = 0; cur < n; cur++)
            {
                short dw = sourceArray[cur];

                bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte tb = (byte)((dw & ((0xFF >> (8 - (bits - bitsWritten))) << bitsWritten)) >> bitsWritten);
                        b |= (byte)(tb << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits;
                    }
                    else
                    {
                        byte tb = (byte)((dw & ((0xFF >> (8 - bitsLeft)) << bitsWritten)) >> bitsWritten);
                        b |= (byte)(tb << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    if (bitsLeft == 0)
                    {
                        destArray[cur] = b;
                        b = 0;
                        bitsLeft = 8;
                        cur++;
                    }
                }
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void EncodeL_24(int n, int bits, IntPtr source, IntPtr dest)
        {
            byte[] sourceArray = new byte[n * 3];
            Marshal.Copy(source, sourceArray, 0, n * 3);

            byte[] destArray = new byte[n];

            byte b = 0;
            int bitsLeft = 8;
            int bitsWritten = 0;

            for (int cur = 0; cur < n; cur++)
            {
                int d24 = (sourceArray[cur * 3]) |
                          (sourceArray[cur * 3 + 1] << 8) |
                          (sourceArray[cur * 3 + 2] << 16);

                bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte tb = (byte)(d24 & (0xFF >> (8 - (bits - bitsWritten))));
                        d24 >>= bits - bitsWritten;
                        b |= (byte)(tb << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits;
                    }
                    else
                    {
                        byte tb = (byte)(d24 & (0xFF >> (8 - bitsLeft)));
                        d24 >>= bitsLeft;
                        b |= (byte)(tb << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    if (bitsLeft == 0)
                    {
                        destArray[cur] = b;
                        b = 0;
                        bitsLeft = 8;
                        cur++;
                    }
                }
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void EncodeL_32(int n, int bits, IntPtr source, IntPtr dest)
        {
            int[] sourceArray = new int[n];
            Marshal.Copy(source, sourceArray, 0, n);

            byte[] destArray = new byte[n];

            byte b = 0;
            int bitsLeft = 8;
            int bitsWritten = 0;

            for (int cur = 0; cur < n; cur++)
            {
                int dw = sourceArray[cur];

                bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte tb = (byte)((dw & ((0xFF >> (8 - (bits - bitsWritten))) << bitsWritten)) >> bitsWritten);
                        b |= (byte)(tb << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits;
                    }
                    else
                    {
                        byte tb = (byte)((dw & ((0xFF >> (8 - bitsLeft)) << bitsWritten)) >> bitsWritten);
                        b |= (byte)(tb << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    if (bitsLeft == 0)
                    {
                        destArray[cur] = b;
                        b = 0;
                        bitsLeft = 8;
                        cur++;
                    }
                }
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void Fill16_8rel(int n, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sourceArray = new byte[n];
            Marshal.Copy(source, sourceArray, 0, n);

            short[] destArray = new short[n];

            destArray[0] = (short)baseValue;

            for (int i = 1; i < n; i++)
            {
                destArray[i] = (short)(sourceArray[i - 1] + destArray[i - 1]);
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void Fill24_8rel(int n, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sourceArray = new byte[n * 8];
            Marshal.Copy(source, sourceArray, 0, n * 8);

            int[] destArray = new int[n * 3];

            int ti = baseValue;
            destArray[0] = ti & 0xFF;
            destArray[1] = (ti >> 8) & 0xFF;
            destArray[2] = (ti >> 16) & 0xFF;

            int sourceIndex = 0;

            for (int i = 1; i < n; i++)
            {
                int di = sourceArray[sourceIndex];
                sourceIndex++;

                di = ChangeIntSign(di, 8);

                ti += di;
                destArray[i * 3] = ti & 0xFF;
                destArray[i * 3 + 1] = (ti >> 8) & 0xFF;
                destArray[i * 3 + 2] = (ti >> 16) & 0xFF;
            }

            Marshal.Copy(destArray, 0, dest, n * 3);
        }

        public static void Fill24_16rel(int n, IntPtr source, IntPtr dest, int baseValue)
        {
            short[] sourceArray = new short[n * 16];
            Marshal.Copy(source, sourceArray, 0, n * 16);

            int[] destArray = new int[n * 3];

            int ti = baseValue;
            destArray[0] = ti & 0xFF;
            destArray[1] = (ti >> 8) & 0xFF;
            destArray[2] = (ti >> 16) & 0xFF;

            int sourceIndex = 0;

            for (int i = 1; i < n; i++)
            {
                int di = sourceArray[sourceIndex];
                sourceIndex++;

                di = ChangeIntSign(di, 16);

                ti += di;
                destArray[i * 3] = ti & 0xFF;
                destArray[i * 3 + 1] = (ti >> 8) & 0xFF;
                destArray[i * 3 + 2] = (ti >> 16) & 0xFF;
            }

            Marshal.Copy(destArray, 0, dest, n * 3);
        }

        public static void Fill32_8rel(int n, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sourceArray = new byte[n];
            Marshal.Copy(source, sourceArray, 0, n);

            int[] destArray = new int[n];

            destArray[0] = baseValue;

            for (int i = 1; i < n; i++)
            {
                destArray[i] = sourceArray[i - 1] + destArray[i - 1];
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void Fill32_16rel(int n, IntPtr source, IntPtr dest, int baseValue)
        {
            short[] sourceArray = new short[n];
            Marshal.Copy(source, sourceArray, 0, n);

            int[] destArray = new int[n];

            destArray[0] = baseValue;

            for (int i = 1; i < n; i++)
            {
                destArray[i] = sourceArray[i - 1] + destArray[i - 1];
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void Fill32_24rel(int n, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sourceArray = new byte[n * 3];
            Marshal.Copy(source, sourceArray, 0, n * 3);

            int[] destArray = new int[n];

            int it1 = baseValue;
            destArray[0] = it1;

            int sourceIndex = 0;

            for (int i = 1; i < n; i++)
            {
                int it = (sourceArray[sourceIndex] & 0xFF) |
                ((sourceArray[sourceIndex + 1] & 0xFF) << 8) |
                ((sourceArray[sourceIndex + 2] & 0xFF) << 16);

                if ((sourceArray[sourceIndex + 2] & 0x80) != 0)
                {
                    it = (int)((uint)it | 0xFF000000);
                }
                sourceIndex += 3;

                it += destArray[i - 1];
                destArray[i] = it;
            }

            Marshal.Copy(destArray, 0, dest, n * 3);
        }

        public static void Fill8abs(int n, IntPtr source, IntPtr dest)
        {
            byte[] buffer = new byte[n];
            Marshal.Copy(source, buffer, 0, n);
            Marshal.Copy(buffer, 0, dest, n);
        }

        public static void Fill16abs(int n, IntPtr source, IntPtr dest)
        {
            byte[] buffer = new byte[n * 2];
            Marshal.Copy(source, buffer, 0, n * 2);
            Marshal.Copy(buffer, 0, dest, n * 2);
        }

        public static void Fill24abs(int n, IntPtr source, IntPtr dest)
        {
            byte[] buffer = new byte[n * 3];
            Marshal.Copy(source, buffer, 0, n * 3);
            Marshal.Copy(buffer, 0, dest, n * 3);
        }

        public static void Fill32abs(int n, IntPtr source, IntPtr dest)
        {
            byte[] buffer = new byte[n * 4];
            Marshal.Copy(source, buffer, 0, n * 4);
            Marshal.Copy(buffer, 0, dest, n * 4);
        }

        public static void Fill8_bits(int n, int bits, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sourceArray = new byte[n];
            Marshal.Copy(source, sourceArray, 0, n);

            byte[] destArray = new byte[n];
            destArray[0] = (byte)baseValue;

            int bitsTotal = 0;
            int sourceIndex = 0;
            byte tb = sourceArray[sourceIndex];
            sourceIndex++;

            for (int i = 1; i < n; i++)
            {
                short ds = 0;
                for (int j = 0; j < bits; j++)
                {
                    ds |= (short)((tb & 1) << j);
                    tb >>= 1;
                    bitsTotal++;
                    if (bitsTotal == 8)
                    {
                        bitsTotal = 0;
                        tb = sourceArray[sourceIndex];
                        sourceIndex++;
                    }
                }

                // checks if the most significant bit of dd is set, 
                // and if it is, set all the bits above the bits position to 1. 
                // if ((ds & (1 << (bits - 1))) != 0)
                // {
                //     ds |= (short)(0xFF << bits);
                // }
                ds = (short)ChangeIntSign(ds, bits);

                ds += destArray[i - 1];
                destArray[i] = (byte)ds;
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void Fill16_bits(int n, int bits, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sourceArray = new byte[n];
            Marshal.Copy(source, sourceArray, 0, n);

            short[] destArray = new short[n];
            destArray[0] = (short)baseValue;

            int bitsLeft = 8;
            int sourceIndex = 0;
            byte tb = sourceArray[sourceIndex];
            sourceIndex++;

            for (int i = 1; i < n; i++)
            {
                short ds = 0;
                int bitsNeeded = bits;

                while (bitsNeeded > 0)
                {
                    if (bitsNeeded >= bitsLeft)
                    {
                        ds |= (short)((tb & (0xFF >> (8 - bitsLeft))) << (bits - bitsNeeded));
                        tb = sourceArray[sourceIndex];
                        sourceIndex++;
                        bitsNeeded -= bitsLeft;
                        bitsLeft = 8;
                    }
                    else
                    {
                        ds |= (short)((tb & (0xFF >> (8 - bitsNeeded))) << (bits - bitsNeeded));
                        tb >>= bitsNeeded;
                        bitsLeft -= bitsNeeded;
                        bitsNeeded = 0;
                    }
                }

                // checks if the most significant bit of dd is set, 
                // and if it is, set all the bits above the bits position to 1. 
                // if ((ds & (1 << (bits - 1))) != 0)
                // {
                //     ds |= (short)(0xFFFF << bits);
                // }
                ds = (short)ChangeIntSign(ds, bits);

                ds += destArray[i - 1];
                destArray[i] = ds;
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void Fill24_bits(int n, int bits, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sourceArray = new byte[bits * 64];
            Marshal.Copy(source, sourceArray, 0, bits * 64);

            int[] destArray = new int[n * 3];

            int ti = baseValue;
            destArray[0] = ti & 0xFF;
            destArray[1] = (ti >> 8) & 0xFF;
            destArray[2] = (ti >> 16) & 0xFF;

            int bitsLeft = 8;
            int sourceIndex = 0;
            byte tb = sourceArray[sourceIndex];
            sourceIndex++;

            for (int i = 1; i < n; i++)
            {
                int di = 0;
                int bitsNeeded = bits;

                while (bitsNeeded > 0)
                {
                    if (bitsNeeded >= bitsLeft)
                    {
                        di |= (tb & (0xFF >> (8 - bitsLeft))) << (bits - bitsNeeded);
                        tb = sourceArray[sourceIndex];
                        sourceIndex++;
                        bitsNeeded -= bitsLeft;
                        bitsLeft = 8;
                    }
                    else
                    {
                        di |= (tb & (0xFF >> (8 - bitsNeeded))) << (bits - bitsNeeded);
                        tb >>= bitsNeeded;
                        bitsLeft -= bitsNeeded;
                        bitsNeeded = 0;
                    }
                }

                // checks if the most significant bit of dd is set, 
                // and if it is, set all the bits above the bits position to 1. 
                // if ((di & (1 << (bits - 1))) != 0)
                // {
                //     di = (int)((uint)di | (0xFFFFFFFF << bits));
                // }
                di = ChangeIntSign(di, bits);

                ti += di;
                destArray[i * 3] = ti & 0xFF;
                destArray[i * 3 + 1] = (ti >> 8) & 0xFF;
                destArray[i * 3 + 2] = (ti >> 16) & 0xFF;
            }

            Marshal.Copy(destArray, 0, dest, n * 3);
        }

        public static void Fill32_bits(int n, int bits, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sourceArray = new byte[n];
            Marshal.Copy(source, sourceArray, 0, n);

            int[] destArray = new int[n];
            destArray[0] = baseValue;

            int bitsLeft = 8;
            int sourceIndex = 0;
            byte tb = sourceArray[sourceIndex];
            sourceIndex++;

            for (int i = 1; i < n; i++)
            {
                int di = 0;
                int bitsNeeded = bits;

                while (bitsNeeded > 0)
                {
                    if (bitsNeeded >= bitsLeft)
                    {
                        di |= (tb & (0xFF >> (8 - bitsLeft))) << (bits - bitsNeeded);
                        tb = sourceArray[sourceIndex];
                        sourceIndex++;
                        bitsNeeded -= bitsLeft;
                        bitsLeft = 8;
                    }
                    else
                    {
                        di |= (tb & (0xFF >> (8 - bitsNeeded))) << (bits - bitsNeeded);
                        tb >>= bitsNeeded;
                        bitsLeft -= bitsNeeded;
                        bitsNeeded = 0;
                    }
                }

                // checks if the most significant bit of dd is set, 
                // and if it is, set all the bits above the bits position to 1. 
                // if ((di & (1 << (bits - 1))) != 0)
                // {
                //     di = (int)((uint)di | (0xFFFFFFFF << bits));
                // }
                di = ChangeIntSign(di, bits);

                di += destArray[i - 1];
                destArray[i] = di;
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void FillIntegers(int n, int bits, IntPtr data, int start, ref int[] ints, bool relative)
        {
            switch (bits)
            {
                // case 8: FillIntegers8(n, data, ref ints, false); break;
                // case 16: FillIntegers16(n, data, ref ints, false); break;
                // case 24: FillIntegers24(n, data, ref ints, false); break;
                // case 32: FillIntegers32(n, data, ref ints, false); break;
                default: FillIntegersL8(n, bits, data, ref ints); break;
            }

            if (relative)
            {
                ints[0] = start + ints[0];
                for (int i = 1; i < n; i++)
                {
                    ints[i] = ints[i - 1] + ints[i];
                }
            }
        }

        public static void FillIntegersAbs(int n, int bits, IntPtr data, int start, ref int[] ints)
        {
            switch (bits)
            {
                case 8: FillIntegers8(n, data, ref ints, true); break;
                case 16: FillIntegers16(n, data, ref ints, true); break;
                case 24: FillIntegers24(n, data, ref ints, true); break;
                case 32: FillIntegers32(n, data, ref ints, true); break;
            }
        }

        public static void FillBits(int n, int bits, IntPtr data, int[] ints)
        {
            switch (bits)
            {
                // case 8: FillBits8(n, ints, data, false); break;
                // case 16: FillBits16(n, ints, data, false); break;
                // case 24: FillBits24(n, ints, data, false); break;
                // case 32: FillBits32(n, ints, data, false); break;
                default: FillBitsL8(n, bits, ints, data); break;
            }
        }

        public static void FillBitsAbs(int n, int bits, IntPtr data, int[] ints)
        {
            switch (bits)
            {
                case 8: FillBits8(n, ints, data, true); break;
                case 16: FillBits16(n, ints, data, true); break;
                case 24: FillBits24(n, ints, data, true); break;
                case 32: FillBits32(n, ints, data, true); break;
            }
        }

        public static void Fill8(int n, int bits, IntPtr source, int base_value, IntPtr dest, bool relative)
        {
            if (relative)
            {
                Fill8_bits(n, bits, source, dest, base_value);
            }
            else
            {
                Fill8abs(n, source, dest);
            }
        }

        public static void Fill16(int n, int bits, IntPtr source, int base_value, IntPtr dest, bool relative)
        {
            if (relative)
            {
                switch (bits)
                {
                    // case 8: Fill16_8rel(n, source, dest, base_value); break;
                    default: Fill16_bits(n, bits, source, dest, base_value); break;
                }
            }
            else
            {
                Fill16abs(n, source, dest);
            }
        }

        public static void Fill24(int n, int bits, IntPtr source, int base_value, IntPtr dest, bool relative)
        {
            if (relative)
            {
                switch (bits)
                {
                    // case 8: Fill24_8rel(n, source, dest, base_value); break;
                    // case 16: Fill24_16rel(n, source, dest, base_value); break;
                    default: Fill24_bits(n, bits, source, dest, base_value); break;
                }
            }
            else
            {
                Fill24abs(n, source, dest);
            }
        }

        public static void Fill32(int n, int bits, IntPtr source, int base_value, IntPtr dest, bool relative)
        {
            if (relative)
            {
                switch (bits)
                {
                    // case 8: Fill32_8rel(n, source, dest, base_value); break;
                    // case 16: Fill32_16rel(n, source, dest, base_value); break;
                    // case 24: Fill32_24rel(n, source, dest, base_value); break;
                    default: Fill32_bits(n, bits, source, dest, base_value); break;
                }
            }
            else
            {
                Fill32abs(n, source, dest);
            }
        }

        public static void Encode_8(int n, int bits, IntPtr source,
                                        IntPtr dest)
        {
            if (bits == 8)
            {
                Encode8_8(n, source, dest);
            }
            else
            {
                EncodeL_8(n, bits, source, dest);
            }
        }

        public static void Encode_16(int n, int bits, IntPtr source,
                                        IntPtr dest)
        {
            if (bits == 8)
            {
                Encode8_16(n, source, dest);
            }
            else if (bits == 16)
            {
                Encode16_16(n, source, dest);
            }
            else
            {
                EncodeL_16(n, bits, source, dest);
            }

        }

        public static void Encode_24(int n, int bits, IntPtr source,
                                        IntPtr dest)
        {
            if (bits == 8)
            {
                Encode8_24(n, source, dest);
            }
            else if (bits == 16)
            {
                Encode16_24(n, source, dest);
            }
            else if (bits == 24)
            {
                Encode24_24(n, source, dest);
            }
            else
            {
                EncodeL_24(n, bits, source, dest);
            }
        }

        public static void Encode_32(int n, int bits, IntPtr source, IntPtr dest)
        {
            if (bits == 8)
            {
                Encode8_32(n, source, dest);
            }
            else if (bits == 16)
            {
                Encode16_32(n, source, dest);
            }
            else if (bits == 24)
            {
                Encode24_32(n, source, dest);
            }
            else if (bits == 32)
            {
                Encode32_32(n, source, dest);
            }
            else
            {
                EncodeL_32(n, bits, source, dest);
            }
        }
    }
}