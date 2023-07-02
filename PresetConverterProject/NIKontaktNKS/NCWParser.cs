using System.Runtime.InteropServices;
using Serilog;

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
            // wp.ReadToIntegers(ref ints);
            // var wavOutPathInts = "C:\\Users\\periv\\Projects\\Temp\\viola_sus_short-portato_64-127_E4 - AB - ints.wav";
            // wp.SaveWAVFromIntegers(wavOutPathInts, ref ints);

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
                    ncwParser.Header.Samplerate > 44100)
                {
                    ncwParser.SaveToWAVEx(outputFilePath);
                }
                else
                {
                    ncwParser.SaveToWAV(outputFilePath);
                }
            }

            ncwParser = null;
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
            public uint Samplerate;
            public uint numSamples;
            public uint block_def_offset;
            public uint blocks_offset;
            public uint blocks_size;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 88)]
            public char[] some_data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TBlockHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] Signature;
            public int BaseValue;
            public short bits;
            public ushort flags;
            public uint zeros2;
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

        private int[]? blocksDefList;
        private int[]? datai;
        private sbyte[]? data8;
        private short[]? data16;
        private byte[,]? data24; // x 3

        private FileStream? fs;

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

            blocksDefList = Array.Empty<int>();
        }

        public void SaveToWAV(string filename)
        {
            WAVParser.TMyWAVHeader waveHeader = new();

            waveHeader.wFormatTag = 1; // Standard wav
            waveHeader.nChannels = header.Channels;
            waveHeader.nSamplesPerSec = header.Samplerate;
            waveHeader.wBitsPerSample = header.Bits;
            waveHeader.nBlockAlign = (ushort)(waveHeader.nChannels * waveHeader.wBitsPerSample / 8);
            waveHeader.nAvgBytesPerSec = waveHeader.nSamplesPerSec * waveHeader.nBlockAlign;
            waveHeader.cbSize = 0;
            waveHeader.dataSize = waveHeader.nBlockAlign * header.numSamples;
            waveHeader.numOfPoints = (int)header.numSamples;
            waveHeader.dataPos = 44;

            WAVParser wp = new()
            {
                WavHeader = waveHeader
            };
            // use chnkSize = 20
            wp.StartSaveBlocks(filename, 20);

            int block_size = 1024;
            int nblocks = (int)waveHeader.dataSize / block_size;
            int nrem = (int)waveHeader.dataSize - nblocks * block_size;

            byte[] buf;
            switch (header.Bits)
            {
                case 8:
                    buf = new byte[(int)header.numSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data8, 0), buf, 0, (int)header.numSamples * header.Channels);
                    break;
                case 16:
                    buf = new byte[2 * (int)header.numSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data16, 0), buf, 0, 2 * (int)header.numSamples * header.Channels);
                    break;
                case 24:
                    buf = new byte[3 * (int)header.numSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data24, 0), buf, 0, 3 * (int)header.numSamples * header.Channels);
                    break;
                case 32:
                    buf = new byte[4 * (int)header.numSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(datai, 0), buf, 0, 4 * (int)header.numSamples * header.Channels);
                    break;
                default:
                    throw new Exception("NCWPARSER.SaveToWav: Unsupported BitsPerSample");
            }

            for (int i = 0; i < 3; i++)
            {
                wp.WriteBlock(buf, block_size);
                buf = buf.Skip(block_size).ToArray();
            }

            // if (nrem != 0)
            // {
            //     wp.WriteBlock(buf, nrem);
            // }

            wp.CloseWav();
        }

        public void SaveToWAVEx(string filename)
        {
            WAVParser.TMyWAVHeader waveHeader = new();

            waveHeader.wFormatTag = 0xFFFE; // Extended wav
            waveHeader.nChannels = header.Channels;
            waveHeader.nSamplesPerSec = header.Samplerate;
            waveHeader.wBitsPerSample = header.Bits;
            waveHeader.nBlockAlign = (ushort)(waveHeader.nChannels * waveHeader.wBitsPerSample / 8);
            waveHeader.nAvgBytesPerSec = waveHeader.nSamplesPerSec * waveHeader.nBlockAlign;
            waveHeader.cbSize = 0;
            waveHeader.dataSize = waveHeader.nBlockAlign * header.numSamples;
            waveHeader.numOfPoints = (int)header.numSamples;
            waveHeader.dataPos = 44;
            waveHeader.cbSize = 0;
            waveHeader.realBps = header.Bits;
            waveHeader.speakers = 0;
            waveHeader.GUID = WAVParser.WAV_TEST_GUID;

            WAVParser wp = new()
            {
                WavHeader = waveHeader
            };
            wp.StartSaveBlocks(filename);

            int block_size = 1024;
            int nblocks = (int)waveHeader.dataSize / block_size;
            int nrem = (int)waveHeader.dataSize - nblocks * block_size;

            byte[] buf;
            switch (header.Bits)
            {
                case 8:
                    buf = new byte[(int)header.numSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data8, 0), buf, 0, (int)header.numSamples * header.Channels);
                    break;
                case 16:
                    buf = new byte[2 * (int)header.numSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data16, 0), buf, 0, 2 * (int)header.numSamples * header.Channels);
                    break;
                case 24:
                    buf = new byte[3 * (int)header.numSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(data24, 0), buf, 0, 3 * (int)header.numSamples * header.Channels);
                    break;
                case 32:
                    buf = new byte[4 * (int)header.numSamples * header.Channels];
                    Marshal.Copy(GetIntPtr(datai, 0), buf, 0, 4 * (int)header.numSamples * header.Channels);
                    break;
                default:
                    throw new Exception("NCWPARSER.SaveToWav: Unsupported BitsPerSample");
            }

            for (int i = 0; i < nblocks; i++)
            {
                wp.WriteBlock(buf, block_size);
                buf = buf.Skip(block_size).ToArray();
            }

            if (nrem != 0)
            {
                wp.WriteBlock(buf, nrem);
            }

            wp.CloseWav();
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

            Log.Information(String.Format("Header @ position {0} [{1} hz {2} bits {3} ch] {4} samples", position, header.Samplerate, header.Bits, header.Channels, header.numSamples));
            Log.Debug("block_def_offset: " + header.block_def_offset);
            Log.Debug("blocks_offset: " + header.blocks_offset);
            Log.Debug("blocks_size: " + header.blocks_size);

            // check if matches either ncw signature 1 or 2
            for (int i = 0; i < 8; i++)
                if ((header.Signature[i] != NCW_SIGNATURE1[i]) && (header.Signature[i] != NCW_SIGNATURE2[i]))
                    throw new Exception("Wrong file signature");

            blocksDefList = new int[(header.blocks_offset - header.block_def_offset) / 4];
            fs.Seek(header.block_def_offset, SeekOrigin.Begin);
            byte[] blocksDefListBytes = new byte[blocksDefList.Length * 4];
            fs.Read(blocksDefListBytes, 0, blocksDefListBytes.Length);
            Buffer.BlockCopy(blocksDefListBytes, 0, blocksDefList, 0, blocksDefListBytes.Length);
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

        private ushort GetBlockHeaderFlags(FileStream fs)
        {
            long position = fs.Position;
            // fs.Seek(position - Marshal.SizeOf(typeof(TBlockHeader)), SeekOrigin.Begin);
            TBlockHeader bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
            fs.Seek(position, SeekOrigin.Begin);
            return bHeader.flags;
        }

        public void ReadNCW8()
        {
            data8 = new sbyte[header.numSamples * header.Channels];

            int curoffset = 0;
            uint cursample = 0;

            IntPtr input_buf = Marshal.AllocHGlobal(header.Bits * 64);
            sbyte[,] temp8 = new sbyte[MAX_CHANNELS, NCW_SAMPLES];

            for (int i = 0; i < blocksDefList.Length - 1; i++)
            {
                fs.Seek(header.blocks_offset + blocksDefList[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    int nbits;
                    if (bHeader.bits < 0)
                    {
                        nbits = Math.Abs(bHeader.bits);
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        BitProcess.Fill8(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp8, j), false);
                    }
                    else
                    {
                        nbits = (bHeader.bits == 0) ? header.Bits : bHeader.bits;
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        bool nrelative = bHeader.bits != 0;
                        BitProcess.Fill8(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp8, j), nrelative);
                    }
                }

                if (bHeader.flags == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        data8[curoffset] = (sbyte)(temp8[0, k] + temp8[1, k]);
                        curoffset++;
                        data8[curoffset] = (sbyte)(temp8[0, k] - temp8[1, k]);
                        curoffset++;
                        cursample++;

                        if (cursample >= header.numSamples) goto ex;
                    }
                }
                else
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            data8[curoffset] = temp8[j, k];
                            curoffset++;
                        }
                        cursample++;

                        if (cursample >= header.numSamples) goto ex;
                    }
                }
            }
        ex:
            Marshal.FreeHGlobal(input_buf);
        }

        public void ReadNCW16()
        {
            data16 = new short[header.numSamples * header.Channels];

            int curoffset = 0;
            uint cursample = 0;

            IntPtr input_buf = Marshal.AllocHGlobal(header.Bits * 64);
            short[,] temp16 = new short[MAX_CHANNELS, NCW_SAMPLES];

            for (int i = 0; i < blocksDefList.Length - 1; i++)
            {
                fs.Seek(header.blocks_offset + blocksDefList[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    int nbits;
                    if (bHeader.bits < 0)
                    {
                        nbits = Math.Abs(bHeader.bits);
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        BitProcess.Fill16(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp16, j), false);
                    }
                    else
                    {
                        nbits = (bHeader.bits == 0) ? header.Bits : bHeader.bits;
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        bool nrelative = bHeader.bits != 0;
                        BitProcess.Fill16(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp16, j), nrelative);
                    }
                }

                if (bHeader.flags == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        data16[curoffset] = (short)(temp16[0, k] + temp16[1, k]);
                        curoffset++;
                        data16[curoffset] = (short)(temp16[0, k] - temp16[1, k]);
                        curoffset++;
                        cursample++;

                        if (cursample >= header.numSamples) goto ex;
                    }
                }
                else
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            data16[curoffset] = temp16[j, k];
                            curoffset++;
                        }
                        cursample++;

                        if (cursample >= header.numSamples) goto ex;
                    }
                }
            }
        ex:
            Marshal.FreeHGlobal(input_buf);
        }

        public void ReadNCW24()
        {
            data24 = new byte[header.numSamples * header.Channels, 3];

            int curoffset = 0;
            uint cursample = 0;

            IntPtr input_buf = Marshal.AllocHGlobal(header.Bits * 64);
            int[,] temp24 = new int[MAX_CHANNELS, NCW_SAMPLES * 3];

            int nbits;

            for (int i = 0; i < blocksDefList.Length - 1; i++)
            {
                fs.Seek(header.blocks_offset + blocksDefList[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    Log.Debug(String.Format("Block Header @ position {0} [{1} base, {2} bits, {3} flags]", position, bHeader.BaseValue, bHeader.bits, bHeader.flags));

                    // If 'Bits' < 0 then compression is used. 
                    // You get actual bits precision by taking absolute value of 'Bits'. 
                    // This is used for 8, 16, 24, and 32 bits.
                    if (bHeader.bits < 0)
                    {
                        nbits = Math.Abs(bHeader.bits);

                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        BitProcess.Fill24(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp24, j), false);
                    }

                    // If 'Bits' = 0 then there's no compression. 
                    // You have to use number of bits from the main header ('Bits' field). 
                    // Another words, bits precision is the same as the original WAV file had.
                    // If 'Bits' > 0 then things become more complicated. 
                    // First, data stored in block are not sample values, but deltas (differences)
                    else
                    {
                        nbits = (bHeader.bits == 0) ? header.Bits : bHeader.bits;
                        bool nrelative = bHeader.bits != 0;

                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        BitProcess.Fill24(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp24, j), nrelative);
                    }
                }

                if (bHeader.flags == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // Considering stereo samples
                        int ti1 = (temp24[0, k * 3] + (temp24[0, k * 3 + 1] << 8) + (temp24[0, k * 3 + 2] << 16)) << 8;
                        int ti2 = (temp24[1, k * 3] + (temp24[1, k * 3 + 1] << 8) + (temp24[1, k * 3 + 2] << 16)) << 8;
                        int ti3 = ti1 + ti2;

                        data24[curoffset, 0] = (byte)((ti3 >> 8) & 0xFF);
                        data24[curoffset, 1] = (byte)((ti3 >> 16) & 0xFF);
                        data24[curoffset, 2] = (byte)((ti3 >> 24) & 0xFF);
                        curoffset++;

                        ti1 = (temp24[0, k * 3] + (temp24[0, k * 3 + 1] << 8) + (temp24[0, k * 3 + 2] << 16)) << 8;
                        ti2 = (temp24[1, k * 3] + (temp24[1, k * 3 + 1] << 8) + (temp24[1, k * 3 + 2] << 16)) << 8;
                        ti3 = ti1 - ti2;

                        data24[curoffset, 0] = (byte)((ti3 >> 8) & 0xFF);
                        data24[curoffset, 1] = (byte)((ti3 >> 16) & 0xFF);
                        data24[curoffset, 2] = (byte)((ti3 >> 24) & 0xFF);
                        curoffset++;
                        cursample++;

                        if (cursample >= header.numSamples) goto ex;
                    }
                }
                else
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            data24[curoffset, 0] = (byte)temp24[j, k * 3];
                            data24[curoffset, 1] = (byte)temp24[j, k * 3 + 1];
                            data24[curoffset, 2] = (byte)temp24[j, k * 3 + 2];
                            curoffset++;
                        }
                        cursample++;

                        if (cursample >= header.numSamples) goto ex;
                    }
                }
            }
        ex:
            Marshal.FreeHGlobal(input_buf);
        }

        public void ReadNCW32()
        {
            datai = new int[header.numSamples * header.Channels];

            int curoffset = 0;
            uint cursample = 0;

            IntPtr input_buf = Marshal.AllocHGlobal(header.Bits * 64);
            int[,] temp32 = new int[MAX_CHANNELS, NCW_SAMPLES];

            int nbits;

            for (int i = 0; i < blocksDefList.Length - 1; i++)
            {
                fs.Seek(header.blocks_offset + blocksDefList[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    if (bHeader.bits < 0)
                    {
                        nbits = Math.Abs(bHeader.bits);
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        BitProcess.Fill32(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp32, j), false);
                    }
                    else
                    {
                        nbits = (bHeader.bits == 0) ? header.Bits : bHeader.bits;
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        bool nrelative = bHeader.bits != 0;
                        BitProcess.Fill32(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp32, j), nrelative);
                    }
                }

                if (bHeader.flags == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // Considering stereo samples
                        datai[curoffset] = temp32[0, k] + temp32[1, k];
                        curoffset++;
                        datai[curoffset] = temp32[0, k] - temp32[1, k];
                        curoffset++;
                        cursample++;

                        if (cursample >= header.numSamples) goto ex;
                    }
                }
                else
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            datai[curoffset] = temp32[j, k];
                            curoffset++;
                        }
                        cursample++;

                        if (cursample >= header.numSamples) goto ex;
                    }
                }
            }
        ex:
            Marshal.FreeHGlobal(input_buf);
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
                    // Handle unsupported bit depth or error
                    break;
            }
        }

        public void ReadNCWIntegers()
        {
            datai = new int[header.numSamples * header.Channels];

            int curoffset = 0;
            uint cursample = 0;

            byte[][] tempb = new byte[header.Channels][];
            int[][] tempi = new int[5][];

            int nbits;

            for (int i = 0; i < blocksDefList.Length - 1; i++)
            {
                fs.Seek(header.blocks_offset + blocksDefList[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    if (bHeader.bits < 0)
                    {
                        nbits = Math.Abs(bHeader.bits);
                        tempb[j] = ReadBytes(fs, nbits * 64);
                        tempi[j] = new int[NCW_SAMPLES];
                        BitProcess.FillIntegersAbs(NCW_SAMPLES, nbits, GetIntPtr(tempb, j), bHeader.BaseValue, ref tempi[j]);
                    }
                    else
                    {
                        nbits = (bHeader.bits == 0) ? header.Bits : bHeader.bits;
                        tempb[j] = ReadBytes(fs, nbits * 64);
                        bool nrelative = bHeader.bits != 0;
                        tempi[j] = new int[NCW_SAMPLES];
                        BitProcess.FillIntegers(NCW_SAMPLES, nbits, GetIntPtr(tempb, j), bHeader.BaseValue, ref tempi[j], nrelative);
                    }
                }

                if (bHeader.flags == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // Considering stereo samples
                        datai[curoffset] = tempi[0][k] + tempi[1][k];
                        curoffset++;
                        datai[curoffset] = tempi[0][k] - tempi[1][k];
                        curoffset++;
                        cursample++;

                        if (cursample >= header.numSamples) goto ex;
                    }
                }
                else
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            datai[curoffset] = tempi[j][k];
                            curoffset++;
                        }
                        cursample++;

                        if (cursample >= header.numSamples) goto ex;
                    }
                }
            }
        ex:
            for (int i = 0; i < header.Channels; i++)
            {
                tempb[i] = null;
                tempi[i] = null;
            }
            tempb = null;
            tempi = null;
        }

        // TODO: PIN DELETE THESE
        public void ReadNCW24V2()
        {
            var data24 = new byte[header.numSamples * header.Channels][];

            int curoffset = 0;
            uint cursample = 0;

            int[][] temp24 = new int[MAX_CHANNELS][];

            int nbits;

            for (int i = 0; i < blocksDefList.Length - 1; i++)
            {
                fs.Seek(header.blocks_offset + blocksDefList[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    long position = fs.Position;
                    var headerBytes = ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader)));
                    bHeader = ByteArrayToStructure<TBlockHeader>(headerBytes);
                    Log.Debug(String.Format("Block Header @ position {0} [{1} base, {2} bits, {3} flags]", position, bHeader.BaseValue, bHeader.bits, bHeader.flags));
                    if (bHeader.bits < 0)
                    {
                        nbits = Math.Abs(bHeader.bits);
                        var inputBuf = ReadBytes(fs, nbits * 64);
                        temp24[j] = new int[NCW_SAMPLES * 3];

                        BitProcess.Fill24V2(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp24[j], false);
                    }
                    else
                    {
                        nbits = (bHeader.bits == 0) ? header.Bits : bHeader.bits;
                        var inputBuf = ReadBytes(fs, nbits * 64);
                        bool nrelative = bHeader.bits != 0;
                        temp24[j] = new int[NCW_SAMPLES * 3];

                        BitProcess.Fill24V2(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp24[j], nrelative);
                    }
                }

                if (bHeader.flags == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // Considering stereo samples
                        int ti1 = (temp24[0][k * 3] + (temp24[0][k * 3 + 1] << 8) + (temp24[0][k * 3 + 2] << 16)) << 8;
                        int ti2 = (temp24[1][k * 3] + (temp24[1][k * 3 + 1] << 8) + (temp24[1][k * 3 + 2] << 16)) << 8;
                        int ti3 = ti1 + ti2;

                        data24[curoffset] = new byte[3];
                        data24[curoffset][0] = (byte)((ti3 >> 8) & 0xFF);
                        data24[curoffset][1] = (byte)((ti3 >> 16) & 0xFF);
                        data24[curoffset][2] = (byte)((ti3 >> 24) & 0xFF);
                        curoffset++;

                        ti1 = (temp24[0][k * 3] + (temp24[0][k * 3 + 1] << 8) + (temp24[0][k * 3 + 2] << 16)) << 8;
                        ti2 = (temp24[1][k * 3] + (temp24[1][k * 3 + 1] << 8) + (temp24[1][k * 3 + 2] << 16)) << 8;
                        ti3 = ti1 - ti2;

                        data24[curoffset] = new byte[3];
                        data24[curoffset][0] = (byte)((ti3 >> 8) & 0xFF);
                        data24[curoffset][1] = (byte)((ti3 >> 16) & 0xFF);
                        data24[curoffset][2] = (byte)((ti3 >> 24) & 0xFF);
                        curoffset++;
                        cursample++;

                        if (cursample >= header.numSamples)
                            return;
                    }
                }
                else
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            data24[curoffset] = new byte[3];
                            data24[curoffset][0] = (byte)temp24[j][k * 3];
                            data24[curoffset][1] = (byte)temp24[j][k * 3 + 1];
                            data24[curoffset][2] = (byte)temp24[j][k * 3 + 2];
                            curoffset++;
                        }
                        cursample++;

                        if (cursample >= header.numSamples)
                            return;
                    }
                }
            }
        }
    }
}