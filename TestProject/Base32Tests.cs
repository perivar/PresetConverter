using Xunit;
using CommonUtils;
using PresetConverterProject.NIKontaktNKS;

namespace TestProject
{
    public class Base32Tests
    {
        const int SNPID_CONST = 4080;

        const int BERLIN_INSPIRE_1 = 36484;
        const string BERLIN_INSPIRE_1_SNPID = "P04";
        const int BERLIN_INSPIRE_2 = 37851;
        const string BERLIN_INSPIRE_2_SNPID = "Q23";
        const int METROPOLIS_ARK_3 = 36736;
        const string METROPOLIS_ARK_3_SNPID = "P74";
        const int RANDYS_PREPARED_PIANO = 45555;
        const string RANDYS_PREPARED_PIANO_SNPID = "W03";


        [Fact]
        public void TestSNPIDConversionFromBase36()
        {
            // Berlin Orchestra Inspire
            var berlinInspire1 = Base36Converter.Decode(BERLIN_INSPIRE_1_SNPID) + SNPID_CONST;
            Assert.Equal(BERLIN_INSPIRE_1, berlinInspire1);

            // Berlin Orchestra Inspire 2
            var berlinInspire2 = Base36Converter.Decode(BERLIN_INSPIRE_2_SNPID) + SNPID_CONST;
            Assert.Equal(BERLIN_INSPIRE_2, berlinInspire2);

            // Metropolis Ark 3
            var metropolisArk3 = Base36Converter.Decode(METROPOLIS_ARK_3_SNPID) + SNPID_CONST;
            Assert.Equal(METROPOLIS_ARK_3, metropolisArk3);

            // Artist Series - Randys Prepared Piano
            var randysPreparedPiano = Base36Converter.Decode(RANDYS_PREPARED_PIANO_SNPID) + SNPID_CONST;
            Assert.Equal(RANDYS_PREPARED_PIANO, randysPreparedPiano);

        }

        [Fact]
        public void TestSNPIDConversionToBase36()
        {
            // Berlin Orchestra Inspire
            var berlinInspire1 = Base36Converter.Encode(BERLIN_INSPIRE_1 - SNPID_CONST);
            Assert.Equal(BERLIN_INSPIRE_1_SNPID, berlinInspire1);

            // Berlin Orchestra Inspire 2
            var berlinInspire2 = Base36Converter.Encode(BERLIN_INSPIRE_2 - SNPID_CONST);
            Assert.Equal(BERLIN_INSPIRE_2_SNPID, berlinInspire2);

            // Metropolis Ark 3
            var metropolisArk3 = Base36Converter.Encode(METROPOLIS_ARK_3 - SNPID_CONST);
            Assert.Equal(METROPOLIS_ARK_3_SNPID, metropolisArk3);

            // Artist Series - Randys Prepared Piano
            var randysPreparedPiano = Base36Converter.Encode(RANDYS_PREPARED_PIANO - SNPID_CONST);
            Assert.Equal(RANDYS_PREPARED_PIANO_SNPID, randysPreparedPiano);
        }
    }
}