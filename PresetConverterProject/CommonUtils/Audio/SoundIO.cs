using System;
using System.Text;

namespace CommonUtils.Audio
{
    public static class SoundIO
    {
        // compression code, 2 bytes
        public const int WAVE_FORMAT_UNKNOWN = 0x0000; // Microsoft Corporation
        public const int WAVE_FORMAT_PCM = 0x0001; // Microsoft Corporation
        public const int WAVE_FORMAT_ADPCM = 0x0002; // Microsoft Corporation
        public const int WAVE_FORMAT_IEEE_FLOAT = 0x0003; // Microsoft Corporation
        public const int WAVE_FORMAT_ALAW = 0x0006; // Microsoft Corporation
        public const int WAVE_FORMAT_MULAW = 0x0007; // Microsoft Corporation
        public const int WAVE_FORMAT_DTS_MS = 0x0008; // Microsoft Corporation
        public const int WAVE_FORMAT_WMAS = 0x000a; // WMA 9 Speech
        public const int WAVE_FORMAT_IMA_ADPCM = 0x0011; // Intel Corporation
        public const int WAVE_FORMAT_TRUESPEECH = 0x0022; // TrueSpeech
        public const int WAVE_FORMAT_GSM610 = 0x0031; // Microsoft Corporation
        public const int WAVE_FORMAT_MSNAUDIO = 0x0032; // Microsoft Corporation
        public const int WAVE_FORMAT_G726 = 0x0045; // ITU-T standard
        public const int WAVE_FORMAT_MPEG = 0x0050; // Microsoft Corporation
        public const int WAVE_FORMAT_MPEGLAYER3 = 0x0055; // ISO/MPEG Layer3 Format Tag
        public const int WAVE_FORMAT_DOLBY_AC3_SPDIF = 0x0092; // Sonic Foundry
        public const int WAVE_FORMAT_A52 = 0x2000;
        public const int WAVE_FORMAT_DTS = 0x2001;
        public const int WAVE_FORMAT_WMA1 = 0x0160; // WMA version 1
        public const int WAVE_FORMAT_WMA2 = 0x0161; // WMA (v2) 7, 8, 9 Series
        public const int WAVE_FORMAT_WMAP = 0x0162; // WMA 9 Professional
        public const int WAVE_FORMAT_WMAL = 0x0163; // WMA 9 Lossless
        public const int WAVE_FORMAT_DIVIO_AAC = 0x4143;
        public const int WAVE_FORMAT_AAC = 0x00FF;
        public const int WAVE_FORMAT_FFMPEG_AAC = 0x706D;

        public const int WAVE_FORMAT_DK3 = 0x0061;
        public const int WAVE_FORMAT_DK4 = 0x0062;
        public const int WAVE_FORMAT_VORBIS = 0x566f;
        public const int WAVE_FORMAT_VORB_1 = 0x674f;
        public const int WAVE_FORMAT_VORB_2 = 0x6750;
        public const int WAVE_FORMAT_VORB_3 = 0x6751;
        public const int WAVE_FORMAT_VORB_1PLUS = 0x676f;
        public const int WAVE_FORMAT_VORB_2PLUS = 0x6770;
        public const int WAVE_FORMAT_VORB_3PLUS = 0x6771;
        public const int WAVE_FORMAT_SPEEX = 0xa109; // Speex audio
        public const int WAVE_FORMAT_EXTENSIBLE = 0xFFFE; // Microsoft

        public static string[] INFO_TYPE = { "IARL", "IART", "ICMS", "ICMT", "ICOP",
            "ICRD", "ICRP", "IDIM", "IDPI", "IENG", "IGNR", "IKEY",
            "ILGT", "IMED", "INAM", "IPLT", "IPRD", "ISBJ",
            "ISFT", "ISHP", "ISRC", "ISRF", "ITCH",
            "ISMP", "IDIT", "VXNG", "TURL" };

        public static string[] INFO_DESC = { "Archival location", "Artist", "Commissioned", "Comments", "Copyright",
            "Creation date", "Cropped", "Dimensions", "Dots per inch", "Engineer", "Genre", "Keywords",
            "Lightness settings", "Medium", "Name of subject", "Palette settings", "Product", "Description",
            "Software package", "Sharpness", "Source", "Source form", "Digitizing technician",
            "SMPTE time code", "Digitization time", "VXNG", "Url" };

        /*
         * Native Formats
         * Number of Bits	MATLAB Data Type			Data Range
         * 8				uint8 (unsigned integer) 	0 <= y <= 255
         * 16				int16 (signed integer) 		-32768 <= y <= +32767
         * 24				int32 (signed integer) 		-2^23 <= y <= 2^23-1
         * 32				single (floating point) 	-1.0 <= y < +1.0
         * 
         * typedef uint8_t  u8_t;     ///< unsigned 8-bit value (0 to 255)
         * typedef int8_t   s8_t;     ///< signed 8-bit value (-128 to +127)
         * typedef uint16_t u16_t;    ///< unsigned 16-bit value (0 to 65535)
         * typedef int16_t  s16_t;    ///< signed 16-bit value (-32768 to 32767)
         * typedef uint32_t u32_t;    ///< unsigned 32-bit value (0 to 4294967296)
         * typedef int32_t  s32_t;    ///< signed 32-bit value (-2147483648 to +2147483647)
         */

        public static void Read8Bit(BinaryFile waveFile, float[][] sound, int sampleCount, int channels)
        {
            // read unsigned 8-bit int and convert to 32-bit float
            // convert [0x0, 0xFF] to [-1.0, 1.0]
            for (int i = 0; i < sampleCount; i++)
            {
                for (int ic = 0; ic < channels; ic++)
                {
                    byte b = waveFile.ReadByte();
                    sound[ic][i] = (float)b / (float)(128 - 1);
                }
            }
        }

        public static void Write8Bit(BinaryFile waveFile, float[][] sound, int sampleCount, int channels)
        {
            // write 32-bit float as unsigned 8-bit int
            // convert [-1.0, 1.0] to [0x0, 0xFF]
            // values outside the range [-1.0, 1.0] are properly clamped to 0 and 255
            for (int i = 0; i < sampleCount; i++)
            {
                for (int ic = 0; ic < channels; ic++)
                {
                    int val = SoundIOUtils.RoundToClosestInt((sound[ic][i] + 1) * 128);

                    if (val > 255) // 0xFF
                        val = 255;
                    if (val < 0)   // 0x0
                        val = 0;

                    byte b = (byte)val;

                    waveFile.Write(b);
                }
            }
        }
        public static void Read16Bit(BinaryFile waveFile, float[][] sound, int sampleCount, int channels)
        {
            // read signed 16-bit int and convert to 32-bit float
            // convert [0x8000, 0x7FFF] to [-1.0, 1.0] 
            for (int i = 0; i < sampleCount; i++)
            {
                for (int ic = 0; ic < channels; ic++)
                {
                    float f = (float)waveFile.ReadInt16();
                    f /= (float)32768; // 0x8000  
                    sound[ic][i] = f;
                }
            }
        }

        public static void Write16Bit(BinaryFile waveFile, float[][] sound, int sampleCount, int channels)
        {
            // write 32-bit float as signed 16-bit int
            // convert [-1.0, 1.0] to [0x8000, 0x7FFF]
            // values outside the range [-1.0, 1.0] are properly clamped to -32768 and 32767
            for (int i = 0; i < sampleCount; i++)
            {
                for (int ic = 0; ic < channels; ic++)
                {
                    int val = SoundIOUtils.RoundToClosestInt(sound[ic][i] * 32768); // 0x8000

                    if (val > 32767)  // 0x7FFF
                        val = 32767;
                    if (val < -32768) // 0x8000
                        val = -32768;

                    waveFile.Write((Int16)val);
                }
            }
        }

        public static void Read24Bit(BinaryFile waveFile, float[][] sound, int sampleCount, int channels)
        {
            // read signed 24-bit int and convert to 32-bit float
            // convert [0x800000, 0x7FFFFF] to [-1.0, 1.0]
            for (int i = 0; i < sampleCount; i++)
            {
                for (int ic = 0; ic < channels; ic++)
                {
                    byte[] buffer = waveFile.ReadBytes(0, 3);
                    float f = (((sbyte)buffer[2] << 16) | (buffer[1] << 8) | buffer[0]) / (float)8388608;
                    sound[ic][i] = f;
                }
            }
        }

        public static void Write24Bit(BinaryFile waveFile, float[][] sound, int sampleCount, int channels)
        {
            // write 32-bit float as signed 24-bit int
            // convert [-1.0, 1.0] to [0x800000, 0x7FFFFF]
            // values outside the range [-1.0, 1.0] are properly clamped to -8388608 and 8388607
            for (int i = 0; i < sampleCount; i++)
            {
                for (int ic = 0; ic < channels; ic++)
                {
                    float sample32 = sound[ic][i];

                    // clip
                    if (sample32 > 1.0f)
                        sample32 = 1.0f;
                    if (sample32 < -1.0f)
                        sample32 = -1.0f;

                    var sample24 = (int)(sample32 * (float)8388608); // 0x800000
                    byte[] buffer = BitConverter.GetBytes(sample24);
                    waveFile.Write(new[] { buffer[0], buffer[1], buffer[2] });
                }
            }
        }

        public static void Read32Bit(BinaryFile waveFile, float[][] sound, int sampleCount, int channels)
        {
            // read signed 32-bit int and convert to 32-bit float
            // convert [0x80000000, 0x7FFFFFFF] to [-1.0, 1.0]
            for (int i = 0; i < sampleCount; i++)
            {
                for (int ic = 0; ic < channels; ic++)
                {
                    float f = (float)waveFile.ReadInt32();
                    f /= (float)2147483648; // 0x80000000
                    sound[ic][i] = f;
                }
            }
        }

        public static void Write32Bit(BinaryFile waveFile, float[][] sound, int sampleCount, int channels)
        {
            // write 32-bit float as signed 32-bit int
            // convert [-1.0, 1.0] to [0x80000000, 0x7fffffff]
            // values outside the range [-1.0, 1.0] are properly clamped to -2147483648 and 2147483647,
            for (int i = 0; i < sampleCount; i++)
            {
                for (int ic = 0; ic < channels; ic++)
                {
                    int val = SoundIOUtils.RoundToClosestInt(sound[ic][i] * 2147483648); //  0x80000000

                    if (val > 2147483647) // 0x7FFFFFFF   
                        val = 2147483647;
                    if (val < -2147483648) // -0x80000000 
                        val = -2147483648;

                    waveFile.Write((int)val);
                }
            }
        }

        public static void Read32BitFloat(BinaryFile waveFile, float[][] sound, int sampleCount, int channels)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                for (int ic = 0; ic < channels; ic++)
                {
                    float d = waveFile.ReadSingle();
                    sound[ic][i] = d;
                }
            }
        }

        public static void Write32BitFloat(BinaryFile waveFile, float[][] sound, int sampleCount, int channels)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                for (int ic = 0; ic < channels; ic++)
                {
                    float sample = sound[ic][i];

                    if (sample < -1 || sample > 1)
                        sample = Math.Max(-1, Math.Min(1, sample));

                    waveFile.Write(sample);
                }
            }
        }

        public static float ReadWaveDurationInSeconds(BinaryFile waveFile)
        {
            int channels = -1;
            int sampleCount = -1;
            int sampleRate = -1;
            float lengthInSeconds = -1;
            int audioFormat = -1;
            int bitsPerSample = -1;
            int bytesPerSec = -1;
            ReadWaveFileHeader(waveFile, ref channels, ref sampleCount, ref sampleRate, ref lengthInSeconds, ref audioFormat, ref bitsPerSample, ref bytesPerSec);

            waveFile.Close();
            return lengthInSeconds;
        }

        public static void ReadWaveFileHeader(BinaryFile waveFile, ref int channels, ref int sampleCount, ref int sampleRate, ref float lengthInSeconds, ref int audioFormat, ref int bitsPerSample, ref int bytesPerSec)
        {
            // Read header while keeping the binary file open

            // default wave integers
            int RIFF = BinaryFile.StringToInt32("RIFF");    // 1179011410
            int WAVE = BinaryFile.StringToInt32("WAVE");    // 1163280727
            int FMT = BinaryFile.StringToInt32("fmt ");     // 544501094
            int DATA = BinaryFile.StringToInt32("data");    // 1635017060

            //			Size  Description                  Value
            // tag[0]	4	  RIFF Header				   RIFF (1179011410)
            // tag[1] 	4	  RIFF data size
            // tag[2] 	4	  form-type (WAVE etc)			(1163280727)
            // tag[3] 	4     Chunk ID                     "fmt " (0x666D7420) = 544501094
            // tag[4]	4     Chunk Data Size              16 + extra format bytes 	// long chunkSize;
            // tag[5]	2     Compression code             1 - 65,535	// short wFormatTag;
            // tag[6]	2     Number of channels           1 - 65,535
            // tag[7]	4     Sample rate                  1 - 0xFFFFFFFF
            // tag[8]	4     Average bytes per second     1 - 0xFFFFFFFF
            // tag[9]	2     Block align                  1 - 65,535 (4)
            // tag[10]	2     Significant bits per sample  2 - 65,535 (32)
            // tag[11]	4	  IEEE = 1952670054 (0x74636166) = fact chunk
            // 				  PCM = 1635017060 (0x61746164)  (datachunk = 1635017060)
            // tag[12] 	4	  Subchunk2Size == NumSamples * NumChannels * BitsPerSample/8

            // tag reading
            var tag = new int[13];
            for (int i = 0; i < 13; i++)
            {
                tag[i] = 0;

                if ((i == 5) || (i == 6) || (i == 9) || (i == 10))
                {
                    tag[i] = waveFile.ReadUInt16();
                }
                else
                {
                    tag[i] = (int)waveFile.ReadUInt32();
                }
            }

            #region File format checking
            if (tag[0] != RIFF || tag[2] != WAVE)
            {
                throw new FormatException("This file is not in WAVE format");
            }

            // fmt tag, chunkSize and data tag
            if (tag[3] != FMT || tag[4] != 16 || tag[11] != DATA)
            {
                throw new NotSupportedException("This WAVE file format is not currently supported");
            }

            // bits per sample
            bitsPerSample = tag[10];

            // audio format
            audioFormat = tag[5];
            if (audioFormat != WAVE_FORMAT_PCM && audioFormat != WAVE_FORMAT_IEEE_FLOAT)
            {
                throw new NotSupportedException("Non PCM WAVE files are not currently supported");
            }
            #endregion File format checking

            channels = tag[6];
            sampleRate = tag[7];
            bytesPerSec = tag[8];

            // calculate sample count
            // Subchunk2Size == NumSamples * NumChannels * BitsPerSample/8
            int subchunk2Size = tag[12];
            sampleCount = subchunk2Size / (bitsPerSample / 8) / channels;

            // calculate duration in seconds            
            lengthInSeconds = ((float)sampleCount / (float)bytesPerSec);

            // Note! Do not close file
        }

        public static float[][] ReadWaveFile(BinaryFile waveFile, ref int channels, ref int sampleCount, ref int sampleRate, ref float lengthInSeconds)
        {
            int audioFormat = -1;
            int bitsPerSample = 1;
            int bytesPerSec = -1;
            ReadWaveFileHeader(waveFile, ref channels, ref sampleCount, ref sampleRate, ref lengthInSeconds, ref audioFormat, ref bitsPerSample, ref bytesPerSec);

            float[][] sound = new float[channels][];

            for (int ic = 0; ic < channels; ic++)
            {
                sound[ic] = new float[sampleCount];
            }

            #region Data loading
            if (bitsPerSample == 8)
            {
                Read8Bit(waveFile, sound, sampleCount, channels);
            }
            else if (bitsPerSample == 16)
            {
                Read16Bit(waveFile, sound, sampleCount, channels);
            }
            else if (bitsPerSample == 24)
            {
                Read24Bit(waveFile, sound, sampleCount, channels);
            }
            else if (bitsPerSample == 32)
            {
                if (audioFormat == WAVE_FORMAT_PCM)
                {
                    Read32Bit(waveFile, sound, sampleCount, channels);
                }
                else if (audioFormat == WAVE_FORMAT_IEEE_FLOAT)
                {
                    Read32BitFloat(waveFile, sound, sampleCount, channels);
                }
            }
            #endregion Data loading

            waveFile.Close();
            return sound;
        }

        public static void WriteWaveFile(string path, byte[] soundData, bool isFloatingPoint, int channelCount, int sampleRate, int bitDepth)
        {
            var bf = new BinaryFile(path, BinaryFile.ByteOrder.LittleEndian, true);

            int totalSampleCount = soundData.Length;

            // RIFF header.
            // Chunk ID.
            bf.Write(Encoding.ASCII.GetBytes("RIFF"));

            // Chunk size.
            bf.Write(BitConverter.GetBytes(totalSampleCount + 36));

            // Format.
            bf.Write(Encoding.ASCII.GetBytes("WAVE"));

            // Sub-chunk 1.
            // Sub-chunk 1 ID.
            bf.Write(Encoding.ASCII.GetBytes("fmt "));

            // Sub-chunk 1 size.
            bf.Write(BitConverter.GetBytes(16));

            // Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
            bf.Write(BitConverter.GetBytes((ushort)(isFloatingPoint ? 3 : 1)));

            // Channels.
            bf.Write(BitConverter.GetBytes((ushort)channelCount));

            // Sample rate.
            bf.Write(BitConverter.GetBytes(sampleRate));

            // Average bytes per second
            bf.Write(BitConverter.GetBytes(sampleRate * channelCount * (bitDepth / 8)));

            // Block align.
            bf.Write(BitConverter.GetBytes((ushort)(channelCount * (bitDepth / 8))));

            // Bits per sample.
            bf.Write(BitConverter.GetBytes((ushort)bitDepth));

            // Sub-chunk 2.
            // Sub-chunk 2 ID.
            bf.Write(Encoding.ASCII.GetBytes("data"));

            // Sub-chunk 2 size.
            bf.Write(BitConverter.GetBytes(totalSampleCount));


            bf.Write(soundData);
            bf.Close();
        }

        public static void WriteWaveFile(string path, float[][] sound, int channels, int sampleRate, int bitsPerSample = 32)
        {
            WriteWaveFile(new BinaryFile(path, BinaryFile.ByteOrder.LittleEndian, true), sound, channels, sound[0].Length, sampleRate, bitsPerSample);
        }

        public static void WriteWaveFile(string path, float[] monosound, int sampleRate, int bitsPerSample = 32)
        {
            WriteWaveFile(new BinaryFile(path, BinaryFile.ByteOrder.LittleEndian, true), new float[][] { monosound }, 1, monosound.Length, sampleRate, bitsPerSample);
        }

        public static void WriteWaveFile(BinaryFile waveFile, float[][] sound, int numChannels, int numSamples, int sampleRate, int bitsPerSample = 32)
        {
            // default wave integers
            int RIFF = BinaryFile.StringToInt32("RIFF");    // 1179011410
            int WAVE = BinaryFile.StringToInt32("WAVE");    // 1163280727
            int FMT = BinaryFile.StringToInt32("fmt ");     // 544501094
            int DATA = BinaryFile.StringToInt32("data");    // 1635017060

            /*
			The canonical WAVE format starts with the RIFF header:
			0         4   ChunkID          Contains the letters "RIFF" in ASCII form
										   (0x52494646 big-endian form).
			4         4   ChunkSize        36 + SubChunk2Size, or more precisely:
										   4 + (8 + SubChunk1Size) + (8 + SubChunk2Size)
										   This is the size of the rest of the chunk 
										   following this number.  This is the size of the 
										   entire file in bytes minus 8 bytes for the
										   two fields not included in this count:
										   ChunkID and ChunkSize.
			8         4   Format           Contains the letters "WAVE"
										   (0x57415645 big-endian form).

			The "WAVE" format consists of two subchunks: "fmt " and "data":
			The "fmt " subchunk describes the sound data's format:

			12        4   Subchunk1ID      Contains the letters "fmt "
										   (0x666d7420 big-endian form).
			16        4   Subchunk1Size    16 for PCM.  This is the size of the
										   rest of the Subchunk which follows this number.
			20        2   AudioFormat      PCM = 1 (i.e. Linear quantization)
										   Values other than 1 indicate some 
										   form of compression.
			22        2   NumChannels      Mono = 1, Stereo = 2, etc.
			24        4   SampleRate       8000, 44100, etc.
			28        4   ByteRate         == SampleRate * NumChannels * BitsPerSample/8
			32        2   BlockAlign       == NumChannels * BitsPerSample/8
										   The number of bytes for one sample including
										   all channels. I wonder what happens when
										   this number isn't an integer?
			34        2   BitsPerSample    8 bits = 8, 16 bits = 16, etc.
					  2   ExtraParamSize   if PCM, then doesn't exist
					  X   ExtraParams      space for extra parameters

			The "data" subchunk contains the size of the data and the actual sound:

			36        4   Subchunk2ID      Contains the letters "data"
										   (0x64617461 big-endian form).
			40        4   Subchunk2Size    == NumSamples * NumChannels * BitsPerSample/8
										   This is the number of bytes in the data.
										   You can also think of this as the size
										   of the read of the subchunk following this 
										   number.
			44        *   Data             The actual sound data.
            */

            #region WAV tags generation
            int[] tag = { RIFF, 0, WAVE, FMT, 16, 1, 1, 0, 0, 0, 0, DATA, 0, 0 };

            tag[12] = numSamples * numChannels * (bitsPerSample / 8);
            tag[1] = tag[12] + 36;

            if ((bitsPerSample == 8) || (bitsPerSample == 16))
                tag[5] = WAVE_FORMAT_PCM;

            if (bitsPerSample == 32)
                tag[5] = WAVE_FORMAT_IEEE_FLOAT;

            tag[6] = numChannels;
            tag[7] = sampleRate;
            tag[8] = sampleRate * bitsPerSample / 8; // Average bytes per second
            tag[9] = numChannels * bitsPerSample / 8; // Block align
            tag[10] = bitsPerSample; // Significant bits per sample

            #endregion WAV tags generation

            // tag writing
            for (int i = 0; i < 13; i++)
            {
                if ((i == 5) || (i == 6) || (i == 9) || (i == 10))
                {
                    waveFile.Write((ushort)tag[i]);
                }
                else
                {
                    waveFile.Write((uint)tag[i]);
                }
            }

            if (bitsPerSample == 8)
            {
                Write8Bit(waveFile, sound, numSamples, numChannels);
            }
            else if (bitsPerSample == 16)
            {
                Write16Bit(waveFile, sound, numSamples, numChannels);
            }
            else if (bitsPerSample == 24)
            {
                Write24Bit(waveFile, sound, numSamples, numChannels);
            }
            else if (bitsPerSample == 32)
            {
                Write32BitFloat(waveFile, sound, numSamples, numChannels);
            }

            waveFile.Close();
        }
    }
}