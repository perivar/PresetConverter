using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CommonUtils
{
    public class JaggedByteComparer : IEqualityComparer<byte[]>
    {
        private readonly double threshold;

        public JaggedByteComparer(double threshold)
        {
            this.threshold = threshold;
        }

        public bool Equals(byte[] x, byte[] y)
        {
            return (x.SequenceEqual<byte>(y, new ByteComparer(threshold)));
        }

        public int GetHashCode(byte[] obj)
        {
            return obj.GetHashCode();
        }
    }

    public class ByteComparer : IEqualityComparer<byte>
    {
        private readonly double threshold;

        public ByteComparer(double threshold)
        {
            this.threshold = threshold;
        }

        public bool Equals(byte x, byte y)
        {
            return Math.Abs(x - y) < this.threshold;
        }

        public int GetHashCode(byte obj)
        {
            return obj.GetHashCode();
        }
    }
}