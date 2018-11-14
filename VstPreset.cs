using System;
using System.IO;
using System.Text;

namespace AbletonLiveConverter
{
    public class VstPreset
    {
        #region Internal Helper Functions
        #region string ReadFourCC(BinaryReader)
        /// <summary>
        /// Convenience function to read a Fourcc chunk id as a string.
        /// </summary>
        /// <param name="br">
        /// The source binary reader.
        /// </param>
        /// <returns>
        /// The resulting fourcc code as string.
        /// </returns>
        private static string ReadFourCC(BinaryReader br)
        {
            char[] c = new char[4];
            c[0] = (char)br.ReadByte();
            c[1] = (char)br.ReadByte();
            c[2] = (char)br.ReadByte();
            c[3] = (char)br.ReadByte();

            // bool isAscii = c[0] < 127 && c[0] > 31;
            // if (!isAscii)
            // {
            //     br.BaseStream.Seek(-4, SeekOrigin.Current);
            //     UInt32 value = (UInt32)(br.ReadByte() | (br.ReadByte() << 8) | (br.ReadByte() << 16) | (br.ReadByte() << 24));
            //     Console.WriteLine("{0}", value);
            // }

            return new string(c);
        }
        #endregion string ReadFourCC(BinaryReader)

        #region UInt32 ReadUInt32(BinaryReader, bool)
        /// <summary>
        /// Endian independend read of an unsigned 32 bit integer from a stream.
        /// </summary>
        /// <param name="br">
        /// The source binary reader.
        /// </param>
        /// <param name="invert">
        /// Vstpreset files use both little endian and big endian numbers in the same
        /// file. :-( This parameter selects which one is needed depending on the file
        /// position.
        /// </param>
        /// <returns>
        /// The resulting integer.
        /// </returns>
        private static UInt32 ReadUInt32(BinaryReader br, bool invert)
        {
            if (invert)
                return (UInt32)(br.ReadByte() | (br.ReadByte() << 8) | (br.ReadByte() << 16) | (br.ReadByte() << 24));
            else
                return (UInt32)((br.ReadByte() << 24) | (br.ReadByte() << 16) | (br.ReadByte() << 8) | br.ReadByte());
        }
        #endregion UInt32 ReadUInt32(BinaryReader, bool)
        #endregion Internal Helper Functions

        public VstPreset(string fileName)
        {
            ReadVstPreset(fileName);
        }

        public enum ByteOrder : int
        {
            LittleEndian,
            BigEndian
        }

        private BinaryReader binaryReader = null;

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

        private void ReadVstPreset(string fileName)
        {
            // Check file for existance:
            if (!File.Exists(fileName))
                throw new Exception("File Not Found: " + fileName);

            // Read the file:
            using (Stream file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    // Create a binary reader:
                    this.binaryReader = new BinaryReader(file, Encoding.ASCII);

                    // Get file size:
                    UInt32 fileSize = (UInt32)file.Length;
                    if (fileSize < 64)
                        throw new Exception("Invalid file size: " + fileSize.ToString());

                    // Read file header:
                    string chunkID = ReadFourCC(binaryReader);
                    if (chunkID != "VST3")
                        throw new Exception("Invalid file type: " + chunkID);

                    // Read version:
                    UInt32 fileVersion = ReadUInt32(binaryReader, true);

                    // Read VST3 ID:
                    string vst3ID = new string(binaryReader.ReadChars(32));

                    UInt32 fileSize2 = ReadUInt32(binaryReader, true);

                    // Read unknown value:
                    UInt32 unknown1 = ReadUInt32(binaryReader, true);

                    // Read data chunk ID:
                    chunkID = ReadFourCC(binaryReader);

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
                        UInt32 unknown2 = ReadUInt32(binaryReader, true);

                        // Read unknown value (most likely VstW chunk version):
                        UInt32 unknown3 = ReadUInt32(binaryReader, true);

                        // Read unknown value (no clue):
                        UInt32 unknown4 = ReadUInt32(binaryReader, true);

                        // Check file size (The other check is needed because Cubase tends to forget the items of this header:
                        if ((fileSize != (fileSize2 + file.Position + 4)) && (fileSize != (fileSize2 + file.Position - 16)))
                            throw new Exception("Invalid file size: " + fileSize);

                        // This is most likely a preset bank:
                        singlePreset = false;
                    }

                    // Unknown file:
                    else
                    {
                        if (vst3ID.Equals("01F6CCC94CAE4668B7C6EC85E681E419"))
                        {
                            // read chunks of 140 bytes untill read 19040 bytes (header = 52 bytes)
                            while (binaryReader.BaseStream.Position != (19180 + 52))
                            {
                                string parameter = new string(binaryReader.ReadChars(128)).TrimEnd('\0');

                                UInt32 unknown = ReadUInt32(binaryReader, true);

                                var value = BitConverter.ToDouble(ReadBytes(binaryReader, 8, ByteOrder.LittleEndian), 0);

                                Console.WriteLine("{0} {1} {2:0.00}", parameter, unknown, value);
                            }

                            // read another 140 bytes
                            // string dummy = new string(binaryReader.ReadChars(140));

                            // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                            var bytes = binaryReader.ReadBytes(432);
                            var xml = Encoding.UTF8.GetString(bytes);

                            Console.WriteLine("{0}", xml);

                            // read LIST and 4 bytes
                            // read COMP and 16 bytes
                            // read Cont and 16 bytes
                            // read Info and 16 bytes
                        }
                        else
                        {
                            throw new Exception("This file does not contain any known formats or FXB or FXP data (1)");
                        }
                    }

                    // OK, getting here we should have access to a fxp/fxb chunk:
                    long chunkStart = file.Position;
                    chunkID = ReadFourCC(binaryReader);
                    if (chunkID != "CcnK")
                        throw new Exception("This file does not contain any FXB or FXP data (2)");

                    // OK, seems to be a valid fxb or fxp chunk. Get chunk size:
                    UInt32 chunkSize = ReadUInt32(binaryReader, false) + 8;
                    if ((file.Position + chunkSize) >= fileSize)
                        throw new Exception("Invalid chunk size: " + chunkSize);

                    // Read magic value:
                    chunkID = ReadFourCC(binaryReader);

                    // Is a single preset?
                    if (chunkID == "FxCk" || chunkID == "FPCh")
                    {
                        // Check consistency with the header:
                        if (singlePreset == false)
                            throw new Exception("Header indicates a bank file but data seems to be a preset file (" + chunkID + ").");
                    }

                    // Is a bank?
                    else if (chunkID == "FxBk" || chunkID == "FBCh")
                    {
                        // Check consistency with the header:
                        if (singlePreset == true)
                            throw new Exception("Header indicates a preset file but data seems to be a bank file (" + chunkID + ").");
                    }

                    // And now for something completely different:
                    else
                    {
                        throw new Exception("This file does not contain any FXB or FXP data (3)");
                    }

                    // Read the source data:
                    file.Position = chunkStart;
                    byte[] fileData = binaryReader.ReadBytes((int)chunkSize);
                }
                finally
                {
                    // Cleanup:
                    if (file != null)
                        file.Close();
                }
            }
        }

    }
}