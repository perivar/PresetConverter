using System.Runtime.InteropServices;

namespace CommonUtils.Audio
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

        public readonly byte[] GetBytes()
        {
            return new byte[] { b0, b1, b2 };
        }

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

            // Check if the most significant bit (bit 23) is set to determine if the value is negative.
            if ((b2 & 0x80) > 0)
            {
                // If the value is negative, sign-extend it to a 32-bit signed integer
                ret = MinValue + ret;
            }

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
            Int24 dest = FromInt(val);

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
}
