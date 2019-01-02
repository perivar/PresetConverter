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
        [InlineData(@"C:\Users\perner\Amazon Drive\Documents\My Projects\Steinberg Media Technologies\Compressor\Sidechain - Default.vstpreset")]
        [InlineData(@"C:\Users\perner\Amazon Drive\Documents\My Projects\Steinberg Media Technologies\Compressor\Sidechain - Vocal Delay Throws.vstpreset")]
        [InlineData(@"C:\Users\perner\Amazon Drive\Documents\My Projects\Steinberg Media Technologies\Frequency\Boost High Side (Stereo).vstpreset")]
        [InlineData(@"C:\Users\perner\Amazon Drive\Documents\My Projects\Steinberg Media Technologies\Frequency\Subtle Stereo Enhancer (Stereo).vstpreset")]
        // [InlineData(@"C:\Users\perner\Amazon Drive\Documents\My Projects\Steinberg Media Technologies\REVerence\Bricasti - Small Room 27.vstpreset")]
        public void Test2(string filePath)
        {
            var readBytes = File.ReadAllBytes(filePath);
            // var readBytesShortenedString = StringUtils.ToHexEditorString(readBytes);

            var preset = VstPresetFactory.GetVstPreset<VstPreset>(filePath);

            var memStream = new MemoryStream();
            var successful = preset.WritePreset(memStream);
            var writeBytes = memStream.ToArray();
            // var writeBytesShortenedString = StringUtils.ToHexEditorString(writeBytes);

            Assert.Equal(readBytes, writeBytes, new JaggedByteComparer(0.001));

            // https://stackoverflow.com/questions/45284937/comparing-two-lists-with-xunit
            // Assert.True(shouldList.All(shouldItem => isList.Any(isItem => isItem == shouldItem)));
        }
    }
}
