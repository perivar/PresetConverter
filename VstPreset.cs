using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommonUtils;

namespace AbletonLiveConverter
{
    public class VstPreset
    {
        private class ListElement
        {
            public string Name;
            public UInt64 value1;
            public UInt64 value2;
        }

        public class Parameter
        {
            public string Name;
            public UInt32 Number;
            public double Value;

            public Parameter()
            {

            }

            public Parameter(string name, UInt32 number, double value)
            {
                this.Name = name;
                this.Number = number;
                this.Value = value;
            }

            public override string ToString()
            {
                return string.Format("[{1}] {0} = {2:0.00}", Name, Number, Value);
            }
        }

        public Dictionary<string, Parameter> Parameters = new Dictionary<string, Parameter>();
        public string Vst3ID;
        public string Xml;
        public byte[] FileData;

        public VstPreset()
        {

        }

        public VstPreset(string fileName)
        {
            ReadVstPreset(fileName);
        }

        #region ReadPreset Functions    
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

        private void ReadVstPreset(string fileName)
        {
            // Check file for existence:
            if (!File.Exists(fileName))
                throw new Exception("File Not Found: " + fileName);

            if (fileName.Equals(@"C:\Users\perner\Amazon Drive\Documents\My Projects\Steinberg Media Technologies\Standard Panner\Mono.vstpreset"))
            {
                // break
            }

            // Read the file
            using (BinaryFile br = new BinaryFile(fileName))
            {
                // Get file size:
                UInt32 fileSize = (UInt32)br.Length;
                if (fileSize < 64)
                {
                    throw new Exception("Invalid file size: " + fileSize.ToString());
                }

                // Read file header:
                string chunkID = ReadFourCC(br);
                if (chunkID != "VST3")
                {
                    throw new Exception("Invalid file type: " + chunkID);
                }

                // Read version:
                UInt32 fileVersion = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                // Read VST3 ID:
                this.Vst3ID = new string(br.ReadChars(32));

                UInt32 listPos = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                Console.WriteLine("DEBUG listPos: {0}", listPos);

                // Read unknown value:
                UInt32 unknown1 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                Console.WriteLine("DEBUG unknown1: {0}", unknown1);

                long oldPos = br.Position;

                // seek to the 'List' index
                br.Seek(listPos, SeekOrigin.Begin);

                // read LIST and 4 bytes
                string list = new string(br.ReadChars(4));
                UInt32 listValue = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                Console.WriteLine("DEBUG: {0} {1}", list, listValue);

                ulong xmlStartPos = 0;
                ulong xmlChunkSize = 0;
                if (list.Equals("List"))
                {
                    for (int i = 0; i < listValue; i++)
                    {
                        // read COMP and 16 bytes
                        // parameter data start position
                        // byte length from parameter data start position up until xml data

                        // read Cont and 16 bytes
                        // xml start position
                        // 0 ?

                        // read Info and 16 bytes
                        // xml start position
                        // byte length of xml data
                        var element = ReadListElement(br);
                        Console.WriteLine("DEBUG: {0} {1} {2}", element.Name, element.value1, element.value2);

                        if (element.Name.Equals("Info"))
                        {
                            xmlStartPos = element.value1;
                            xmlChunkSize = element.value2;
                        }
                    }
                }
                if (xmlStartPos == 0) xmlStartPos = (19180 + 52);
                if (xmlChunkSize == 0) xmlChunkSize = 432;

                // reset position
                br.Seek(oldPos, SeekOrigin.Begin);

                // Read data chunk length. i.e. total length minus 4 ('VST3')
                // In some cases this is supposedly the chunk ID
                chunkID = ReadFourCC(br);
                Console.WriteLine("DEBUG: dataChunkID {0}", chunkID);

                // Single preset?
                bool singlePreset = false;
                if (chunkID == "LPXF")
                {
                    // Check file size:
                    if (fileSize != (listPos + (br.Position - 4)))
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
                    if ((fileSize != (listPos + br.Position + 4)) && (fileSize != (listPos + br.Position - 16)))
                        throw new Exception("Invalid file size: " + fileSize);

                    // This is most likely a preset bank:
                    singlePreset = false;
                }

                // Unknown file:
                else
                {
                    // if Frequency preset
                    if (this.Vst3ID.Equals(SteinbergFrequency.Vst3ID))

                    // if (!vst3ID.Equals("D3F57B09EC6B49998C534F50787A9F86") // Groove Agent ONE
                    // && !vst3ID.Equals("91585860BA1748E581441ECD96B153ED") // Groove Agent SE
                    // && !vst3ID.Equals("FFF583CCDFB246F894308DB9C5D94C8D") // Prologue
                    // && !vst3ID.Equals("ED824AB48E0846D5959682F5626D0972") // REVerence
                    // && !vst3ID.Equals("44E1149EDB3E4387BDD827FEA3A39EE7") // Standard Panner
                    // && !vst3ID.Equals("04F35DB10F0C47B9965EA7D63B0CCE67") // VST Amp Rack
                    // )
                    {
                        // read chunks of 140 bytes until read 19180 bytes (header = 52 bytes)
                        // (19180 + 52) = 19232 bytes
                        while (br.Position != (long)xmlStartPos)
                        {
                            var parameter = new Parameter();

                            // read the null terminated string
                            parameter.Name = br.ReadStringZ();

                            // read until 128 bytes have been read
                            var ignore = br.ReadBytes(128 - parameter.Name.Length - 1);

                            parameter.Number = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                            parameter.Value = BitConverter.ToDouble(br.ReadBytes(0, 8, BinaryFile.ByteOrder.LittleEndian), 0);

                            Parameters.Add(parameter.Name, parameter);
                        }

                        // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                        var bytes = br.ReadBytes((int)xmlChunkSize);
                        this.Xml = Encoding.UTF8.GetString(bytes);

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
                        // throw new Exception("This file does not contain any known formats or FXB or FXP data (1)");
                        return;
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
                this.FileData = br.ReadBytes((int)chunkSize);
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

        /// <summary>
        /// Search with an array of bytes to find a specific pattern
        /// </summary>
        /// <param name="array">byte array</param>
        /// <param name="pattern">byte array pattern</param>
        /// <param name="startIndex">index to start searching at</param>
        /// <param name="count">how many elements to look through</param>
        /// <returns>position</returns>
        /// <example>
        /// find the last 'List' entry	
        /// reading all bytes at once is not very performant, but works for these relatively small files	
        /// byte[] allBytes = File.ReadAllBytes(fileName);
        /// reading from the end of the file by reversing the array	
        /// byte[] reversed = allBytes.Reverse().ToArray();
        /// find 'List' backwards	
        /// int reverseIndex = IndexOfBytes(reversed, Encoding.UTF8.GetBytes("tsiL"), 0, reversed.Length);
        /// if (reverseIndex < 0)
        /// {
        /// reverseIndex = 64;
        /// }
        /// int index = allBytes.Length - reverseIndex - 4; // length of List is 4	
        /// Console.WriteLine("DEBUG: File length: {0}, 'List' found at index: {1}", allBytes.Length, index);
        /// </example>
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

        public string BeautifyXml(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            StringWriterWithEncoding stringWriter = new StringWriterWithEncoding(sb, Encoding.UTF8);
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(stringWriter, settings))
            {
                doc.Save(writer);
            }

            // remove whitespace in self closing tags when writing xml document
            var stripSelfClose = sb.ToString().Replace(" />", "/>");
            return stripSelfClose;
        }

        // class to fix the problem of XmlWriter defaulting to utf-16
        // see http://www.csharp411.com/how-to-force-xmlwriter-or-xmltextwriter-to-use-encoding-other-than-utf-16/
        public class StringWriterWithEncoding : StringWriter
        {
            public StringWriterWithEncoding(StringBuilder sb, Encoding encoding)
                : base(sb)
            {
                this.m_Encoding = encoding;
            }
            private readonly Encoding m_Encoding;
            public override Encoding Encoding
            {
                get
                {
                    return this.m_Encoding;
                }
            }
        }


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Vst3ID: {0}\n", Vst3ID);
            foreach (var parameter in Parameters.Values)
            {
                sb.AppendLine(parameter.ToString());
            }

            if (null != Xml) sb.AppendLine(Xml);
            return sb.ToString();
        }
    }
}