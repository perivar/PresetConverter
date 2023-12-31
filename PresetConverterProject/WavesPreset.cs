﻿using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;

using CommonUtils;

namespace PresetConverter
{
    /// <summary>
    /// a Waves Preset
    /// </summary>
    public abstract class WavesPreset : VstPreset
    {
        public string PresetName = "";
        public string PresetGenericType = "";
        public string? PresetGroup = null;
        public string? PresetPluginName = null;
        public string? PresetPluginSubComp = null;
        public string? PresetPluginVersion = null;
        public string PresetActiveSetup = "CURRENT";
        public string? PresetSetupName = null;
        public string PresetRealWorldParameters = "";

        public bool ReadFXP(string filePath)
        {
            var fxp = new FXP();
            fxp.ReadFile(filePath);

            if (fxp.Content != null)
            {
                if (fxp.Content is FXP.FxProgramSet)
                {
                    byte[] chunkDataByteArray = ((FXP.FxProgramSet)fxp.Content).ChunkData;
                    return ReadChunkData(chunkDataByteArray);
                }
                else if (fxp.Content is FXP.FxChunkSet)
                {
                    byte[] chunkDataByteArray = ((FXP.FxChunkSet)fxp.Content).ChunkData;
                    return ReadChunkData(chunkDataByteArray);
                }
            }
            return false;
        }

        public bool WriteTextSummary(string filePath)
        {
            if (PresetPluginName != null)
            {
                // create a writer and open the file
                TextWriter tw = new StreamWriter(filePath);

                // write the preset string
                tw.Write(ToString());

                // close the stream
                tw.Close();

                return true;
            }
            return false;
        }

        protected override bool PreparedForWriting()
        {
            InitCompChunkData();
            InitInfoXml();
            CalculateBytePositions();
            return true;
        }

        protected abstract void InitCompChunkData();

        /// <summary>
        /// Read Waves XPst files
        /// E.g.
        /// C:\Program Files (x86)\Waves\Plug-Ins\SSLChannel.bundle\Contents\Resources\XPst\1000
        /// or
        /// C:\Users\Public\Waves Audio\Plug-In Settings\*.xps files
        /// </summary>
        /// <param name="filePath">file to xps file (e.g. with the filename '1000' or *.xps)</param>
        /// <typeparam name="T">generics type</typeparam>
        /// <example>
        /// List<WavesSSLChannel> presetList = WavesPreset.ReadXps<WavesSSLChannel>(@"C:\Program Files (x86)\Waves\Plug-Ins\SSLChannel.bundle\Contents\Resources\XPst\1000");
        /// </example>
        /// <remarks>This method is using generics to allow us to specify which preset type we are processing</remarks>
        /// <returns>a list of the WavesPreset type</returns>
        public static List<T>? ReadXps<T>(string filePath) where T : WavesPreset, new()
        {
            string xmlString = File.ReadAllText(filePath);
            return ParseXml<T>(xmlString);
        }

        /// <summary>
        /// Parse Waves XPst content
        /// </summary>
        /// <param name="xmlString">file to xps file (e.g. with the filename '1000' or *.xps)</param>
        /// <typeparam name="T">generics type</typeparam>
        /// <example>
        /// List<WavesSSLChannel> presetList = WavesPreset.ParseXml<WavesSSLChannel>(xmlContent);
        /// </example>
        /// <remarks>This method is using generics to allow us to specify which preset type we are processing</remarks>
        /// <returns>a list of the WavesPreset type</returns>
        public static List<T>? ParseXml<T>(string xmlString) where T : WavesPreset, new()
        {
            var presetList = new List<T>();

            var xml = new XmlDocument();
            try
            {
                if (xmlString != null) xml.LoadXml(xmlString);

                // foreach Preset node that has a Name attribute
                XmlNodeList? presetNodeList = xml.SelectNodes("//Preset[@Name]");
                if (presetNodeList != null)
                {
                    foreach (XmlNode presetNode in presetNodeList)
                    {
                        var preset = new T();
                        if (preset.ParsePresetNode(presetNode))
                        {
                            presetList.Add(preset);
                        }
                    }
                }
            }
            catch (XmlException)
            {
                return null;
            }
            return presetList;
        }

        /// <summary>
        /// Read Waves XPst files
        /// E.g.
        /// C:\Program Files (x86)\Waves\Plug-Ins\SSLChannel.bundle\Contents\Resources\XPst\1000
        /// or
        /// C:\Users\Public\Waves Audio\Plug-In Settings\*.xps files
        /// </summary>
        /// <param name="filePath">file to xps file (e.g. with the filename '1000' or *.xps)</param>
        /// <example>
        /// 	WavesSSLChannel ssl = new WavesSSLChannel();
        /// 	TextWriter tw1 = new StreamWriter("sslchannel-output.txt");
        /// 	ssl.ReadXps(@"C:\Program Files (x86)\Waves\Plug-Ins\SSLChannel.bundle\Contents\Resources\XPst\1000", tw1);
        /// 	ssl.ReadXps(@"C:\Users\Public\Waves Audio\Plug-In Settings\SSLChannel Settings.xps", tw1);
        /// 	tw1.Close();
        /// </example>
        /// <returns>true if successful</returns>
        public bool ReadXps(string filePath, TextWriter tw)
        {
            string xmlString = File.ReadAllText(filePath);
            return ParseXml(xmlString, tw);
        }

        /// <summary>
        /// Parse out the xml string from the passed chunk data byte array
        /// </summary>
        /// <param name="chunkDataByteArray"></param>
        /// <returns>xml string</returns>
        private static string ParseChunkData(byte[] chunkDataByteArray)
        {
            var bf = new BinaryFile(chunkDataByteArray, BinaryFile.ByteOrder.BigEndian, Encoding.ASCII);

            int val1 = bf.ReadInt32();
            int val2 = bf.ReadInt32();
            int val3 = bf.ReadInt32();
            string val4 = bf.ReadString(4);
            string val5 = bf.ReadString(4);

            int chunkSize = bf.ReadInt32();

            string val7 = bf.ReadString(4);

            var xmlChunkBytes = new byte[chunkSize];
            xmlChunkBytes = bf.ReadBytes(0, chunkSize, BinaryFile.ByteOrder.LittleEndian);
            string xmlString = BinaryFile.ByteArrayToString(xmlChunkBytes);

            int val8 = bf.ReadInt32(BinaryFile.ByteOrder.LittleEndian);

            return xmlString;
        }

        public bool ReadChunkData(byte[] chunkDataByteArray)
        {
            string xmlString = ParseChunkData(chunkDataByteArray);
            return ParseXml(xmlString, null);
        }

        public static string? GetPluginName(byte[] chunkDataByteArray)
        {
            string xmlString = ParseChunkData(chunkDataByteArray);
            return GetPluginName(xmlString);
        }

        private static string? GetPluginName(string xmlString)
        {
            var xml = new XmlDocument();
            try
            {
                if (xmlString != null) xml.LoadXml(xmlString);

                // Get preset node that has a Name attribute
                // e.g. <Preset Name=""><PresetHeader><PluginName>SSLChannel</PluginName></PresetHeader></Preset>
                XmlNode? firstPresetNode = xml.SelectSingleNode("//Preset[@Name]");

                if (firstPresetNode != null)
                {

                    // Read some values from the PresetHeader section
                    XmlNode? pluginNameNode = firstPresetNode.SelectSingleNode("PresetHeader/PluginName");
                    if (pluginNameNode != null && pluginNameNode.InnerText != null)
                    {
                        return pluginNameNode.InnerText;
                    }
                }
                return null;
            }
            catch (XmlException)
            {
                return null;
            }
        }

        private bool ParseXml(string xmlString, TextWriter? tw)
        {
            var xml = new XmlDocument();
            try
            {
                if (xmlString != null) xml.LoadXml(xmlString);

                // foreach Preset node that has a Name attribute
                XmlNodeList? presetNodeList = xml.SelectNodes("//Preset[@Name]");
                if (presetNodeList != null)
                {
                    foreach (XmlNode presetNode in presetNodeList)
                    {
                        if (ParsePresetNode(presetNode))
                        {
                            if (tw != null)
                            {
                                tw.WriteLine(ToString());
                                tw.WriteLine();
                                tw.WriteLine("-------------------------------------------------------");
                            }
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (XmlException)
            {
                return false;
            }
            return false;
        }

        /// <summary>
        /// Parse a Waves Preset Node and extract parameters
        /// </summary>
        /// <param name="presetNode">XmlNode</param>
        /// <returns>true if parsing was successful</returns>
        private bool ParsePresetNode(XmlNode presetNode)
        {
            // Get the preset node's attributes
            XmlAttribute? nameAtt = presetNode.Attributes?["Name"];
            if (nameAtt != null && nameAtt.Value != null)
            {
                PresetName = nameAtt.Value;
            }
            else
            {
                PresetName = "";
            }

            XmlAttribute? genericTypeAtt = presetNode.Attributes?["GenericType"];
            if (genericTypeAtt != null && genericTypeAtt.Value != null)
            {
                PresetGenericType = genericTypeAtt.Value;
            }
            else
            {
                PresetGenericType = "";
            }

            // Read some values from the PresetHeader section
            XmlNode? pluginNameNode = presetNode.SelectSingleNode("PresetHeader/PluginName");
            if (pluginNameNode != null && pluginNameNode.InnerText != null)
            {
                PresetPluginName = pluginNameNode.InnerText;
            }
            else
            {
                PresetPluginName = "";
            }

            XmlNode? pluginSubCompNode = presetNode.SelectSingleNode("PresetHeader/PluginSubComp");
            if (pluginSubCompNode != null && pluginSubCompNode.InnerText != null)
            {
                PresetPluginSubComp = pluginSubCompNode.InnerText;
            }
            else
            {
                PresetPluginSubComp = "";
            }

            XmlNode? pluginVersionNode = presetNode.SelectSingleNode("PresetHeader/PluginVersion");
            if (pluginVersionNode != null && pluginVersionNode.InnerText != null)
            {
                PresetPluginVersion = pluginVersionNode.InnerText;
            }
            else
            {
                PresetPluginVersion = "";
            }

            XmlNode? pluginGroupNode = presetNode.SelectSingleNode("PresetHeader/Group");
            if (pluginGroupNode != null && pluginGroupNode.InnerText != null)
            {
                PresetGroup = pluginGroupNode.InnerText;
            }
            else
            {
                PresetGroup = null;
            }

            XmlNode? activeSetupNode = presetNode.SelectSingleNode("PresetHeader/ActiveSetup");
            if (activeSetupNode != null && activeSetupNode.InnerText != null)
            {
                PresetActiveSetup = activeSetupNode.InnerText;
            }
            else
            {
                PresetActiveSetup = "CURRENT";
            }

            // Read the preset data node
            XmlNode? presetDataNode = presetNode.SelectSingleNode("PresetData[@Setup='" + PresetActiveSetup + "']");
            if (presetDataNode != null)
            {
                // Get the preset data node's attributes
                XmlAttribute? setupNameAtt = presetDataNode.Attributes?["SetupName"];
                if (setupNameAtt != null && setupNameAtt.Value != null)
                {
                    PresetSetupName = setupNameAtt.Value;
                }

                // And get the real world data
                XmlNode? parametersNode = presetDataNode.SelectSingleNode("Parameters[@Type='RealWorld']");
                if (parametersNode != null && parametersNode.InnerText != null)
                {
                    PresetRealWorldParameters = StringUtils.TrimMultiLine(parametersNode.InnerText);
                }

                return ReadRealWorldParameters();
            }

            return true;
        }

        public string BeautifyXml(XElement doc)
        {
            // Write Steinberg XML format (without XmlDeclaration)
            StringBuilder sb = new StringBuilder();
            StringWriterWithEncoding stringWriter = new StringWriterWithEncoding(sb, Encoding.UTF8);
            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace
            };

            using (XmlWriter writer = XmlWriter.Create(stringWriter, settings))
            {
                doc.Save(writer);
            }

            // add \n at the end (0A)
            sb.Append("\n");

            // ugly way to remove whitespace in self closing tags when writing xml document
            sb.Replace(" />", "/>");

            return sb.ToString();
        }

        /// <summary>
        /// Format the float value as a real world parameter
        /// </summary>
        /// <param name="value">float value</param>
        /// <returns>formatted value</returns>
        protected string FormatRealWorldParameter(float value)
        {
            if (Math.Abs(value) >= 0.01 && Math.Abs(value) <= 1000.0 || value == 0)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:0.##}", value);
            }
            else
            {
                return (string.Format(CultureInfo.InvariantCulture, "{0:0.#####e-000}", value));
            }
        }

        protected abstract bool ReadRealWorldParameters();

    }
}
