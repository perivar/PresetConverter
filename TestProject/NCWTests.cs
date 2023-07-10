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
        [InlineData(
            @"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\viola_sus_short-portato_64-127_E4 - AB-comp Samples\\viola_sus_short-portato_64-127_E4 - AB.ncw",
            @"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB.wav")
        ]
        public void ReadNCW(string inputFilePath, string compareToFilePath)
        {
            var outputDirectoryPath = "C:\\Users\\periv\\Projects\\Temp";

            output.WriteLine("Reading NCW - {0}", inputFilePath);

            bool doVerbose = true;
            var ncwParser = new NCWParser(doVerbose);
            ncwParser.Clear();
            ncwParser.OpenNCWFile(inputFilePath);
            ncwParser.ReadNCW();

            string outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".wav";
            string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName);
            output.WriteLine("Writing WAV file as {0} ...", outputFilePath);
            ncwParser.SaveToWAV(outputFilePath);

            // test using integers
            ncwParser.ReadNCWIntegers();
            string outputFileNameInt = Path.GetFileNameWithoutExtension(inputFilePath) + "_ints.wav";
            string outputFilePathInt = Path.Combine(outputDirectoryPath, outputFileNameInt);
            output.WriteLine("Writing file {0} ...", outputFilePathInt);
            ncwParser.SaveToWAVIntegers(outputFilePathInt);

            // read as bytes
            var bytesOriginal = File.ReadAllBytes(compareToFilePath);
            var bytesNormal = File.ReadAllBytes(outputFilePath);
            var bytesInts = File.ReadAllBytes(outputFilePathInt);

            Assert.Equal(bytesOriginal, bytesNormal);
            Assert.Equal(bytesOriginal, bytesInts);
        }

        [Theory]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\viola_sus_short-portato_64-127_E4 - AB-comp Samples\\viola_sus_short-portato_64-127_E4 - AB.ncw")]
        public void WriteNCW(string inputFilePath)
        {
            var outputDirectoryPath = "C:\\Users\\periv\\Projects\\Temp";

            output.WriteLine("Reading NCW - {0}", inputFilePath);

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
            output.WriteLine("Writing NCW file w/24 bit {0} ...", outputFilePathNCW24);
            ncwParser.WriteNCW24(wavHeader);
            ncwParser.SaveToNCW(outputFilePathNCW24);

            string outputFileNameNCW32 = Path.GetFileNameWithoutExtension(inputFilePath) + "_32.ncw";
            string outputFilePathNCW32 = Path.Combine(outputDirectoryPath, outputFileNameNCW32);
            output.WriteLine("Writing NCW file w/32 bit file {0} ...", outputFilePathNCW32);
            ncwParser.WriteNCW32(wavHeader);
            ncwParser.SaveToNCW(outputFilePathNCW32);

            // read as bytes, but skip to after the unknown bytes
            var bytesOriginal = File.ReadAllBytes(inputFilePath).AsSpan().Slice(120).ToArray();
            var bytes24bit = File.ReadAllBytes(outputFilePathNCW24).AsSpan().Slice(120).ToArray();
            var bytes32bit = File.ReadAllBytes(outputFilePathNCW32).AsSpan().Slice(120).ToArray();

            Assert.Equal(bytesOriginal, bytes24bit);
            Assert.Equal(bytesOriginal, bytes32bit);
        }

        [Theory]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB.wav")]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - u8bit.wav")]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 16bit.wav")]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 24bit.wav")]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 32bit.wav")]
        [InlineData(@"C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 32bit float.wav")]
        public void ReadWAV(string inputFilePath)
        {
            var outputDirectoryPath = "C:\\Users\\periv\\Projects\\Temp";

            // read wave file
            var wp = new WAVParser();
            wp.OpenWav(inputFilePath);
            int origChunkSize = (int)wp.WavHeader.chnkSize;
            int origDataPos = (int)wp.WavHeader.dataPos;
            int newDataPos = 44 + origChunkSize - 16;

            output.WriteLine("Reading WAV w/ chunkSize: {0} and dataPos: {1} - {2}", origChunkSize, origDataPos, inputFilePath);

            // read as ints
            int[] ints = new int[wp.WavHeader.numOfPoints * wp.WavHeader.nChannels];
            wp.ReadToIntegers(ref ints);
            string outputFileNameInts = Path.GetFileNameWithoutExtension(inputFilePath) + " - ints.wav";
            string outputFilePathInts = Path.Combine(outputDirectoryPath, outputFileNameInts);
            output.WriteLine("Writing WAV w/ chunkSize: {0} and dataPos: {1} - {2}", (int)wp.WavHeader.dataPos, newDataPos, outputFilePathInts);
            wp.SaveWAVFromIntegers(outputFilePathInts, ref ints, wp.WavHeader.chnkSize);

            // read as floats
            float[] floats = new float[wp.WavHeader.numOfPoints * wp.WavHeader.nChannels];
            wp.ReadToFloats(ref floats, (uint)wp.WavHeader.numOfPoints);
            string outputFileNameFloats = Path.GetFileNameWithoutExtension(inputFilePath) + " - floats.wav";
            string outputFilePathFloats = Path.Combine(outputDirectoryPath, outputFileNameFloats);
            output.WriteLine("Writing WAV w/ chunkSize: {0} and dataPos: {1} - {2}", (int)wp.WavHeader.dataPos, newDataPos, outputFilePathFloats);
            wp.SaveStandardWAVMulti(outputFilePathFloats, ref floats, wp.WavHeader.chnkSize);

            wp.CloseReader();

            // read as bytes, but skip to datapos to avoid potential meta chunks
            var bytesOriginal = File.ReadAllBytes(inputFilePath).AsSpan().Slice(origDataPos).ToArray();
            var bytesInts = File.ReadAllBytes(outputFilePathInts).AsSpan().Slice(newDataPos).ToArray();
            var bytesFloats = File.ReadAllBytes(outputFilePathFloats).AsSpan().Slice(newDataPos).ToArray();

            Assert.Equal(bytesOriginal, bytesInts);
            Assert.Equal(bytesOriginal, bytesFloats);
        }
    }
}