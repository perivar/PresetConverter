using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CommonUtils;
using Serilog;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class NICNT
    {
        static readonly byte[] NKS_NICNT_MTD = new byte[] { 0x2F, 0x5C, 0x20, 0x4E, 0x49, 0x20, 0x46, 0x43, 0x20, 0x4D, 0x54, 0x44, 0x20, 0x20, 0x2F, 0x5C }; // /\ NI FC MTD  /\
        static readonly byte[] NKS_NICNT_TOC = new byte[] { 0x2F, 0x5C, 0x20, 0x4E, 0x49, 0x20, 0x46, 0x43, 0x20, 0x54, 0x4F, 0x43, 0x20, 0x20, 0x2F, 0x5C }; // /\ NI FC TOC  /\

        public static void Unpack(string inputFilePath, string outputDirectoryPath, bool doList, bool doVerbose)
        {
            using (BinaryFile bf = new BinaryFile(inputFilePath, BinaryFile.ByteOrder.LittleEndian, false))
            {
                var header = bf.ReadBytes(16);
                if (header.SequenceEqual(NKS_NICNT_MTD)) // 2F 5C 20 4E 49 20 46 43 20 4D 54 44 20 20 2F 5C   /\ NI FC MTD  /\
                {
                    bf.Seek(66, SeekOrigin.Begin);
                    string version = bf.ReadString(66, Encoding.Unicode).TrimEnd('\0');
                    Log.Information("Version: " + version);

                    string outputFileName = Path.GetFileNameWithoutExtension(inputFilePath);
                    if (!doList) IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, outputFileName));

                    // Save version in ContentVersion.txt 
                    if (!doList) IOUtils.WriteTextToFile(Path.Combine(outputDirectoryPath, outputFileName, "ContentVersion.txt"), version);

                    int unknown1 = bf.ReadInt32();
                    if (doVerbose) Log.Debug("Unknown1: " + unknown1);

                    bf.Seek(144, SeekOrigin.Begin);

                    int startOffset = bf.ReadInt32();
                    Log.Information("Start Offset: " + startOffset);

                    int unknown3 = bf.ReadInt32();
                    if (doVerbose) Log.Debug("Unknown3: " + unknown3);

                    bf.Seek(256, SeekOrigin.Begin);

                    string productHintsXml = bf.ReadStringNull();
                    Log.Information(string.Format("Read ProductHints Xml with length {0} characters.", productHintsXml.Length));
                    if (doVerbose) Log.Debug("ProductHints Xml:\n" + productHintsXml);

                    // Save productHints as xml 
                    if (!doList) IOUtils.WriteTextToFile(Path.Combine(outputDirectoryPath, outputFileName, outputFileName + ".xml"), productHintsXml);

                    // get the product hints as an object
                    var productHints = ProductHintsFactory.ReadFromString(productHintsXml);
                    if (productHints != null && productHints.Product.Icon != null && productHints.Product.Icon.ImageBytes != null)
                    {
                        ProductHintsFactory.UpdateImageFromImageBytes(productHints);

                        var image = productHints.Product.Icon.Image;
                        var imageFormat = productHints.Product.Icon.ImageFormat;
                        if (image != null && imageFormat != null)
                        {
                            Log.Information(string.Format("Found Icon in ProductHints Xml in {0} format. (Dimensions: {1} x {2}, Width: {1} pixels, Height: {2} pixels, Bit depth: {3} bpp)", imageFormat.Name, image.Width, image.Height, image.PixelType.BitsPerPixel));

                            // save icon to file
                            if (!doList)
                            {
                                var iconFileName = outputFileName + " Icon." + imageFormat.Name.ToLower();
                                var iconFilePath = Path.Combine(outputDirectoryPath, outputFileName, iconFileName);

                                if (doVerbose) Log.Debug("Saving Icon to: " + iconFilePath);

                                // save using ImageSharp
                                // var imageEncoder = image.GetConfiguration().ImageFormatsManager.GetEncoder(imageFormat);
                                // image.Save(iconFilePath, imageEncoder);

                                // save using image bytes
                                BinaryFile.ByteArrayToFile(iconFilePath, productHints.Product.Icon.ImageBytes);
                            }
                        }
                    }

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
                        Log.Information("Total Resource Byte Length: " + totalResourceLength);

                        var resourceList = new List<NICNTResource>();
                        var header3 = bf.ReadBytes(16);
                        if (header3.SequenceEqual(NKS_NICNT_TOC)) // 2F 5C 20 4E 49 20 46 43 20 54 4F 43 20 20 2F 5C  /\ NI FC TOC  /\
                        {
                            bf.ReadBytes(600);

                            long lastEndIndex = 0;
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
                            }
                            else
                            {
                                Log.Error(inputFilePath + ": Header4 not as expected '/\\ NI FC TOC  /\\' but got " + StringUtils.ToHexAndAsciiString(header4));
                            }
                        }
                        else
                        {
                            Log.Error(inputFilePath + ": Header3 not as expected '/\\ NI FC TOC  /\\' but got " + StringUtils.ToHexAndAsciiString(header3));
                        }
                    }
                    else
                    {
                        Log.Error(inputFilePath + ": Header2 not as expected '/\\ NI FC MTD  /\\' but got " + StringUtils.ToHexAndAsciiString(header2));
                    }
                }
                else
                {
                    Log.Error(inputFilePath + ": Header not as expected '/\\ NI FC MTD  /\\' but got " + StringUtils.ToHexAndAsciiString(header));
                }
            }
        }

        public static void Pack(string inputDirectoryPath, string outputDirectoryPath, bool doList, bool doVerbose)
        {
            Log.Information("Packing directory {0} ...", inputDirectoryPath);

            string libraryName = Path.GetFileNameWithoutExtension(inputDirectoryPath);
            string outputFileName = libraryName + ".nicnt";
            string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName);
            Log.Information("Packing into file {0} ...", outputFilePath);

            // read the ContentVersion.txt file
            string contentVersionPath = Path.Combine(inputDirectoryPath, "ContentVersion.txt");
            string version = "1.0";
            if (File.Exists(contentVersionPath))
            {
                version = IOUtils.ReadTextFromFile(contentVersionPath);
                if (doVerbose) Log.Debug("Read version: '" + version + "' from ContentVersion.txt");
            }
            else
            {
                Log.Information("No ContentVersion.txt file found - using default version: '" + version + "'");
            }

            // read the [LibraryName].xml file (should have the same name as the input directory)
            string productHintsXmlFileName = libraryName + ".xml";
            string productHintsXmlFilePath = Path.Combine(inputDirectoryPath, productHintsXmlFileName);
            string productHintsXml = "";
            ProductHints productHints = null;
            if (File.Exists(productHintsXmlFilePath))
            {
                productHintsXml = IOUtils.ReadTextFromFile(productHintsXmlFilePath);
                Log.Information(string.Format("Found ProductHints Xml with length {0} characters.", productHintsXml.Length));
                if (doVerbose) Log.Debug("ProductHints Xml:\n" + productHintsXml);

                // get the product hints as an object
                productHints = ProductHintsFactory.ReadFromString(productHintsXml);

                // get the productHints as string
                productHintsXml = ProductHintsFactory.ToString(productHints);

                // test output
                // var xmlBytesTest = Encoding.UTF8.GetBytes(productHintsXml);
                // BinaryFile.ByteArrayToFile(Path.Combine(inputDirectoryPath, libraryName + "-test.xml"), xmlBytesTest);

                // check that the directory name is the same as the Name and RegKey in the xml file
                if (productHints.Product.Name != productHints.Product.RegKey || libraryName != productHints.Product.Name)
                {
                    Log.Error(string.Format("Fixing '{0}' due to incorrect values! (Library-Name: '{1}', Xml-Name: '{2}', Xml-RegKey: '{3}')", productHintsXmlFileName, libraryName, productHints.Product.Name, productHints.Product.RegKey));

                    // change into the library name
                    productHints.Product.Name = libraryName;
                    productHints.Product.RegKey = libraryName;

                    // update the productHints as string
                    productHintsXml = ProductHintsFactory.ToString(productHints);

                    // and overwrite
                    var xmlBytes = Encoding.UTF8.GetBytes(productHintsXml);
                    BinaryFile.ByteArrayToFile(productHintsXmlFilePath, xmlBytes);
                }
            }
            else
            {
                Log.Error("Mandatory ProductHints XML file not found at: " + productHintsXmlFilePath);
                return;
            }

            // read the files in the resources directory
            var resourceList = new List<NICNTResource>();
            string resourcesDirectoryPath = Path.Combine(inputDirectoryPath, "Resources");
            if (Directory.Exists(resourcesDirectoryPath))
            {
                var resourcesFilePaths = Directory.GetFiles(resourcesDirectoryPath, "*.*", SearchOption.AllDirectories);
                int counter = 1;
                foreach (var filePath in resourcesFilePaths)
                {
                    var res = new NICNTResource();

                    string name = Path.GetFileName(filePath);

                    // remove the counter in front
                    string escapedFileNameWithNumber = Regex.Replace(name, @"^\d{3}(.*?)$", "$1", RegexOptions.IgnoreCase);

                    // convert the windows supported unix filename to the original unix filename 
                    string unescapedFileName = ToUnixFileName(escapedFileNameWithNumber);
                    res.Name = unescapedFileName;

                    var bytes = File.ReadAllBytes(filePath);
                    res.Data = bytes;
                    res.Length = bytes.Length;

                    res.Count = counter;

                    resourceList.Add(res);
                    counter++;
                }

                if (resourceList.Count > 0)
                {
                    if (doVerbose) Log.Debug("Added " + resourceList.Count + " files found in the Resources directory.");
                }
                else
                {
                    if (doVerbose) Log.Information("No files in the Resource directory found!");
                }
            }
            else
            {
                if (doVerbose) Log.Information("No Resources directory found!");
            }

            // check for mandatory .db.cache
            if (!resourceList.Any(m => m.Name.ToLower() == ".db.cache"))
            {
                XmlDocument xml = new XmlDocument();
                // Adding the XmlDeclaration (version encoding and standalone) is not necessary as it is added using the XmlWriterSettings
                // XmlNode docNode = xml.CreateXmlDeclaration("1.0", "UTF-8", "no");
                // xml.AppendChild(docNode);
                XmlElement root = xml.CreateElement("soundinfos");
                root.SetAttribute("version", "110");
                xml.AppendChild(root);

                XmlElement all = xml.CreateElement("all");
                root.AppendChild(all);

                // format the xml string
                string xmlString = BeautifyXml(xml);

                var res = new NICNTResource();
                res.Name = ".db.cache";
                var bytes = Encoding.UTF8.GetBytes(xmlString);
                res.Data = bytes;
                res.Length = bytes.Length;
                // res.EndIndex is calculated during the write resource operation later 
                res.Count = resourceList.Count + 1;

                resourceList.Add(res);
                if (doVerbose) Log.Information("No .db.cache found - using default .db.cache:\n" + xml.OuterXml);
            }

            using (BinaryFile bf = new BinaryFile(outputFilePath, BinaryFile.ByteOrder.LittleEndian, true))
            {
                bf.Write(NKS_NICNT_MTD); // 2F 5C 20 4E 49 20 46 43 20 4D 54 44 20 20 2F 5C   /\ NI FC MTD  /\                
                bf.Write(new byte[50]); // 50 zero bytes

                bf.WriteStringPadded(version, 66, Encoding.Unicode); // zero padded string

                Int32 unknown1 = 1;
                bf.Write(unknown1);

                bf.Write(new byte[8]); // 8 zero bytes

                Int32 startOffset = 512000;
                bf.Write(startOffset);

                Int32 unknown3 = startOffset;
                bf.Write(unknown3);

                bf.Write(new byte[104]); // 104 zero bytes

                bf.Write(productHintsXml);

                bf.Write(new byte[startOffset + 256 - bf.Position]); // 512000 + 256 zero bytes - current pos

                bf.Write(NKS_NICNT_MTD); // 2F 5C 20 4E 49 20 46 43 20 4D 54 44 20 20 2F 5C   /\ NI FC MTD  /\                
                bf.Write(new byte[116]); // 116 zero bytes

                Int64 unknown4 = 2;
                bf.Write(unknown4);

                bf.Write(new byte[4]); // 4 zero bytes

                Int64 unknown5 = 1;
                bf.Write(unknown5);

                bf.Write(new byte[104]); // 104 zero bytes

                Int64 unknown6 = 1;
                bf.Write(unknown6);

                // write delimiter
                bf.Write(StringUtils.HexStringToByteArray("F0F0F0F0F0F0F0F0"));

                Int64 totalResourceCount = resourceList.Count;
                bf.Write(totalResourceCount);

                Int64 totalResourceLength = resourceList.Sum(item => item.Length); // sum of bytes in the resourceList
                bf.Write(totalResourceLength);

                bf.Write(NKS_NICNT_TOC); // 2F 5C 20 4E 49 20 46 43 20 54 4F 43 20 20 2F 5C  /\ NI FC TOC  /\

                bf.Write(new byte[600]); // 600 zero bytes

                Int64 resCounter = 1;
                Int64 lastEndIndex = 0;
                foreach (var res in resourceList)
                {
                    bf.Write(resCounter);

                    bf.Write(new byte[16]); // 16 zero bytes

                    bf.WriteStringPadded(res.Name, 600, Encoding.Unicode); // zero padded string    

                    Int64 resUnknown = 0;
                    bf.Write(resUnknown);

                    Int64 resEndIndex = lastEndIndex + res.Length; // aggregated end index
                    bf.Write(resEndIndex);

                    lastEndIndex = resEndIndex;
                    resCounter++;
                }

                // write delimiter
                bf.Write(StringUtils.HexStringToByteArray("F1F1F1F1F1F1F1F1"));

                Int64 unknown13 = 0;
                bf.Write(unknown13);

                Int64 unknown14 = 0;
                bf.Write(unknown14);

                bf.Write(NKS_NICNT_TOC); // 2F 5C 20 4E 49 20 46 43 20 54 4F 43 20 20 2F 5C  /\ NI FC TOC  /\

                bf.Write(new byte[592]); // 592 zero bytes

                foreach (var res in resourceList)
                {
                    Log.Information(String.Format("Resource '{0}' @ position {1} [{2} bytes]", res.Name, bf.Position, res.Length));
                    bf.Write(res.Data);
                }
            }
        }

        /// <summary>
        /// Return the XmlDocument as a formatted Xml Section
        /// </summary>
        /// <param name="doc">XmlDocument</param>
        /// <returns>a formatted Xml Section</returns>
        private static string BeautifyXml(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            StringWriterWithEncoding stringWriter = new StringWriterWithEncoding(sb, Encoding.UTF8);
            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true, // when using false, the xml declaration and encoding is added (<?xml version="1.0" encoding="utf-8"?>)
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(stringWriter, settings))
            {
                // writer.WriteStartDocument(false); // when using OmitXmlDeclaration = false, add the standalone="no" property to the xml declaration

                // write custom xml declaration to duplicate the original NICNT xml format
                writer.WriteRaw("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>\r\n");

                doc.Save(writer);
            }

            // add \r \n at the end (0D 0A)
            sb.Append("\r\n");

            // ugly way to remove whitespace in self closing tags when writing xml document
            sb.Replace(" />", "/>");

            return sb.ToString();
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
            public long EndIndex { get; set; }
            public long RealIndex { get; set; }
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