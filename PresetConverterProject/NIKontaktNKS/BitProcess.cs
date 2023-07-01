using System.Runtime.InteropServices;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class BitProcess
    {
        public static byte nbits8(int n)
        {
            int i;
            if (n > 8) return 0xFF;
            else if (n == 0) return 0;
            else
            {
                byte result = 1;
                for (i = 2; i <= n; i++)
                    result = (byte)(result << 1 + 1);
                return result;
            }
        }

        public static ushort nbits16(int n)
        {
            int i;
            if (n > 16) return 0xFFFF;
            else if (n == 0) return 0;
            else
            {
                ushort result = 1;
                for (i = 2; i <= n; i++)
                    result = (ushort)(result << 1 + 1);
                return result;
            }
        }

        public static uint nbits32(int n)
        {
            int i;
            if (n > 32) return 0xFFFFFFFF;
            else if (n == 0) return 0;
            else
            {
                uint result = 1;
                for (i = 2; i <= n; i++)
                    result = result << 1 + 1;
                return result;
            }
        }

        public static uint nbits32old(int n)
        {
            int i;
            if (n > 16) return 0xFFFFFFFF;
            else if (n == 0) return 0;
            else
            {
                uint result = 1;
                for (i = 2; i <= n; i++)
                    result <<= 1 + 1;
                return result;
            }
        }

        public static int ChangeIntSign(int i, int sbit)
        {
            uint dw;
            if ((i & (1 << (sbit - 1))) != 0)
            {
                dw = (uint)i;
                dw |= nbits32(32 - sbit) << sbit;
                return (int)dw;
            }
            else
                return i;
        }

        public static void FillIntegers8(int n, IntPtr data, ref int[] ints, bool abs_)
        {
            int cur;
            short[] s = new short[n];
            Marshal.Copy(data, s, 0, n);

            int start = abs_ ? 0 : 1;

            for (cur = start; cur < n; cur++)
            {
                ints[cur] = s[cur];
            }

            if (!abs_)
            {
                ints[0] = 0;
            }
        }

        public static void FillIntegers16(int n, IntPtr data, ref int[] ints, bool abs_)
        {
            int cur;
            short[] s = new short[n];
            Marshal.Copy(data, s, 0, n);

            int start = abs_ ? 0 : 1;

            for (cur = start; cur < n; cur++)
            {
                ints[cur] = s[cur];
            }

            if (!abs_)
            {
                ints[0] = 0;
            }
        }

        public static void FillIntegers24(int n, IntPtr data, ref int[] ints, bool abs_)
        {
            int t;
            uint dw;
            int cur;
            byte[] b = new byte[n * sizeof(byte)];
            Marshal.Copy(data, b, 0, b.Length);

            int start = abs_ ? 0 : 1;

            for (cur = start; cur < n; cur++)
            {
                dw = b[cur];
                dw += (uint)(b[cur + 1] << 8);
                dw += (uint)(b[cur + 2] << 16);

                if ((b[cur] & 128) == 0)
                    t = (int)dw;
                else
                    t = ChangeIntSign((int)dw, 24);

                ints[cur] = t;
            }

            if (!abs_)
            {
                ints[0] = 0;
            }
        }

        public static void FillIntegers32(int n, IntPtr data, ref int[] ints, bool abs_)
        {
            int cur;
            int[] ip = new int[n];
            Marshal.Copy(data, ip, 0, n);

            int start = abs_ ? 0 : 1;

            for (cur = start; cur < n; cur++)
            {
                ints[cur] = ip[cur];
            }

            if (!abs_)
            {
                ints[0] = 0;
            }
        }

        public static void FillIntegersL8(int n, int bits, IntPtr data, ref int[] ints)
        {
            int j;
            int t;
            uint dw;
            int cur;
            byte[] b = new byte[n];
            Marshal.Copy(data, b, 0, n);

            byte tb = b[0];
            int bitstotal = 0;

            for (cur = 1; cur < n; cur++)
            {
                dw = 0;
                for (j = 0; j < bits; j++)
                {
                    dw += (uint)(tb & 1) << j;
                    tb >>= 1;
                    bitstotal++;
                    if (bitstotal == 8)
                    {
                        tb = b[cur];
                        bitstotal = 0;
                    }
                }
                t = ChangeIntSign((int)dw, bits);
                ints[cur] = t;
            }
            ints[0] = 0;
        }

        public static void FillBits8(int n, int[] ints, IntPtr data, bool abs_)
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

        public static void FillBits16(int n, int[] ints, IntPtr data, bool abs_)
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

        public static void FillBits24(int n, int[] ints, IntPtr data, bool abs_)
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
                    b[cur * 3 + 2] |= 0x80;
            }
            Marshal.Copy(b, 0, data, n * 3);
        }

        public static void FillBits32(int n, int[] ints, IntPtr data, bool abs_)
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

        public static void FillBitsL8(int n, int bits, int[] ints, IntPtr data)
        {
            byte[] b = new byte[n];
            Marshal.Copy(data, b, 0, n);

            int tb = 0;
            int bitswritten = 0;

            for (int cur = 0; cur < n; cur++)
            {
                uint dw = (uint)ints[cur];
                for (int j = 0; j < bits; j++)
                {
                    tb += (byte)((dw & 1) << bitswritten);
                    dw >>= 1;
                    bitswritten++;
                    if (bitswritten == 8)
                    {
                        b[cur] = (byte)tb;
                        tb = 0;
                        bitswritten = 0;
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
                        bitsLeft -= (bits - bitsWritten);
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
                        bitsLeft -= (bits - bitsWritten);
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
                int d24 = (sourceArray[cur * 3]) | (sourceArray[cur * 3 + 1] << 8) | (sourceArray[cur * 3 + 2] << 16);

                bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte tb = (byte)(d24 & (0xFF >> (8 - (bits - bitsWritten))));
                        d24 >>= (bits - bitsWritten);
                        b |= (byte)(tb << (8 - bitsLeft));
                        bitsLeft -= (bits - bitsWritten);
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
                        bitsLeft -= (bits - bitsWritten);
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
            short[] ss = new short[n];
            Marshal.Copy(source, ss, 0, n);
            short[] sd = new short[n];

            sd[0] = (short)baseValue;

            for (int i = 1; i < n; i++)
            {
                sd[i] = (short)(ss[i - 1] + sd[i - 1]);
            }

            Marshal.Copy(sd, 0, dest, n);
        }

        public static void Fill24_8rel(int n, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] ss = new byte[n];
            Marshal.Copy(source, ss, 0, n);
            byte[] sd = new byte[n * 3];

            int it1 = baseValue;
            sd[0] = (byte)(it1 & 0xFF);
            sd[1] = (byte)((it1 >> 8) & 0xFF);
            sd[2] = (byte)((it1 >> 16) & 0xFF);

            for (int i = 1; i < n; i++)
            {
                int it2 = ss[i - 1];
                it1 += it2;
                sd[i * 3] = (byte)(it1 & 0xFF);
                sd[i * 3 + 1] = (byte)((it1 >> 8) & 0xFF);
                sd[i * 3 + 2] = (byte)((it1 >> 16) & 0xFF);
            }

            Marshal.Copy(sd, 0, dest, n * 3);
        }

        public static void Fill24_16rel(int n, IntPtr source, IntPtr dest, int baseValue)
        {
            short[] ss = new short[n];
            Marshal.Copy(source, ss, 0, n);
            byte[] sd = new byte[n * 3];

            int it1 = baseValue;
            sd[0] = (byte)(it1 & 0xFF);
            sd[1] = (byte)((it1 >> 8) & 0xFF);
            sd[2] = (byte)((it1 >> 16) & 0xFF);

            for (int i = 1; i < n; i++)
            {
                int it2 = ss[i - 1];
                it1 += it2;
                sd[i * 3] = (byte)(it1 & 0xFF);
                sd[i * 3 + 1] = (byte)((it1 >> 8) & 0xFF);
                sd[i * 3 + 2] = (byte)((it1 >> 16) & 0xFF);
            }

            Marshal.Copy(sd, 0, dest, n * 3);
        }

        public static void Fill32_8rel(int n, IntPtr source, IntPtr dest, int baseValue)
        {
            short[] ss = new short[n];
            Marshal.Copy(source, ss, 0, n);
            int[] sd = new int[n];

            sd[0] = baseValue;

            for (int i = 1; i < n; i++)
            {
                sd[i] = ss[i - 1] + sd[i - 1];
            }

            Marshal.Copy(sd, 0, dest, n);
        }

        public static void Fill32_16rel(int n, IntPtr source, IntPtr dest, int baseValue)
        {
            short[] ss = new short[n];
            Marshal.Copy(source, ss, 0, n);
            int[] sd = new int[n];

            sd[0] = baseValue;

            for (int i = 1; i < n; i++)
            {
                sd[i] = ss[i - 1] + sd[i - 1];
            }

            Marshal.Copy(sd, 0, dest, n);
        }

        public static void Fill32_24rel(int n, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] ss = new byte[n * 3];
            Marshal.Copy(source, ss, 0, n * 3);
            int[] sd = new int[n];

            int it1 = baseValue;
            sd[0] = it1;

            for (int i = 1; i < n; i++)
            {
                int it = (ss[i * 3]) | (ss[i * 3 + 1] << 8) | (ss[i * 3 + 2] << 16);
                if ((ss[i * 3 + 2] & 0x80) != 0)
                {
                    it = it | unchecked((int)0xFF000000);
                }
                it += sd[i - 1];
                sd[i] = it;
            }

            Marshal.Copy(sd, 0, dest, n);
        }

        public static void Fill8abs(int n, IntPtr source, IntPtr dest)
        {
            byte[] buffer = new byte[n];
            Marshal.Copy(source, buffer, 0, n);
            Marshal.Copy(buffer, 0, dest, n);
        }

        public static void Fill16abs(int n, IntPtr source, IntPtr dest)
        {
            short[] buffer = new short[n];
            Marshal.Copy(source, buffer, 0, n);
            Marshal.Copy(buffer, 0, dest, n);
        }

        public static void Fill24abs(int n, IntPtr source, IntPtr dest)
        {
            int[] buffer = new int[n];
            Marshal.Copy(source, buffer, 0, n);
            Marshal.Copy(buffer, 0, dest, n);
        }

        public static void Fill32abs(int n, IntPtr source, IntPtr dest)
        {
            int[] buffer = new int[n];
            Marshal.Copy(source, buffer, 0, n);
            Marshal.Copy(buffer, 0, dest, n);
        }

        public static void Fill8_bits(int n, int bits, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sBuffer = new byte[n];
            byte[] dBuffer = new byte[n];

            Marshal.Copy(source, sBuffer, 0, n);
            dBuffer[0] = (byte)baseValue;

            int bitstotal = 0;
            byte tb = sBuffer[0];

            for (int i = 1; i < n; i++)
            {
                short db = 0;
                for (int j = 0; j < bits; j++)
                {
                    db |= (short)((tb & 1) << j);
                    tb >>= 1;
                    bitstotal++;
                    if (bitstotal == 8)
                    {
                        bitstotal = 0;
                        tb = sBuffer[i];
                    }
                }

                if ((db & (1 << (bits - 1))) != 0)
                    db |= (short)(0xFF << bits);

                db += dBuffer[i - 1];
                dBuffer[i] = (byte)db;
            }

            Marshal.Copy(dBuffer, 0, dest, n);
        }

        public static void Fill16_bits(int n, int bits, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sBuffer = new byte[n];
            short[] dBuffer = new short[n];

            Marshal.Copy(source, sBuffer, 0, n);
            dBuffer[0] = (short)baseValue;

            int bitsLeft = 8;
            byte tb = sBuffer[0];

            for (int i = 1; i < n; i++)
            {
                short dw = 0;
                int bitsNeeded = bits;

                while (bitsNeeded > 0)
                {
                    if (bitsNeeded >= bitsLeft)
                    {
                        dw |= (short)((tb & (0xFF >> (8 - bitsLeft))) << (bits - bitsNeeded));
                        tb = sBuffer[i];
                        bitsNeeded -= bitsLeft;
                        bitsLeft = 8;
                    }
                    else
                    {
                        dw |= (short)((tb & (0xFF >> (8 - bitsNeeded))) << (bits - bitsNeeded));
                        tb >>= bitsNeeded;
                        bitsLeft -= bitsNeeded;
                        bitsNeeded = 0;
                    }
                }

                if ((dw & (1 << (bits - 1))) != 0)
                    dw |= (short)(0xFFFF << bits);

                dw += dBuffer[i - 1];
                dBuffer[i] = dw;
            }

            Marshal.Copy(dBuffer, 0, dest, n);
        }

        public static void Fill24V2(int n, int bits, byte[] source, int base_value, int[] dest, bool relative)
        {
            if (relative)
                switch (bits)
                {
                    // case 8: Fill24_8rel(n, source, dest, (int)base_value); break;
                    // case 16: Fill24_16rel(n, source, dest, (int)base_value); break;
                    default: Fill24_bitsV2(n, bits, source, dest, base_value); break;
                }
            else
            {
                Fill24absV2(n, source, dest);
            }
        }

        public static void Fill24absV2(int n, byte[] source, int[] dest)
        {
            Buffer.BlockCopy(source, 0, dest, 0, n);
        }

        public static void Fill24_bits(int n, int bits, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sBuffer = new byte[n];
            int[] dBuffer = new int[n * 3];

            Marshal.Copy(source, sBuffer, 0, n);

            int ti = baseValue;
            dBuffer[0] = ti & 0xFF;
            dBuffer[1] = (ti >> 8) & 0xFF;
            dBuffer[2] = (ti >> 16) & 0xFF;

            byte tb = sBuffer[0];
            int bitsLeft = 8;

            for (int i = 1; i < n; i++)
            {
                int dd = 0;
                int bitsNeeded = bits;

                while (bitsNeeded > 0)
                {
                    if (bitsNeeded >= bitsLeft)
                    {
                        dd |= (tb & (0xFF >> (8 - bitsLeft))) << (bits - bitsNeeded);
                        tb = sBuffer[i];
                        bitsNeeded -= bitsLeft;
                        bitsLeft = 8;
                    }
                    else
                    {
                        dd |= (tb & (0xFF >> (8 - bitsNeeded))) << (bits - bitsNeeded);
                        tb >>= bitsNeeded;
                        bitsLeft -= bitsNeeded;
                        bitsNeeded = 0;
                    }
                }

                // checks if the most significant bit of dd is set, 
                // and if it is, it sets all the bits above the bits position to 1. 
                if ((dd & (1 << (bits - 1))) != 0)
                {
                    // dd |= unchecked((int)0xFFFFFFFF << bits);
                    dd |= (int)(0xFFFFFFFF << bits);
                }

                ti += dd;
                dBuffer[i * 3] = ti & 0xFF;
                dBuffer[i * 3 + 1] = (ti >> 8) & 0xFF;
                dBuffer[i * 3 + 2] = (ti >> 16) & 0xFF;
            }

            Marshal.Copy(dBuffer, 0, dest, n);
        }

        public static void Fill24_bitsV2(int n, int bits, byte[] source, int[] dest, int baseValue)
        {
            int ti = baseValue;
            dest[0] = ti & 0xFF;
            dest[1] = (ti >> 8) & 0xFF;
            dest[2] = (ti >> 16) & 0xFF;

            byte tb = source[0];
            int bitsLeft = 8;

            for (int i = 1; i < n; i++)
            {
                int dd = 0;
                int bitsNeeded = bits;

                while (bitsNeeded > 0 && i < source.Length)
                {
                    if (bitsNeeded >= bitsLeft)
                    {
                        dd |= (tb & (0xFF >> (8 - bitsLeft))) << (bits - bitsNeeded);
                        tb = source[i];
                        bitsNeeded -= bitsLeft;
                        bitsLeft = 8;
                    }
                    else
                    {
                        dd |= (tb & (0xFF >> (8 - bitsNeeded))) << (bits - bitsNeeded);
                        tb >>= bitsNeeded;
                        bitsLeft -= bitsNeeded;
                        bitsNeeded = 0;
                    }
                }

                // checks if the most significant bit of dd is set, 
                // and if it is, it sets all the bits above the bits position to 1. 
                // if ((dd & (1 << (bits - 1))) != 0)
                // {
                //     dd |= (int)(0xFFFFFFFF << bits);
                // }

                ti += dd;
                dest[i * 3] = ti & 0xFF;
                dest[i * 3 + 1] = (ti >> 8) & 0xFF;
                dest[i * 3 + 2] = (ti >> 16) & 0xFF;
            }
        }

        public static void Fill32_bits(int n, int bits, IntPtr source, IntPtr dest, int baseValue)
        {
            byte[] sBuffer = new byte[n];
            int[] dBuffer = new int[n];

            Marshal.Copy(source, sBuffer, 0, n);
            dBuffer[0] = baseValue;

            int bitsLeft = 8;
            byte tb = sBuffer[0];

            for (int i = 1; i < n; i++)
            {
                int dd = 0;
                int bitsNeeded = bits;

                while (bitsNeeded > 0)
                {
                    if (bitsNeeded >= bitsLeft)
                    {
                        dd |= (tb & (0xFF >> (8 - bitsLeft))) << (bits - bitsNeeded);
                        tb = sBuffer[i];
                        bitsNeeded -= bitsLeft;
                        bitsLeft = 8;
                    }
                    else
                    {
                        dd |= (tb & (0xFF >> (8 - bitsNeeded))) << (bits - bitsNeeded);
                        tb >>= bitsNeeded;
                        bitsLeft -= bitsNeeded;
                        bitsNeeded = 0;
                    }
                }

                if ((dd & (1 << (bits - 1))) != 0)
                    dd |= unchecked((int)0xFFFFFFFF << bits);

                dd += dBuffer[i - 1];
                dBuffer[i] = dd;
            }

            Marshal.Copy(dBuffer, 0, dest, n);
        }

        public static void FillIntegers(int n, int bits, IntPtr data, int start, ref int[] ints, bool relative)
        {
            if (bits == 8) FillIntegers8(n, data, ref ints, false);
            else if (bits == 16) FillIntegers16(n, data, ref ints, false);
            else if (bits == 24) FillIntegers24(n, data, ref ints, false);
            else if (bits == 32) FillIntegers32(n, data, ref ints, false);
            else FillIntegersL8(n, bits, data, ref ints);

            if (relative)
            {
                ints[0] = start + ints[0];
                for (int i = 1; i < n; i++)
                    ints[i] = ints[i - 1] + ints[i];
            }
        }

        public static void FillIntegersAbs(int n, int bits, IntPtr data, int start, ref int[] ints)
        {
            if (bits == 8) FillIntegers8(n, data, ref ints, true);
            else if (bits == 16) FillIntegers16(n, data, ref ints, true);
            else if (bits == 24) FillIntegers24(n, data, ref ints, true);
            else if (bits == 32) FillIntegers32(n, data, ref ints, true);
        }

        public static void FillBits(int n, int bits, IntPtr data, int[] ints)
        {
            if (bits == 8) FillBits8(n, ints, data, false);
            else if (bits == 16) FillBits16(n, ints, data, false);
            else if (bits == 24) FillBits24(n, ints, data, false);
            else if (bits == 32) FillBits32(n, ints, data, false);
            else FillBitsL8(n, bits, ints, data);

        }

        public static void FillBitsAbs(int n, int bits, IntPtr data, int[] ints)
        {
            if (bits == 8) FillBits8(n, ints, data, true);
            else if (bits == 16) FillBits16(n, ints, data, true);
            else if (bits == 24) FillBits24(n, ints, data, true);
            else if (bits == 32) FillBits32(n, ints, data, true);

        }

        public static void Fill8(int n, int bits, IntPtr source, int base_value, IntPtr dest, bool relative)
        {
            if (relative)
                Fill8_bits(n, bits, source, dest, (int)base_value);
            else
                Fill8abs(n, source, dest);

        }

        public static void Fill16(int n, int bits, IntPtr source, int base_value, IntPtr dest, bool relative)
        {
            if (relative)
                if (bits == 8)
                    Fill16_8rel(n, source, dest, (int)base_value);
                else
                    Fill16_bits(n, bits, source, dest, (int)base_value);
            else
                Fill16abs(n, source, dest);

        }

        public static void Fill24(int n, int bits, IntPtr source, int base_value, IntPtr dest, bool relative)
        {
            if (relative)
                switch (bits)
                {
                    case 8: Fill24_8rel(n, source, dest, (int)base_value); break;
                    case 16: Fill24_16rel(n, source, dest, (int)base_value); break;
                    default: Fill24_bits(n, bits, source, dest, (int)base_value); break;
                }
            else
            {
                Fill24abs(n, source, dest);
            }
        }

        public static void Fill32(int n, int bits, IntPtr source, int base_value, IntPtr dest, bool relative)
        {
            if (relative)
                switch (bits)
                {
                    case 8: Fill32_8rel(n, source, dest, (int)base_value); break;
                    case 16: Fill32_16rel(n, source, dest, (int)base_value); break;
                    case 24: Fill32_24rel(n, source, dest, (int)base_value); break;
                    default: Fill32_bits(n, bits, source, dest, (int)base_value); break;
                }
            else
            {
                Fill32abs(n, (IntPtr)(source), (IntPtr)(dest));
            }
        }

        public static void Encode_8(int n, int bits, IntPtr source,
                                        IntPtr dest)
        {
            if (bits == 8)
                Encode8_8(n, source, dest);
            else
                EncodeL_8(n, bits, source, dest);

        }

        public static void Encode_16(int n, int bits, IntPtr source,
                                        IntPtr dest)
        {
            if (bits == 8)
                Encode8_16(n, source, dest);
            else if (bits == 16)
                Encode16_16(n, source, dest);
            else
                EncodeL_16(n, bits, source, dest);

        }

        public static void Encode_24(int n, int bits, IntPtr source,
                                        IntPtr dest)
        {
            if (bits == 8)
                Encode8_24(n, source, dest);
            else if (bits == 16)
                Encode16_24(n, source, dest);
            else if (bits == 24)
                Encode24_24(n, source, dest);
            else
                EncodeL_24(n, bits, source, dest);

        }

        public static void Encode_32(int n, int bits, IntPtr source, IntPtr dest)
        {
            if (bits == 8)
                Encode8_32(n, source, dest);
            else if (bits == 16)
                Encode16_32(n, source, dest);
            else if (bits == 24)
                Encode24_32(n, source, dest);
            else if (bits == 32)
                Encode32_32(n, source, dest);
            else EncodeL_32(n, bits, source, dest);
        }

    }
}