/* 
	REVerenceVSTPresetGenerator 
	Copyright  Per Ivar Nerseth 2011 
	Version 1.6
    http://192.168.10.159/backup/
*/
using System;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

// for the command line library
using System.Collections.Specialized;
using CommandLine.Utility;
using CommonUtils;

class Script
{
    static string _version = "1.6";

    //static string _wavDir = @"F:\IMPULSE RESPONSES\Bricasti M7 reverb unit\Samplicity M7 Main - 04 - Wave Quad files, 32 bit, 48 Khz, v1.1";
    //static string _wavDir = @"C:\Users\perivar.nerseth\Music\impulse responses\Altiverb";
    static string _wavDir = "";

    //static string _presetDir = @"C:\Documents and Settings\Administrator\Mine dokumenter\VST3 Presets\Steinberg Media Technologies\REVerence\";
    //static string _presetDir = @"C:\Users\perivar.nerseth\Documents\CSScripts\VSTPresets";
    static string _presetDir = "";

    //static string _imageFilePath = @"F:\IMPULSE RESPONSES\Bricasti M7 reverb unit\Bricasti M7.JPG";
    //static string _imageFilePath = @"C:\Users\perivar.nerseth\Music\impulse responses\Bricasti M7.JPG";
    static string _imageFilePath = "";
    static bool _altiverbImageMode = false;

    // the mediaFamily is generated when you first create a preset in REVerence for a unit
    static string _mediaFamily = ""; // example: "Bricasti M7 reverb unit"
    static string _mediaFamilyGUID = ""; // example: "1D0302ADFF6241C4B96D2C36B8E02B7E" 

    static string _vstPresetPrefixText = ""; // example: "Bricasti-"
    static string _vstPresetSuffixText = ""; // example: "-48khz";

    // This is the GUID for REverence Reverb Plugin
    static string _vst3UniqueID = "ED824AB48E0846D5959682F5626D0972";

    static string _scriptDir = Environment.CurrentDirectory;
    static bool _automatic_mediaRoomType = true;
    static string _mediaRoomType = "";

    // some impulses have similar names but lives in different folders, then prefix vst name with parent-folder-name
    static int _prefixWithParentFolderLevels = 0;

    public static void PrintUsage()
    {
        Console.WriteLine("STEINBERG REVERENCE VST PRESET GENERATOR. Version {0}.", _version);
        Console.WriteLine("Copyright (C) 2009-2011 Per Ivar Nerseth.");
        Console.WriteLine();
        Console.WriteLine("Usage: cscs <Script Name>.cs <Arguments>");
        Console.WriteLine();
        Console.WriteLine("Mandatory Arguments:");
        Console.WriteLine("\t-wavdir=<directory path to the impulse responses (.wav)>");
        Console.WriteLine("\t-presetdir=<path to the directory where the vstpresets will be saved>");
        Console.WriteLine("\t-mediafamily=<mediafamily, i.e. the impulse response origins\n\t\te.g 'Bricasti M7 reverb unit'>");
        Console.WriteLine();
        Console.WriteLine("Optional Arguments:");
        Console.WriteLine("\t-image=<path to the image to use in _ALL_ the \n\t\tgenerated .vstpresets (.jpg|.bmp|.gif|.png)\n\t\tIf omitted, no image will be used, unless altiverb mode.>");
        Console.WriteLine("\t-altiverb - Use automatic altiverb image detection.");
        Console.WriteLine("\t-prefix=<text to prefix the vstpreset names, e.g. 'Bricasti-'>");
        Console.WriteLine("\t-suffix=<text to suffix the vstpreset names, e.g. '-48khz'>");
        Console.WriteLine("\t-mediafamilyguid=<mediafamily GUID (global unique id)\n\t\te.g '1D0302ADFF6241C4B96D2C36B8E02B7E'.\n\t\tIf omitted, the GUID will be generated.>");
        Console.WriteLine("\t-prefixparentlevels - Number of levels to prefix vstpreset filename with parent folder name.");
    }

    static public void CommandLine(string[] args)
    {

        // Command line parsing
        Arguments CommandLine = new Arguments(args);
        if (CommandLine["wavdir"] != null)
        {
            _wavDir = CommandLine["wavdir"];
        }
        if (CommandLine["presetdir"] != null)
        {
            _presetDir = CommandLine["presetdir"];
        }
        if (CommandLine["image"] != null)
        {
            _imageFilePath = CommandLine["image"];
        }
        if (CommandLine["prefix"] != null)
        {
            _vstPresetPrefixText = CommandLine["prefix"];
        }
        if (CommandLine["suffix"] != null)
        {
            _vstPresetSuffixText = CommandLine["suffix"];
        }
        if (CommandLine["mediafamily"] != null)
        {
            _mediaFamily = CommandLine["mediafamily"];
        }
        if (CommandLine["mediafamilyguid"] != null)
        {
            _mediaFamilyGUID = CommandLine["mediafamilyguid"];
        }
        if (CommandLine["altiverb"] != null)
        {
            _altiverbImageMode = true;
        }
        if (CommandLine["prefixparentlevels"] != null)
        {
            _prefixWithParentFolderLevels = Int32.Parse(CommandLine["prefixparentlevels"]);
        }
        if (_wavDir == "" || _presetDir == "" || _mediaFamily == "")
        {
            PrintUsage();
            return;
        }
        if (_mediaFamilyGUID == "")
        {
            // generate GUID
            _mediaFamilyGUID = System.Guid.NewGuid().ToString("N").ToUpper();
            Console.WriteLine("Generated media family GUID: {0}", _mediaFamilyGUID);
        }

        try
        {
            // read wave directory    
            string[] wavFilePaths = Directory.GetFiles(_wavDir, "*.wav", SearchOption.AllDirectories);
            foreach (string wavFilePath in wavFilePaths)
            {
                _mediaRoomType = ""; // reset the automatically deteceted media room type
                string imageFile = ""; // reset imageFile
                string fileName = Path.GetFileNameWithoutExtension(wavFilePath);

                if (_prefixWithParentFolderLevels > 0)
                {
                    fileName = GetParentPrefix(fileName, new DirectoryInfo(wavFilePath), _prefixWithParentFolderLevels);
                    Console.Out.WriteLine("Prefixing with parent folder name(s): {0}", fileName);
                }
                Console.WriteLine("Processing: {0}", fileName);

                //string wavFilePath = 'F:\\IMPULSE RESPONSES\\Bricasti M7 reverb unit\\Samplicity M7 Main - 04 - Wave Quad files, 32 bit, 48 Khz, v1.1\\' + wav
                string wavFileName = Path.GetFileName(wavFilePath);
                Console.WriteLine("Impulse Response (Wav) File: {0}", wavFileName);

                //string vstPresetFilePath = 'C:\\Documents and Settings\\Administrator\\Mine dokumenter\\VST3 Presets\\Steinberg Media Technologies\\REVerence\\' + vstpreset
                string vstPresetFileName = String.Format("{0}{1}{2}.vstpreset", _vstPresetPrefixText, fileName, _vstPresetSuffixText);
                string vstPresetFilePath = Path.Combine(_presetDir, vstPresetFileName);
                Console.WriteLine("vstPresetFilePath: {0}", vstPresetFilePath);

                // Altiverb mode?
                // guess image name based on the image content in the parent directory?
                // i.e 
                // Scoring Stages (Orchestral Studios)\Todd-AO - California US:
                // Todd-AO-2779.jpg
                // Todd-AO-2782.jpg
                // Todd-AO-2813.jpg
                // Todd-AO-Marcs-layout.jpg
                // TODD-stats.jpg
                //
                // Cathedrals\Caen - Saint-Etienne:
                // caen  interior 1.jpg
                // caen  interior 2 .jpg
                // caen  interior 3.jpg
                // caen  exterior.jpg
                // caen IR stats.jpg
                // St Etienne Caen.mov ?!!!
                //
                // Tombs\Vigeland Mausoleum (Oslo):
                // 1 Vigeland-Museum-interior.jpg
                // Vigeland-Museum-exterior.jpg
                // Vigeland-Museum-stats.jpg				
                if (_altiverbImageMode)
                {
                    // Rule, look in parent directory for file NOT ending with "stats.jpg"
                    var imageFiles = new DirectoryInfo(wavFilePath).Parent.GetFiles("*.jpg");

                    if (null != imageFiles && imageFiles.Length > 0)
                    {

                        // Order by size (LINQ).
                        var sortedImageFiles = from file in imageFiles
                                               where !file.Name.Contains("stats.jpg")
                                               orderby file.Length ascending
                                               select file;

                        foreach (FileInfo fi in sortedImageFiles)
                        {
                            imageFile = fi.FullName;
                            Console.Out.WriteLine("Altiverb Mode - Found image file to use: {0}", imageFile);
                            break;
                        }
                    }
                }
                else
                {
                    imageFile = _imageFilePath;
                }
                if (null == imageFile || imageFile == "")
                {
                    Console.WriteLine("Not using any image.");
                }
                else
                {
                    Console.WriteLine("Using image: {0}", imageFile);
                }

                // Read wav attrubutes
                int audioChannels = 0;
                RiffRead wavReader = new RiffRead(wavFilePath);
                if (wavReader.Process())
                {
                    audioChannels = wavReader.Channels;
                    Console.WriteLine("audioChannels: {0}", audioChannels);

                    //foreach (var iChunk in wavReader.InfoChunks) {
                    //	Console.WriteLine("info-chunk: {0}={1}", iChunk.Key, iChunk.Value);
                    //}
                }

                if (_automatic_mediaRoomType)
                {
                    while (true)
                    {
                        // Altiverb categorizations
                        if (Regex.IsMatch(fileName, @".*?Cathedral.*")) { _mediaRoomType = "Cathedrals"; Console.Out.WriteLine("Found Cathedrals ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Church.*")) { _mediaRoomType = "Churches"; Console.Out.WriteLine("Found Churches ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Club.*")) { _mediaRoomType = "Clubs"; Console.Out.WriteLine("Found Clubs ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Concert Halls Large.*")) { _mediaRoomType = "Concert Halls Large"; Console.Out.WriteLine("Found Concert Halls Large ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Concert Halls Medium and Auditoriums.*")) { _mediaRoomType = "Concert Halls Medium and Auditoriums"; Console.Out.WriteLine("Found Concert Halls Medium and Auditoriums ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Gear \(Plates, Springs eq's etc\).*")) { _mediaRoomType = "Gear (Plates, Springs eq's etc)"; Console.Out.WriteLine("Found Gear (Plates, Springs eq's etc) ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Metallic Resonant Spaces.*")) { _mediaRoomType = "Metallic Resonant Spaces"; Console.Out.WriteLine("Found Metallic Resonant Spaces ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Operas & Theaters.*")) { _mediaRoomType = "Operas & Theaters"; Console.Out.WriteLine("Found Operas & Theaters ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Outdoor.*")) { _mediaRoomType = "Outdoor"; Console.Out.WriteLine("Found Outdoor ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Post Production Ambiences.*")) { _mediaRoomType = "Post Production Ambiences"; Console.Out.WriteLine("Found Post Production Ambiences ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Recording Studio's Echo Chambers.*")) { _mediaRoomType = "Recording Studio's Echo Chambers"; Console.Out.WriteLine("Found Recording Studio's Echo Chambers ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Recording Studios.*")) { _mediaRoomType = "Recording Studios"; Console.Out.WriteLine("Found Recording Studios ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Scoring Stages \(Orchestral Studios\).*")) { _mediaRoomType = "Scoring Stages (Orchestral Studios)"; Console.Out.WriteLine("Found Scoring Stages (Orchestral Studios) ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Small Rooms for Music.*")) { _mediaRoomType = "Small Rooms for Music"; Console.Out.WriteLine("Found Small Rooms for Music ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Sound Design.*")) { _mediaRoomType = "Sound Design"; Console.Out.WriteLine("Found Sound Design ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Stadium.*")) { _mediaRoomType = "Stadiums"; Console.Out.WriteLine("Found Stadiums ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Tomb.*")) { _mediaRoomType = "Tombs"; Console.Out.WriteLine("Found Tombs ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Underground.*")) { _mediaRoomType = "Underground"; Console.Out.WriteLine("Found Underground ..."); break; }

                        if (Regex.IsMatch(fileName, @".*?Studio.*")) { _mediaRoomType = "Studio"; Console.Out.WriteLine("Found Studio ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Hall.*")) { _mediaRoomType = "Hall"; Console.Out.WriteLine("Found Hall ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Plate.*")) { _mediaRoomType = "Plate"; Console.Out.WriteLine("Found Plate ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Room.*")) { _mediaRoomType = "Room"; Console.Out.WriteLine("Found Room ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Chamber.*")) { _mediaRoomType = "Chamber"; Console.Out.WriteLine("Found Chamber ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Ambience.*")) { _mediaRoomType = "Ambience"; Console.Out.WriteLine("Found Ambience ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Space.*")) { _mediaRoomType = "Space"; Console.Out.WriteLine("Found Space ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Band.*")) { _mediaRoomType = "Band"; Console.Out.WriteLine("Found Band ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Scene.*")) { _mediaRoomType = "Scene"; Console.Out.WriteLine("Found Scene ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Stage.*")) { _mediaRoomType = "Stage"; Console.Out.WriteLine("Found Stage ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Theatre.*")) { _mediaRoomType = "Theatre"; Console.Out.WriteLine("Found Theatre ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Arena.*")) { _mediaRoomType = "Arena"; Console.Out.WriteLine("Found Arena ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Venue.*")) { _mediaRoomType = "Venue"; Console.Out.WriteLine("Found Venue ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Warehouse.*")) { _mediaRoomType = "Warehouse"; Console.Out.WriteLine("Found Warehouse ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?House.*")) { _mediaRoomType = "House"; Console.Out.WriteLine("Found House ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Basement.*")) { _mediaRoomType = "Basement"; Console.Out.WriteLine("Found Basement ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Bathroom.*")) { _mediaRoomType = "Bathroom"; Console.Out.WriteLine("Found Bathroom ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Kitchen.*")) { _mediaRoomType = "Kitchen"; Console.Out.WriteLine("Found Kitchen ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Closet.*")) { _mediaRoomType = "Closet"; Console.Out.WriteLine("Found Closet ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Booth.*")) { _mediaRoomType = "Booth"; Console.Out.WriteLine("Found Booth ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Mics.*")) { _mediaRoomType = "Mics"; Console.Out.WriteLine("Found Mics ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Rhodes.*")) { _mediaRoomType = "Rhodes"; Console.Out.WriteLine("Found Rhodes ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Piano.*")) { _mediaRoomType = "Piano"; Console.Out.WriteLine("Found Piano ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Snare.*")) { _mediaRoomType = "Snare"; Console.Out.WriteLine("Found Snare ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Drum.*")) { _mediaRoomType = "Drum"; Console.Out.WriteLine("Found Drum ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Bin.*")) { _mediaRoomType = "Bin"; Console.Out.WriteLine("Found Bin ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Tight.*")) { _mediaRoomType = "Tight"; Console.Out.WriteLine("Found Tight ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Velvet.*")) { _mediaRoomType = "Velvet"; Console.Out.WriteLine("Found Velvet ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Dark.*")) { _mediaRoomType = "Dark"; Console.Out.WriteLine("Found Dark ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Verb.*")) { _mediaRoomType = "Verb"; Console.Out.WriteLine("Found Verb ..."); break; }
                        if (Regex.IsMatch(fileName, @".*?Cluster.*")) { _mediaRoomType = "Cluster"; Console.Out.WriteLine("Found Cluster ..."); break; }
                        { _mediaRoomType = "Unknown"; Console.Out.WriteLine("Found Unknown ..."); break; }
                    }
                }
                Console.WriteLine("_mediaRoomType: {0}", _mediaRoomType);

                string xml = "\xef\xbb\xbf";
                xml += "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n";
                xml += "<MetaInfo>\r\n";
                xml += String.Format("\t<Attribute id=\"AudioChannels\" value=\"{0}\" type=\"int\" flags=\"\"/>\r\n", audioChannels);
                xml += String.Format("\t<Attribute id=\"FileName\" value=\"{0}\" type=\"string\" flags=\"\"/>\r\n", vstPresetFileName);
                xml += String.Format("\t<Attribute id=\"MediaFamily\" value=\"{0}|{1}\" type=\"string\" flags=\"\"/>\r\n", _mediaFamily, _mediaFamilyGUID);
                xml += String.Format("\t<Attribute id=\"MediaRoomType\" value=\"{0}\" type=\"string\" flags=\"\"/>\r\n", _mediaRoomType);
                xml += "\t<Attribute id=\"MediaType\" value=\"VstPreset\" type=\"string\" flags=\"writeProtected\"/>\r\n";
                xml += "\t<Attribute id=\"PlugInCategory\" value=\"Fx|Reverb\" type=\"string\" flags=\"writeProtected\"/>\r\n";
                xml += "\t<Attribute id=\"PlugInName\" value=\"REVerence\" type=\"string\" flags=\"writeProtected\"/>\r\n";
                xml += "\t<Attribute id=\"PlugInVendor\" value=\"Steinberg Media Technologies\" type=\"string\" flags=\"writeProtected\"/>\r\n";
                xml += String.Format("\t<Attribute id=\"VST3UniqueID\" value=\"{0}\" type=\"string\" flags=\"hidden|writeProtected\"/>\r\n", _vst3UniqueID);
                xml += "\t<Attribute id=\"VST3UnitTypePath\" value=\"program\" type=\"string\" flags=\"hidden|writeProtected\"/>\r\n";
                xml += "</MetaInfo>\r\n"; // TODO: might not always have \r \n at the end ?

                PresetFile.SavePreset(vstPresetFilePath, _vst3UniqueID, wavFilePath, vstPresetFilePath, imageFile, xml, _scriptDir);
                Console.WriteLine("Processing: {0} ... DONE", fileName);
                Console.WriteLine("---------------------------------------------------------------------");
            } // end foreach
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e.ToString());
        }

        /*
		DirectoryInfo di = new DirectoryInfo(_presetDir);
		FileInfo[] presetFiles = di.GetFiles("*.vstpreset");
		int presetCounter = 0;
		foreach(FileInfo fi in presetFiles)
		{
			PresetFile.LoadPreset(Path.Combine(_presetDir, fi.Name), _vst3UniqueID);
		}
		
		DirectoryInfo di = new DirectoryInfo(_scriptDir);
		FileInfo[] fxpFiles = di.GetFiles("*.fxp");
		int fxpCounter = 0;
		FXP fxp = new FXP();
		foreach(FileInfo fi in fxpFiles)
		{
			fxp.ReadFile(Path.Combine(_scriptDir, fi.Name));
			Console.Out.WriteLine("--------------------------------");
			Console.Out.WriteLine("chunkMagic: {0}", fxp.chunkMagic);
			Console.Out.WriteLine("byteSize: {0}", fxp.byteSize);
			Console.Out.WriteLine("fxMagic: {0}", fxp.fxMagic);
			Console.Out.WriteLine("version: {0}", fxp.version);
			Console.Out.WriteLine("fxID: {0}", fxp.fxID);
			Console.Out.WriteLine("fxVersion: {0}", fxp.fxVersion);
			Console.Out.WriteLine("numPrograms: {0}", fxp.numPrograms);
			Console.Out.WriteLine("name: {0}", fxp.name);
			Console.Out.WriteLine("chunkSize: {0}", fxp.chunkSize);		

			fxp.WriteFile(Path.Combine(_scriptDir, String.Format("{0}-test.fxp", fi.Name)));			
		}
		*/
    }

    public static string GetParentPrefix(string fileName, DirectoryInfo dirNode, int intMaxLevels)
    {
        //Console.Out.WriteLine(">> current: {0}", fileName);
        int currentLevel = 0;
        while (currentLevel < intMaxLevels)
        {
            DirectoryInfo parent = dirNode.Parent;
            fileName = String.Format("{0}_{1}", parent.Name, fileName);
            //Console.Out.WriteLine(">> prefixing parent folder: {0}", fileName);
            dirNode = parent;
            currentLevel++;
        }
        return fileName;
    }
} // end Script class


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
public class PresetFile
{

    static string[] commonChunks = {    "VST3",    // kHeader
										"Comp",    // kComponentState
										"Cont",    // kControllerState
										"Prog",    // kProgramData
										"Info",    // kMetaInfo
										"List" };  // kChunkList

    public enum ChunkType
    {
        HEADER,
        COMPONENTSTATE,
        CONTROLLERSTATE,
        PROGRAMDATA,
        METAINFO,
        CHUNKLIST
    }

    public class Entry
    {
        public string id;
        public long offset;
        public long size;
        public byte[] bytes;
    }

    // Preset Header: header id + version + class id + list offset
    static int _formatVersion = 1;
    static int _classIDSize = 32; // ASCII-encoded String (FUID)
    static int _intSize = 4;

    static long _headerSize = 4 + _intSize + _classIDSize + 4;
    static long _listOffsetPos = _headerSize - 4;
    static int _maxEntries = 128;

    private int entryCount = 0;
    private Entry[] entries = new Entry[_maxEntries];
    public Entry[] Entries { get { return entries; } set { entries = value; } }
    public int EntryCount { get { return entryCount; } set { entryCount = value; } }

    // private vars
    private string classID = null;
    private string fileName = null;
    private BinaryReader binaryReader = null;
    private BinaryFile bf = null;

    // reverence data
    private string wavFilePath = "";
    private string vstPresetFilePath = "";
    private string imageFilePath = "";

    // program data
    private byte[] programListData;
    private byte[] unitData;
    private byte[] unitProgramData;
    private byte[] metaInfoXml;

    public string ClassID { get { return classID; } set { classID = value; } }
    public string FileName { get { return fileName; } set { fileName = value; } }
    public string WavFilePath { get { return wavFilePath; } set { wavFilePath = value; } }
    public string VstPresetFilePath { get { return vstPresetFilePath; } set { vstPresetFilePath = value; } }
    public string ImageFilePath { get { return imageFilePath; } set { imageFilePath = value; } }
    public byte[] ProgramListData { get { return programListData; } set { programListData = value; } }
    public byte[] UnitData { get { return unitData; } set { unitData = value; } }
    public byte[] UnitProgramData { get { return unitProgramData; } set { unitProgramData = value; } }
    public byte[] MetaInfoXml { get { return metaInfoXml; } set { metaInfoXml = value; } }

    public PresetFile(String value)
    {
        this.fileName = value;
        bf = new BinaryFile(value, BinaryFile.ByteOrder.LittleEndian, true);
    }

    public ChunkType ChunkTypeFromValue(string stringValue)
    {
        return (ChunkType)Enum.Parse(typeof(ChunkType), stringValue);
    }

    public string GetChunkID(ChunkType type)
    {
        return commonChunks[(int)type];
    }

    public bool Contains(ChunkType type)
    {
        return GetEntry(type) != null;
    }

    public Entry GetEntry(ChunkType type)
    {
        string id = GetChunkID(type);
        for (int i = 0; i < entryCount; i++)
        {
            if (entries[i].id == id)
            {
                return entries[i];
            }
        }
        return null;
    }

    public Entry GetLastEntry()
    {
        return entryCount > 0 ? entries[entryCount - 1] : new Entry();
    }

    public bool Close()
    {
        bf.Close();
        return true;
    }

    public bool WriteMetaInfo(string xmlBuffer, int size, bool forceWriting)
    {
        if (Contains(ChunkType.METAINFO)) // already exists!
        {
            if (!forceWriting)
                return false;
        }
        if (!PrepareMetaInfoUpdate())
            return false;

        if (size == -1)
            size = (int)xmlBuffer.Length;

        Entry e = new Entry();
        return BeginChunk(e, ChunkType.METAINFO) && bf.Write(xmlBuffer, size) && EndChunk(e);
    }

    public bool PrepareMetaInfoUpdate()
    {
        long writePos = 0;
        Entry e = GetEntry(ChunkType.METAINFO);
        if (e != null)
        {
            // meta info must be the last entry!    
            if (e != GetLastEntry())
                return false;

            writePos = e.offset;
            entryCount--;
        }
        else
        {
            // entries must be sorted ascending by offset!
            e = GetLastEntry();
            writePos = e != null ? e.offset + e.size : _headerSize;
        }

        return bf.SeekTo(writePos) != 0;
    }

    public bool BeginChunk(Entry e, ChunkType chunkType)
    {
        if (entryCount >= _maxEntries)
            return false;

        e.id = GetChunkID(chunkType);
        e.offset = bf.GetPosition();
        e.size = 0;
        return true;
    }

    public bool EndChunk(Entry e)
    {
        if (entryCount >= _maxEntries)
            return false;

        int pos = (int)bf.GetPosition();
        e.size = pos - e.offset;
        entries[entryCount++] = e;
        return true;
    }

    public bool ReadChunkData()
    {
        for (int i = 0; i < entryCount; i++)
        {
            Entry e = entries[i];
            bf.SeekTo(e.offset);
            e.bytes = bf.ReadBytes((int)e.size);
        }
        return true;
    }

    public bool ReadChunkList()
    {
        bf.SeekTo(0);

        // Read header
        string tempHeaderID = GetChunkID(ChunkType.HEADER);
        string chunkHeaderID = bf.ReadString(4);
        Console.Out.WriteLine("chunkHeaderID: {0}", chunkHeaderID);
        if (tempHeaderID != chunkHeaderID) return false;

        // Read sample rate, 4 bytes	
        int version = bf.ReadInt32();
        Console.Out.WriteLine("version: {0}", version);

        ClassID = bf.ReadString(_classIDSize);
        Console.Out.WriteLine("Class ID: {0}", ClassID);

        long listOffset = bf.ReadInt64();
        Console.Out.WriteLine("List Offset: {0}", listOffset);

        if (listOffset <= 0) return false;
        bf.SeekTo(listOffset);

        // Read list
        string tempListID = GetChunkID(ChunkType.CHUNKLIST);
        string chunkListID = bf.ReadString(4);
        if (tempListID != chunkListID) return false;

        int count = bf.ReadInt32();
        if (count <= 0) return false;

        if (count > _maxEntries)
            count = _maxEntries;

        Console.Out.WriteLine("Count: {0}", count);
        for (int i = 0; i < count; i++)
        {
            Entry e = new Entry();
            e.id = bf.ReadString(4);
            e.offset = bf.ReadInt64();
            e.size = bf.ReadInt64();

            entries[i] = e;
            Console.Out.WriteLine("e.id: {0}, e.offset: {1}, e.size {2}", e.id, e.offset, e.size);

            if (e.id == "" || e.offset == 0)
                break;

            entryCount++;
        }
        return entryCount > 0;
    }

    public void ReadProgramData()
    {
        Entry e = GetEntry(ChunkType.PROGRAMDATA);
        int savedProgramListID = -1;
        if (e != null && bf.SeekTo(e.offset) != 0)
        {
            savedProgramListID = bf.ReadInt32();
            Console.Out.WriteLine("savedProgramListID: {0}", savedProgramListID);
            if (savedProgramListID >= 0)
            {
                int alreadyRead = _intSize;

                // print the wavfile
                bf.SeekTo(e.offset + alreadyRead);
                Console.Out.WriteLine(BinaryFile.ByteArrayToString(bf.ReadBytes(292)));

                // print the presetfile
                bf.SeekTo(344);
                Console.Out.WriteLine(BinaryFile.ByteArrayToString(bf.ReadBytes(368)));

                bf.SeekTo(712);
                byte[] unknown1 = bf.ReadBytes(368);

                // print wav file + rest of Reverence text
                bf.SeekTo(1080);
                Console.Out.WriteLine(BinaryFile.ByteArrayToString(bf.ReadBytes(476)));

                bf.SeekTo(1556);
                byte[] unknown2 = bf.ReadBytes(124);

                bf.SeekTo(1680);
                byte[] unknown3 = bf.ReadBytes(428);

                // image file + rest of Reverence text
                bf.SeekTo(2108);
                Console.Out.WriteLine(BinaryFile.ByteArrayToString(bf.ReadBytes(476)));

                bf.SeekTo(2584);
                byte[] unknown4 = bf.ReadBytes(552);

                bf.SeekTo(3136);
                byte[] unknown5 = bf.ReadBytes(10644);
            }
        }
    }

    public void WriteProgramData(String scriptDir)
    {
        Entry e = new Entry();
        BeginChunk(e, ChunkType.PROGRAMDATA);

        // write ProgramListID
        int ProgramListID = 0;
        bf.SeekTo(_headerSize);
        bf.Write((Int32)ProgramListID);

        // unknown data between the first wav file and the presetfile
        //bf.SeekTo(336)
        //bf.Write( BinaryFile.HexStringToByteArray("780f160048000001") );          
        bf.SeekTo(340);
        bf.Write(BinaryFile.HexStringToByteArray("48000001"));

        // wavfile
        int skip = 8;
        bf.SeekTo(_headerSize + skip);
        bf.Write(PadZero(WavFilePath));

        // insert filepath to presetfile
        bf.SeekTo(344);
        bf.Write(PadZero(VstPresetFilePath));

        bf.SeekTo(712);
        bf.Write(BinaryFile.HexStringToByteArray("c499120000000000b800917cb8541b00909a12004100917c781315005d00917c000000002dff907c"));

        bf.SeekTo(836);
        bf.Write(BinaryFile.HexStringToByteArray("28020000000000000000000000000000000000000000000000000000bb2e917c95090000e0971200000015005c9f120020e9907cb842917cffffffffaf42917cbad0010045000000d4991200800010405c9f120020e9907c6000917cffffffff000000002dff907c089b1200ff09817cb700000000000040306e4a0308ddef0e1800000000000000f09a12004000000000000000d49a12000000000000000000000000000c000000020000000101180fc8d2921a16000000000000000300000024011a02c0541b0000000000c0541b00050000000b0000007df029010cddef0effffffff8fae28010cddef0e306e4a03"));

        // insert filepath to wav file (+ rest of Reverence text?)
        bf.SeekTo(1080);
        bf.Write(PadZero(WavFilePath));

        byte[] rawData;
        if (ImageFilePath != "")
        {
            bf.SeekTo(1556);
            bf.Write(BinaryFile.HexStringToByteArray("c890120000000000b800917c708d651a949112004100917c98083f005d00917cafac807c788d651a10000000c0541b"));

            bf.SeekTo(1680);
            bf.Write(BinaryFile.HexStringToByteArray("580000002802000000000000000000000000000000000000000000000000000000000000a091120000003f0000001500ffffffffd095120020e9907cb842917cffff01000b000000d8901200dc901200c491120020e9907c6000917cffffffff5d00917c75312a0100003f000000000094312a01c8e5f2bcafac807c589212001000000048db580fa89112001c000000f4911200e0a82a0154dd52bdfeffffff94312a0156d32801788d651a4c92120098d00d01010000004c9212005892120008921200d13c4f01fffffffff3020e014c921200d0951200b0434f01ffffffff99820e01fc1abb01a0981200041bbb01a0981200a0921200000000006492120000003f002202917c0300000018073f0000003f00407bdb0e105e8d0101000000d8b48901845c8d010000000000000080105e8d010100000000000000845c8d010000000000000000845c8d010000000000000000845c8d010000000000000000845c8d010000000000000000845c8d01000000000000000000000100845c8d010000000000000000ffffffffc400917c0d000000004d87010000917c950900000893120000003f0001000000"));

            // insert filepath to image file (+ rest of Reverence text?)
            bf.SeekTo(2108);
            bf.Write(PadZero(ImageFilePath));

            bf.SeekTo(2584);
            rawData = File.ReadAllBytes(Path.Combine(scriptDir, "binary-preset-content1.dat"));
            bf.Write(rawData);
            rawData = File.ReadAllBytes(Path.Combine(scriptDir, "binary-preset-content2.dat"));
            bf.Write(rawData);
            bf.Write(ZeroBytes(10));
        }
        else
        {
            // if not image - then the binaryPresetContent starts at 1556
            bf.SeekTo(1556);
            rawData = File.ReadAllBytes(Path.Combine(scriptDir, "binary-preset-content1.dat"));
            bf.Write(rawData);

            bf.SeekTo(2106);
            rawData = File.ReadAllBytes(Path.Combine(scriptDir, "binary-preset-content2.dat"));
            bf.Write(rawData);
            bf.Write(ZeroBytes(10));
        }

        EndChunk(e);
    }

    public bool WriteChunkList()
    {
        // Update list offset
        int pos = (int)bf.GetPosition();
        if (!(bf.SeekTo(_listOffsetPos) > 0 && bf.Write(pos) && bf.SeekTo(pos) > 0))
            return false;

        // Write list
        if (!bf.Write(GetChunkID(ChunkType.CHUNKLIST)))
            return false;

        if (!bf.Write((Int32)entryCount))
            return false;

        for (int i = 0; i < entryCount; i++)
        {
            Entry e = entries[i];
            if (!(bf.Write(e.id) && bf.Write(e.offset) && bf.Write(e.size)))
                return false;
        }
        return true;
    }

    public bool WriteChunk(byte[] data, ChunkType type)
    {
        if (Contains(type)) // already exists!
            return false;

        Entry e = new Entry();
        return BeginChunk(e, type) && bf.Write(data) && EndChunk(e);
    }

    public bool StoreState(ChunkType type)
    {
        if (Contains(type)) // already exists!
            return false;

        Entry e = new Entry();
        return BeginChunk(e, type) && EndChunk(e);
    }

    public bool RestoreState(ChunkType type)
    {
        byte[] b;
        Entry e = GetEntry(type);
        if (e != null)
        {
            b = bf.ReadBytes((int)e.offset, (int)e.size);
            e.bytes = b;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void ReadMetaInfoData()
    {

        Entry e = GetEntry(ChunkType.METAINFO);
        string metaDataID = null;
        if (e != null && bf.SeekTo(e.offset) != 0)
        {
            metaDataID = bf.ReadString(3);
            Console.Out.WriteLine("metaDataID: {0}", metaDataID);
            if (metaDataID != null)
            {
                int alreadyRead = 3;

                //byte[] b;               
                bf.SeekTo(e.offset + alreadyRead);

                //b = ReadBytes((int) e.size - alreadyRead);                              
                //Console.Out.WriteLine(BinaryFile.ByteArrayToString(b));

                string xmlData = bf.ReadString((int)e.size - alreadyRead);
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xmlData);
                writeTree(xmlDocument, Console.Out);
            }
        }
    }

    public static void writeTree(XmlDocument xmlDocument, TextWriter writer)
    {
        StringWriter stringWriter = new StringWriter();
        XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);
        xmlTextWriter.Formatting = Formatting.Indented;

        xmlDocument.WriteTo(xmlTextWriter); //xmlDocument can be replaced with a XmlNode

        xmlTextWriter.Flush();
        writer.Write(stringWriter.ToString());
    }

    public bool WriteHeader()
    {
        // header id + version + class id + list offset (unknown yet)
        bf.SeekTo(0);
        bf.Write(GetChunkID(ChunkType.HEADER));
        bf.Write((Int32)_formatVersion);
        bf.Write(ClassID, _classIDSize);
        bf.Write((Int64)0);
        return true;
    }

    public static bool LoadPreset(string fileName, string classID)
    {
        return LoadPreset(fileName, classID, ChunkType.COMPONENTSTATE, ChunkType.CONTROLLERSTATE);
    }

    public static bool LoadPreset(string fileName, string classID, ChunkType component, ChunkType editController)
    {
        Console.Out.WriteLine("==========================================================================");
        Console.Out.WriteLine(" LOADING PRESET: {0}", fileName);
        Console.Out.WriteLine(" USING CLASS ID: {0}", classID);
        Console.Out.WriteLine("==========================================================================");
        PresetFile pf = new PresetFile(fileName);

        if (!pf.ReadChunkList())
            return false;

        if (!pf.ReadChunkData())
            return false;

        pf.ReadProgramData();
        pf.ReadMetaInfoData();

        if (pf.ClassID != classID)
            return false;

        if (!pf.RestoreState(component))
            return false;

        /*
        if (editController != 0)
        {
            // assign component state to controller
            if (!pf.RestoreState(editController))
				return false;
    
           // restore controller-only state (if present)
            if (pf.Contains(ChunkType.CONTROLLERSTATE) && !pf.RestoreState(editController))
                return false; 	
        }
		*/

        pf.Close();
        return true;
    }

    public static bool SavePreset(string fileName, string classID, string wavFilePath, string vstPresetFilePath, string imageFilePath, string xmlBuffer, string scriptDir)
    {
        return SavePreset(fileName, classID, wavFilePath, vstPresetFilePath, imageFilePath, 0, 0, xmlBuffer, scriptDir);
    }

    public static bool SavePreset(string fileName, string classID, string wavFilePath, string vstPresetFilePath, string imageFilePath, ChunkType componentState, ChunkType editController, string xmlBuffer, string scriptDir)
    {
        Console.Out.WriteLine("==========================================================================");
        Console.Out.WriteLine(" SAVING PRESET: {0}", fileName);
        Console.Out.WriteLine(" USING CLASS ID: {0}", classID);
        Console.Out.WriteLine("==========================================================================");

        int xmlSize = xmlBuffer.Length;

        PresetFile pf = new PresetFile(fileName);
        pf.ClassID = classID;
        pf.WavFilePath = wavFilePath;
        pf.VstPresetFilePath = vstPresetFilePath;
        pf.ImageFilePath = imageFilePath;

        if (!pf.WriteHeader())
            return false;

        pf.WriteProgramData(scriptDir);

        if (componentState != 0 && !pf.StoreState(componentState))
            return false;

        if (editController != 0 && !pf.StoreState(editController))
            return false;

        if (xmlBuffer != null && !pf.WriteMetaInfo(xmlBuffer, xmlSize, false))
            return false;

        pf.WriteChunkList();

        pf.Close();
        return true;
    }

    public static string PadZero(string original)
    {
        byte[] b = BinaryFile.StringToByteArray(original);
        List<byte> list = new List<byte>();
        for (int i = 0; i < b.Length; i++)
        {
            list.Add(b[i]);
            list.Add(0x00);
        }
        return BinaryFile.ByteArrayToString(list.ToArray());
    }

    public static byte[] ZeroBytes(int numberOfZeros)
    {
        byte[] zeroBytes = new byte[numberOfZeros];
        for (int i = 0; i < numberOfZeros - 1; i++)
        {
            zeroBytes[i] = 0x00;
        }
        return zeroBytes;
    }
}

/*
* Arguments class: application arguments interpreter
*
* Authors:      R. LOPES
* Contributors: R. LOPES
* Created:      October 2002
* Modified:     October 2002
*
* Version:      1.0
*/
//using System;
//using System.Collections.Specialized;
//using System.Text.RegularExpressions;

namespace CommandLine.Utility
{
    /// <summary>
    /// Arguments class
    /// </summary>
    public class Arguments
    {
        // Variables
        private StringDictionary Parameters;

        // Constructor
        public Arguments(string[] Args)
        {
            Parameters = new StringDictionary();
            Regex Spliter = new Regex(@"^-{1,2}|^/|=|:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex Remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            string Parameter = null;
            string[] Parts;

            // Valid parameters forms:
            // {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples: -paramvalue--param/param3:"Test-:-work" /param4=happy -param'--=nice=--'
            foreach (string Txt in Args)
            {
                // Look for new parameters (-,/ or --) and a possible enclosed value (=,:)
                Parts = Spliter.Split(Txt, 3);
                switch (Parts.Length)
                {
                    // Found a value (for the last parameter found (space separator))
                    case 1:
                        if (Parameter != null)
                        {
                            if (!Parameters.ContainsKey(Parameter))
                            {
                                Parts[0] = Remover.Replace(Parts[0], "$1");
                                Parameters.Add(Parameter, Parts[0]);
                            }
                            Parameter = null;
                        }
                        // else Error: no parameter waiting for a value (skipped)
                        break;
                    // Found just a parameter
                    case 2:
                        // The last parameter is still waiting. With no value, set it to true.
                        if (Parameter != null)
                        {
                            if (!Parameters.ContainsKey(Parameter)) Parameters.Add(Parameter, "true");
                        }
                        Parameter = Parts[1];
                        break;
                    // Parameter with enclosed value
                    case 3:
                        // The last parameter is still waiting. With no value, set it to true.
                        if (Parameter != null)
                        {
                            if (!Parameters.ContainsKey(Parameter)) Parameters.Add(Parameter, "true");
                        }
                        Parameter = Parts[1];
                        // Remove possible enclosing characters (",')
                        if (!Parameters.ContainsKey(Parameter))
                        {
                            Parts[2] = Remover.Replace(Parts[2], "$1");
                            Parameters.Add(Parameter, Parts[2]);
                        }
                        Parameter = null;
                        break;
                }
            }
            // In case a parameter is still waiting
            if (Parameter != null)
            {
                if (!Parameters.ContainsKey(Parameter)) Parameters.Add(Parameter, "true");
            }
        }

        // Retrieve a parameter value if it exists
        public string this[string Param]
        {
            get
            {
                return (Parameters[Param]);
            }
        }
    }
}