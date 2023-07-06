using System.Runtime.InteropServices;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class BitProcess
    {
        /// <summary>
        /// Calculates the bitmask for 'n' bits.
        /// This returns a byte where n number of bytes are set to 1.
        /// </summary>
        /// <param name="n">number of bits to set</param>
        /// <returns>a bitmask where 'n' bits are set to 1</returns>
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

        /// <summary>
        /// Calculates the bitmask for 'n' bits.
        /// This returns a ushort where n number of bytes are set to 1.
        /// </summary>
        /// <param name="n">number of bits to set</param>
        /// <returns>a bitmask where 'n' bits are set to 1</returns>
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

        /// <summary>
        /// Calculates the bitmask for 'n' bits.
        /// This returns a uint where n number of bytes are set to 1.
        /// </summary>
        /// <param name="n">number of bits to set</param>
        /// <returns>a bitmask where 'n' bits are set to 1</returns>
        public static uint nbits32(int n)
        {
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

        /// <summary>
        /// Ensure the sign is kept when converting a nbit number to Int32.
        /// Checks if the most significant bit of destShort is set, 
        /// and if it is, set all the bits above the bits position to 1. 
        /// This ensures that the resulting 32-bit integer preserves the correct negative value.
        /// </summary>
        /// <param name="i">number to check</param>
        /// <param name="msbit">most significant bit</param>
        /// <returns>a Int32 number with the correct sign</returns>
        public static int ChangeIntSign(int i, int msbit)
        {
            // Checks if the most significant bit of 'i' is set
            if ((i & (1 << (msbit - 1))) != 0)
            {
                uint destUint = (uint)i;
                destUint |= nbits32(32 - msbit) << msbit;
                // Changes the sign of 'destShort' by applying a bitmask
                // to the upper bits based on the value of 'msbit'
                return (int)destUint;
            }
            else
            {
                return i;
            }
        }

        public static void FillIntegers8(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, bool abs)
        {
            int start = abs ? 0 : 1;

            int sourceIndex = 0;

            for (int cur = start; cur < n; cur++)
            {
                int srcByteAsInt = sourceSpan[sourceIndex];
                sourceIndex++;

                int destInt = ChangeIntSign(srcByteAsInt, 8);
                destSpan[cur] = destInt;
            }

            if (!abs)
            {
                destSpan[0] = 0;
            }
        }

        public static void FillIntegers16(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, bool abs)
        {
            // convert byte span into short span
            ReadOnlySpan<short> sourceArray = MemoryMarshal.Cast<byte, short>(sourceSpan);

            int start = abs ? 0 : 1;

            int sourceIndex = 0;

            for (int cur = start; cur < n; cur++)
            {
                short srcShort = sourceArray[sourceIndex];
                sourceIndex++;

                int destInt = ChangeIntSign(srcShort, 16);
                destSpan[cur] = destInt;
            }

            if (!abs)
            {
                destSpan[0] = 0;
            }
        }

        public static void FillIntegers24(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, bool abs)
        {
            int start = abs ? 0 : 1;

            int sourceIndex = 0;

            for (int cur = start; cur < n; cur++)
            {
                int srcInt = (sourceSpan[sourceIndex] & 0xFF) |
               ((sourceSpan[sourceIndex + 1] & 0xFF) << 8) |
               ((sourceSpan[sourceIndex + 2] & 0xFF) << 16);
                sourceIndex += 3;

                int destInt = ChangeIntSign(srcInt, 24);
                destSpan[cur] = destInt;
            }

            if (!abs)
            {
                destSpan[0] = 0;
            }
        }

        public static void FillIntegers32(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, bool abs)
        {
            // convert byte span into int span
            ReadOnlySpan<int> sourceArray = MemoryMarshal.Cast<byte, int>(sourceSpan);

            int start = abs ? 0 : 1;

            int sourceIndex = 0;

            for (int cur = start; cur < n; cur++)
            {
                int srcInt = sourceArray[sourceIndex];
                sourceIndex++;

                int destInt = ChangeIntSign(srcInt, 32);
                destSpan[cur] = destInt;
            }

            if (!abs)
            {
                destSpan[0] = 0;
            }
        }

        // can be used for all bits: L8 = not divisible by 8
        public static void FillIntegersL8(int n, int bits, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan)
        {
            int bitsTotal = 0;
            int sourceIndex = 0;
            byte srcByte = sourceSpan[sourceIndex];
            sourceIndex++;

            for (int cur = 1; cur < n; cur++)
            {
                int srcInt = 0;
                for (int j = 0; j < bits; j++)
                {
                    // Extracts 'bits' number of bits from 'srcByte'
                    srcInt += (srcByte & 1) << j;
                    srcByte >>= 1;
                    bitsTotal++;
                    if (bitsTotal == 8)
                    {
                        srcByte = sourceSpan[sourceIndex];
                        sourceIndex++;
                        bitsTotal = 0;
                    }
                }

                int destInt = ChangeIntSign(srcInt, bits);
                destSpan[cur] = destInt;
            }

            destSpan[0] = 0;
        }

        public static void FillBits8(int n, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan, bool abs)
        {
            for (int cur = 0; cur < n; cur++)
            {
                destSpan[cur] = (byte)sourceSpan[cur];
            }
        }

        public static void FillBits16(int n, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan, bool abs)
        {
            short[] destArray = new short[n];

            for (int cur = 0; cur < n; cur++)
            {
                destArray[cur] = (short)sourceSpan[cur];
            }

            MemoryMarshal.Cast<short, byte>(destArray).CopyTo(destSpan);
        }

        public static void FillBits24(int n, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan, bool abs)
        {
            for (int cur = 0; cur < n; cur++)
            {
                int srcInt = sourceSpan[cur];
                destSpan[cur * 3] = (byte)(srcInt & 0xFF);
                srcInt >>= 8;
                destSpan[cur * 3 + 1] = (byte)(srcInt & 0xFF);
                srcInt >>= 8;
                destSpan[cur * 3 + 2] = (byte)(srcInt & 0xFF);

                // make sure to preserve the minus sign
                if (sourceSpan[cur] < 0)
                {
                    destSpan[cur * 3 + 2] |= 0x80; // 0b10000000
                }
            }
        }

        public static void FillBits32(int n, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan, bool abs)
        {
            // Reinterpret the memory of the source span as a span of bytes
            ReadOnlySpan<byte> sourceBytes = MemoryMarshal.Cast<int, byte>(sourceSpan);

            // Check if the destination span has enough capacity to hold all the bytes
            if (destSpan.Length < n * 4)
            {
                throw new ArgumentException("Destination span is not large enough to hold all the bytes.");
            }

            // Copy the desired number of bytes from the source to the destination
            sourceBytes.Slice(0, n * 4).CopyTo(destSpan);
        }

        // can be used for all bits: L8 = not divisible by 8
        public static void FillBitsL8(int n, int bits, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan)
        {
            int destInt = 0;
            int bitsWritten = 0;

            for (int cur = 0; cur < n; cur++)
            {
                uint suint = (uint)sourceSpan[cur];
                for (int j = 0; j < bits; j++)
                {
                    destInt += (byte)((suint & 1) << bitsWritten);
                    suint >>= 1;
                    bitsWritten++;
                    if (bitsWritten == 8)
                    {
                        destSpan[cur] = (byte)destInt;
                        destInt = 0;
                        bitsWritten = 0;
                        cur++;
                    }
                }
            }
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

            byte destByte = 0;
            int bitsLeft = 8;

            for (int cur = 0; cur < n; cur++)
            {
                byte curSrc8 = sourceArray[cur];

                int bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte srcByte = (byte)((curSrc8 & ((0xFF >> (8 - (bits - bitsWritten))) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(srcByte << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits;
                    }
                    else
                    {
                        byte srcByte = (byte)((curSrc8 & ((0xFF >> (8 - bitsLeft)) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(srcByte << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    if (bitsLeft == 0)
                    {
                        destArray[cur] = destByte;
                        destByte = 0;
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

            byte destByte = 0;
            int bitsLeft = 8;

            for (int cur = 0; cur < n; cur++)
            {
                short curSrc16 = sourceArray[cur];

                int bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte srcByte = (byte)((curSrc16 & ((0xFF >> (8 - (bits - bitsWritten))) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(srcByte << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits;
                    }
                    else
                    {
                        byte srcByte = (byte)((curSrc16 & ((0xFF >> (8 - bitsLeft)) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(srcByte << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    if (bitsLeft == 0)
                    {
                        destArray[cur] = destByte;
                        destByte = 0;
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

            byte destByte = 0;
            int bitsLeft = 8;

            for (int cur = 0; cur < n; cur++)
            {
                int curSrc24 = (sourceArray[cur * 3]) |
                          (sourceArray[cur * 3 + 1] << 8) |
                          (sourceArray[cur * 3 + 2] << 16);

                int bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte srcByte = (byte)(curSrc24 & (0xFF >> (8 - (bits - bitsWritten))));
                        curSrc24 >>= bits - bitsWritten;
                        destByte |= (byte)(srcByte << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits;
                    }
                    else
                    {
                        byte srcByte = (byte)(curSrc24 & (0xFF >> (8 - bitsLeft)));
                        curSrc24 >>= bitsLeft;
                        destByte |= (byte)(srcByte << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    if (bitsLeft == 0)
                    {
                        destArray[cur] = destByte;
                        destByte = 0;
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

            byte destByte = 0;
            int bitsLeft = 8;

            for (int cur = 0; cur < n; cur++)
            {
                int curSrc32 = sourceArray[cur];

                int bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte srcByte = (byte)((curSrc32 & ((0xFF >> (8 - (bits - bitsWritten))) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(srcByte << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits;
                    }
                    else
                    {
                        byte srcByte = (byte)((curSrc32 & ((0xFF >> (8 - bitsLeft)) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(srcByte << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    if (bitsLeft == 0)
                    {
                        destArray[cur] = destByte;
                        destByte = 0;
                        bitsLeft = 8;
                        cur++;
                    }
                }
            }

            Marshal.Copy(destArray, 0, dest, n);
        }

        public static void Fill16_8rel(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, int baseValue)
        {
            destSpan[0] = (short)baseValue;

            int sourceIndex = 0;

            for (int i = 1; i < n; i++)
            {
                byte srcByte = sourceSpan[sourceIndex];
                sourceIndex++;

                destSpan[i] = (short)(srcByte + destSpan[i - 1]);
            }
        }

        public static void Fill24_8rel(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, int baseValue)
        {
            int destInt = baseValue;
            destSpan[0] = destInt & 0xFF;
            destSpan[1] = (destInt >> 8) & 0xFF;
            destSpan[2] = (destInt >> 16) & 0xFF;

            int sourceIndex = 0;

            for (int i = 1; i < n; i++)
            {
                int srcByteAsInt = sourceSpan[sourceIndex];
                sourceIndex++;

                srcByteAsInt = ChangeIntSign(srcByteAsInt, 8);

                destInt += srcByteAsInt;
                destSpan[i * 3] = destInt & 0xFF;
                destSpan[i * 3 + 1] = (destInt >> 8) & 0xFF;
                destSpan[i * 3 + 2] = (destInt >> 16) & 0xFF;
            }
        }

        public static void Fill24_16rel(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, int baseValue)
        {
            // convert byte span into short span
            ReadOnlySpan<short> sourceArray = MemoryMarshal.Cast<byte, short>(sourceSpan);

            int destInt = baseValue;
            destSpan[0] = destInt & 0xFF;
            destSpan[1] = (destInt >> 8) & 0xFF;
            destSpan[2] = (destInt >> 16) & 0xFF;

            int sourceIndex = 0;

            for (int i = 1; i < n; i++)
            {
                short srcShort = sourceArray[sourceIndex];
                sourceIndex++;

                srcShort = (short)ChangeIntSign(srcShort, 16);

                destInt += srcShort;
                destSpan[i * 3] = destInt & 0xFF;
                destSpan[i * 3 + 1] = (destInt >> 8) & 0xFF;
                destSpan[i * 3 + 2] = (destInt >> 16) & 0xFF;
            }
        }

        public static void Fill32_8rel(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, int baseValue)
        {
            destSpan[0] = baseValue;

            int sourceIndex = 0;

            for (int i = 1; i < n; i++)
            {
                byte srcByte = sourceSpan[sourceIndex];
                sourceIndex++;

                destSpan[i] = srcByte + destSpan[i - 1];
            }
        }

        public static void Fill32_16rel(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, int baseValue)
        {
            // convert byte span into short span
            ReadOnlySpan<short> sourceArray = MemoryMarshal.Cast<byte, short>(sourceSpan);

            destSpan[0] = baseValue;

            int sourceIndex = 0;

            for (int i = 1; i < n; i++)
            {
                short srcShort = sourceArray[sourceIndex];
                sourceIndex++;

                destSpan[i] = srcShort + destSpan[i - 1];
            }
        }

        public static void Fill32_24rel(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, int baseValue)
        {
            destSpan[0] = baseValue;

            int sourceIndex = 0;

            for (int i = 1; i < n; i++)
            {
                // extract a 24-bit integer value from three consecutive bytes in little-endian format.
                int destInt = (sourceSpan[sourceIndex] & 0xFF) |
                ((sourceSpan[sourceIndex + 1] & 0xFF) << 8) |
                ((sourceSpan[sourceIndex + 2] & 0xFF) << 16);
                sourceIndex += 3;

                // checks if the most significant bit of destInt is set, 
                // and if it is, set all the bits above the bits position to 1. 
                // this ensures that the resulting 32-bit Int32 preserves the correct negative value.
                destInt = ChangeIntSign(destInt, 24);

                destInt += destSpan[i - 1];
                destSpan[i] = destInt;
            }
        }

        public static void Fill8abs(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan)
        {
            var sourceBytes = MemoryMarshal.Cast<byte, int>(sourceSpan.Slice(0, n));
            var destIntegers = MemoryMarshal.Cast<int, int>(destSpan.Slice(0, n));

            sourceBytes.CopyTo(destIntegers);
        }

        public static void Fill16abs(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan)
        {
            var sourceBytes = MemoryMarshal.Cast<byte, int>(sourceSpan.Slice(0, n * 2));
            var destIntegers = MemoryMarshal.Cast<int, int>(destSpan.Slice(0, n));

            sourceBytes.CopyTo(destIntegers);
        }

        public static void Fill24abs(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan)
        {
            var sourceBytes = MemoryMarshal.Cast<byte, int>(sourceSpan.Slice(0, n * 3));
            var destIntegers = MemoryMarshal.Cast<int, int>(destSpan.Slice(0, n));

            sourceBytes.CopyTo(destIntegers);
        }

        public static void Fill32abs(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan)
        {
            var sourceBytes = MemoryMarshal.Cast<byte, int>(sourceSpan.Slice(0, n * 4));
            var destIntegers = MemoryMarshal.Cast<int, int>(destSpan.Slice(0, n));

            sourceBytes.CopyTo(destIntegers);
        }

        public static void Fill8_bits(int n, int bits, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, int baseValue)
        {
            // Set the initial value in the destination array based on the baseValue
            destSpan[0] = baseValue;

            int bitsTotal = 0;
            int sourceIndex = 0;
            byte srcByte = sourceSpan[sourceIndex];
            sourceIndex++;

            // Extract the necessary bits from the source and accumulate them in destShort
            for (int i = 1; i < n; i++)
            {
                int destInt = 0;
                for (int j = 0; j < bits; j++)
                {
                    // Extract the least significant bit from 'srcByte' and shift it to the appropriate position in 'destShort'
                    destInt |= (srcByte & 1) << j;

                    // Shift the bits of 'srcByte' to the right by 1 position to discard the consumed bit
                    srcByte >>= 1;

                    // Increment the counter to keep track of the number of bits consumed
                    bitsTotal++;

                    // If we have consumed all the bits in the current byte (8 bits),
                    // fetch the next byte from the 'sourceArray' and reset the counter
                    if (bitsTotal == 8)
                    {
                        bitsTotal = 0;
                        srcByte = sourceSpan[sourceIndex];
                        sourceIndex++;
                    }
                }

                // checks if the most significant bit of destInt is set, 
                // and if it is, set all the bits above the bits position to 1. 
                // this ensures that the resulting 8-bit Int32 preserves the correct negative value.
                destInt = ChangeIntSign(destInt, bits);

                destInt += destSpan[i - 1];
                destSpan[i] = destInt;
            }
        }

        public static void Fill16_bits(int n, int bits, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, int baseValue)
        {
            // Set the initial value of the first element in the destArray based on the baseValue
            destSpan[0] = baseValue;

            int bitsLeft = 8;
            int sourceIndex = 0;
            byte srcByte = sourceSpan[sourceIndex];
            sourceIndex++;

            // Loop through each element of the destArray, starting from the second element (i = 1)
            for (int i = 1; i < n; i++)
            {
                int destInt = 0;
                int bitsNeeded = bits;

                // Extract the necessary bits from the source and accumulate them in destInt
                while (bitsNeeded > 0)
                {
                    if (bitsNeeded >= bitsLeft)
                    {
                        // If we need more bits than what's left in the current byte,
                        // we take the remaining bits from the current byte and combine them
                        // with the next byte to fulfill the bit requirement.
                        destInt |= (srcByte & (0xFF >> (8 - bitsLeft))) << (bits - bitsNeeded);
                        srcByte = sourceSpan[sourceIndex]; // Get the next byte from sourceArray
                        sourceIndex++; // Move to the next byte in sourceArray
                        bitsNeeded -= bitsLeft; // Reduce the remaining bit requirement by the number of bits taken
                        bitsLeft = 8; // Reset the number of bits left in the current byte to 8
                    }
                    else
                    {
                        // If we have enough bits left in the current byte to fulfill the remaining bit requirement,
                        // we take the required number of bits and shift them to their final position in the destination integer.
                        destInt |= (srcByte & (0xFF >> (8 - bitsNeeded))) << (bits - bitsNeeded);
                        srcByte >>= bitsNeeded; // Shift the remaining bits in the current byte to the right
                        bitsLeft -= bitsNeeded; // Reduce the number of bits left in the current byte by the number of bits taken
                        bitsNeeded = 0; // No more bits needed, the requirement is fulfilled
                    }
                }

                // checks if the most significant bit of destInt is set, 
                // and if it is, set all the bits above the bits position to 1. 
                // this ensures that the resulting 16-bit Int32 preserves the correct negative value.
                destInt = ChangeIntSign(destInt, bits);

                destInt += destSpan[i - 1];
                destSpan[i] = destInt;
            }
        }

        public static void Fill24_bits(int n, int bits, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, int baseValue)
        {
            // Set the initial value in the destination array based on the baseValue
            int intValue = baseValue;
            destSpan[0] = intValue & 0xFF;
            destSpan[1] = (intValue >> 8) & 0xFF;
            destSpan[2] = (intValue >> 16) & 0xFF;

            int bitsLeft = 8;
            int sourceIndex = 0;
            byte srcByte = sourceSpan[sourceIndex];
            sourceIndex++;

            for (int i = 1; i < n; i++)
            {
                int destInt = 0;
                int bitsNeeded = bits;

                // Extract the necessary bits from the source and accumulate them in destInt
                while (bitsNeeded > 0)
                {
                    if (bitsNeeded >= bitsLeft)
                    {
                        // If we need more bits than what's left in the current byte,
                        // we take the remaining bits from the current byte and combine them
                        // with the next byte to fulfill the bit requirement.
                        destInt |= (srcByte & (0xFF >> (8 - bitsLeft))) << (bits - bitsNeeded);
                        srcByte = sourceSpan[sourceIndex]; // Get the next byte from sourceArray
                        sourceIndex++; // Move to the next byte in sourceArray
                        bitsNeeded -= bitsLeft; // Reduce the remaining bit requirement by the number of bits taken
                        bitsLeft = 8; // Reset the number of bits left in the current byte to 8
                    }
                    else
                    {
                        // If we have enough bits left in the current byte to fulfill the remaining bit requirement,
                        // we take the required number of bits and shift them to their final position in the destination integer.
                        destInt |= (srcByte & (0xFF >> (8 - bitsNeeded))) << (bits - bitsNeeded);
                        srcByte >>= bitsNeeded; // Shift the remaining bits in the current byte to the right
                        bitsLeft -= bitsNeeded; // Reduce the number of bits left in the current byte by the number of bits taken
                        bitsNeeded = 0; // No more bits needed, the requirement is fulfilled
                    }
                }

                // checks if the most significant bit of destInt is set, 
                // and if it is, set all the bits above the bits position to 1. 
                // this ensures that the resulting 24-bit Int32 preserves the correct negative value.
                destInt = ChangeIntSign(destInt, bits);

                intValue += destInt;
                destSpan[i * 3] = intValue & 0xFF;
                destSpan[i * 3 + 1] = (intValue >> 8) & 0xFF;
                destSpan[i * 3 + 2] = (intValue >> 16) & 0xFF;
            }
        }

        public static void Fill32_bits(int n, int bits, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan, int baseValue)
        {
            // Set the initial value in the destination array based on the baseValue
            destSpan[0] = baseValue;

            int bitsLeft = 8;
            int sourceIndex = 0;
            byte srcByte = sourceSpan[sourceIndex];
            sourceIndex++;

            for (int i = 1; i < n; i++)
            {
                int destInt = 0;
                int bitsNeeded = bits;

                // Extract the necessary bits from the source and accumulate them in destInt
                while (bitsNeeded > 0)
                {
                    if (bitsNeeded >= bitsLeft)
                    {
                        // If we need more bits than what's left in the current byte,
                        // we take the remaining bits from the current byte and combine them
                        // with the next byte to fulfill the bit requirement.
                        destInt |= (srcByte & (0xFF >> (8 - bitsLeft))) << (bits - bitsNeeded);
                        srcByte = sourceSpan[sourceIndex]; // Get the next byte from sourceArray
                        sourceIndex++; // Move to the next byte in sourceArray
                        bitsNeeded -= bitsLeft; // Reduce the remaining bit requirement by the number of bits taken
                        bitsLeft = 8; // Reset the number of bits left in the current byte to 8
                    }
                    else
                    {
                        // If we have enough bits left in the current byte to fulfill the remaining bit requirement,
                        // we take the required number of bits and shift them to their final position in the destination integer.
                        destInt |= (srcByte & (0xFF >> (8 - bitsNeeded))) << (bits - bitsNeeded);
                        srcByte >>= bitsNeeded; // Shift the remaining bits in the current byte to the right
                        bitsLeft -= bitsNeeded; // Reduce the number of bits left in the current byte by the number of bits taken
                        bitsNeeded = 0; // No more bits needed, the requirement is fulfilled
                    }
                }

                // checks if the most significant bit of destInt is set, 
                // and if it is, set all the bits above the bits position to 1. 
                // this ensures that the resulting 32-bit Int32 preserves the correct negative value.
                destInt = ChangeIntSign(destInt, bits);

                destInt += destSpan[i - 1];
                destSpan[i] = destInt;
            }
        }

        public static void FillIntegers(int n, int bits, ReadOnlySpan<byte> sourceSpan, int start, Span<int> destSpan, bool relative)
        {
            switch (bits)
            {
                case 8: FillIntegers8(n, sourceSpan, destSpan, false); break;
                case 16: FillIntegers16(n, sourceSpan, destSpan, false); break;
                case 24: FillIntegers24(n, sourceSpan, destSpan, false); break;
                case 32: FillIntegers32(n, sourceSpan, destSpan, false); break;
                default: FillIntegersL8(n, bits, sourceSpan, destSpan); break;
            }

            if (relative)
            {
                destSpan[0] = start + destSpan[0];
                for (int i = 1; i < n; i++)
                {
                    destSpan[i] = destSpan[i - 1] + destSpan[i];
                }
            }
        }

        public static void FillIntegersAbs(int n, int bits, ReadOnlySpan<byte> sourceSpan, int start, Span<int> destSpan)
        {
            switch (bits)
            {
                case 8: FillIntegers8(n, sourceSpan, destSpan, true); break;
                case 16: FillIntegers16(n, sourceSpan, destSpan, true); break;
                case 24: FillIntegers24(n, sourceSpan, destSpan, true); break;
                case 32: FillIntegers32(n, sourceSpan, destSpan, true); break;
            }
        }

        public static void FillBits(int n, int bits, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan)
        {
            switch (bits)
            {
                case 8: FillBits8(n, sourceSpan, destSpan, false); break;
                case 16: FillBits16(n, sourceSpan, destSpan, false); break;
                case 24: FillBits24(n, sourceSpan, destSpan, false); break;
                case 32: FillBits32(n, sourceSpan, destSpan, false); break;
                default: FillBitsL8(n, bits, sourceSpan, destSpan); break;
            }
        }

        public static void FillBitsAbs(int n, int bits, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan)
        {
            switch (bits)
            {
                case 8: FillBits8(n, sourceSpan, destSpan, true); break;
                case 16: FillBits16(n, sourceSpan, destSpan, true); break;
                case 24: FillBits24(n, sourceSpan, destSpan, true); break;
                case 32: FillBits32(n, sourceSpan, destSpan, true); break;
            }
        }

        public static void Fill8(int n, int bits, ReadOnlySpan<byte> sourceSpan, int baseValue, Span<int> destSpan, bool relative)
        {
            if (relative)
            {
                Fill8_bits(n, bits, sourceSpan, destSpan, baseValue);
            }
            else
            {
                Fill8abs(n, sourceSpan, destSpan);
            }
        }

        public static void Fill16(int n, int bits, ReadOnlySpan<byte> sourceSpan, int baseValue, Span<int> destSpan, bool relative)
        {
            if (relative)
            {
                switch (bits)
                {
                    // case 8: Fill16_8rel(n, sourceSpan, destSpan, baseValue); break;
                    default: Fill16_bits(n, bits, sourceSpan, destSpan, baseValue); break;
                }
            }
            else
            {
                Fill16abs(n, sourceSpan, destSpan);
            }
        }

        public static void Fill24(int n, int bits, ReadOnlySpan<byte> sourceSpan, int baseValue, Span<int> destSpan, bool relative)
        {
            if (relative)
            {
                switch (bits)
                {
                    // case 8: Fill24_8rel(n, sourceSpan, destSpan, baseValue); break;
                    // case 16: Fill24_16rel(n, sourceSpan, destSpan, baseValue); break;
                    default: Fill24_bits(n, bits, sourceSpan, destSpan, baseValue); break;
                }
            }
            else
            {
                Fill24abs(n, sourceSpan, destSpan);
            }
        }

        public static void Fill32(int n, int bits, ReadOnlySpan<byte> sourceSpan, int baseValue, Span<int> destSpan, bool relative)
        {
            if (relative)
            {
                switch (bits)
                {
                    // case 8: Fill32_8rel(n, sourceSpan, destSpan, baseValue); break;
                    // case 16: Fill32_16rel(n, sourceSpan, destSpan, baseValue); break;
                    // case 24: Fill32_24rel(n, sourceSpan, destSpan, baseValue); break;
                    default: Fill32_bits(n, bits, sourceSpan, destSpan, baseValue); break;
                }
            }
            else
            {
                Fill32abs(n, sourceSpan, destSpan);
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