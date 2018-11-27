using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;
using PresetConverter;

namespace AbletonLiveConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.Name = "AbletonLiveConverter";
            app.Description = "Convert Ableton Live presets to readable formats";
            app.HelpOption();
            var optionInputDirectory = app.Option("-i|--input <path>", "The Input directory", CommandOptionType.SingleValue);
            var optionOutputDirectory = app.Option("-o|--output <path>", "The Output directory", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                if (optionInputDirectory.HasValue()
                && optionOutputDirectory.HasValue())
                {
                    string inputDirectoryPath = optionInputDirectory.Value();
                    string outputDirectoryPath = optionOutputDirectory.Value();

                    var extensions = new List<string> { ".adv", ".vstpreset", ".xps" };
                    var files = Directory.GetFiles(inputDirectoryPath, "*.*", SearchOption.AllDirectories)
                    .Where(s => extensions.Contains(Path.GetExtension(s)));

                    foreach (var file in files)
                    {
                        Console.WriteLine("Processing {0} ...", file);

                        string extension = new FileInfo(file).Extension;
                        switch (extension)
                        {
                            case ".adv":
                                HandleAbletonLivePreset(file, outputDirectoryPath);
                                break;
                            case ".vstpreset":
                                HandleSteinbergVstPreset(file, outputDirectoryPath);
                                break;
                            case ".xps":
                                HandleWavesXpsPreset(file, outputDirectoryPath);
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
                    // using (var tw = new StreamWriter(outputFilePath))
                    // {
                        List<WavesSSLChannel> channelPresetList = WavesPreset.ParseXml<WavesSSLChannel>(vstPreset.Parameters.FirstOrDefault().Value.StringValue);
                        foreach (var wavesSSLChannel in channelPresetList)
                        {
                            // tw.WriteLine(wavesSSLChannel);
                            wavesSSLChannel.Write(outputFilePath + ".vstpreset");
                        }
                    // }
                }
                else
                {
                    File.WriteAllText(outputFilePath, vstPreset.ToString());
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
