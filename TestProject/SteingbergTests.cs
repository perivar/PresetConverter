using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using CommonUtils;
using PresetConverter;
using Xunit;

namespace PresetConverterTests
{
    public class SteinbergTests
    {
        [Fact]
        public void Test1()
        {

        }

        [Theory]
        [InlineData(@"Compressor\Sidechain - Default.vstpreset")]
        [InlineData(@"Compressor\Sidechain - Vocal Delay Throws.vstpreset")]
        [InlineData(@"Frequency\Boost High Side (Stereo).vstpreset")]
        [InlineData(@"Frequency\Subtle Stereo Enhancer (Stereo).vstpreset")]
        [InlineData(@"MultibandCompressor\Drum Bus (Get Bass Under Control Before Saturation).vstpreset")]
        [InlineData(@"Groove Agent SE\Loveless - They Don't Know.vstpreset")]
        // [InlineData(@"C:\Users\perner\Amazon Drive\Documents\My Projects\Steinberg Media Technologies\REVerence\Bricasti - Small Room 27.vstpreset")]
        public void Test2(string fileName)
        {
            string outputDirectoryPath = @"C:\Users\perner\Amazon Drive\Documents\My Projects\Steinberg Media Technologies";
            string filePath = Path.Combine(outputDirectoryPath, fileName);

            var readBytes = File.ReadAllBytes(filePath);

            var preset = VstPresetFactory.GetVstPreset<VstPreset>(filePath);

            var memStream = new MemoryStream();
            var successful = preset.WritePreset(memStream);
            var writeBytes = memStream.ToArray();

            Assert.Equal(readBytes, writeBytes, new JaggedByteComparer(0.001));

            // https://stackoverflow.com/questions/45284937/comparing-two-lists-with-xunit
            // Assert.True(shouldList.All(shouldItem => isList.Any(isItem => isItem == shouldItem)));
        }
    }
}
