using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using CommonUtils;
using PresetConverter;
using PresetConverterProject.NIKontaktNKS;
using Xunit;
using Xunit.Abstractions;

namespace PresetConverterTests
{
    public class NKSTests
    {
        private readonly ITestOutputHelper output;

        public NKSTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test1()
        {
            var ctr = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                Random rnd = new Random();
                ctr[i] = (byte)rnd.Next(0, 0xff);
                output.WriteLine("{0}", ctr[i]);
            }
            ctr[15] = 0xff;
            ctr[14] = 0xff;
            ctr[13] = 0xff;

            NKS.IncrementCounter(ctr);

        }
    }
}