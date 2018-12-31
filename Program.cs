using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CommonUtils;
using CommonUtils.Audio;
using CSCore.Codecs.RIFF;
using McMaster.Extensions.CommandLineUtils;
using PresetConverter;
using SDIR2WavConverter;
using Serilog;
using Serilog.Events;

namespace AbletonLiveConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup command line parser
            var app = new CommandLineApplication();
            app.Name = "AbletonLiveConverter";
            app.Description = "Convert Ableton Live presets to readable formats";
            app.HelpOption();
            var optionInputDirectory = app.Option("-i|--input <path>", "The Input directory", CommandOptionType.SingleValue);
            var optionOutputDirectory = app.Option("-o|--output <path>", "The Output directory", CommandOptionType.SingleValue);
            var optionInputExtra = app.Option("-e|--extra <path>", "Extra information as used by the different converters", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                if (optionInputDirectory.HasValue()
                && optionOutputDirectory.HasValue())
                {
                    string inputDirectoryPath = optionInputDirectory.Value();
                    string outputDirectoryPath = optionOutputDirectory.Value();
                    string inputExtra = optionInputExtra.Value();

                    // Setup Logger
                    string errorLogFilePath = Path.Combine(outputDirectoryPath, "log-error.log");
                    string verboseLogFilePath = Path.Combine(outputDirectoryPath, "log-verbose.log");
                    var logConfig = new LoggerConfiguration()
                        .WriteTo.File(verboseLogFilePath)
                        .WriteTo.Console(LogEventLevel.Information)
                        .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error).WriteTo.File(errorLogFilePath))
                        ;
                    logConfig.MinimumLevel.Verbose();
                    Log.Logger = logConfig.CreateLogger();

                    var extensions = new List<string> { ".als", ".adv", ".vstpreset", ".xps", ".wav", ".sdir", ".cpr", ".ffp" };
                    var files = Directory.GetFiles(inputDirectoryPath, "*.*", SearchOption.AllDirectories)
                    .Where(s => extensions.Contains(Path.GetExtension(s).ToLowerInvariant()));

                    foreach (var file in files)
                    {
                        Log.Information("Processing {0} ...", file);

                        string extension = new FileInfo(file).Extension.ToLowerInvariant();
                        switch (extension)
                        {
                            case ".als":
                                HandleAbletonLiveProject(file, outputDirectoryPath);
                                break;
                            case ".adv":
                                HandleAbletonLivePreset(file, outputDirectoryPath);
                                break;
                            case ".vstpreset":
                                HandleSteinbergVstPreset(file, outputDirectoryPath);
                                break;
                            case ".xps":
                                HandleWavesXpsPreset(file, outputDirectoryPath);
                                break;
                            case ".wav":
                                HandleWaveFile(file, outputDirectoryPath, inputExtra);
                                break;
                            case ".sdir":
                                HandleSDIRFile(file, outputDirectoryPath);
                                break;
                            case ".cpr":
                                HandleCubaseProjectFile(file, outputDirectoryPath);
                                break;
                            case ".ffp":
                                HandleFabfilterPresetFile(file, outputDirectoryPath);
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

        private static void HandleAbletonLiveProject(string file, string outputDirectoryPath)
        {
            var bytes = File.ReadAllBytes(file);
            var decompressed = Decompress(bytes);
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
                        outputFilePath = Path.Combine(outputDirectoryPath, "Frequency", "Ableton - " + outputFileName + ".vstpreset");
                        CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Frequency"));
                        steinbergFrequency.Write(outputFilePath);
                        break;
                    case "Compressor2":
                        // Convert Compressor2 to Steinberg Compressor
                        var compressor = new AbletonCompressor(xelement);
                        var steinbergCompressor = compressor.ToSteinbergCompressor();
                        outputFilePath = Path.Combine(outputDirectoryPath, "Compressor", "Ableton - " + outputFileName + ".vstpreset");
                        CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Compressor"));
                        steinbergCompressor.Write(outputFilePath);
                        break;
                    case "GlueCompressor":
                        // Convert Glue compressor to Waves SSL Compressor
                        var glueCompressor = new AbletonGlueCompressor(xelement);
                        var wavesSSLComp = glueCompressor.ToWavesSSLComp();
                        outputFilePath = Path.Combine(outputDirectoryPath, "SSLComp Stereo", "Ableton - " + outputFileName + ".vstpreset");
                        CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "SSLComp Stereo"));
                        wavesSSLComp.Write(outputFilePath);
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
            var decompressed = Decompress(bytes);
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
                    outputFilePath = Path.Combine(outputDirectoryPath, "Frequency", "Ableton - " + outputFileName + ".vstpreset");
                    CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Frequency"));
                    steinbergFrequency.Write(outputFilePath);
                    break;
                case "Compressor2":
                    // Convert Compressor2 to Steinberg Compressor
                    var compressor = new AbletonCompressor(xelement);
                    var steinbergCompressor = compressor.ToSteinbergCompressor();
                    outputFilePath = Path.Combine(outputDirectoryPath, "Compressor", "Ableton - " + outputFileName + ".vstpreset");
                    CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Compressor"));
                    steinbergCompressor.Write(outputFilePath);
                    break;
                case "GlueCompressor":
                    // Convert Glue compressor to Waves SSL Compressor
                    var glueCompressor = new AbletonGlueCompressor(xelement);
                    var wavesSSLComp = glueCompressor.ToWavesSSLComp();
                    outputFilePath = Path.Combine(outputDirectoryPath, "SSLComp Stereo", "Ableton - " + outputFileName + ".vstpreset");
                    CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "SSLComp Stereo"));
                    wavesSSLComp.Write(outputFilePath);
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

        private static void HandleCubaseProjectFile(string file, string outputDirectoryPath)
        {
            var riffReader = new RIFFFileReader(file, false);

            // get fourth chunk
            var chunk = riffReader.Chunks[3];

            // get chunk byte array            
            var chunkBytes = chunk.Read((int)chunk.StartPosition, (int)chunk.ChunkDataSize);

            // search for 'VST Multitrack'
            var vstMultitrackBytePattern = Encoding.ASCII.GetBytes("VST Multitrack\0");
            var vstMultitrackIndices = chunkBytes.FindAll(vstMultitrackBytePattern).ToList();

            // since we are processing each entry based on the previous
            // we will not process the last index without adding an extra element
            // to the list, namely the index of the very last byte in the chunk byte array
            if (vstMultitrackIndices.Count() > 0) vstMultitrackIndices.Add(chunkBytes.Length - 1);

            var binaryFile = riffReader.BinaryFile;
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

                // 'RuntimeID' field
                var runtimeIDLen = binaryFile.ReadInt32();
                var runtimeIDField = binaryFile.ReadString(runtimeIDLen, Encoding.ASCII).TrimEnd('\0');
                if (IsWrongField(binaryFile, "RuntimeID", runtimeIDField)) continue;
                var b1 = binaryFile.ReadBytes(10);

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
                outputFileName = string.Format("{0}_{1:D3}_{2}", outputFileName, trackNumber, trackName);
                outputFileName = StringUtils.MakeValidFileName(outputFileName);

                // 'Type'
                var typeLen = binaryFile.ReadInt32();
                var typeField = binaryFile.ReadString(typeLen, Encoding.ASCII).TrimEnd('\0');
                if (IsWrongField(binaryFile, "Type", typeField)) continue;

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

                    if (!HandleCubaseVstInsertEffect(binaryFile, vstEffectBytePattern, vstEffectIndex,
                        vstMultitrackCurrentIndex, vstMultitrackNextIndex,
                        outputDirectoryPath, outputFileName
                    )) continue;
                }
                if (vstEffectIndex < 0)
                {
                    Log.Warning("Could not find any insert effects ('VstCtrlInternalEffect')");
                }
            }
        }

        private static bool HandleCubaseVstInsertEffect(
            BinaryFile binaryFile,
            byte[] vstEffectBytePattern, int vstEffectIndex,
            int vstMultitrackCurrentIndex, int vstMultitrackNextIndex,
            string outputDirectoryPath, string outputFileName
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
            pluginName = pluginName.Replace("\0", "");
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
                origPluginName = origPluginName.Replace("\0", "");
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

            return HandleCubaseAudioComponent(binaryFile,
            audioComponentPattern,
            guid,
            vstEffectIndex,
            pluginName, origPluginName,
            outputDirectoryPath, outputFileName);
        }

        private static bool HandleCubaseAudioComponent(BinaryFile binaryFile,
            byte[] audioComponentPattern,
            string guid,
            int vstEffectIndex,
            string pluginName, string origPluginName,
            string outputDirectoryPath, string outputFileName
        )
        {
            // 'audioComponent' field            
            var audioComponentField = binaryFile.ReadString(audioComponentPattern.Length, Encoding.ASCII).TrimEnd('\0');
            if (IsWrongField(binaryFile, "audioComponent", audioComponentField)) return false;

            var t10 = binaryFile.ReadInt16();
            var t11 = binaryFile.ReadInt16();
            var presetByteLen = binaryFile.ReadInt32();
            Log.Debug("Reading preset bytes: {0}", presetByteLen);
            var presetBytes = binaryFile.ReadBytes(0, presetByteLen, BinaryFile.ByteOrder.LittleEndian);
            var vstPreset = VstPresetFactory.GetVstPreset<VstPreset>(presetBytes, guid, origPluginName != null ? origPluginName + " - " + pluginName : pluginName);

            string fileNameNoExtensionPart = string.Format("{0}_{1}{2}", outputFileName, vstEffectIndex, origPluginName == null ? "" : "_" + origPluginName);
            fileNameNoExtensionPart = StringUtils.MakeValidFileName(fileNameNoExtensionPart);
            string fileNameNoExtension = string.Format("{0}_{1}", fileNameNoExtensionPart, pluginName);
            fileNameNoExtension = StringUtils.MakeValidFileName(fileNameNoExtension);

            if (vstPreset.HasFXP)
            {
                var fxp = vstPreset.FXP;

                // write fxp content to file
                string fxpOutputFilePath = Path.Combine(outputDirectoryPath, fileNameNoExtension + ".fxp");
                fxp.Write(fxpOutputFilePath);

                // check if FabFilterProQ2 
                if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2x64)
                {
                    if (fxp.Content is FXP.FxSet)
                    {
                        var set = (FXP.FxSet)fxp.Content;

                        for (int i = 0; i < set.NumPrograms; i++)
                        {
                            var program = set.Programs[i];
                            var parameters = program.Parameters;

                            // Note that the floats are stored as IEEE (meaning between 0.0 - 1.0)
                            var preset = FabfilterProQ2.Convert2FabfilterProQ2(parameters);
                            string presetOutputFileName = set.NumPrograms > 1 ? string.Format("{0}{1}", fileNameNoExtensionPart, i) : fileNameNoExtensionPart;
                            HandleFabfilterPresetFile(preset, "FabFilterProQ2x64", outputDirectoryPath, presetOutputFileName);
                        }
                    }
                }
                else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQx64)
                {
                    if (fxp.Content is FXP.FxSet)
                    {
                        var set = (FXP.FxSet)fxp.Content;

                        for (int i = 0; i < set.NumPrograms; i++)
                        {
                            var program = set.Programs[i];
                            var parameters = program.Parameters;

                            // Note that the floats are stored as IEEE (meaning between 0.0 - 1.0)
                            var preset = FabfilterProQ.Convert2FabfilterProQ(parameters);
                            string presetOutputFileName = set.NumPrograms > 1 ? string.Format("{0}{1}", fileNameNoExtensionPart, i) : fileNameNoExtensionPart;
                            HandleFabfilterPresetFile(preset, "FabFilterProQx64", outputDirectoryPath, presetOutputFileName);
                        }
                    }
                }

                // check if NI Kontakt 5
                else if (vstPreset.Vst3ID == VstPreset.VstIDs.NIKontakt5)
                {
                    // save the kontakt presets as .vstpreset files
                    string kontaktOutputFilePath = Path.Combine(outputDirectoryPath, "Kontakt 5", fileNameNoExtension + ".vstpreset");
                    CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Kontakt 5"));
                    vstPreset.Write(kontaktOutputFilePath);

                    // and dump the text info as well
                    string kontaktOutputFilePathText = Path.Combine(outputDirectoryPath, "Kontakt 5", fileNameNoExtension + ".txt");
                    File.WriteAllText(kontaktOutputFilePathText, vstPreset.ToString());
                }
            }
            else
            {
                if (vstPreset.Parameters.Count > 0)
                {
                    // FabFilterProQ stores the parameters as floats not chunk
                    // Note that the floats are not stored as IEEE (meaning between 0.0 - 1.0) but as floats representing the real values 
                    if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ)
                    {
                        var parameters = vstPreset.Parameters.Select(a => (float)a.Value.NumberValue).ToArray();
                        var preset = FabfilterProQ.Convert2FabfilterProQ(parameters, false);
                        HandleFabfilterPresetFile(preset, "FabfilterProQ", outputDirectoryPath, fileNameNoExtensionPart);
                    }

                    // FabFilterProQ2 stores the parameters as floats not chunk
                    // Note that the floats are not stored as IEEE (meaning between 0.0 - 1.0) but as floats representing the real values 
                    else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2)
                    {
                        var parameters = vstPreset.Parameters.Select(a => (float)a.Value.NumberValue).ToArray();
                        var preset = FabfilterProQ2.Convert2FabfilterProQ2(parameters, false);
                        HandleFabfilterPresetFile(preset, "FabFilterProQ2", outputDirectoryPath, fileNameNoExtensionPart);
                    }

                    // Save the preset parameters
                    else
                    {
                        string outputFilePath = Path.Combine(outputDirectoryPath, fileNameNoExtension + ".txt");
                        File.WriteAllText(outputFilePath, vstPreset.ToString());
                    }
                }
            }

            // read next field, we expect editController
            var editControllerLen = binaryFile.ReadInt32();
            var editControllerField = binaryFile.ReadString(editControllerLen, Encoding.ASCII).TrimEnd('\0');
            if (IsWrongField(binaryFile, "editController", editControllerField)) return false;

            return true;
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
            string outputFilePath = Path.Combine(outputDirectoryPath, fileNameNoExtension + ".txt");

            // if not using chunk-data but parameters instead
            if (vstPreset.Parameters.Count > 0 && !vstPreset.HasFXP)
            {
                if (vstPreset.Vst3ID.Equals(VstPreset.VstIDs.WavesSSLCompStereo))
                {
                    using (var tw = new StreamWriter(outputFilePath))
                    {
                        List<WavesSSLComp> compPresetList = WavesPreset.ParseXml<WavesSSLComp>(vstPreset.Parameters.FirstOrDefault().Value.StringValue);
                        foreach (var wavesSSLComp in compPresetList)
                        {
                            tw.WriteLine(wavesSSLComp);
                        }
                    }
                }
                else if (vstPreset.Vst3ID.Equals(VstPreset.VstIDs.WavesSSLChannelStereo))
                {
                    using (var tw = new StreamWriter(outputFilePath))
                    {
                        List<WavesSSLChannel> channelPresetList = WavesPreset.ParseXml<WavesSSLChannel>(vstPreset.Parameters.FirstOrDefault().Value.StringValue);
                        foreach (var wavesSSLChannel in channelPresetList)
                        {
                            tw.WriteLine(wavesSSLChannel);

                            // convert to UAD SSL Channel
                            var uadSSLChannel = wavesSSLChannel.ToUADSSLChannel();
                            string outputPresetFilePath = Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip", uadSSLChannel.PresetName);
                            CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip"));
                            uadSSLChannel.Write(outputPresetFilePath + ".vstpreset");

                            // and dump the UAD SSL Channel info as well
                            File.WriteAllText(outputPresetFilePath + ".txt", uadSSLChannel.ToString());

                            // and store FXP as well
                            // string outputFXPFilePath = Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip", uadSSLChannel.PresetName + ".fxp");
                            // uadSSLChannel.WriteFXP(outputFXPFilePath);
                        }
                    }
                }
                else if (vstPreset.Vst3ID.Equals(VstPreset.VstIDs.SteinbergREVerence))
                {
                    string reverenceOutputFilePath = Path.Combine(outputDirectoryPath, "REVerence", "Converted_" + fileNameNoExtension);
                    CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "REVerence"));
                    vstPreset.Write(reverenceOutputFilePath + ".vstpreset");

                    // and dump the text info as well
                    File.WriteAllText(reverenceOutputFilePath + ".txt", vstPreset.ToString());
                }

                else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ)
                {
                    var parameters = vstPreset.Parameters.Select(a => (float)a.Value.NumberValue).ToArray();
                    var preset = FabfilterProQ.Convert2FabfilterProQ(parameters, false);
                    HandleFabfilterPresetFile(preset, "FabfilterProQ", outputDirectoryPath, fileNameNoExtension);
                }

                else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2)
                {
                    var parameters = vstPreset.Parameters.Select(a => (float)a.Value.NumberValue).ToArray();
                    var preset = FabfilterProQ2.Convert2FabfilterProQ2(parameters, false);
                    HandleFabfilterPresetFile(preset, "FabFilterProQ2", outputDirectoryPath, fileNameNoExtension);
                }

                // always output the information
                else
                {
                    File.WriteAllText(outputFilePath, vstPreset.ToString());
                }
            }
            else
            {
                // use chunk data
                if (vstPreset.HasFXP)
                {
                    var fxp = vstPreset.FXP;

                    // check if FabFilterProQx64
                    if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQx64)
                    {
                        if (fxp.Content is FXP.FxSet)
                        {
                            var set = (FXP.FxSet)fxp.Content;

                            for (int i = 0; i < set.NumPrograms; i++)
                            {
                                var program = set.Programs[i];
                                var parameters = program.Parameters;
                                var preset = FabfilterProQ.Convert2FabfilterProQ(parameters);
                                string presetOutputFileName = set.NumPrograms > 1 ? string.Format("{0}{1}", fileNameNoExtension, i) : fileNameNoExtension;
                                HandleFabfilterPresetFile(preset, "FabFilterProQx64", outputDirectoryPath, presetOutputFileName);
                            }
                        }
                    }
                    // check if FabFilterProQ2x64
                    else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2x64)
                    {
                        if (fxp.Content is FXP.FxSet)
                        {
                            var set = (FXP.FxSet)fxp.Content;

                            for (int i = 0; i < set.NumPrograms; i++)
                            {
                                var program = set.Programs[i];
                                var parameters = program.Parameters;
                                var preset = FabfilterProQ2.Convert2FabfilterProQ2(parameters);
                                string presetOutputFileName = set.NumPrograms > 1 ? string.Format("{0}{1}", fileNameNoExtension, i) : fileNameNoExtension;
                                HandleFabfilterPresetFile(preset, "FabFilterProQ2x64", outputDirectoryPath, presetOutputFileName);
                            }
                        }
                    }

                    // check if NI Kontakt 5
                    else if (vstPreset.Vst3ID == VstPreset.VstIDs.NIKontakt5)
                    {
                        // save the kontakt presets as .vstpreset files
                        string kontaktOutputFilePath = Path.Combine(outputDirectoryPath, "Kontakt 5", fileNameNoExtension + ".vstpreset");
                        CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Kontakt 5"));
                        vstPreset.Write(kontaktOutputFilePath);

                        // and dump the tex info as well
                        string kontaktOutputFilePathText = Path.Combine(outputDirectoryPath, "Kontakt 5", fileNameNoExtension + ".txt");
                        File.WriteAllText(kontaktOutputFilePathText, vstPreset.ToString());
                    }

                    // always output the information
                    else
                    {
                        File.WriteAllText(outputFilePath, vstPreset.ToString());
                    }
                }

                // always output the information
                else
                {
                    File.WriteAllText(outputFilePath, vstPreset.ToString());
                }
            }
        }

        private static void HandleFabfilterPresetFile(string file, string outputDirectoryPath)
        {
            string outputFileName = Path.GetFileNameWithoutExtension(file);

            float[] floatArray = null;
            floatArray = FabfilterProQ.ReadFloats(file);
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
                floatArray = FabfilterProQ2.ReadFloats(file);
                var preset = new FabfilterProQ2();
                if (preset.ReadFFP(file))
                {
                    HandleFabfilterPresetFile(preset, "FabfilterProQ2", outputDirectoryPath, outputFileName);
                }
            }
        }

        private static void HandleFabfilterPresetFile(FabfilterProQ preset, string pluginName, string outputDirectoryPath, string outputFileName)
        {
            string fileNameNoExtension = string.Format("{0}_{1}", outputFileName, pluginName);
            string outputFilePath = Path.Combine(outputDirectoryPath, fileNameNoExtension);
            File.WriteAllText(outputFilePath + ".txt", preset.ToString());

            // write the preset file as well
            preset.WriteFFP(outputFilePath + ".ffp");

            // convert to steinberg Frequency format
            var steinbergFrequency = preset.ToSteinbergFrequency();
            string frequencyOutputFilePath = Path.Combine(outputDirectoryPath, "Frequency", fileNameNoExtension + ".vstpreset");
            CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Frequency"));
            steinbergFrequency.Write(frequencyOutputFilePath);

            // and dump the steinberg frequency info as well
            string frequencyOutputFilePathText = Path.Combine(outputDirectoryPath, "Frequency", fileNameNoExtension + ".txt");
            File.WriteAllText(frequencyOutputFilePathText, steinbergFrequency.ToString());
        }

        private static void HandleFabfilterPresetFile(FabfilterProQ2 preset, string pluginName, string outputDirectoryPath, string outputFileName)
        {
            string fileNameNoExtension = string.Format("{0}_{1}", outputFileName, pluginName);
            string outputFilePath = Path.Combine(outputDirectoryPath, fileNameNoExtension);
            File.WriteAllText(outputFilePath + ".txt", preset.ToString());

            // write the preset file as well
            preset.WriteFFP(outputFilePath + ".ffp");

            // convert to steinberg Frequency format
            var steinbergFrequency = preset.ToSteinbergFrequency();
            string frequencyOutputFilePath = Path.Combine(outputDirectoryPath, "Frequency", fileNameNoExtension + ".vstpreset");
            CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Frequency"));
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
                CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip"));
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

        private static void CreateDirectoryIfNotExist(string filePath)
        {
            try
            {
                Directory.CreateDirectory(filePath);
            }
            catch (Exception ex)
            {
                // handle them here
            }
        }

        private static byte[] Decompress(byte[] gzip)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
                CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
    }
}
