using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommonUtils;
using PresetConverter;
using Serilog;

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
            // Steinberg
            public const string SteinbergCompressor = "5B38F28281144FFE80285FF7CCF20483";
            public const string SteinbergDeEsser = "75FD13A528D24880982197D541BC582A";
            public const string SteinbergDistortion = "A990C1062CDE43839ECEF8FE91743DA5";
            public const string SteinbergEQ = "297BA567D83144E1AE921DEF07B41156";
            public const string SteinbergExpander = "2A4C06FF24F14078868891D184CEFB73";
            public const string SteinbergFrequency = "01F6CCC94CAE4668B7C6EC85E681E419";
            public const string SteinbergGrooveAgentONE = "D3F57B09EC6B49998C534F50787A9F86";
            public const string SteinbergGrooveAgentSE = "91585860BA1748E581441ECD96B153ED";
            public const string SteinbergHALionSonicSE = "5B6D6402C5F74C35B3BE88ADF7FC7D27";
            public const string SteinbergMonoDelay = "42A36F8AEE394B98BB2E8B63CB68E3E7";
            public const string SteinbergMultibandCompressor = "86DFC3F5415C40388D3AA69030C380B1";
            public const string SteinbergPingPongDelay = "37A3AA84E3A24D069C39030EC68768E1";
            public const string SteinbergPrologue = "FFF583CCDFB246F894308DB9C5D94C8D";
            public const string SteinbergREVerence = "ED824AB48E0846D5959682F5626D0972";
            public const string SteinbergStandardPanner = "44E1149EDB3E4387BDD827FEA3A39EE7";
            public const string SteinbergStereoDelay = "001DCD3345D14A13B59DAECF75A37536";
            public const string SteinbergStereoEnhancer = "77BBA7CA90F14C9BB298BA9010D6DD78";
            public const string SteinbergStudioEQ = "946051208E29496E804F64A825C8A047";
            public const string SteinbergTremolo = "E97A6873690F40E986F3EE1007B5C8FC";
            public const string SteinbergVSTAmpRack = "04F35DB10F0C47B9965EA7D63B0CCE67";

            // Waves
            public const string WavesAPI2500Mono = "5653544150434D6170692D3235303020";
            public const string WavesC1CompStereo = "565354434D5053633120636F6D702073";
            public const string WavesC4Stereo = "5653544445515363342073746572656F";
            public const string WavesCLAGuitarsStereo = "56535443475453636C61206775697461";
            public const string WavesDoubler2Stereo = "56535457443253646F75626C65723220";
            public const string WavesDoubler4Stereo = "56535457443453646F75626C65723420";
            public const string WavesHDelayStereo = "56535448424453682D64656C61792073";
            public const string WavesKramerTapeStereo = "565354544150536B72616D6572207461";
            public const string WavesL3LLMultiStereo = "565354523350536C332D6C6C206D756C";
            public const string WavesMaseratiACGStereo = "565354544E41536D6173657261746920";
            public const string WavesMaseratiVX1Stereo = "565354544E56536D6173657261746920";
            public const string WavesMetaFlangerStereo = "565354464C4E536D657461666C616E67";
            public const string WavesPuigChild670Stereo = "56535446434853707569676368696C64";
            public const string WavesPuigTecEQP1AStereo = "56535450314153707569677465632065";
            public const string WavesQ10Stereo = "56535445514153713130207374657265";
            public const string WavesQ2Stereo = "5653544551325371322073746572656F";
            public const string WavesRChannelStereo = "565354524E5453726368616E6E656C20";
            public const string WavesRCompressorStereo = "5653545552435372636F6D7072657373";
            public const string WavesREQ6Stereo = "56535452513653726571203620737465";
            public const string WavesRVerbStereo = "56535452524653727665726220737465";
            public const string WavesS1ImagerStereo = "5653544E534853733120696D61676572";
            public const string WavesSSLChannelStereo = "5653545343485373736C6368616E6E65";
            public const string WavesSSLCompStereo = "565354534C435373736C636F6D702073";
            public const string WavesSSLEQMono = "565354534C514D73736C6571206D6F6E";
            public const string WavesSSLEQStereo = "565354534C515373736C657120737465";
            public const string WavesSuperTap2TapsMonoStereo = "5653544D543258737570657274617020";
            public const string WavesSuperTap2TapsStereo = "5653544D543253737570657274617020";
            public const string WavesTrueVerbStereo = "56535454563453747275657665726220";

            // UAD
            public const string UADSSLEChannel = "5653544A3941557561642073736C2065";

            // Native Instruments
            public const string NIKontakt5 = "5653544E694F356B6F6E74616B742035";

            // Fabfilter
            public const string FabFilterProQ = "E45D59E8CB2540FAB0F346E115F8AFD4";
            public const string FabFilterProQx64 = "5653544650517266616266696C746572";
            public const string FabFilterProQ2 = "55FD08E6C00B44A697DA68F61C6FD576";
            public const string FabFilterProQ2x64 = "5653544651327066616266696C746572";
        }

        private class ListElement
        {
            public string ID;
            public long Offset;
            public long Size;
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
                        shortenedString = GetShortenedString(StringValue);
                        return string.Format("[{1}] {0} = {2}", Name, Number, shortenedString);
                    case ParameterType.Bytes:
                        // shortenedString = GetShortenedString(ByteValue);
                        shortenedString = StringUtils.ToHexEditorString(ByteValue);
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

        // byte positions and sizes within a vstpreset (for writing)
        public long ListPos; // position of List chunk
        public long CompDataStartPos; // parameter data start position (Comp)
        public long CompDataChunkSize; // byte length of parameter data (Comp)
        public long ContDataStartPos; // parameter data start position (Cont)
        public long ContDataChunkSize; // byte length of parameter data (Cont)
        public long InfoXmlStartPos; // info xml section start position (Info)
        public long InfoXmlChunkSize; // info xml section length in bytes including BOM (Info)

        /// <summary>
        /// Gets the zero-based position where the chunk data ends (Comp).
        /// </summary>
        public long CompDataEndPosition
        {
            get { return (CompDataStartPos + CompDataChunkSize); }
        }

        /// <summary>
        /// Gets the zero-based position where the chunk data ends (Cont).
        /// </summary>
        public long ContDataEndPosition
        {
            get { return (ContDataStartPos + ContDataChunkSize); }
        }

        /// <summary>
        /// Gets the zero-based position where the chunk data ends (Info).
        /// </summary>
        public long InfoXmlEndPosition
        {
            get { return (InfoXmlStartPos + InfoXmlChunkSize); }
        }

        public VstPreset()
        {

        }

        public VstPreset(string fileName)
        {
            Read(fileName);
        }

        #region Parameter methods
        public void AddParameter(string name, int number, double value)
        {
            Parameters.Add(name, new Parameter(name, (UInt32)number, value));
        }

        public void AddParameter(string name, int number, string value)
        {
            Parameters.Add(name, new Parameter(name, (UInt32)number, value));
        }

        public void AddParameter(string name, int number, byte[] value)
        {
            Parameters.Add(name, new Parameter(name, (UInt32)number, value));
        }

        public double GetNumberParameter(string key)
        {
            if (Parameters.ContainsKey(key)
            && Parameters[key].NumberValue != null)
            {
                return Parameters[key].NumberValue;
            }
            else
            {
                return -1;
            }
        }

        public string GetStringParameter(string key)
        {
            if (Parameters.ContainsKey(key)
            && Parameters[key].StringValue != null)
            {
                return Parameters[key].StringValue;
            }
            else
            {
                return null;
            }
        }

        public byte[] GetByteParameter(string key)
        {
            if (Parameters.ContainsKey(key)
            && Parameters[key].ByteValue != null)
            {
                return Parameters[key].ByteValue;
            }
            else
            {
                return null;
            }
        }

        public bool HasChunkData()
        {
            string key = "ChunkData";
            if (Parameters.ContainsKey(key)
            && Parameters[key].ByteValue != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Store the chunk data
        /// </summary>
        /// <param name="chunkData"></param>
        public void SetChunkData(byte[] chunkData)
        {
            if (!HasChunkData())
            {
                AddParameter("ChunkData", 0, chunkData);
            }
            else
            {
                // parameter already exist
                // warn and overwrite
                Log.Warning(string.Format("{0} bytes of chunk data already exist! Overwriting with new content of {1} bytes ...", GetChunkData().Length, chunkData.Length));
                Parameters["ChunkData"].ByteValue = chunkData;
            }
        }

        /// <summary>
        /// Store the chunk data and wrap in a VstW container   
        /// </summary>
        /// <param name="fxp">fxp content</param>
        public void SetChunkData(FXP fxp)
        {
            if (fxp != null)
            {
                var memStream = new MemoryStream();
                using (BinaryFile bf = new BinaryFile(memStream, BinaryFile.ByteOrder.BigEndian, Encoding.ASCII))
                {
                    bf.Write("VstW");

                    // Write VstW chunk size
                    UInt32 vst2ChunkSize = 8;
                    bf.Write(vst2ChunkSize);

                    // Write VstW chunk version
                    UInt32 vst2Version = 1;
                    bf.Write(vst2Version);

                    // Write VstW bypass
                    UInt32 vst2Bypass = 0;
                    bf.Write(vst2Bypass);

                    fxp.Write(bf);
                }
                this.SetChunkData(memStream.ToArray());
            }
        }

        public byte[] GetChunkData()
        {
            return GetByteParameter("ChunkData");
        }
        #endregion

        public bool Read(string filePath)
        {
            ReadVstPreset(filePath);

            // check that it worked
            if (Parameters.Count == 0)
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
                throw new FileNotFoundException("File Not Found: " + fileName);

            // Read the file
            using (BinaryFile bf = new BinaryFile(fileName, BinaryFile.ByteOrder.LittleEndian, false, Encoding.ASCII))
            {
                // Get file size
                UInt32 fileSize = (UInt32)bf.Length;
                if (fileSize < 64)
                {
                    throw new FormatException("Invalid file size: " + fileSize.ToString());
                }

                // Read file header
                string fileChunkID = bf.ReadString(4);
                if (fileChunkID != "VST3")
                {
                    throw new FormatException("Invalid file type: " + fileChunkID);
                }

                // Read version
                UInt32 fileVersion = bf.ReadUInt32();

                // Read VST3 ID:
                this.Vst3ID = bf.ReadString(32);

                // Read position of 'List' section 
                this.ListPos = (long)bf.ReadUInt64();
                Log.Verbose("listPos: {0}", ListPos);

                // Store current position
                long oldPos = bf.Position;

                // seek to the 'List' position
                // List = kChunkList
                bf.Seek(this.ListPos, SeekOrigin.Begin);

                // read LIST and 4 bytes
                string listElement = bf.ReadString(4);
                UInt32 listElementValue = bf.ReadUInt32();
                Log.Verbose("{0} {1}", listElement, listElementValue);

                // Comp = kComponentState
                // Cont = kControllerState
                // Prog = kProgramData
                // Info = kMetaInfo
                if (listElement.Equals("List"))
                {
                    for (int i = 0; i < listElementValue; i++)
                    {
                        // read Comp and 16 bytes
                        // Comp parameter data start position
                        // Comp parameter data byte length

                        // read Cont and 16 bytes
                        // Cont parameter data start position
                        // Cont parameter data byte length

                        // read Info and 16 bytes
                        // xml start position
                        // byte length of xml data
                        var element = ReadListElement(bf);
                        Log.Verbose("{0} {1} {2}", element.ID, element.Offset, element.Size);

                        if (element.ID.Equals("Comp"))
                        {
                            this.CompDataStartPos = element.Offset;
                            this.CompDataChunkSize = element.Size;
                        }

                        if (element.ID.Equals("Cont"))
                        {
                            this.ContDataStartPos = element.Offset;
                            this.ContDataChunkSize = element.Size;
                        }

                        if (element.ID.Equals("Info"))
                        {
                            this.InfoXmlStartPos = element.Offset;
                            this.InfoXmlChunkSize = element.Size;
                        }
                    }
                }

                // Reset position
                bf.Seek(oldPos, SeekOrigin.Begin);

                // This is where the data start.
                try
                {
                    ReadData(bf, fileSize);
                }
                catch (System.Exception e)
                {
                    Log.Error("Failed reading {0} with Vst3Id: '{1}'. Error: {2}", fileName, Vst3ID, e.Message);
                }

                VerifyListElements(bf);
            }
        }
        public void ReadData(BinaryFile bf, UInt32 fileSize, bool performFileSizeChecks = true)
        {
            // Some presets start with a chunkID here,
            // Others start with the preset content
            string dataChunkID = bf.ReadString(4);
            Log.Verbose("data chunk id: '{0}'", dataChunkID);

            // Single preset?
            bool singlePreset = false;
            if (dataChunkID == "LPXF")
            {
                // Check file size:
                if (fileSize != ((long)this.ListPos + (bf.Position - 4)))
                    throw new FormatException("Invalid file size: " + fileSize);

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
                        throw new FormatException("Invalid file size: " + fileSize);
                }

                // This is most likely a preset bank:
                singlePreset = false;
            }
            else if (dataChunkID == "FabF")
            {
                UInt32 version = bf.ReadUInt32();
                UInt32 nameLength = bf.ReadUInt32();
                var name = bf.ReadString((int)nameLength);
                UInt32 unknown = bf.ReadUInt32();
                UInt32 parameterCount = bf.ReadUInt32();

                Log.Verbose("'{0}', version: {1}, unknown: {2}, param count: {3}", name, version, unknown, parameterCount);

                for (int counter = 0; counter < parameterCount; counter++)
                {
                    var parameterName = string.Format("unknown{0}", counter); // don't have a name
                    var parameterNumber = counter;
                    var parameterNumberValue = bf.ReadSingle();
                    AddParameter(parameterName, parameterNumber, parameterNumberValue);
                }

                // try to read the info xml 
                TryReadInfoXml(bf);

                return;
            }

            // Check for some known fileformats
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
                    this.Vst3ID.Equals(VstIDs.SteinbergStudioEQ) ||
                    this.Vst3ID.Equals(VstIDs.SteinbergTremolo)
                    )
                {
                    // read chunks of 140 bytes until read 19180 bytes (header = 52 bytes)
                    // (19180 + 52) = 19232 bytes
                    while (bf.Position != CompDataEndPosition)
                    {
                        // read the null terminated string
                        var parameterName = bf.ReadStringNull();

                        // read until 128 bytes have been read
                        var ignore = bf.ReadBytes(128 - parameterName.Length - 1);

                        var parameterNumber = (int)bf.ReadUInt32();

                        // Note! For some reason bf.ReadDouble() doesn't work, neither with LittleEndian or BigEndian
                        var parameterNumberValue = BitConverter.ToDouble(bf.ReadBytes(0, 8), 0);

                        AddParameter(parameterName, parameterNumber, parameterNumberValue);
                    }

                    // try to read the info xml 
                    TryReadInfoXml(bf);

                    return;
                }
                else if (
                    this.Vst3ID.Equals(VstIDs.SteinbergGrooveAgentONE))
                {
                    // rewind 4 bytes (seek to data start pos)
                    bf.Seek(this.CompDataStartPos, SeekOrigin.Begin);

                    // read until all bytes have been read
                    var xmlContent = bf.ReadString((int)this.CompDataChunkSize);

                    AddParameter("XmlContent", 1, xmlContent);

                    // try to read the info xml 
                    TryReadInfoXml(bf);

                    return;
                }

                else if (
                    this.Vst3ID.Equals(VstIDs.SteinbergGrooveAgentSE) ||
                    this.Vst3ID.Equals(VstIDs.SteinbergPrologue) ||
                    this.Vst3ID.Equals(VstIDs.SteinbergHALionSonicSE) ||
                    this.Vst3ID.Equals(VstIDs.SteinbergVSTAmpRack)
                    )
                {
                    // rewind 4 bytes (seek to data start pos)
                    bf.Seek(this.CompDataStartPos, SeekOrigin.Begin);

                    // read until all bytes have been read
                    SetChunkData(bf.ReadBytes((int)this.CompDataChunkSize));

                    // try to read the info xml 
                    TryReadInfoXml(bf);

                    return;
                }

                else if (
                    this.Vst3ID.Equals(VstIDs.SteinbergREVerence))
                {
                    // rewind 4 bytes (seek to data start pos)
                    bf.Seek(this.CompDataStartPos, SeekOrigin.Begin);

                    var wavFilePath1 = ReadStringNullAndSkip(bf, Encoding.Unicode, 1024);
                    Log.Verbose("Wave Path 1: {0}", wavFilePath1);
                    AddParameter("wave-file-path-1", 0, wavFilePath1);

                    var wavCount = bf.ReadUInt32();
                    Log.Verbose("numWavFiles: {0}", wavCount);
                    AddParameter("wav-count", 0, wavCount);

                    var unknown = bf.ReadUInt32();
                    Log.Verbose("unknown: {0}", unknown);

                    int parameterCount = -1;
                    if (wavCount > 0)
                    {
                        var wavFilePath2 = ReadStringNullAndSkip(bf, Encoding.Unicode, 1024);
                        AddParameter("wave-file-path-2", 0, wavFilePath2);
                        Log.Verbose("Wave Path 2: {0}", wavFilePath2);

                        var wavFileName = ReadStringNullAndSkip(bf, Encoding.Unicode, 1024);
                        AddParameter("wave-file-name", 0, wavFileName);
                        Log.Verbose("Wav filename: {0}", wavFileName);

                        var imageCount = bf.ReadUInt32();
                        AddParameter("image-count", 0, imageCount);
                        Log.Verbose("Image count: {0}", imageCount);

                        for (int i = 0; i < imageCount; i++)
                        {
                            // images
                            var imagePath = ReadStringNullAndSkip(bf, Encoding.Unicode, 1024);
                            AddParameter("image-file-name-" + (i + 1), 0, imagePath);
                            Log.Verbose("Image {0}: {1}", i + 1, imagePath);
                        }

                        parameterCount = bf.ReadInt32();
                        AddParameter("parameter-count", 0, parameterCount);
                        Log.Verbose("Parameter count: {0}", parameterCount);
                    }

                    int parameterCounter = 0;
                    while (bf.Position != CompDataEndPosition)
                    {
                        parameterCounter++;

                        if (parameterCount > 0 && parameterCounter > parameterCount) break;

                        // read the null terminated string
                        var parameterName = bf.ReadStringNull();
                        Log.Verbose("parameterName: [{0}] {1}", parameterCounter, parameterName);

                        // read until 128 bytes have been read
                        var ignore = bf.ReadBytes(128 - parameterName.Length - 1);

                        var parameterNumber = (int)bf.ReadUInt32();
                        Log.Verbose("parameterNumber: {0}", parameterNumber);

                        // Note! For some reason bf.ReadDouble() doesn't work, neither with LittleEndian or BigEndian
                        var parameterNumberValue = BitConverter.ToDouble(bf.ReadBytes(0, 8), 0);
                        Log.Verbose("parameterNumberValue: {0}", parameterNumberValue);

                        AddParameter(parameterName, parameterNumber, parameterNumberValue);
                    }

                    // try to read the info xml 
                    TryReadInfoXml(bf);

                    return;
                }


                else if (
                   this.Vst3ID.Equals(VstIDs.SteinbergStandardPanner))
                {
                    // rewind 4 bytes (seek to data start pos)
                    bf.Seek(this.CompDataStartPos, SeekOrigin.Begin);

                    // read floats
                    AddParameter("Unknown1", 1, bf.ReadSingle());
                    AddParameter("Unknown2", 2, bf.ReadSingle());

                    // read ints
                    AddParameter("Unknown3", 3, bf.ReadUInt32());
                    AddParameter("Unknown4", 4, bf.ReadUInt32());
                    AddParameter("Unknown5", 5, bf.ReadUInt32());

                    // try to read the info xml 
                    TryReadInfoXml(bf);

                    return;
                }

                else if (
                    this.Vst3ID.Equals(VstIDs.WavesAPI2500Mono) ||
                    this.Vst3ID.Equals(VstIDs.WavesC1CompStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesC4Stereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesCLAGuitarsStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesDoubler2Stereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesDoubler4Stereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesHDelayStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesKramerTapeStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesL3LLMultiStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesMaseratiACGStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesMaseratiVX1Stereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesMetaFlangerStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesPuigChild670Stereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesPuigTecEQP1AStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesQ10Stereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesQ2Stereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesRChannelStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesRCompressorStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesREQ6Stereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesRVerbStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesS1ImagerStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesSSLChannelStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesSSLCompStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesSSLEQMono) ||
                    this.Vst3ID.Equals(VstIDs.WavesSSLEQStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesSuperTap2TapsMonoStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesSuperTap2TapsStereo) ||
                    this.Vst3ID.Equals(VstIDs.WavesTrueVerbStereo))
                {
                    // rewind 4 bytes (seek to data start pos)
                    bf.Seek(this.CompDataStartPos, SeekOrigin.Begin);

                    var unknown2 = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian);
                    var unknown3 = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian);
                    var unknown4 = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian);

                    var presetType = bf.ReadString(4);
                    Log.Verbose("PresetType: {0}", presetType);

                    var setType = bf.ReadString(4);
                    Log.Verbose("SetType: {0}", setType);

                    var xmlMainLength = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian);

                    var xpsID = bf.ReadString(4);
                    if (xpsID.Equals("XPst"))
                    {
                        Log.Verbose("Found XPst content");
                    }
                    else
                    {
                        Log.Warning("XPst content expected. Got '{0}' instead.", xpsID);
                    }

                    var xmlContent = bf.ReadString((int)xmlMainLength);
                    var param1Name = "XmlContent";
                    AddParameter(param1Name, 1, xmlContent);

                    var postType = bf.ReadString(4);
                    Log.Verbose("PostType: {0}", postType);

                    // there is some xml content after the PresetChunkXMLTree chunk
                    // read in this also
                    // total size - PresetChunkXMLTree size - 32
                    // e.g. 844 - 777 - 32 = 35
                    var xmlPostLength = this.CompDataChunkSize - xmlMainLength - 32;
                    var xmlPostContent = bf.ReadString((int)xmlPostLength);
                    var param2Name = "XmlContentPost";
                    AddParameter(param2Name, 2, xmlPostContent);

                    // try to read the info xml 
                    TryReadInfoXml(bf);

                    return;
                }

                else if (this.Vst3ID.Equals(VstIDs.NIKontakt5))
                {
                    // rewind 4 bytes (seek to data start pos)
                    bf.Seek(this.CompDataStartPos, SeekOrigin.Begin);

                    var unknown2 = bf.ReadUInt32(BinaryFile.ByteOrder.LittleEndian);

                    while (bf.Position != CompDataEndPosition)
                    {
                        // read the null terminated string
                        var parameterName = bf.ReadStringNull();

                        // read until 128 bytes have been read
                        var ignore = bf.ReadBytes(128 - parameterName.Length - 1);

                        var parameterNumber = (int)bf.ReadUInt32();

                        // Note! For some reason bf.ReadDouble() doesn't work, neither with LittleEndian or BigEndian
                        var parameterNumberValue = BitConverter.ToDouble(bf.ReadBytes(0, 8), 0);

                        AddParameter(parameterName, parameterNumber, parameterNumberValue);
                    }

                    // try to read the info xml 
                    TryReadInfoXml(bf);

                    return;

                }
                else
                {
                    throw new FormatException("Data does not contain any known formats or FXB or FXP data (1)");
                }
            }

            // OK, getting here we should have access to a fxp/fxb chunk:
            long chunkStart = bf.Position;
            string dataChunkStart = bf.ReadString(4);
            if (dataChunkStart != "CcnK")
            {
                throw new FormatException("Data does not contain any known formats or FXB or FXP data (2) (DataChunkStart: " + dataChunkStart + ")");
            }

            // OK, seems to be a valid fxb or fxp chunk. Get chunk size:
            // add 8 bytes to include all bytes from 'CcnK' and the 4 chunk-size bytes
            UInt32 chunkSize = bf.ReadUInt32(BinaryFile.ByteOrder.BigEndian) + 8;
            if (performFileSizeChecks)
            {
                if ((bf.Position + chunkSize) >= fileSize)
                {
                    throw new FormatException("Invalid chunk size: " + chunkSize);
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
                    throw new FormatException("Header indicates a bank file but data seems to be a preset file (" + magicChunkID + ")");
                }
            }

            // Is a bank?
            else if (magicChunkID == "FxBk" || magicChunkID == "FBCh")
            {
                // Check consistency with the header:
                if (singlePreset == true)
                {
                    throw new FormatException("Header indicates a preset file but data seems to be a bank file (" + magicChunkID + ")");
                }
            }

            // And now for something completely different:
            else
            {
                throw new FormatException("Data does not contain any known formats or FXB or FXP data (3)");
            }

            // Read the source data:
            bf.Position = chunkStart;

            // read until all bytes have been read
            SetChunkData(bf.ReadBytes((int)chunkSize));

            // try to read the info xml 
            TryReadInfoXml(bf);
        }

        private void VerifyListElements(BinaryFile bf)
        {
            if (bf.Position >= bf.Length - 8) return;

            // read LIST and 4 bytes
            string listElement = bf.ReadString(4);
            UInt32 listElementValue = bf.ReadUInt32();
            if (listElement.Equals("List"))
            {
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
                        Log.Error("Expected 'Comp|Cont|Info' but got: {0} {1} {2}", element.ID, element.Offset, element.Size);
                    }
                }
            }
            else
            {
                Log.Error("Expected 'List' but got: {0} {1}", listElement, listElementValue);
            }
        }

        private ListElement ReadListElement(BinaryFile bf)
        {
            //  +----------------------+
            //  | chunk id             |    4 Bytes
            //  | offset to chunk data |    8 Bytes (int64)
            //  | size of chunk data   |    8 Bytes (int64)
            //  +----------------------+ 

            var elem = new ListElement();
            elem.ID = bf.ReadString(4);
            elem.Offset = (long)bf.ReadUInt64(BinaryFile.ByteOrder.LittleEndian);
            elem.Size = (long)bf.ReadUInt64(BinaryFile.ByteOrder.LittleEndian);

            return elem;
        }

        private void TryReadInfoXml(BinaryFile bf)
        {
            // seek to start of meta xml
            SeekToInfoXmlPosition(bf);

            // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
            this.MetaXmlBytesWithBOM = bf.ReadBytes((int)this.InfoXmlChunkSize);
            this.MetaXml = Encoding.UTF8.GetString(this.MetaXmlBytesWithBOM);
        }

        private void SeekToInfoXmlPosition(BinaryFile bf)
        {
            long skipBytes = (this.InfoXmlStartPos - bf.Position);
            if (skipBytes > 0)
            {
                Log.Verbose("Skipping bytes: {0}", skipBytes);

                // seek to start of meta xml
                bf.Seek(this.InfoXmlStartPos, SeekOrigin.Begin);
            }
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

                // Write listPoss
                bf.Write(this.ListPos);

                // Write binary content
                if (HasChunkData())
                {
                    bf.Write(GetChunkData());
                }

                // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
                bf.Write(this.MetaXmlBytesWithBOM);

                // write LIST and 4 bytes
                bf.Write("List");
                bf.Write((UInt32)3);

                // write COMP and 16 bytes
                bf.Write("Comp");
                bf.Write((UInt64)this.CompDataStartPos); // parameter data start position (Comp)
                bf.Write((UInt64)this.CompDataChunkSize); // byte length of parameter data (Comp)

                // write Cont and 16 bytes
                bf.Write("Cont");
                bf.Write((UInt64)this.ContDataStartPos); // parameter data start position (Cont)
                bf.Write((UInt64)this.ContDataChunkSize); // byte length of parameter data (Cont)

                // write Info and 16 bytes
                bf.Write("Info");
                bf.Write((UInt64)this.InfoXmlStartPos); // info xml start position
                bf.Write((UInt64)this.InfoXmlChunkSize); // byte length of info xml data

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
            this.CompDataStartPos = 48; // parameter data start position
            this.CompDataChunkSize = 0;
            if (HasChunkData())
            {
                this.CompDataChunkSize = GetChunkData().Length; // byte length of parameter data 
            }
            this.ContDataStartPos = this.CompDataStartPos + this.CompDataChunkSize;
            this.ContDataChunkSize = 0;
            this.InfoXmlStartPos = this.CompDataStartPos + this.CompDataChunkSize; // xml start position
            this.InfoXmlChunkSize = MetaXmlBytesWithBOM.Length;
            this.ListPos = (this.InfoXmlStartPos + this.MetaXmlBytesWithBOM.Length); // position of List chunk
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

        private static string GetShortenedString(string stringValue)
        {
            return string.Format("'{0}' ...", string.Join(string.Empty, stringValue.Take(100)));
        }

        private static string GetShortenedString(byte[] byteValue)
        {
            return string.Format("'{0}' ...", Encoding.ASCII.GetString(byteValue.Take(100).ToArray()).Replace('\0', ' '));
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

            if (Parameters.Count > 0)
            {
                // output parameters
                foreach (var parameter in Parameters.Values)
                {
                    sb.AppendLine(parameter.ToString());
                }
            }

            if (null != MetaXml) sb.AppendLine(MetaXml);
            return sb.ToString();
        }
    }
}