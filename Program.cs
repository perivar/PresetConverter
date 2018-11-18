using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;

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

                    var ext = new List<string> { ".adv", ".vstpreset" };
                    var files = Directory.GetFiles(inputDirectoryPath, "*.*", SearchOption.AllDirectories)
                    .Where(s => ext.Contains(Path.GetExtension(s)));

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
            byte[] bytes = File.ReadAllBytes(file);
            byte[] decompressed = Decompress(bytes);
            string str = Encoding.UTF8.GetString(decompressed);

            XElement xelement = XElement.Parse(str);

            // find preset type
            var presetType = xelement.Elements().First().Name.ToString();
            switch (presetType)
            {
                case "Eq8":
                    var eq = new AbletonEq8(xelement);
                    var eqAdapter = new AbletonEq8ToSteinbergFrequencyAdapter(eq);
                    var steinbergFrequency = eqAdapter.ToSteinbergFrequencyPreset();
                    string outputFileName = Path.GetFileNameWithoutExtension(file);
                    string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName + ".vstpreset");
                    steinbergFrequency.Write(outputFilePath);
                    break;
                case "Compressor2":
                    var compressor = new AbletonCompressor(xelement);
                    break;
                case "GlueCompressor":
                    var glueCompressor = new AbletonGlueCompressor(xelement);
                    break;
                case "MultibandDynamics":
                // var multibandCompressor = new MultibandCompressor(xelement);
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
            var vstPreset = new VstPreset(file);
            Console.WriteLine(vstPreset);
        }

        static byte[] Decompress(byte[] gzip)
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
