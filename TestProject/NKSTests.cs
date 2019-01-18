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
            var fromBytes = new byte[16];
            fromBytes[15] = 0xff;
            fromBytes[14] = 0xff;
            fromBytes[13] = 0xff;
            fromBytes[12] = 0x44;

            NKS.IncrementCounter(fromBytes);

            var toBytes = new byte[16];
            toBytes[15] = 0x00;
            toBytes[14] = 0x00;
            toBytes[13] = 0x00;
            toBytes[12] = 0x45;

            Assert.Equal(fromBytes, toBytes, new JaggedByteComparer(0.001));

        }
    }
}