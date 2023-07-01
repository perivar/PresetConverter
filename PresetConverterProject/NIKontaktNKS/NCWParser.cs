using System.Runtime.InteropServices;
using System.Text;
using CommonUtils;
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

            var wavtype = WavType.Standard;

            string outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".wav";
            string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName);
            Log.Information("Writing file {0} ...", outputFilePath);

            if (wavtype == WavType.Standard)
                ncwParser.SaveToWAV(outputFilePath);
            else if (wavtype == WavType.Extended)
                ncwParser.SaveToWAVEx(outputFilePath);
            else
            {
                if (ncwParser.Header.Channels > 2 ||
                    ncwParser.Header.Bits > 16 ||
                    ncwParser.Header.Samplerate > 44100)
                    ncwParser.SaveToWAVEx(outputFilePath);
                else
                    ncwParser.SaveToWAV(outputFilePath);
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

        private FileStream fs;

        private TNCWHeader header;
        public TNCWHeader Header { get => header; set => header = value; }

        private int[] blocksDefList;
        private int[] datai;
        private sbyte[] data8;
        private short[] data16;
        private byte[,] data24; // x 3

        public void Clear()
        {
            CloseFile();
            datai = new int[0];
            data8 = new sbyte[0];
            data16 = new short[0];
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
            blocksDefList = new int[0];
        }

        public void SaveToWAV(string filename)
        {
            var waveHeader = new WAVParser.TMyWAVHeader();

            waveHeader.wFormatTag = 1; // Standard wav
            waveHeader.nChannels = Header.Channels;
            waveHeader.nSamplesPerSec = Header.Samplerate;
            waveHeader.wBitsPerSample = Header.Bits;
            waveHeader.nBlockAlign = (ushort)(waveHeader.nChannels * waveHeader.wBitsPerSample / 8);
            waveHeader.nAvgBytesPerSec = waveHeader.nSamplesPerSec * waveHeader.nBlockAlign;
            waveHeader.cbSize = 0;
            waveHeader.dataSize = waveHeader.nBlockAlign * Header.numSamples;
            waveHeader.numOfPoints = (int)Header.numSamples;
            waveHeader.dataPos = 44;

            var wp = new WAVParser();
            wp.WavHeader = waveHeader;
            wp.StartSaveBlocks(filename);

            int block_size = 1024;
            int nblocks = (int)waveHeader.dataSize / block_size;
            int nrem = (int)waveHeader.dataSize - nblocks * block_size;

            byte[] buf;
            switch (Header.Bits)
            {
                case 8:
                    buf = new byte[(int)Header.numSamples];
                    Marshal.Copy(GetIntPtr(data8, 0), buf, 0, (int)Header.numSamples);
                    break;
                case 16:
                    buf = new byte[2 * (int)Header.numSamples];
                    Marshal.Copy(GetIntPtr(data16, 0), buf, 0, 2 * (int)Header.numSamples);
                    break;
                case 24:
                    buf = new byte[3 * (int)Header.numSamples];
                    Marshal.Copy(GetIntPtr(data24, 0), buf, 0, 3 * (int)Header.numSamples);
                    break;
                case 32:
                    buf = new byte[4 * (int)Header.numSamples];
                    Marshal.Copy(GetIntPtr(datai, 0), buf, 0, 4 * (int)Header.numSamples);
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

        public void SaveToWAVEx(string filename)
        {
            var wp = new WAVParser();
            var waveHeader = new WAVParser.TMyWAVHeader();
            wp.WavHeader = waveHeader;

            waveHeader.wFormatTag = 0xFFFE; // Extended wav
            waveHeader.nChannels = Header.Channels;
            waveHeader.nSamplesPerSec = Header.Samplerate;
            waveHeader.wBitsPerSample = Header.Bits;
            waveHeader.nBlockAlign = (ushort)(waveHeader.nChannels * waveHeader.wBitsPerSample / 8);
            waveHeader.nAvgBytesPerSec = waveHeader.nSamplesPerSec * waveHeader.nBlockAlign;
            waveHeader.cbSize = 0;
            waveHeader.dataSize = waveHeader.nBlockAlign * Header.numSamples;
            waveHeader.numOfPoints = (int)Header.numSamples;
            waveHeader.dataPos = 44;
            waveHeader.cbSize = 0;
            waveHeader.realBps = Header.Bits;
            waveHeader.speakers = 0;
            waveHeader.GUID = WAVParser.WAV_TEST_GUID;

            wp.StartSaveBlocks(filename);

            int block_size = 1024;
            int nblocks = (int)waveHeader.dataSize / block_size;
            int nrem = (int)waveHeader.dataSize - nblocks * block_size;

            byte[] buf;
            switch (Header.Bits)
            {
                case 8:
                    buf = new byte[(int)Header.numSamples];
                    Marshal.Copy(GetIntPtr(data8, 0), buf, 0, (int)Header.numSamples);
                    break;
                case 16:
                    buf = new byte[2 * (int)Header.numSamples];
                    Marshal.Copy(GetIntPtr(data16, 0), buf, 0, 2 * (int)Header.numSamples);
                    break;
                case 24:
                    buf = new byte[3 * (int)Header.numSamples];
                    Marshal.Copy(GetIntPtr(data24, 0), buf, 0, 3 * (int)Header.numSamples);
                    break;
                case 32:
                    buf = new byte[4 * (int)Header.numSamples];
                    Marshal.Copy(GetIntPtr(datai, 0), buf, 0, 4 * (int)Header.numSamples);
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
            Header = ByteArrayToStructure<TNCWHeader>(headerBytes);

            Log.Information(String.Format("Header @ position {0} [{1} hz {2} bits {3} ch] {4} samples", position, Header.Samplerate, Header.Bits, Header.Channels, Header.numSamples));
            Log.Debug("block_def_offset: " + Header.block_def_offset);
            Log.Debug("blocks_offset: " + Header.blocks_offset);
            Log.Debug("blocks_size: " + Header.blocks_size);

            // check if matches either ncw signature 1 or 2
            for (int i = 0; i < 8; i++)
                if ((Header.Signature[i] != NCW_SIGNATURE1[i]) && (Header.Signature[i] != NCW_SIGNATURE2[i]))
                    throw new Exception("Wrong file signature");

            blocksDefList = new int[(Header.blocks_offset - Header.block_def_offset) / 4];
            fs.Seek(Header.block_def_offset, SeekOrigin.Begin);
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
            data8 = new sbyte[Header.numSamples * Header.Channels];
            int curoffset = 0;
            uint cursample = 0;

            IntPtr input_buf = Marshal.AllocHGlobal(Header.Bits * 64);
            sbyte[,] temp8 = new sbyte[MAX_CHANNELS, NCW_SAMPLES];

            for (int i = 0; i < blocksDefList.Length - 1; i++)
            {
                fs.Seek(Header.blocks_offset + blocksDefList[i], SeekOrigin.Begin);
                for (int j = 0; j < Header.Channels; j++)
                {
                    TBlockHeader bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    int nbits;
                    if (bHeader.bits < 0)
                    {
                        nbits = Math.Abs(bHeader.bits);
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        BitProcess.Fill8(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp8, j), false);
                    }
                    else
                    {
                        if (bHeader.bits == 0) nbits = Header.Bits;
                        else nbits = bHeader.bits;
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        bool nrelative = (bHeader.bits != 0);
                        BitProcess.Fill8(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp8, j), nrelative);
                    }
                }

                if (GetBlockHeaderFlags(fs) == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        data8[curoffset] = (sbyte)(temp8[0, k] + temp8[1, k]);
                        curoffset++;
                        data8[curoffset] = (sbyte)(temp8[0, k] - temp8[1, k]);
                        curoffset++;
                        cursample++;
                        if (cursample >= Header.numSamples) goto ex;
                    }
                }
                else
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < Header.Channels; j++)
                        {
                            data8[curoffset] = temp8[j, k];
                            curoffset++;
                        }
                        cursample++;
                        if (cursample >= Header.numSamples) goto ex;
                    }
                }
            }
        ex:
            Marshal.FreeHGlobal(input_buf);
        }

        public void ReadNCW16()
        {
            data16 = new short[Header.numSamples * Header.Channels];
            int curoffset = 0;
            uint cursample = 0;

            IntPtr input_buf = Marshal.AllocHGlobal(Header.Bits * 64);
            short[,] temp16 = new short[MAX_CHANNELS, NCW_SAMPLES];

            for (int i = 0; i < blocksDefList.Length - 1; i++)
            {
                fs.Seek(Header.blocks_offset + blocksDefList[i], SeekOrigin.Begin);
                for (int j = 0; j < Header.Channels; j++)
                {
                    TBlockHeader bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    int nbits;
                    if (bHeader.bits < 0)
                    {
                        nbits = Math.Abs(bHeader.bits);
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        BitProcess.Fill16(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp16, j), false);
                    }
                    else
                    {
                        if (bHeader.bits == 0) nbits = Header.Bits;
                        else nbits = bHeader.bits;
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        bool nrelative = (bHeader.bits != 0);
                        BitProcess.Fill16(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp16, j), nrelative);
                    }
                }

                if (GetBlockHeaderFlags(fs) == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        data16[curoffset] = (short)(temp16[0, k] + temp16[1, k]);
                        curoffset++;
                        data16[curoffset] = (short)(temp16[0, k] - temp16[1, k]);
                        curoffset++;
                        cursample++;
                        if (cursample >= Header.numSamples) goto ex;
                    }
                }
                else
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < Header.Channels; j++)
                        {
                            data16[curoffset] = temp16[j, k];
                            curoffset++;
                        }
                        cursample++;
                        if (cursample >= Header.numSamples) goto ex;
                    }
                }
            }
        ex:
            Marshal.FreeHGlobal(input_buf);
        }

        public void ReadNCW24()
        {
            data24 = new byte[Header.numSamples * Header.Channels, 3];

            int curoffset = 0;
            uint cursample = 0;
            IntPtr input_buf = Marshal.AllocHGlobal(Header.Bits * 64);
            int[,] temp24 = new int[MAX_CHANNELS, NCW_SAMPLES * 3];
            int nbits;
            bool nrelative;
            int ti1, ti2, ti3;

            for (int i = 0; i < blocksDefList.Length - 1; i++)
            {
                fs.Seek(Header.blocks_offset + blocksDefList[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < Header.Channels; j++)
                {
                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    Log.Debug(String.Format("Block Header @ position {0} [{1} base, {2} bits, {3} flags]", position, bHeader.BaseValue, bHeader.bits, bHeader.flags));
                    if (bHeader.bits < 0)
                    {
                        nbits = Math.Abs(bHeader.bits);
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        BitProcess.Fill24(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp24, j), false);
                    }
                    else
                    {
                        if (bHeader.bits == 0)
                            nbits = Header.Bits;
                        else
                            nbits = bHeader.bits;
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        nrelative = (bHeader.bits != 0);
                        BitProcess.Fill24(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp24, j), nrelative);
                    }
                }

                if (bHeader.flags == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // Considering stereo samples
                        ti1 = (temp24[0, k * 3] + (temp24[0, k * 3 + 1] << 8) + (temp24[0, k * 3 + 2] << 16)) << 8;
                        ti2 = (temp24[1, k * 3] + (temp24[1, k * 3 + 1] << 8) + (temp24[1, k * 3 + 2] << 16)) << 8;
                        ti3 = (ti1 + ti2);
                        data24[curoffset, 0] = (byte)((ti3 >> 8) & 0xFF);
                        data24[curoffset, 1] = (byte)((ti3 >> 16) & 0xFF);
                        data24[curoffset, 2] = (byte)((ti3 >> 24) & 0xFF);
                        curoffset++;

                        ti1 = (temp24[0, k * 3] + (temp24[0, k * 3 + 1] << 8) + (temp24[0, k * 3 + 2] << 16)) << 8;
                        ti2 = (temp24[1, k * 3] + (temp24[1, k * 3 + 1] << 8) + (temp24[1, k * 3 + 2] << 16)) << 8;
                        ti3 = (ti1 - ti2);
                        data24[curoffset, 0] = (byte)((ti3 >> 8) & 0xFF);
                        data24[curoffset, 1] = (byte)((ti3 >> 16) & 0xFF);
                        data24[curoffset, 2] = (byte)((ti3 >> 24) & 0xFF);
                        curoffset++;

                        cursample++;
                        if (cursample >= Header.numSamples) goto ex;
                    }
                }
                else
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < Header.Channels; j++)
                        {
                            data24[curoffset, 0] = (byte)temp24[j, k * 3];
                            data24[curoffset, 1] = (byte)temp24[j, k * 3 + 1];
                            data24[curoffset, 2] = (byte)temp24[j, k * 3 + 2];
                            curoffset++;
                        }
                        cursample++;
                        if (cursample >= Header.numSamples) goto ex;
                    }
                }
            }
        ex:
            Marshal.FreeHGlobal(input_buf);
        }

        public void ReadNCW32()
        {
            datai = new int[Header.numSamples * Header.Channels];
            int curoffset = 0;
            uint cursample = 0;
            IntPtr input_buf = Marshal.AllocHGlobal(Header.Bits * 64);
            int[,] temp32 = new int[MAX_CHANNELS, NCW_SAMPLES];
            int nbits;
            bool nrelative;

            for (int i = 0; i < blocksDefList.Length - 1; i++)
            {
                fs.Seek(Header.blocks_offset + blocksDefList[i], SeekOrigin.Begin);
                for (int j = 0; j < Header.Channels; j++)
                {
                    TBlockHeader bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    if (bHeader.bits < 0)
                    {
                        nbits = Math.Abs(bHeader.bits);
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        BitProcess.Fill32(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp32, j), false);
                    }
                    else
                    {
                        if (bHeader.bits == 0)
                            nbits = Header.Bits;
                        else
                            nbits = bHeader.bits;
                        Marshal.Copy(ReadBytes(fs, nbits * 64), 0, input_buf, nbits * 64);
                        nrelative = (bHeader.bits != 0);
                        BitProcess.Fill32(NCW_SAMPLES, nbits, input_buf, bHeader.BaseValue, GetIntPtr(temp32, j), nrelative);
                    }
                }

                if (GetBlockHeaderFlags(fs) == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // Considering stereo samples
                        datai[curoffset] = temp32[0, k] + temp32[1, k];
                        curoffset++;
                        datai[curoffset] = temp32[0, k] - temp32[1, k];
                        curoffset++;
                        cursample++;
                        if (cursample >= Header.numSamples) goto ex;
                    }
                }
                else
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < Header.Channels; j++)
                        {
                            datai[curoffset] = temp32[j, k];
                            curoffset++;
                        }
                        cursample++;
                        if (cursample >= Header.numSamples) goto ex;
                    }
                }
            }
        ex:
            Marshal.FreeHGlobal(input_buf);
        }

        public void ReadNCW()
        {
            switch (Header.Bits)
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
            datai = new int[Header.numSamples * Header.Channels];
            int curoffset = 0;
            uint cursample = 0;
            byte[][] tempb = new byte[Header.Channels][];
            int[][] tempi = new int[5][];
            int nbits;
            bool nrelative;

            for (int i = 0; i < blocksDefList.Length - 1; i++)
            {
                fs.Seek(Header.blocks_offset + blocksDefList[i], SeekOrigin.Begin);
                for (int j = 0; j < Header.Channels; j++)
                {
                    TBlockHeader bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    if (bHeader.bits < 0)
                    {
                        nbits = Math.Abs(bHeader.bits);
                        tempb[j] = ReadBytes(fs, nbits * 64);
                        tempi[j] = new int[NCW_SAMPLES];
                        BitProcess.FillIntegersAbs(NCW_SAMPLES, nbits, GetIntPtr(tempb, j), bHeader.BaseValue, ref tempi[j]);
                    }
                    else
                    {
                        if (bHeader.bits == 0)
                            nbits = Header.Bits;
                        else
                            nbits = bHeader.bits;
                        tempb[j] = ReadBytes(fs, nbits * 64);
                        nrelative = (bHeader.bits != 0);
                        tempi[j] = new int[NCW_SAMPLES];
                        BitProcess.FillIntegers(NCW_SAMPLES, nbits, GetIntPtr(tempb, j), bHeader.BaseValue, ref tempi[j], nrelative);
                    }
                }

                if (GetBlockHeaderFlags(fs) == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // Considering stereo samples
                        datai[curoffset] = tempi[0][k] + tempi[1][k];
                        curoffset++;
                        datai[curoffset] = tempi[0][k] - tempi[1][k];
                        curoffset++;
                        cursample++;
                        if (cursample >= Header.numSamples) goto ex;
                    }
                }
                else
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < Header.Channels; j++)
                        {
                            datai[curoffset] = tempi[j][k];
                            curoffset++;
                        }
                        cursample++;
                        if (cursample >= Header.numSamples) goto ex;
                    }
                }
            }
        ex:
            for (int i = 0; i < Header.Channels; i++)
            {
                tempb[i] = null;
                tempi[i] = null;
            }
            tempb = null;
            tempi = null;
        }
    }
}