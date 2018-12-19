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

namespace AbletonLiveConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup Logger
            var logConfig = new LoggerConfiguration()
                // .WriteTo.File(logFilePath)
                .WriteTo.Console()
                // .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Fatal).WriteTo.File(errorLogFilePath))
                ;
            logConfig.MinimumLevel.Verbose();
            Log.Logger = logConfig.CreateLogger();

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

                    var extensions = new List<string> { ".als", ".adv", ".vstpreset", ".xps", ".wav", ".sdir", ".cpr", ".ffp" };
                    var files = Directory.GetFiles(inputDirectoryPath, "*.*", SearchOption.AllDirectories)
                    .Where(s => extensions.Contains(Path.GetExtension(s).ToLowerInvariant()));

                    foreach (var file in files)
                    {
                        Console.WriteLine("Processing {0} ...", file);

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
                Console.WriteLine("{0}", e.Message);
            }
        }

        private static void HandleAbletonLiveProject(string file, string outputDirectoryPath)
        {
            var bytes = File.ReadAllBytes(file);
            var decompressed = Decompress(bytes);
            var str = Encoding.UTF8.GetString(decompressed);
            var xelement = XElement.Parse(str);

            string outputFileName = Path.GetFileNameWithoutExtension(file);
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
                // var multibandCompressor = new AbletonMultibandCompressor(xelement);
                // break;
                case "AutoFilter":
                case "Reverb":
                case "Saturator":
                case "Tuner":
                default:
                    Console.WriteLine("{0} not supported!", presetType);
                    break;
            }
        }

        private static void HandleCubaseProjectFile(string file, string outputDirectoryPath)
        {
            string outputFileName = Path.GetFileNameWithoutExtension(file);

            var riffReader = new RIFFFileReader(file, false);

            // get fourth chunk
            var chunk = riffReader.Chunks[3];

            // get chunk byte array
            var chunkBytes = chunk.Read((int)chunk.StartPosition, (int)chunk.ChunkDataSize);

            // search for 'VstCtrlInternalEffect'
            var vstEffectBytePattern = Encoding.ASCII.GetBytes("VstCtrlInternalEffect\0");
            var vstEffectIndices = chunkBytes.FindAll(vstEffectBytePattern);

            var binaryFile = riffReader.BinaryFile;
            foreach (int index in vstEffectIndices)
            {
                // seek to index
                int vstEffectIndex = (int)chunk.StartPosition + index;
                Console.WriteLine("vstEffectIndex: {0}", vstEffectIndex);
                binaryFile.Seek(vstEffectIndex);

                // 'VstCtrlInternalEffect' Field            
                var vstEffectField = binaryFile.ReadString(vstEffectBytePattern.Length, Encoding.ASCII).TrimEnd('\0');

                var pluginFieldLen = binaryFile.ReadInt32();
                var pluginFieldField = binaryFile.ReadString(pluginFieldLen, Encoding.ASCII).TrimEnd('\0');
                var t1 = binaryFile.ReadInt16();
                var t2 = binaryFile.ReadInt16();
                var t3 = binaryFile.ReadInt32();

                // 'Plugin UID' Field
                var pluginUIDFieldLen = binaryFile.ReadInt32();
                var pluginUIDField = binaryFile.ReadString(pluginUIDFieldLen, Encoding.ASCII).TrimEnd('\0');
                var t4 = binaryFile.ReadInt16();
                var t5 = binaryFile.ReadInt16();
                var t6 = binaryFile.ReadInt32();

                // 'GUID' Field
                var guidFieldLen = binaryFile.ReadInt32();
                var guidField = binaryFile.ReadString(guidFieldLen, Encoding.ASCII).TrimEnd('\0');
                var t7 = binaryFile.ReadInt16();

                // GUID
                var guidLen = binaryFile.ReadInt32();
                var guid = binaryFile.ReadString(guidLen, Encoding.UTF8);
                guid = StringUtils.RemoveByteOrderMark(guid);
                Console.WriteLine("GUID: {0}", guid);

                // 'Plugin Name' Field
                var pluginNameFieldLen = binaryFile.ReadInt32();
                var pluginNameField = binaryFile.ReadString(pluginNameFieldLen, Encoding.ASCII).TrimEnd('\0');
                var t8 = binaryFile.ReadInt16();

                // Plugin Name
                var pluginNameLen = binaryFile.ReadInt32();
                var pluginName = binaryFile.ReadString(pluginNameLen, Encoding.UTF8);
                pluginName = pluginName.Replace("\0", "");
                Console.WriteLine("Plugin Name: {0}", pluginName);

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
                    Console.WriteLine("Original Plugin Name: {0}", origPluginName);
                }

                // skip to 'audioComponent'
                var audioComponentPattern = Encoding.ASCII.GetBytes("audioComponent\0");
                int audioComponentIndex = binaryFile.IndexOf(audioComponentPattern, 0);

                // 'audioComponent' Field            
                var audioComponentField = binaryFile.ReadString(audioComponentPattern.Length, Encoding.ASCII).TrimEnd('\0');

                var t10 = binaryFile.ReadInt16();
                var t11 = binaryFile.ReadInt16();
                var presetByteLen = binaryFile.ReadInt32();
                Console.WriteLine("Reading preset bytes: {0}", presetByteLen);
                var presetBytes = binaryFile.ReadBytes(0, presetByteLen, BinaryFile.ByteOrder.LittleEndian);
                var vstPreset = new SteinbergVstPreset();
                vstPreset.Vst3ID = guid;
                vstPreset.MetaXmlStartPos = (ulong)presetBytes.Length;
                vstPreset.ReadData(new BinaryFile(presetBytes, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII), (UInt32)presetBytes.Length, false);
                if (vstPreset.ChunkData != null)
                {
                    var fxp = new FXP(vstPreset.ChunkData);
                    string fileName = string.Format("{0} - {1} - {2} - {3}.{4}", outputFileName, origPluginName == null ? "EMPTY" : origPluginName, pluginName, vstEffectIndex, "fxp");
                    fileName = StringUtils.MakeValidFileName(fileName);
                    string outputFilePath = Path.Combine(outputDirectoryPath, fileName);
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
                                FabfilterProQ.Convert2FabfilterProQ(parameters);
                            }
                        }
                    }
                }

                var nextFieldLen2 = binaryFile.ReadInt32();
                var nextField2 = binaryFile.ReadString(nextFieldLen2, Encoding.ASCII).TrimEnd('\0');
                Console.WriteLine("Found {0} at index {1}", nextField2, binaryFile.Position - nextFieldLen2);
            }
        }

        private static void HandleSteinbergVstPreset(string file, string outputDirectoryPath)
        {
            var vstPreset = new SteinbergVstPreset(file);
            string outputFileName = Path.GetFileNameWithoutExtension(file);
            string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName + ".txt");
            Console.WriteLine(vstPreset);
            if (vstPreset.Parameters.Count > 0)
            {
                if (vstPreset.Vst3ID.Equals(VstPreset.VstIDs.WavesSSLComp))
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
                else if (vstPreset.Vst3ID.Equals(VstPreset.VstIDs.WavesSSLChannel))
                {
                    using (var tw = new StreamWriter(outputFilePath))
                    {
                        List<WavesSSLChannel> channelPresetList = WavesPreset.ParseXml<WavesSSLChannel>(vstPreset.Parameters.FirstOrDefault().Value.StringValue);
                        foreach (var wavesSSLChannel in channelPresetList)
                        {
                            tw.WriteLine(wavesSSLChannel);
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
                else
                {
                    File.WriteAllText(outputFilePath, vstPreset.ToString());
                }
            }
            else
            {
                // no parameters
                if (vstPreset.Parameters.Count == 0 && vstPreset.ChunkData != null)
                {
                    // check if FabFilterProQ2 
                    if (vstPreset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2x64)
                    {
                        var fxp = new FXP(vstPreset.ChunkData);
                        if (fxp.Content is FXP.FxSet)
                        {
                            var set = (FXP.FxSet)fxp.Content;

                            for (int i = 0; i < set.NumPrograms; i++)
                            {
                                var program = set.Programs[i];
                                var parameters = program.Parameters;

                                string outputFilePathNew = Path.Combine(outputDirectoryPath, "FabFilterProQ2x64_" + outputFileName + "_" + i + ".txt");
                                using (var tw = new StreamWriter(outputFilePathNew))
                                {
                                    foreach (var param in parameters)
                                    {
                                        tw.WriteLine(string.Format("{0}", param));
                                    }
                                }

                                // FabfilterProQ.Convert2FabfilterProQ(parameters);
                            }
                        }
                    }
                }
            }
        }

        private static void HandleFabfilterPresetFile(string file, string outputDirectoryPath)
        {
            string outputFileName = Path.GetFileNameWithoutExtension(file);
            string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName + ".txt");
            var fabfilterProQ2 = new FabfilterProQ2();
            fabfilterProQ2.Read(file);

            using (var tw = new StreamWriter(outputFilePath))
            {
                foreach (var band in fabfilterProQ2.ProQBands)
                {
                    tw.WriteLine(band.ToString());
                }
            }

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
                Console.WriteLine("Ignoring {0} ...", file);
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
