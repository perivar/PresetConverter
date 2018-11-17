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
                string vst3ID = new string(br.ReadChars(32));

                UInt32 listPos = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                Console.WriteLine("DEBUG: listPos: {0}", listPos);

                // Read unknown value:
                UInt32 unknown1 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                Console.WriteLine("DEBUG: unknown1 '{0}'", unknown1);

                long oldPos = br.Position;

                // seek to the 'List' index
                br.Seek(listPos, SeekOrigin.Begin);

                // read LIST and 4 bytes
                string list = new string(br.ReadChars(4));
                UInt32 listValue = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                Console.WriteLine("DEBUG: '{0}' {1}", list, listValue);

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

                // Read data chunk ID:
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
                    if (vst3ID.Equals("01F6CCC94CAE4668B7C6EC85E681E419"))

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
                            // read the null terminated string
                            string paramName = br.ReadStringZ();

                            // read until 128 bytes have been read
                            var ignore = br.ReadBytes(128 - paramName.Length - 1);

                            UInt32 paramNumber = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                            var paramValue = BitConverter.ToDouble(br.ReadBytes(0, 8, BinaryFile.ByteOrder.LittleEndian), 0);

                            Console.WriteLine("[{1}] {0} = {2:0.00}", paramName, paramNumber, paramValue);
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

            // Write file header
            br.Write("VST3");

            // Write version
            br.Write((UInt32)1);

            // Write VST3 ID
            br.Write("01F6CCC94CAE4668B7C6EC85E681E419");

            // Write listPos
            UInt32 listPos = 19669; // 19664;
            br.Write(listPos);

            // Write unknown value
            br.Write((UInt32)0);

            // Write data chunk ID
            UInt32 chunkID = 19737 - 4; // 19728; // total length minus 4 ('VST3')
            br.Write(chunkID);

            // write parameters
            // (19180 + 52) = 19232 bytes
            for (int i = 1; i <= 8; i++)
            {
                var band = GetFrequencyBandParameters(i);
                foreach (var bandParameter in band)
                {
                    var paramName = bandParameter.paramName.PadRight(128, '\0').Substring(0, 128);
                    br.Write(paramName);
                    br.Write(bandParameter.paramNumber);
                    br.Write(bandParameter.paramValue);
                }
            }

            var post = GetFrequencyPostParameters();
            foreach (var postParameter in post)
            {
                var paramName = postParameter.paramName.PadRight(128, '\0').Substring(0, 128);
                br.Write(paramName);
                br.Write(postParameter.paramNumber);
                br.Write(postParameter.paramValue);
            }

            // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
            var xmlString = GetFrequencyXml();
            var xmlBytes = Encoding.UTF8.GetBytes(xmlString);
            var xmlBytesBOM = Encoding.UTF8.GetPreamble().Concat(xmlBytes).ToArray();
            br.Write(xmlBytesBOM);
            br.Write("\r\n");

            // write LIST and 4 bytes
            br.Write("List");
            br.Write((UInt32)3);

            // write COMP and 16 bytes
            br.Write("Comp");
            br.Write((UInt64)48); // parameter data start position
            br.Write((UInt64)19184); // byte length from parameter data start position up until xml data

            // write Cont and 16 bytes
            br.Write("Cont");
            br.Write((UInt64)19232); // xml start position
            br.Write((UInt64)0);// ?

            // write Info and 16 bytes
            br.Write("Info");
            br.Write((UInt64)19232); // xml start position
            br.Write((UInt64)xmlBytesBOM.Length); // byte length of xml data

            br.Close();
        }

        class Parameter
        {
            public string paramName;
            public UInt32 paramNumber;
            public double paramValue;

            public Parameter(string paramName, UInt32 paramNumber, double paramValue)
            {
                this.paramName = paramName;
                this.paramNumber = paramNumber;
                this.paramValue = paramValue;
            }
        }

        private List<Parameter> GetFrequencyBandParameters(int bandNumber)
        {
            uint increment = (uint)bandNumber - 1;
            var bandParameters = new List<Parameter>();
            bandParameters.Add(new Parameter(String.Format("equalizerAon{0}", bandNumber), 100 + increment, 1.00));
            bandParameters.Add(new Parameter(String.Format("equalizerAgain{0}", bandNumber), 108 + increment, 0.00));
            bandParameters.Add(new Parameter(String.Format("equalizerAfreq{0}", bandNumber), 116 + increment, 100.00 * bandNumber));
            bandParameters.Add(new Parameter(String.Format("equalizerAq{0}", bandNumber), 124 + increment, 1.00));
            bandParameters.Add(new Parameter(String.Format("equalizerAtype{0}", bandNumber), 132 + increment, bandNumber == 1 || bandNumber == 8 ? 3.0 : 1.0)); // type
            bandParameters.Add(new Parameter(String.Format("invert{0}", bandNumber), 1022 + increment, 0.00));
            bandParameters.Add(new Parameter(String.Format("equalizerAon{0}Ch2", bandNumber), 260 + increment, 1.00));
            bandParameters.Add(new Parameter(String.Format("equalizerAgain{0}Ch2", bandNumber), 268 + increment, 0.00));
            bandParameters.Add(new Parameter(String.Format("equalizerAfreq{0}Ch2", bandNumber), 276 + increment, 25.00));
            bandParameters.Add(new Parameter(String.Format("equalizerAq{0}Ch2", bandNumber), 284 + increment, 1.00));
            bandParameters.Add(new Parameter(String.Format("equalizerAtype{0}Ch2", bandNumber), 292 + increment, 6.00));
            bandParameters.Add(new Parameter(String.Format("invert{0}Ch2", bandNumber), 1030 + increment, 0.00));
            bandParameters.Add(new Parameter(String.Format("equalizerAeditchannel{0}", bandNumber), 50 + increment, 2.00));
            bandParameters.Add(new Parameter(String.Format("equalizerAbandon{0}", bandNumber), 58 + increment, 1.00));
            bandParameters.Add(new Parameter(String.Format("linearphase{0}", bandNumber), 66 + increment, 0.00));
            return bandParameters;
        }

        private List<Parameter> GetFrequencyPostParameters()
        {
            var parameters = new List<Parameter>();
            parameters.Add(new Parameter("equalizerAbypass", 1, 0.00));
            parameters.Add(new Parameter("equalizerAoutput", 2, 0.00));
            parameters.Add(new Parameter("bypass", 1002, 0.00));
            parameters.Add(new Parameter("reset", 1003, 0.00));
            parameters.Add(new Parameter("autoListen", 1005, 0.00));
            parameters.Add(new Parameter("spectrumonoff", 1007, 1.00));
            parameters.Add(new Parameter("spectrum2ChMode", 1008, 0.00));
            parameters.Add(new Parameter("spectrumintegrate", 1010, 40.00));
            parameters.Add(new Parameter("spectrumPHonoff", 1011, 1.00));
            parameters.Add(new Parameter("spectrumslope", 1012, 0.00));
            parameters.Add(new Parameter("draweq", 1013, 1.00));
            parameters.Add(new Parameter("draweqfilled", 1014, 1.00));
            parameters.Add(new Parameter("spectrumbargraph", 1015, 0.00));
            parameters.Add(new Parameter("showPianoRoll", 1019, 1.00));
            parameters.Add(new Parameter("transparency", 1020, 0.30));
            parameters.Add(new Parameter("autoGainOutputValue", 1021, 0.00));
            parameters.Add(new Parameter("", 3, 0.00));
            return parameters;
        }

        private string GetFrequencyXml()
        {
            XmlDocument xml = new XmlDocument();
            XmlNode docNode = xml.CreateXmlDeclaration("1.0", "utf-8", null);
            xml.AppendChild(docNode);
            XmlElement root = xml.CreateElement("MetaInfo");
            xml.AppendChild(root);

            XmlElement attr1 = xml.CreateElement("Attribute");
            attr1.SetAttribute("id", "MediaType");
            attr1.SetAttribute("value", "VstPreset");
            attr1.SetAttribute("type", "string");
            attr1.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr1);

            XmlElement attr2 = xml.CreateElement("Attribute");
            attr2.SetAttribute("id", "PlugInCategory");
            attr2.SetAttribute("value", "Fx|EQ");
            attr2.SetAttribute("type", "string");
            attr2.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr2);

            XmlElement attr3 = xml.CreateElement("Attribute");
            attr3.SetAttribute("id", "PlugInName");
            attr3.SetAttribute("value", "Frequency");
            attr3.SetAttribute("type", "string");
            attr3.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr3);

            XmlElement attr4 = xml.CreateElement("Attribute");
            attr4.SetAttribute("id", "PlugInVendor");
            attr4.SetAttribute("value", "Steinberg Media Technologies");
            attr4.SetAttribute("type", "string");
            attr4.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr4);

            return BeautifyXml(xml);
        }

        public string BeautifyXml(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }
        #endregion
    }
}