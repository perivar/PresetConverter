using System.Runtime.InteropServices;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class NCW
    {
        public static bool NCW2Wav(string source, string dest)
        {
            bool result = false;

            // Convert file
            NCWParser ncwParser = new NCWParser();
            ncwParser.Clear();
            ncwParser.OpenNCWFile(source);
            ncwParser.ReadNCW();

            // string tempfile = GetTempFile(dest);

            // if (wavtype == wtStandard)
            //     ncwParser.SaveToWAV(tempfile);
            // else if (wavtype == wtExtended)
            //     ncwParser.SaveToWAVEx(tempfile);
            // else
            // {
            //     if (ncwParser.Header.Channels > 2 ||
            //         ncwParser.Header.Bits > 16 ||
            //         ncwParser.Header.Samplerate > 44100)
            //         ncwParser.SaveToWAVEx(tempfile);
            //     else
            //         ncwParser.SaveToWAV(tempfile);
            // }

            ncwParser = null;

            // Rename temp file
            // string destfile = dest;
            // if (File.Exists(destfile))
            // {
            //     if (isrewrite)
            //         File.Delete(destfile);
            //     else
            //         destfile = GetUniqueFileName(destfile);
            // }

            // File.Move(tempfile, destfile);

            result = true;
            return result;
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

        private FileStream fs;
        private TNCWHeader Header;
        private int[] BlocksDefList;
        private int[] datai;
        private sbyte[] data8;
        private short[] data16;
        private byte[][] data24;

        public void Clear()
        {
            CloseFile();
            datai = new int[0];
            data8 = new sbyte[0];
            data16 = new short[0];
            data24 = new byte[0][];
        }

        public void CloseFile()
        {
            if (fs != null)
            {
                fs.Dispose();
                fs = null;
            }
            BlocksDefList = new int[0];
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
            fs.Read(headerBytes, 0, headerBytes.Length);
            Header = ByteArrayToStructure<TNCWHeader>(headerBytes);

            // check if matches either ncw signature 1 or 2
            for (int i = 0; i < 8; i++)
                if ((Header.Signature[i] != NCW_SIGNATURE1[i]) && (Header.Signature[i] != NCW_SIGNATURE2[i]))
                    throw new Exception("Wrong file signature");

            BlocksDefList = new int[(Header.blocks_offset - Header.block_def_offset) / 4];
            fs.Seek(Header.block_def_offset, SeekOrigin.Begin);
            byte[] blocksDefListBytes = new byte[BlocksDefList.Length * 4];
            fs.Read(blocksDefListBytes, 0, blocksDefListBytes.Length);
            Buffer.BlockCopy(blocksDefListBytes, 0, BlocksDefList, 0, blocksDefListBytes.Length);
        }

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
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

        private static byte[] ReadBytes(FileStream fs, int count)
        {
            byte[] bytes = new byte[count];
            fs.Read(bytes, 0, count);
            return bytes;
        }

        private static IntPtr GetIntPtr(Array array, int index)
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
            fs.Seek(position - Marshal.SizeOf(typeof(TBlockHeader)), SeekOrigin.Begin);
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

            for (int i = 0; i < BlocksDefList.Length - 1; i++)
            {
                fs.Seek(Header.blocks_offset + BlocksDefList[i], SeekOrigin.Begin);
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

            for (int i = 0; i < BlocksDefList.Length - 1; i++)
            {
                fs.Seek(Header.blocks_offset + BlocksDefList[i], SeekOrigin.Begin);
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
            data24 = new byte[Header.numSamples * Header.Channels][];
            for (int i = 0; i < data24.Length; i++)
            {
                data24[i] = new byte[3];
            }

            int curoffset = 0;
            uint cursample = 0;
            IntPtr input_buf = Marshal.AllocHGlobal(Header.Bits * 64);
            int[,] temp24 = new int[MAX_CHANNELS, NCW_SAMPLES * 3];
            int nbits;
            bool nrelative;
            int ti1, ti2, ti3;

            for (int i = 0; i < BlocksDefList.Length - 1; i++)
            {
                fs.Seek(Header.blocks_offset + BlocksDefList[i], SeekOrigin.Begin);
                for (int j = 0; j < Header.Channels; j++)
                {
                    TBlockHeader bHeader = ByteArrayToStructure<TBlockHeader>(ReadBytes(fs, Marshal.SizeOf(typeof(TBlockHeader))));
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

                if (GetBlockHeaderFlags(fs) == 1)
                {
                    for (int k = 0; k < NCW_SAMPLES; k++)
                    {
                        // Considering stereo samples
                        ti1 = (temp24[0, k * 3] + (temp24[0, k * 3 + 1] << 8) + (temp24[0, k * 3 + 2] << 16)) << 8;
                        ti2 = (temp24[1, k * 3] + (temp24[1, k * 3 + 1] << 8) + (temp24[1, k * 3 + 2] << 16)) << 8;
                        ti3 = (ti1 + ti2);
                        data24[curoffset][0] = (byte)((ti3 >> 8) & 0xFF);
                        data24[curoffset][1] = (byte)((ti3 >> 16) & 0xFF);
                        data24[curoffset][2] = (byte)((ti3 >> 24) & 0xFF);
                        curoffset++;

                        ti1 = (temp24[0, k * 3] + (temp24[0, k * 3 + 1] << 8) + (temp24[0, k * 3 + 2] << 16)) << 8;
                        ti2 = (temp24[1, k * 3] + (temp24[1, k * 3 + 1] << 8) + (temp24[1, k * 3 + 2] << 16)) << 8;
                        ti3 = (ti1 - ti2);
                        data24[curoffset][0] = (byte)((ti3 >> 8) & 0xFF);
                        data24[curoffset][1] = (byte)((ti3 >> 16) & 0xFF);
                        data24[curoffset][2] = (byte)((ti3 >> 24) & 0xFF);
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
                            data24[curoffset][0] = (byte)temp24[j, k * 3];
                            data24[curoffset][1] = (byte)temp24[j, k * 3 + 1];
                            data24[curoffset][2] = (byte)temp24[j, k * 3 + 2];
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

            for (int i = 0; i < BlocksDefList.Length - 1; i++)
            {
                fs.Seek(Header.blocks_offset + BlocksDefList[i], SeekOrigin.Begin);
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

            for (int i = 0; i < BlocksDefList.Length - 1; i++)
            {
                fs.Seek(Header.blocks_offset + BlocksDefList[i], SeekOrigin.Begin);
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