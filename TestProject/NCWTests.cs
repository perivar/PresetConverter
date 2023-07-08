using CommonUtils.Audio;
using PresetConverterProject.NIKontaktNKS;
using Xunit;
using Xunit.Abstractions;

namespace TestProject
{
    public class NCWTests
    {
        private readonly ITestOutputHelper output;

        public NCWTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\viola_sus_short-portato_64-127_E4 - AB-comp Samples\\viola_sus_short-portato_64-127_E4 - AB.ncw")]
        public void ReadNCW(string inputFilePath)
        {
            var outputDirectoryPath = "C:\\Users\\periv\\Projects\\Temp";

            bool doVerbose = true;
            var ncwParser = new NCWParser(doVerbose);
            ncwParser.Clear();
            ncwParser.OpenNCWFile(inputFilePath);
            ncwParser.ReadNCW();

            string outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".wav";
            string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName);
            output.WriteLine("Writing WAV file as {1} ...", outputFilePath);
            ncwParser.SaveToWAV(outputFilePath);

            // test using integers
            ncwParser.ReadNCWIntegers();
            string outputFileNameInt = Path.GetFileNameWithoutExtension(inputFilePath) + "_ints.wav";
            string outputFilePathInt = Path.Combine(outputDirectoryPath, outputFileNameInt);
            output.WriteLine("Writing file {0} ...", outputFilePathInt);
            ncwParser.SaveToWAVIntegers(outputFilePathInt);

            // Assert.Equal(fromBytes, toBytes, new JaggedByteComparer(0.001));
        }

        [Theory]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\viola_sus_short-portato_64-127_E4 - AB-comp Samples\\viola_sus_short-portato_64-127_E4 - AB.ncw")]
        public void WriteNCW(string inputFilePath)
        {
            var outputDirectoryPath = "C:\\Users\\periv\\Projects\\Temp";

            bool doVerbose = true;
            var ncwParser = new NCWParser(doVerbose);
            ncwParser.Clear();
            ncwParser.OpenNCWFile(inputFilePath);

            ncwParser.ReadNCW();
            ncwParser.ReadNCWIntegers();

            // test writing NCW
            WAVParser.TMyWAVHeader wavHeader = new()
            {
                wFormatTag = SoundIO.WAVE_FORMAT_PCM, // Standard wav
                nChannels = ncwParser.Header.Channels,
                nSamplesPerSec = ncwParser.Header.SampleRate,
                wBitsPerSample = ncwParser.Header.Bits,
                numOfPoints = (int)ncwParser.Header.NumSamples
            };

            string outputFileNameNCW24 = Path.GetFileNameWithoutExtension(inputFilePath) + "_24.ncw";
            string outputFilePathNCW24 = Path.Combine(outputDirectoryPath, outputFileNameNCW24);
            output.WriteLine("Writing file {0} ...", outputFilePathNCW24);
            ncwParser.WriteNCW24(wavHeader);
            ncwParser.SaveToNCW(outputFilePathNCW24);

            string outputFileNameNCW32 = Path.GetFileNameWithoutExtension(inputFilePath) + "_32.ncw";
            string outputFilePathNCW32 = Path.Combine(outputDirectoryPath, outputFileNameNCW32);
            output.WriteLine("Writing file {0} ...", outputFilePathNCW32);
            ncwParser.WriteNCW32(wavHeader);
            ncwParser.SaveToNCW(outputFilePathNCW32);
        }

        [Theory]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB.wav")]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - u8bit.wav")]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 16bit.wav")]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 32bit.wav")]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 24bit.wav")]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 32bit float.wav")]
        public void ReadWAV(string inputFilePath)
        {
            // read wave file
            var wp = new WAVParser();
            wp.OpenWav(inputFilePath);

            // ints
            int[] ints = new int[wp.WavHeader.numOfPoints * wp.WavHeader.nChannels];
            wp.ReadToIntegers(ref ints);
            var wavOutPathInts = "C:\\Users\\periv\\Projects\\Temp\\viola_sus_short-portato_64-127_E4 - AB - ints.wav";
            wp.SaveWAVFromIntegers(wavOutPathInts, ref ints);

            // floats
            float[] floats = new float[wp.WavHeader.numOfPoints * wp.WavHeader.nChannels];
            wp.ReadToFloats(ref floats, (uint)wp.WavHeader.numOfPoints);
            var wavOutPathFloats = "C:\\Users\\periv\\Projects\\Temp\\viola_sus_short-portato_64-127_E4 - AB - floats.wav";
            wp.SaveStandardWAVMulti(wavOutPathFloats, ref floats);
        }

    }
}