using System.Text;
using System.Text.RegularExpressions;
using CommonUtils;
using Serilog;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class NI
    {
        public static readonly byte[] NI_FC_MTD = new byte[] { 0x2F, 0x5C, 0x20, 0x4E, 0x49, 0x20, 0x46, 0x43, 0x20, 0x4D, 0x54, 0x44, 0x20, 0x20, 0x2F, 0x5C }; // /\ NI FC MTD  /\
        public static readonly byte[] NI_FC_TOC = new byte[] { 0x2F, 0x5C, 0x20, 0x4E, 0x49, 0x20, 0x46, 0x43, 0x20, 0x54, 0x4F, 0x43, 0x20, 0x20, 0x2F, 0x5C }; // /\ NI FC TOC  /\

        public static bool TryReadNIResources(string inputFilePath, string outputDirectoryPath, BinaryFile bf, bool doList, bool doVerbose)
        {
            var isSuccess = false;

            string outputFileName = Path.GetFileNameWithoutExtension(inputFilePath);

            // should start with /\ NI FC MTD  /\
            var header2 = bf.ReadBytes(16);
            if (header2.SequenceEqual(NI_FC_MTD)) // 2F 5C 20 4E 49 20 46 43 20 4D 54 44 20 20 2F 5C   /\ NI FC MTD  /\
            {
                var skipBytes1 = 116;
                if (doVerbose) Log.Debug("Skipping bytes: " + skipBytes1);
                bf.ReadBytes(skipBytes1);

                long unknown4 = bf.ReadInt64();
                if (doVerbose) Log.Debug("Unknown4: " + unknown4);

                var skipBytes2 = 4;
                if (doVerbose) Log.Debug("Skipping bytes: " + skipBytes2);
                bf.ReadBytes(skipBytes2);

                long unknown5 = bf.ReadInt64();
                if (doVerbose) Log.Debug("Unknown5: " + unknown5);

                var skipBytes3 = 104;
                if (doVerbose) Log.Debug("Skipping bytes: " + skipBytes3);
                bf.ReadBytes(skipBytes3);

                long unknown6 = bf.ReadInt64();
                if (doVerbose) Log.Debug("Unknown6: " + unknown6);

                // read delimiter
                var delimiter1 = bf.ReadBytes(8);
                if (doVerbose) Log.Debug("Delimiter1: " + StringUtils.ByteArrayToHexString(delimiter1)); // F0 F0 F0 F0 F0 F0 F0 F0
                if (!delimiter1.SequenceEqual(new byte[] { 0xF0, 0xF0, 0xF0, 0xF0, 0xF0, 0xF0, 0xF0, 0xF0 }))
                {
                    Log.Error("Delimiter1 not as expected 'F0 F0 F0 F0 F0 F0 F0 F0' but got " + StringUtils.ToHexAndAsciiString(delimiter1));
                }

                long totalResourceCount = bf.ReadInt64();
                Log.Information("Total Resource Count: " + totalResourceCount);

                long totalResourceLength = bf.ReadInt64();
                Log.Information("Total Resource Byte Length: " + totalResourceLength);

                // store found resources
                var resourceList = new List<NIResource>();

                // should start with /\ NI FC TOC  /\
                var header3 = bf.ReadBytes(16);
                if (header3.SequenceEqual(NI_FC_TOC)) // 2F 5C 20 4E 49 20 46 43 20 54 4F 43 20 20 2F 5C  /\ NI FC TOC  /\
                {
                    var skipBytes4 = 592;
                    if (doVerbose) Log.Debug("Skipping bytes: " + skipBytes4);
                    bf.ReadBytes(skipBytes4);

                    long unknown7 = bf.ReadInt64();
                    if (doVerbose) Log.Debug("Unknown7: " + unknown7);

                    long lastEndIndex = 0;
                    for (int i = 0; i < totalResourceCount; i++)
                    {
                        var resource = new NIResource();

                        Log.Information("-------- Index: " + bf.Position + " --------");

                        long resCounter = bf.ReadInt64();
                        Log.Information("Resource Counter: " + resCounter);
                        resource.Count = resCounter;

                        var skipBytes5 = 16;
                        if (doVerbose) Log.Debug("Skipping bytes: " + skipBytes5);
                        bf.ReadBytes(skipBytes5);

                        string resName = bf.ReadString(600, Encoding.Unicode).TrimEnd('\0');
                        Log.Information("Resource Name: " + resName);
                        resource.Name = resName;

                        long resUnknown = bf.ReadInt64();
                        if (doVerbose) Log.Debug("Resource Unknown: " + resUnknown);

                        long resEndIndex = bf.ReadInt64();
                        Log.Information("Resource End Index: " + resEndIndex);
                        resource.EndIndex = resEndIndex;

                        // store calculated length
                        if (lastEndIndex > 0)
                        {
                            resource.Length = resEndIndex - lastEndIndex;
                        }
                        else
                        {
                            // for the very first entry the end index is the same as the byte length
                            resource.Length = resEndIndex;
                        }
                        Log.Information("Calculated Resource Byte Length: " + resource.Length);

                        lastEndIndex = resEndIndex;
                        resourceList.Add(resource);
                    }
                    Log.Information("-------- Index: " + bf.Position + " --------");

                    // read delimiter
                    var delimiter2 = bf.ReadBytes(8);
                    if (doVerbose) Log.Debug("Delimiter2: " + StringUtils.ByteArrayToHexString(delimiter2)); // F1 F1 F1 F1 F1 F1 F1 F1

                    if (!delimiter2.SequenceEqual(new byte[] { 0xF1, 0xF1, 0xF1, 0xF1, 0xF1, 0xF1, 0xF1, 0xF1 }))
                    {
                        Log.Error("Delimiter2 not as expected 'F1 F1 F1 F1 F1 F1 F1 F1' but got " + StringUtils.ToHexAndAsciiString(delimiter2));
                    }

                    long unknown8 = bf.ReadInt64();
                    if (doVerbose) Log.Debug("Unknown8: " + unknown8);

                    long unknown9 = bf.ReadInt64();
                    if (doVerbose) Log.Debug("Unknown9: " + unknown9);

                    var header4 = bf.ReadBytes(16);
                    if (header4.SequenceEqual(NI_FC_TOC)) // 2F 5C 20 4E 49 20 46 43 20 54 4F 43 20 20 2F 5C  /\ NI FC TOC  /\
                    {
                        var skipBytes6 = 592;
                        if (doVerbose) Log.Debug("Skipping bytes: " + skipBytes6);
                        bf.ReadBytes(skipBytes6);

                        if (!doList) IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, outputFileName, "Resources"));

                        foreach (var res in resourceList)
                        {
                            // convert the unix filename to a windows supported filename
                            string escapedFileName = FromUnixFileNames(res.Name);

                            // and add the counter in front
                            string escapedFileNameWithNumber = string.Format("{0:D3}{1}", res.Count, escapedFileName);

                            Log.Information(String.Format("Resource '{0}' @ position {1} [{2} bytes]", escapedFileNameWithNumber, bf.Position, res.Length));

                            res.Data = bf.ReadBytes((int)res.Length);

                            // if not only listing, save files
                            if (!doList)
                            {
                                string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName, "Resources", escapedFileNameWithNumber);
                                BinaryFile outBinaryFile = new BinaryFile(outputFilePath, BinaryFile.ByteOrder.LittleEndian, true);

                                outBinaryFile.Write(res.Data);
                                outBinaryFile.Close();
                            }
                        }

                        isSuccess = true;
                    }
                    else
                    {
                        Log.Warning(inputFilePath + ": Header4 not as expected '/\\ NI FC TOC  /\\' but got " + StringUtils.ToHexAndAsciiString(header4));
                    }
                }
                else
                {
                    Log.Warning(inputFilePath + ": Header3 not as expected '/\\ NI FC TOC  /\\' but got " + StringUtils.ToHexAndAsciiString(header3));
                }
            }
            else
            {
                // this is typically ok, so just debug
                Log.Debug("Header2 is not a '/\\ NI FC MTD  /\\' header. Got " + StringUtils.ToHexAndAsciiString(header2));
            }

            return isSuccess;
        }

        // replacement map
        static Dictionary<string, string> entityReplacements = new Dictionary<string, string> {
                { "\\", "[bslash]" },
                { "?", "[qmark]" },
                { "*", "[star]" },
                { "\"", "[quote]" },
                { "|", "[pipe]" },
                { ":", "[colon]" },
                { "<", "[less]" },
                { ">", "[greater]" }
             };

        /// <summary>
        /// Convert from unix filenames to a filename that can be stored on windows
        /// i.e. convert | to [pipe], etc.
        /// </summary>
        /// <param name="fileName">unix filename</param>
        /// <returns>a windows supported unix filename</returns>
        public static string FromUnixFileNames(string fileName)
        {
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

            // Regexp background information - test using https://regex101.com/
            // ----------------------------------------------------------------------------------------------------------------
            // https://stackoverflow.com/questions/8113104/what-is-regex-for-odd-length-series-of-a-known-character-in-a-string
            // (?<!A)(?:AA)*A(?!A)        
            //   (?<!A)     # asserts that it should not be preceded by an 'A' 
            //   (?:AA)*A   # matches an odd number of 'A's 
            //   (?!A)      # asserts it should not be followed by an 'A'.

            // https://stackoverflow.com/questions/28113962/regular-expression-to-match-unescaped-characters-only
            // (?<!\\)(?:(\\\\)*)[*]

            // https://stackoverflow.com/questions/816915/match-uneven-number-of-escape-symbols
            // (?<!\\)(?:\\\\)*\\ \n
            //   (?<!\\)    # not preceded by a backslash
            //   (?:\\\\)*  # zero or more escaped backslashes
            //   \\ \n      # single backslash and linefeed

            // https://stackoverflow.com/questions/22375138/regex-in-c-sharp-expression-in-negative-lookbehind
            // (?<=(^|[^?])(\?\?)*\?)
            //    (^|[^?])   # not a question mark (possibly also start of string, i.e. nothing)
            //    (\?\?)*    # any number of question mark pairs
            //    \?         # a single question mark

            // https://www.wipfli.com/insights/blogs/connect-microsoft-dynamics-365-blog/c-regex-multiple-replacements
            // using Regex.Replace MatchEvaluator delegate to perform multiple replacements

            // escape all control sequences 
            // match even number of [ in front of a control character
            const string replaceControlSequencesEven = @"(?<!\[)(\[\[)+(?!\[)(?:bslash|qmark|star|quote|pipe|colon|less|greater|space|dot)";
            // (?<!\[)               # asserts that it should not be preceded by a '['
            // (\[\[)+               # matches an even number of '['s (at least one pair)
            // (?!\[)                # asserts it should not be followed by an '['
            // (?:bslash|qmark|...)  # non-caputuring group that only matches a control sequence  
            fileName = Regex.Replace(fileName, replaceControlSequencesEven,
                // add the first group to effectively double the found '['s, which will escape them  
                m => m.Groups[1].Value + m.Value
            );

            // escape all control sequences 
            // match odd number of [ in front of a control character
            const string replaceControlSequencesOdd = @"(?<!\[)((?:\[\[)*\[)(?!\[)(?:bslash|qmark|star|quote|pipe|colon|less|greater|space|dot)";
            // (?<!\[)               # asserts that it should not be preceded by a '['
            // ((?:\[\[)*\[)         # matches a odd number of '['s (at least one)
            // (?!\[)                # asserts it should not be followed by an '['
            // (?:bslash|qmark|...)  # non-caputuring group that only matches a control sequence  
            fileName = Regex.Replace(fileName, replaceControlSequencesOdd,
                // escape every odd number of '[' with another '[', which makes them even - meaning this regexp must come after the even check!
                m => "[" + m.Value
            );

            // replace all control characters that does start with a character [
            // Note! remember to add another [
            const string replaceControlWithEscape = @"(\[+)([\?*""|:<>])";
            // (\[+)                 # match at least one '['
            // ([\?*""|:<>])         # match the control character
            fileName = Regex.Replace(fileName, replaceControlWithEscape,
                // double the first group match to effectively double the found '['s, which will escape them  
                m => m.Groups[1].Value + m.Groups[1].Value + entityReplacements[m.Groups[2].Value]
            );

            // replace all control characters that doesn't start with an escape character [
            const string replaceControlWithoutEscape = @"(?<!\[)[\?*""|:<>]";
            // (?<!\[)               # asserts that it should not be preceded by a '['
            // [\?*""|:<>]           # match the control character
            fileName = Regex.Replace(fileName, replaceControlWithoutEscape,
                m => entityReplacements[m.Value]
            );

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

            // replace all control sequences 
            // match odd number of [ in front of a control character
            const string replaceControlSequencesOdd = @"(?<!\[)((?:\[\[)*\[)(?!\[)(bslash|qmark|star|quote|pipe|colon|less|greater|space|dot)\]";
            // (?<!\[)               # asserts that it should not be preceded by a '['
            // ((?:\[\[)*\[)         # matches a odd number of '['s (at least one)
            // (?!\[)                # asserts it should not be followed by an '['
            // (bslash|qmark|...)    # matches any control sequence  
            // \]                    # asserts that it needs to end with a ']'
            fileName = Regex.Replace(fileName, replaceControlSequencesOdd,
                m =>
                {
                    var val = "[" + m.Groups[2].Value + "]";
                    var entity = entityReplacements.FirstOrDefault(x => x.Value == val);
                    // if the number of brackets are 3 - reduce them by two
                    var prefix = (m.Groups[1].Value.Length >= 3 ? new String('[', (m.Groups[1].Value.Length - 2)) : "");
                    return prefix + entity.Key;
                }
            );

            // replace all control sequences 
            // match even number of [ in front of a control character
            // each pair of these brackets ([[) will be replaced with a single ([).
            const string replaceControlSequencesEven = @"(?<!\[)((?:\[\[)+)(?!\[)(bslash|qmark|star|quote|pipe|colon|less|greater|space|dot)\]";
            // (?<!\[)               # asserts that it should not be preceded by a '['
            // ((?:\[\[)+)           # matches an even number of '['s (at least one pair)
            // (?!\[)                # asserts it should not be followed by an '['
            // (bslash|qmark|...)    # matches any control sequence  
            // \]                    # asserts that it needs to end with a ']'
            fileName = Regex.Replace(fileName, replaceControlSequencesEven,
                m =>
                {
                    var prefix = (m.Groups[1].Value.Length >= 4 ? new String('[', (m.Groups[1].Value.Length / 2)) : "[");
                    return prefix + m.Groups[2].Value + "]";
                }
            );

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