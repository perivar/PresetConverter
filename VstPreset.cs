using System;
using System.IO;
using System.Text;

namespace AbletonLiveConverter
{
    public class VstPreset
    {
        public enum ByteOrder : int
        {
            LittleEndian,
            BigEndian
        }

        #region Internal Helper Functions

        /// <summary>
        /// Convenience function to read a FourCC chunk id as a string.
        /// </summary>
        /// <param name="br">
        /// The source binary reader.
        /// </param>
        /// <returns>
        /// The resulting fourCC code as string.
        /// </returns>
        private static string ReadFourCC(BinaryReader br)
        {
            char[] c = new char[4];
            c[0] = (char)br.ReadByte();
            c[1] = (char)br.ReadByte();
            c[2] = (char)br.ReadByte();
            c[3] = (char)br.ReadByte();

            // check if this might not be ascii
            bool isAscii = c[0] < 127 && c[0] > 31;
            if (!isAscii)
            {
                br.BaseStream.Seek(-4, SeekOrigin.Current);
                UInt32 value = (UInt32)(br.ReadByte() | (br.ReadByte() << 8) | (br.ReadByte() << 16) | (br.ReadByte() << 24));
                Console.WriteLine("FourCC not ascii but number: {0}", value);
            }

            return new string(c);
        }

        #region Public Static Read Methods

        public static byte[] ReadBytes(BinaryReader reader, int fieldSize, ByteOrder byteOrder)
        {
            var bytes = new byte[fieldSize];
            if (byteOrder == ByteOrder.LittleEndian)
            {
                return reader.ReadBytes(fieldSize);
            }
            else
            {
                for (int i = fieldSize - 1; i > -1; i--)
                    bytes[i] = reader.ReadByte();
                return bytes;
            }
        }

        public static short ReadInt16(BinaryReader reader, ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
            {
                return reader.ReadInt16();
            }
            else // Big-Endian
            {
                return BitConverter.ToInt16(ReadBytes(reader, 2, ByteOrder.BigEndian), 0);
            }
        }

        public static int ReadInt32(BinaryReader reader, ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
            {
                return reader.ReadInt32();
            }
            else // Big-Endian
            {
                return BitConverter.ToInt32(ReadBytes(reader, 4, ByteOrder.BigEndian), 0);
            }
        }

        public static long ReadInt64(BinaryReader reader, ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
            {
                return reader.ReadInt64();
            }
            else // Big-Endian
            {
                return BitConverter.ToInt64(ReadBytes(reader, 8, ByteOrder.BigEndian), 0);
            }
        }

        public static UInt16 ReadUInt16(BinaryReader reader, ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
            {
                return reader.ReadUInt16();
            }
            else // Big-Endian
            {
                return BitConverter.ToUInt16(ReadBytes(reader, 2, ByteOrder.BigEndian), 0);
            }
        }

        public static UInt32 ReadUInt32(BinaryReader reader, ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
            {
                return reader.ReadUInt32();
            }
            else // Big-Endian
            {
                return BitConverter.ToUInt32(ReadBytes(reader, 4, ByteOrder.BigEndian), 0);
            }
        }

        public static UInt64 ReadUInt64(BinaryReader reader, ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
            {
                return reader.ReadUInt64();
            }
            else // Big-Endian
            {
                return BitConverter.ToUInt64(ReadBytes(reader, 8, ByteOrder.BigEndian), 0);
            }
        }

        public static float ReadSingle(BinaryReader reader, ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
            {
                return reader.ReadSingle();
            }
            else // Big-Endian
            {
                return BitConverter.ToSingle(ReadBytes(reader, 4, ByteOrder.BigEndian), 0);
            }
        }

        public static double ReadDouble(BinaryReader reader, ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
            {
                return reader.ReadDouble();
            }
            else // Big-Endian
            {
                return BitConverter.ToDouble(ReadBytes(reader, 8, ByteOrder.BigEndian), 0);
            }
        }
        #endregion
        #endregion

        public VstPreset(string fileName)
        {
            ReadVstPreset(fileName);
        }

        private void ReadVstPreset(string fileName)
        {
            // Check file for existence:
            if (!File.Exists(fileName))
                throw new Exception("File Not Found: " + fileName);

            // Read the file:
            using (Stream file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    // Create a binary reader:
                    var br = new BinaryReader(file, Encoding.ASCII);

                    // Get file size:
                    UInt32 fileSize = (UInt32)file.Length;
                    if (fileSize < 64)
                        throw new Exception("Invalid file size: " + fileSize.ToString());

                    // Read file header:
                    string chunkID = ReadFourCC(br);
                    if (chunkID != "VST3")
                        throw new Exception("Invalid file type: " + chunkID);

                    // Read version:
                    UInt32 fileVersion = ReadUInt32(br, ByteOrder.LittleEndian);

                    // Read VST3 ID:
                    string vst3ID = new string(br.ReadChars(32));

                    UInt32 fileSize2 = ReadUInt32(br, ByteOrder.LittleEndian);

                    // Read unknown value:
                    UInt32 unknown1 = ReadUInt32(br, ByteOrder.LittleEndian);

                    // Read data chunk ID:
                    chunkID = ReadFourCC(br);

                    // Single preset?
                    bool singlePreset = false;
                    if (chunkID == "LPXF")
                    {
                        // Check file size:
                        if (fileSize != (fileSize2 + (file.Position - 4)))
                            throw new Exception("Invalid file size: " + fileSize);

                        // This is most likely a single preset:
                        singlePreset = true;
                    }
                    else if (chunkID == "VstW")
                    {
                        // Read unknown value (most likely VstW chunk size):
                        UInt32 unknown2 = ReadUInt32(br, ByteOrder.LittleEndian);

                        // Read unknown value (most likely VstW chunk version):
                        UInt32 unknown3 = ReadUInt32(br, ByteOrder.LittleEndian);

                        // Read unknown value (no clue):
                        UInt32 unknown4 = ReadUInt32(br, ByteOrder.LittleEndian);

                        // Check file size (The other check is needed because Cubase tends to forget the items of this header:
                        if ((fileSize != (fileSize2 + file.Position + 4)) && (fileSize != (fileSize2 + file.Position - 16)))
                            throw new Exception("Invalid file size: " + fileSize);

                        // This is most likely a preset bank:
                        singlePreset = false;
                    }

                    // Unknown file:
                    else
                    {
                        // if Frequency preset
                        if (vst3ID.Equals("01F6CCC94CAE4668B7C6EC85E681E419"))
                        {
                            // read chunks of 140 bytes until read 19180 bytes (header = 52 bytes)
                            // = 19232 bytes
                            while (br.BaseStream.Position != (19180 + 52))
                            {
                                string paramName = new string(br.ReadChars(128)).TrimEnd('\0');
                                UInt32 paramNumber = ReadUInt32(br, ByteOrder.LittleEndian);
                                var paramValue = BitConverter.ToDouble(ReadBytes(br, 8, ByteOrder.LittleEndian), 0);

                                Console.WriteLine("{0} {1} {2:0.00}", paramName, paramNumber, paramValue);
                            }

                            // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                            var bytes = br.ReadBytes(432);
                            var xml = Encoding.UTF8.GetString(bytes);

                            Console.WriteLine("{0}", xml);

                            // read LIST and 4 bytes
                            string list = new string(br.ReadChars(4));
                            UInt32 listValue = ReadUInt32(br, ByteOrder.LittleEndian);
                            Console.WriteLine("{0} {1}", list, listValue);

                            if (list.Equals("List"))
                            {
                                for (int i = 0; i < listValue; i++)
                                {
                                    // read COMP and 16 bytes
                                    // read Cont and 16 bytes
                                    // read Info and 16 bytes
                                    ReadListElements(br);
                                }
                            }

                            return;
                        }
                        else
                        {
                            throw new Exception("This file does not contain any known formats or FXB or FXP data (1)");
                        }
                    }

                    // OK, getting here we should have access to a fxp/fxb chunk:
                    long chunkStart = file.Position;
                    chunkID = ReadFourCC(br);
                    if (chunkID != "CcnK")
                    {
                        throw new Exception("This file does not contain any FXB or FXP data (2)");
                    }

                    // OK, seems to be a valid fxb or fxp chunk. Get chunk size:
                    UInt32 chunkSize = ReadUInt32(br, ByteOrder.BigEndian) + 8;
                    if ((file.Position + chunkSize) >= fileSize)
                    {
                        throw new Exception("Invalid chunk size: " + chunkSize);
                    }

                    // Read magic value:
                    chunkID = ReadFourCC(br);

                    // Is a single preset?
                    if (chunkID == "FxCk" || chunkID == "FPCh")
                    {
                        // Check consistency with the header:
                        if (singlePreset == false)
                        {
                            throw new Exception("Header indicates a bank file but data seems to be a preset file (" + chunkID + ").");
                        }
                    }

                    // Is a bank?
                    else if (chunkID == "FxBk" || chunkID == "FBCh")
                    {
                        // Check consistency with the header:
                        if (singlePreset == true)
                        {
                            throw new Exception("Header indicates a preset file but data seems to be a bank file (" + chunkID + ").");
                        }
                    }

                    // And now for something completely different:
                    else
                    {
                        throw new Exception("This file does not contain any FXB or FXP data (3)");
                    }

                    // Read the source data:
                    file.Position = chunkStart;
                    byte[] fileData = br.ReadBytes((int)chunkSize);
                }
                finally
                {
                    // Cleanup:
                    if (file != null)
                        file.Close();
                }
            }
        }

        private void ReadListElements(BinaryReader br)
        {
            string listElement = new string(br.ReadChars(4));
            UInt64 listValue1 = ReadUInt64(br, ByteOrder.LittleEndian);
            UInt64 listValue2 = ReadUInt64(br, ByteOrder.LittleEndian);

            Console.WriteLine("{0} {1} {2}", listElement, listValue1, listValue2);
        }

    }
}