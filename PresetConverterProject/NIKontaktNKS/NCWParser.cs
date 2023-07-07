using System.Runtime.InteropServices;
using CommonUtils.Audio;
using Serilog;
using static PresetConverterProject.NIKontaktNKS.BitProcess;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class NCW
    {
        public enum WavType
        {
            Standard,
            Extended,
            Auto
        }

        public static bool NCW2Wav(string inputFilePath, string outputDirectoryPath, bool doList, bool doVerbose)
        {
            // // read wave file
            // var wp = new WAVParser();
            // var wavInPath = "C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB.wav";
            // // var wavInPath = "C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - u8bit.wav";
            // // var wavInPath = "C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 16bit.wav";
            // // var wavInPath = "C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 32bit.wav";
            // // var wavInPath = "C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 24bit.wav";
            // // var wavInPath = "C:\\Users\\periv\\OneDrive\\DevProjects\\Native Instruments GmbH\\Instruments\\viola_sus_short-portato_64-127_E4 - AB Samples\\viola_sus_short-portato_64-127_E4 - AB - 32bit float.wav";
            // wp.OpenWav(wavInPath);

            // // ints
            // int[] ints = new int[wp.WavHeader.numOfPoints * wp.WavHeader.nChannels];
            // wp.ReadToIntegers(ints);
            // var wavOutPathInts = "C:\\Users\\periv\\Projects\\Temp\\viola_sus_short-portato_64-127_E4 - AB - ints.wav";
            // wp.SaveWAVFromIntegers(wavOutPathInts, ints);

            // // floats
            // float[] floats = new float[wp.WavHeader.numOfPoints * wp.WavHeader.nChannels];
            // wp.ReadToFloats(ref floats, (uint)wp.WavHeader.numOfPoints);
            // var wavOutPathFloats = "C:\\Users\\periv\\Projects\\Temp\\viola_sus_short-portato_64-127_E4 - AB - floats.wav";
            // wp.SaveStandardWAVMulti(wavOutPathFloats, ref floats);

            // Convert file
            var ncwParser = new NCWParser();
            ncwParser.Clear();
            ncwParser.OpenNCWFile(inputFilePath);
            ncwParser.ReadNCW();

            // test using integers
            // ncwParser.ReadNCWIntegers();
            // string outputFileNameInt = Path.GetFileNameWithoutExtension(inputFilePath) + "_ints.wav";
            // string outputFilePathInt = Path.Combine(outputDirectoryPath, outputFileNameInt);
            // Log.Information("Writing file {0} ...", outputFilePathInt);
            // ncwParser.SaveToWAVIntegers(outputFilePathInt);

            // Output file to wav
            var wavtype = WavType.Standard;

            string outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".wav";
            string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName);
            Log.Information("Writing file {0} ...", outputFilePath);

            if (wavtype == WavType.Standard)
            {
                ncwParser.SaveToWAV(outputFilePath);
            }
            else if (wavtype == WavType.Extended)
            {
                ncwParser.SaveToWAVEx(outputFilePath);
            }
            else
            {
                if (ncwParser.Header.Channels > 2 ||
                    ncwParser.Header.Bits > 16 ||
                    ncwParser.Header.SampleRate > 44100)
                {
                    ncwParser.SaveToWAVEx(outputFilePath);
                }
                else
                {
                    ncwParser.SaveToWAV(outputFilePath);
                }
            }

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
            Log.Information("Writing file {0} ...", outputFilePathNCW24);
            ncwParser.WriteNCW24(wavHeader);
            ncwParser.SaveToNCW(outputFilePathNCW24);

            return true;
        }
    }

    public class NCWParser
    {
        public static readonly byte[] NCW_SIGNATURE1 = new byte[] { 0x01, 0xA8, 0x9E, 0xD6, 0x31, 0x01, 0x00, 0x00 };
        public static readonly byte[] NCW_SIGNATURE2 = new byte[] { 0x01, 0xA8, 0x9E, 0xD6, 0x30, 0x01, 0x00, 0x00 };
        public static readonly byte[] BLOCK_SIGNATURE = new byte[] { 0x16, 0x0C, 0x9A, 0x3E };

        public const int NCW_SAMPLES = 512;
        public const int MAX_CHANNELS = 6;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TNCWHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Signature;
            public ushort Channels;
            public ushort Bits;
            public uint SampleRate;
            public uint NumSamples;
            public uint BlockDefOffset;
            public uint BlocksOffset;
            public uint BlocksSize;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 88)]
            public byte[] SomeData;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TBlockHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Signature;
            public int BaseValue;
            public short Bits;
            public ushort Flags;
            public uint Zeros2;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct TChunkHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] Tag;
            public uint Size;
        }

        private TNCWHeader header;
        public TNCWHeader Header { get => header; set => header = value; }

        private int[]? blocksDefArray;
        private int[]? datai;
        private sbyte[]? data8;
        private short[]? data16;
        private byte[,]? data24; // x 3

        private FileStream? fs;
        private MemoryStream? ms;

        public void Clear()
        {
            CloseFile();

            datai = Array.Empty<int>();
            data8 = Array.Empty<sbyte>();
            data16 = Array.Empty<short>();
            data24 = new byte[0, 0];
        }

        public void CloseFile()
        {
            if (fs != null)
            {
                fs.Close();
                fs.Dispose();
                fs = null;
            }

            blocksDefArray = Array.Empty<int>();
        }

        public void SaveToWAV(string filename)
        {
            WAVParser.TMyWAVHeader wavHeader = new();
            wavHeader.wFormatTag = SoundIO.WAVE_FORMAT_PCM; // Standard wav
            wavHeader.nChannels = header.Channels;
            wavHeader.nSamplesPerSec = header.SampleRate;
            wavHeader.wBitsPerSample = header.Bits;
            wavHeader.nBlockAlign = (ushort)(wavHeader.nChannels * wavHeader.wBitsPerSample / 8);
            wavHeader.nAvgBytesPerSec = wavHeader.nSamplesPerSec * wavHeader.nBlockAlign;
            wavHeader.cbSize = 0;
            wavHeader.dataSize = wavHeader.nBlockAlign * header.NumSamples;
            wavHeader.numOfPoints = (int)header.NumSamples;
            wavHeader.dataPos = 44;

            WAVParser wp = new()
            {
                WavHeader = wavHeader
            };

            // use chnkSize = 20
            wp.StartSaveBlocks(filename, 20);

            byte[] buf;
            switch (header.Bits)
            {
                case 8:
                    buf = new byte[(int)header.NumSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data8, 0), buf, 0, (int)header.NumSamples * header.Channels);
                    break;
                case 16:
                    buf = new byte[2 * (int)header.NumSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data16, 0), buf, 0, 2 * (int)header.NumSamples * header.Channels);
                    break;
                case 24:
                    buf = new byte[3 * (int)header.NumSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data24, 0), buf, 0, 3 * (int)header.NumSamples * header.Channels);
                    break;
                case 32:
                    buf = new byte[4 * (int)header.NumSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(datai, 0), buf, 0, 4 * (int)header.NumSamples * header.Channels);
                    break;
                default:
                    throw new Exception("NCWPARSER.SaveToWav: Unsupported BitsPerSample");
            }

            // int block_size = 1024;
            // int nblocks = (int)wavHeader.dataSize / block_size;
            // int nrem = (int)wavHeader.dataSize - nblocks * block_size;

            // for (int i = 0; i < nblocks; i++)
            // {
            //     wp.WriteBlock(buf, block_size);
            //     buf = buf.Skip(block_size).ToArray();
            // }

            // if (nrem != 0)
            // {
            //     wp.WriteBlock(buf, nrem);
            // }

            // write everything in one go
            wp.WriteBlock(buf, buf.Length);

            wp.CloseWav();
        }

        public void SaveToWAVEx(string filename)
        {
            WAVParser.TMyWAVHeader wavHeader = new();
            wavHeader.wFormatTag = SoundIO.WAVE_FORMAT_EXTENSIBLE; // Extended wav
            wavHeader.nChannels = header.Channels;
            wavHeader.nSamplesPerSec = header.SampleRate;
            wavHeader.wBitsPerSample = header.Bits;
            wavHeader.nBlockAlign = (ushort)(wavHeader.nChannels * wavHeader.wBitsPerSample / 8);
            wavHeader.nAvgBytesPerSec = wavHeader.nSamplesPerSec * wavHeader.nBlockAlign;
            wavHeader.cbSize = 0;
            wavHeader.dataSize = wavHeader.nBlockAlign * header.NumSamples;
            wavHeader.numOfPoints = (int)header.NumSamples;
            wavHeader.dataPos = 44;
            wavHeader.cbSize = 0;
            wavHeader.realBps = header.Bits;
            wavHeader.speakers = 0;
            wavHeader.GUID = WAVParser.WAV_TEST_GUID;

            WAVParser wp = new()
            {
                WavHeader = wavHeader
            };
            wp.StartSaveBlocks(filename);

            byte[] buf;
            switch (header.Bits)
            {
                case 8:
                    buf = new byte[(int)header.NumSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data8, 0), buf, 0, (int)header.NumSamples * header.Channels);
                    break;
                case 16:
                    buf = new byte[2 * (int)header.NumSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data16, 0), buf, 0, 2 * (int)header.NumSamples * header.Channels);
                    break;
                case 24:
                    buf = new byte[3 * (int)header.NumSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data24, 0), buf, 0, 3 * (int)header.NumSamples * header.Channels);
                    break;
                case 32:
                    buf = new byte[4 * (int)header.NumSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(datai, 0), buf, 0, 4 * (int)header.NumSamples * header.Channels);
                    break;
                default:
                    throw new Exception("NCWPARSER.SaveToWav: Unsupported BitsPerSample");
            }

            // int block_size = 1024;
            // int nblocks = (int)wavHeader.dataSize / block_size;
            // int nrem = (int)wavHeader.dataSize - nblocks * block_size;

            // for (int i = 0; i < nblocks; i++)
            // {
            //     wp.WriteBlock(buf, block_size);
            //     buf = buf.Skip(block_size).ToArray();
            // }

            // if (nrem != 0)
            // {
            //     wp.WriteBlock(buf, nrem);
            // }

            // write everything in one go
            wp.WriteBlock(buf, buf.Length);

            wp.CloseWav();
        }

        public void SaveToWAVIntegers(string filename)
        {
            WAVParser.TMyWAVHeader wavHeader = new();
            wavHeader.wFormatTag = SoundIO.WAVE_FORMAT_PCM; // Standard wav
            wavHeader.nChannels = header.Channels;
            wavHeader.nSamplesPerSec = header.SampleRate;
            wavHeader.wBitsPerSample = header.Bits;
            wavHeader.nBlockAlign = (ushort)(wavHeader.nChannels * wavHeader.wBitsPerSample / 8);
            wavHeader.nAvgBytesPerSec = wavHeader.nSamplesPerSec * wavHeader.nBlockAlign;
            wavHeader.cbSize = 0;
            wavHeader.dataSize = wavHeader.nBlockAlign * header.NumSamples;
            wavHeader.numOfPoints = (int)header.NumSamples;
            wavHeader.dataPos = 44;

            WAVParser wp = new()
            {
                WavHeader = wavHeader
            };

            // use chnkSize = 20
            wp.SaveWAVFromIntegers(filename, ref datai, 20);
        }

        public void OpenNCWFile(string filename)
        {
            try
            {
                fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch
            {
                throw new Exception("Can't open file");
            }

            byte[] headerBytes = new byte[Marshal.SizeOf(typeof(TNCWHeader))];
            long position = fs.Position;
            fs.Read(headerBytes, 0, headerBytes.Length);
            header = ByteArrayToStructure<TNCWHeader>(headerBytes);

            Log.Information(string.Format("Found file-header @ position {0} [{1} hz, {2} bits, {3} ch, {4} samples]", position, header.SampleRate, header.Bits, header.Channels, header.NumSamples));
            Log.Debug("BlockDefOffset: " + header.BlockDefOffset);
            Log.Debug("BlocksOffset: " + header.BlocksOffset);
            Log.Debug("BlocksSize: " + header.BlocksSize);

            // check if matches either ncw signature 1 or 2
            for (int i = 0; i < 8; i++)
            {
                if ((header.Signature[i] != NCW_SIGNATURE1[i]) && (header.Signature[i] != NCW_SIGNATURE2[i]))
                    throw new Exception("Wrong file signature");
            }

            blocksDefArray = new int[(header.BlocksOffset - header.BlockDefOffset) / 4];
            fs.Seek(header.BlockDefOffset, SeekOrigin.Begin);
            byte[] blocksDefListBytes = new byte[blocksDefArray.Length * 4];
            fs.Read(blocksDefListBytes, 0, blocksDefListBytes.Length);
            Buffer.BlockCopy(blocksDefListBytes, 0, blocksDefArray, 0, blocksDefListBytes.Length);
        }

        public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[] StructureToBytes<T>(T structure) where T : struct
        {
            int size = Marshal.SizeOf(structure);
            byte[] bytes = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structure, ptr, true);
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
            return bytes;
        }

        public static byte[] ReadBytes(FileStream fs, int count)
        {
            byte[] bytes = new byte[count];
            fs.Read(bytes, 0, count);
            return bytes;
        }

        public static IntPtr GetIntPtr(Array array, int index)
        {
            GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                return Marshal.UnsafeAddrOfPinnedArrayElement(array, index * array.GetLength(1));
            }
            finally
            {
                handle.Free();
            }
        }

        public void ReadNCW8()
        {
            Log.Information("Reading NCW 8 bit...");

            data8 = new sbyte[header.NumSamples * header.Channels];

            int curOffset = 0;
            uint curSample = 0;

            int[][] temp8 = new int[MAX_CHANNELS][];

            for (int i = 0; i < blocksDefArray.Length - 1; i++)
            {
                Log.Debug(string.Format("Processing block {0}/{1}...", i + 1, blocksDefArray.Length));

                fs.Seek(header.BlocksOffset + blocksDefArray[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    Log.Debug(string.Format("Processing channel {0} @ block {1}...", j + 1, i + 1));

                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    Log.Debug(string.Format("Found block-header @ position {0} [base: {1}, bits: {2}, flags: {3} = {4}]", position, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        int nbits = Math.Abs(bHeader.Bits);
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        temp8[j] = new int[NCW_SAMPLES];
                        BitProcess.Fill8(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp8[j], false);
                    }
                    else
                    {
                        int nbits = (bHeader.Bits == 0) ? header.Bits : bHeader.Bits;
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        temp8[j] = new int[NCW_SAMPLES];
                        bool nrelative = bHeader.Bits != 0;
                        BitProcess.Fill8(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp8[j], nrelative);
                    }
                }

                if (bHeader.Flags == 1)
                {
                    // Data stored in the block are MID/SIDE
                    Log.Debug("Processing MID/SIDE data. Converting to LEFT/RIGHT...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        data8[curOffset] = (sbyte)(temp8[0][k] + temp8[1][k]);
                        curOffset++;
                        data8[curOffset] = (sbyte)(temp8[0][k] - temp8[1][k]);
                        curOffset++;
                        curSample++;


                        if (curSample >= header.NumSamples)
                            break;
                    }
                }
                else
                {
                    // Data stored in the block are LEFT/RIGHT
                    Log.Debug("Processing LEFT/RIGHT data...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            data8[curOffset] = (sbyte)temp8[j][k];
                            curOffset++;
                        }
                        curSample++;

                        if (curSample >= header.NumSamples)
                            break;
                    }
                }

                Log.Debug(string.Format("Processed block {0}. [CurSample: {1}, CurOffset: {2}, BytePos: {3}]", i + 1, curSample, curOffset, curOffset * 1));
            }
        }

        public void ReadNCW16()
        {
            Log.Information("Reading NCW 16 bit...");

            data16 = new short[header.NumSamples * header.Channels];

            int curOffset = 0;
            uint curSample = 0;

            int[][] temp16 = new int[MAX_CHANNELS][];

            for (int i = 0; i < blocksDefArray.Length - 1; i++)
            {
                Log.Debug(string.Format("Processing block {0}/{1}...", i + 1, blocksDefArray.Length));

                fs.Seek(header.BlocksOffset + blocksDefArray[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    Log.Debug(string.Format("Processing channel {0} @ block {1}...", j + 1, i + 1));

                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    Log.Debug(string.Format("Found block-header @ position {0} [base: {1}, bits: {2}, flags: {3} = {4}]", position, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        int nbits = Math.Abs(bHeader.Bits);
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        temp16[j] = new int[NCW_SAMPLES];
                        BitProcess.Fill16(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp16[j], false);
                    }
                    else
                    {
                        int nbits = (bHeader.Bits == 0) ? header.Bits : bHeader.Bits;
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        temp16[j] = new int[NCW_SAMPLES];
                        bool nrelative = bHeader.Bits != 0;
                        BitProcess.Fill16(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp16[j], nrelative);
                    }
                }

                if (bHeader.Flags == 1)
                {
                    // Data stored in the block are MID/SIDE
                    Log.Debug("Processing MID/SIDE data. Converting to LEFT/RIGHT...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        data16[curOffset] = (short)(temp16[0][k] + temp16[1][k]);
                        curOffset++;
                        data16[curOffset] = (short)(temp16[0][k] - temp16[1][k]);
                        curOffset++;
                        curSample++;


                        if (curSample >= header.NumSamples)
                            break;
                    }
                }
                else
                {
                    // Data stored in the block are LEFT/RIGHT
                    Log.Debug("Processing LEFT/RIGHT data...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            data16[curOffset] = (short)temp16[j][k];
                            curOffset++;
                        }
                        curSample++;

                        if (curSample >= header.NumSamples)
                            break;
                    }
                }

                Log.Debug(string.Format("Processed block {0}. [CurSample: {1}, CurOffset: {2}, BytePos: {3}]", i + 1, curSample, curOffset, curOffset * 2));
            }
        }

        public void ReadNCW24()
        {
            Log.Information("Reading NCW 24 bit...");

            data24 = new byte[header.NumSamples * header.Channels, 3];

            int curOffset = 0;
            uint curSample = 0;

            int[][] temp24 = new int[MAX_CHANNELS][];

            for (int i = 0; i < blocksDefArray.Length - 1; i++)
            {
                Log.Debug(string.Format("Processing block {0}/{1}...", i + 1, blocksDefArray.Length));

                fs.Seek(header.BlocksOffset + blocksDefArray[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    Log.Debug(string.Format("Processing channel {0} @ block {1}...", j + 1, i + 1));

                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    Log.Debug(string.Format("Found block-header @ position {0} [base: {1}, bits: {2}, flags: {3} = {4}]", position, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        // If 'Bits' < 0 then compression is used. 
                        // You get actual bits precision by taking absolute value of 'Bits'. 
                        // This is used for 8, 16, 24, and 32 bits.
                        int nbits = Math.Abs(bHeader.Bits);

                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        temp24[j] = new int[NCW_SAMPLES * 3];
                        BitProcess.Fill24(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp24[j], false);
                    }

                    else
                    {
                        // If 'Bits' = 0 then there's no compression. 
                        // You have to use the number of bits from the main header ('Bits' field). 
                        // In other words, bits precision is the same as the original WAV file had.
                        int nbits = (bHeader.Bits == 0) ? header.Bits : bHeader.Bits;

                        // If 'Bits' > 0 then data stored in the block are not sample values, but deltas (differences)
                        bool nrelative = bHeader.Bits != 0;

                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        temp24[j] = new int[NCW_SAMPLES * 3];
                        BitProcess.Fill24(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp24[j], nrelative);
                    }
                }

                // Considering channels, for stereo audio data stored like this: 
                // first, you have all blocks for one channel, 
                // then you have all blocks for the second channel. 
                // This is different from typical audio file formats, 
                // where they try to keep channels data interleaving. 
                // Second, if 'Flags' value from the block header equals 1 ('Flags'=1) 
                // then data stored in the block are not LEFT/RIGHT, but MID/SIDE. 
                if (bHeader.Flags == 1)
                {
                    // Data stored in the block are MID/SIDE
                    Log.Debug("Processing MID/SIDE data. Converting to LEFT/RIGHT...");
                    // MID = (LEFT+RIGHT)/2, SIDE = (LEFT-RIGHT)/2. 
                    // To get LEFT/RIGHT channels, you have to reverse the operation: 
                    // LEFT = MID+SIDE, RIGHT = MID-SIDE.

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        int ti1 = (temp24[0][k * 3] + (temp24[0][k * 3 + 1] << 8) + (temp24[0][k * 3 + 2] << 16)) << 8;
                        int ti2 = (temp24[1][k * 3] + (temp24[1][k * 3 + 1] << 8) + (temp24[1][k * 3 + 2] << 16)) << 8;
                        int ti3 = ti1 + ti2;

                        data24[curOffset, 0] = (byte)((ti3 >> 8) & 0xFF);
                        data24[curOffset, 1] = (byte)((ti3 >> 16) & 0xFF);
                        data24[curOffset, 2] = (byte)((ti3 >> 24) & 0xFF);
                        curOffset++;

                        ti1 = (temp24[0][k * 3] + (temp24[0][k * 3 + 1] << 8) + (temp24[0][k * 3 + 2] << 16)) << 8;
                        ti2 = (temp24[1][k * 3] + (temp24[1][k * 3 + 1] << 8) + (temp24[1][k * 3 + 2] << 16)) << 8;
                        ti3 = ti1 - ti2;

                        data24[curOffset, 0] = (byte)((ti3 >> 8) & 0xFF);
                        data24[curOffset, 1] = (byte)((ti3 >> 16) & 0xFF);
                        data24[curOffset, 2] = (byte)((ti3 >> 24) & 0xFF);
                        curOffset++;
                        curSample++;

                        if (curSample >= header.NumSamples)
                            break;
                    }
                }
                else
                {
                    // Data stored in the block are LEFT/RIGHT
                    Log.Debug("Processing LEFT/RIGHT data...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            data24[curOffset, 0] = (byte)temp24[j][k * 3];
                            data24[curOffset, 1] = (byte)temp24[j][k * 3 + 1];
                            data24[curOffset, 2] = (byte)temp24[j][k * 3 + 2];
                            curOffset++;
                        }
                        curSample++;

                        if (curSample >= header.NumSamples)
                            break;
                    }
                }

                Log.Debug(string.Format("Processed block {0}. [CurSample: {1}, CurOffset: {2}, BytePos: {3}]", i + 1, curSample, curOffset, curOffset * 3));
            }
        }

        public void ReadNCW32()
        {
            Log.Information("Reading NCW 32 bit...");

            datai = new int[header.NumSamples * header.Channels];

            int curOffset = 0;
            uint curSample = 0;

            int[][] temp32 = new int[MAX_CHANNELS][];

            for (int i = 0; i < blocksDefArray.Length - 1; i++)
            {
                Log.Debug(string.Format("Processing block {0}/{1}...", i + 1, blocksDefArray.Length));

                fs.Seek(header.BlocksOffset + blocksDefArray[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    Log.Debug(string.Format("Processing channel {0} @ block {1}...", j + 1, i + 1));

                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    Log.Debug(string.Format("Found block-header @ position {0} [base: {1}, bits: {2}, flags: {3} = {4}]", position, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        int nbits = Math.Abs(bHeader.Bits);
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        temp32[j] = new int[NCW_SAMPLES];
                        BitProcess.Fill32(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp32[j], false);
                    }
                    else
                    {
                        int nbits = (bHeader.Bits == 0) ? header.Bits : bHeader.Bits;
                        bool nrelative = bHeader.Bits != 0;
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        temp32[j] = new int[NCW_SAMPLES];
                        BitProcess.Fill32(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp32[j], nrelative);
                    }
                }

                if (bHeader.Flags == 1)
                {
                    // Data stored in the block are MID/SIDE
                    Log.Debug("Processing MID/SIDE data. Converting to LEFT/RIGHT...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        datai[curOffset] = temp32[0][k] + temp32[1][k];
                        curOffset++;
                        datai[curOffset] = temp32[0][k] - temp32[1][k];
                        curOffset++;
                        curSample++;

                        if (curSample >= header.NumSamples)
                            break;
                    }
                }
                else
                {
                    // Data stored in the block are LEFT/RIGHT
                    Log.Debug("Processing LEFT/RIGHT data...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            datai[curOffset] = temp32[j][k];
                            curOffset++;
                        }
                        curSample++;

                        if (curSample >= header.NumSamples)
                            break;
                    }
                }

                Log.Debug(string.Format("Processed block {0}. [CurSample: {1}, CurOffset: {2}, BytePos: {3}]", i + 1, curSample, curOffset, curOffset * 4));
            }
        }

        public void ReadNCW()
        {
            switch (header.Bits)
            {
                case 8:
                    ReadNCW8();
                    break;
                case 16:
                    ReadNCW16();
                    break;
                case 24:
                    ReadNCW24();
                    break;
                case 32:
                    ReadNCW32();
                    break;
                default:
                    throw new Exception("NCWPARSER.ReadNCW: Unsupported BitsPerSample");
            }
        }

        public void ReadNCWIntegers()
        {
            Log.Information("Reading NCW {0} bit integers ...", header.Bits);

            datai = new int[header.NumSamples * header.Channels];

            int curOffset = 0;
            uint curSample = 0;

            int[][] tempi = new int[MAX_CHANNELS][];

            for (int i = 0; i < blocksDefArray.Length - 1; i++)
            {
                Log.Debug(string.Format("Processing block {0}/{1}...", i + 1, blocksDefArray.Length));

                fs.Seek(header.BlocksOffset + blocksDefArray[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    Log.Debug(string.Format("Processing channel {0} @ block {1}...", j + 1, i + 1));

                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    Log.Debug(string.Format("Found block-header @ position {0} [base: {1}, bits: {2}, flags: {3} = {4}]", position, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        int nbits = Math.Abs(bHeader.Bits);
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        tempi[j] = new int[NCW_SAMPLES];
                        BitProcess.FillIntegersAbs(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, tempi[j]);
                    }

                    else
                    {
                        int nbits = (bHeader.Bits == 0) ? header.Bits : bHeader.Bits;
                        bool nrelative = bHeader.Bits != 0;
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        tempi[j] = new int[NCW_SAMPLES];
                        BitProcess.FillIntegers(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, tempi[j], nrelative);
                    }
                }

                if (bHeader.Flags == 1)
                {
                    // Data stored in the block are MID/SIDE
                    Log.Debug("Processing MID/SIDE data. Converting to LEFT/RIGHT...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        datai[curOffset] = tempi[0][k] + tempi[1][k];
                        curOffset++;
                        datai[curOffset] = tempi[0][k] - tempi[1][k];
                        curOffset++;
                        curSample++;

                        if (curSample >= header.NumSamples)
                            break;
                    }
                }
                else
                {
                    // Data stored in the block are LEFT/RIGHT
                    Log.Debug("Processing LEFT/RIGHT data...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            datai[curOffset] = tempi[j][k];
                            curOffset++;
                        }
                        curSample++;

                        if (curSample >= header.NumSamples)
                            break;
                    }
                }

                Log.Debug(string.Format("Processed block {0}. [CurSample: {1}, CurOffset: {2}, BytePos: {3}]", i + 1, curSample, curOffset, curOffset * 4));
            }
        }

        public void SaveToNCW(string filename)
        {
            using (fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                byte[] headerBytes = StructureToBytes(header);
                // fs.Write(headerBytes, 0, Marshal.SizeOf(typeof(TNCWHeader)));
                fs.Write(headerBytes);

                foreach (int blockDef in blocksDefArray)
                {
                    byte[] blockDefBytes = BitConverter.GetBytes(blockDef);
                    fs.Write(blockDefBytes, 0, blockDefBytes.Length);
                }

                // write everything in MemoryStream
                ms.Position = 0; // You have to rewind the MemoryStream before copying
                ms.CopyTo(fs);
                fs.Flush();
            }
        }

        public void WriteNCW24(WAVParser.TMyWAVHeader wavHeader)
        {
            // Fill header
            header = new TNCWHeader()
            {
                Signature = NCW_SIGNATURE1,
                Channels = wavHeader.nChannels,
                Bits = wavHeader.wBitsPerSample,
                SampleRate = wavHeader.nSamplesPerSec,
                NumSamples = (uint)wavHeader.numOfPoints,
                SomeData = new byte[] {
                    0x00, 0x00, 0x00, 0x00, 0x3E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x42, 0x01, 0x00, 0x00, 0x04, 0x01, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00
                },
                BlockDefOffset = 120 // 0x78
            };

            int nblocks = wavHeader.numOfPoints / 512 + 1;
            if (wavHeader.numOfPoints % 512 != 0)
                nblocks++;

            Log.Debug("Processing {0} number of blocks.", nblocks);

            header.BlocksOffset = header.BlockDefOffset + (uint)nblocks * 4;
            Log.Debug("Setting initial BlocksOffset: " + header.BlocksOffset);

            ms = new MemoryStream();

            List<int> blocksDefList = new();
            int curBlockOffset = 0;

            // initialize temporary arrays
            Int24[][] temp24 = new Int24[header.Channels][];
            Int24[][] temp24dif = new Int24[header.Channels][];
            byte[] tempB = new byte[header.Bits * 64 * 3];

            for (int j = 0; j < header.Channels; j++)
            {
                temp24[j] = new Int24[NCW_SAMPLES];
                temp24dif[j] = new Int24[NCW_SAMPLES];
            }

            for (int curBlockNumber = 0; curBlockNumber < nblocks - 1; curBlockNumber++)
            {
                Log.Debug(string.Format("Processing block {0}/{1} at offset: {2}.", curBlockNumber + 1, nblocks, curBlockOffset));

                blocksDefList.Add(curBlockOffset);

                // Fill 512 samples arrays
                for (int i = 0; i < NCW_SAMPLES; i++)
                {
                    for (int j = 0; j < header.Channels; j++)
                    {
                        int curIndex = curBlockNumber * 512 * header.Channels + i * header.Channels + j;
                        if (curIndex < data24.GetLength(0))
                        {
                            temp24[j][i] = new Int24(data24[curIndex, 0], data24[curIndex, 1], data24[curIndex, 2]);
                        }
                        else
                        {
                            temp24[j][i] = Int24.Zero;
                        }
                    }
                }

                for (int j = 0; j < header.Channels; j++)
                {
                    DifArray24(temp24[j], temp24dif[j], out int max, out int min);
                    int nbits = Math.Max(MinBits(min), MinBits(max));

                    TBlockHeader bHeader = new();
                    FillBlockHeader(ref bHeader);

                    bHeader.BaseValue = temp24[j][0];

                    if (nbits >= header.Bits)
                    {
                        bHeader.Bits = (short)-header.Bits;
                        nbits = header.Bits;
                    }
                    else
                    {
                        bHeader.Bits = (short)nbits;
                    }

                    int blockSize = nbits * 64;

                    if (bHeader.Bits < 0)
                    {
                        BitProcess.Encode_24(NCW_SAMPLES, nbits, temp24[j], tempB);
                    }
                    else
                    {
                        BitProcess.Encode_24(NCW_SAMPLES, nbits, temp24dif[j], tempB);
                    }

                    byte[] bHeaderBytes = StructureToBytes(bHeader);
                    ms.Write(bHeaderBytes);

                    // add bytes according to blockSize
                    ms.Write(tempB, 0, blockSize);

                    curBlockOffset += bHeaderBytes.Length + blockSize;
                }
            }

            blocksDefList.Add(curBlockOffset);
            blocksDefArray = blocksDefList.ToArray();

            header.BlocksSize = (uint)curBlockOffset;
            header.BlocksOffset = (uint)(Marshal.SizeOf(header) + blocksDefList.Count * 4);

            Log.Information(string.Format("Creating file-header [{0} hz, {1} bits, {2} ch, {3} samples]", header.SampleRate, header.Bits, header.Channels, header.NumSamples));
            Log.Debug("BlockDefOffset: " + header.BlockDefOffset);
            Log.Debug("BlocksOffset: " + header.BlocksOffset);
            Log.Debug("BlocksSize: " + header.BlocksSize);
        }

        private void WriteNCW32(WAVParser.TMyWAVHeader wavHeader)
        {
            // Fill header
            header = new TNCWHeader()
            {
                Signature = NCW_SIGNATURE1,
                Channels = wavHeader.nChannels,
                Bits = wavHeader.wBitsPerSample,
                SampleRate = wavHeader.nSamplesPerSec,
                NumSamples = (uint)wavHeader.numOfPoints,
                SomeData = new byte[88],
                BlockDefOffset = 120 // 0x78
            };

            int nblocks = wavHeader.numOfPoints / 512 + 1;
            if (wavHeader.numOfPoints % 512 != 0)
                nblocks++;

            header.BlocksOffset = header.BlockDefOffset + (uint)nblocks * 4;

            ms = new MemoryStream();

            List<int> blocksDefList = new();
            int curBlockOffset = 0;

            int[][] temp32 = new int[header.Channels][];
            int[][] temp32dif = new int[header.Channels][];
            byte[] tempB = new byte[header.Bits * 64 * 4];

            for (int j = 0; j < header.Channels; j++)
            {
                temp32[j] = new int[NCW_SAMPLES];
                temp32dif[j] = new int[NCW_SAMPLES];
            }

            for (int curBlockNumber = 0; curBlockNumber < nblocks - 1; curBlockNumber++)
            {
                blocksDefList.Add(curBlockOffset);

                // Fill 512 samples arrays
                for (int i = 0; i < NCW_SAMPLES; i++)
                {
                    for (int j = 0; j < header.Channels; j++)
                    {
                        if (curBlockNumber * 512 * header.Channels + i * header.Channels + j < datai.Length)
                        {
                            temp32[j][i] = datai[curBlockNumber * 512 * header.Channels + i * header.Channels + j];
                        }
                        else
                        {
                            temp32[j][i] = 0;
                        }
                    }
                }

                for (int j = 0; j < header.Channels; j++)
                {
                    DifArray32(temp32[j], temp32dif[j], out int max, out int min);
                    int nbits = Math.Max(MinBits(min), MinBits(max));

                    TBlockHeader bHeader = new();
                    FillBlockHeader(ref bHeader);
                    bHeader.BaseValue = temp32[j][0];

                    if (nbits >= header.Bits)
                    {
                        bHeader.Bits = (short)-header.Bits;
                        nbits = header.Bits;
                    }
                    else
                    {
                        bHeader.Bits = (short)nbits;
                    }

                    int blockSize = nbits * 64;

                    if (bHeader.Bits < 0)
                    {
                        BitProcess.Encode_32(NCW_SAMPLES, nbits, temp32[j], tempB);
                    }
                    else
                    {
                        BitProcess.Encode_32(NCW_SAMPLES, nbits, temp32dif[j], tempB);
                    }

                    // ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref bHeader, 1)));
                    byte[] bHeaderBytes = StructureToBytes(bHeader);
                    ms.Write(bHeaderBytes);

                    ms.Write(tempB.AsSpan().Slice(0, blockSize));

                    // curBlockOffset += Marshal.SizeOf(bHeader) + blockSize;
                    curBlockOffset += bHeaderBytes.Length + blockSize;
                }
            }

            blocksDefList.Add(curBlockOffset);
            blocksDefArray = blocksDefList.ToArray();

            header.BlocksSize = (uint)curBlockOffset;
            header.BlocksOffset = (uint)(Marshal.SizeOf(header) + blocksDefList.Count * 4);
        }

        private void FillBlockHeader(ref TBlockHeader bheader)
        {
            bheader.Signature = BLOCK_SIGNATURE;
            bheader.Flags = 0;
            bheader.Zeros2 = 0;
        }

        /// <summary>
        /// Calculate the minimum number of bits required to represent the absolute value of an integer.
        /// </summary>
        /// <param name="x">integer</param>
        /// <returns>minimum number of bits required</returns>
        private static int MinBits(int x)
        {
            int bits; // Variable to store the minimum number of bits required

            if (x == 0)
                bits = 2; // If x is zero, minimum bits required is 2 (to represent 0 or 1)
            else if (x > 0)
            {
                bits = 32; // Initialize bits to 32 for positive values of x (assuming 32-bit integers)
                while (bits > 2)  // Iterate until minimum bits is greater than 2
                {
                    x <<= 1; // Left shift x by 1 (equivalent to multiplying by 2)
                    if ((x & 0x80000000) != 0)  // Check if the most significant bit is set
                        goto ex; // If the most significant bit is set, go to the label 'ex'
                    bits--; // Decrement bits by 1
                }
                goto ex; // After the loop, go to the label 'ex'
            }
            else
            {
                bits = 32; // Initialize bits to 32 for negative values of x (assuming 32-bit integers)
                while (bits > 2)  // Iterate until minimum bits is greater than 2
                {
                    x <<= 1; // Left shift x by 1 (equivalent to multiplying by 2)
                    if ((x & 0x80000000) == 0)  // Check if the most significant bit is not set
                        goto ex; // If the most significant bit is not set, go to the label 'ex'
                    bits--; // Decrement bits by 1
                }
            }

        ex:
            return bits; // Return the minimum number of bits required
        }

        public static void DifArray24(Int24[] ars, Int24[] ard, out int max, out int min)
        {
            // Initialize max and min variables
            max = int.MinValue;
            min = int.MaxValue;

            for (int i = 0; i < ars.Length - 1; i++)
            {
                // Calculate the differences between consecutive Int24 values
                int ti1 = ars[i] << 8;
                int ti2 = ars[i + 1] << 8;
                ti2 -= ti1;

                // Update max and min values
                if (ti2 > max)
                    max = ti2;
                if (ti2 < min)
                    min = ti2;

                // Shift and assign the differences to ard
                ti2 >>= 8;

                ard[i] = (Int24)ti2;
            }

            // Adjust the max and min values
            if (max < 0)
                max = (int)(((uint)max >> 8) | 0xFF000000);
            else
                max >>= 8;

            if (min < 0)
                min = (int)(((uint)min >> 8) | 0xFF000000);
            else
                min >>= 8;

            // Assign Int24.Zero to the last element of ard
            ard[ars.Length - 1] = Int24.Zero;
        }

        private void DifArray32(int[] ars, int[] ard, out int max, out int min)
        {
            max = int.MinValue;
            min = int.MaxValue;

            for (int i = 0; i < ars.Length - 1; i++)
            {
                ard[i] = ars[i + 1] - ars[i];
                if (ard[i] > max)
                    max = ard[i];
                if (ard[i] < min)
                    min = ard[i];
            }

            ard[ars.Length - 1] = 0;
        }
    }
}