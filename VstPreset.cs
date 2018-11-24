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

        // cannot use Enums with strings, struct works
        public struct VstIDs
        {
            public const string SteinbergCompressor = "5B38F28281144FFE80285FF7CCF20483";
            public const string SteinbergDeEsser = "75FD13A528D24880982197D541BC582A";
            public const string SteinbergDistortion = "A990C1062CDE43839ECEF8FE91743DA5";
            public const string SteinbergEQ = "297BA567D83144E1AE921DEF07B41156";
            public const string SteinbergExpander = "2A4C06FF24F14078868891D184CEFB73";
            public const string SteinbergFrequency = "01F6CCC94CAE4668B7C6EC85E681E419";
            public const string SteinbergGrooveAgentONE = "D3F57B09EC6B49998C534F50787A9F86";
            public const string SteinbergGrooveAgentSE = "91585860BA1748E581441ECD96B153ED";
            public const string SteinbergMonoDelay = "42A36F8AEE394B98BB2E8B63CB68E3E7";
            public const string SteinbergMultibandCompressor = "86DFC3F5415C40388D3AA69030C380B1";
            public const string SteinbergPingPongDelay = "37A3AA84E3A24D069C39030EC68768E1";
            public const string SteinbergPrologue = "FFF583CCDFB246F894308DB9C5D94C8D";
            public const string SteinbergREVerence = "ED824AB48E0846D5959682F5626D0972";
            public const string SteinbergStandardPanner = "44E1149EDB3E4387BDD827FEA3A39EE7";
            public const string SteinbergStereoDelay = "001DCD3345D14A13B59DAECF75A37536";
            public const string SteinbergStudioEQ = "946051208E29496E804F64A825C8A047";
            public const string SteinbergVSTAmpRack = "04F35DB10F0C47B9965EA7D63B0CCE67";
            public const string WavesSSLComp = "565354534C435373736C636F6D702073";

        }

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
            public double NumberValue;
            public string StringValue;
            public byte[] ByteValue;

            public Parameter()
            {

            }

            public Parameter(string name, UInt32 number, double value)
            {
                this.Name = name;
                this.Number = number;
                this.NumberValue = value;
            }

            public override string ToString()
            {
                return string.Format("[{1}] {0} = {2:0.00}", Name, Number, NumberValue);
            }
        }

        public Dictionary<string, Parameter> Parameters = new Dictionary<string, Parameter>();
        public string Vst3ID;
        public string PlugInCategory;
        public string PlugInName;
        public string Xml;
        public byte[] XmlBytesBOM;
        public byte[] FileData;

        // byte positions and sizes within a vstpreset (for writing)
        public UInt32 ListPos; // position of List chunk
        public UInt32 DataChunkSize; // data chunk length. i.e. total length minus 4 ('VST3')
        public UInt64 ParameterDataStartPos; // parameter data start position
        public UInt64 ParameterDataSize; // byte length from parameter data start position up until xml data
        public UInt64 XmlStartPos; // xml start position
        public UInt64 XmlChunkSize; // xml length in bytes (including BOM)

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
            // Check file for existence
            if (!File.Exists(fileName))
                throw new Exception("File Not Found: " + fileName);

            if (fileName.Equals(@"C:\Users\perner\Amazon Drive\Documents\My Projects\Steinberg Media Technologies\Standard Panner\Mono.vstpreset"))
            {
                // break
            }

            // Read the file
            using (BinaryFile br = new BinaryFile(fileName))
            {
                // Get file size
                UInt32 fileSize = (UInt32)br.Length;
                if (fileSize < 64)
                {
                    throw new Exception("Invalid file size: " + fileSize.ToString());
                }

                // Read file header
                string fileChunkID = ReadFourCC(br);
                if (fileChunkID != "VST3")
                {
                    throw new Exception("Invalid file type: " + fileChunkID);
                }

                // Read version
                UInt32 fileVersion = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                // Read VST3 ID:
                this.Vst3ID = new string(br.ReadChars(32));

                this.ListPos = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                Console.WriteLine("DEBUG listPos: {0}", ListPos);

                // Read unknown value
                UInt32 unknown1 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                Console.WriteLine("DEBUG unknown1: {0}", unknown1);

                long oldPos = br.Position;

                // seek to the 'List' index
                br.Seek(this.ListPos, SeekOrigin.Begin);

                // read LIST and 4 bytes
                string list = new string(br.ReadChars(4));
                UInt32 listValue = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                Console.WriteLine("DEBUG: {0} {1}", list, listValue);

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
                            this.XmlStartPos = element.value1;
                            this.XmlChunkSize = element.value2;
                        }

                        if (element.Name.Equals("Comp"))
                        {
                            this.ParameterDataStartPos = element.value1;
                            this.ParameterDataSize = element.value2;
                        }
                    }
                }

                // reset position
                br.Seek(oldPos, SeekOrigin.Begin);

                // Read data chunk length. i.e. total length minus 4 ('VST3')
                // In some cases this is supposedly the chunk ID
                string dataChunkID = ReadFourCC(br);
                Console.WriteLine("DEBUG: dataChunkID {0}", dataChunkID);

                // Single preset?
                bool singlePreset = false;
                if (dataChunkID == "LPXF")
                {
                    // Check file size:
                    if (fileSize != (this.ListPos + (br.Position - 4)))
                        throw new Exception("Invalid file size: " + fileSize);

                    // This is most likely a single preset:
                    singlePreset = true;
                }
                else if (dataChunkID == "VstW")
                {
                    // Read unknown value (most likely VstW chunk size):
                    UInt32 unknown2 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                    // Read unknown value (most likely VstW chunk version):
                    UInt32 unknown3 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                    // Read unknown value (no clue):
                    UInt32 unknown4 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                    // Check file size (The other check is needed because Cubase tends to forget the items of this header:
                    if ((fileSize != (this.ListPos + br.Position + 4)) && (fileSize != (this.ListPos + br.Position - 16)))
                        throw new Exception("Invalid file size: " + fileSize);

                    // This is most likely a preset bank:
                    singlePreset = false;
                }
                else if (dataChunkID == "FabF")
                {
                    // Read unknown value (most likely VstW chunk size):
                    UInt32 unknown2 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                    // Read unknown value (most likely VstW chunk version):
                    UInt32 nameLength = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                    var name = br.ReadString((int)nameLength);
                    UInt32 unknown3 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                    UInt32 unknown4 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                    UInt32 unknown5 = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                    Console.WriteLine("DEBUG: '{0}' {1} {2} {3} {4}", name, unknown2, unknown3, unknown4, unknown5);

                    var counter = 0;
                    while (br.Position != (long)this.XmlStartPos)
                    {
                        counter++;

                        var parameter = new Parameter();
                        parameter.Name = string.Format("unknown{0}", counter); // don't have a name
                        parameter.Number = (UInt32)counter;
                        parameter.NumberValue = br.ReadSingle();
                        Parameters.Add(parameter.Name, parameter);
                    }

                    // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                    var bytes = br.ReadBytes((int)this.XmlChunkSize);
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

                // Unknown file:
                else
                {
                    if (
                        this.Vst3ID.Equals(VstIDs.SteinbergCompressor) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergDeEsser) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergDistortion) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergEQ) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergExpander) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergFrequency) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergMonoDelay) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergMultibandCompressor) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergPingPongDelay) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergStereoDelay) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergStudioEQ))
                    {
                        // read chunks of 140 bytes until read 19180 bytes (header = 52 bytes)
                        // (19180 + 52) = 19232 bytes
                        while (br.Position != (long)this.XmlStartPos)
                        {
                            var parameter = new Parameter();

                            // read the null terminated string
                            parameter.Name = br.ReadStringZ();

                            // read until 128 bytes have been read
                            var ignore = br.ReadBytes(128 - parameter.Name.Length - 1);

                            parameter.Number = br.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);
                            parameter.NumberValue = BitConverter.ToDouble(br.ReadBytes(0, 8, BinaryFile.ByteOrder.LittleEndian), 0);

                            Parameters.Add(parameter.Name, parameter);
                        }

                        // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                        var bytes = br.ReadBytes((int)this.XmlChunkSize);
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
                    else if (
                        this.Vst3ID.Equals(VstIDs.SteinbergGrooveAgentONE))
                    {
                        // rewind 4 bytes
                        br.Seek((long)this.ParameterDataStartPos, SeekOrigin.Begin);

                        // read until all bytes have been read
                        // var xmlContent = br.ReadString((int)this.ParameterDataSize);
                        var xmlContent = br.ReadString((int)this.XmlStartPos - 48);

                        var parameter = new Parameter();
                        parameter.Name = "XmlContent";
                        parameter.StringValue = xmlContent;
                        Parameters.Add(parameter.Name, parameter);

                        // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                        var bytes = br.ReadBytes((int)this.XmlChunkSize);
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

                    else if (
                        this.Vst3ID.Equals(VstIDs.SteinbergGrooveAgentSE) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergREVerence) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergPrologue) ||
                        this.Vst3ID.Equals(VstIDs.SteinbergVSTAmpRack)
                        )
                    {
                        // rewind 4 bytes
                        br.Seek((long)this.ParameterDataStartPos, SeekOrigin.Begin);

                        // read until all bytes have been read
                        var presetContent = br.ReadBytes((int)this.XmlStartPos - 48);

                        var parameter = new Parameter();
                        parameter.Name = "ByteContent";
                        parameter.ByteValue = presetContent;
                        Parameters.Add(parameter.Name, parameter);

                        // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                        var bytes = br.ReadBytes((int)this.XmlChunkSize);
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

                    else if (
                       this.Vst3ID.Equals(VstIDs.SteinbergStandardPanner))
                    {
                        // rewind 4 bytes
                        br.Seek((long)this.ParameterDataStartPos, SeekOrigin.Begin);

                        // read floats
                        Parameters.Add("Unknown1", new Parameter("Unknown1", 1, br.ReadSingle()));
                        Parameters.Add("Unknown2", new Parameter("Unknown2", 2, br.ReadSingle()));

                        // read ints
                        Parameters.Add("Unknown3", new Parameter("Unknown3", 3, br.ReadUInt32()));
                        Parameters.Add("Unknown4", new Parameter("Unknown4", 4, br.ReadUInt32()));
                        Parameters.Add("Unknown5", new Parameter("Unknown5", 5, br.ReadUInt32()));

                        // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                        var bytes = br.ReadBytes((int)this.XmlChunkSize);
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

                    else if (
                       this.Vst3ID.Equals(VstIDs.WavesSSLComp))
                    {
                        // rewind 4 bytes
                        br.Seek((long)this.ParameterDataStartPos, SeekOrigin.Begin);

                        var unknown2 = br.ReadUInt32(BinaryFile.ByteOrder.BigEndian);
                        var unknown3 = br.ReadUInt32(BinaryFile.ByteOrder.BigEndian);
                        var unknown4 = br.ReadUInt32(BinaryFile.ByteOrder.BigEndian);

                        var presetType = ReadFourCC(br);
                        if (presetType.Equals("SLCS"))
                        {
                            Console.WriteLine("DEBUG: Found SLCS content");
                        }

                        var setType = ReadFourCC(br);
                        Console.WriteLine("DEBUG: setType: {0}", setType);

                        var unknown5 = br.ReadUInt32(BinaryFile.ByteOrder.BigEndian);

                        var xpsID = ReadFourCC(br);
                        if (xpsID.Equals("XPst"))
                        {
                            Console.WriteLine("DEBUG: Found XPst content");
                        }

                        var xmlContent = br.ReadString((int)unknown5);
                        var param1 = new Parameter();
                        param1.Name = "XmlContent";
                        param1.Number = 1;
                        param1.StringValue = xmlContent;
                        Parameters.Add(param1.Name, param1);

                        var postType = ReadFourCC(br);
                        Console.WriteLine("DEBUG: postType: {0}", setType);

                        // there is some xml content after the PresetChunkXMLTree chunk
                        // read in this also
                        // total size - PresetChunkXMLTree size - 32
                        // e.g. 844 - 777 - 32 = 35
                        var xmlPostLength = this.ParameterDataSize - unknown5 - 32;
                        var xmlPostContent = br.ReadString((int)xmlPostLength);
                        var param2 = new Parameter();
                        param2.Name = "XmlContentPost";
                        param2.Number = 2;
                        param2.StringValue = xmlPostContent;
                        Parameters.Add(param2.Name, param2);

                        // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                        var bytes = br.ReadBytes((int)this.XmlChunkSize);
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
                string dataChunkStart = ReadFourCC(br);
                if (dataChunkStart != "CcnK")
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
                string magicChunkID = ReadFourCC(br);

                // Is a single preset?
                if (magicChunkID == "FxCk" || magicChunkID == "FPCh")
                {
                    // Check consistency with the header:
                    if (singlePreset == false)
                    {
                        throw new Exception("Header indicates a bank file but data seems to be a preset file (" + fileChunkID + ").");
                    }
                }

                // Is a bank?
                else if (magicChunkID == "FxBk" || magicChunkID == "FBCh")
                {
                    // Check consistency with the header:
                    if (singlePreset == true)
                    {
                        throw new Exception("Header indicates a preset file but data seems to be a bank file (" + fileChunkID + ").");
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

        public void AddParameterToDictionary(string name, UInt32 number, double value)
        {
            var parameter = new Parameter(name, number, value);
            this.Parameters.Add(name, parameter);
        }

        public void Write(string fileName)
        {
            var br = new BinaryFile(fileName, BinaryFile.ByteOrder.LittleEndian, true);

            // Write file header
            br.Write("VST3");

            // Write version
            br.Write((UInt32)1);

            // Write VST3 ID
            br.Write(this.Vst3ID);

            // Write listPos
            br.Write(this.ListPos);

            // Write unknown value
            br.Write((UInt32)0);

            // Write data chunk length. i.e. total length minus 4 ('VST3')
            br.Write(this.DataChunkSize);

            // write parameters
            foreach (var parameter in this.Parameters.Values)
            {
                var paramName = parameter.Name.PadRight(128, '\0').Substring(0, 128);
                br.Write(paramName);
                br.Write(parameter.Number);
                br.Write(parameter.NumberValue);
            }

            // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
            br.Write(this.XmlBytesBOM);

            // write LIST and 4 bytes
            br.Write("List");
            br.Write((UInt32)3);

            // write COMP and 16 bytes
            br.Write("Comp");
            br.Write(this.ParameterDataStartPos); // parameter data start position
            br.Write(this.ParameterDataSize); // byte length from parameter data start position up until xml data

            // write Cont and 16 bytes
            br.Write("Cont");
            br.Write(this.XmlStartPos); // xml start position
            br.Write((UInt64)0);// ?

            // write Info and 16 bytes
            br.Write("Info");
            br.Write(this.XmlStartPos); // xml start position
            br.Write((UInt64)XmlBytesBOM.Length); // byte length of xml data

            br.Close();
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

        public void InitXml()
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
            attr2.SetAttribute("value", this.PlugInCategory);
            attr2.SetAttribute("type", "string");
            attr2.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr2);

            XmlElement attr3 = xml.CreateElement("Attribute");
            attr3.SetAttribute("id", "PlugInName");
            attr3.SetAttribute("value", this.PlugInName);
            attr3.SetAttribute("type", "string");
            attr3.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr3);

            XmlElement attr4 = xml.CreateElement("Attribute");
            attr4.SetAttribute("id", "PlugInVendor");
            attr4.SetAttribute("value", "Steinberg Media Technologies");
            attr4.SetAttribute("type", "string");
            attr4.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr4);

            this.Xml = BeautifyXml(xml);

            // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
            var xmlBytes = Encoding.UTF8.GetBytes(this.Xml);
            this.XmlBytesBOM = Encoding.UTF8.GetPreamble().Concat(xmlBytes).ToArray();
        }

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

            // add \r \n at the end (0D 0A)
            sb.Append("\r\n");

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