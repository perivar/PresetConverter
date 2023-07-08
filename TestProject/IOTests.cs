using CommonUtils;
using PresetConverterProject.NIKontaktNKS;
using Xunit;
using Xunit.Abstractions;

namespace TestProject
{
    public class IOTests
    {
        private readonly ITestOutputHelper output;

        public IOTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void TestFromUnixFileNames()
        {
            string windowsUnixName1 = NI.FromUnixFileNames(".PAResources|database|PAL|PAL.meta");
            Assert.Equal(".PAResources[pipe]database[pipe]PAL[pipe]PAL.meta", windowsUnixName1);

            string windowsUnixName2 = NI.FromUnixFileNames("[more][music][less][noise].aif");
            Assert.Equal("[more][music][[less][noise].aif", windowsUnixName2);

            string windowsUnixName3 = NI.FromUnixFileNames("[[pipe]]organ[:]A#.aiff");
            Assert.Equal("[[[[pipe]]organ[[[colon]]A#.aiff", windowsUnixName3);

            string windowsUnixName4 = NI.FromUnixFileNames("pipeorgan:A#.aiff");
            Assert.Equal("pipeorgan[colon]A#.aiff", windowsUnixName4);
        }

        [Fact]
        public void TestToUnixFileNames()
        {
            string unixName1 = NI.ToUnixFileName(".PAResources[pipe]database[pipe]PAL[pipe]PAL.meta");
            Assert.Equal(".PAResources|database|PAL|PAL.meta", unixName1);

            string unixName2 = NI.ToUnixFileName("[more][music][[less][noise].aif");
            Assert.Equal("[more][music][less][noise].aif", unixName2);

            string unixName3 = NI.ToUnixFileName("[[[[pipe]]organ[[[colon]]A#.aiff");
            Assert.Equal("[[pipe]]organ[:]A#.aiff", unixName3);

            string unixName4 = NI.ToUnixFileName("pipeorgan[colon]A#.aiff");
            Assert.Equal("pipeorgan:A#.aiff", unixName4);
        }

        [Fact]
        public void TestEscapeRepresentative()
        {
            string windowsUnixName1 = StringUtils.EscapeRepresentative(".PAResources|database|PAL|PAL.meta");
            Assert.Equal(".PAResources｜database｜PAL｜PAL.meta", windowsUnixName1);

            string windowsUnixName2 = StringUtils.EscapeRepresentative("[more][music][less][noise].aif");
            Assert.Equal("[more][music][less][noise].aif", windowsUnixName2);

            string windowsUnixName3 = StringUtils.EscapeRepresentative("[[[pipe]]organ[:]A#.aiff");
            Assert.Equal("[[[pipe]]organ[：]A#.aiff", windowsUnixName3);
        }

        [Fact]
        public void TestUnescapeRepresentative()
        {
            string unixName1 = StringUtils.UnescapeRepresentative(".PAResources｜database｜PAL｜PAL.meta");
            Assert.Equal(".PAResources|database|PAL|PAL.meta", unixName1);

            string unixName2 = StringUtils.UnescapeRepresentative("[more][music][less][noise].aif");
            Assert.Equal("[more][music][less][noise].aif", unixName2);

            string unixName3 = StringUtils.UnescapeRepresentative("[[[pipe]]organ[：]A#.aiff");
            Assert.Equal("[[[pipe]]organ[:]A#.aiff", unixName3);
        }

        [Fact]
        public void TestEscapeHex()
        {
            string windowsUnixName1 = StringUtils.EscapeHex(".PAResources|database|PAL|PAL.meta");
            Assert.Equal(".PAResources%007Cdatabase%007CPAL%007CPAL.meta", windowsUnixName1);

            string windowsUnixName2 = StringUtils.EscapeHex("[more][music][less][noise].aif");
            Assert.Equal("[more][music][less][noise].aif", windowsUnixName2);

            string windowsUnixName3 = StringUtils.EscapeHex("[[[pipe]]organ[:]A#.aiff");
            Assert.Equal("[[[pipe]]organ[%003A]A#.aiff", windowsUnixName3);
        }

        [Fact]
        public void TestUnescapeHex()
        {
            string unixName1 = StringUtils.UnescapeHex(".PAResources%007Cdatabase%007CPAL%007CPAL.meta");
            Assert.Equal(".PAResources|database|PAL|PAL.meta", unixName1);

            string unixName2 = StringUtils.UnescapeHex("[more][music][less][noise].aif");
            Assert.Equal("[more][music][less][noise].aif", unixName2);

            string unixName3 = StringUtils.UnescapeHex("[[[pipe]]organ[%003A]A#.aiff");
            Assert.Equal("[[[pipe]]organ[:]A#.aiff", unixName3);
        }
    }
}