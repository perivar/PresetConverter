using CommonUtils;
using PresetConverter;
using Xunit;

namespace TestProject
{
    public class SteinbergTests
    {
        [Theory]
        [UserHomeRelativeData(@"/OneDrive/DevProjects/Steinberg Media Technologies/Compressor/Sidechain - Default.vstpreset")]
        [UserHomeRelativeData(@"/OneDrive/DevProjects/Steinberg Media Technologies/Compressor/Sidechain - Vocal Delay Throws.vstpreset")]
        [UserHomeRelativeData(@"/OneDrive/DevProjects/Steinberg Media Technologies/Frequency/Boost High Side (Stereo).vstpreset")]
        [UserHomeRelativeData(@"/OneDrive/DevProjects/Steinberg Media Technologies/Frequency/Subtle Stereo Enhancer (Stereo).vstpreset")]
        [UserHomeRelativeData(@"/OneDrive/DevProjects/Steinberg Media Technologies/MultibandCompressor/Drum Bus (Get Bass Under Control Before Saturation).vstpreset")]
        [UserHomeRelativeData(@"/OneDrive/DevProjects/Steinberg Media Technologies/Groove Agent SE/Loveless - They Don't Know.vstpreset")]
        // [UserHomeRelativeData(@"/OneDrive/DevProjects/Steinberg Media Technologies/REVerence/Bricasti - Small Room 27.vstpreset")]
        public void Test2(string filePath)
        {
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
