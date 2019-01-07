using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using CommonUtils;
using PresetConverter;
using PresetConverterProject.NIKontaktNKS;
using Xunit;

namespace PresetConverterTests
{
    public class NKSTests
    {
        [Fact]
        public void Test1()
        {
            var ctr = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                Random rnd = new Random();
                ctr[i] = (byte)rnd.Next(0, 0xff);
            }

            NKS.IncrementCounter(ctr);
        }
    }
}