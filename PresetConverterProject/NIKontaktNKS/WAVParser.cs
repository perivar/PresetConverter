using System.Runtime.InteropServices;
using System.Text;
using CommonUtils;
using CommonUtils.Audio;
using Serilog;

namespace PresetConverterProject.NIKontaktNKS
{
    public class WAVParser
    {
        public static readonly byte[] WAV_TEST_GUID = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x16, 0x00, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71 };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct TFileWAVHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] RIFFtag;
            public uint fileSize;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] WAVEtag;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] FMTtag;
            public uint chnkSize;
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct TFileWAVHeaderEx
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] RIFFtag;
            public uint fileSize;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] WAVEtag;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] FMTtag;
            public uint chnkSize;
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
            public ushort realBps;
            public uint speakers;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public char[] GUID;
        }

        public struct TMyWAVHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] RIFFtag;        // Contains "RIFF"
            public uint fileSize;         // Size of the wav portion of the file, which follows the first 8 bytes. File size - 8
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] WAVEtag;        // Contains "WAVE"
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] FMTtag;         // Contains "fmt " (includes trailing space)
            public uint chnkSize;         // Should be 16 for PCM
            public ushort wFormatTag;     // Should be 1 for PCM. 3 for IEEE Float
            public ushort nChannels;
            public uint nSamplesPerSec;   // = Sample Rate
            public uint nAvgBytesPerSec;  // Number of bytes per second. sample_rate * num_channels * Bytes Per Sample
            public ushort nBlockAlign;    // num_channels * Bytes Per Sample
            public ushort wBitsPerSample; // Number of bits per sample
            public ushort cbSize;
            public ushort realBps;
            public uint speakers;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] GUID;
            public bool extended;
            public uint dataPos;
            public uint dataSize;
            public int numOfPoints;
        }

        // signed 8-bit value  (-128 to +127)               = (-0x80 to 0x7F)
        // signed 16-bit value (-32768 to 32767)            = (-0x8000 to 0x7FFF)
        // signed 24-bit value (-8388608 to 8388607)        = (-0x800000 to 0x7FFFFF) 
        // signed 32-bit value (-2147483648 to +2147483647) = (-0x80000000 to +0x7FFFFFFF)

        // use one mroe than the real max value
        const int MAX_8BIT = 128; // 0x80
        const int MAX_16BIT = 32768; // 0x8000
        const int MAX_24BIT = 8388608; // 0x800000
        const long MAX_32BIT = 2147483648; // 0x80000000

        private TMyWAVHeader wavHeader;
        public TMyWAVHeader WavHeader { get => wavHeader; set => wavHeader = value; }

        private FileStream? fsWriter;
        private BinaryFile? bfReader;

        private readonly bool doVerbose = false;

        public WAVParser(bool doVerbose = false)
        {
            this.doVerbose = doVerbose;
        }

        // Reading
        public void OpenWav(string filename)
        {
            try
            {
                bfReader = new BinaryFile(filename, BinaryFile.ByteOrder.LittleEndian);
            }
            catch (Exception e)
            {
                throw new Exception("Can't open file: " + e);
            }

            ProcessHeader();
        }

        public bool ProcessHeader()
        {
            if (bfReader == null || bfReader != null && !bfReader.CanRead)
            {
                Log.Error("File not opened");
                return false;
            }

            bfReader.Seek(0, SeekOrigin.Begin);
            char[] head = bfReader.ReadChars(4);
            wavHeader.RIFFtag = head;
            if (new string(head) != "RIFF")
            {
                Log.Error("Wrong format or bad file header (RIFF)");
                return false;
            }

            uint fSize = bfReader.ReadUInt32();
            wavHeader.fileSize = fSize;
            fSize += 8;
            if (bfReader.Length != fSize)
            {
                Log.Error("Wrong file size or bad header");
                return false;
            }

            head = bfReader.ReadChars(4);
            wavHeader.WAVEtag = head;
            if (new string(head) != "WAVE")
            {
                Log.Error("Wrong format or bad file header (WAVE)");
                return false;
            }

            head = bfReader.ReadChars(4);
            wavHeader.FMTtag = head;
            if (new string(head) != "fmt ")
            {
                Log.Error("Wrong format or bad file header (fmt )");
                return false;
            }

            uint chnkSize = bfReader.ReadUInt32();
            wavHeader.chnkSize = chnkSize;

            ushort frmt = bfReader.ReadUInt16();

            // --- Standard PCM file
            if (frmt == SoundIO.WAVE_FORMAT_PCM)
            {
                wavHeader.extended = false;
                if ((chnkSize != 16) && (chnkSize != 20))
                {
                    Log.Error("Wrong format or bad file header (fmt chunk size)");
                    return false;
                }

                wavHeader.wFormatTag = frmt;
                wavHeader.nChannels = bfReader.ReadUInt16();
                wavHeader.nSamplesPerSec = bfReader.ReadUInt32();
                wavHeader.nAvgBytesPerSec = bfReader.ReadUInt32();
                wavHeader.nBlockAlign = bfReader.ReadUInt16();
                wavHeader.wBitsPerSample = bfReader.ReadUInt16();

                if ((wavHeader.nChannels * wavHeader.wBitsPerSample * wavHeader.nSamplesPerSec / 8) != wavHeader.nAvgBytesPerSec)
                    Log.Error("Bad file header (AvgBytesPerSec)");
                else if ((wavHeader.wBitsPerSample != 8) && (wavHeader.wBitsPerSample != 16) && (wavHeader.wBitsPerSample != 24))
                    Log.Error("Wrong bits per sample (8, 16 and 24 allowed)");
                else if ((wavHeader.nChannels != 1) && (wavHeader.nChannels != 2))
                    Log.Error("Only mono and stereo allowed");
            }

            // --- 32 bit float
            else if (frmt == SoundIO.WAVE_FORMAT_IEEE_FLOAT)
            {
                wavHeader.extended = false;
                if ((chnkSize != 16) && (chnkSize != 20))
                {
                    Log.Error("Wrong format or bad file header (fmt chunk size)");
                    return false;
                }

                wavHeader.wFormatTag = frmt;
                wavHeader.nChannels = bfReader.ReadUInt16();
                wavHeader.nSamplesPerSec = bfReader.ReadUInt32();
                wavHeader.nAvgBytesPerSec = bfReader.ReadUInt32();
                wavHeader.nBlockAlign = bfReader.ReadUInt16();
                wavHeader.wBitsPerSample = bfReader.ReadUInt16();

                if ((wavHeader.nChannels * wavHeader.wBitsPerSample * wavHeader.nSamplesPerSec / 8) != wavHeader.nAvgBytesPerSec)
                    Log.Error("Bad file header (AvgBytesPerSec)");
                else if (wavHeader.wBitsPerSample != 32)
                    Log.Error("Wrong bits per sample (only 32 allowed)");
                else if ((wavHeader.nChannels != 1) && (wavHeader.nChannels != 2))
                    Log.Error("Only mono and stereo allowed");
            }

            // --- Extensible file header
            else if ((frmt == SoundIO.WAVE_FORMAT_EXTENSIBLE) && (chnkSize == 40))
            {
                wavHeader.extended = true;

                wavHeader.wFormatTag = frmt;
                wavHeader.nChannels = bfReader.ReadUInt16();
                wavHeader.nSamplesPerSec = bfReader.ReadUInt32();
                wavHeader.nAvgBytesPerSec = bfReader.ReadUInt32();
                wavHeader.nBlockAlign = bfReader.ReadUInt16();
                wavHeader.wBitsPerSample = bfReader.ReadUInt16();
                wavHeader.cbSize = bfReader.ReadUInt16();
                wavHeader.realBps = bfReader.ReadUInt16();

                if ((wavHeader.nChannels * wavHeader.wBitsPerSample * wavHeader.nSamplesPerSec / 8) != wavHeader.nAvgBytesPerSec)
                    Log.Error("Bad file header (AvgBytesPerSec)");
                else if (wavHeader.cbSize != 22)
                    Log.Error("Bad file header (extension chink size)");
                else if ((wavHeader.realBps != 8) && (wavHeader.realBps != 16) && (wavHeader.realBps != 24))
                    Log.Error("Wrong bits per sample (8, 16 and 24 allowed)");
                else if ((wavHeader.nChannels != 1) && (wavHeader.nChannels != 2))
                    Log.Error("Only mono and stereo allowed");

                wavHeader.speakers = bfReader.ReadUInt32();
                wavHeader.GUID = bfReader.ReadBytes(16);

                if (wavHeader.GUID.SequenceEqual(WAV_TEST_GUID))
                {
                    Log.Error("Non supported format (GUID)");
                    return false;
                }
            }
            else
            {
                Log.Error("Non supported WAV format");
                return false;
            }

            // --- Search for data chunk
            if (chnkSize == 20)
            {
                head = bfReader.ReadChars(4);
            }

            bool dataFound = false;
            while ((!dataFound) && (bfReader.Position < (bfReader.Length - 1)))
            {
                head = bfReader.ReadChars(4);
                chnkSize = bfReader.ReadUInt32();
                wavHeader.dataPos = (uint)bfReader.Position;
                wavHeader.dataSize = chnkSize;

                bfReader.Seek(chnkSize, SeekOrigin.Current);
                if (new string(head) == "data") dataFound = true;
            }
            if (!dataFound)
            {
                Log.Error("No data chunk found");
                return false;
            }

            wavHeader.numOfPoints = (int)(wavHeader.dataSize / wavHeader.nBlockAlign);

            bfReader.Seek(wavHeader.dataPos, SeekOrigin.Begin);

            return true;
        }

        public int ReadToFloats(ref float[] data, uint num, uint offset)
        {
            if (bfReader == null || bfReader != null && !bfReader.CanRead)
            {
                Log.Error("File not opened");
                return -1;
            }

            if (offset >= (uint)wavHeader.numOfPoints)
            {
                Log.Error("Offset beyond the end of file");
                return -1;
            }

            if ((num + offset) > (uint)wavHeader.numOfPoints)
            {
                num = (uint)wavHeader.numOfPoints - offset;
            }

            uint step = 0;
            if (wavHeader.extended)
            {
                step = (uint)(wavHeader.realBps * wavHeader.nChannels / 8);
            }
            else
            {
                step = (uint)wavHeader.nBlockAlign;
            }

            bfReader.Seek(wavHeader.dataPos + step * offset, SeekOrigin.Begin);

            int bytesRead = 0;

            // --- 8 bit
            if (wavHeader.wBitsPerSample == 8)
            {
                for (int i = 0; i < num; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        // read unsigned 8-bit int and convert to 32-bit float
                        // convert [0x0, 0xFF] to [-1.0, 1.0]
                        // 0 - 255
                        byte b = (byte)bfReader.ReadByte();
                        bytesRead += 1;
                        data[i * wavHeader.nChannels + j] = (float)(b - MAX_8BIT + 1) / MAX_8BIT;
                    }
                }
            }
            else
            // --- 16 bit
            if (wavHeader.wBitsPerSample == 16)
            {
                for (int i = 0; i < num; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        // read signed 16-bit int and convert to 32-bit float
                        // convert [0x8000, 0x7FFF] to [-1.0, 1.0] 
                        // -32768 to 32767
                        short s = bfReader.ReadInt16();
                        bytesRead += 2;
                        data[i * wavHeader.nChannels + j] = (float)s / MAX_16BIT;
                    }
                }
            }
            else
            // --- 24 bit (3 bytes per sample)
            if (wavHeader.wBitsPerSample == 24)
            {
                for (int i = 0; i < num; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        // read signed 24-bit int and convert to 32-bit float
                        // convert [0x800000, 0x7FFFFF] to [-1.0, 1.0]
                        // -8388608 to 8388607
                        byte[] bytes = bfReader.ReadBytes(3);
                        int l = (bytes[0] << 8) | (bytes[1] << 16) | (bytes[2] << 24);
                        bytesRead += 3;
                        // l <<= 8;
                        data[i * wavHeader.nChannels + j] = (float)l / MAX_24BIT;
                    }
                }
            }
            else
            // --- 32 bit
            if (wavHeader.wBitsPerSample == 32)
            {
                for (int i = 0; i < num; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        if (wavHeader.wFormatTag == SoundIO.WAVE_FORMAT_PCM)
                        {
                            // read signed 32-bit int and convert to 32-bit float
                            // convert [0x80000000, 0x7FFFFFFF] to [-1.0, 1.0]
                            // -2147483648 to 2147483647
                            int l = bfReader.ReadInt32();
                            bytesRead += 4;
                            data[i * wavHeader.nChannels + j] = (float)l / MAX_32BIT;
                        }
                        else if (wavHeader.wFormatTag == SoundIO.WAVE_FORMAT_IEEE_FLOAT)
                        {
                            float d = bfReader.ReadSingle();
                            data[i * wavHeader.nChannels + j] = d;
                        }
                    }
                }
            }
            else
            {
                Log.Error("Wrong data format (sps/bps/align etc)");
                return -1;
            }

            return bytesRead / (int)step;
        }

        public int ReadToFloats(ref float[] data, uint num)
        {
            return ReadToFloats(ref data, num, 0);
        }

        public bool ReadToIntegers(ref int[] data)
        {
            if (bfReader == null || bfReader != null && !bfReader.CanRead)
            {
                Log.Error("File not opened");
                return false;
            }

            bfReader.Seek(wavHeader.dataPos, SeekOrigin.Begin);

            // --- 8-bit
            if (wavHeader.wBitsPerSample == 8)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        byte b = bfReader.ReadByte();
                        data[i * wavHeader.nChannels + j] = b;
                    }
                }
            }
            else
            // --- 16-bit
            if (wavHeader.wBitsPerSample == 16)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        short s = BitConverter.ToInt16(bfReader.ReadBytes(2), 0);
                        data[i * wavHeader.nChannels + j] = s;
                    }
                }
            }
            else
            // --- 24-bit (3 bytes per sample, 1 channel)
            if (wavHeader.wBitsPerSample == 24)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        byte[] bytes = bfReader.ReadBytes(3);
                        // Combine the three bytes into a 32-bit signed int
                        int l = (bytes[0] << 8) | (bytes[1] << 16) | (bytes[2] << 24);
                        // Since the original sample was stored as a 24-bit value, 
                        // the remaining 8 bits in the 32-bit integer will be filled with zeros. 
                        // Therefore, dividing the value by 256 (which is equivalent to shifting the value right by 8 bits) 
                        // effectively discards the extra zeros and converts the 32-bit value to a 24-bit value.
                        // l / 256 = l >> 8
                        data[i * wavHeader.nChannels + j] = l / 256;
                    }
                }
            }

            else
            // --- 32-bit
            if (wavHeader.wBitsPerSample == 32)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        int l = BitConverter.ToInt32(bfReader.ReadBytes(4), 0);
                        data[i * wavHeader.nChannels + j] = l;
                    }
                }
            }
            else
            {
                Log.Error("Wrong data format (sps/bps/align etc)");
                return false;
            }

            return true;
        }

        // Writing
        public bool SaveStandardWAVMulti(string filename, ref float[] data, uint chnkSize = 16)
        {
            if (!StartSaveBlocks(filename, chnkSize))
            {
                Log.Error("Failed saving standard wav multi");
                return false;
            }

            // --- 8 bit ---
            if (wavHeader.wBitsPerSample == 8)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        int sample = (int)Math.Round(data[i * wavHeader.nChannels + j] * MAX_8BIT + MAX_8BIT - 1);
                        fsWriter.WriteByte((byte)sample);
                    }
                }
            }
            // --- 16 bit ---
            else if (wavHeader.wBitsPerSample == 16)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        int sample = (int)Math.Round(data[i * wavHeader.nChannels + j] * MAX_16BIT);
                        fsWriter.Write(BitConverter.GetBytes((short)sample), 0, 2);
                    }
                }
            }
            // --- 24 bit ---
            else if (wavHeader.wBitsPerSample == 24)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        int sample = (int)Math.Round(data[i * wavHeader.nChannels + j] * MAX_24BIT);
                        // Since the original sample was stored as a 24-bit value, 
                        // the remaining 8 bits in the 32-bit integer will be filled with zeros. 
                        // Therefore, dividing the value by 256 (which is equivalent to shifting the value right by 8 bits) 
                        // effectively discards the extra zeros and converts the 32-bit value to a 24-bit value.
                        // l / 256 = l >> 8
                        sample /= 256;
                        byte[] bytes = BitConverter.GetBytes(sample);
                        fsWriter.Write(bytes, 0, 3);
                    }
                }
            }
            // --- 32 bit ---
            else if (wavHeader.wBitsPerSample == 32)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        if (wavHeader.wFormatTag == SoundIO.WAVE_FORMAT_PCM)
                        {
                            int sample = (int)Math.Round(data[i * wavHeader.nChannels + j] * MAX_32BIT);
                            fsWriter.Write(BitConverter.GetBytes(sample), 0, 4);
                        }
                        else if (wavHeader.wFormatTag == SoundIO.WAVE_FORMAT_IEEE_FLOAT)
                        {
                            float sample = data[i * wavHeader.nChannels + j];
                            fsWriter.Write(BitConverter.GetBytes(sample), 0, 4);
                        }
                    }
                }
            }

            CloseWav();

            return true;
        }

        public bool SaveWAVFromIntegers(string filename, ref int[] data, uint chnkSize = 16)
        {
            if (!StartSaveBlocks(filename, chnkSize))
            {
                Log.Error("Failed saving standard wav multi");
                return false;
            }

            if (doVerbose) Log.Debug("Writing {0} integers to output stream.", data.Length);

            // --- 8 bit ---
            if (wavHeader.wBitsPerSample == 8)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        int sample = data[i * wavHeader.nChannels + j];
                        fsWriter.WriteByte((byte)sample);
                    }
                }
            }
            // --- 16 bit ---
            else if (wavHeader.wBitsPerSample == 16)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        int sample = data[i * wavHeader.nChannels + j];
                        fsWriter.Write(BitConverter.GetBytes((short)sample), 0, 2);
                    }
                }
            }
            // --- 24 bit ---
            else if (wavHeader.wBitsPerSample == 24)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        int sample = data[i * wavHeader.nChannels + j];
                        fsWriter.Write(BitConverter.GetBytes(sample), 0, 3);
                    }
                }
            }
            // --- 32 bit ---
            else if (wavHeader.wBitsPerSample == 32)
            {
                for (int i = 0; i < wavHeader.numOfPoints; i++)
                {
                    for (int j = 0; j < wavHeader.nChannels; j++)
                    {
                        int sample = data[i * wavHeader.nChannels + j];
                        fsWriter.Write(BitConverter.GetBytes(sample), 0, 4);
                    }
                }
            }

            CloseWav();

            return true;
        }

        public bool StartSaveBlocks(string filename, uint chnkSize = 16)
        {
            if (File.Exists(filename))
            {
                if (doVerbose) Log.Debug("Found existing file. Deleting: " + filename);
                File.Delete(filename);
            }

            try
            {
                fsWriter = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            }
            catch (IOException e)
            {
                Log.Error("Can't open file: " + e);
                return false;
            }
            catch (Exception e)
            {
                Log.Error("Error while opening the file: " + e);
                return false;
            }

            return WriteHeader(chnkSize);
        }

        public bool WriteHeader(uint chnkSize = 16)
        {
            if (wavHeader.wFormatTag == SoundIO.WAVE_FORMAT_PCM
            || wavHeader.wFormatTag == SoundIO.WAVE_FORMAT_IEEE_FLOAT)
            {
                return WriteStandardHeader(chnkSize);
            }
            else if (wavHeader.wFormatTag == SoundIO.WAVE_FORMAT_EXTENSIBLE)
            {
                return WriteExtendedHeader();
            }
            else
            {
                return false;
            }
        }

        public bool WriteStandardHeader(uint chnkSize = 16)
        {
            // calculate extra bytes if chunk size is more tha 16
            // this will normally be zero
            uint extraChunkSizeBytes = chnkSize - 16;

            wavHeader.RIFFtag = "RIFF".ToCharArray();
            wavHeader.fileSize = wavHeader.dataSize + 44 - 8 + extraChunkSizeBytes;
            wavHeader.WAVEtag = "WAVE".ToCharArray();
            wavHeader.FMTtag = "fmt ".ToCharArray();
            wavHeader.chnkSize = chnkSize;
            wavHeader.extended = false;

            Log.Information(string.Format("Creating WAV standard file-header {0}[{1} hz, {2} bits, {3} ch, {4} samples]", wavHeader.chnkSize != 16 ? "(chunk size: " + wavHeader.chnkSize + ") " : "", wavHeader.nSamplesPerSec, wavHeader.wBitsPerSample, wavHeader.nChannels, wavHeader.numOfPoints));

            byte[] headerBytes = NCWParser.StructureToBytes(wavHeader);
            if (doVerbose) Log.Debug("Writing header bytes: " + StringUtils.ToHexAndAsciiString(headerBytes));
            fsWriter.Write(headerBytes, 0, 36);

            if (extraChunkSizeBytes > 0)
            {
                if (doVerbose) Log.Debug("Writing {0} extra chunk-size bytes.", extraChunkSizeBytes);
                for (int i = 0; i < extraChunkSizeBytes; i++)
                {
                    fsWriter.WriteByte(0);
                }
            }

            // --- Record data chunk
            byte[] dataChunkTag = Encoding.ASCII.GetBytes("data");
            fsWriter.Write(dataChunkTag, 0, 4);
            fsWriter.Write(BitConverter.GetBytes(wavHeader.dataSize), 0, 4);

            return true;
        }

        public bool WriteExtendedHeader()
        {
            wavHeader.RIFFtag = "RIFF".ToCharArray();
            wavHeader.fileSize = wavHeader.dataSize + 60;
            wavHeader.WAVEtag = "WAVE".ToCharArray();
            wavHeader.FMTtag = "fmt ".ToCharArray();
            wavHeader.chnkSize = 40;
            wavHeader.cbSize = 0;
            wavHeader.realBps = wavHeader.wBitsPerSample;
            wavHeader.speakers = 0;
            wavHeader.GUID = WAV_TEST_GUID;
            wavHeader.extended = true;

            Log.Information(string.Format("Creating WAV extended file-header (chunk size {0}) [{1} hz, {2} bits, {3} ch, {4} samples]", wavHeader.chnkSize, wavHeader.nSamplesPerSec, wavHeader.wBitsPerSample, wavHeader.nChannels, wavHeader.numOfPoints));

            byte[] headerBytes = NCWParser.StructureToBytes(wavHeader);
            if (doVerbose) Log.Debug("Writing header bytes: " + StringUtils.ToHexAndAsciiString(headerBytes));
            fsWriter.Write(headerBytes, 0, 60);

            // --- Record data chunk
            byte[] head = Encoding.ASCII.GetBytes("data");
            fsWriter.Write(head, 0, 4);
            fsWriter.Write(BitConverter.GetBytes(wavHeader.dataSize), 0, 4);

            return true;
        }

        public void WriteBlock(Span<byte> source, int count)
        {
            if (doVerbose) Log.Debug("Writing {0} bytes to output stream.", count);

            if (fsWriter != null)
            {
                fsWriter.Write(source.Slice(0, count));
            }
        }

        public void CloseWav()
        {
            if (fsWriter != null)
            {
                if (doVerbose) Log.Debug("Closing output stream.");

                fsWriter.Close();
                fsWriter.Dispose();
            }
        }

    }
}