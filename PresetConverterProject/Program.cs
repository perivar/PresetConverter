using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using CommonUtils;
using CommonUtils.Audio;
using CommonUtils.Audio.RIFF;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using PresetConverterProject.NIKontaktNKS;
using SDIR2WavConverter;
using Serilog;
using Serilog.Events;

namespace PresetConverter
{
    class Program
    {
        public static object SnpidCSVParser(int lineCounter, string[] splittedLine, Dictionary<int, string> lookupDictionary)
        {
            var snpid = splittedLine[0];
            var companyName = splittedLine[1];
            var libraryName = splittedLine[2];

            NksLibraryDesc libDesc = new NksLibraryDesc();
            libDesc.Id = snpid;
            libDesc.Company = companyName;
            libDesc.Name = libraryName;

            return libDesc;
        }

        public static object RTStringsParser(int lineCounter, string[] splittedLine, Dictionary<int, string> lookupDictionary)
        {
            var snpidAndCode = splittedLine[0];

            Regex snpidAndCodeRegex = new(@"^(\d+):\s(.*?)$");
            Match match = snpidAndCodeRegex.Match(snpidAndCode);
            string? snpid = null;
            string? jdx = null;
            if (match.Success)
            {
                int id = int.Parse(match.Groups[1].Value);
                snpid = string.Format("{0:000}", id);
                jdx = match.Groups[2].Value;
            }

            var hu = splittedLine[1];

            NksLibraryDesc libDesc = new()
            {
                Id = snpid
            };
            NKS.NksGeneratingKeySetKeyStr(libDesc.GenKey, jdx);
            NKS.NksGeneratingKeySetIvStr(libDesc.GenKey, hu);

            var libraryName = splittedLine[2];
            var libraryCompanyId = splittedLine.Length > 3 ? splittedLine[3] : null;

            if (!String.IsNullOrEmpty(libraryName))
            {
                libDesc.Name = libraryName;
            }

            if (libraryCompanyId != null)
            {
                var companyId = int.Parse(libraryCompanyId);
                libDesc.Company = lookupDictionary[companyId];
            }

            return libDesc;
        }

        public static string NKSLibCSVHeader(string columnSeparator)
        {
            var elements = new List<string>();

            // elements.Add("Line");
            elements.Add("SNPID");
            elements.Add("Company Name");
            elements.Add("Product Name");
            elements.Add("JDX");
            elements.Add("HU");

            return string.Join(columnSeparator, elements);
        }

        public static string NKSLibCSVFormatter(object line, int lineCounter, string columnSeparator)
        {
            var elements = new List<string>();
            var libDesc = (NksLibraryDesc)line;

            // elements.Add(String.Format("{0,4}", lineCounter));
            elements.Add(libDesc.Id);
            elements.Add(libDesc.Company);
            elements.Add(libDesc.Name);

            if (libDesc.GenKey.KeyLength > 0) elements.Add(StringUtils.ByteArrayToHexString(libDesc.GenKey.Key));
            if (libDesc.GenKey.IVLength > 0) elements.Add(StringUtils.ByteArrayToHexString(libDesc.GenKey.IV));

            return string.Join(columnSeparator, elements);
        }

        public static string NKSLibCSVHeaderShort(string columnSeparator)
        {
            var elements = new List<string>();

            // elements.Add("Line");
            elements.Add("SNPID");
            elements.Add("Company Name");
            elements.Add("Product Name");

            return string.Join(columnSeparator, elements);
        }

        public static string NKSLibCSVFormatterShort(object line, int lineCounter, string columnSeparator)
        {
            var elements = new List<string>();
            var libDesc = (NksLibraryDesc)line;

            // elements.Add(String.Format("{0,4}", lineCounter));
            elements.Add(libDesc.Id);
            elements.Add(libDesc.Company ?? "Undefined");
            elements.Add(libDesc.Name ?? "Undefined");

            return string.Join(columnSeparator, elements);
        }

        public class SemiNumericComparer : IComparer<string>
        {
            /// <summary>
            /// Method to determine if a string is a number
            /// </summary>
            /// <param name="value">String to test</param>
            /// <returns>True if numeric</returns>
            public static bool IsNumeric(string value)
            {
                return int.TryParse(value, out _);
            }

            /// <inheritdoc />
            public int Compare(string s1, string s2)
            {
                const int S1GreaterThanS2 = 1;
                const int S2GreaterThanS1 = -1;

                var IsNumeric1 = IsNumeric(s1);
                var IsNumeric2 = IsNumeric(s2);

                if (IsNumeric1 && IsNumeric2)
                {
                    var i1 = Convert.ToInt32(s1);
                    var i2 = Convert.ToInt32(s2);

                    if (i1 > i2)
                    {
                        return S1GreaterThanS2;
                    }

                    if (i1 < i2)
                    {
                        return S2GreaterThanS1;
                    }

                    return 0;
                }

                if (IsNumeric1)
                {
                    return S2GreaterThanS1;
                }

                if (IsNumeric2)
                {
                    return S1GreaterThanS2;
                }

                return string.Compare(s1, s2, true, CultureInfo.InvariantCulture);
            }
        }

        static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            // // Read settings into NKSLibraries.Libraries
            // NKS.NksReadLibrariesInfo(config["NksSettingsPath"], false, false);
            // var libList = NKSLibraries.Libraries.Values.AsEnumerable();

            // // find current directory
            // var curDirPath = Directory.GetCurrentDirectory();

            // // Read CSV
            // var cvsPath = Path.Combine(curDirPath, "PresetConverterProject/NIKontaktNKS/SNPID List.csv");
            // var csvList = IOUtils.ReadCSV(cvsPath, true, SnpidCSVParser, null, ";", false).Cast<NksLibraryDesc>();

            // // Read CSV2
            // // var cvs2Path = Path.Combine(curDirPath, "PresetConverterProject/NIKontaktNKS/SNPID List2.csv");
            // // var csv2List = IOUtils.ReadCSV(cvs2Path, true, SnpidCSVParser, null, ";", false).Cast<NksLibraryDesc>();

            // // Read CSV Code
            // var cvsCodePath = Path.Combine(curDirPath, "PresetConverterProject/NIKontaktNKS/SNPID List Code.csv");

            // // read RT_STRINGS_COMPANY as lookup list
            // var rtCompanyPath = Path.Combine(curDirPath, "PresetConverterProject/NIKontaktNKS/RT_STRINGS_COMPANY.TXT");
            // Dictionary<int, string> companyDict =
            //     File.ReadLines(rtCompanyPath).
            //     Select((value, number) => (value, number)).
            //     ToDictionary(x => x.number, x => x.value.Trim());

            // // read RT_STRINGS with / delimiter
            // var rtPath = Path.Combine(curDirPath, "PresetConverterProject/NIKontaktNKS/RT_STRINGS.TXT");
            // var rtList = IOUtils.ReadCSV(rtPath, false, RTStringsParser, companyDict, "/", false).Cast<NksLibraryDesc>();
            // var rtListWithId = rtList.Select(x =>
            //  new NksLibraryDesc
            //  {
            //      Id = NKS.ConvertToBase36(long.Parse(x.Id)).PadLeft(3, '0'),
            //      Company = x.Company,
            //      Name = x.Name,
            //      GenKey = x.GenKey
            //  });

            // // check for differences in the csv files
            // // var inCSVButNotCSV2 = csvList.Where(csv => csv2List.All(csv2 => csv2.Id != csv.Id));
            // // var inCSV2ButNotCSV = csv2List.Where(csv2 => csvList.All(csv => csv.Id != csv2.Id));
            // // var inBothCSVButDifferentName = csv2List.Join(csvList, csv2 => csv2.Id, csv => csv.Id, (csv2, csv) => new { csv2, csv }).Where(both => both.csv2.Name != both.csv.Name);

            // // check for differences between rtlist and csv2List
            // var inCSVButNotRT = csvList.Where(csv => rtListWithId.All(rt => rt.Id != csv.Id));
            // var inRTButNotCSV = rtListWithId.Where(rt => csvList.All(csv => csv.Id != rt.Id));
            // var inBothButDifferentName = rtListWithId.Join(csvList, rt => rt.Id, csv => csv.Id, (rt, csv) => new { rt, csv }).Where(both => both.rt.Name != both.csv.Name);

            // // check agains the lib list (Settings.cfg)           
            // var sameIDsButDifferentGenKeys = NKSLibraries.Libraries.Values.Join(rtListWithId, nks => nks.Id, rt => rt.Id, (nks, rt) => new { nks, rt }).Where(both => both.nks.GenKey != both.rt.GenKey);
            // var inRTButNotLib = rtListWithId.Where(rt => NKSLibraries.Libraries.Values.All(nks => nks.Id != rt.Id));
            // var inLibButNotRT = NKSLibraries.Libraries.Values.Where(nks => rtListWithId.All(rt => rt.Id != nks.Id));

            // // build and save the complete list
            // // var completeList = libList.Union(rtListWithId).Union(csvList).OrderBy(a => a.Id, new SemiNumericComparer());
            // // var completeList = libList.Union(rtListWithId).OrderBy(a => a.Id, new SemiNumericComparer());
            // var completeList = libList.Union(rtListWithId).Union(csvList).OrderBy(a => a.Id, new SemiNumericComparer());
            // // var completeList = csvList.Union(csv2List).OrderBy(a => a.Id, new SemiNumericComparer());
            // // var completeList = csv2List.OrderBy(a => a.Id, new SemiNumericComparer());
            // // var completeList = libList.OrderBy(a => a.Id, new SemiNumericComparer());
            // List<object> lines = completeList.Cast<object>().ToList();
            // IOUtils.WriteCSV(cvsCodePath, lines, NKSLibCSVFormatter, NKSLibCSVHeader, ";");
            // IOUtils.WriteCSV(cvsPath, lines, NKSLibCSVFormatterShort, NKSLibCSVHeaderShort, ";");

            // return;

            // Setup command line parser
            var app = new CommandLineApplication();
            app.Name = "PresetConverter";
            app.Description = "Convert different DAW presets to other formats (both fxp, vstpresets and txt)";
            app.HelpOption();
            var optionInputDirectoryOrFilePath = app.Option("-i|--input <path>", "The Input directory or file", CommandOptionType.MultipleValue);
            var optionOutputDirectory = app.Option("-o|--output <path>", "The Output directory", CommandOptionType.SingleValue);
            var optionExtraInformation = app.Option("-e|--extra <path-or-type>", "Extra information as used by the different converters. (E.g. for wav this is an image-path, for packing this is the type)", CommandOptionType.SingleValue);
            var switchConvertKontakt6 = app.Option("-k6|--kontakt6", "Convert discovered Kontakt presets to Kontakt 6", CommandOptionType.NoValue);
            var switchList = app.Option("-l|--list", "List the content of archives", CommandOptionType.NoValue);
            var switchVerbose = app.Option("-v|--verbose", "Output more verbose information", CommandOptionType.NoValue);
            var switchPack = app.Option("-p|--pack", "Pack the input directory into a file using the directory as filename with the --extra option as extension", CommandOptionType.NoValue);
            var switchWCX = app.Option("--wcx", "Use the included wcx plugin for reading Kontakt files", CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                if (optionInputDirectoryOrFilePath.HasValue()
                && optionOutputDirectory.HasValue())
                {
                    string outputDirectoryPath = optionOutputDirectory.Value();
                    string extraInformation = optionExtraInformation.Value();

                    // check convert arguments
                    bool doConvertToKontakt6 = switchConvertKontakt6.HasValue();
                    bool doList = switchList.HasValue();
                    bool doVerbose = switchVerbose.HasValue();
                    bool doPack = switchPack.HasValue();
                    bool doWCX = switchWCX.HasValue();

                    // Setup Logger to use the outputDirectory
                    string errorLogFilePath = Path.Combine(outputDirectoryPath, "log-error.log");
                    string verboseLogFilePath = Path.Combine(outputDirectoryPath, "log-verbose.log");
                    var logConfig = new LoggerConfiguration()
                        .WriteTo.File(verboseLogFilePath)
                        .WriteTo.Console(LogEventLevel.Information)
                        .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error).WriteTo.File(errorLogFilePath))
                        ;
                    logConfig.MinimumLevel.Verbose();
                    Log.Logger = logConfig.CreateLogger();

                    if (!doPack)
                    {
                        var extensions = new List<string> { ".als", ".adv", ".vstpreset", ".xps", ".wav", ".sdir", ".cpr", ".ffp", ".nkx", ".nks", ".nkr", ".nki", ".nicnt", ".ncw", ".exe", ".dll", ".wcx64" };
                        var filePaths = HandleMultipleInputPaths(optionInputDirectoryOrFilePath, extensions);

                        foreach (var inputFilePath in filePaths)
                        {
                            Log.Information("Processing {0} ...", inputFilePath);

                            string extension = new FileInfo(inputFilePath).Extension.ToLowerInvariant();
                            switch (extension)
                            {
                                case ".als":
                                    HandleAbletonLiveProject(inputFilePath, outputDirectoryPath);
                                    break;
                                case ".adv":
                                    HandleAbletonLivePreset(inputFilePath, outputDirectoryPath);
                                    break;
                                case ".vstpreset":
                                    HandleSteinbergVstPreset(inputFilePath, outputDirectoryPath);
                                    break;
                                case ".xps":
                                    HandleWavesXpsPreset(inputFilePath, outputDirectoryPath);
                                    break;
                                case ".wav":
                                    HandleWaveFile(inputFilePath, outputDirectoryPath, extraInformation);
                                    break;
                                case ".sdir":
                                    HandleSDIRFile(inputFilePath, outputDirectoryPath);
                                    break;
                                case ".cpr":
                                    HandleCubaseProjectFile(inputFilePath, outputDirectoryPath, config, doConvertToKontakt6);
                                    break;
                                case ".ffp":
                                    HandleFabfilterPresetFile(inputFilePath, outputDirectoryPath);
                                    break;
                                case ".nkx":
                                case ".nks":
                                case ".nkr":
                                case ".nki":
                                case ".nicnt":
                                case ".ncw":
                                    HandleNIKontaktFile(inputFilePath, outputDirectoryPath, extension, config, doList, doVerbose, doPack, doWCX);
                                    break;
                                case ".exe":
                                case ".dll":
                                case ".wcx64":
                                    HandleWindowsFile(inputFilePath, outputDirectoryPath, config, doList, doVerbose);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // pack an input directory
                        if (extraInformation == null)
                        {
                            Console.Error.WriteLine("Please provide packing type using the --extra information!");
                            app.ShowHelp();
                            return 0;
                        }

                        // check packing type
                        string packingType = extraInformation.ToLowerInvariant();
                        switch (packingType)
                        {
                            case ".nicnt":
                            case "nicnt":
                                foreach (var inputDirectoryOrFilePath in optionInputDirectoryOrFilePath.Values)
                                {
                                    // check if input is a filepath or a directory
                                    var isDirectory = IOUtils.IsDirectory(inputDirectoryOrFilePath);
                                    if (isDirectory.HasValue && isDirectory.Value)
                                    {
                                        // directory
                                        HandleNIKontaktFile(inputDirectoryOrFilePath, outputDirectoryPath, ".nicnt", config, doList, doVerbose, doPack, doWCX);
                                    }
                                }
                                break;
                        }
                    }
                }
                else
                {
                    app.ShowHint();
                }
                return 0;
            });


            try
            {
                app.Execute(args);
            }
            catch (System.Exception e)
            {
                Log.Error("{0}", e.Message);
            }
        }

        private static IEnumerable<string> HandleMultipleInputPaths(CommandOption optionInputDirectoryOrFilePath, List<string> extensions)
        {
            List<string> files = new List<string>();

            foreach (var inputDirectoryOrFilePath in optionInputDirectoryOrFilePath.Values)
            {
                // check if input is a filepath or a directory
                var isDirectory = IOUtils.IsDirectory(inputDirectoryOrFilePath);
                if (isDirectory.HasValue)
                {
                    if (isDirectory.Value)
                    {
                        // directory
                        var directoryFilePaths = Directory.GetFiles(inputDirectoryOrFilePath, "*.*", SearchOption.AllDirectories)
                        .Where(s => extensions.Contains(Path.GetExtension(s).ToLowerInvariant()));

                        // append to main file list
                        files.AddRange(directoryFilePaths);
                    }
                    else
                    {
                        // file
                        if (extensions.Contains(Path.GetExtension(inputDirectoryOrFilePath).ToLowerInvariant()))
                        {
                            files.Add(inputDirectoryOrFilePath);
                        }
                        else
                        {
                            Log.Error("Not a valid file extension {0} ...", inputDirectoryOrFilePath);
                        }
                    }
                }
                else
                {
                    Log.Error("Not a valid file or directory {0} ...", inputDirectoryOrFilePath);
                }
            }

            return files;
        }

        private static void HandleAbletonLiveProject(string file, string outputDirectoryPath)
        {
            var bytes = File.ReadAllBytes(file);
            var decompressed = IOUtils.Decompress(bytes);
            var str = Encoding.UTF8.GetString(decompressed);
            var docXelement = XElement.Parse(str);

            // string outputFileName = Path.GetFileNameWithoutExtension(file);
            // string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName + ".xml");
            // docXelement.Save(outputFilePath);

            var tracks = docXelement.Descendants("Devices");
            foreach (XElement xelement in tracks.Elements())
            {
                var pluginName = xelement.Name.ToString();

                // find track name
                var trackName = xelement.AncestorsAndSelf().Where(a => a.Name.LocalName.Contains("Track"))
                .Elements("Name")
                .Elements("EffectiveName").Attributes("Value").First().Value;

                Log.Information("Track: {0} - Plugin: {1}", trackName, pluginName);

                string outputFileName = string.Format("{0} - {1}", Path.GetFileNameWithoutExtension(file), trackName);
                string outputFilePath = null;

                // find preset type
                switch (pluginName)
                {
                    case "Eq8":
                        // Convert EQ8 to Steinberg Frequency
                        var eq = new AbletonEq8(xelement);
                        var steinbergFrequency = eq.ToSteinbergFrequency();
                        outputFilePath = Path.Combine(outputDirectoryPath, "Frequency", "Ableton - " + outputFileName);
                        IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Frequency"));
                        steinbergFrequency.Write(outputFilePath + ".vstpreset");

                        // and dump the text info as well
                        File.WriteAllText(outputFilePath + ".txt", steinbergFrequency.ToString());
                        break;
                    case "Compressor2":
                        // Convert Compressor2 to Steinberg Compressor
                        var compressor = new AbletonCompressor(xelement);
                        var steinbergCompressor = compressor.ToSteinbergCompressor();
                        outputFilePath = Path.Combine(outputDirectoryPath, "Compressor", "Ableton - " + outputFileName);
                        IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Compressor"));
                        steinbergCompressor.Write(outputFilePath + ".vstpreset");

                        // and dump the text info as well
                        File.WriteAllText(outputFilePath + ".txt", steinbergCompressor.ToString());
                        break;
                    case "GlueCompressor":
                        // Convert Glue compressor to Waves SSL Compressor
                        var glueCompressor = new AbletonGlueCompressor(xelement);
                        var wavesSSLComp = glueCompressor.ToWavesSSLComp();
                        outputFilePath = Path.Combine(outputDirectoryPath, "SSLComp Stereo", "Ableton - " + outputFileName);
                        IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "SSLComp Stereo"));
                        wavesSSLComp.Write(outputFilePath + ".vstpreset");

                        // and dump the text info as well
                        File.WriteAllText(outputFilePath + ".txt", wavesSSLComp.ToString());
                        break;
                    case "MultibandDynamics":
                    case "AutoFilter":
                    case "Reverb":
                    case "Saturator":
                    case "Tuner":
                    default:
                        Log.Information("{0} not supported!", pluginName);
                        break;
                }
            }
        }

        private static void HandleAbletonLivePreset(string file, string outputDirectoryPath)
        {
            var bytes = File.ReadAllBytes(file);
            var decompressed = IOUtils.Decompress(bytes);
            var str = Encoding.UTF8.GetString(decompressed);
            var xelement = XElement.Parse(str);

            string outputFileName = Path.GetFileNameWithoutExtension(file);
            string outputFilePath = "";

            // find preset type
            var presetType = xelement.Elements().First().Name.ToString();
            switch (presetType)
            {
                case "Eq8":
                    // Convert EQ8 to Steinberg Frequency
                    var eq = new AbletonEq8(xelement);
                    var steinbergFrequency = eq.ToSteinbergFrequency();
                    outputFilePath = Path.Combine(outputDirectoryPath, "Frequency", "Ableton - " + outputFileName);
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Frequency"));
                    steinbergFrequency.Write(outputFilePath + ".vstpreset");

                    // and dump the text info as well
                    File.WriteAllText(outputFilePath + ".txt", steinbergFrequency.ToString());
                    break;
                case "Compressor2":
                    // Convert Compressor2 to Steinberg Compressor
                    var compressor = new AbletonCompressor(xelement);
                    var steinbergCompressor = compressor.ToSteinbergCompressor();
                    outputFilePath = Path.Combine(outputDirectoryPath, "Compressor", "Ableton - " + outputFileName);
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Compressor"));
                    steinbergCompressor.Write(outputFilePath + ".vstpreset");

                    // and dump the text info as well
                    File.WriteAllText(outputFilePath + ".txt", steinbergCompressor.ToString());
                    break;
                case "GlueCompressor":
                    // Convert Glue compressor to Waves SSL Compressor
                    var glueCompressor = new AbletonGlueCompressor(xelement);
                    var wavesSSLComp = glueCompressor.ToWavesSSLComp();
                    outputFilePath = Path.Combine(outputDirectoryPath, "SSLComp Stereo", "Ableton - " + outputFileName);
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "SSLComp Stereo"));
                    wavesSSLComp.Write(outputFilePath + ".vstpreset");

                    // and dump the text info as well
                    File.WriteAllText(outputFilePath + ".txt", wavesSSLComp.ToString());
                    break;
                case "MultibandDynamics":
                case "AutoFilter":
                case "Reverb":
                case "Saturator":
                case "Tuner":
                default:
                    Log.Information("{0} not supported!", presetType);
                    break;
            }
        }

        // Simple preset storage object to ensure we only process unique presets
        // the vstEffectIndex can be different but the rest of the variables must be equal 
        public class PresetInfo : IEquatable<PresetInfo>
        {
            public string OutputFileName { get; set; }
            public string PluginName { get; set; }
            public string GUID { get; set; }
            public int VsteffectIndex { get; set; }
            public byte[] Bytes { get; set; }


            public override string ToString()
            {
                return string.Format("{0} {1} {2} {3}", GUID, OutputFileName, PluginName, Bytes.Length);
            }

            public override bool Equals(object obj) => Equals(obj as PresetInfo);
            public override int GetHashCode() => (GUID, OutputFileName, PluginName, Bytes).GetHashCode();

            public bool Equals(PresetInfo other)
            {
                if (other is null) return false;

                return (this.OutputFileName.Equals(other.OutputFileName) &&
                this.PluginName.Equals(other.PluginName) &&
                this.GUID.Equals(other.GUID) &&
                this.Bytes.SequenceEqual(other.Bytes));
            }

            public static bool operator ==(PresetInfo presetInfo1, PresetInfo presetInfo2)
            {
                if (((object)presetInfo1) == null || ((object)presetInfo2) == null)
                    return Object.Equals(presetInfo1, presetInfo2);

                return presetInfo1.Equals(presetInfo2);
            }

            public static bool operator !=(PresetInfo presetInfo1, PresetInfo presetInfo2)
            {
                if (((object)presetInfo1) == null || ((object)presetInfo2) == null)
                    return !Object.Equals(presetInfo1, presetInfo2);

                return !(presetInfo1.Equals(presetInfo2));
            }
        }

        private static void HandleCubaseProjectFile(string file, string outputDirectoryPath, IConfiguration config, bool doConvertToKontakt6)
        {
            // read Kontakt library ids
            NKS.NksReadLibrariesInfo(config["NksSettingsPath"], true);

            // dictionary to hold the processed presets, to avoid duplicates
            var processedPresets = new List<PresetInfo>();

            // parse the project file
            var riffReader = new RIFFFileReader(file);
            var binaryFile = riffReader.BinaryFile;

            // second chunk should contain Cubase Project File information
            var infoChunk = riffReader.Chunks[1];

            // 'Cubase' field
            binaryFile.Seek(99);
            var cubaseLen = binaryFile.ReadInt32();
            var cubaseField = binaryFile.ReadString(cubaseLen, Encoding.ASCII).TrimEnd('\0');
            if (IsWrongField(binaryFile, "Cubase", cubaseField))
            {
                Log.Error("Fatal error! Could not read Cubase Project File!");
                return;
            }

            var versionLen = binaryFile.ReadInt32();
            var versionField = binaryFile.ReadString(versionLen, Encoding.ASCII).TrimEnd('\0');
            if (!versionField.StartsWith("Version"))
            {
                Log.Error("Fatal error! Could not read Cubase Project File!");
                return;
            }

            var versionText = versionField.Substring(8);
            var version = new Version(versionText);
            Log.Information("Found Cubase Version {0} Project File", version);

            // get fourth chunk
            var chunk = riffReader.Chunks[3];

            // get chunk byte array            
            var chunkBytes = chunk.Read((int)chunk.StartPosition, (int)chunk.ChunkDataSize);

            // search for 'VST Multitrack'
            var vstMultitrackBytePattern = Encoding.ASCII.GetBytes("VST Multitrack\0");
            var vstMultitrackIndices = chunkBytes.FindAll(vstMultitrackBytePattern).ToList();

            // since we are processing each entry while requiring the index of the next entry
            // we need to add an extra element to the list, 
            // namely the index of the very last byte in the chunk byte array
            if (vstMultitrackIndices.Count() > 0) vstMultitrackIndices.Add(chunkBytes.Length - 1);

            for (int i = 0, trackNumber = 1; i < vstMultitrackIndices.Count() - 1; i++, trackNumber++)
            {
                // the current and next index as within the chunk byte array
                int curChunkCopyIndex = vstMultitrackIndices.ElementAt(i);
                int nextChunkCopyIndex = vstMultitrackIndices.ElementAt(i + 1);

                // fix the index when using binaryFile which is the actual byte position
                // and not the positions within the byte array chunk copy
                // by adding the chunk start position
                int vstMultitrackCurrentIndex = (int)chunk.StartPosition + curChunkCopyIndex;
                int vstMultitrackNextIndex = (int)chunk.StartPosition + nextChunkCopyIndex;
                Log.Information("Found VST Multitrack at index: {0}", vstMultitrackCurrentIndex);
                binaryFile.Seek(vstMultitrackCurrentIndex);

                // 'VST Multitrack' field
                var vstMultitrackField = binaryFile.ReadString(vstMultitrackBytePattern.Length, Encoding.ASCII).TrimEnd('\0');
                var v1 = binaryFile.ReadInt32();
                var v2 = binaryFile.ReadInt32();
                var v3 = binaryFile.ReadInt32();

                if (version.Major > 8)
                {
                    // 'RuntimeID' field
                    var runtimeIDLen = binaryFile.ReadInt32();
                    var runtimeIDField = binaryFile.ReadString(runtimeIDLen, Encoding.ASCII).TrimEnd('\0');
                    if (IsWrongField(binaryFile, "RuntimeID", runtimeIDField)) continue;
                    var b1 = binaryFile.ReadBytes(10);
                }

                // 'Name' field
                var nameLen = binaryFile.ReadInt32();
                var nameField = binaryFile.ReadString(nameLen, Encoding.ASCII).TrimEnd('\0');
                if (IsWrongField(binaryFile, "Name", nameField)) continue;
                var v4 = binaryFile.ReadInt16();
                var v5 = binaryFile.ReadInt16();
                var v6 = binaryFile.ReadInt32();

                // 'String' field
                var stringLen = binaryFile.ReadInt32();
                var stringField = binaryFile.ReadString(stringLen, Encoding.ASCII).TrimEnd('\0');
                if (IsWrongField(binaryFile, "String", stringField)) continue;
                var v7 = binaryFile.ReadInt16();

                // Track Name (for channels supporting audio insert plugins)
                var trackNameLen = binaryFile.ReadInt32();
                var trackName = binaryFile.ReadString(trackNameLen, Encoding.UTF8);
                trackName = StringUtils.RemoveByteOrderMark(trackName);
                Log.Information("Processing track name: {0}", trackName);

                // reset the output filename
                string outputFileName = Path.GetFileNameWithoutExtension(file);
                outputFileName = string.Format("{0} {1:D3} - {2}", outputFileName, trackNumber, trackName);
                outputFileName = StringUtils.MakeValidFileName(outputFileName);

                if (version.Major > 8)
                {
                    // 'Type'
                    var typeLen = binaryFile.ReadInt32();
                    var typeField = binaryFile.ReadString(typeLen, Encoding.ASCII).TrimEnd('\0');
                    if (IsWrongField(binaryFile, "Type", typeField)) continue;
                }

                // skip to the next 'VstCtrlInternalEffect' field            
                var vstEffectBytePattern = Encoding.ASCII.GetBytes("VstCtrlInternalEffect\0");

                // since we are using the chunk byte pattern we can use the 
                // current and next index as is (without the start position) in the find method
                var vstEffectIndices = chunkBytes.FindAll(vstEffectBytePattern, curChunkCopyIndex, nextChunkCopyIndex);
                int vstEffectIndex = -1;
                foreach (var vstEffectChunkCopyIndex in vstEffectIndices)
                {
                    // fix the index when using binaryFile which is the actual byte position
                    // and not the positions within the byte array chunk copy
                    // by adding the chunk start position
                    vstEffectIndex = (int)chunk.StartPosition + vstEffectChunkCopyIndex;
                    Log.Information("Found VST Insert Effect at index: {0}", vstEffectIndex);
                    binaryFile.Seek(vstEffectIndex);

                    if (!HandleCubaseVstInsertEffect(processedPresets, binaryFile, vstEffectBytePattern, vstEffectIndex,
                        vstMultitrackCurrentIndex, vstMultitrackNextIndex,
                        outputDirectoryPath, outputFileName,
                        doConvertToKontakt6
                    )) continue;
                }
                if (vstEffectIndex < 0)
                {
                    Log.Warning("Could not find any insert effects ('VstCtrlInternalEffect')");
                }
            }
        }

        private static bool HandleCubaseVstInsertEffect(
            List<PresetInfo> processedPresets,
            BinaryFile binaryFile,
            byte[] vstEffectBytePattern, int vstEffectIndex,
            int vstMultitrackCurrentIndex, int vstMultitrackNextIndex,
            string outputDirectoryPath, string outputFileName,
            bool doConvertToKontakt6
            )
        {
            var vstEffectField = binaryFile.ReadString(vstEffectBytePattern.Length, Encoding.ASCII).TrimEnd('\0');

            var pluginFieldLen = binaryFile.ReadInt32();
            var pluginFieldField = binaryFile.ReadString(pluginFieldLen, Encoding.ASCII).TrimEnd('\0');
            if (IsWrongField(binaryFile, "Plugin", pluginFieldField)) return false;
            var t1 = binaryFile.ReadInt16();
            var t2 = binaryFile.ReadInt16();
            var t3 = binaryFile.ReadInt32();

            // 'Plugin UID' field
            var pluginUIDFieldLen = binaryFile.ReadInt32();
            var pluginUIDField = binaryFile.ReadString(pluginUIDFieldLen, Encoding.ASCII).TrimEnd('\0');
            if (IsWrongField(binaryFile, "Plugin UID", pluginUIDField)) return false;
            var t4 = binaryFile.ReadInt16();
            var t5 = binaryFile.ReadInt16();
            var t6 = binaryFile.ReadInt32();

            // 'GUID' field
            var guidFieldLen = binaryFile.ReadInt32();
            var guidField = binaryFile.ReadString(guidFieldLen, Encoding.ASCII).TrimEnd('\0');
            if (IsWrongField(binaryFile, "GUID", guidField)) return false;
            var t7 = binaryFile.ReadInt16();

            // GUID
            var guidLen = binaryFile.ReadInt32();
            var guid = binaryFile.ReadString(guidLen, Encoding.UTF8);
            guid = StringUtils.RemoveByteOrderMark(guid);
            Log.Information("GUID: {0}", guid);

            // 'Plugin Name' field
            var pluginNameFieldLen = binaryFile.ReadInt32();
            var pluginNameField = binaryFile.ReadString(pluginNameFieldLen, Encoding.ASCII).TrimEnd('\0');
            if (IsWrongField(binaryFile, "Plugin Name", pluginNameField)) return false;
            var t8 = binaryFile.ReadInt16();

            // Plugin Name
            var pluginNameLen = binaryFile.ReadInt32();
            var pluginName = binaryFile.ReadString(pluginNameLen, Encoding.UTF8);
            pluginName = StringUtils.RemoveByteOrderMark(pluginName);
            Log.Information("Plugin Name: {0}", pluginName);

            // 'Original Plugin Name' or 'Audio Input Count'
            var len = binaryFile.ReadInt32();
            var nextField = binaryFile.ReadString(len, Encoding.ASCII).TrimEnd('\0');

            string origPluginName = null;
            if (nextField.Equals("Original Plugin Name"))
            {
                var t9 = binaryFile.ReadInt16();
                var origPluginNameLen = binaryFile.ReadInt32();
                origPluginName = binaryFile.ReadString(origPluginNameLen, Encoding.UTF8);
                origPluginName = StringUtils.RemoveByteOrderMark(origPluginName);
                Log.Information("Original Plugin Name: {0}", origPluginName);
            }

            // skip to 'audioComponent'
            var audioComponentPattern = Encoding.ASCII.GetBytes("audioComponent\0");
            int audioComponentIndex = binaryFile.IndexOf(audioComponentPattern, 0, vstMultitrackNextIndex);
            if (audioComponentIndex < 0)
            {
                Log.Warning("Could not find the preset content ('audioComponent')");
                return false;
            }

            return HandleCubaseAudioComponent(
                processedPresets,
                binaryFile,
                audioComponentPattern,
                guid,
                vstEffectIndex,
                pluginName, origPluginName,
                outputDirectoryPath, outputFileName,
                doConvertToKontakt6);
        }

        private static bool HandleCubaseAudioComponent(
            List<PresetInfo> processedPresets,
            BinaryFile binaryFile,
            byte[] audioComponentPattern,
            string guid,
            int vstEffectIndex,
            string pluginName, string origPluginName,
            string outputDirectoryPath, string outputFileName,
            bool doConvertToKontakt6
        )
        {
            // 'audioComponent' field            
            var audioComponentField = binaryFile.ReadString(audioComponentPattern.Length, Encoding.ASCII).TrimEnd('\0');
            if (IsWrongField(binaryFile, "audioComponent", audioComponentField)) return false;

            var t10 = binaryFile.ReadInt16();
            var t11 = binaryFile.ReadInt16();
            var presetByteLen = binaryFile.ReadInt32();
            Log.Debug("Reading {0} bytes ...", presetByteLen);
            var presetBytes = binaryFile.ReadBytes(0, presetByteLen, BinaryFile.ByteOrder.LittleEndian);

            // store in processed preset list
            var presetInfo = new PresetInfo();
            presetInfo.OutputFileName = outputFileName;
            presetInfo.PluginName = pluginName;
            presetInfo.GUID = guid;
            presetInfo.VsteffectIndex = vstEffectIndex;
            presetInfo.Bytes = presetBytes;

            if (processedPresets.Contains(presetInfo))
            {
                int idx = processedPresets.IndexOf(presetInfo);
                var previouslyProcessed = processedPresets.ElementAt(idx);
                Log.Information("Skipping {0} preset at index {1} since we already have processed an identical at index {2}", presetInfo.PluginName, presetInfo.VsteffectIndex, previouslyProcessed.VsteffectIndex);
                return false;
            }
            else
            {
                processedPresets.Add(presetInfo);
            }

            var vstPreset = VstPresetFactory.GetVstPreset<VstPreset>(presetBytes, guid, origPluginName != null ? origPluginName + " - " + pluginName : pluginName);

            string fileNameNoExtensionPart = string.Format("{0} ({1}){2}", outputFileName, vstEffectIndex, origPluginName == null ? "" : " - " + origPluginName);
            fileNameNoExtensionPart = StringUtils.MakeValidFileName(fileNameNoExtensionPart);
            string fileNameNoExtension = fileNameNoExtensionPart;
            if (!fileNameNoExtensionPart.Contains(pluginName, StringComparison.InvariantCultureIgnoreCase))
            {
                fileNameNoExtension = string.Format("{0} - {1}", fileNameNoExtensionPart, pluginName);
            }
            fileNameNoExtension = StringUtils.MakeValidFileName(fileNameNoExtension);

            if (vstPreset.HasFXP)
            {
                var fxp = vstPreset.FXP;

                // write fxp content to file
                string fxpOutputFilePath = Path.Combine(outputDirectoryPath, fileNameNoExtension + ".fxp");
                fxp.Write(fxpOutputFilePath);

                if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQx64)
                {
                    var fabFilterProQ = vstPreset as FabfilterProQ;
                    HandleFabfilterPresetFile(fabFilterProQ, "FabFilterProQx64", outputDirectoryPath, fileNameNoExtensionPart);
                }

                else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2x64)
                {
                    var fabFilterProQ2 = vstPreset as FabfilterProQ2;
                    HandleFabfilterPresetFile(fabFilterProQ2, "FabFilterProQ2x64", outputDirectoryPath, fileNameNoExtensionPart);
                }

                else if (vstPreset.Vst3ID == VstPreset.VstIDs.NIKontakt5)
                {
                    var kontakt = vstPreset as NIKontakt5;
                    origPluginName = "Kontakt 5";

                    // check if we should convert to kontakt 6 64 out preset
                    if (doConvertToKontakt6)
                    {
                        origPluginName = "Kontakt 6";
                        kontakt.Vst3ID = VstPreset.VstIDs.NIKontakt6_64out;
                        kontakt.FXP.Content.FxID = "Ni$D"; // make sure to set the fxID to the right kontakt version
                    }

                    HandleNIKontaktFXP(kontakt, fxp, origPluginName, fileNameNoExtension, outputDirectoryPath);
                }

                else if (vstPreset.Vst3ID == VstPreset.VstIDs.NIKontakt6)
                {
                    var kontakt = vstPreset as NIKontakt6;
                    origPluginName = "Kontakt 6";

                    // check if we should convert to kontakt 6 64 out preset
                    if (doConvertToKontakt6)
                    {
                        origPluginName = "Kontakt 6";
                        kontakt.Vst3ID = VstPreset.VstIDs.NIKontakt6_64out;
                        kontakt.FXP.Content.FxID = "Ni$D"; // make sure to set the fxID to the right kontakt version
                    }

                    HandleNIKontaktFXP(kontakt, fxp, origPluginName, fileNameNoExtension, outputDirectoryPath);
                }

                else if (vstPreset.Vst3ID == VstPreset.VstIDs.NIKontakt6_64out)
                {
                    var kontakt = vstPreset as NIKontakt6_64out;
                    origPluginName = "Kontakt 6";

                    // already Kontakt 6

                    HandleNIKontaktFXP(kontakt, fxp, origPluginName, fileNameNoExtension, outputDirectoryPath);
                }
            }
            else
            {
                if (vstPreset.Parameters.Count > 0)
                {
                    // FabFilterProQ stores the parameters as floats not chunk
                    if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ)
                    {
                        var fabFilterProQ = vstPreset as FabfilterProQ;
                        HandleFabfilterPresetFile(fabFilterProQ, "FabfilterProQ", outputDirectoryPath, fileNameNoExtensionPart);
                    }

                    // FabFilterProQ2 stores the parameters as floats not chunk
                    else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2)
                    {
                        var fabFilterProQ2 = vstPreset as FabfilterProQ2;
                        HandleFabfilterPresetFile(fabFilterProQ2, "FabFilterProQ2", outputDirectoryPath, fileNameNoExtensionPart);
                    }

                    else if (vstPreset.Vst3ID == VstPreset.VstIDs.EastWestPlay)
                    {
                        var play = vstPreset as EastWestPlay;

                        // save the Play presets as .vstpreset files
                        string playOutputFilePath = Path.Combine(outputDirectoryPath, "Play", fileNameNoExtension);
                        IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Play"));
                        play.Write(playOutputFilePath + ".vstpreset");

                        // and dump the text info as well
                        File.WriteAllText(playOutputFilePath + ".txt", play.ToString());
                    }

                    // Save the preset parameters
                    else
                    {
                        string outputFilePath = Path.Combine(outputDirectoryPath, fileNameNoExtension);
                        File.WriteAllText(outputFilePath + ".txt", vstPreset.ToString());

                        // and output vstpreset as well
                        vstPreset.Write(outputFilePath + ".vstpreset");
                    }
                }
            }

            // read next field, we expect editController
            var editControllerLen = binaryFile.ReadInt32();
            var editControllerField = binaryFile.ReadString(editControllerLen, Encoding.ASCII).TrimEnd('\0');
            if (IsWrongField(binaryFile, "editController", editControllerField)) return false;

            return true;
        }

        private static void HandleNIKontaktFXP(NIKontaktBase kontakt, FXP fxp,
        string origPluginName,
        string fileNameNoExtension,
        string outputDirectoryPath)
        {
            string kontaktLibraryName = "";
            var snpid = GetSNPIDFromKontaktFXP(fxp);
            if (!string.IsNullOrEmpty(snpid))
            {
                Log.Debug("snpid: " + snpid);

                // loookup library name
                NksLibraryDesc lib = NKSLibraries.Libraries.Where(a => a.Key == snpid).FirstOrDefault().Value;
                if (lib != null)
                {
                    kontaktLibraryName = lib.Name;
                }
                else
                {
                    var snpidNum = NKS.ConvertToBase10(snpid);
                    if (snpidNum != snpid)
                    {
                        Log.Error("Could not find any kontakt libraries using the snpid: " + snpid + " (" + snpidNum + ") and filename: " + fileNameNoExtension);
                    }
                    else
                    {
                        Log.Error("Could not find any kontakt libraries using the snpid: " + snpid + " and filename: " + fileNameNoExtension);
                    }

                    kontaktLibraryName = snpid;
                }
                fileNameNoExtension += (" - " + kontaktLibraryName);
            }

            // save the kontakt presets as .vstpreset files
            string kontaktOutputFilePath = Path.Combine(outputDirectoryPath, origPluginName, fileNameNoExtension);
            IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, origPluginName));
            kontakt.Write(kontaktOutputFilePath + ".vstpreset");

            // also save as Kontakt NKI preset file
            // this doesn't seem to work properly
            // kontakt.WriteNKI(kontaktOutputFilePath + ".nki");

            // and dump the text info as well
            // File.WriteAllText(kontaktOutputFilePath + ".txt", kontakt.ToString());
        }

        private static string GetSNPIDFromKontaktFXP(FXP fxp)
        {
            var byteArray = new byte[0];
            if (fxp.Content is FXP.FxProgramSet)
            {
                byteArray = ((FXP.FxProgramSet)fxp.Content).ChunkData;
            }
            else if (fxp.Content is FXP.FxChunkSet)
            {
                byteArray = ((FXP.FxChunkSet)fxp.Content).ChunkData;
            }

            // read the snpid
            string snpid = null;
            using (BinaryFile bf = new BinaryFile(byteArray))
            {
                UInt32 fileSize = bf.ReadUInt32();

                if (fileSize == byteArray.Length)
                {
                    bf.Seek(543, SeekOrigin.Begin);
                    int snpidCount = bf.ReadInt32();
                    snpid = bf.ReadString(snpidCount * 2, Encoding.Unicode);

                    // snpid cannot have more than 4 characters (?!)
                    if (snpidCount > 4)
                    {
                        snpid = null;
                    }
                }
            }

            return snpid;
        }

        private static bool IsWrongField(BinaryFile binaryFile, string expectedValue, string foundValue)
        {
            if (foundValue != expectedValue)
            {
                Log.Warning("Expected '{0}' but got '{1}' at pos: {2}", expectedValue, foundValue, binaryFile.Position);
                return true;
            }
            return false;
        }
        private static void HandleSteinbergVstPreset(string file, string outputDirectoryPath)
        {
            var vstPreset = VstPresetFactory.GetVstPreset<VstPreset>(file);
            string fileNameNoExtension = Path.GetFileNameWithoutExtension(file);
            string outputFilePathText = Path.Combine(outputDirectoryPath, fileNameNoExtension + ".txt");

            // if not using chunk-data but parameters instead
            if (vstPreset.Parameters.Count > 0 && !vstPreset.HasFXP)
            {
                if (vstPreset.Vst3ID.Equals(VstPreset.VstIDs.WavesSSLCompStereo))
                {
                    // output the vstpreset
                    string wavesSSLCompOutputFilePath = Path.Combine(outputDirectoryPath, "Waves", fileNameNoExtension);
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Waves"));
                    vstPreset.Write(wavesSSLCompOutputFilePath + ".vstpreset");

                    // and dump the text info as well
                    File.WriteAllText(wavesSSLCompOutputFilePath + ".txt", vstPreset.ToString());
                }
                else if (vstPreset.Vst3ID.Equals(VstPreset.VstIDs.WavesSSLChannelStereo))
                {
                    // output the vstpreset
                    string wavesSSLChannelOutputFilePath = Path.Combine(outputDirectoryPath, "Waves", fileNameNoExtension);
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Waves"));
                    vstPreset.Write(wavesSSLChannelOutputFilePath + ".vstpreset");

                    // and dump the text info as well
                    File.WriteAllText(wavesSSLChannelOutputFilePath + ".txt", vstPreset.ToString());

                    // convert to UAD SSL Channel
                    var wavesSSLChannel = vstPreset as WavesSSLChannel;
                    var uadSSLChannel = wavesSSLChannel.ToUADSSLChannel();
                    string outputPresetFilePath = Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip", uadSSLChannel.PresetName);
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip"));
                    uadSSLChannel.Write(outputPresetFilePath + ".vstpreset");

                    // and dump the UAD SSL Channel info as well
                    File.WriteAllText(outputPresetFilePath + ".txt", uadSSLChannel.ToString());

                    // and store FXP as well
                    // uadSSLChannel.WriteFXP(outputPresetFilePath + ".fxp");
                }
                else if (vstPreset.Vst3ID.Equals(VstPreset.VstIDs.SteinbergREVerence))
                {
                    // output the vstpreset
                    string reverenceOutputFilePath = Path.Combine(outputDirectoryPath, "REVerence", fileNameNoExtension);
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "REVerence"));
                    vstPreset.Write(reverenceOutputFilePath + ".vstpreset");

                    // and dump the text info as well
                    File.WriteAllText(reverenceOutputFilePath + ".txt", vstPreset.ToString());
                }

                else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ)
                {
                    var fabFilterProQ = vstPreset as FabfilterProQ;
                    HandleFabfilterPresetFile(fabFilterProQ, "FabfilterProQ", outputDirectoryPath, fileNameNoExtension);
                }

                else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2)
                {
                    var fabFilterProQ2 = vstPreset as FabfilterProQ2;
                    HandleFabfilterPresetFile(fabFilterProQ2, "FabFilterProQ2", outputDirectoryPath, fileNameNoExtension);
                }

                else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ3)
                {
                    var fabFilterProQ3 = vstPreset as FabfilterProQ3;
                    HandleFabfilterPresetFile(fabFilterProQ3, "FabFilterProQ3", outputDirectoryPath, fileNameNoExtension);
                }

                // always output the information
                else
                {
                    // output the vstpreset
                    string presetOutputFilePath = Path.Combine(outputDirectoryPath, vstPreset.PlugInName, fileNameNoExtension);
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, vstPreset.PlugInName));
                    vstPreset.Write(presetOutputFilePath + ".vstpreset");

                    // and dump the text info as well
                    File.WriteAllText(presetOutputFilePath + ".txt", vstPreset.ToString());
                }
            }
            else
            {
                // use chunk data
                if (vstPreset.HasFXP)
                {
                    // check if FabFilterProQx64
                    if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQx64)
                    {
                        var fabFilterProQ = vstPreset as FabfilterProQ;
                        HandleFabfilterPresetFile(fabFilterProQ, "FabFilterProQx64", outputDirectoryPath, fileNameNoExtension);
                    }

                    // check if FabFilterProQ2x64
                    else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2x64)
                    {
                        var fabFilterProQ2 = vstPreset as FabfilterProQ2;
                        HandleFabfilterPresetFile(fabFilterProQ2, "FabFilterProQ2x64", outputDirectoryPath, fileNameNoExtension);
                    }

                    // check if FabFilter Pro Q3
                    else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ3)
                    {
                        var fabFilterProQ3 = vstPreset as FabfilterProQ3;
                        HandleFabfilterPresetFile(fabFilterProQ3, "FabFilterProQ3", outputDirectoryPath, fileNameNoExtension);
                    }

                    else if (vstPreset.Vst3ID == VstPreset.VstIDs.NIKontakt5)
                    {
                        var snpid = GetSNPIDFromKontaktFXP(vstPreset.FXP);
                        if (!string.IsNullOrEmpty(snpid))
                        {
                            Log.Debug("snpid: " + snpid);
                            fileNameNoExtension += ("_" + snpid);
                        }

                        // save the kontakt presets as .vstpreset files
                        string kontaktOutputFilePath = Path.Combine(outputDirectoryPath, "Kontakt 5", fileNameNoExtension + ".vstpreset");
                        IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Kontakt 5"));
                        vstPreset.Write(kontaktOutputFilePath);

                        // and dump the text info as well
                        // string kontaktOutputFilePathText = Path.Combine(outputDirectoryPath, "Kontakt 5", fileNameNoExtension + ".txt");
                        // File.WriteAllText(kontaktOutputFilePathText, vstPreset.ToString());
                    }

                    // always output the information
                    else
                    {
                        // output the vstpreset
                        string presetOutputFilePath = Path.Combine(outputDirectoryPath, vstPreset.PlugInName, fileNameNoExtension);
                        IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, vstPreset.PlugInName));
                        vstPreset.Write(presetOutputFilePath + ".vstpreset");

                        // and dump the text info as well
                        File.WriteAllText(presetOutputFilePath + ".txt", vstPreset.ToString());
                    }
                }

                // always output the information
                else
                {
                    File.WriteAllText(outputFilePathText, vstPreset.ToString());
                }
            }
        }

        private static void HandleFabfilterPresetFile(string file, string outputDirectoryPath)
        {
            string outputFileName = Path.GetFileNameWithoutExtension(file);

            float[] floatArray = null;
            floatArray = FabfilterProQBase.ReadFloats(file, "FPQr");
            if (floatArray != null)
            {
                var preset = new FabfilterProQ();
                if (preset.ReadFFP(file))
                {
                    HandleFabfilterPresetFile(preset, "FabfilterProQ", outputDirectoryPath, outputFileName);
                }
            }
            else
            {
                floatArray = FabfilterProQBase.ReadFloats(file, "FQ2p");
                if (floatArray != null)
                {
                    var preset = new FabfilterProQ2();
                    if (preset.ReadFFP(file))
                    {
                        HandleFabfilterPresetFile(preset, "FabfilterProQ2", outputDirectoryPath, outputFileName);
                    }
                }
                else
                {
                    floatArray = FabfilterProQBase.ReadFloats(file, "FQ3p");
                    if (floatArray != null)
                    {
                        var preset = new FabfilterProQ3();
                        if (preset.ReadFFP(file))
                        {
                            HandleFabfilterPresetFile(preset, "FabfilterProQ3", outputDirectoryPath, outputFileName);
                        }
                    }
                    else
                    {
                        // failed
                        Log.Error("Failed reading fabfilter eq information {0}...", file);
                    }
                }
            }
        }

        private static void HandleFabfilterPresetFile(FabfilterProQ preset, string pluginName, string outputDirectoryPath, string fileNameNoExtension)
        {
            // output the vstpreset
            string fabFilterOutputFilePath = Path.Combine(outputDirectoryPath, pluginName, fileNameNoExtension);
            IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, pluginName));
            preset.Write(fabFilterOutputFilePath + ".vstpreset");

            // and dump the text info as well
            File.WriteAllText(fabFilterOutputFilePath + ".txt", preset.ToString());

            // write the preset file as well
            preset.WriteFFP(fabFilterOutputFilePath + ".ffp");

            // convert to steinberg Frequency format
            var steinbergFrequency = preset.ToSteinbergFrequency();
            string frequencyOutputFilePath = Path.Combine(outputDirectoryPath, "Frequency", fileNameNoExtension + ".vstpreset");
            IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Frequency"));
            steinbergFrequency.Write(frequencyOutputFilePath);

            // and dump the steinberg frequency info as well
            string frequencyOutputFilePathText = Path.Combine(outputDirectoryPath, "Frequency", fileNameNoExtension + ".txt");
            File.WriteAllText(frequencyOutputFilePathText, steinbergFrequency.ToString());
        }

        private static void HandleFabfilterPresetFile(FabfilterProQ2 preset, string pluginName, string outputDirectoryPath, string fileNameNoExtension)
        {
            // output the vstpreset
            string fabFilterOutputFilePath = Path.Combine(outputDirectoryPath, pluginName, fileNameNoExtension);
            IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, pluginName));
            preset.Write(fabFilterOutputFilePath + ".vstpreset");

            // and dump the text info as well
            File.WriteAllText(fabFilterOutputFilePath + ".txt", preset.ToString());

            // write the preset file as well
            preset.WriteFFP(fabFilterOutputFilePath + ".ffp");

            // convert to steinberg Frequency format
            var steinbergFrequency = preset.ToSteinbergFrequency();
            string frequencyOutputFilePath = Path.Combine(outputDirectoryPath, "Frequency", fileNameNoExtension + ".vstpreset");
            IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Frequency"));
            steinbergFrequency.Write(frequencyOutputFilePath);

            // and dump the steinberg frequency info as well
            string frequencyOutputFilePathText = Path.Combine(outputDirectoryPath, "Frequency", fileNameNoExtension + ".txt");
            File.WriteAllText(frequencyOutputFilePathText, steinbergFrequency.ToString());
        }

        private static void HandleFabfilterPresetFile(FabfilterProQ3 preset, string pluginName, string outputDirectoryPath, string fileNameNoExtension)
        {
            // output the vstpreset (Note! have not tested if the vstpreset file works!)
            string fabFilterOutputFilePath = Path.Combine(outputDirectoryPath, pluginName, fileNameNoExtension);
            IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, pluginName));
            preset.Write(fabFilterOutputFilePath + ".vstpreset");

            // and dump the text info as well
            File.WriteAllText(fabFilterOutputFilePath + ".txt", preset.ToString());

            // write the preset file as well
            preset.WriteFFP(fabFilterOutputFilePath + ".ffp");

            // convert to steinberg Frequency format
            var steinbergFrequency = preset.ToSteinbergFrequency();
            string frequencyOutputFilePath = Path.Combine(outputDirectoryPath, "Frequency", fileNameNoExtension + ".vstpreset");
            IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Frequency"));
            steinbergFrequency.Write(frequencyOutputFilePath);

            // and dump the steinberg frequency info as well
            string frequencyOutputFilePathText = Path.Combine(outputDirectoryPath, "Frequency", fileNameNoExtension + ".txt");
            File.WriteAllText(frequencyOutputFilePathText, steinbergFrequency.ToString());
        }

        private static void HandleWavesXpsPreset(string file, string outputDirectoryPath)
        {
            // Convert Waves SSLChannel to UAD SSLChannel
            string outputFileName = Path.GetFileNameWithoutExtension(file);
            string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName + ".txt");
            TextWriter tw = new StreamWriter(outputFilePath);
            List<WavesSSLChannel> channelPresetList = WavesPreset.ReadXps<WavesSSLChannel>(file);
            foreach (var wavesSSLChannel in channelPresetList)
            {
                // convert to UAD SSL Channel
                var uadSSLChannel = wavesSSLChannel.ToUADSSLChannel();
                string outputPresetFilePath = Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip", uadSSLChannel.PresetName);
                IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip"));
                uadSSLChannel.Write(outputPresetFilePath + ".vstpreset");

                // and dump the UAD SSL Channel info as well
                File.WriteAllText(outputPresetFilePath + ".txt", uadSSLChannel.ToString());

                // and store FXP as well
                // string outputFXPFilePath = Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip", uadSSLChannel.PresetName + ".fxp");
                // uadSSLChannel.WriteFXP(outputFXPFilePath);

                // // dump original Wave SSL Channel preset
                // string outputPresetFilePathOrig = Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip", uadSSLChannel.PresetName + "_wavesorig.vstpreset");
                // wavesSSLChannel.Write(outputPresetFilePathOrig);
                // wavesSSLChannel.WriteTextSummary(outputPresetFilePathOrig + "_text.txt");

                // // convert back to Waves SSL Channel
                // var wavesSSLChannelNew = uadSSLChannel.ToWavesSSLChannel();
                // string outputPresetFilePathNew = Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip", uadSSLChannel.PresetName + "_wavesnew.vstpreset");
                // wavesSSLChannelNew.Write(outputPresetFilePathNew);
                // wavesSSLChannelNew.WriteTextSummary(outputPresetFilePathNew + "_text.txt");

                // write text content
                tw.WriteLine(wavesSSLChannel);
                tw.WriteLine();
                tw.WriteLine("-------------------------------------------------------");
            }

            // Convert Waves SSLComp to UAD SSLComp
            List<WavesSSLComp> compPresetList = WavesPreset.ReadXps<WavesSSLComp>(file);
            foreach (var wavesSSLComp in compPresetList)
            {
                // write text content
                tw.WriteLine(wavesSSLComp);
                tw.WriteLine();
                tw.WriteLine("-------------------------------------------------------");
            }
            tw.Close();
        }

        private static void HandleWaveFile(string file, string outputDirectoryPath, string inputExtra)
        {
            var images = new List<string>();
            if (inputExtra != null) images.Add(inputExtra);

            if (file.Contains("Quad.wav"))
            {
                // Generate Steinberg REVerence vst preset
                if (file.Contains("Altiverb"))
                {
                    REVerenceVSTPresetGenerator.CreatePreset(file, images, outputDirectoryPath, "Altiverb_", 2);
                }
                else if (file.Contains("Bricasti"))
                {
                    REVerenceVSTPresetGenerator.CreatePreset(file, images, outputDirectoryPath, "Bricasti_");
                }
                else if (file.Contains("TCE System"))
                {
                    REVerenceVSTPresetGenerator.CreatePreset(file, images, outputDirectoryPath, "TCE_");
                }
                else
                {
                    REVerenceVSTPresetGenerator.CreatePreset(file, images, outputDirectoryPath);
                }
            }
            else if (file.Contains("Lexicon"))
            {
                REVerenceVSTPresetGenerator.CreatePreset(file, images, outputDirectoryPath, "", 2);
            }
            else
            {
                Log.Information("Ignoring {0} ...", file);
            }
        }

        private static void HandleSDIRFile(string file, string outputDirectoryPath)
        {
            // Convert Logic Space Designer Impulse files to .wav
            LogicSpaceDesignerImpulse sdir = LogicSpaceDesignerImpulse.ReadSdirPreset(file);
            if (sdir != null)
            {
                string outputFileName = Path.GetFileNameWithoutExtension(file);
                string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName + ".wav");
                SoundIO.WriteWaveFile(outputFilePath, sdir.WaveformData, false, sdir.Channels, sdir.SampleRate, sdir.BitsPerSample);
            }
        }

        private static void HandleNIKontaktFile(string inputDirectoryOrFilePath, string outputDirectory, string extension, IConfiguration config, bool doList, bool doVerbose, bool doPack, bool doWCX)
        {
            // check if we are using a wcx plugin
            if (doWCX)
            {
                var appExecutionPath = IOUtils.GetApplicationExecutionPath();
                var wcxPluginPath = Path.Combine(appExecutionPath, "WCXPlugins", "nkx.wcx64");

                if (doVerbose)
                {
                    WCXUtils.Call64BitWCXPlugin(wcxPluginPath, inputDirectoryOrFilePath, outputDirectory, WCXUtils.TodoOperations.TODO_FLIST);
                }
                else if (doList)
                {
                    WCXUtils.Call64BitWCXPlugin(wcxPluginPath, inputDirectoryOrFilePath, outputDirectory, WCXUtils.TodoOperations.TODO_LIST);
                }
                else
                {
                    if (doPack)
                    {
                        WCXUtils.Call64BitWCXPlugin(wcxPluginPath, inputDirectoryOrFilePath, outputDirectory, WCXUtils.TodoOperations.TODO_PACK);
                    }
                    else
                    {
                        if (!IOUtils.IsDirectory(inputDirectoryOrFilePath).Value)
                        {
                            // if this is a file, make sure to append the file (without extension) to the output path
                            string outputFileName = Path.GetFileNameWithoutExtension(inputDirectoryOrFilePath);
                            outputDirectory = Path.Combine(outputDirectory, outputFileName);
                        }
                        WCXUtils.Call64BitWCXPlugin(wcxPluginPath, inputDirectoryOrFilePath, outputDirectory, WCXUtils.TodoOperations.TODO_UNPACK);
                    }
                }
            }
            else
            {
                // use internal methods 

                // read the library info (keys and ids etc.)
                NKS.NksReadLibrariesInfo(config["NksSettingsPath"]);

                if (extension == ".nki")
                {
                    NKI.Unpack(inputDirectoryOrFilePath, outputDirectory, doList, doVerbose);
                }
                else if (extension == ".nicnt")
                {
                    if (doPack)
                    {
                        NICNT.Pack(inputDirectoryOrFilePath, outputDirectory, doList, doVerbose);
                    }
                    else
                    {
                        NICNT.Unpack(inputDirectoryOrFilePath, outputDirectory, doList, doVerbose);
                    }
                }
                else if (extension == ".ncw")
                {
                    NCW.NCW2Wav(inputDirectoryOrFilePath, outputDirectory, doList, doVerbose);
                }
                else
                {
                    try
                    {
                        if (doVerbose)
                        {
                            var memStream = new MemoryStream();
                            var streamWriter = new StreamWriter(memStream);

                            streamWriter.WriteLine("\nRegistryLibraryInfo:");
                            NKS.PrintRegistryLibraryInfo(streamWriter);

                            streamWriter.WriteLine("SettingsLibraryInfo:");
                            NKS.PrintSettingsLibraryInfo(config["NksSettingsPath"], streamWriter);

                            streamWriter.WriteLine("NKLibsLibraryInfo:");
                            NKS.PrintNKLibsLibraryInfo(streamWriter);

                            streamWriter.Flush();
                            string libraryInfo = Encoding.UTF8.GetString(memStream.ToArray());

                            Log.Debug(libraryInfo);
                            memStream.Close();

                            NKS.Scan(inputDirectoryOrFilePath);
                        }
                        else if (doList)
                        {
                            NKS.List(inputDirectoryOrFilePath);
                        }
                        else
                        {
                            NKS.Unpack(inputDirectoryOrFilePath, outputDirectory);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Log.Error("Error processing {0} ({1})...", inputDirectoryOrFilePath, e);
                    }
                }
            }
        }

        private static void HandleWindowsFile(string inputDirectoryOrFilePath, string outputDirectoryPath, IConfiguration config, bool doList, bool doVerbose)
        {
            string outputFileName = Path.GetFileNameWithoutExtension(inputDirectoryOrFilePath);
            var destinationDirectoryPath = Path.Combine(outputDirectoryPath, outputFileName);

            try
            {
                if (doList)
                {
                    ResourceExtractor.List(inputDirectoryOrFilePath);
                }
                else
                {
                    IOUtils.CreateDirectoryIfNotExist(destinationDirectoryPath);
                    ResourceExtractor.ExtractAll(inputDirectoryOrFilePath, destinationDirectoryPath);
                }
            }
            catch (System.Exception)
            {
                Log.Error("Failed loading resource! This means that the resource is probably packed. Use a tool like upx (https://github.com/upx/upx) to unpack before running this script again.");
            }
        }
    }
}
