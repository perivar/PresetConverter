using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CommonUtils;
using Serilog;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class NICNT
    {
        public static void Unpack(string inputFilePath, string outputDirectoryPath, bool doList, bool doVerbose)
        {
            using (BinaryFile bf = new BinaryFile(inputFilePath, BinaryFile.ByteOrder.LittleEndian, false))
            {
                // should start with /\ NI FC MTD  /\
                var header = bf.ReadBytes(16);
                if (header.SequenceEqual(NI.NI_FC_MTD)) // 2F 5C 20 4E 49 20 46 43 20 4D 54 44 20 20 2F 5C   /\ NI FC MTD  /\
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

                                // save using image bytes
                                BinaryFile.ByteArrayToFile(iconFilePath, productHints.Product.Icon.ImageBytes);
                            }
                        }
                    }

                    bf.Seek(startOffset + 256, SeekOrigin.Begin);

                    // try to read NI FC MTD again, and extract resources
                    if (NI.TryReadNIResources(inputFilePath, outputDirectoryPath, bf, doList, doVerbose))
                    {
                        Log.Information(inputFilePath + ": Succesfully parsed NI Resources.");
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
            var resourceList = new List<NIResource>();
            string resourcesDirectoryPath = Path.Combine(inputDirectoryPath, "Resources");
            if (Directory.Exists(resourcesDirectoryPath))
            {
                var resourcesFilePaths = Directory.GetFiles(resourcesDirectoryPath, "*.*", SearchOption.AllDirectories);
                int counter = 1;
                foreach (var filePath in resourcesFilePaths)
                {
                    var res = new NIResource();

                    string name = Path.GetFileName(filePath);

                    // remove the counter in front
                    string escapedFileNameWithNumber = Regex.Replace(name, @"^\d{3}(.*?)$", "$1", RegexOptions.IgnoreCase);

                    // convert the windows supported unix filename to the original unix filename 
                    string unescapedFileName = NI.ToUnixFileName(escapedFileNameWithNumber);
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

                var res = new NIResource();
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
                bf.Write(NI.NI_FC_MTD); // 2F 5C 20 4E 49 20 46 43 20 4D 54 44 20 20 2F 5C   /\ NI FC MTD  /\                
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

                bf.Write(NI.NI_FC_MTD); // 2F 5C 20 4E 49 20 46 43 20 4D 54 44 20 20 2F 5C   /\ NI FC MTD  /\                
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

                bf.Write(NI.NI_FC_TOC); // 2F 5C 20 4E 49 20 46 43 20 54 4F 43 20 20 2F 5C  /\ NI FC TOC  /\

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

                Int64 unknown8 = 0;
                bf.Write(unknown8);

                Int64 unknown9 = 0;
                bf.Write(unknown9);

                bf.Write(NI.NI_FC_TOC); // 2F 5C 20 4E 49 20 46 43 20 54 4F 43 20 20 2F 5C  /\ NI FC TOC  /\

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
    }
}