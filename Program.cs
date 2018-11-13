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
            var optionDirectory = app.Option("-d|--directory <path>", "The directory", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                if (optionDirectory.HasValue())
                {
                    string inputDirectoryPath = optionDirectory.Value();

                    string[] files = Directory.GetFiles(inputDirectoryPath, "*.adv", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        Console.WriteLine("Processing {0} ...", file);
                        byte[] bytes = File.ReadAllBytes(file);
                        byte[] decompressed = Decompress(bytes);
                        string str = Encoding.UTF8.GetString(decompressed);

                        XElement xelement = XElement.Parse(str);

                        // find preset type
                        var presetType = xelement.Elements().First().Name.ToString();
                        switch (presetType)
                        {
                            case "Eq8":
                                var eq = new Eq(xelement);
                                break;
                            case "Compressor2":
                                var compressor = new Compressor(xelement);
                                break;
                            case "GlueCompressor":
                                var glueCompressor = new GlueCompressor(xelement);
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
