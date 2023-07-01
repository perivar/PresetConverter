using System.Text;

namespace CommonUtils.Audio
{
    public static class SoundIOUtils
    {
        #region Rounding
        public static int RoundToClosestInt(double x)
        {
            // use AwayFromZero since default rounding is "round to even", which would make 1.5 => 1
            int y = (int)Math.Round(x, MidpointRounding.AwayFromZero);

            // nearbyint: The value of x rounded to a nearby integral (as a floating-point value).
            // Rounding using to-nearest rounding:
            // nearbyint (2.3) = 2.0
            // nearbyint (3.8) = 4.0
            // nearbyint (-2.3) = -2.0
            // nearbyint (-3.8) = -4.0
            return y;
        }

        public static int RoundUpToClosestInt(double x)
        {
            int y = (int)MathUtils.RoundUp(x);
            return y;
        }
        #endregion

        public static byte[] GetWaveHeaderBytes(bool isFloatingPoint, ushort channelCount, ushort bitDepth, int sampleRate, int totalSampleCount)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteWavHeader(ms,
                            isFloatingPoint,
                            channelCount,
                            bitDepth,
                            sampleRate,
                            totalSampleCount);

                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
        }

        // totalSampleCount needs to be the combined count of samples of all channels. 
        // So if the left and right channels contain 1000 samples each, then totalSampleCount should be 2000.
        // isFloatingPoint should only be true if the audio data is in 32-bit floating-point format.
        private static void WriteWavHeader(Stream stream, bool isFloatingPoint, ushort channelCount, ushort bitDepth, int sampleRate, int totalSampleCount)
        {
            stream.Position = 0;

            // RIFF header.
            // Chunk ID.
            stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);

            // Chunk size.
            stream.Write(BitConverter.GetBytes((bitDepth / 8 * totalSampleCount) + 36), 0, 4);

            // Format.
            stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);


            // Sub-chunk 1.
            // Sub-chunk 1 ID.
            stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);

            // Sub-chunk 1 size.
            stream.Write(BitConverter.GetBytes(16), 0, 4);

            // Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
            stream.Write(BitConverter.GetBytes((ushort)(isFloatingPoint ? 3 : 1)), 0, 2);

            // Channels.
            stream.Write(BitConverter.GetBytes(channelCount), 0, 2);

            // Sample rate.
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // Average bytes per second
            stream.Write(BitConverter.GetBytes(sampleRate * channelCount * (bitDepth / 8)), 0, 4);

            // Block align.
            stream.Write(BitConverter.GetBytes(channelCount * (bitDepth / 8)), 0, 2);

            // Bits per sample.
            stream.Write(BitConverter.GetBytes(bitDepth), 0, 2);


            // Sub-chunk 2.
            // Sub-chunk 2 ID.
            stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);

            // Sub-chunk 2 size.
            stream.Write(BitConverter.GetBytes(bitDepth / 8 * totalSampleCount), 0, 4);
        }
    }
}