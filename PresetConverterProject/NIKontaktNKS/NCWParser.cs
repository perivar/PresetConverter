using System.Runtime.InteropServices;
using CommonUtils.Audio;
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
            // Output file to standard wav
            var wavtype = WavType.Standard;

            // Convert file
            var ncwParser = new NCWParser(doVerbose);
            ncwParser.Clear();
            ncwParser.OpenNCWFile(inputFilePath);

            // if (ncwParser.Header.Bits != 24)
            // {
            //     Log.Information("Found != 24 bit: {0}", inputFilePath);
            // }

            ncwParser.ReadNCW();

            if (!doList)
            {
                string outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".wav";
                string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName);
                Log.Information("Writing WAV file ({0}) as {1} ...", wavtype, outputFilePath);

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
            }

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

        private sbyte[]? data8; // 1 byte
        private short[]? data16; // 2 bytes
        private Int24[]? data24; // 3 bytes
        private int[]? datai; // 4 bytes

        private FileStream? fs;
        private MemoryStream? ms;

        private readonly bool doVerbose = false;

        public NCWParser(bool doVerbose = false)
        {
            this.doVerbose = doVerbose;
        }

        /// <summary>
        /// Convert a byte array to a given structure
        /// </summary>
        /// <example>
        /// <code>
        /// bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));         /// </code>
        /// </example>
        /// <typeparam name="T">Stuct type</typeparam>
        /// <param name="bytes">byte array</param>
        /// <returns>a structure</returns> 
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

        /// <summary>
        /// Convert a structure to its byte array
        /// </summary>
        /// <typeparam name="T">Stuct type</typeparam>
        /// <param name="structure">a strucure</param>
        /// <returns>byte array</returns>
        /// </summary>
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

        /// <summary>
        /// Get the int pointer for an array.
        /// </summary>
        /// <example>
        /// <code>
        /// private byte[,]? data24; // x 3
        /// var buf = new byte[3 * (int)header.NumSamples * header.Channels];
        /// Marshal.Copy(GetIntPtr(data24, 0), buf, 0, 3 * (int)header.NumSamples * header.Channels);
        /// </code>
        /// </example>
        /// <param name="array">array</param>
        /// <param name="index">index</param>
        /// <returns>a int pointer</returns>
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

        public void Clear()
        {
            CloseFile();

            datai = Array.Empty<int>();
            data8 = Array.Empty<sbyte>();
            data16 = Array.Empty<short>();
            data24 = Array.Empty<Int24>();

            if (ms != null)
            {
                ms.Close();
                ms.Dispose();
                ms = null;
            }
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
            WAVParser.TMyWAVHeader wavHeader = new()
            {
                wFormatTag = SoundIO.WAVE_FORMAT_PCM, // Standard wav
                nChannels = header.Channels,
                nSamplesPerSec = header.SampleRate,
                wBitsPerSample = header.Bits
            };

            wavHeader.nBlockAlign = (ushort)(wavHeader.nChannels * wavHeader.wBitsPerSample / 8);
            wavHeader.nAvgBytesPerSec = wavHeader.nSamplesPerSec * wavHeader.nBlockAlign;
            wavHeader.cbSize = 0;
            wavHeader.dataSize = wavHeader.nBlockAlign * header.NumSamples;
            wavHeader.numOfPoints = (int)header.NumSamples;
            wavHeader.dataPos = 44;

            WAVParser wp = new(doVerbose)
            {
                WavHeader = wavHeader
            };

            // use chnkSize = 20
            wp.StartSaveBlocks(filename, 20);

            Span<byte> buf;
            switch (header.Bits)
            {
                case 8:
                    buf = MemoryMarshal.Cast<sbyte, byte>(data8);
                    break;
                case 16:
                    buf = MemoryMarshal.Cast<short, byte>(data16);
                    break;
                case 24:
                    buf = MemoryMarshal.Cast<Int24, byte>(data24);
                    break;
                case 32:
                    buf = MemoryMarshal.Cast<int, byte>(datai);
                    break;
                default:
                    throw new Exception("NCWPARSER.SaveToWAV: Unsupported BitsPerSample");
            }

            // write in blocks
            // WriteInBlocks(wavHeader, wp, buf);

            // write everything in one go
            wp.WriteBlock(buf, buf.Length);

            wp.CloseWriter();
        }

        public void SaveToWAVEx(string filename)
        {
            WAVParser.TMyWAVHeader wavHeader = new()
            {
                wFormatTag = SoundIO.WAVE_FORMAT_EXTENSIBLE, // Extended wav
                nChannels = header.Channels,
                nSamplesPerSec = header.SampleRate,
                wBitsPerSample = header.Bits
            };

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

            WAVParser wp = new(doVerbose)
            {
                WavHeader = wavHeader
            };
            wp.StartSaveBlocks(filename);

            Span<byte> buf;
            switch (header.Bits)
            {
                case 8:
                    buf = MemoryMarshal.Cast<sbyte, byte>(data8);
                    break;
                case 16:
                    buf = MemoryMarshal.Cast<short, byte>(data16);
                    break;
                case 24:
                    buf = MemoryMarshal.Cast<Int24, byte>(data24);
                    break;
                case 32:
                    buf = MemoryMarshal.Cast<int, byte>(datai);
                    break;
                default:
                    throw new Exception("NCWPARSER.SaveToWAVEx: Unsupported BitsPerSample");
            }

            // write in blocks
            // WriteInBlocks(wavHeader, wp, buf);

            // write everything in one go
            wp.WriteBlock(buf, buf.Length);

            wp.CloseWriter();
        }

        public void SaveToWAVIntegers(string filename)
        {
            WAVParser.TMyWAVHeader wavHeader = new()
            {
                wFormatTag = SoundIO.WAVE_FORMAT_PCM, // Standard wav
                nChannels = header.Channels,
                nSamplesPerSec = header.SampleRate,
                wBitsPerSample = header.Bits
            };

            wavHeader.nBlockAlign = (ushort)(wavHeader.nChannels * wavHeader.wBitsPerSample / 8);
            wavHeader.nAvgBytesPerSec = wavHeader.nSamplesPerSec * wavHeader.nBlockAlign;
            wavHeader.cbSize = 0;
            wavHeader.dataSize = wavHeader.nBlockAlign * header.NumSamples;
            wavHeader.numOfPoints = (int)header.NumSamples;
            wavHeader.dataPos = 44;

            WAVParser wp = new(doVerbose)
            {
                WavHeader = wavHeader
            };

            // use chnkSize = 20
            wp.SaveWAVFromIntegers(filename, ref datai, 20);
        }

        private static void WriteInBlocks(WAVParser.TMyWAVHeader wavHeader, WAVParser wp, byte[] buf)
        {
            int blockSize = 1024;
            int nblocks = (int)wavHeader.dataSize / blockSize;
            int nrem = (int)wavHeader.dataSize - nblocks * blockSize;

            for (int i = 0; i < nblocks; i++)
            {
                wp.WriteBlock(buf, blockSize);
                buf = buf.Skip(blockSize).ToArray();
            }

            if (nrem != 0)
            {
                wp.WriteBlock(buf, nrem);
            }
        }

        private static void WriteInBlocks(WAVParser.TMyWAVHeader wavHeader, WAVParser wp, Span<byte> source)
        {
            int blockSize = 1024;
            int nblocks = (int)wavHeader.dataSize / blockSize;
            int nrem = (int)wavHeader.dataSize - nblocks * blockSize;

            for (int i = 0; i < nblocks; i++)
            {
                wp.WriteBlock(source, blockSize);
                source = source.Slice(blockSize);
            }

            if (nrem != 0)
            {
                wp.WriteBlock(source, nrem);
            }
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
            if (doVerbose) Log.Debug("BlockDefOffset: " + header.BlockDefOffset);
            if (doVerbose) Log.Debug("BlocksOffset: " + header.BlocksOffset);
            if (doVerbose) Log.Debug("BlocksSize: " + header.BlocksSize);

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

        public static byte[] ReadBytes(FileStream fs, int count)
        {
            byte[] bytes = new byte[count];
            fs.Read(bytes, 0, count);
            return bytes;
        }

        public void ReadNCW8()
        {
            Log.Information("Reading NCW 8 bit ...");

            data8 = new sbyte[header.NumSamples * header.Channels];

            int curOffset = 0;
            uint curSample = 0;

            sbyte[][] temp8 = new sbyte[MAX_CHANNELS][];

            for (int i = 0; i < blocksDefArray.Length - 1; i++)
            {
                if (doVerbose) Log.Debug(string.Format("Processing block {0}/{1}...", i + 1, blocksDefArray.Length));

                fs.Seek(header.BlocksOffset + blocksDefArray[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    if (doVerbose) Log.Debug(string.Format("Processing channel {0} @ block {1}...", j + 1, i + 1));

                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    if (doVerbose) Log.Debug(string.Format("Found block-header @ position {0} [base: {1}, bits: {2}, flags: {3} = {4}]", position, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        int nbits = Math.Abs(bHeader.Bits);
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        if (doVerbose) Log.Debug(string.Format("Filling input buffer with compression [nbits: {0}, blockSize: {1}]", nbits, nbits * 64));

                        temp8[j] = new sbyte[NCW_SAMPLES];
                        BitProcess.Fill8(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp8[j], false);
                    }
                    else
                    {
                        int nbits = (bHeader.Bits == 0) ? header.Bits : bHeader.Bits;
                        bool nrelative = bHeader.Bits != 0;
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        if (doVerbose) Log.Debug(string.Format("Filling input buffer no compression [nbits: {0}, blockSize: {1}, nrelative: {2}]", nbits, nbits * 64, nrelative));

                        temp8[j] = new sbyte[NCW_SAMPLES];
                        BitProcess.Fill8(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp8[j], nrelative);
                    }
                }

                if (bHeader.Flags == 1)
                {
                    // Data stored in the block are MID/SIDE
                    if (doVerbose) Log.Debug("Processing MID/SIDE data. Converting to LEFT/RIGHT...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // left
                        data8[curOffset] = (sbyte)(temp8[0][k] + temp8[1][k]);
                        curOffset++;

                        // right
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
                    if (doVerbose) Log.Debug("Processing LEFT/RIGHT data...");

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

                if (doVerbose) Log.Debug(string.Format("Processed block {0}. [CurSample: {1}, CurOffset: {2}, BytePos: {3}]", i + 1, curSample, curOffset, curOffset * 1));
            }
        }

        public void ReadNCW16()
        {
            Log.Information("Reading NCW 16 bit ...");

            data16 = new short[header.NumSamples * header.Channels];

            int curOffset = 0;
            uint curSample = 0;

            short[][] temp16 = new short[MAX_CHANNELS][];

            for (int i = 0; i < blocksDefArray.Length - 1; i++)
            {
                if (doVerbose) Log.Debug(string.Format("Processing block {0}/{1}...", i + 1, blocksDefArray.Length));

                fs.Seek(header.BlocksOffset + blocksDefArray[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    if (doVerbose) Log.Debug(string.Format("Processing channel {0} @ block {1}...", j + 1, i + 1));

                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    if (doVerbose) Log.Debug(string.Format("Found block-header @ position {0} [base: {1}, bits: {2}, flags: {3} = {4}]", position, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        int nbits = Math.Abs(bHeader.Bits);
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        if (doVerbose) Log.Debug(string.Format("Filling input buffer with compression [nbits: {0}, blockSize: {1}]", nbits, nbits * 64));

                        temp16[j] = new short[NCW_SAMPLES];
                        BitProcess.Fill16(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp16[j], false);
                    }
                    else
                    {
                        int nbits = (bHeader.Bits == 0) ? header.Bits : bHeader.Bits;
                        bool nrelative = bHeader.Bits != 0;
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        if (doVerbose) Log.Debug(string.Format("Filling input buffer no compression [nbits: {0}, blockSize: {1}, nrelative: {2}]", nbits, nbits * 64, nrelative));

                        temp16[j] = new short[NCW_SAMPLES];
                        BitProcess.Fill16(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp16[j], nrelative);
                    }
                }

                if (bHeader.Flags == 1)
                {
                    // Data stored in the block are MID/SIDE
                    if (doVerbose) Log.Debug("Processing MID/SIDE data. Converting to LEFT/RIGHT...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // left
                        data16[curOffset] = (short)(temp16[0][k] + temp16[1][k]);
                        curOffset++;

                        // right
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
                    if (doVerbose) Log.Debug("Processing LEFT/RIGHT data...");

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

                if (doVerbose) Log.Debug(string.Format("Processed block {0}. [CurSample: {1}, CurOffset: {2}, BytePos: {3}]", i + 1, curSample, curOffset, curOffset * 2));
            }
        }

        public void ReadNCW24()
        {
            Log.Information("Reading NCW 24 bit ...");

            data24 = new Int24[header.NumSamples * header.Channels];

            int curOffset = 0;
            uint curSample = 0;

            Int24[][] temp24 = new Int24[MAX_CHANNELS][];

            for (int i = 0; i < blocksDefArray.Length - 1; i++)
            {
                if (doVerbose) Log.Debug(string.Format("Processing block {0}/{1}...", i + 1, blocksDefArray.Length));

                fs.Seek(header.BlocksOffset + blocksDefArray[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    if (doVerbose) Log.Debug(string.Format("Processing channel {0} @ block {1}...", j + 1, i + 1));

                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    if (doVerbose) Log.Debug(string.Format("Found block-header @ position {0} [base: {1}, bits: {2}, flags: {3} = {4}]", position, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        // If 'Bits' < 0 then compression is used. 
                        // You get actual bits precision by taking absolute value of 'Bits'. 
                        // This is used for 8, 16, 24, and 32 bits.
                        int nbits = Math.Abs(bHeader.Bits);

                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        if (doVerbose) Log.Debug(string.Format("Filling input buffer with compression [nbits: {0}, blockSize: {1}]", nbits, nbits * 64));

                        temp24[j] = new Int24[NCW_SAMPLES];
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
                        if (doVerbose) Log.Debug(string.Format("Filling input buffer no compression [nbits: {0}, blockSize: {1}, nrelative: {2}]", nbits, nbits * 64, nrelative));

                        temp24[j] = new Int24[NCW_SAMPLES];
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
                    if (doVerbose) Log.Debug("Processing MID/SIDE data. Converting to LEFT/RIGHT...");
                    // MID = (LEFT+RIGHT)/2, SIDE = (LEFT-RIGHT)/2. 
                    // To get LEFT/RIGHT channels, you have to reverse the operation: 
                    // LEFT = MID+SIDE, RIGHT = MID-SIDE.

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // left
                        data24[curOffset] = temp24[0][k] + temp24[1][k];
                        curOffset++;

                        // right
                        data24[curOffset] = temp24[0][k] - temp24[1][k];
                        curOffset++;
                        curSample++;

                        if (curSample >= header.NumSamples)
                            break;
                    }
                }
                else
                {
                    // Data stored in the block are LEFT/RIGHT
                    if (doVerbose) Log.Debug("Processing LEFT/RIGHT data...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        for (int j = 0; j < header.Channels; j++)
                        {
                            data24[curOffset] = temp24[j][k];
                            curOffset++;
                        }
                        curSample++;

                        if (curSample >= header.NumSamples)
                            break;
                    }
                }

                if (doVerbose) Log.Debug(string.Format("Processed block {0}. [CurSample: {1}, CurOffset: {2}, BytePos: {3}]", i + 1, curSample, curOffset, curOffset * 3));
            }
        }

        public void ReadNCW32()
        {
            Log.Information("Reading NCW 32 bit ...");

            datai = new int[header.NumSamples * header.Channels];

            int curOffset = 0;
            uint curSample = 0;

            int[][] temp32 = new int[MAX_CHANNELS][];

            for (int i = 0; i < blocksDefArray.Length - 1; i++)
            {
                if (doVerbose) Log.Debug(string.Format("Processing block {0}/{1}...", i + 1, blocksDefArray.Length));

                fs.Seek(header.BlocksOffset + blocksDefArray[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    if (doVerbose) Log.Debug(string.Format("Processing channel {0} @ block {1}...", j + 1, i + 1));

                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    if (doVerbose) Log.Debug(string.Format("Found block-header @ position {0} [base: {1}, bits: {2}, flags: {3} = {4}]", position, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        int nbits = Math.Abs(bHeader.Bits);
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        if (doVerbose) Log.Debug(string.Format("Filling input buffer with compression [nbits: {0}, blockSize: {1}]", nbits, nbits * 64));

                        temp32[j] = new int[NCW_SAMPLES];
                        BitProcess.Fill32(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp32[j], false);
                    }
                    else
                    {
                        int nbits = (bHeader.Bits == 0) ? header.Bits : bHeader.Bits;
                        bool nrelative = bHeader.Bits != 0;
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        if (doVerbose) Log.Debug(string.Format("Filling input buffer no compression [nbits: {0}, blockSize: {1}, nrelative: {2}]", nbits, nbits * 64, nrelative));

                        temp32[j] = new int[NCW_SAMPLES];
                        BitProcess.Fill32(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, temp32[j], nrelative);
                    }
                }

                if (bHeader.Flags == 1)
                {
                    // Data stored in the block are MID/SIDE
                    if (doVerbose) Log.Debug("Processing MID/SIDE data. Converting to LEFT/RIGHT...");

                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // left
                        datai[curOffset] = temp32[0][k] + temp32[1][k];
                        curOffset++;

                        // right
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
                    if (doVerbose) Log.Debug("Processing LEFT/RIGHT data...");

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

                if (doVerbose) Log.Debug(string.Format("Processed block {0}. [CurSample: {1}, CurOffset: {2}, BytePos: {3}]", i + 1, curSample, curOffset, curOffset * 4));
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
                if (doVerbose) Log.Debug(string.Format("Processing block {0}/{1}...", i + 1, blocksDefArray.Length));

                fs.Seek(header.BlocksOffset + blocksDefArray[i], SeekOrigin.Begin);

                TBlockHeader bHeader = new();
                for (int j = 0; j < header.Channels; j++)
                {
                    if (doVerbose) Log.Debug(string.Format("Processing channel {0} @ block {1}...", j + 1, i + 1));

                    long position = fs.Position;
                    bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
                    if (doVerbose) Log.Debug(string.Format("Found block-header @ position {0} [base: {1}, bits: {2}, flags: {3} = {4}]", position, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        int nbits = Math.Abs(bHeader.Bits);
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        if (doVerbose) Log.Debug(string.Format("Filling input buffer with compression [nbits: {0}, blockSize: {1}]", nbits, nbits * 64));

                        tempi[j] = new int[NCW_SAMPLES];
                        BitProcess.FillIntegersAbs(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, tempi[j]);
                    }

                    else
                    {
                        int nbits = (bHeader.Bits == 0) ? header.Bits : bHeader.Bits;
                        bool nrelative = bHeader.Bits != 0;
                        byte[] inputBuf = ReadBytes(fs, nbits * 64);
                        if (doVerbose) Log.Debug(string.Format("Filling input buffer no compression [nbits: {0}, blockSize: {1}, nrelative: {2}]", nbits, nbits * 64, nrelative));

                        tempi[j] = new int[NCW_SAMPLES];
                        BitProcess.FillIntegers(NCW_SAMPLES, nbits, inputBuf, bHeader.BaseValue, tempi[j], nrelative);
                    }
                }

                if (bHeader.Flags == 1)
                {
                    // Data stored in the block are MID/SIDE
                    if (doVerbose) Log.Debug("Processing MID/SIDE data. Converting to LEFT/RIGHT...");

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
                    if (doVerbose) Log.Debug("Processing LEFT/RIGHT data...");

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

                if (doVerbose) Log.Debug(string.Format("Processed block {0}. [CurSample: {1}, CurOffset: {2}, BytePos: {3}]", i + 1, curSample, curOffset, curOffset * 4));
            }
        }

        public void SaveToNCW(string filename)
        {
            using (fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                byte[] headerBytes = StructureToBytes(header);
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

        public void WriteNCW8(WAVParser.TMyWAVHeader wavHeader)
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

            if (doVerbose) Log.Debug("Processing {0} number of blocks.", nblocks);

            header.BlocksOffset = header.BlockDefOffset + (uint)nblocks * 4;
            if (doVerbose) Log.Debug("Setting initial BlocksOffset: " + header.BlocksOffset);

            ms = new MemoryStream();

            List<int> blocksDefList = new();
            int curBlockOffset = 0;

            sbyte[][] temp8 = new sbyte[header.Channels][];
            sbyte[][] temp8dif = new sbyte[header.Channels][];
            byte[] tempB = new byte[header.Bits * 64];

            for (int j = 0; j < header.Channels; j++)
            {
                temp8[j] = new sbyte[NCW_SAMPLES];
                temp8dif[j] = new sbyte[NCW_SAMPLES];
            }

            for (int curBlockNumber = 0; curBlockNumber < nblocks - 1; curBlockNumber++)
            {
                if (doVerbose) Log.Debug(string.Format("Processing block {0}/{1} at offset: {2}.", curBlockNumber + 1, nblocks, curBlockOffset));

                blocksDefList.Add(curBlockOffset);

                // Fill 512 samples arrays
                for (int i = 0; i < NCW_SAMPLES; i++)
                {
                    for (int j = 0; j < header.Channels; j++)
                    {
                        int curIndex = curBlockNumber * 512 * header.Channels + i * header.Channels + j;
                        if (curIndex < data8.Length)
                        {
                            temp8[j][i] = data8[curIndex];
                        }
                        else
                        {
                            temp8[j][i] = 0;
                        }
                    }
                }

                for (int j = 0; j < header.Channels; j++)
                {
                    DiffArray8(temp8[j], temp8dif[j], out int max, out int min);
                    int nbits = Math.Max(MinBits(min), MinBits(max));

                    TBlockHeader bHeader = new();
                    FillBlockHeader(ref bHeader);

                    bHeader.BaseValue = temp8[j][0];

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

                    if (doVerbose) Log.Debug(string.Format("Encoding 8bit block @ position {0} [blockSize: {1}, base: {2}, bits: {3}, flags: {4} = {5}]", ms.Position, blockSize, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        BitProcess.Encode_8(NCW_SAMPLES, nbits, temp8[j], tempB);
                    }
                    else
                    {
                        BitProcess.Encode_8(NCW_SAMPLES, nbits, temp8dif[j], tempB);
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

            Log.Information(string.Format("Creating NCW file-header [{0} hz, {1} bits, {2} ch, {3} samples]", header.SampleRate, header.Bits, header.Channels, header.NumSamples));
            if (doVerbose) Log.Debug("BlockDefOffset: " + header.BlockDefOffset);
            if (doVerbose) Log.Debug("BlocksOffset: " + header.BlocksOffset);
            if (doVerbose) Log.Debug("BlocksSize: " + header.BlocksSize);
        }

        public void WriteNCW16(WAVParser.TMyWAVHeader wavHeader)
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

            if (doVerbose) Log.Debug("Processing {0} number of blocks.", nblocks);

            header.BlocksOffset = header.BlockDefOffset + (uint)nblocks * 4;
            if (doVerbose) Log.Debug("Setting initial BlocksOffset: " + header.BlocksOffset);

            ms = new MemoryStream();

            List<int> blocksDefList = new();
            int curBlockOffset = 0;

            short[][] temp16 = new short[header.Channels][];
            short[][] temp16dif = new short[header.Channels][];
            byte[] tempB = new byte[header.Bits * 64 * 2];

            for (int j = 0; j < header.Channels; j++)
            {
                temp16[j] = new short[NCW_SAMPLES];
                temp16dif[j] = new short[NCW_SAMPLES];
            }

            for (int curBlockNumber = 0; curBlockNumber < nblocks - 1; curBlockNumber++)
            {
                if (doVerbose) Log.Debug(string.Format("Processing block {0}/{1} at offset: {2}.", curBlockNumber + 1, nblocks, curBlockOffset));

                blocksDefList.Add(curBlockOffset);

                // Fill 512 samples arrays
                for (int i = 0; i < NCW_SAMPLES; i++)
                {
                    for (int j = 0; j < header.Channels; j++)
                    {
                        int curIndex = curBlockNumber * 512 * header.Channels + i * header.Channels + j;
                        if (curIndex < data16.Length)
                        {
                            temp16[j][i] = data16[curIndex];
                        }
                        else
                        {
                            temp16[j][i] = 0;
                        }
                    }
                }

                for (int j = 0; j < header.Channels; j++)
                {
                    DiffArray16(temp16[j], temp16dif[j], out int max, out int min);
                    int nbits = Math.Max(MinBits(min), MinBits(max));

                    TBlockHeader bHeader = new();
                    FillBlockHeader(ref bHeader);

                    bHeader.BaseValue = temp16[j][0];

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

                    if (doVerbose) Log.Debug(string.Format("Encoding 16bit block @ position {0} [blockSize: {1}, base: {2}, bits: {3}, flags: {4} = {5}]", ms.Position, blockSize, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        BitProcess.Encode_16(NCW_SAMPLES, nbits, temp16[j], tempB);
                    }
                    else
                    {
                        BitProcess.Encode_16(NCW_SAMPLES, nbits, temp16dif[j], tempB);
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

            Log.Information(string.Format("Creating NCW file-header [{0} hz, {1} bits, {2} ch, {3} samples]", header.SampleRate, header.Bits, header.Channels, header.NumSamples));
            if (doVerbose) Log.Debug("BlockDefOffset: " + header.BlockDefOffset);
            if (doVerbose) Log.Debug("BlocksOffset: " + header.BlocksOffset);
            if (doVerbose) Log.Debug("BlocksSize: " + header.BlocksSize);
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

                SomeData = new byte[88],

                // example bytes
                // SomeData = new byte[] {
                //     0x00, 0x00, 0x00, 0x00, 0x3E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                //     0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                //     0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                //     0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                //     0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                //     0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                //     0x00, 0x00, 0x00, 0x00, 0x42, 0x01, 0x00, 0x00, 0x04, 0x01, 0x00, 0x00,
                //     0x00, 0x00, 0x00, 0x00
                // },

                // SomeData = new byte[] {
                //     0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x14, 0xFA, 0x4B, 0x11,
                //     0x01, 0x00, 0x00, 0x00, 0x85, 0x16, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
                //     0x14, 0x5A, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x68, 0xC8, 0x10,
                //     0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                //     0x80, 0x67, 0xC8, 0x10, 0x01, 0x00, 0x00, 0x00, 0x82, 0xA1, 0xAA, 0x00,
                //     0x01, 0x00, 0x00, 0x00, 0x7C, 0x67, 0xC8, 0x10, 0x01, 0x00, 0x00, 0x00,
                //     0x14, 0xDA, 0xD4, 0x11, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x67, 0xC8, 0x10,
                //     0x01, 0x00, 0x00, 0x00
                // },

                BlockDefOffset = 120 // 0x78
            };

            int nblocks = wavHeader.numOfPoints / 512 + 1;
            if (wavHeader.numOfPoints % 512 != 0)
                nblocks++;

            if (doVerbose) Log.Debug("Processing {0} number of blocks.", nblocks);

            header.BlocksOffset = header.BlockDefOffset + (uint)nblocks * 4;
            if (doVerbose) Log.Debug("Setting initial BlocksOffset: " + header.BlocksOffset);

            ms = new MemoryStream();

            List<int> blocksDefList = new();
            int curBlockOffset = 0;

            // initialize temporary arrays
            Int24[][] temp24 = new Int24[header.Channels][]; // Left/Right
            Int24[][] temp24dif = new Int24[header.Channels][];

            // TODO: Currently we only support saving to Left/Right even when Mid/Side might compress better
            // Int24[][] temp24MS = new Int24[header.Channels][]; // Mid/Side
            // Int24[][] temp24difMS = new Int24[header.Channels][];

            byte[] tempB = new byte[header.Bits * 64 * 3];

            for (int j = 0; j < header.Channels; j++)
            {
                temp24[j] = new Int24[NCW_SAMPLES];
                temp24dif[j] = new Int24[NCW_SAMPLES];

                // temp24MS[j] = new Int24[NCW_SAMPLES];
                // temp24difMS[j] = new Int24[NCW_SAMPLES];
            }

            for (int curBlockNumber = 0; curBlockNumber < nblocks - 1; curBlockNumber++)
            {
                if (doVerbose) Log.Debug(string.Format("Processing block {0}/{1} at offset: {2}.", curBlockNumber + 1, nblocks, curBlockOffset));

                blocksDefList.Add(curBlockOffset);

                // Fill 512 samples arrays
                for (int i = 0; i < NCW_SAMPLES; i++)
                {
                    for (int j = 0; j < header.Channels; j++)
                    {
                        // fill Left/Right samples
                        int curIndex = curBlockNumber * 512 * header.Channels + i * header.Channels + j;
                        if (curIndex < data24.Length)
                        {
                            temp24[j][i] = data24[curIndex];
                        }
                        else
                        {
                            temp24[j][i] = Int24.Zero;
                        }
                    }

                    // also fill Mid/Side
                    // Int24 left = temp24[0][i];
                    // Int24 right = temp24[1][i];

                    // int mid = (left + right) / 2;
                    // int side = (left - right) / 2;

                    // temp24MS[0][i] = (Int24)mid;
                    // temp24MS[1][i] = (Int24)side;
                }

                // bool useMS = false;
                for (int j = 0; j < header.Channels; j++)
                {
                    // temp24 contains left/right samples, fill the temp diff array with deltas
                    DiffArray24(temp24[j], temp24dif[j], out int max, out int min);
                    int nbits = Math.Max(MinBits(min), MinBits(max));

                    // check whether we can compress to less bits when using mid side and not left/right
                    // DiffArray24(temp24MS[j], temp24difMS[j], out int maxMS, out int minMS);
                    // int nbitsMS = Math.Max(MinBits(minMS), MinBits(maxMS));

                    // if (nbitsMS < nbits)
                    // {
                    //     useMS = true;
                    // }
                    // else
                    // {
                    //     // turn of MS if not already set for this block
                    //     if (!useMS) useMS = false;
                    // }

                    TBlockHeader bHeader = new();
                    FillBlockHeader(ref bHeader);

                    int blockSize;
                    // if (useMS)
                    // {
                    //     // set Mid/Side flag
                    //     bHeader.Flags = 1;
                    //     bHeader.BaseValue = temp24MS[j][0];

                    //     if (nbitsMS >= header.Bits)
                    //     {
                    //         bHeader.Bits = (short)-header.Bits;
                    //         nbitsMS = header.Bits;
                    //     }
                    //     else
                    //     {
                    //         bHeader.Bits = (short)nbitsMS;
                    //     }

                    //     blockSize = nbitsMS * 64;

                    //     if (doVerbose) Log.Debug(string.Format("Encoding 24bit block M/S @ position {0} [blockSize: {1}, base: {2}, bits: {3}, flags: {4} = {5}]", ms.Position, blockSize, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    //     if (bHeader.Bits < 0)
                    //     {
                    //         BitProcess.Encode_24(NCW_SAMPLES, nbitsMS, temp24MS[j], tempB);
                    //     }
                    //     else
                    //     {
                    //         BitProcess.Encode_24(NCW_SAMPLES, nbitsMS, temp24difMS[j], tempB);
                    //     }
                    // }
                    // else
                    // {
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

                    blockSize = nbits * 64;

                    if (doVerbose) Log.Debug(string.Format("Encoding 24bit block @ position {0} [blockSize: {1}, base: {2}, bits: {3}, flags: {4} = {5}]", ms.Position, blockSize, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        BitProcess.Encode_24(NCW_SAMPLES, nbits, temp24[j], tempB);
                    }
                    else
                    {
                        BitProcess.Encode_24(NCW_SAMPLES, nbits, temp24dif[j], tempB);
                    }
                    // }

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

            Log.Information(string.Format("Creating NCW file-header [{0} hz, {1} bits, {2} ch, {3} samples]", header.SampleRate, header.Bits, header.Channels, header.NumSamples));
            if (doVerbose) Log.Debug("BlockDefOffset: " + header.BlockDefOffset);
            if (doVerbose) Log.Debug("BlocksOffset: " + header.BlocksOffset);
            if (doVerbose) Log.Debug("BlocksSize: " + header.BlocksSize);
        }

        public void WriteNCW32(WAVParser.TMyWAVHeader wavHeader)
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

            if (doVerbose) Log.Debug("Processing {0} number of blocks.", nblocks);

            header.BlocksOffset = header.BlockDefOffset + (uint)nblocks * 4;
            if (doVerbose) Log.Debug("Setting initial BlocksOffset: " + header.BlocksOffset);

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
                if (doVerbose) Log.Debug(string.Format("Processing block {0}/{1} at offset: {2}.", curBlockNumber + 1, nblocks, curBlockOffset));

                blocksDefList.Add(curBlockOffset);

                // Fill 512 samples arrays
                for (int i = 0; i < NCW_SAMPLES; i++)
                {
                    for (int j = 0; j < header.Channels; j++)
                    {
                        int curIndex = curBlockNumber * 512 * header.Channels + i * header.Channels + j;
                        if (curIndex < datai.Length)
                        {
                            temp32[j][i] = datai[curIndex];
                        }
                        else
                        {
                            temp32[j][i] = 0;
                        }
                    }
                }

                for (int j = 0; j < header.Channels; j++)
                {
                    DiffArray32(temp32[j], temp32dif[j], out int max, out int min);
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

                    if (doVerbose) Log.Debug(string.Format("Encoding 32bit block @ position {0} [blockSize: {1}, base: {2}, bits: {3}, flags: {4} = {5}]", ms.Position, blockSize, bHeader.BaseValue, bHeader.Bits, bHeader.Flags, bHeader.Flags == 1 ? "mid/side" : "left/right"));

                    if (bHeader.Bits < 0)
                    {
                        BitProcess.Encode_32(NCW_SAMPLES, nbits, temp32[j], tempB);
                    }
                    else
                    {
                        BitProcess.Encode_32(NCW_SAMPLES, nbits, temp32dif[j], tempB);
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

            Log.Information(string.Format("Creating NCW file-header [{0} hz, {1} bits, {2} ch, {3} samples]", header.SampleRate, header.Bits, header.Channels, header.NumSamples));
            if (doVerbose) Log.Debug("BlockDefOffset: " + header.BlockDefOffset);
            if (doVerbose) Log.Debug("BlocksOffset: " + header.BlocksOffset);
            if (doVerbose) Log.Debug("BlocksSize: " + header.BlocksSize);
        }

        private static void FillBlockHeader(ref TBlockHeader bheader)
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
            {
                bits = 2; // If x is zero, minimum bits required is 2 (to represent 0 or 1)
            }
            else if (x > 0)
            {
                bits = 32; // Initialize bits to 32 for positive values of x (assuming 32-bit integers)
                while (bits > 2) // Iterate until minimum bits is greater than 2
                {
                    x <<= 1; // Left shift x by 1 (equivalent to multiplying by 2)
                    if ((x & 0x80000000) != 0) // Check if the most significant bit is set
                    {
                        goto ex; // If the most significant bit is set, go to the label 'ex'
                    }
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
                    if ((x & 0x80000000) == 0) // Check if the most significant bit is not set
                    {
                        goto ex; // If the most significant bit is not set, go to the label 'ex'
                    }
                    bits--; // Decrement bits by 1
                }
            }

        ex:
            return bits; // Return the minimum number of bits required
        }

        // create a delta array from the source array, as well as returning the min and max value
        private static void DiffArray8(sbyte[] sourceArray, sbyte[] diffArray, out int max, out int min)
        {
            // signed 8 - bit value(-128 to + 127) = (-0x80 to 0x7F)
            max = sbyte.MinValue; // -128;
            min = sbyte.MaxValue; // 127

            for (int i = 0; i < sourceArray.Length - 1; i++)
            {
                diffArray[i] = (sbyte)(sourceArray[i + 1] - sourceArray[i]);

                // Update max and min values
                if (diffArray[i] > max)
                    max = diffArray[i];
                if (diffArray[i] < min)
                    min = diffArray[i];
            }

            diffArray[sourceArray.Length - 1] = 0;
        }

        // create a delta array from the source array, as well as returning the min and max value
        private static void DiffArray16(short[] sourceArray, short[] diffArray, out int max, out int min)
        {
            // signed 16 - bit value(-32768 to 32767) = (-0x8000 to 0x7FFF)
            max = short.MinValue; // -32768;
            min = short.MaxValue; // 32767;

            for (int i = 0; i < sourceArray.Length - 1; i++)
            {
                diffArray[i] = (short)(sourceArray[i + 1] - sourceArray[i]);

                // Update max and min values
                if (diffArray[i] > max)
                    max = diffArray[i];
                if (diffArray[i] < min)
                    min = diffArray[i];
            }

            diffArray[sourceArray.Length - 1] = 0;
        }

        // create a delta array from the source array, as well as returning the min and max value
        private static void DiffArray24(Int24[] sourceArray, Int24[] diffArray, out int max, out int min)
        {
            // signed 24 - bit value(-8388608 to 8388607) = (-0x800000 to 0x7FFFFF) 
            max = Int24.MinValue; // -8388608
            min = Int24.MaxValue; // 8388607

            for (int i = 0; i < sourceArray.Length - 1; i++)
            {
                diffArray[i] = sourceArray[i + 1] - sourceArray[i];

                // Update max and min values
                if (diffArray[i] > max)
                    max = diffArray[i];
                if (diffArray[i] < min)
                    min = diffArray[i];
            }

            diffArray[sourceArray.Length - 1] = Int24.Zero;
        }

        // create a delta array from the source array, as well as returning the min and max value
        private static void DiffArray32(int[] sourceArray, int[] diffArray, out int max, out int min)
        {
            // signed 32 - bit value(-2147483648 to + 2147483647) = (-0x80000000 to + 0x7FFFFFFF)
            max = int.MinValue; // -2147483648
            min = int.MaxValue; // 2147483647

            for (int i = 0; i < sourceArray.Length - 1; i++)
            {
                diffArray[i] = sourceArray[i + 1] - sourceArray[i];

                // Update max and min values
                if (diffArray[i] > max)
                    max = diffArray[i];
                if (diffArray[i] < min)
                    min = diffArray[i];
            }

            diffArray[sourceArray.Length - 1] = 0;
        }
    }
}