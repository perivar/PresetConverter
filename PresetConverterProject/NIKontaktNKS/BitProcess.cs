using System.Runtime.InteropServices;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class BitProcess
    {
        // Signed 24-bit value (-8388608 to 8388607) = (-0x800000 to 0x7FFFFF) 
        // References:
        // https://github.com/Gota7/GotaSoundIO/blob/master/Int24.cs
        // but also
        // https://github.com/rubendal/BitStream/blob/master/Int24.cs
        // https://github.com/GridProtectionAlliance/gsf/blob/master/Source/Libraries/GSF.Core.Shared/Int24.cs
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Int24
        {
            private byte b0 = 0x00;
            private byte b1 = 0x00;
            private byte b2 = 0x00;

            /// <summary>
            /// Max value.
            /// </summary>
            public const int MaxValue = 8388607; // 0x7FFFFF

            /// <summary>
            /// Min value.
            /// </summary>
            public const int MinValue = -8388608; // -0x800000

            // <summary>
            /// Get this as an int.
            /// </summary>
            /// <returns>This as an int.</returns>
            private readonly int GetInt()
            {
                int ret = 0;

                // Combine the bytes of the Int24 struct to form the 32-bit integer
                ret |= b0;                    // Lower 8 bits
                ret |= b1 << 8;               // Middle 8 bits
                ret |= (b2 & 0x7F) << 16;     // Upper 7 bits (excluding the sign bit)

                // Check if the sign bit is set and adjust the value accordingly
                if ((b2 & 0x80) > 0) { ret = MinValue + ret; }

                return ret;
            }

            /// <summary>
            /// Convert an int to an Int24.
            /// </summary>
            /// <param name="val">Value to convert.</param>
            /// <returns>Value as an Int24.</returns>
            private static Int24 FromInt(int val)
            {
                Int24 ret = new();

                // Clamp the value to the valid range of Int24
                if (val > MaxValue) { val = MaxValue; }
                if (val < MinValue) { val = MinValue; }

                uint un = (uint)val; // Treat the value as an unsigned integer

                // If the value is negative, convert it to its two's complement representation
                if (val < 0) { un = (uint)(val - MinValue); }

                // Store the 24-bit value in three bytes
                ret.b0 = (byte)(un & 0xFF);           // Lower 8 bits
                ret.b1 = (byte)((un >> 8) & 0xFF);    // Middle 8 bits
                ret.b2 = (byte)((un >> 16) & 0x7F);   // Upper 7 bits

                // Set the sign bit if the original value was negative
                if (val < 0) { ret.b2 |= 0x80; }

                return ret;
            }

            /// <summary>
            /// Create an Int24 using three bytes
            /// </summary>
            /// <param name="byte0">byte 0</param>
            /// <param name="byte1">byte 1</param>
            /// <param name="byte2">byte 2</param>
            public Int24(byte byte0, byte byte1, byte byte2)
            {
                b0 = byte0;
                b1 = byte1;
                b2 = byte2;
            }

            /// <summary>
            /// Create and Int24 using int
            /// Note you can also create this using a cast
            /// Int24 v = (Int24) val;
            /// </summary>
            /// <param name="val">int value</param>
            public Int24(int val)
            {
                Int24 dest = (Int24)val;

                b0 = dest.b0;
                b1 = dest.b1;
                b2 = dest.b2;
            }

            /// <summary>
            /// Zero
            /// </summary>
            /// <returns>Zero version</returns>
            public static Int24 Zero => new(0, 0, 0);

            /// <summary>
            /// Get this as an int.
            /// </summary>
            /// <param name="val">Value.</param>
            public static implicit operator int(Int24 val) => val.GetInt();

            /// <summary>
            /// Convert from an int.
            /// </summary>
            /// <param name="val">Value.</param>
            public static explicit operator Int24(int val) => FromInt(val);

            /// <summary>
            /// Convert from an uint.
            /// </summary>
            /// <param name="val">Value.</param>
            public static explicit operator Int24(uint val) => FromInt((int)val);

            /// <summary>
            /// Convert from a float.
            /// </summary>
            /// <param name="val">Value.</param>
            public static explicit operator Int24(float val) => FromInt((int)val);

            // + and - operators
            public static Int24 operator +(Int24 a, Int24 b) => FromInt((int)a + (int)b);
            public static Int24 operator -(Int24 a, Int24 b) => FromInt((int)a - (int)b);

            /// <summary>
            /// Get String representation of this Int24 value
            /// </summary>
            /// <returns>string representation</returns>
            public override string ToString()
            {
                int res = GetInt();
                return string.Format("{0}: 0x{1:X2},0x{2:X2},0x{3:X2}", res, b0, b1, b2);
            }

            public static implicit operator string(Int24 value)
            {
                return value.ToString();
            }
        }

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

        public static void Encode8_8(int n, ReadOnlySpan<sbyte> sourceSpan, Span<byte> destSpan)
        {
            for (int i = 0; i < n; i++)
            {
                int value = sourceSpan[i];

                // truncate the int value to fit within the range of a byte
                // signed 8-bit value  (-128 to +127)               = (-0x80 to 0x7F)
                // unsigned 8-bit value (0 to 255)                  = (0x00 to 0xFF)
                destSpan[i] = (byte)(value & 0xFF);
            }
        }

        public static void Encode8_16(int n, ReadOnlySpan<short> sourceSpan, Span<byte> destSpan)
        {
            for (int i = 0; i < n; i++)
            {
                int value = sourceSpan[i];

                // truncate the int value to fit within the range of two bytes, i.e. a short
                // signed 16-bit value (-32768 to 32767)            = (-0x8000 to 0x7FFF)
                destSpan[i * 2] = (byte)(value & 0xFF);
                destSpan[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
            }
        }

        public static void Encode8_24(int n, ReadOnlySpan<Int24> sourceSpan, Span<byte> destSpan)
        {
            for (int i = 0; i < n; i++)
            {
                Int24 value = sourceSpan[i];

                // truncate the int value to fit within the range of three bytes
                // signed 24-bit value (-8388608 to 8388607)        = (-0x800000 to 0x7FFFFF) 
                destSpan[i * 3] = (byte)(value & 0xFF);
                destSpan[i * 3 + 1] = (byte)((value >> 8) & 0xFF);
                destSpan[i * 3 + 2] = (byte)((value >> 16) & 0xFF);
            }
        }

        public static void Encode8_32(int n, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan)
        {
            for (int i = 0; i < n; i++)
            {
                int value = sourceSpan[i];

                // truncate the int value to fit within the range of four bytes
                // signed 32-bit value (-2147483648 to +2147483647) = (-0x80000000 to +0x7FFFFFFF)
                destSpan[i * 4] = (byte)(value & 0xFF);
                destSpan[i * 4 + 1] = (byte)((value >> 8) & 0xFF);
                destSpan[i * 4 + 2] = (byte)((value >> 16) & 0xFF);
                destSpan[i * 4 + 3] = (byte)((value >> 32) & 0xFF);
            }
        }

        public static void Encode16_16(int n, ReadOnlySpan<short> sourceSpan, Span<byte> destSpan)
        {
            for (int i = 0; i < n; i++)
            {
                int value = sourceSpan[i];

                // truncate the int value to fit within the range of two bytes, i.e. a short
                // signed 16-bit value (-32768 to 32767)            = (-0x8000 to 0x7FFF)
                destSpan[i * 2] = (byte)(value & 0xFF);
                destSpan[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
            }
        }

        public static void Encode16_24(int n, ReadOnlySpan<Int24> sourceSpan, Span<byte> destSpan)
        {
            for (int i = 0; i < n; i++)
            {
                Int24 value = sourceSpan[i];

                // truncate the int value to fit within the range of three bytes
                // signed 24-bit value (-8388608 to 8388607)        = (-0x800000 to 0x7FFFFF) 
                destSpan[i * 3] = (byte)(value & 0xFF);
                destSpan[i * 3 + 1] = (byte)((value >> 8) & 0xFF);
                destSpan[i * 3 + 2] = (byte)((value >> 16) & 0xFF);
            }
        }

        public static void Encode16_32(int n, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan)
        {
            for (int i = 0; i < n; i++)
            {
                int value = sourceSpan[i];

                // truncate the int value to fit within the range of four bytes
                // signed 32-bit value (-2147483648 to +2147483647) = (-0x80000000 to +0x7FFFFFFF)
                destSpan[i * 4] = (byte)(value & 0xFF);
                destSpan[i * 4 + 1] = (byte)((value >> 8) & 0xFF);
                destSpan[i * 4 + 2] = (byte)((value >> 16) & 0xFF);
                destSpan[i * 4 + 3] = (byte)((value >> 32) & 0xFF);
            }
        }

        public static void Encode24_24(int n, ReadOnlySpan<Int24> sourceSpan, Span<byte> destSpan)
        {
            // TODO: PIN - is this needed?
            // var destShort = MemoryMarshal.Cast<byte, short>(destSpan);
            // var destBytes = MemoryMarshal.Cast<short, byte>(destShort);
            // destBytes.CopyTo(destSpan);

            for (int i = 0; i < n; i++)
            {
                Int24 value = sourceSpan[i];

                // truncate the int value to fit within the range of three bytes
                // signed 24-bit value (-8388608 to 8388607)        = (-0x800000 to 0x7FFFFF) 
                destSpan[i * 3] = (byte)(value & 0xFF);
                destSpan[i * 3 + 1] = (byte)((value >> 8) & 0xFF);
                destSpan[i * 3 + 2] = (byte)((value >> 16) & 0xFF);
            }
        }

        public static void Encode24_32(int n, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan)
        {
            for (int i = 0; i < n; i++)
            {
                int value = sourceSpan[i];

                // copy the int into four bytes
                // signed 32-bit value (-2147483648 to +2147483647) = (-0x80000000 to +0x7FFFFFFF)
                destSpan[i * 4] = (byte)(value & 0xFF);
                destSpan[i * 4 + 1] = (byte)((value >> 8) & 0xFF);
                destSpan[i * 4 + 2] = (byte)((value >> 16) & 0xFF);
                destSpan[i * 4 + 3] = (byte)((value >> 24) & 0xFF);
            }
        }

        public static void Encode32_32(int n, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan)
        {
            for (int i = 0; i < n; i++)
            {
                int value = sourceSpan[i];

                // copy the int into four bytes
                // signed 32-bit value (-2147483648 to +2147483647) = (-0x80000000 to +0x7FFFFFFF)
                destSpan[i * 4] = (byte)(value & 0xFF);
                destSpan[i * 4 + 1] = (byte)((value >> 8) & 0xFF);
                destSpan[i * 4 + 2] = (byte)((value >> 16) & 0xFF);
                destSpan[i * 4 + 3] = (byte)((value >> 24) & 0xFF);
            }
        }

        public static void EncodeL_8(int n, int bits, ReadOnlySpan<sbyte> sourceSpan, Span<byte> destSpan)
        {
            int destIndex = 0;
            byte destByte = 0;
            int bitsLeft = 8;

            for (int i = 0; i < n; i++)
            {
                int src8 = sourceSpan[i];
                int bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte curByte = (byte)((src8 & ((0xFF >> (8 - (bits - bitsWritten))) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(curByte << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits;
                    }
                    else
                    {
                        byte curByte = (byte)((src8 & ((0xFF >> (8 - bitsLeft)) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(curByte << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    if (bitsLeft == 0)
                    {
                        destSpan[destIndex] = destByte;
                        destByte = 0;
                        bitsLeft = 8;
                        destIndex++;
                    }
                }
            }

            if (bitsLeft < 8)
            {
                destSpan[destIndex] = destByte;
            }
        }

        public static void EncodeL_16(int n, int bits, ReadOnlySpan<short> sourceSpan, Span<byte> destSpan)
        {
            int destIndex = 0;
            byte destByte = 0;
            int bitsLeft = 8;

            for (int i = 0; i < n; i++)
            {
                short src16 = sourceSpan[i];
                int bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte curByte = (byte)((src16 & ((0xFF >> (8 - (bits - bitsWritten))) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(curByte << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits;
                    }
                    else
                    {
                        byte curByte = (byte)((src16 & ((0xFF >> (8 - bitsLeft)) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(curByte << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    if (bitsLeft == 0)
                    {
                        destSpan[destIndex] = destByte;
                        destByte = 0;
                        bitsLeft = 8;
                        destIndex++;
                    }
                }
            }

            if (bitsLeft < 8)
            {
                destSpan[destIndex] = destByte;
            }
        }

        public static void EncodeL_24(int n, int bits, ReadOnlySpan<Int24> sourceSpan, Span<byte> destSpan)
        {
            int destIndex = 0;          // Index of the current position in the destination span
            byte destByte = 0;          // Accumulator for the bits to be written to the destination
            int bitsLeft = 8;           // Number of bits remaining in the current destination byte

            for (int i = 0; i < n; i++)
            {
                int srcInt24 = sourceSpan[i];   // Get the current Int24 value from the source span
                int bitsWritten = 0;            // Number of bits written for the current Int24 value

                while (bitsWritten < bits)
                {
                    // If the remaining bits to be written fit in the current destination byte
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        // Extract the bits from the Int24 value and update the destination byte
                        byte curByte = (byte)(srcInt24 & (0xFF >> (8 - (bits - bitsWritten))));
                        srcInt24 >>= bits - bitsWritten;
                        destByte |= (byte)(curByte << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits; // All bits have been written for the current value
                    }
                    else
                    {
                        // Extract the bits from the Int24 value and update the destination byte
                        byte curByte = (byte)(srcInt24 & (0xFF >> (8 - bitsLeft)));
                        srcInt24 >>= bitsLeft;
                        destByte |= (byte)(curByte << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    // If the current destination byte is full, write it to the destination span
                    if (bitsLeft == 0)
                    {
                        destSpan[destIndex] = destByte;
                        destIndex++;
                        destByte = 0;       // Reset the accumulator
                        bitsLeft = 8;       // Reset the number of bits remaining in the destination byte
                    }
                }
            }

            // If there are remaining bits in the last destination byte, write it to the destination span
            if (bitsLeft < 8)
            {
                destSpan[destIndex] = destByte;
            }
        }

        public static void EncodeL_32(int n, int bits, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan)
        {
            int destIndex = 0;
            byte destByte = 0;
            int bitsLeft = 8;

            for (int i = 0; i < n; i++)
            {
                int src32 = sourceSpan[i];
                int bitsWritten = 0;

                while (bitsWritten < bits)
                {
                    if ((bits - bitsWritten) <= bitsLeft)
                    {
                        byte curByte = (byte)((src32 & ((0xFF >> (8 - (bits - bitsWritten))) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(curByte << (8 - bitsLeft));
                        bitsLeft -= bits - bitsWritten;
                        bitsWritten = bits;
                    }
                    else
                    {
                        byte curByte = (byte)((src32 & ((0xFF >> (8 - bitsLeft)) << bitsWritten)) >> bitsWritten);
                        destByte |= (byte)(curByte << (8 - bitsLeft));
                        bitsWritten += bitsLeft;
                        bitsLeft = 0;
                    }

                    if (bitsLeft == 0)
                    {
                        destSpan[destIndex] = destByte;
                        destByte = 0;
                        bitsLeft = 8;
                        destIndex++;
                    }
                }
            }

            if (bitsLeft < 8)
            {
                destSpan[destIndex] = destByte;
            }
        }

        public static void Fill16_8rel(int n, ReadOnlySpan<byte> sourceSpan, Span<short> destSpan, int baseValue)
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

        public static void Fill24_8rel(int n, ReadOnlySpan<byte> sourceSpan, Span<Int24> destSpan, int baseValue)
        {
            destSpan[0] = (Int24)baseValue;

            int sourceIndex = 0;

            for (int i = 1; i < n; i++)
            {
                byte srcByte = sourceSpan[sourceIndex];
                sourceIndex++;

                destSpan[i] = (Int24)(srcByte + destSpan[i - 1]);
            }
        }

        public static void Fill24_16rel(int n, ReadOnlySpan<byte> sourceSpan, Span<Int24> destSpan, int baseValue)
        {
            // convert byte span into Int24 span
            ReadOnlySpan<Int24> sourceArray = MemoryMarshal.Cast<byte, Int24>(sourceSpan);

            destSpan[0] = (Int24)baseValue;

            int sourceIndex = 0;

            for (int i = 1; i < n; i++)
            {
                Int24 srcInt24 = sourceArray[sourceIndex];
                sourceIndex++;

                srcInt24 = (Int24)ChangeIntSign(srcInt24, 16);

                destSpan[i] = (Int24)(srcInt24 + destSpan[i - 1]);
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

        public static void Fill8abs(int n, ReadOnlySpan<byte> sourceSpan, Span<sbyte> destSpan)
        {
            var sourceBytes = MemoryMarshal.Cast<byte, int>(sourceSpan.Slice(0, n));
            var destIntegers = MemoryMarshal.Cast<sbyte, int>(destSpan.Slice(0, n));

            sourceBytes.CopyTo(destIntegers);
        }

        public static void Fill16abs(int n, ReadOnlySpan<byte> sourceSpan, Span<short> destSpan)
        {
            var sourceBytes = MemoryMarshal.Cast<byte, int>(sourceSpan.Slice(0, n * 2));
            var destIntegers = MemoryMarshal.Cast<short, int>(destSpan.Slice(0, n));

            sourceBytes.CopyTo(destIntegers);
        }

        public static void Fill24abs(int n, ReadOnlySpan<byte> sourceSpan, Span<Int24> destSpan)
        {
            var sourceBytes = MemoryMarshal.Cast<byte, int>(sourceSpan.Slice(0, n * 3));
            var destIntegers = MemoryMarshal.Cast<Int24, int>(destSpan.Slice(0, n));

            sourceBytes.CopyTo(destIntegers);
        }

        public static void Fill32abs(int n, ReadOnlySpan<byte> sourceSpan, Span<int> destSpan)
        {
            var sourceBytes = MemoryMarshal.Cast<byte, int>(sourceSpan.Slice(0, n * 4));
            var destIntegers = MemoryMarshal.Cast<int, int>(destSpan.Slice(0, n));

            sourceBytes.CopyTo(destIntegers);
        }

        public static void Fill8_bits(int n, int bits, ReadOnlySpan<byte> sourceSpan, Span<sbyte> destSpan, int baseValue)
        {
            // Set the initial value in the destination array based on the baseValue
            destSpan[0] = (sbyte)baseValue;

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
                destSpan[i] = (sbyte)destInt;
            }
        }

        public static void Fill16_bits(int n, int bits, ReadOnlySpan<byte> sourceSpan, Span<short> destSpan, int baseValue)
        {
            // Set the initial value of the first element in the destArray based on the baseValue
            destSpan[0] = (short)baseValue;

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
                destSpan[i] = (short)destInt;
            }
        }

        public static void Fill24_bits(int n, int bits, ReadOnlySpan<byte> sourceSpan, Span<Int24> destSpan, int baseValue)
        {
            // Set the initial value of the first element in the destArray based on the baseValue
            destSpan[0] = (Int24)baseValue;

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

                destInt += destSpan[i - 1];
                destSpan[i] = (Int24)destInt;
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

        public static void Fill8(int n, int bits, ReadOnlySpan<byte> sourceSpan, int baseValue, Span<sbyte> destSpan, bool relative)
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

        public static void Fill16(int n, int bits, ReadOnlySpan<byte> sourceSpan, int baseValue, Span<short> destSpan, bool relative)
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

        public static void Fill24(int n, int bits, ReadOnlySpan<byte> sourceSpan, int baseValue, Span<Int24> destSpan, bool relative)
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

        public static void Encode_8(int n, int bits, ReadOnlySpan<sbyte> sourceSpan, Span<byte> destSpan)
        {
            switch (bits)
            {
                // case 8: Encode8_8(n, sourceSpan, destSpan); break;
                default: EncodeL_8(n, bits, sourceSpan, destSpan); break;
            }
        }

        public static void Encode_16(int n, int bits, ReadOnlySpan<short> sourceSpan, Span<byte> destSpan)
        {
            switch (bits)
            {
                // case 8: Encode8_16(n, sourceSpan, destSpan); break;
                // case 16: Encode16_16(n, sourceSpan, destSpan); break;
                default: EncodeL_16(n, bits, sourceSpan, destSpan); break;
            }
        }

        public static void Encode_24(int n, int bits, ReadOnlySpan<Int24> sourceSpan, Span<byte> destSpan)
        {
            switch (bits)
            {
                // case 8: Encode8_24(n, sourceSpan, destSpan); break;
                // case 16: Encode16_24(n, sourceSpan, destSpan); break;
                // case 24: Encode24_24(n, sourceSpan, destSpan); break;
                default: EncodeL_24(n, bits, sourceSpan, destSpan); break;
            }
        }

        public static void Encode_32(int n, int bits, ReadOnlySpan<int> sourceSpan, Span<byte> destSpan)
        {
            switch (bits)
            {
                // case 8: Encode8_32(n, sourceSpan, destSpan); break;
                // case 16: Encode16_32(n, sourceSpan, destSpan); break;
                // case 24: Encode24_32(n, sourceSpan, destSpan); break;
                // case 32: Encode32_32(n, sourceSpan, destSpan); break;
                default: EncodeL_32(n, bits, sourceSpan, destSpan); break;
            }
        }
    }
}