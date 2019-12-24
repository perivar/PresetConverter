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
            string windowsUnixName1 = StringUtils.EscapeRepresentative(".PAResources|database|PAL|PAL.meta");
            // Assert.Equal(".PAResources[pipe]database[pipe]PAL[pipe]PAL.meta", windowsUnixName1);
            Assert.Equal(".PAResources｜database｜PAL｜PAL.meta", windowsUnixName1);

            string windowsUnixName2 = StringUtils.EscapeRepresentative("[more][music][less][noise].aif");
            // Assert.Equal("[more][music][[less][noise].aif", windowsUnixName2);
            Assert.Equal("[more][music][less][noise].aif", windowsUnixName2);

            string windowsUnixName3 = StringUtils.EscapeRepresentative("[[[pipe]]organ[:]A#.aiff");
            // Assert.Equal("[[[[[pipe]]organ[[[colon]]A#.aiff", windowsUnixName3);
            Assert.Equal("[[[pipe]]organ[：]A#.aiff", windowsUnixName3);
        }

        [Fact]
        public void TestToUnixFileNames()
        {
            // string unixName1 = NICNT.ToUnixFileName(".PAResources[pipe]database[pipe]PAL[pipe]PAL.meta");
            string unixName1 = StringUtils.UnescapeRepresentative(".PAResources｜database｜PAL｜PAL.meta");
            Assert.Equal(".PAResources|database|PAL|PAL.meta", unixName1);

            // string unixName2 = NICNT.ToUnixFileName("[more][music][[less][noise].aif");
            string unixName2 = StringUtils.UnescapeRepresentative("[more][music][less][noise].aif");
            Assert.Equal("[more][music][less][noise].aif", unixName2);

            // string unixName3 = NICNT.ToUnixFileName("[[[[[pipe]]organ[[[colon]]A#.aiff");
            string unixName3 = StringUtils.UnescapeRepresentative("[[[pipe]]organ[：]A#.aiff");
            Assert.Equal("[[[pipe]]organ[:]A#.aiff", unixName3);
        }
    }
}