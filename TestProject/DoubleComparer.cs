using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PresetConverterTests
{
    public class JaggedDoubleComparer : IEqualityComparer<double[]>
    {
        private readonly double threshold;

        public JaggedDoubleComparer(double threshold)
        {
            this.threshold = threshold;
        }

        public bool Equals(double[] x, double[] y)
        {
            return (x.SequenceEqual<double>(y, new DoubleComparer(threshold)));
        }

        public int GetHashCode(double[] obj)
        {
            return obj.GetHashCode();
        }
    }

    public class DoubleComparer : IEqualityComparer<double>
    {
        private readonly double threshold;

        public DoubleComparer(double threshold)
        {
            this.threshold = threshold;
        }

        public bool Equals(double x, double y)
        {
            return Math.Abs(x - y) < this.threshold;
        }

        public int GetHashCode(double obj)
        {
            return obj.GetHashCode();
        }
    }
}