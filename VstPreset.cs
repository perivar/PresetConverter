using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommonUtils;
using PresetConverter;

namespace PresetConverter
{
    /// <summary>
    /// A Steinberg .vstpreset file
    /// </summary>
    public abstract class VstPreset : Preset
    {

        /*
        VST3 Preset File Format Definition
        ==================================

        0   +---------------------------+
            | HEADER                    |
            | header id ('VST3')        |       4 Bytes
            | version                   |       4 Bytes (int32)
            | ASCII-encoded class id    |       32 Bytes 
        +--| offset to chunk list      |       8 Bytes (int64)
        |  +---------------------------+
        |  | DATA AREA                 |<-+
        |  | data of chunks 1..n       |  |
        |  ...                       ...  |
        |  |                           |  |
        +->+---------------------------+  |
            | CHUNK LIST                |  |
            | list id ('List')          |  |    4 Bytes
            | entry count               |  |    4 Bytes (int32)
            +---------------------------+  |
            |  1..n                     |  |
            |  +----------------------+ |  |
            |  | chunk id             | |  |    4 Bytes
            |  | offset to chunk data |----+    8 Bytes (int64)
            |  | size of chunk data   | |       8 Bytes (int64)
            |  +----------------------+ |
        EOF +---------------------------+   
        */

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
            public const string SteinbergStereoEnhancer = "77BBA7CA90F14C9BB298BA9010D6DD78";
            public const string SteinbergStudioEQ = "946051208E29496E804F64A825C8A047";
            public const string SteinbergVSTAmpRack = "04F35DB10F0C47B9965EA7D63B0CCE67";
            public const string WavesSSLComp = "565354534C435373736C636F6D702073";
            public const string WavesSSLChannel = "5653545343485373736C6368616E6E65";
            public const string UADSSLEChannel = "5653544A3941557561642073736C2065";
            public const string NIKontakt5 = "5653544E694F356B6F6E74616B742035";
            public const string FabFilterProQ2 = "5653544651327066616266696C746572";
        }

        private class ListElement
        {
            public string ID;
            public UInt64 Offset;
            public UInt64 Size;
        }

        public class Parameter
        {
            public enum ParameterType
            {
                Number,
                String,
                Bytes
            }

            public string Name;
            public UInt32 Number;
            public double NumberValue;
            public string StringValue;
            public byte[] ByteValue;
            public ParameterType Type = ParameterType.Number;

            public Parameter(string name, UInt32 number, double value)
            {
                this.Name = name;
                this.Number = number;
                this.NumberValue = value;
                this.Type = ParameterType.Number;
            }

            public Parameter(string name, UInt32 number, string value)
            {
                this.Name = name;
                this.Number = number;
                this.StringValue = value;
                this.Type = ParameterType.String;
            }

            public Parameter(string name, UInt32 number, byte[] value)
            {
                this.Name = name;
                this.Number = number;
                this.ByteValue = value;
                this.Type = ParameterType.Bytes;
            }

            public override string ToString()
            {
                string shortenedString;
                switch (this.Type)
                {
                    case ParameterType.Number:
                        return string.Format("[{1}] {0} = {2:0.00}", Name, Number, NumberValue);
                    case ParameterType.String:
                        shortenedString = string.Join(string.Empty, StringValue.Take(100));
                        return string.Format("[{1}] {0} = {2}", Name, Number, shortenedString);
                    case ParameterType.Bytes:
                        shortenedString = Encoding.ASCII.GetString(ByteValue.Take(100).ToArray()).Replace('\0', ' ');
                        return string.Format("[{1}] {0} = {2}", Name, Number, shortenedString);
                    default:
                        return string.Format("[{1}] {0} = 'No Values Set']", Name, Number);
                }
            }
        }

        public Dictionary<string, Parameter> Parameters = new Dictionary<string, Parameter>();
        public string Vst3ID;
        public string PlugInCategory;
        public string PlugInName;
        public string PlugInVendor;
        public string MetaXml; // VstPreset MetaInfo Xml section as string
        public byte[] MetaXmlBytesWithBOM; // VstPreset MetaInfo Xml section as bytes, including the BOM
        public byte[] ChunkData;

        // byte positions and sizes within a vstpreset (for writing)
        public UInt64 ListPos; // position of List chunk
        public UInt64 DataStartPos; // data start position
        public UInt64 DataSize; // byte length from data start position up until xml data
        public UInt64 MetaXmlStartPos; // VstPreset MetaInfo Xml section start position
        public UInt64 MetaXmlChunkSize; // VstPreset MetaInfo Xml section length in bytes (including BOM)

        public VstPreset()
        {

        }

        public VstPreset(string fileName)
        {
            Read(fileName);
        }

        public bool Read(string filePath)
        {
            ReadVstPreset(filePath);

            // check that it worked
            if (Parameters.Count == 0 && this.ChunkData == null)
            {
                return false;
            }
            return true;
        }

        public bool Write(string filePath)
        {
            return WritePreset(filePath);
        }

        #region ReadPreset Functions    
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
            using (BinaryFile bf = new BinaryFile(fileName, BinaryFile.ByteOrder.LittleEndian, false, Encoding.ASCII))
            {
                // Get file size
                UInt32 fileSize = (UInt32)bf.Length;
                if (fileSize < 64)
                {
                    throw new Exception("Invalid file size: " + fileSize.ToString());
                }

                // Read file header
                string fileChunkID = bf.ReadString(4);
                if (fileChunkID != "VST3")
                {
                    throw new Exception("Invalid file type: " + fileChunkID);
                }

                // Read version
                UInt32 fileVersion = bf.ReadUInt32();

                // Read VST3 ID:
                this.Vst3ID = bf.ReadString(32);

                // Read position of 'List' section 
                this.ListPos = bf.ReadUInt64();
                Console.WriteLine("DEBUG listPos: {0}", ListPos);

                // Store current position
                long oldPos = bf.Position;

                // seek to the 'List' position
                // List = kChunkList
                bf.Seek((long)this.ListPos, SeekOrigin.Begin);

                // read LIST and 4 bytes
                string listElement = bf.ReadString(4);
                UInt32 listElementValue = bf.ReadUInt32();
                Console.WriteLine("DEBUG: {0} {1}", listElement, listElementValue);

                // Comp = kComponentState
                // Cont = kControllerState
                // Prog = kProgramData
                // Info = kMetaInfo
                if (listElement.Equals("List"))
                {
                    for (int i = 0; i < listElementValue; i++)
                    {
                        // read Comp and 16 bytes
                        // parameter data start position
                        // byte length from parameter data start position up until xml data

                        // read Cont and 16 bytes
                        // xml start position
                        // 0 ?

                        // read Info and 16 bytes
                        // xml start position
                        // byte length of xml data
                        var element = ReadListElement(bf);
                        Console.WriteLine("DEBUG: {0} {1} {2}", element.ID, element.Offset, element.Size);

                        if (element.ID.Equals("Info"))
                        {
                            this.MetaXmlStartPos = element.Offset;
                            this.MetaXmlChunkSize = element.Size;
                        }

                        if (element.ID.Equals("Comp"))
                        {
                            this.DataStartPos = element.Offset;
                            this.DataSize = element.Size;
                        }
                    }
                }

                // Reset position
                bf.Seek(oldPos, SeekOrigin.Begin);

                // This is where the data start.
                ReadData(bf, fileSize);

                // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                var xmlBytes = bf.ReadBytes((int)this.MetaXmlChunkSize);
                this.MetaXml = Encoding.UTF8.GetString(xmlBytes);

                VerifyListElements(bf, Console.Out);
            }
        }
        public void ReadData(BinaryFile bf, UInt32 fileSize, bool performFileSizeChecks = true)
        {
            // Some presets start with a chunkID here,
            // Others start with the preset content
            string dataChunkID = bf.ReadString(4);
            Console.WriteLine("DEBUG: data chunk id: '{0}'", dataChunkID);

            // Single preset?
            bool singlePreset = false;
            if (dataChunkID == "LPXF")
            {
                // Check file size:
                if (fileSize != ((long)this.ListPos + (bf.Position - 4)))
                    throw new Exception("Invalid file size: " + fileSize);

                // This is most likely a single preset:
                singlePreset = true;
            }
            else if (dataChunkID == "VstW")
            {
                // https://searchcode.com/codesearch/view/90021517/

                // Read VstW chunk size
                UInt32 vst2ChunkSize = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian);

                // Read VstW chunk version
                UInt32 vst2Version = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian);

                // Read VstW bypass
                UInt32 vst2Bypass = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian);

                // Check file size (The other check is needed because Cubase tends to forget the items of this header
                if (performFileSizeChecks)
                {
                    if ((fileSize != ((long)this.ListPos + bf.Position + 4))
                    && (fileSize != ((long)this.ListPos + bf.Position - 16)))
                        throw new Exception("Invalid file size: " + fileSize);
                }

                // This is most likely a preset bank:
                singlePreset = false;
            }
            else if (dataChunkID == "FabF")
            {
                // Read unknown value (most likely VstW chunk size):
                UInt32 unknown2 = bf.ReadUInt32();

                // Read unknown value (most likely VstW chunk version):
                UInt32 nameLength = bf.ReadUInt32();

                var name = bf.ReadString((int)nameLength);
                UInt32 unknown3 = bf.ReadUInt32();
                UInt32 unknown4 = bf.ReadUInt32();
                UInt32 unknown5 = bf.ReadUInt32();

                Console.WriteLine("DEBUG: '{0}' {1} {2} {3} {4}", name, unknown2, unknown3, unknown4, unknown5);

                var counter = 0;
                while (bf.Position != (long)this.MetaXmlStartPos)
                {
                    counter++;

                    var parameterName = string.Format("unknown{0}", counter); // don't have a name
                    var parameterNumber = (UInt32)counter;
                    var parameterNumberValue = bf.ReadSingle();
                    AddParameter(parameterName, parameterNumber, parameterNumberValue);
                }

                // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                var bytes = bf.ReadBytes((int)this.MetaXmlChunkSize);
                this.MetaXml = Encoding.UTF8.GetString(bytes);

                VerifyListElements(bf, Console.Out);

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
                    this.Vst3ID.Equals(VstIDs.SteinbergStereoEnhancer) ||
                    this.Vst3ID.Equals(VstIDs.SteinbergStudioEQ))
                {
                    // read chunks of 140 bytes until read 19180 bytes (header = 52 bytes)
                    // (19180 + 52) = 19232 bytes
                    while (bf.Position != (long)this.MetaXmlStartPos)
                    {
                        // read the null terminated string
                        var parameterName = bf.ReadStringNull();

                        // read until 128 bytes have been read
                        var ignore = bf.ReadBytes(128 - parameterName.Length - 1);

                        var parameterNumber = bf.ReadUInt32();

                        // Note! For some reason bf.ReadDouble() doesn't work, neither with LittleEndian or BigEndian
                        var parameterNumberValue = BitConverter.ToDouble(bf.ReadBytes(0, 8), 0);

                        AddParameter(parameterName, parameterNumber, parameterNumberValue);
                    }

                    // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                    var bytes = bf.ReadBytes((int)this.MetaXmlChunkSize);
                    this.MetaXml = Encoding.UTF8.GetString(bytes);

                    VerifyListElements(bf, Console.Out);

                    return;
                }
                else if (
                    this.Vst3ID.Equals(VstIDs.SteinbergGrooveAgentONE))
                {
                    // rewind 4 bytes
                    bf.Seek((long)this.DataStartPos, SeekOrigin.Begin);

                    // read until all bytes have been read
                    // var xmlContent = br.ReadString((int)this.ParameterDataSize);
                    var xmlContent = bf.ReadString((int)this.MetaXmlStartPos - 48);

                    AddParameter("XmlContent", 1, xmlContent);

                    // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                    var bytes = bf.ReadBytes((int)this.MetaXmlChunkSize);
                    this.MetaXml = Encoding.UTF8.GetString(bytes);

                    VerifyListElements(bf, Console.Out);

                    return;
                }

                else if (
                    this.Vst3ID.Equals(VstIDs.SteinbergGrooveAgentSE) ||
                    this.Vst3ID.Equals(VstIDs.SteinbergPrologue) ||
                    this.Vst3ID.Equals(VstIDs.SteinbergVSTAmpRack)
                    )
                {
                    // rewind 4 bytes
                    bf.Seek((long)this.DataStartPos, SeekOrigin.Begin);

                    // read until all bytes have been read
                    var byteContent = bf.ReadBytes((int)this.MetaXmlStartPos - 48);

                    AddParameter("ByteContent", 1, byteContent);

                    // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                    var bytes = bf.ReadBytes((int)this.MetaXmlChunkSize);
                    this.MetaXml = Encoding.UTF8.GetString(bytes);

                    VerifyListElements(bf, Console.Out);

                    return;
                }

                else if (
                    this.Vst3ID.Equals(VstIDs.SteinbergREVerence))
                {
                    // rewind 4 bytes
                    bf.Seek((long)this.DataStartPos, SeekOrigin.Begin);

                    var wavFilePath1 = ReadStringNullAndSkip(bf, Encoding.Unicode, 1024);
                    Console.Out.WriteLine("DEBUG: Wave Path 1: {0}", wavFilePath1);
                    AddParameter("wave-file-path-1", 0, wavFilePath1);

                    var wavCount = bf.ReadUInt32();
                    Console.Out.WriteLine("DEBUG: numWavFiles: {0}", wavCount);
                    AddParameter("wav-count", 0, wavCount);

                    var unknown = bf.ReadUInt32();
                    Console.Out.WriteLine("DEBUG: unknown: {0}", unknown);

                    int parameterCount = -1;
                    if (wavCount > 0)
                    {
                        var wavFilePath2 = ReadStringNullAndSkip(bf, Encoding.Unicode, 1024);
                        AddParameter("wave-file-path-2", 0, wavFilePath2);
                        Console.Out.WriteLine("DEBUG: Wave Path 2: {0}", wavFilePath2);

                        var wavFileName = ReadStringNullAndSkip(bf, Encoding.Unicode, 1024);
                        AddParameter("wave-file-name", 0, wavFileName);
                        Console.Out.WriteLine("DEBUG: Wav filename: {0}", wavFileName);

                        var imageCount = bf.ReadUInt32();
                        AddParameter("image-count", 0, imageCount);
                        Console.Out.WriteLine("DEBUG: Image count: {0}", imageCount);

                        for (int i = 0; i < imageCount; i++)
                        {
                            // images
                            var imagePath = ReadStringNullAndSkip(bf, Encoding.Unicode, 1024);
                            AddParameter("image-file-name-" + (i + 1), 0, imagePath);
                            Console.Out.WriteLine("DEBUG: Image {0}: {1}", i + 1, imagePath);
                        }

                        parameterCount = bf.ReadInt32();
                        AddParameter("parameter-count", 0, parameterCount);
                        Console.Out.WriteLine("DEBUG: Parameter count: {0}", parameterCount);
                    }

                    int parameterCounter = 0;
                    while (bf.Position < (long)this.DataSize)
                    {
                        parameterCounter++;

                        if (parameterCount > 0 && parameterCounter > parameterCount) break;

                        // read the null terminated string
                        var parameterName = bf.ReadStringNull();
                        // Console.Out.WriteLine("parameterName: [{0}] {1}", parameterCounter, parameterName);

                        // read until 128 bytes have been read
                        var ignore = bf.ReadBytes(128 - parameterName.Length - 1);

                        var parameterNumber = bf.ReadUInt32();
                        // Console.Out.WriteLine("parameterNumber: {0}", parameterNumber);

                        // Note! For some reason bf.ReadDouble() doesn't work, neither with LittleEndian or BigEndian
                        var parameterNumberValue = BitConverter.ToDouble(bf.ReadBytes(0, 8), 0);
                        // Console.Out.WriteLine("parameterNumberValue: {0}", parameterNumberValue);

                        AddParameter(parameterName, parameterNumber, parameterNumberValue);
                    }

                    long skipBytes = (long)this.MetaXmlStartPos - bf.Position;
                    Console.Out.WriteLine("DEBUG: Skipping bytes: {0}", skipBytes);

                    // seek to start of meta xml
                    bf.Seek((long)this.MetaXmlStartPos, SeekOrigin.Begin);

                    // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                    var bytes = bf.ReadBytes((int)this.MetaXmlChunkSize);
                    this.MetaXml = Encoding.UTF8.GetString(bytes);

                    VerifyListElements(bf, Console.Out);

                    return;
                }


                else if (
                   this.Vst3ID.Equals(VstIDs.SteinbergStandardPanner))
                {
                    // rewind 4 bytes
                    bf.Seek((long)this.DataStartPos, SeekOrigin.Begin);

                    // read floats
                    AddParameter("Unknown1", 1, bf.ReadSingle());
                    AddParameter("Unknown2", 2, bf.ReadSingle());

                    // read ints
                    AddParameter("Unknown3", 3, bf.ReadUInt32());
                    AddParameter("Unknown4", 4, bf.ReadUInt32());
                    AddParameter("Unknown5", 5, bf.ReadUInt32());

                    // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                    var bytes = bf.ReadBytes((int)this.MetaXmlChunkSize);
                    this.MetaXml = Encoding.UTF8.GetString(bytes);

                    VerifyListElements(bf, Console.Out);

                    return;
                }

                else if (
                   this.Vst3ID.Equals(VstIDs.WavesSSLComp) ||
                   this.Vst3ID.Equals(VstIDs.WavesSSLChannel))
                {
                    // rewind 4 bytes
                    bf.Seek((long)this.DataStartPos, SeekOrigin.Begin);

                    var unknown2 = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian);
                    var unknown3 = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian);
                    var unknown4 = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian);

                    var presetType = bf.ReadString(4);
                    Console.WriteLine("DEBUG: PresetType: {0}", presetType);

                    var setType = bf.ReadString(4);
                    Console.WriteLine("DEBUG: SetType: {0}", setType);

                    var xmlMainLength = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian);

                    var xpsID = bf.ReadString(4);
                    if (xpsID.Equals("XPst"))
                    {
                        Console.WriteLine("DEBUG: Found XPst content");
                    }

                    var xmlContent = bf.ReadString((int)xmlMainLength);
                    var param1Name = "XmlContent";
                    AddParameter(param1Name, 1, xmlContent);

                    var postType = bf.ReadString(4);
                    Console.WriteLine("DEBUG: PostType: {0}", postType);

                    // there is some xml content after the PresetChunkXMLTree chunk
                    // read in this also
                    // total size - PresetChunkXMLTree size - 32
                    // e.g. 844 - 777 - 32 = 35
                    var xmlPostLength = this.DataSize - xmlMainLength - 32;
                    var xmlPostContent = bf.ReadString((int)xmlPostLength);
                    var param2Name = "XmlContentPost";
                    AddParameter(param2Name, 2, xmlPostContent);

                    // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                    var bytes = bf.ReadBytes((int)this.MetaXmlChunkSize);
                    this.MetaXml = Encoding.UTF8.GetString(bytes);

                    VerifyListElements(bf, Console.Out);

                    return;
                }

                else if (this.Vst3ID.Equals(VstIDs.NIKontakt5))
                {
                    // rewind 4 bytes
                    bf.Seek((long)this.DataStartPos, SeekOrigin.Begin);

                    var unknown2 = bf.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                    while (bf.Position != (long)this.MetaXmlStartPos)
                    {
                        // read the null terminated string
                        var parameterName = bf.ReadStringNull();

                        // read until 128 bytes have been read
                        var ignore = bf.ReadBytes(128 - parameterName.Length - 1);

                        var parameterNumber = bf.ReadUInt32();

                        // Note! For some reason bf.ReadDouble() doesn't work, neither with LittleEndian or BigEndian
                        var parameterNumberValue = BitConverter.ToDouble(bf.ReadBytes(0, 8), 0);

                        AddParameter(parameterName, parameterNumber, parameterNumberValue);
                    }
                    return;

                }
                else
                {
                    // throw new Exception("This file does not contain any known formats or FXB or FXP data (1)");
                    Console.WriteLine("This file does not contain any known formats or FXB or FXP data (1)");
                    return;
                }
            }

            // OK, getting here we should have access to a fxp/fxb chunk:
            long chunkStart = bf.Position;
            string dataChunkStart = bf.ReadString(4);
            if (dataChunkStart != "CcnK")
            {
                throw new Exception("This file does not contain any FXB or FXP data (2)");
            }

            // OK, seems to be a valid fxb or fxp chunk. Get chunk size:
            // add 8 bytes to include all bytes from 'CcnK' and the 4 chunk-size bytes
            UInt32 chunkSize = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian) + 8;
            if (performFileSizeChecks)
            {
                if ((bf.Position + chunkSize) >= fileSize)
                {
                    throw new Exception("Invalid chunk size: " + chunkSize);
                }
            }

            // Read magic value:
            string magicChunkID = bf.ReadString(4);

            // Is a single preset?
            if (magicChunkID == "FxCk" || magicChunkID == "FPCh")
            {
                // Check consistency with the header:
                if (singlePreset == false)
                {
                    throw new Exception("Header indicates a bank file but data seems to be a preset file (" + magicChunkID + ").");
                }
            }

            // Is a bank?
            else if (magicChunkID == "FxBk" || magicChunkID == "FBCh")
            {
                // Check consistency with the header:
                if (singlePreset == true)
                {
                    throw new Exception("Header indicates a preset file but data seems to be a bank file (" + magicChunkID + ").");
                }
            }

            // And now for something completely different:
            else
            {
                throw new Exception("This file does not contain any FXB or FXP data (3)");
            }

            // Read the source data:
            bf.Position = chunkStart;
            this.ChunkData = bf.ReadBytes((int)chunkSize);
        }

        private void VerifyListElements(BinaryFile bf, TextWriter writer)
        {
            if (bf.Position >= bf.Length - 8) return;

            // read LIST and 4 bytes
            string listElement = bf.ReadString(4);
            UInt32 listElementValue = bf.ReadUInt32();
            if (listElement.Equals("List"))
            {
                // writer.WriteLine("DEBUG: {0} {1}", listElement, listElementValue);
                for (int i = 0; i < listElementValue; i++)
                {
                    // read Comp and 16 bytes
                    // read Cont and 16 bytes
                    // read Info and 16 bytes
                    var element = ReadListElement(bf);
                    if (!element.ID.Equals("Comp")
                    && !element.ID.Equals("Cont")
                    && !element.ID.Equals("Info"))
                    {
                        writer.WriteLine("ERROR - expected 'Comp|Cont|Info' but got: {0} {1} {2}", element.ID, element.Offset, element.Size);
                    }
                    // writer.WriteLine("DEBUG: {0} {1} {2}", element.ID, element.Offset, element.Size);
                }
            }
            else
            {
                writer.WriteLine("ERROR - expected 'List' but got: {0} {1}", listElement, listElementValue);
            }
        }

        private ListElement ReadListElement(BinaryFile bf)
        {
            //  +----------------------+
            //  | chunk id             |    4 Bytes
            //  | offset to chunk data |    8 Bytes (int64)
            //  | size of chunk data   |    8 Bytes (int64)
            //  +----------------------+ 

            string id = bf.ReadString(4);
            UInt64 offset = bf.ReadUInt64(BinaryFile.ByteOrder.LittleEndian);
            UInt64 size = bf.ReadUInt64(BinaryFile.ByteOrder.LittleEndian);

            var elem = new ListElement();
            elem.ID = id;
            elem.Offset = offset;
            elem.Size = size;

            return elem;
        }

        private string ReadStringNullAndSkip(BinaryFile bf, Encoding encoding, long totalBytes)
        {
            long posBeforeNull = bf.Position;
            var text = bf.ReadStringNull(Encoding.Unicode);
            long posAfterNull = bf.Position;
            long bytesToSkip = totalBytes - (posAfterNull - posBeforeNull);
            var ignore = bf.ReadBytes((int)bytesToSkip);
            return text;
        }

        private void AddParameter(string name, UInt32 number, double value)
        {
            Parameters.Add(name, new Parameter(name, number, value));
        }

        private void AddParameter(string name, UInt32 number, string value)
        {
            Parameters.Add(name, new Parameter(name, number, value));
        }

        private void AddParameter(string name, UInt32 number, byte[] value)
        {
            Parameters.Add(name, new Parameter(name, number, value));
        }
        #endregion

        #region WritePreset Functions    
        private bool WritePreset(string fileName)
        {
            if (PreparedForWriting())
            {
                var bf = new BinaryFile(fileName, BinaryFile.ByteOrder.LittleEndian, true, Encoding.ASCII);

                // Write file header
                bf.Write("VST3");

                // Write version
                bf.Write((UInt32)1);

                // Write VST3 ID
                bf.Write(this.Vst3ID);

                // Write listPos
                bf.Write(this.ListPos);

                // Write binary content
                bf.Write(this.ChunkData);

                // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                bf.Write(this.MetaXmlBytesWithBOM);

                // write LIST and 4 bytes
                bf.Write("List");
                bf.Write((UInt32)3);

                // write COMP and 16 bytes
                bf.Write("Comp");
                bf.Write(this.DataStartPos); // parameter data start position
                bf.Write(this.DataSize); // byte length from parameter data start position up until xml data

                // write Cont and 16 bytes
                bf.Write("Cont");
                bf.Write(this.MetaXmlStartPos); // xml start position
                bf.Write((UInt64)0);// ?

                // write Info and 16 bytes
                bf.Write("Info");
                bf.Write(this.MetaXmlStartPos); // xml start position
                bf.Write((UInt64)MetaXmlBytesWithBOM.Length); // byte length of xml data

                bf.Close();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Calculate byte positions and sizes within the vstpreset (for writing)
        /// </summary>
        public void CalculateBytePositions()
        {
            // Frequency:
            // ListPos = 19664; // position of List chunk
            // DataStartPos = 48; // parameter data start position
            // DataSize = 19184; // byte length from parameter data start position up until xml data
            // MetaXmlStartPos = 19232; // xml start position

            // Compressor:
            // ListPos = 2731; // position of List chunk
            // DataStartPos = 48; // parameter data start position
            // DataSize = 2244; // byte length from parameter data start position up until xml data
            // MetaXmlStartPos = 2292; // xml start position

            DataStartPos = 48; // parameter data start position
            DataSize = (ulong)this.ChunkData.Length; // byte length from parameter data start position up until xml data
            MetaXmlStartPos = this.DataStartPos + this.DataSize; // xml start position
            ListPos = (uint)(this.MetaXmlStartPos + (ulong)this.MetaXmlBytesWithBOM.Length); // position of List chunk
        }

        /// <summary>
        /// Initialize VstPreset MetaInfo Xml Section as both string and bytes array (including the Byte Order Mark)    
        /// </summary>
        public void InitMetaInfoXml()
        {
            XmlDocument xml = new XmlDocument();
            // Adding the XmlDeclaration (version and utf-8) is not necessary as it is added  
            // using the XmlWriterSettings
            // XmlNode docNode = xml.CreateXmlDeclaration("1.0", "utf-8", null);
            // xml.AppendChild(docNode);
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
            attr4.SetAttribute("value", this.PlugInVendor);
            attr4.SetAttribute("type", "string");
            attr4.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr4);

            this.MetaXml = BeautifyXml(xml);

            // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
            var xmlBytes = Encoding.UTF8.GetBytes(this.MetaXml);
            this.MetaXmlBytesWithBOM = Encoding.UTF8.GetPreamble().Concat(xmlBytes).ToArray();
        }

        public string BeautifyXml(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            StringWriterWithEncoding stringWriter = new StringWriterWithEncoding(sb, Encoding.UTF8);
            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
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

            // ugly way to remove whitespace in self closing tags when writing xml document
            sb.Replace(" />", "/>");

            return sb.ToString();
        }

        /// <summary>
        /// Ensure all variables are ready and populated before writing the preset
        /// I.e. the binary content (ChunkData, MetaXmlBytesWithBOM etc.) 
        /// and the calculated positions (ListPos etc.)
        /// </summary>
        /// <returns>true if ready</returns>
        protected abstract bool PreparedForWriting();

        #endregion

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Vst3ID: {0}\n", Vst3ID);
            foreach (var parameter in Parameters.Values)
            {
                sb.AppendLine(parameter.ToString());
            }

            if (null != MetaXml) sb.AppendLine(MetaXml);
            return sb.ToString();
        }
    }
}