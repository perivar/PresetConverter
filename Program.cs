using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace AbletonLiveConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputFolder = @"C:\Users\perner\Amazon Drive\Documents\My Projects\Ableton";

            string[] files = Directory.GetFiles(inputFolder, "*.adv", SearchOption.AllDirectories);
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
                    case "AutoFilter":
                    case "GlueCompressor":
                    case "MultibandDynamics":
                    case "Reverb":
                    case "Saturator":
                    case "Tuner":
                        Console.WriteLine("Reading {0}", presetType);
                        break;
                    default:
                        Console.WriteLine("{0} not supported!", presetType);
                        break;
                }
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
