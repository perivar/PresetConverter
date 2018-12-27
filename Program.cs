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
            var vstMultitrackIndices = chunkBytes.FindAll(vstMultitrackBytePattern);

            int trackNumber = 1;
            var binaryFile = riffReader.BinaryFile;
            foreach (int index in vstMultitrackIndices)
            {
                int vstMultitrackIndex = (int)chunk.StartPosition + index;
                Log.Debug("vstMultitrackIndex: {0}", vstMultitrackIndex);
                binaryFile.Seek(vstMultitrackIndex);

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
                Log.Debug("TrackName: {0}", trackName);

                // reset the output filename
                string outputFileName = Path.GetFileNameWithoutExtension(file);
                outputFileName = string.Format("{0} - {1:D4} - {2}", outputFileName, trackNumber, trackName);
                outputFileName = StringUtils.MakeValidFileName(outputFileName);
                trackNumber++;

                // 'Type'
                var typeLen = binaryFile.ReadInt32();
                var typeField = binaryFile.ReadString(typeLen, Encoding.ASCII).TrimEnd('\0');
                if (IsWrongField(binaryFile, "Type", typeField)) continue;

                // skip to the 'VstCtrlInternalEffect' field            
                var vstEffectBytePattern = Encoding.ASCII.GetBytes("VstCtrlInternalEffect\0");
                int vstEffectIndex = binaryFile.IndexOf(vstEffectBytePattern, 0, (int)chunk.ChunkDataSize - vstMultitrackIndex);
                if (vstEffectIndex < 0)
                {
                    Log.Warning("Could not find any insert effects ('VstCtrlInternalEffect')");
                    continue;
                }
                var vstEffectField = binaryFile.ReadString(vstEffectBytePattern.Length, Encoding.ASCII).TrimEnd('\0');

                var pluginFieldLen = binaryFile.ReadInt32();
                var pluginFieldField = binaryFile.ReadString(pluginFieldLen, Encoding.ASCII).TrimEnd('\0');
                var t1 = binaryFile.ReadInt16();
                var t2 = binaryFile.ReadInt16();
                var t3 = binaryFile.ReadInt32();

                // 'Plugin UID' field
                var pluginUIDFieldLen = binaryFile.ReadInt32();
                var pluginUIDField = binaryFile.ReadString(pluginUIDFieldLen, Encoding.ASCII).TrimEnd('\0');
                if (IsWrongField(binaryFile, "Plugin UID", pluginUIDField)) continue;
                var t4 = binaryFile.ReadInt16();
                var t5 = binaryFile.ReadInt16();
                var t6 = binaryFile.ReadInt32();

                // 'GUID' field
                var guidFieldLen = binaryFile.ReadInt32();
                var guidField = binaryFile.ReadString(guidFieldLen, Encoding.ASCII).TrimEnd('\0');
                if (IsWrongField(binaryFile, "GUID", guidField)) continue;
                var t7 = binaryFile.ReadInt16();

                // GUID
                var guidLen = binaryFile.ReadInt32();
                var guid = binaryFile.ReadString(guidLen, Encoding.UTF8);
                guid = StringUtils.RemoveByteOrderMark(guid);
                Log.Debug("GUID: {0}", guid);

                // 'Plugin Name' field
                var pluginNameFieldLen = binaryFile.ReadInt32();
                var pluginNameField = binaryFile.ReadString(pluginNameFieldLen, Encoding.ASCII).TrimEnd('\0');
                if (IsWrongField(binaryFile, "Plugin Name", pluginNameField)) continue;
                var t8 = binaryFile.ReadInt16();

                // Plugin Name
                var pluginNameLen = binaryFile.ReadInt32();
                var pluginName = binaryFile.ReadString(pluginNameLen, Encoding.UTF8);
                pluginName = pluginName.Replace("\0", "");
                Log.Debug("Plugin Name: {0}", pluginName);

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
                    Log.Debug("Original Plugin Name: {0}", origPluginName);
                }

                // skip to 'audioComponent'
                var audioComponentPattern = Encoding.ASCII.GetBytes("audioComponent\0");
                int audioComponentIndex = binaryFile.IndexOf(audioComponentPattern, 0, (int)chunk.ChunkDataSize - vstMultitrackIndex);
                if (audioComponentIndex < 0)
                {
                    Log.Warning("Could not find the preset content ('audioComponent')");
                    continue;
                }

                // 'audioComponent' field            
                var audioComponentField = binaryFile.ReadString(audioComponentPattern.Length, Encoding.ASCII).TrimEnd('\0');
                if (IsWrongField(binaryFile, "audioComponent", audioComponentField)) continue;

                var t10 = binaryFile.ReadInt16();
                var t11 = binaryFile.ReadInt16();
                var presetByteLen = binaryFile.ReadInt32();
                Log.Debug("Reading preset bytes: {0}", presetByteLen);
                var presetBytes = binaryFile.ReadBytes(0, presetByteLen, BinaryFile.ByteOrder.LittleEndian);
                var vstPreset = new SteinbergVstPreset(guid, presetBytes);

                if (vstPreset.HasChunkData())
                {
                    var fxp = new FXP(vstPreset.GetChunkData());
                    string fileNameNoExtension = string.Format("{0} - {1}{2}{3}", outputFileName, vstEffectIndex, origPluginName == null ? " - " : " - " + origPluginName + " - ", pluginName);
                    fileNameNoExtension = StringUtils.MakeValidFileName(fileNameNoExtension);
                    string outputFilePath = Path.Combine(outputDirectoryPath, fileNameNoExtension + ".fxp");
                    fxp.Write(outputFilePath);

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

                                // using (var tw = new StreamWriter(outputFilePathNew))
                                // {
                                // int counter = 0;
                                // foreach (var f in parameters)
                                // {
                                //     tw.WriteLine("{0:0.0000}", f);
                                //     counter++;
                                //     if (counter % 7 == 0) tw.WriteLine();
                                // }
                                // }

                                var preset = FabfilterProQ2.Convert2FabfilterProQ(parameters);
                                string outputFileNameNew = string.Format("{0}_{1}", outputFileName, i);
                                HandleFabfilterPresetFile(preset, "FabFilterProQ2x64", outputDirectoryPath, outputFileNameNew);
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

                                // using (var tw = new StreamWriter(outputFilePathNew))
                                // {
                                // int counter = 0;
                                // foreach (var f in parameters)
                                // {
                                //     tw.WriteLine("{0:0.0000}", f);
                                //     counter++;
                                //     if ((counter - 1) % 7 == 0) tw.WriteLine();
                                // }
                                // }

                                var preset = FabfilterProQ.Convert2FabfilterProQ(parameters);
                                string outputFileNameNew = string.Format("{0}_{1}", outputFileName, i);
                                HandleFabfilterPresetFile(preset, "FabFilterProQx64", outputDirectoryPath, outputFileNameNew);
                            }
                        }
                    }
                }
                else
                {
                    if (vstPreset.Parameters.Count > 0)
                    {
                        // FabFilterProQ stores the parameters as floats not chunk
                        if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ)
                        {
                            var parameters = vstPreset.Parameters.Select(a => (float)a.Value.NumberValue).ToArray();
                            var preset = FabfilterProQ.Convert2FabfilterProQ(parameters, false);
                            HandleFabfilterPresetFile(preset, "FabfilterProQ", outputDirectoryPath, outputFileName);
                        }

                        // FabFilterProQ2 stores the parameters as floats not chunk
                        else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2)
                        {
                            var parameters = vstPreset.Parameters.Select(a => (float)a.Value.NumberValue).ToArray();
                            var preset = FabfilterProQ2.Convert2FabfilterProQ(parameters, false);
                            HandleFabfilterPresetFile(preset, "FabFilterProQ2", outputDirectoryPath, outputFileName);
                        }

                        // Save the preset parameters
                        else
                        {
                            string fileNameNoExtension = string.Format("{0} - {1}{2}{3}", outputFileName, vstEffectIndex, origPluginName == null ? " - " : " - " + origPluginName + " - ", pluginName);
                            fileNameNoExtension = StringUtils.MakeValidFileName(fileNameNoExtension);
                            string outputFilePath = Path.Combine(outputDirectoryPath, fileNameNoExtension + ".txt");
                            File.WriteAllText(outputFilePath, vstPreset.ToString());
                        }
                    }
                }

                // read next field, we expect editController
                var editControllerLen = binaryFile.ReadInt32();
                var editControllerField = binaryFile.ReadString(editControllerLen, Encoding.ASCII).TrimEnd('\0');
                if (IsWrongField(binaryFile, "editController", editControllerField)) continue;
            }
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
            var vstPreset = new SteinbergVstPreset(file);
            string outputFileName = Path.GetFileNameWithoutExtension(file);
            string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName + ".txt");

            // if not using chunk-data but parameters instead
            if (vstPreset.Parameters.Count > 0 && !vstPreset.HasChunkData())
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
                            string outputPresetFilePath = Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip", uadSSLChannel.PresetName + ".vstpreset");
                            CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip"));
                            uadSSLChannel.Write(outputPresetFilePath);

                            string outputFXPFilePath = Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip", uadSSLChannel.PresetName + ".fxp");
                            uadSSLChannel.WriteFXP(outputFXPFilePath);
                        }
                    }
                }
                else if (vstPreset.Vst3ID.Equals(VstPreset.VstIDs.SteinbergREVerence))
                {
                    var reverence = new SteinbergREVerence();

                    // copy parameters to the new preset
                    if (vstPreset.Parameters.ContainsKey("wave-file-path-1")) reverence.WavFilePath1 = vstPreset.Parameters["wave-file-path-1"].StringValue;
                    if (vstPreset.Parameters.ContainsKey("wave-file-path-2")) reverence.WavFilePath2 = vstPreset.Parameters["wave-file-path-2"].StringValue;
                    if (vstPreset.Parameters.ContainsKey("wave-file-name")) reverence.WavFileName = vstPreset.Parameters["wave-file-name"].StringValue;

                    // and copy the images
                    for (int i = 0; i < 10; i++)
                    {
                        string key = string.Format("image-file-name-{0}", (i + 1));
                        if (vstPreset.Parameters.ContainsKey(key))
                        {
                            reverence.Images.Add(vstPreset.Parameters[key].StringValue);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // set parameters
                    reverence.Parameters["mix"].NumberValue = vstPreset.Parameters["mix"].NumberValue;
                    reverence.Parameters["predelay"].NumberValue = vstPreset.Parameters["predelay"].NumberValue;
                    reverence.Parameters["time"].NumberValue = vstPreset.Parameters["time"].NumberValue;
                    reverence.Parameters["size"].NumberValue = vstPreset.Parameters["size"].NumberValue;
                    reverence.Parameters["level"].NumberValue = vstPreset.Parameters["level"].NumberValue;
                    reverence.Parameters["ertailsplit"].NumberValue = vstPreset.Parameters["ertailsplit"].NumberValue;
                    reverence.Parameters["ertailmix"].NumberValue = vstPreset.Parameters["ertailmix"].NumberValue;
                    reverence.Parameters["reverse"].NumberValue = vstPreset.Parameters["reverse"].NumberValue;
                    reverence.Parameters["trim"].NumberValue = vstPreset.Parameters["trim"].NumberValue;
                    reverence.Parameters["autolevel"].NumberValue = vstPreset.Parameters["autolevel"].NumberValue;
                    reverence.Parameters["trimstart"].NumberValue = vstPreset.Parameters["trimstart"].NumberValue;
                    reverence.Parameters["trimend"].NumberValue = vstPreset.Parameters["trimend"].NumberValue;
                    reverence.Parameters["eqon"].NumberValue = vstPreset.Parameters["eqon"].NumberValue;
                    reverence.Parameters["lowfilterfreq"].NumberValue = vstPreset.Parameters["lowfilterfreq"].NumberValue;
                    reverence.Parameters["lowfiltergain"].NumberValue = vstPreset.Parameters["lowfiltergain"].NumberValue;
                    reverence.Parameters["peakfreq"].NumberValue = vstPreset.Parameters["peakfreq"].NumberValue;
                    reverence.Parameters["peakgain"].NumberValue = vstPreset.Parameters["peakgain"].NumberValue;
                    reverence.Parameters["highfilterfreq"].NumberValue = vstPreset.Parameters["highfilterfreq"].NumberValue;
                    reverence.Parameters["highfiltergain"].NumberValue = vstPreset.Parameters["highfiltergain"].NumberValue;
                    reverence.Parameters["lowfilteron"].NumberValue = vstPreset.Parameters["lowfilteron"].NumberValue;
                    reverence.Parameters["peakon"].NumberValue = vstPreset.Parameters["peakon"].NumberValue;
                    reverence.Parameters["highfilteron"].NumberValue = vstPreset.Parameters["highfilteron"].NumberValue;
                    reverence.Parameters["output"].NumberValue = vstPreset.Parameters["output"].NumberValue;
                    reverence.Parameters["predelayoffset"].NumberValue = vstPreset.Parameters["predelayoffset"].NumberValue;
                    reverence.Parameters["timeoffset"].NumberValue = vstPreset.Parameters["timeoffset"].NumberValue;
                    reverence.Parameters["sizeoffset"].NumberValue = vstPreset.Parameters["sizeoffset"].NumberValue;
                    reverence.Parameters["leveloffset"].NumberValue = vstPreset.Parameters["leveloffset"].NumberValue;
                    reverence.Parameters["ertailsplitoffset"].NumberValue = vstPreset.Parameters["ertailsplitoffset"].NumberValue;
                    reverence.Parameters["ertailmixoffset"].NumberValue = vstPreset.Parameters["ertailmixoffset"].NumberValue;
                    reverence.Parameters["store"].NumberValue = vstPreset.Parameters["store"].NumberValue;
                    reverence.Parameters["erase"].NumberValue = vstPreset.Parameters["erase"].NumberValue;
                    reverence.Parameters["autopresetnr"].NumberValue = vstPreset.Parameters["autopresetnr"].NumberValue;
                    reverence.Parameters["channelselect"].NumberValue = vstPreset.Parameters["channelselect"].NumberValue;
                    reverence.Parameters["transProgress"].NumberValue = vstPreset.Parameters["transProgress"].NumberValue;
                    reverence.Parameters["impulseTrigger"].NumberValue = vstPreset.Parameters["impulseTrigger"].NumberValue;
                    reverence.Parameters["bypass"].NumberValue = vstPreset.Parameters["bypass"].NumberValue;
                    reverence.Parameters["allowFading"].NumberValue = vstPreset.Parameters["allowFading"].NumberValue;

                    string outputFilePathNew = Path.Combine(outputDirectoryPath, "REVerence", "Converted_" + outputFileName);
                    CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "REVerence"));

                    reverence.Write(outputFilePathNew + ".vstpreset");

                    using (var tw = new StreamWriter(outputFilePathNew + ".txt"))
                    {
                        foreach (var param in vstPreset.Parameters)
                        {
                            switch (param.Value.Type)
                            {
                                case VstPreset.Parameter.ParameterType.Number:
                                    tw.WriteLine(string.Format("[{1}] {0} = {2:0.00}", param.Value.Name, param.Value.Number, param.Value.NumberValue));
                                    break;
                                case VstPreset.Parameter.ParameterType.String:
                                    tw.WriteLine(string.Format("[{0}] = {1}", param.Value.Name, param.Value.StringValue));
                                    break;
                                case VstPreset.Parameter.ParameterType.Bytes:
                                    var shortenedString = Encoding.ASCII.GetString(param.Value.ByteValue.Take(100).ToArray()).Replace('\0', ' ');
                                    tw.WriteLine(string.Format("[{1}] {0} = {2}", param.Value.Name, param.Value.Number, shortenedString));
                                    break;
                            }
                        }
                    }
                }

                else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ)
                {
                    var parameters = vstPreset.Parameters.Select(a => (float)a.Value.NumberValue).ToArray();
                    var preset = FabfilterProQ.Convert2FabfilterProQ(parameters, false);
                    HandleFabfilterPresetFile(preset, "FabfilterProQ", outputDirectoryPath, outputFileName);
                }

                else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2)
                {
                    var parameters = vstPreset.Parameters.Select(a => (float)a.Value.NumberValue).ToArray();
                    var preset = FabfilterProQ2.Convert2FabfilterProQ(parameters, false);
                    HandleFabfilterPresetFile(preset, "FabFilterProQ2", outputDirectoryPath, outputFileName);
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
                if (vstPreset.HasChunkData())
                {
                    // check if FabFilterProQx64
                    if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQx64)
                    {
                        var fxp = new FXP(vstPreset.GetChunkData());
                        if (fxp.Content is FXP.FxSet)
                        {
                            var set = (FXP.FxSet)fxp.Content;

                            for (int i = 0; i < set.NumPrograms; i++)
                            {
                                var program = set.Programs[i];
                                var parameters = program.Parameters;
                                var preset = FabfilterProQ.Convert2FabfilterProQ(parameters);
                                string outputFileNameNew = string.Format("{0}_{1}", outputFileName, i);
                                HandleFabfilterPresetFile(preset, "FabFilterProQx64", outputDirectoryPath, outputFileNameNew);
                            }
                        }
                    }
                    // check if FabFilterProQ2x64
                    else if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2x64)
                    {
                        var fxp = new FXP(vstPreset.GetChunkData());
                        if (fxp.Content is FXP.FxSet)
                        {
                            var set = (FXP.FxSet)fxp.Content;

                            for (int i = 0; i < set.NumPrograms; i++)
                            {
                                var program = set.Programs[i];
                                var parameters = program.Parameters;
                                var preset = FabfilterProQ2.Convert2FabfilterProQ(parameters);
                                string outputFileNameNew = string.Format("{0}_{1}", outputFileName, i);
                                HandleFabfilterPresetFile(preset, "FabFilterProQ2x64", outputDirectoryPath, outputFileNameNew);
                            }
                        }
                    }

                    // check if NI Kontakt 5
                    else if (vstPreset.Vst3ID == VstPreset.VstIDs.NIKontakt5)
                    {
                        var fxp = new FXP(vstPreset.GetChunkData());

                    }

                    // always output the information
                    else
                    {
                        File.WriteAllText(outputFilePath, vstPreset.ToString());
                    }
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
                // using (var tw = new StreamWriter(outputFilePath))
                // {
                // int counter = 0;
                // foreach (var f in floatArray)
                // {
                //     tw.WriteLine("{0:0.0000}", f);
                //     counter++;
                //     if ((counter - 1) % 7 == 0) tw.WriteLine();
                // }
                // }

                var preset = new FabfilterProQ();
                if (preset.Read(file))
                {
                    HandleFabfilterPresetFile(preset, "FabfilterProQ", outputDirectoryPath, outputFileName);
                }
            }
            else
            {
                floatArray = FabfilterProQ2.ReadFloats(file);
                // using (var tw = new StreamWriter(outputFilePath))
                // {
                // int counter = 0;
                // foreach (var f in floatArray)
                // {
                //     tw.WriteLine("{0:0.0000}", f);
                //     counter++;
                //     if (counter % 7 == 0) tw.WriteLine();
                // }
                // }

                var preset = new FabfilterProQ2();
                if (preset.Read(file))
                {
                    HandleFabfilterPresetFile(preset, "FabfilterProQ2", outputDirectoryPath, outputFileName);
                }
            }
        }

        private static void HandleFabfilterPresetFile(FabfilterProQ preset, string pluginName, string outputDirectoryPath, string outputFileName)
        {
            string fileNameNoExtension = string.Format("{0}_{1}", outputFileName, pluginName);
            string outputFilePath = Path.Combine(outputDirectoryPath, fileNameNoExtension + ".txt");
            File.WriteAllText(outputFilePath, preset.ToString());

            // write the preset file as well
            string outputFilePathFFP = Path.Combine(outputDirectoryPath, fileNameNoExtension + ".ffp");
            preset.Write(outputFilePathFFP);

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
            string outputFilePath = Path.Combine(outputDirectoryPath, fileNameNoExtension + ".txt");
            File.WriteAllText(outputFilePath, preset.ToString());

            // write the preset file as well
            string outputFilePathFFP = Path.Combine(outputDirectoryPath, fileNameNoExtension + ".ffp");
            preset.Write(outputFilePathFFP);

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
                string outputPresetFilePath = Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip", uadSSLChannel.PresetName + ".vstpreset");
                CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip"));
                uadSSLChannel.Write(outputPresetFilePath);

                string outputFXPFilePath = Path.Combine(outputDirectoryPath, "UAD SSL E Channel Strip", uadSSLChannel.PresetName + ".fxp");
                uadSSLChannel.WriteFXP(outputFXPFilePath);

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
