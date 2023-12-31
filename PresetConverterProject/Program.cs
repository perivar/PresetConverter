using System.Text;
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
        static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            Console.WriteLine("Using Environment (DOTNET_ENVIRONMENT): {0}", environmentName);

            IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            // uncomment to generate new combined SPNID CSV List
            // SNPID_CSVParsers.GenerateSNPIDList(config["NksSettingsPath"]);
            // return;

            // Setup command line parser
            var app = new CommandLineApplication
            {
                Name = "PresetConverter",
                Description = "Convert different DAW presets to other formats (both fxp, vstpresets and txt)"
            };

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

                    Log.Information("Using Environment (DOTNET_ENVIRONMENT): {0}", environmentName);

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
                                    HandleWindowsFile(inputFilePath, outputDirectoryPath, doList);
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
            catch (Exception e)
            {
                Log.Error("{0}", e.Message);
            }
        }

        private static IEnumerable<string> HandleMultipleInputPaths(CommandOption optionInputDirectoryOrFilePath, List<string> extensions)
        {
            List<string> files = new();

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
            var decompressed = bytes;
            if (bytes[0] == 0x1F && bytes[1] == 0x8B)
            {
                decompressed = IOUtils.Decompress(bytes);
            }
            var str = Encoding.UTF8.GetString(decompressed);
            var rootXElement = XElement.Parse(str);

            AbletonProject.HandleAbletonLiveContent(rootXElement, file, outputDirectoryPath);
        }

        private static void HandleAbletonLivePreset(string file, string outputDirectoryPath)
        {
            var bytes = File.ReadAllBytes(file);
            var decompressed = bytes;
            if (bytes[0] == 0x1F && bytes[1] == 0x8B)
            {
                decompressed = IOUtils.Decompress(bytes);
            }
            var str = Encoding.UTF8.GetString(decompressed);
            var rootXElement = XElement.Parse(str);

            AbletonProject.DoDevices(rootXElement, rootXElement, null, null, new string[] { "preset" }, outputDirectoryPath, file);
        }

        private static void HandleCubaseProjectFile(string file, string outputDirectoryPath, IConfiguration config, bool doConvertToKontakt6)
        {
            // read Kontakt library ids
            NKS.NksReadLibrariesInfo(config["NksSettingsPath"], true);

            // dictionary to hold the processed presets, to avoid duplicates
            var processedPresets = new List<CubasePresetInfo>();

            // parse the project file
            var riffReader = new RIFFFileReader(file);
            var binaryFile = riffReader.BinaryFile;

            // 'Cubase' field
            binaryFile.Seek(99);
            if (!ReadCubaseField(binaryFile, "Cubase", out string? _))
            {
                Log.Fatal("Fatal error! Could not read Cubase Project File!");
                return;
            }

            // Version field including version number
            if (!ReadCubaseField(binaryFile, null, out string versionField))
            {
                Log.Fatal("Fatal error! Could not read Cubase Project File!");
                return;
            }

            Version version;
            if (!versionField.StartsWith("Version"))
            {
                Log.Error("Fatal error! Could not read Cubase Project File!");
                return;
            }
            else
            {
                // Store version
                var versionText = versionField.Substring(8);
                version = new Version(versionText);
                Log.Information("Found Cubase Version {0} Project File", version);
            }

            // Release Date
            if (!ReadCubaseField(binaryFile, null, out string releaseDateField))
            {
                Log.Error("Fatal error! Could not read release date!");
                return;
            }
            Log.Information("Release Date: {0}", releaseDateField);

            // skip four bytes and try to read architecture
            binaryFile.ReadInt32();

            if (!ReadCubaseField(binaryFile, null, out string architectureField))
            {
                // Older 32-bit versions of Cubase didn't list the architecture in the project file.
                Log.Information("Architecture: Unknown");
            }
            Log.Information("Architecture: {0}", architectureField);

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
            if (vstMultitrackIndices.Count > 0) vstMultitrackIndices.Add(chunkBytes.Length - 1);

            for (int i = 0, trackNumber = 1; i < vstMultitrackIndices.Count - 1; i++, trackNumber++)
            {
                // the current and next index as within the chunk byte array
                int curChunkCopyIndex = vstMultitrackIndices.ElementAt(i);
                int nextChunkCopyIndex = vstMultitrackIndices.ElementAt(i + 1);

                // fix the index when using binaryFile which is the actual byte position
                // and not the positions within the byte array chunk copy
                // by adding the chunk start position
                int vstMultitrackCurrentIndex = (int)chunk.StartPosition + curChunkCopyIndex;
                int vstMultitrackNextIndex = (int)chunk.StartPosition + nextChunkCopyIndex;
                Log.Information("Found VST Multitrack at index {0} to {1}", vstMultitrackCurrentIndex, vstMultitrackNextIndex - 1);
                binaryFile.Seek(vstMultitrackCurrentIndex);

                // 'VST Multitrack' field
                var vstMultitrackField = binaryFile.ReadString(vstMultitrackBytePattern.Length, Encoding.ASCII).TrimEnd('\0');
                if (IsWrongField("VST Multitrack", vstMultitrackField, binaryFile.Position))
                {
                    continue;
                }

                binaryFile.ReadInt32();
                binaryFile.ReadInt32();
                binaryFile.ReadInt32();

                if (version.Major > 8)
                {
                    // 'RuntimeID' field
                    if (!ReadCubaseField(binaryFile, "RuntimeID", out string? _))
                    {
                        continue;
                    }

                    // skip some bytes
                    binaryFile.ReadBytes(10);
                }

                // 'Name' field
                if (!ReadCubaseField(binaryFile, "Name", out string? _))
                {
                    continue;
                }

                binaryFile.ReadInt32();
                binaryFile.ReadInt32();

                // 'String' field
                if (!ReadCubaseField(binaryFile, "String", out string? _))
                {
                    continue;
                }

                binaryFile.ReadInt16();

                // Track Name (for channels supporting audio insert plugins)
                if (ReadCubaseUTF8String(binaryFile, out string? trackName))
                {
                    if (!string.IsNullOrEmpty(trackName))
                    {
                        Log.Information("Processing track name: {0}", trackName);
                    }
                    else
                    {
                        Log.Information("Processing empty track name");
                    }
                }

                // reset the output filename
                string outputFileName = Path.GetFileNameWithoutExtension(file);
                outputFileName = string.Format("{0} {1:D3} - {2}", outputFileName, trackNumber, trackName);
                outputFileName = StringUtils.MakeValidFileName(outputFileName);

                if (version.Major > 8)
                {
                    // 'Type'
                    if (!ReadCubaseField(binaryFile, "Type", out string? _))
                    {
                        continue;
                    }
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
            List<CubasePresetInfo> processedPresets,
            BinaryFile binaryFile,
            byte[] vstEffectBytePattern,
            int vstEffectIndex,
            int vstMultitrackCurrentIndex,
            int vstMultitrackNextIndex,
            string outputDirectoryPath,
            string outputFileName,
            bool doConvertToKontakt6
            )
        {

            // 'VstCtrlInternalEffect' field
            var vstEffectField = binaryFile.ReadString(vstEffectBytePattern.Length, Encoding.ASCII).TrimEnd('\0');
            if (IsWrongField("VstCtrlInternalEffect", vstEffectField, binaryFile.Position))
            {
                return false;
            }

            // 'Plugin' field
            if (!ReadCubaseField(binaryFile, "Plugin", out string? _))
            {
                return false;
            }

            binaryFile.ReadInt32();
            binaryFile.ReadInt32();

            // 'Plugin UID' field
            if (!ReadCubaseField(binaryFile, "Plugin UID", out string? _))
            {
                return false;
            }

            binaryFile.ReadInt32();
            binaryFile.ReadInt32();

            // 'GUID' field
            if (!ReadCubaseField(binaryFile, "GUID", out string? _))
            {
                return false;
            }

            binaryFile.ReadInt16();

            // GUID
            if (!ReadCubaseUTF8String(binaryFile, out string? guid))
            {
                return false;
            }
            Log.Information("GUID: {0}", guid);

            // 'Plugin Name' field
            if (!ReadCubaseField(binaryFile, "Plugin Name", out string? _))
            {
                return false;
            }

            binaryFile.ReadInt16();

            // Plugin Name
            if (!ReadCubaseUTF8String(binaryFile, out string? pluginName))
            {
                return false;
            }
            Log.Information("Plugin Name: {0}", pluginName);

            // 'Original Plugin Name' or 'Audio Input Count'
            if (!ReadCubaseField(binaryFile, null, out string? nextField))
            {
                return false;
            }

            string? origPluginName = null;
            if (nextField.Equals("Original Plugin Name"))
            {
                binaryFile.ReadInt16();

                if (ReadCubaseUTF8String(binaryFile, out origPluginName))
                {
                    Log.Information("Original Plugin Name: {0}", origPluginName);
                }
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
                pluginName,
                origPluginName,
                outputDirectoryPath,
                outputFileName,
                doConvertToKontakt6);
        }

        private static bool HandleCubaseAudioComponent(
            List<CubasePresetInfo> processedPresets,
            BinaryFile binaryFile,
            byte[] audioComponentPattern,
            string guid,
            int vstEffectIndex,
            string pluginName,
            string origPluginName,
            string outputDirectoryPath,
            string outputFileName,
            bool doConvertToKontakt6
        )
        {
            // 'audioComponent' field            
            var audioComponentField = binaryFile.ReadString(audioComponentPattern.Length, Encoding.ASCII).TrimEnd('\0');
            if (IsWrongField("audioComponent", audioComponentField, binaryFile.Position)) return false;

            binaryFile.ReadInt32();

            var presetByteLen = binaryFile.ReadInt32();
            Log.Debug("Reading {0} bytes ...", presetByteLen);
            var presetBytes = binaryFile.ReadBytes(0, presetByteLen, BinaryFile.ByteOrder.LittleEndian);

            // store in processed preset list
            var presetInfo = new CubasePresetInfo
            {
                OutputFileName = outputFileName,
                PluginName = pluginName,
                GUID = guid,
                VsteffectIndex = vstEffectIndex,
                Bytes = presetBytes
            };

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

                if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.FabFilterProQx64)
                {
                    var fabFilterProQ = vstPreset as FabfilterProQ;
                    HandleFabfilterPresetFile(fabFilterProQ, "FabFilterProQx64", outputDirectoryPath, fileNameNoExtensionPart);
                }

                else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.FabFilterProQ2x64)
                {
                    var fabFilterProQ2 = vstPreset as FabfilterProQ2;
                    HandleFabfilterPresetFile(fabFilterProQ2, "FabFilterProQ2x64", outputDirectoryPath, fileNameNoExtensionPart);
                }

                else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.NIKontakt5)
                {
                    var kontakt = vstPreset as NIKontakt5;
                    origPluginName = "Kontakt 5";

                    // check if we should convert to kontakt 6 64 out preset
                    if (doConvertToKontakt6)
                    {
                        origPluginName = "Kontakt 6";
                        kontakt.Vst3ClassID = VstPreset.VstClassIDs.NIKontakt6_64out;
                        kontakt.FXP.Content.FxID = "Ni$D"; // make sure to set the fxID to the right kontakt version
                    }

                    HandleNIKontaktFXP(kontakt, fxp, origPluginName, fileNameNoExtension, outputDirectoryPath);
                }

                else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.NIKontakt6)
                {
                    var kontakt = vstPreset as NIKontakt6;
                    origPluginName = "Kontakt 6";

                    // check if we should convert to kontakt 6 64 out preset
                    if (doConvertToKontakt6)
                    {
                        origPluginName = "Kontakt 6";
                        kontakt.Vst3ClassID = VstPreset.VstClassIDs.NIKontakt6_64out;
                        kontakt.FXP.Content.FxID = "Ni$D"; // make sure to set the fxID to the right kontakt version
                    }

                    HandleNIKontaktFXP(kontakt, fxp, origPluginName, fileNameNoExtension, outputDirectoryPath);
                }

                else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.NIKontakt6_64out)
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
                    if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.FabFilterProQ)
                    {
                        var fabFilterProQ = vstPreset as FabfilterProQ;
                        HandleFabfilterPresetFile(fabFilterProQ, "FabfilterProQ", outputDirectoryPath, fileNameNoExtensionPart);
                    }

                    // FabFilterProQ2 stores the parameters as floats not chunk
                    else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.FabFilterProQ2)
                    {
                        var fabFilterProQ2 = vstPreset as FabfilterProQ2;
                        HandleFabfilterPresetFile(fabFilterProQ2, "FabFilterProQ2", outputDirectoryPath, fileNameNoExtensionPart);
                    }

                    else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.EastWestPlay)
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
            if (IsWrongField("editController", editControllerField, binaryFile.Position)) return false;

            return true;
        }

        private static bool ReadCubaseField(BinaryFile binaryFile, string? verifyFieldValue, out string? fieldValue)
        {
            int fieldLength = binaryFile.ReadInt32();

            if (fieldLength > 0)
            {
                // read ascii string
                fieldValue = binaryFile.ReadString(fieldLength, Encoding.ASCII);

                if (fieldValue.EndsWith('\0'))
                {
                    fieldValue = fieldValue.TrimEnd('\0');

                    if (IsWrongField(verifyFieldValue, fieldValue, binaryFile.Position))
                    {
                        return false;
                    }

                    return true;
                }
            }
            else
            {
                // when told to read zero bytes, we are fine and return true
                fieldValue = null;
                return true;
            }

            return false;
        }

        private static bool ReadCubaseUTF8String(BinaryFile binaryFile, out string? fieldValue)
        {
            int fieldLength = binaryFile.ReadInt32();

            if (fieldLength > 0)
            {
                // read utf8 string
                fieldValue = binaryFile.ReadString(fieldLength, Encoding.UTF8);
                fieldValue = StringUtils.RemoveByteOrderMark(fieldValue);

                if (fieldValue.EndsWith('\0'))
                {
                    fieldValue = fieldValue.TrimEnd('\0');
                    return true;
                }
            }
            else
            {
                // when told to read zero bytes, we are fine and return true
                fieldValue = null;
                return true;
            }

            return false;
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

        private static string? GetSNPIDFromKontaktFXP(FXP fxp)
        {
            var byteArray = Array.Empty<byte>();
            if (fxp.Content is FXP.FxProgramSet prgSet)
            {
                byteArray = prgSet.ChunkData;
            }
            else if (fxp.Content is FXP.FxChunkSet chkSet)
            {
                byteArray = chkSet.ChunkData;
            }

            // read the snpid
            string? snpid = null;
            using (BinaryFile bf = new(byteArray))
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

        private static bool IsWrongField(string? expectedValue, string foundValue, long position)
        {
            if (string.IsNullOrEmpty(expectedValue)) return false;

            if (foundValue != expectedValue)
            {
                Log.Warning("Expected '{0}' but got '{1}' @ pos: {2}", expectedValue, foundValue, position);
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
                if (vstPreset.Vst3ClassID.Equals(VstPreset.VstClassIDs.WavesSSLCompStereo))
                {
                    // output the vstpreset
                    string wavesSSLCompOutputFilePath = Path.Combine(outputDirectoryPath, "Waves", fileNameNoExtension);
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Waves"));
                    vstPreset.Write(wavesSSLCompOutputFilePath + ".vstpreset");

                    // and dump the text info as well
                    File.WriteAllText(wavesSSLCompOutputFilePath + ".txt", vstPreset.ToString());
                }
                else if (vstPreset.Vst3ClassID.Equals(VstPreset.VstClassIDs.WavesSSLChannelStereo))
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
                else if (vstPreset.Vst3ClassID.Equals(VstPreset.VstClassIDs.SteinbergREVerence))
                {
                    // output the vstpreset
                    string reverenceOutputFilePath = Path.Combine(outputDirectoryPath, "REVerence", fileNameNoExtension);
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "REVerence"));
                    vstPreset.Write(reverenceOutputFilePath + ".vstpreset");

                    // and dump the text info as well
                    File.WriteAllText(reverenceOutputFilePath + ".txt", vstPreset.ToString());
                }

                else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.FabFilterProQ)
                {
                    var fabFilterProQ = vstPreset as FabfilterProQ;
                    HandleFabfilterPresetFile(fabFilterProQ, "FabfilterProQ", outputDirectoryPath, fileNameNoExtension);
                }

                else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.FabFilterProQ2)
                {
                    var fabFilterProQ2 = vstPreset as FabfilterProQ2;
                    HandleFabfilterPresetFile(fabFilterProQ2, "FabFilterProQ2", outputDirectoryPath, fileNameNoExtension);
                }

                else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.FabFilterProQ3)
                {
                    var fabFilterProQ3 = vstPreset as FabfilterProQ3;
                    HandleFabfilterPresetFile(fabFilterProQ3, "FabFilterProQ3", outputDirectoryPath, fileNameNoExtension);
                }

                // always output the information
                else
                {
                    // output the vstpreset
                    string presetOutputFilePath = Path.Combine(outputDirectoryPath, vstPreset.PlugInName ?? "Unknown", fileNameNoExtension);
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, vstPreset.PlugInName ?? "Unknown"));
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
                    if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.FabFilterProQx64)
                    {
                        var fabFilterProQ = vstPreset as FabfilterProQ;
                        HandleFabfilterPresetFile(fabFilterProQ, "FabFilterProQx64", outputDirectoryPath, fileNameNoExtension);
                    }

                    // check if FabFilterProQ2x64
                    else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.FabFilterProQ2x64)
                    {
                        var fabFilterProQ2 = vstPreset as FabfilterProQ2;
                        HandleFabfilterPresetFile(fabFilterProQ2, "FabFilterProQ2x64", outputDirectoryPath, fileNameNoExtension);
                    }

                    // check if FabFilter Pro Q3
                    else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.FabFilterProQ3)
                    {
                        var fabFilterProQ3 = vstPreset as FabfilterProQ3;
                        HandleFabfilterPresetFile(fabFilterProQ3, "FabFilterProQ3", outputDirectoryPath, fileNameNoExtension);
                    }

                    else if (vstPreset.Vst3ClassID == VstPreset.VstClassIDs.NIKontakt5)
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
                        string presetOutputFilePath = Path.Combine(outputDirectoryPath, vstPreset.PlugInName ?? "Unknown", fileNameNoExtension);
                        IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, vstPreset.PlugInName ?? "Unknown"));
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

            float[]? floatArray = null;
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
            // and fxp file as well
            preset.WriteFXP(fabFilterOutputFilePath + ".fxp");

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

                Log.Information("Processing Kontakt file with WCX using path: {0}", wcxPluginPath);

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
                Log.Information("Processing Kontakt file normally using config: {0}", config["NksSettingsPath"]);

                if (doVerbose)
                {
                    Log.Verbose("Debug information for all settings:");

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

                    Log.Verbose(libraryInfo);
                    memStream.Close();
                }

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
                    catch (Exception e)
                    {
                        Log.Error("Error processing {0} ({1})...", inputDirectoryOrFilePath, e);
                    }
                }
            }
        }

        private static void HandleWindowsFile(string inputDirectoryOrFilePath, string outputDirectoryPath, bool doList)
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
            catch (Exception)
            {
                Log.Error("Failed loading resource! This means that the resource is probably packed. Use a tool like upx (https://github.com/upx/upx) to unpack before running this script again.");
            }
        }
    }
}
