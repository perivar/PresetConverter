using System;
using System.IO;
using System.Linq;
using System.Text;
using CommonUtils;

namespace AbletonLiveConverter
{
    public class VstPreset
    {
        class ListElement
        {
            public string Name;
            public UInt64 value1;
            public UInt64 value2;
        }

        /// <summary>
        /// Convenience function to read a FourCC chunk id as a string.
        /// </summary>
        /// <param name="bf">
        /// The source binary reader.
        /// </param>
        /// <returns>
        /// The resulting fourCC code as string.
        /// </returns>
        private static string ReadFourCC(BinaryFile bf)
        {
            byte[] b = new byte[4];
            b[0] = bf.ReadByte();
            b[1] = bf.ReadByte();
            b[2] = bf.ReadByte();
            b[3] = bf.ReadByte();

            // check if this might not be ascii
            bool isAscii = b[0] < 127 && b[0] > 31;
            if (!isAscii)
            {
                UInt32 value = (UInt32)(b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24));
                Console.WriteLine("DEBUG: FourCC not ascii but number: {0}", value);
            }

            return Encoding.Default.GetString(b);
        }

        public VstPreset()
        {

        }

        public VstPreset(string fileName)
        {
            ReadVstPreset(fileName);
        }

        #region ReadPreset Functions    
        private void ReadVstPreset(string fileName)
        {
            // Check file for existence:
            if (!File.Exists(fileName))
                throw new Exception("File Not Found: " + fileName);

            // find the last 'List' entry
            // reading all bytes at once is not very performant, but works for these relatively small files
            byte[] allBytes = File.ReadAllBytes(fileName);
            // reading from the end of the file by reversing the array
            byte[] reversed = allBytes.Reverse().ToArray();
            // find 'List' backwards
            int reverseIndex = IndexOfBytes(reversed, Encoding.UTF8.GetBytes("tsiL"), 0, reversed.Length);
            if (reverseIndex < 0)
            {
                reverseIndex = 64;
            }
            int index = allBytes.Length - reverseIndex - 4; // length of List is 4
            Console.WriteLine("DEBUG: File length: {0}, 'List' found at index: {1}", allBytes.Length, index);

            // Read the file
            using (BinaryFile br = new BinaryFile(allBytes))
            {
                // Get file size:
                UInt32 fileSize = (UInt32)br.Length;
                if (fileSize < 64)
                    throw new Exception("Invalid file size: " + fileSize.ToString());

                // Read file header:
                string chunkID = ReadFourCC(br);
                if (chunkID != "VST3")
                    throw new Exception("Invalid file type: " + chunkID);

                // Read version:
                UInt32 fileVersion = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                // Read VST3 ID:
                string vst3ID = new string(br.ReadChars(32));

                UInt32 fileSize2 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                // Read unknown value:
                UInt32 unknown1 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                long oldPos = br.Position;

                // seek to the 'List' index
                br.Seek(index, SeekOrigin.Begin);

                // read LIST and 4 bytes
                string list = new string(br.ReadChars(4));
                UInt32 listValue = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                Console.WriteLine("DEBUG: '{0}' {1}", list, listValue);

                ulong paramChunkSize = 0;
                ulong xmlChunkSize = 0;
                if (list.Equals("List"))
                {
                    for (int i = 0; i < listValue; i++)
                    {
                        // read COMP and 16 bytes
                        // read Cont and 16 bytes
                        // read Info and 16 bytes
                        var element = ReadListElement(br);
                        Console.WriteLine("DEBUG: {0} {1} {2}", element.Name, element.value1, element.value2);

                        if (element.Name.Equals("Info"))
                        {
                            paramChunkSize = element.value1;
                            xmlChunkSize = element.value2;
                        }
                    }
                }
                if (paramChunkSize == 0) paramChunkSize = (19180 + 52);
                if (xmlChunkSize == 0) xmlChunkSize = 432;

                // reset position
                br.Seek(oldPos, SeekOrigin.Begin);


                // Read data chunk ID:
                chunkID = ReadFourCC(br);

                // Single preset?
                bool singlePreset = false;
                if (chunkID == "LPXF")
                {
                    // Check file size:
                    if (fileSize != (fileSize2 + (br.Position - 4)))
                        throw new Exception("Invalid file size: " + fileSize);

                    // This is most likely a single preset:
                    singlePreset = true;
                }
                else if (chunkID == "VstW")
                {
                    // Read unknown value (most likely VstW chunk size):
                    UInt32 unknown2 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                    // Read unknown value (most likely VstW chunk version):
                    UInt32 unknown3 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                    // Read unknown value (no clue):
                    UInt32 unknown4 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                    // Check file size (The other check is needed because Cubase tends to forget the items of this header:
                    if ((fileSize != (fileSize2 + br.Position + 4)) && (fileSize != (fileSize2 + br.Position - 16)))
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
                        // (19180 + 52) = 19232 bytes
                        while (br.Position != (long)paramChunkSize)
                        {
                            string paramName = new string(br.ReadChars(128)).TrimEnd('\0');
                            UInt32 paramNumber = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                            var paramValue = BitConverter.ToDouble(br.ReadBytes(0, 8, BinaryFile.ByteOrder.LittleEndian), 0);

                            Console.WriteLine("{0} {1} {2:0.00}", paramName, paramNumber, paramValue);
                        }

                        // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                        var bytes = br.ReadBytes((int)xmlChunkSize);
                        var xml = Encoding.UTF8.GetString(bytes);

                        Console.WriteLine("{0}", xml);

                        // read LIST and 4 bytes
                        string listElement = new string(br.ReadChars(4));
                        UInt32 listElementValue = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                        Console.WriteLine("DEBUG: {0} {1}", listElement, listElementValue);

                        if (listElement.Equals("List"))
                        {
                            for (int i = 0; i < listElementValue; i++)
                            {
                                // read COMP and 16 bytes
                                // read Cont and 16 bytes
                                // read Info and 16 bytes
                                var element = ReadListElement(br);
                                Console.WriteLine("DEBUG: {0} {1} {2}", element.Name, element.value1, element.value2);
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
                long chunkStart = br.Position;
                chunkID = ReadFourCC(br);
                if (chunkID != "CcnK")
                {
                    throw new Exception("This file does not contain any FXB or FXP data (2)");
                }

                // OK, seems to be a valid fxb or fxp chunk. Get chunk size:
                UInt32 chunkSize = br.ReadUInt32(BinaryFile.ByteOrder.BigEndian) + 8;
                if ((br.Position + chunkSize) >= fileSize)
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
                br.Position = chunkStart;
                byte[] fileData = br.ReadBytes((int)chunkSize);
            }
        }

        private ListElement ReadListElement(BinaryFile br)
        {
            string name = new string(br.ReadChars(4));
            UInt64 value1 = br.ReadUInt64(BinaryFile.ByteOrder.LittleEndian);
            UInt64 value2 = br.ReadUInt64(BinaryFile.ByteOrder.LittleEndian);

            var elem = new ListElement();
            elem.Name = name;
            elem.value1 = value1;
            elem.value2 = value2;

            return elem;
        }

        public int IndexOfBytes(byte[] array, byte[] pattern, int startIndex, int count)
        {
            if (array == null || array.Length == 0 || pattern == null || pattern.Length == 0 || count == 0)
            {
                return -1;
            }

            int i = startIndex;
            int endIndex = count > 0 ? Math.Min(startIndex + count, array.Length) : array.Length;
            int fidx = 0;
            int lastFidx = 0;

            while (i < endIndex)
            {
                lastFidx = fidx;
                fidx = (array[i] == pattern[fidx]) ? ++fidx : 0;
                if (fidx == pattern.Length)
                {
                    return i - fidx + 1;
                }
                if (lastFidx > 0 && fidx == 0)
                {
                    i = i - lastFidx;
                    lastFidx = 0;
                }
                i++;
            }
            return -1;
        }
        #endregion

        #region WritePreset Functions
        public void Write(string fileName)
        {
            var br = new BinaryFile(fileName, BinaryFile.ByteOrder.LittleEndian, true);

            // Get file size:
            UInt32 fileSize = 10000;

            // Write file header
            br.Write("VST3");

            // Write version
            UInt32 fileVersion = 1;
            br.Write(fileVersion);

            // Write VST3 ID
            string vst3ID = "01F6CCC94CAE4668B7C6EC85E681E419";
            br.Write(vst3ID);

            // Write fileSize2
            UInt32 fileSize2 = 2;
            br.Write(fileSize2);

            // Write unknown value
            UInt32 unknown1 = 0;
            br.Write(unknown1);

            // Write data chunk ID
            UInt32 chunkID = 0;
            br.Write(chunkID);

            // write chunks of 140 bytes until wrote 19180 bytes (header = 52 bytes)
            // (19180 + 52) = 19232 bytes
            ulong paramChunkSize = 19232;
            while (br.Position != (long)paramChunkSize)
            {
                string paramName = "";
                // string paramName = new string(br.ReadChars(128)).TrimEnd('\0');
                UInt32 paramNumber = 0;
                //  = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                // var paramValue = BitConverter.ToDouble(ReadBytes(br, 8, ByteOrder.LittleEndian), 0);
            }

            // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
            // var bytes = br.ReadBytes((int)xmlChunkSize);
            // var xml = Encoding.UTF8.GetString(bytes);

        }
        #endregion
    }
}