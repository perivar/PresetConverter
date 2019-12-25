using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CommonUtils;
using Serilog;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class NICNT
    {
        static readonly byte[] NKS_NICNT_MTD = new byte[] { 0x2F, 0x5C, 0x20, 0x4E, 0x49, 0x20, 0x46, 0x43, 0x20, 0x4D, 0x54, 0x44, 0x20, 0x20, 0x2F, 0x5C }; // /\ NI FC MTD  /\
        static readonly byte[] NKS_NICNT_TOC = new byte[] { 0x2F, 0x5C, 0x20, 0x4E, 0x49, 0x20, 0x46, 0x43, 0x20, 0x54, 0x4F, 0x43, 0x20, 0x20, 0x2F, 0x5C }; // /\ NI FC TOC  /\

        public static void Parse(string file, string outputDirectoryPath, bool doList, bool doVerbose)
        {
            using (BinaryFile bf = new BinaryFile(file, BinaryFile.ByteOrder.LittleEndian, false))
            {
                var header = bf.ReadBytes(16);
                if (header.SequenceEqual(NKS_NICNT_MTD)) // 2F 5C 20 4E 49 20 46 43 20 4D 54 44 20 20 2F 5C   /\ NI FC MTD  /\
                {
                    bf.Seek(66, SeekOrigin.Begin);
                    string version = bf.ReadString(3 * 2, Encoding.Unicode);
                    Log.Information("Version: " + version);

                    string outputFileName = Path.GetFileNameWithoutExtension(file);
                    if (!doList) IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, outputFileName));

                    // Save version in ContentVersion.txt 
                    if (!doList) IOUtils.WriteTextToFile(Path.Combine(outputDirectoryPath, outputFileName, "ContentVersion.txt"), version);

                    bf.Seek(132, SeekOrigin.Begin);
                    int unknown1 = bf.ReadInt32();
                    if (doVerbose) Log.Debug("Unknown1: " + unknown1);

                    bf.Seek(144, SeekOrigin.Begin);

                    int startOffset = bf.ReadInt32();
                    Log.Information("Start Offset: " + startOffset);

                    int unknown3 = bf.ReadInt32();
                    if (doVerbose) Log.Debug("Unknown3: " + unknown3);

                    bf.Seek(256, SeekOrigin.Begin);

                    string productHintsXml = bf.ReadStringNull();
                    if (doVerbose) Log.Debug("ProductHints Xml:\n" + productHintsXml);

                    // Save productHints as xml 
                    if (!doList) IOUtils.WriteTextToFile(Path.Combine(outputDirectoryPath, outputFileName, outputFileName + ".xml"), productHintsXml);

                    // the Data is an icon stored as Base64 String
                    // https://codebeautify.org/base64-to-image-converter

                    bf.Seek(startOffset + 256, SeekOrigin.Begin);
                    var header2 = bf.ReadBytes(16);
                    if (header2.SequenceEqual(NKS_NICNT_MTD)) // 2F 5C 20 4E 49 20 46 43 20 4D 54 44 20 20 2F 5C   /\ NI FC MTD  /\
                    {
                        bf.ReadBytes(116);

                        long unknown4 = bf.ReadInt64();
                        if (doVerbose) Log.Debug("Unknown4: " + unknown4);

                        bf.ReadBytes(4);

                        long unknown5 = bf.ReadInt64();
                        if (doVerbose) Log.Debug("Unknown5: " + unknown5);

                        bf.ReadBytes(104);

                        long unknown6 = bf.ReadInt64();
                        if (doVerbose) Log.Debug("Unknown6: " + unknown6);

                        var delimiter1 = bf.ReadBytes(8);
                        if (doVerbose) Log.Debug("Delimiter1: " + StringUtils.ByteArrayToHexString(delimiter1)); // F0 F0 F0 F0 F0 F0 F0 F0
                        if (!delimiter1.SequenceEqual(new byte[] { 0xF0, 0xF0, 0xF0, 0xF0, 0xF0, 0xF0, 0xF0, 0xF0 }))
                        {
                            Log.Error("Delimiter1 not as expected 'F0 F0 F0 F0 F0 F0 F0 F0' but got " + StringUtils.ToHexAndAsciiString(delimiter1));
                        }

                        long totalResourceCount = bf.ReadInt64();
                        Log.Information("Total Resource Count: " + totalResourceCount);

                        long totalResourceLength = bf.ReadInt64();
                        Log.Information("Total Resource Length: " + totalResourceLength);

                        var resourceList = new List<NICNTResource>();
                        var header3 = bf.ReadBytes(16);
                        if (header3.SequenceEqual(NKS_NICNT_TOC)) // 2F 5C 20 4E 49 20 46 43 20 54 4F 43 20 20 2F 5C  /\ NI FC TOC  /\
                        {
                            bf.ReadBytes(600);

                            long lastIndex = 0;
                            for (int i = 0; i < totalResourceCount; i++)
                            {
                                var resource = new NICNTResource();

                                Log.Information("-------- Index: " + bf.Position + " --------");

                                long resCounter = bf.ReadInt64();
                                Log.Information("Resource Counter: " + resCounter);
                                resource.Count = resCounter;

                                bf.ReadBytes(16);

                                string resName = bf.ReadString(600, Encoding.Unicode).TrimEnd('\0');
                                Log.Information("Resource Name: " + resName);
                                resource.Name = resName;

                                long resUnknown = bf.ReadInt64();
                                if (doVerbose) Log.Debug("Resource Unknown: " + resUnknown);

                                long resIndex = bf.ReadInt64();
                                Log.Information("Resource Index: " + resIndex);
                                resource.Index = resIndex;

                                if (lastIndex > 0)
                                {
                                    resource.Length = resIndex - lastIndex;
                                }
                                else
                                {
                                    resource.Length = resIndex;
                                }
                                Log.Information("Resource Length: " + resource.Length);

                                lastIndex = resIndex;
                                resourceList.Add(resource);
                            }
                            Log.Information("-------- Index: " + bf.Position + " --------");

                            var delimiter2 = bf.ReadBytes(8);
                            if (doVerbose) Log.Debug("Delimiter2: " + StringUtils.ByteArrayToHexString(delimiter2)); // F1 F1 F1 F1 F1 F1 F1 F1

                            if (!delimiter2.SequenceEqual(new byte[] { 0xF1, 0xF1, 0xF1, 0xF1, 0xF1, 0xF1, 0xF1, 0xF1 }))
                            {
                                Log.Error("Delimiter2 not as expected 'F1 F1 F1 F1 F1 F1 F1 F1' but got " + StringUtils.ToHexAndAsciiString(delimiter2));
                            }

                            long unknown13 = bf.ReadInt64();
                            if (doVerbose) Log.Debug("Unknown13: " + unknown13);

                            long unknown14 = bf.ReadInt64();
                            if (doVerbose) Log.Debug("Unknown14: " + unknown14);

                            var header4 = bf.ReadBytes(16);
                            if (header4.SequenceEqual(NKS_NICNT_TOC)) // 2F 5C 20 4E 49 20 46 43 20 54 4F 43 20 20 2F 5C  /\ NI FC TOC  /\
                            {
                                bf.ReadBytes(592);

                                if (!doList) IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, outputFileName, "Resources"));

                                foreach (var res in resourceList)
                                {
                                    string escapedFileName = FromUnixFileNames(res.Name);
                                    Log.Information(String.Format("Resource '{0}' @ position {1} [{2} bytes]", escapedFileName, bf.Position, res.Length));

                                    res.Data = bf.ReadBytes((int)res.Length);

                                    // if not only listing, save files
                                    if (!doList)
                                    {
                                        string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName, "Resources", escapedFileName);
                                        BinaryFile outBinaryFile = new BinaryFile(outputFilePath, BinaryFile.ByteOrder.LittleEndian, true);

                                        outBinaryFile.Write(res.Data);
                                        outBinaryFile.Close();
                                    }
                                }
                            }
                            else
                            {
                                Log.Error("Header4 not as expected '/\\ NI FC TOC  /\\' but got " + StringUtils.ToHexAndAsciiString(header4));
                            }
                        }
                        else
                        {
                            Log.Error("Header3 not as expected '/\\ NI FC TOC  /\\' but got " + StringUtils.ToHexAndAsciiString(header3));
                        }
                    }
                    else
                    {
                        Log.Error("Header2 not as expected '/\\ NI FC MTD  /\\' but got " + StringUtils.ToHexAndAsciiString(header2));
                    }
                }
                else
                {
                    Log.Error("Header not as expected '/\\ NI FC MTD  /\\' but got " + StringUtils.ToHexAndAsciiString(header));
                }
            }
        }


        /// <summary>
        /// Class to store a NICNT resource
        /// </summary>
        class NICNTResource
        {
            public long Count { get; set; }
            public string Name { get; set; }
            public long Length { get; set; }
            public byte[] Data { get; set; }
            public long Index { get; set; }
            public long RealIndex { get; set; }
        }

        /// <summary>
        /// Convert from unix filenames to a filename that can be stored on windows
        /// i.e. convert | to [pipe], etc.
        /// </summary>
        /// <param name="fileName">unix filename</param>
        /// <returns>a windows supported unix filename</returns>
        public static string FromUnixFileNames(string fileName)
        {
            // return StringUtils.EscapeHex(fileName);

            // https://stackoverflow.com/questions/33830658/regex-that-considers-custom-escape-characters-in-the-string-not-in-the-pattern
            // %(?<!%%)\{*([0-9a-zA-Z_\/]+)\}*

            // https://stackoverflow.com/questions/28113962/regular-expression-to-match-unescaped-characters-only
            // (?<!\\)(?:(\\\\)*)[*]

            // https://stackoverflow.com/questions/816915/match-uneven-number-of-escape-symbols
            // (?<!\\)    # not preceded by a backslash
            // (?:\\\\)*  # zero or more escaped backslashes
            // \\ \n      # single backslash and linefeed

            // fixed regexp
            // (?<!\[)(?:\[\[)*\[([0-9a-zA-Z_\/:]+)\]

            // https://stackoverflow.com/questions/22375138/regex-in-c-sharp-expression-in-negative-lookbehind
            // The regex you are looking for is (?<=(^|[^?])(\?\?)*\?)

            // Let's break the lookbehind (I changed it to positive) down:
            // (^|[^?])     not a question mark (possibly also start of string, i.e. nothing)
            // (\?\?)*      any number of question mark pairs
            // \?           a single question mark

            // \ [bslash]
            // ? [qmark]
            // * [star]
            // " [quote]
            // | [pipe]
            // : [colon]
            // < [less]
            // > [greater]
            // _ [space] (only at the end of the name)
            // . [dot] (only at the end of the name)

            // var matchesWithoutEscape = Regex.Matches(fileName, replaceControlWithoutEscape);
            // if (matchesWithoutEscape.Count > 0)
            // {
            //     foreach (Match m in matchesWithoutEscape)
            //     {
            //         Console.WriteLine("'{0}' found at index {1}.", m.Value, m.Index);

            //         // We only one index 1 and over since 0 is actually the entire string
            //         for (var i = 1; i < m.Groups.Count; i++)
            //         {
            //             Console.WriteLine("Group: {0}, Value: {1}", i, m.Groups[i].Value);
            //         }

            //         switch (m.Groups[1].Value)
            //         {
            //             case "\\":
            //                 fileName = m.Replace(fileName, "[bslash]");
            //                 break;
            //             case "?":
            //                 fileName = m.Replace(fileName, "[qmark]");
            //                 break;
            //             case "*":
            //                 fileName = m.Replace(fileName, "[star]");
            //                 break;
            //             case "\"":
            //                 fileName = m.Replace(fileName, "[quote]");
            //                 break;
            //             case "|":
            //                 fileName = m.Replace(fileName, "[pipe]");
            //                 break;
            //             case ":":
            //                 fileName = m.Replace(fileName, "[colon]");
            //                 break;
            //             case "<":
            //                 fileName = m.Replace(fileName, "[less]");
            //                 break;
            //             case ">":
            //                 fileName = m.Replace(fileName, "[greater]");
            //                 break;
            //             default:
            //                 break;
            //         }
            //     }
            // }

            // https://www.wipfli.com/insights/blogs/connect-microsoft-dynamics-365-blog/c-regex-multiple-replacements
            var entityReplacements = new Dictionary<string, string> {
                { "\\", "[bslash]" },
                { "?", "[qmark]" },
                { "*", "[star]" },
                { "\"", "[quote]" },
                { "|", "[pipe]" },
                { ":", "[colon]" },
                { "<", "[less]" },
                { ">", "[greater]" }
             };

            // then escape all control sequences 
            // match even number of [ in front of a control character
            const string replaceControlSequencesEven = @"(?<!\[)(\[\[)*(?!\[)(bslash|qmark|star|quote|pipe|colon|less|greater|space|dot)";
            fileName = Regex.Replace(fileName, replaceControlSequencesEven,
                m => m.Groups[1].Value + m.Value // m.Groups[1].Value + entityReplacements[m.Groups[2].Value]
            );

            // match odd number of [ in front of a control character
            const string replaceControlSequencesOdd = @"(?<!\[)(\[)(?:\[\[)*(?!\[)(bslash|qmark|star|quote|pipe|colon|less|greater|space|dot)";
            fileName = Regex.Replace(fileName, replaceControlSequencesOdd,
                m => m.Groups[1].Value + m.Value //  + entityReplacements[m.Groups[2].Value]
            );

            // then replace all control characters that does start with an character [
            // remember to add another [
            const string replaceControlWithEscape = @"(\[+)([""\/?:<>*|])";
            fileName = Regex.Replace(fileName, replaceControlWithEscape,
                m => m.Groups[1].Value + entityReplacements[m.Groups[2].Value]
            );

            // first replace all control characters that doesn't start with an escape character [
            const string replaceControlWithoutEscape = @"((?!\[).)([\?*""|:<>])";
            fileName = Regex.Replace(fileName, replaceControlWithoutEscape,
                m => m.Groups[1].Value + entityReplacements[m.Groups[2].Value]
            );

            // check if we have unescaped control sequences
            // const string pattern = @"(?<!\[)(?:\[\[)*(\[(bslash|qmark|star|quote|pipe|colon|less|greater|space|dot)\])";

            // Let's break up the lookbehind
            // (?<!\[)                          # not preceded by a [
            // (?:\[\[)*                        # any number of [ pairs
            // \[(bslash|qmark)\]               # a single [ followed by a control sequence ending with a ]

            // fileName = fileName
            // // escape existing control sequences with another [
            // .Replace("[bslash]", "[[bslash]")
            // .Replace("[qmark]", "[[qmark]")
            // .Replace("[star]", "[[star]")
            // .Replace("[quote]", "[[quote]")
            // .Replace("[pipe]", "[[pipe]")
            // .Replace("[colon]", "[[colon]")
            // .Replace("[less]", "[[less]")
            // .Replace("[greater]", "[[greater]")

            // replace illegal characters with control sequences
            // .Replace("\\", "[bslash]")
            //                 .Replace("?", "[qmark]")
            //                 .Replace("*", "[star]")
            //                 .Replace("\"", "[quote]")
            //                 .Replace("|", "[pipe]")
            //                 .Replace(":", "[colon]")
            //                 .Replace("<", "[less]")
            //                 .Replace(">", "[greater]");

            while (fileName.EndsWith(" "))
            {
                fileName = fileName.Replace(" ", "[space]");
            }

            while (fileName.EndsWith("."))
            {
                fileName = fileName.Replace(".", "[dot]");
            }

            return fileName;
        }

        /// <summary>
        /// Convert from windows filename with unix patterns back to unix filename
        /// i.e. convert from [pipe] to |, etc.
        /// </summary>
        /// <param name="fileName">windows supported unix filename</param>
        /// <returns>a unix filename</returns>
        public static string ToUnixFileName(string fileName)
        {
            return StringUtils.UnescapeHex(fileName);

            // \ [bslash]
            // ? [qmark]
            // * [star]
            // " [quote]
            // | [pipe]
            // : [colon]
            // < [less]
            // > [greater]
            // _ [space] (only at the end of the name)
            // . [dot] (only at the end of the name)

            // check if we have unescaped control sequences
            const string pattern = @"(?<!\[)(?:\[\[)*(\[(bslash|qmark|star|quote|pipe|colon|less|greater|space|dot)\])";

            // Let's break up the lookbehind
            // (?<!\[)                          # not preceded by a [
            // (?:\[\[)*                        # any number of [ pairs
            // \[(bslash|qmark)\]               # a single [ followed by a control sequence ending with a ]

            var matches = Regex.Matches(fileName, pattern);
            if (matches.Count > 0)
            {
                foreach (Match m in matches)
                {
                    Console.WriteLine("'{0}' found at index {1}.", m.Value, m.Index);

                    // We only one index 1 and over since 0 is actually the entire string
                    for (var i = 1; i < m.Groups.Count; i++)
                    {
                        Console.WriteLine("Group: {0}, Value: {1}", i, m.Groups[i].Value);
                    }

                    if (m.Groups.Count > 2)
                    {
                        // convert back
                        switch (m.Groups[2].Value)
                        {
                            case "\\":
                                fileName = fileName.Replace("[bslash]", "\\");
                                break;
                            case "?":
                                fileName = fileName.Replace("[qmark]", "?");
                                break;

                            default:
                                break;
                        }
                    }
                }
            }

            fileName = fileName
                .Replace("[bslash]", "\\")
                .Replace("[qmark]", "?")
                .Replace("[star]", "*")
                .Replace("[quote]", "\"")
                .Replace("[pipe]", "|")
                .Replace("[colon]", ":")
                .Replace("[less]", "<")
                .Replace("[greater]", ">");

            while (fileName.EndsWith("[space]"))
            {
                fileName = fileName.Replace("[space]", " ");
            }

            while (fileName.EndsWith("[dot]"))
            {
                fileName = fileName.Replace("[dot]", ".");
            }

            return fileName;
        }
    }
}