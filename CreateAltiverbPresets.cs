using System;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CommonUtils;

/*
	Generate Altiverb Presets
	Version: 3
    http://192.168.10.159/backup/
	
	This was originally generated using the file: generate-cs-from-xml.cs and then modified
	
	Version log:
	Version 1: Initial version
	Version 2: Support quad and stereo preset generation
	Version 3: Disable the direct signal and renamed from create-altiverb-presets.cs to CreateAltiverbPresets.cs
	Version 4: Disabled EQ
*/
class Script2
{
    static public void CommandLine(string[] args)
    {

        if (args.Length == 3 || args.Length == 4)
        {
            //string xmlAltiverbFilePath = @"C:\Users\perivar.nerseth\Documents\CSScripts\altiverb-full.xml";
            string xmlAltiverbFilePath = args[0];
            //string baseSearchDirectory = @"C:\Users\perivar.nerseth\Documents\CSScripts\Altiverb 6.3.5 Library";
            string baseSearchDirectory = args[1];
            string outputDirectory = args[2];

            string presetType = "";
            if (args.Length == 4)
            {
                presetType = args[3];
            }
            else
            {
                presetType = "quad";
            }

            string searchFileType = "";
            if (presetType == "quad")
            {
                searchFileType = "2 to 2";
            }
            else if (presetType == "stereo")
            {
                searchFileType = "1 to 2";
            }
            else
            {
                searchFileType = "2 to 2";
            }

            // generate FXP presets for all the true stereo IR's first
            string[] files = SearchFile.SearchForText(baseSearchDirectory, "*.iri", searchFileType);
            foreach (string filePath in files)
            {
                // change path name into a string like 'Scoring Stages (Orchestral Studios)/Todd-AO - California US/st to st narrow mics at 03m50'
                // first get the directory path, i.e. everything except the filename
                string relativeIRPath = Path.GetDirectoryName(filePath);
                // then remove the base-search-directory (with the extra slash)
                relativeIRPath = relativeIRPath.Replace(String.Format(@"{0}\", baseSearchDirectory), "");
                // and then replace the slashes with forward slashes (as used in the altiverb fxp files
                relativeIRPath = relativeIRPath.Replace(@"\", "/");
                Console.Out.WriteLine("Processing {0} ...", relativeIRPath);
                //string outputFileName = Path.GetDirectoryName(filePath) + "/" + Path.GetFileNameWithoutExtension(filePath) + ".fxp";
                string outputFileName = outputDirectory + "/" + relativeIRPath.Replace("/", "_") + ".fxp";

                Altiverb altiverb = new Altiverb();
                altiverb.PresetNameID = "aa50";
                altiverb.PresetNameVersion = 2;
                altiverb.PresetNameNeedssave = 1;
                altiverb.PresetNameInpresetfolder = 0;
                altiverb.PresetNamePresetpath = "";
                altiverb.AllChunksID = "aa60";
                altiverb.AllChunksVersion = 2;
                altiverb.SnapshotID = "aa65";
                altiverb.SnapshotValueLong = 0;
                altiverb.AutomationPresetIndexID = "aa70";
                altiverb.AutomationPresetIndexValueLong = 0;
                altiverb.EarlyReflectionsOnID = "EROn";
                altiverb.EarlyReflectionsOnValueLong = 1;
                altiverb.DirectGainID = "DrLv";
                altiverb.DirectGainValueFloat = 0F;
                altiverb.DirectColorID = "DrCl";
                altiverb.DirectColorValueFloat = 1F;
                altiverb.EarlyRefGainID = "ERLv";
                altiverb.EarlyRefGainValueFloat = 0F;
                altiverb.EarlyRefDelayID = "ERDl";
                altiverb.EarlyRefDelayValueFloat = 0F;
                altiverb.TailGainID = "TlLv";
                altiverb.TailGainValueFloat = 0F;
                altiverb.TailDelayID = "TlDl";
                altiverb.TailDelayValueFloat = 0F;
                altiverb.ReverbTimeID = "DecM";
                altiverb.ReverbTimeValueFloat = 1F;
                altiverb.LowDampID = "Dec1";
                altiverb.LowDampValueFloat = 1F;
                altiverb.MidDampID = "Dec2";
                altiverb.MidDampValueFloat = 1F;
                altiverb.HighDampID = "Dec3";
                altiverb.HighDampValueFloat = 1F;
                altiverb.EQBassLevelID = "EQBl";
                altiverb.EQBassLevelValueFloat = 0F;
                altiverb.EQTrebleLevelID = "EQTb";
                altiverb.EQTrebleLevelValueFloat = 0F;
                altiverb.EQLoFreqID = "EQ1f";
                altiverb.EQLoFreqValueFloat = 120F;
                altiverb.EQLoQID = "EQ1q";
                altiverb.EQLoQValueFloat = 1.25F;
                altiverb.EQLoGainID = "EQ1g";
                altiverb.EQLoGainValueFloat = 0F;
                altiverb.EQHiFreqID = "EQ2f";
                altiverb.EQHiFreqValueFloat = 2000F;
                altiverb.EQHiQID = "EQ2q";
                altiverb.EQHiQValueFloat = 1.25F;
                altiverb.EQHiGainID = "EQ2g";
                altiverb.EQHiGainValueFloat = 0F;
                altiverb.MasterInLevelID = "MsIn";
                altiverb.MasterInLevelValueFloat = 0F;
                altiverb.MasterOutLevelID = "MsOt";
                altiverb.MasterOutLevelValueFloat = 0F;
                altiverb.MasterFrontLevelID = "FrLv";
                altiverb.MasterFrontLevelValueFloat = 0F;
                altiverb.MasterRearLevelID = "ReLv";
                altiverb.MasterRearLevelValueFloat = 0F;
                altiverb.DryWetMixID = "DrWt";
                altiverb.DryWetMixValueFloat = 1F;
                altiverb.CenterBleedID = "CtBl";
                altiverb.CenterBleedValueFloat = -144F;
                altiverb.LfeBleedID = "LfBl";
                altiverb.LfeBleedValueFloat = -144F;
                altiverb.SampleLevelID = "SmVl";
                altiverb.SampleLevelValueFloat = -10F;
                altiverb.SpeakerLeftXID = "SpLx";
                altiverb.SpeakerLeftXValueFloat = -1F;
                altiverb.SpeakerRightXID = "SpRx";
                altiverb.SpeakerRightXValueFloat = 1F;
                altiverb.SpeakerCenterXID = "SpCx";
                altiverb.SpeakerCenterXValueFloat = 0F;
                altiverb.SpeakerYID = "Spky";
                altiverb.SpeakerYValueFloat = 1F;
                altiverb.EQOnID = "EqOn";
                altiverb.EQOnValueLong = 0;
                altiverb.SizeID = "PtSh";
                altiverb.SizeValueFloat = 100F;
                altiverb.TailCutID = "Endd";
                altiverb.TailCutValueFloat = -120F;
                altiverb.LatencyModeID = "latn";
                altiverb.LatencyModeValueLong = 1;
                altiverb.StagePositionsOnID = "SPOn";
                altiverb.StagePositionsOnValueLong = 0;
                altiverb.LinkEditID = "SPMr";
                altiverb.LinkEditValueLong = 1;
                altiverb.LowCrossoverID = "DCr1";
                altiverb.LowCrossoverValueFloat = 320F;
                altiverb.HighCrossoverID = "DCr2";
                altiverb.HighCrossoverValueFloat = 2400F;
                altiverb.CamAngleID = "CmAg";
                altiverb.CamAngleValueFloat = 29.1094F;
                altiverb.CamYID = "CmPy";
                altiverb.CamYValueFloat = 0.14F;
                altiverb.CamZID = "CmPz";
                altiverb.CamZValueFloat = -0.2F;
                altiverb.CamRHID = "CmRh";
                altiverb.CamRHValueFloat = -34.2F;
                altiverb.CamRVID = "CmRv";
                altiverb.CamRVValueFloat = 12.7333F;
                altiverb.ScrollZoomID = "CmZm";
                altiverb.ScrollZoomValueFloat = 1F;
                altiverb.WaveZoomID = "wcZm";
                altiverb.WaveZoomValueFloat = 1F;
                altiverb.WaveOffsetID = "wcOs";
                altiverb.WaveOffsetValueFloat = 0F;
                altiverb.TabViewID = "TbVw";
                altiverb.TabViewValueLong = 0;
                altiverb.IRScreenMouseModeID = "MsMd";
                altiverb.IRScreenMouseModeValueLong = 0;
                altiverb.ControlAdjustmentModeID = "ctla";
                altiverb.ControlAdjustmentModeValueLong = 0;
                altiverb.DecayOnID = "DcOn";
                altiverb.DecayOnValueLong = 0;
                altiverb.ReverseID = "Revs";
                altiverb.ReverseValueLong = 0;
                altiverb.IRLoadModeID = "irld";
                altiverb.IRLoadModeValueLong = 0;
                altiverb.IRLoadAsAuxID = "irlx";
                altiverb.IRLoadAsAuxValueLong = 0;
                altiverb.SelectedIRID = "SlIR";
                altiverb.SelectedIRVersion = 2;
                //altiverb.SelectedIRIrpath = "Operas & Theaters/Royal Opera Stockholm/Stereo to stereo 09m70";
                altiverb.SelectedIRIrpath = relativeIRPath;
                altiverb.SelectedIRIrid = "000-000-000-000-000-000";
                altiverb.BypassID = "byps";
                altiverb.BypassValueLong = 0;
                altiverb.ShowWaveID = "irsh";
                altiverb.ShowWaveValueLong = 2;
                altiverb.AudioEaseVersionID = "vers";
                altiverb.AudioEaseVersionValueLong = 1;
                altiverb.IRBrowserStatusID = "zz50";
                altiverb.DisplayModeID = "DsMd";
                altiverb.DisplayModeValueLong = 0;
                altiverb.DirectOnID = "DrOn";
                altiverb.DirectOnValueLong = 0; // no direct signal
                altiverb.TailOnID = "TlOn";
                altiverb.TailOnValueLong = 1;

                XmlDocument doc = CreateXMLDocument(altiverb, xmlAltiverbFilePath);

                /*
                string fxpFilePath = @"C:\Users\perivar.nerseth\Documents\CSScripts\TODD-AO st to st wide mics at 18m90.fxp";
                byte[] presetByteArray = FileUtils.readByteArrayFromFile(fxpFilePath);
                FXP fxp = new FXP(presetByteArray);
                Console.Out.WriteLine("chunkMagic: {0}", fxp.chunkMagic);
                Console.Out.WriteLine("byteSize: {0}", fxp.byteSize);
                Console.Out.WriteLine("fxMagic: {0}", fxp.fxMagic);
                Console.Out.WriteLine("version: {0}", fxp.version);
                Console.Out.WriteLine("fxID: {0}", fxp.fxID);
                Console.Out.WriteLine("fxVersion: {0}", fxp.fxVersion);
                Console.Out.WriteLine("numPrograms: {0}", fxp.numPrograms);
                Console.Out.WriteLine("name: {0}", fxp.name);
                Console.Out.WriteLine("chunkSize: {0}", fxp.chunkSize);
                */

                FXP fxp = new FXP();
                FXP.FxProgramSet fxpContent = new FXP.FxProgramSet();
                fxp.Content = fxpContent;
                fxpContent.ChunkMagic = "CcnK";
                fxpContent.FxMagic = "FPCh";
                fxpContent.Version = 0;
                fxpContent.FxID = "AVr5";
                fxpContent.FxVersion = 1;
                fxpContent.NumPrograms = 38;
                fxpContent.Name = "";
                fxp.XmlDocument = doc;
                fxp.WriteFile(outputFileName);

                /*
                // Create the Altiverb Directories
                string baseSearchDirectory = @"C:\Users\perivar.nerseth\Documents\CSScripts";
                XmlDocument xmlAltiverbFilesDoc = new XmlDocument();
                xmlAltiverbFilesDoc.Load(xmlAltiverbFilePath);
                XmlNode startNode = xmlAltiverbFilesDoc.DocumentElement;
                CreateAltiverbDirectories(startNode, baseSearchDirectory, "", 0);
                */

            }
        }
        else
        {
            PrintUsage();
        }
    }

    public static void PrintUsage()
    {
        Console.WriteLine("Usage: cscs <Script Name>.cs \n\t<Path to 'altiverb-full.xml'> \n\t<Altiverb Library Directory> \n\t<FXP Output Directory> \n\t[optionally preset type, i.e. 'stereo' or 'quad']");
        Console.WriteLine("Altiverb Library Directory example = 'C:\\Users\\<username>\\Documents\\CSScripts\\Altiverb 6.3.5 Library'>");
    }

    private static void CreateAltiverbDirectories(XmlNode xmlElement, string basePath, string nodePath, int level)
    {
        if (xmlElement.Name == "folder")
        {
            string nameAttribute = "";
            XmlAttributeCollection xmlAttributeCollection = xmlElement.Attributes;
            foreach (XmlAttribute x in xmlAttributeCollection)
            {
                if (x.Name == "name")
                {
                    nameAttribute = x.Value;
                    break;
                }
            }
            nodePath += "/" + nameAttribute;
            string fullPath = String.Format("{0}{1}", basePath, nodePath);
            Console.Out.WriteLine("Creating {0}", fullPath);
            Directory.CreateDirectory(fullPath);
            XmlNodeList xmlNodeList = xmlElement.SelectNodes("child::folder");
            ++level;
            foreach (XmlNode x in xmlNodeList)
            {
                CreateAltiverbDirectories((XmlNode)x, basePath, nodePath, level);
            }
        }
    }

    private static void WriteIRBrowserStatus(XmlNode xmlElement, XmlNode startNode, int level, TextWriter writer, string selectedPath)
    {
        //writer.Write("<!-- xmlElement {0}, startNode {1}, level {2}, selected path {3} -->", xmlElement.Name, startNode.Name, level, selectedPath);

        if (xmlElement.Name == "folder")
        {
            string[] pathNodes = selectedPath.Split('/');
            string pathNode = "";
            if ((level - 1 >= 0) && (level - 1 <= pathNodes.Length - 1))
            {
                pathNode = pathNodes[level - 1];
            }

            String levelDepth = "";
            for (int i = 0; i < level; i++)
            {
                levelDepth += "   ";
            }

            string nameAttribute = "";
            XmlAttributeCollection xmlAttributeCollection = xmlElement.Attributes;
            foreach (XmlAttribute x in xmlAttributeCollection)
            {
                if (x.Name == "name")
                {
                    nameAttribute = FilterXMLString(x.Value);
                    break;
                }
            }

            // check if xmlElement is the topNode.
            if (xmlElement == startNode)
            {
                writer.Write("\n{0}<BrowserStatus>", levelDepth);
                writer.Write("\n{0} <dict>", levelDepth);
                writer.Write("\n{0}  <s_Name />", levelDepth);
            }
            else
            {
                writer.Write("\n{0} <dict>", levelDepth);
                writer.Write("\n{0}  <s_Name>{1}</s_Name>", levelDepth, nameAttribute);
            }

            // only scan children that is a part of the selected path
            if (pathNode == nameAttribute || level == 0)
            { // || level <= 0
              //writer.Write("<!-- before foreach: xmlElement {0}, level {1} -->", xmlElement.Name, level);
                XmlNodeList xmlNodeList = xmlElement.SelectNodes("child::folder");
                if (xmlNodeList.Count > 0)
                {
                    //writer.Write("<!-- is on path and has children, level: {0} -->", level);
                    if (level == 2)
                    {
                        writer.Write("\n{0}  <i_Selected>{1}</i_Selected>", levelDepth, 1);
                        writer.Write("\n{0}  <i_Status>{1}</i_Status>", levelDepth, 0);
                    }
                    else if (level == 3)
                    {
                        writer.Write("\n{0}  <i_Selected>{1}</i_Selected>", levelDepth, 1);
                    }
                    else
                    {
                        writer.Write("\n{0}  <i_Selected>{1}</i_Selected>", levelDepth, 0);
                        writer.Write("\n{0}  <i_Status>{1}</i_Status>", levelDepth, 0);
                    }
                    writer.Write("\n{0}  <ChildNodes>", levelDepth);
                }
                else
                {
                    //writer.Write("<!-- is on path but have no children, level: {0} -->", level);
                    writer.Write("\n{0}  <i_Selected>{1}</i_Selected>", levelDepth, 1);
                    if (level < 3) writer.Write("\n{0}  <i_Status>{1}</i_Status>", levelDepth, 0);
                }
                ++level;
                foreach (XmlNode x in xmlNodeList)
                {
                    WriteIRBrowserStatus((XmlNode)x, startNode, level, writer, selectedPath);
                }
                //writer.Write("<!-- after foreach: xmlElement {0}, level {1} -->", xmlElement.Name, level);
                if (xmlNodeList.Count > 0)
                {
                    writer.Write("\n{0}  </ChildNodes>", levelDepth);
                }

            }
            else
            {
                //writer.Write("<!-- is NOT on path, level: {0} -->", level);
                if (level == 3)
                {
                    writer.Write("\n{0}  <i_Selected>{1}</i_Selected>", levelDepth, 1);
                }
                else
                {
                    writer.Write("\n{0}  <i_Selected>{1}</i_Selected>", levelDepth, 0);
                }
                if (level < 3) writer.Write("\n{0}  <i_Status>{1}</i_Status>", levelDepth, 3);
            }

            // check if xmlElement is the topNode.
            if (xmlElement == startNode)
            {
                writer.Write("\n{0}  <BrowserScrollbar>", levelDepth);
                writer.Write("\n{0}  	<dict>", levelDepth);
                writer.Write("\n{0}  		<r_ContentHeight>0</r_ContentHeight>", levelDepth); // 264
                writer.Write("\n{0}  		<r_VisibleTop>0</r_VisibleTop>", levelDepth);
                writer.Write("\n{0}  	</dict>", levelDepth);
                writer.Write("\n{0}  </BrowserScrollbar>", levelDepth);
                writer.Write("\n{0}  <IRListScrollbar>", levelDepth);
                writer.Write("\n{0}  	<dict>", levelDepth);
                writer.Write("\n{0}  		<r_ContentHeight>0</r_ContentHeight>", levelDepth); //55
                writer.Write("\n{0}  		<r_VisibleTop>0</r_VisibleTop>", levelDepth);
                writer.Write("\n{0}  	</dict>", levelDepth);
                writer.Write("\n{0}  </IRListScrollbar>", levelDepth);

                writer.Write("\n{0} </dict>", levelDepth);
                writer.Write("\n{0}</BrowserStatus>", levelDepth);
            }
            else
            {
                writer.Write("\n{0} </dict>", levelDepth);
            }
        }
    }

    private static string FilterXMLString(string inputString)
    {
        string returnString = inputString;
        if (inputString.IndexOf("&") > 0)
        {
            returnString = inputString.Replace("&", "&amp;");
            //Console.WriteLine("Replacing '&': '{0}'->'{1}'", inputString, returnString);
        }
        if (inputString.IndexOf("'") > 0)
        {
            returnString = inputString.Replace("'", "&apos;");
            //Console.WriteLine("Replacing ''': '{0}'->'{1}'", inputString, returnString);
        }
        return returnString;
    }

    private static string GetQName(XmlNode node)
    {
        string qname = string.Empty;

        if (node.NamespaceURI.CompareTo(string.Empty) != 0)
        {
            if (node.Prefix.CompareTo(string.Empty) != 0)
            {
                //If the prefix is present, use it.
                if (node.NodeType == XmlNodeType.Attribute)
                {
                    qname = "@";
                }
                qname = qname + node.Prefix + ":" + node.LocalName;
            }
            else
            {
                //The node is in the default namespace, the prefix is not present.
                if (node.NodeType == XmlNodeType.Attribute)
                {
                    qname = "@*[local-name() = '" + node.LocalName + "' and namespace-uri()='" + node.NamespaceURI + "']";
                }
                else
                {
                    //QName is a misnomer here, but the current node belongs in a non-prefixed namespace...
                }
                qname = "node()[local-name() = '" + node.LocalName + "' and namespace-uri()='" + node.NamespaceURI + "']";
            }
        }
        else
        {
            if (node.NodeType == XmlNodeType.Attribute)
            {
                qname = "@" + node.Name;
            }
            else
            {
                qname = node.Name;
            }
        }
        return (qname);
    }

    private static string GetPathFromNode(XmlNode baseNode)
    {
        string path = "";
        XmlNodeList nodes = null;
        if (baseNode.NodeType == XmlNodeType.Attribute)
        {
            nodes = baseNode.SelectNodes("ancestor::*");
        }
        else
        {
            nodes = baseNode.SelectNodes("ancestor-or-self::*"); //"ancestor-or-self::* | ancestor-or-self::@*"
        }
        foreach (XmlNode node in nodes)
        {
            int nodePosition = node.SelectNodes("preceding-sibling::*[local-name()='" + node.LocalName + "' and namespace-uri()='" + node.NamespaceURI + "']").Count + 1;
            path += "/" + GetQName(node) + "[" + nodePosition.ToString() + "]";
        }
        if (baseNode.NodeType == XmlNodeType.Attribute)
        {
            path += "/" + GetQName(baseNode);
        }
        return (path);
    }

    static XmlDocument CreateXMLDocument(Altiverb altiverb, string xmlAltiverbFilePath)
    {
        XmlDocument doc = new XmlDocument();
        XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", null, null);

        doc.AppendChild(dec);
        XmlElement root = doc.CreateElement("plist");
        root.SetAttribute("version", "1.0");
        doc.AppendChild(root);

        XmlElement dictNode0 = doc.CreateElement("dict");
        root.AppendChild(dictNode0);

        XmlElement presetTypeNode = doc.CreateElement("s_Type");
        presetTypeNode.InnerText = "Mission Preset";
        dictNode0.AppendChild(presetTypeNode);
        XmlElement presetVersionNode = doc.CreateElement("i_Version");
        presetVersionNode.InnerText = "1";
        dictNode0.AppendChild(presetVersionNode);
        XmlElement parmsNode = doc.CreateElement("Parms");
        dictNode0.AppendChild(parmsNode);

        XmlElement dictNode1 = doc.CreateElement("dict");
        XmlElement presetNameNode = doc.CreateElement("s_Name");
        presetNameNode.InnerText = "Preset Name";
        dictNode1.AppendChild(presetNameNode);
        XmlElement presetNameIDNode = doc.CreateElement("s_ID");
        presetNameIDNode.InnerText = altiverb.PresetNameID;
        dictNode1.AppendChild(presetNameIDNode);
        XmlElement presetNameVersionNode = doc.CreateElement("i_version");
        presetNameVersionNode.InnerText = altiverb.PresetNameVersion.ToString();
        dictNode1.AppendChild(presetNameVersionNode);
        XmlElement presetNameNeedssaveNode = doc.CreateElement("i_needssave");
        presetNameNeedssaveNode.InnerText = altiverb.PresetNameNeedssave.ToString();
        dictNode1.AppendChild(presetNameNeedssaveNode);
        XmlElement presetNameInpresetfolderNode = doc.CreateElement("i_inpresetfolder");
        presetNameInpresetfolderNode.InnerText = altiverb.PresetNameInpresetfolder.ToString();
        dictNode1.AppendChild(presetNameInpresetfolderNode);
        XmlElement presetNamePresetpathNode = doc.CreateElement("s_presetpath");
        presetNamePresetpathNode.InnerText = altiverb.PresetNamePresetpath;
        dictNode1.AppendChild(presetNamePresetpathNode);
        parmsNode.AppendChild(dictNode1);

        XmlElement dictNode2 = doc.CreateElement("dict");
        XmlElement allChunksNode = doc.CreateElement("s_Name");
        allChunksNode.InnerText = "All Chunks";
        dictNode2.AppendChild(allChunksNode);
        XmlElement allChunksIDNode = doc.CreateElement("s_ID");
        allChunksIDNode.InnerText = altiverb.AllChunksID;
        dictNode2.AppendChild(allChunksIDNode);
        XmlElement allChunksVersionNode = doc.CreateElement("i_version");
        allChunksVersionNode.InnerText = altiverb.AllChunksVersion.ToString();
        dictNode2.AppendChild(allChunksVersionNode);
        parmsNode.AppendChild(dictNode2);

        XmlElement dictNode3 = doc.CreateElement("dict");
        XmlElement snapshotNode = doc.CreateElement("s_Name");
        snapshotNode.InnerText = "Snapshot";
        dictNode3.AppendChild(snapshotNode);
        XmlElement snapshotIDNode = doc.CreateElement("s_ID");
        snapshotIDNode.InnerText = altiverb.SnapshotID;
        dictNode3.AppendChild(snapshotIDNode);
        XmlElement snapshotValueLongNode = doc.CreateElement("i_ValueLong");
        snapshotValueLongNode.InnerText = altiverb.SnapshotValueLong.ToString();
        dictNode3.AppendChild(snapshotValueLongNode);
        parmsNode.AppendChild(dictNode3);

        XmlElement dictNode4 = doc.CreateElement("dict");
        XmlElement automationPresetIndexNode = doc.CreateElement("s_Name");
        automationPresetIndexNode.InnerText = "Automation Preset Index";
        dictNode4.AppendChild(automationPresetIndexNode);
        XmlElement automationPresetIndexIDNode = doc.CreateElement("s_ID");
        automationPresetIndexIDNode.InnerText = altiverb.AutomationPresetIndexID;
        dictNode4.AppendChild(automationPresetIndexIDNode);
        XmlElement automationPresetIndexValueLongNode = doc.CreateElement("i_ValueLong");
        automationPresetIndexValueLongNode.InnerText = altiverb.AutomationPresetIndexValueLong.ToString();
        dictNode4.AppendChild(automationPresetIndexValueLongNode);
        parmsNode.AppendChild(dictNode4);

        XmlElement dictNode5 = doc.CreateElement("dict");
        XmlElement earlyReflectionsOnNode = doc.CreateElement("s_Name");
        earlyReflectionsOnNode.InnerText = "Early Reflections On";
        dictNode5.AppendChild(earlyReflectionsOnNode);
        XmlElement earlyReflectionsOnIDNode = doc.CreateElement("s_ID");
        earlyReflectionsOnIDNode.InnerText = altiverb.EarlyReflectionsOnID;
        dictNode5.AppendChild(earlyReflectionsOnIDNode);
        XmlElement earlyReflectionsOnValueLongNode = doc.CreateElement("i_ValueLong");
        earlyReflectionsOnValueLongNode.InnerText = altiverb.EarlyReflectionsOnValueLong.ToString();
        dictNode5.AppendChild(earlyReflectionsOnValueLongNode);
        parmsNode.AppendChild(dictNode5);

        XmlElement dictNode6 = doc.CreateElement("dict");
        XmlElement directGainNode = doc.CreateElement("s_Name");
        directGainNode.InnerText = "Direct Gain";
        dictNode6.AppendChild(directGainNode);
        XmlElement directGainIDNode = doc.CreateElement("s_ID");
        directGainIDNode.InnerText = altiverb.DirectGainID;
        dictNode6.AppendChild(directGainIDNode);
        XmlElement directGainValueFloatNode = doc.CreateElement("r_ValueFloat");
        directGainValueFloatNode.InnerText = altiverb.DirectGainValueFloat.ToString().Replace(",", ".");
        dictNode6.AppendChild(directGainValueFloatNode);
        parmsNode.AppendChild(dictNode6);

        XmlElement dictNode7 = doc.CreateElement("dict");
        XmlElement directColorNode = doc.CreateElement("s_Name");
        directColorNode.InnerText = "Direct Color";
        dictNode7.AppendChild(directColorNode);
        XmlElement directColorIDNode = doc.CreateElement("s_ID");
        directColorIDNode.InnerText = altiverb.DirectColorID;
        dictNode7.AppendChild(directColorIDNode);
        XmlElement directColorValueFloatNode = doc.CreateElement("r_ValueFloat");
        directColorValueFloatNode.InnerText = altiverb.DirectColorValueFloat.ToString().Replace(",", ".");
        dictNode7.AppendChild(directColorValueFloatNode);
        parmsNode.AppendChild(dictNode7);

        XmlElement dictNode8 = doc.CreateElement("dict");
        XmlElement earlyrefGainNode = doc.CreateElement("s_Name");
        earlyrefGainNode.InnerText = "EarlyRef Gain";
        dictNode8.AppendChild(earlyrefGainNode);
        XmlElement earlyrefGainIDNode = doc.CreateElement("s_ID");
        earlyrefGainIDNode.InnerText = altiverb.EarlyRefGainID;
        dictNode8.AppendChild(earlyrefGainIDNode);
        XmlElement earlyrefGainValueFloatNode = doc.CreateElement("r_ValueFloat");
        earlyrefGainValueFloatNode.InnerText = altiverb.EarlyRefGainValueFloat.ToString().Replace(",", ".");
        dictNode8.AppendChild(earlyrefGainValueFloatNode);
        parmsNode.AppendChild(dictNode8);

        XmlElement dictNode9 = doc.CreateElement("dict");
        XmlElement earlyrefDelayNode = doc.CreateElement("s_Name");
        earlyrefDelayNode.InnerText = "EarlyRef Delay";
        dictNode9.AppendChild(earlyrefDelayNode);
        XmlElement earlyrefDelayIDNode = doc.CreateElement("s_ID");
        earlyrefDelayIDNode.InnerText = altiverb.EarlyRefDelayID;
        dictNode9.AppendChild(earlyrefDelayIDNode);
        XmlElement earlyrefDelayValueFloatNode = doc.CreateElement("r_ValueFloat");
        earlyrefDelayValueFloatNode.InnerText = altiverb.EarlyRefDelayValueFloat.ToString().Replace(",", ".");
        dictNode9.AppendChild(earlyrefDelayValueFloatNode);
        parmsNode.AppendChild(dictNode9);

        XmlElement dictNode10 = doc.CreateElement("dict");
        XmlElement tailGainNode = doc.CreateElement("s_Name");
        tailGainNode.InnerText = "Tail Gain";
        dictNode10.AppendChild(tailGainNode);
        XmlElement tailGainIDNode = doc.CreateElement("s_ID");
        tailGainIDNode.InnerText = altiverb.TailGainID;
        dictNode10.AppendChild(tailGainIDNode);
        XmlElement tailGainValueFloatNode = doc.CreateElement("r_ValueFloat");
        tailGainValueFloatNode.InnerText = altiverb.TailGainValueFloat.ToString().Replace(",", ".");
        dictNode10.AppendChild(tailGainValueFloatNode);
        parmsNode.AppendChild(dictNode10);

        XmlElement dictNode11 = doc.CreateElement("dict");
        XmlElement tailDelayNode = doc.CreateElement("s_Name");
        tailDelayNode.InnerText = "Tail Delay";
        dictNode11.AppendChild(tailDelayNode);
        XmlElement tailDelayIDNode = doc.CreateElement("s_ID");
        tailDelayIDNode.InnerText = altiverb.TailDelayID;
        dictNode11.AppendChild(tailDelayIDNode);
        XmlElement tailDelayValueFloatNode = doc.CreateElement("r_ValueFloat");
        tailDelayValueFloatNode.InnerText = altiverb.TailDelayValueFloat.ToString().Replace(",", ".");
        dictNode11.AppendChild(tailDelayValueFloatNode);
        parmsNode.AppendChild(dictNode11);

        XmlElement dictNode12 = doc.CreateElement("dict");
        XmlElement reverbTimeNode = doc.CreateElement("s_Name");
        reverbTimeNode.InnerText = "Reverb Time";
        dictNode12.AppendChild(reverbTimeNode);
        XmlElement reverbTimeIDNode = doc.CreateElement("s_ID");
        reverbTimeIDNode.InnerText = altiverb.ReverbTimeID;
        dictNode12.AppendChild(reverbTimeIDNode);
        XmlElement reverbTimeValueFloatNode = doc.CreateElement("r_ValueFloat");
        reverbTimeValueFloatNode.InnerText = altiverb.ReverbTimeValueFloat.ToString().Replace(",", ".");
        dictNode12.AppendChild(reverbTimeValueFloatNode);
        parmsNode.AppendChild(dictNode12);

        XmlElement dictNode13 = doc.CreateElement("dict");
        XmlElement lowDampNode = doc.CreateElement("s_Name");
        lowDampNode.InnerText = "Low Damp";
        dictNode13.AppendChild(lowDampNode);
        XmlElement lowDampIDNode = doc.CreateElement("s_ID");
        lowDampIDNode.InnerText = altiverb.LowDampID;
        dictNode13.AppendChild(lowDampIDNode);
        XmlElement lowDampValueFloatNode = doc.CreateElement("r_ValueFloat");
        lowDampValueFloatNode.InnerText = altiverb.LowDampValueFloat.ToString().Replace(",", ".");
        dictNode13.AppendChild(lowDampValueFloatNode);
        parmsNode.AppendChild(dictNode13);

        XmlElement dictNode14 = doc.CreateElement("dict");
        XmlElement midDampNode = doc.CreateElement("s_Name");
        midDampNode.InnerText = "Mid Damp";
        dictNode14.AppendChild(midDampNode);
        XmlElement midDampIDNode = doc.CreateElement("s_ID");
        midDampIDNode.InnerText = altiverb.MidDampID;
        dictNode14.AppendChild(midDampIDNode);
        XmlElement midDampValueFloatNode = doc.CreateElement("r_ValueFloat");
        midDampValueFloatNode.InnerText = altiverb.MidDampValueFloat.ToString().Replace(",", ".");
        dictNode14.AppendChild(midDampValueFloatNode);
        parmsNode.AppendChild(dictNode14);

        XmlElement dictNode15 = doc.CreateElement("dict");
        XmlElement highDampNode = doc.CreateElement("s_Name");
        highDampNode.InnerText = "High Damp";
        dictNode15.AppendChild(highDampNode);
        XmlElement highDampIDNode = doc.CreateElement("s_ID");
        highDampIDNode.InnerText = altiverb.HighDampID;
        dictNode15.AppendChild(highDampIDNode);
        XmlElement highDampValueFloatNode = doc.CreateElement("r_ValueFloat");
        highDampValueFloatNode.InnerText = altiverb.HighDampValueFloat.ToString().Replace(",", ".");
        dictNode15.AppendChild(highDampValueFloatNode);
        parmsNode.AppendChild(dictNode15);

        XmlElement dictNode16 = doc.CreateElement("dict");
        XmlElement eqBassLevelNode = doc.CreateElement("s_Name");
        eqBassLevelNode.InnerText = "EQ Bass Level";
        dictNode16.AppendChild(eqBassLevelNode);
        XmlElement eqBassLevelIDNode = doc.CreateElement("s_ID");
        eqBassLevelIDNode.InnerText = altiverb.EQBassLevelID;
        dictNode16.AppendChild(eqBassLevelIDNode);
        XmlElement eqBassLevelValueFloatNode = doc.CreateElement("r_ValueFloat");
        eqBassLevelValueFloatNode.InnerText = altiverb.EQBassLevelValueFloat.ToString().Replace(",", ".");
        dictNode16.AppendChild(eqBassLevelValueFloatNode);
        parmsNode.AppendChild(dictNode16);

        XmlElement dictNode17 = doc.CreateElement("dict");
        XmlElement eqTrebleLevelNode = doc.CreateElement("s_Name");
        eqTrebleLevelNode.InnerText = "EQ Treble Level";
        dictNode17.AppendChild(eqTrebleLevelNode);
        XmlElement eqTrebleLevelIDNode = doc.CreateElement("s_ID");
        eqTrebleLevelIDNode.InnerText = altiverb.EQTrebleLevelID;
        dictNode17.AppendChild(eqTrebleLevelIDNode);
        XmlElement eqTrebleLevelValueFloatNode = doc.CreateElement("r_ValueFloat");
        eqTrebleLevelValueFloatNode.InnerText = altiverb.EQTrebleLevelValueFloat.ToString().Replace(",", ".");
        dictNode17.AppendChild(eqTrebleLevelValueFloatNode);
        parmsNode.AppendChild(dictNode17);

        XmlElement dictNode18 = doc.CreateElement("dict");
        XmlElement eqLoFreqNode = doc.CreateElement("s_Name");
        eqLoFreqNode.InnerText = "EQ Lo Freq";
        dictNode18.AppendChild(eqLoFreqNode);
        XmlElement eqLoFreqIDNode = doc.CreateElement("s_ID");
        eqLoFreqIDNode.InnerText = altiverb.EQLoFreqID;
        dictNode18.AppendChild(eqLoFreqIDNode);
        XmlElement eqLoFreqValueFloatNode = doc.CreateElement("r_ValueFloat");
        eqLoFreqValueFloatNode.InnerText = altiverb.EQLoFreqValueFloat.ToString().Replace(",", ".");
        dictNode18.AppendChild(eqLoFreqValueFloatNode);
        parmsNode.AppendChild(dictNode18);

        XmlElement dictNode19 = doc.CreateElement("dict");
        XmlElement eqLoQNode = doc.CreateElement("s_Name");
        eqLoQNode.InnerText = "EQ Lo Q";
        dictNode19.AppendChild(eqLoQNode);
        XmlElement eqLoQIDNode = doc.CreateElement("s_ID");
        eqLoQIDNode.InnerText = altiverb.EQLoQID;
        dictNode19.AppendChild(eqLoQIDNode);
        XmlElement eqLoQValueFloatNode = doc.CreateElement("r_ValueFloat");
        eqLoQValueFloatNode.InnerText = altiverb.EQLoQValueFloat.ToString().Replace(",", ".");
        dictNode19.AppendChild(eqLoQValueFloatNode);
        parmsNode.AppendChild(dictNode19);

        XmlElement dictNode20 = doc.CreateElement("dict");
        XmlElement eqLoGainNode = doc.CreateElement("s_Name");
        eqLoGainNode.InnerText = "EQ Lo Gain";
        dictNode20.AppendChild(eqLoGainNode);
        XmlElement eqLoGainIDNode = doc.CreateElement("s_ID");
        eqLoGainIDNode.InnerText = altiverb.EQLoGainID;
        dictNode20.AppendChild(eqLoGainIDNode);
        XmlElement eqLoGainValueFloatNode = doc.CreateElement("r_ValueFloat");
        eqLoGainValueFloatNode.InnerText = altiverb.EQLoGainValueFloat.ToString().Replace(",", ".");
        dictNode20.AppendChild(eqLoGainValueFloatNode);
        parmsNode.AppendChild(dictNode20);

        XmlElement dictNode21 = doc.CreateElement("dict");
        XmlElement eqHiFreqNode = doc.CreateElement("s_Name");
        eqHiFreqNode.InnerText = "EQ Hi Freq";
        dictNode21.AppendChild(eqHiFreqNode);
        XmlElement eqHiFreqIDNode = doc.CreateElement("s_ID");
        eqHiFreqIDNode.InnerText = altiverb.EQHiFreqID;
        dictNode21.AppendChild(eqHiFreqIDNode);
        XmlElement eqHiFreqValueFloatNode = doc.CreateElement("r_ValueFloat");
        eqHiFreqValueFloatNode.InnerText = altiverb.EQHiFreqValueFloat.ToString().Replace(",", ".");
        dictNode21.AppendChild(eqHiFreqValueFloatNode);
        parmsNode.AppendChild(dictNode21);

        XmlElement dictNode22 = doc.CreateElement("dict");
        XmlElement eqHiQNode = doc.CreateElement("s_Name");
        eqHiQNode.InnerText = "EQ Hi Q";
        dictNode22.AppendChild(eqHiQNode);
        XmlElement eqHiQIDNode = doc.CreateElement("s_ID");
        eqHiQIDNode.InnerText = altiverb.EQHiQID;
        dictNode22.AppendChild(eqHiQIDNode);
        XmlElement eqHiQValueFloatNode = doc.CreateElement("r_ValueFloat");
        eqHiQValueFloatNode.InnerText = altiverb.EQHiQValueFloat.ToString().Replace(",", ".");
        dictNode22.AppendChild(eqHiQValueFloatNode);
        parmsNode.AppendChild(dictNode22);

        XmlElement dictNode23 = doc.CreateElement("dict");
        XmlElement eqHiGainNode = doc.CreateElement("s_Name");
        eqHiGainNode.InnerText = "EQ Hi Gain";
        dictNode23.AppendChild(eqHiGainNode);
        XmlElement eqHiGainIDNode = doc.CreateElement("s_ID");
        eqHiGainIDNode.InnerText = altiverb.EQHiGainID;
        dictNode23.AppendChild(eqHiGainIDNode);
        XmlElement eqHiGainValueFloatNode = doc.CreateElement("r_ValueFloat");
        eqHiGainValueFloatNode.InnerText = altiverb.EQHiGainValueFloat.ToString().Replace(",", ".");
        dictNode23.AppendChild(eqHiGainValueFloatNode);
        parmsNode.AppendChild(dictNode23);

        XmlElement dictNode24 = doc.CreateElement("dict");
        XmlElement masterInLevelNode = doc.CreateElement("s_Name");
        masterInLevelNode.InnerText = "Master In Level";
        dictNode24.AppendChild(masterInLevelNode);
        XmlElement masterInLevelIDNode = doc.CreateElement("s_ID");
        masterInLevelIDNode.InnerText = altiverb.MasterInLevelID;
        dictNode24.AppendChild(masterInLevelIDNode);
        XmlElement masterInLevelValueFloatNode = doc.CreateElement("r_ValueFloat");
        masterInLevelValueFloatNode.InnerText = altiverb.MasterInLevelValueFloat.ToString().Replace(",", ".");
        dictNode24.AppendChild(masterInLevelValueFloatNode);
        parmsNode.AppendChild(dictNode24);

        XmlElement dictNode25 = doc.CreateElement("dict");
        XmlElement masterOutLevelNode = doc.CreateElement("s_Name");
        masterOutLevelNode.InnerText = "Master Out Level";
        dictNode25.AppendChild(masterOutLevelNode);
        XmlElement masterOutLevelIDNode = doc.CreateElement("s_ID");
        masterOutLevelIDNode.InnerText = altiverb.MasterOutLevelID;
        dictNode25.AppendChild(masterOutLevelIDNode);
        XmlElement masterOutLevelValueFloatNode = doc.CreateElement("r_ValueFloat");
        masterOutLevelValueFloatNode.InnerText = altiverb.MasterOutLevelValueFloat.ToString().Replace(",", ".");
        dictNode25.AppendChild(masterOutLevelValueFloatNode);
        parmsNode.AppendChild(dictNode25);

        XmlElement dictNode26 = doc.CreateElement("dict");
        XmlElement masterFrontLevelNode = doc.CreateElement("s_Name");
        masterFrontLevelNode.InnerText = "Master Front Level";
        dictNode26.AppendChild(masterFrontLevelNode);
        XmlElement masterFrontLevelIDNode = doc.CreateElement("s_ID");
        masterFrontLevelIDNode.InnerText = altiverb.MasterFrontLevelID;
        dictNode26.AppendChild(masterFrontLevelIDNode);
        XmlElement masterFrontLevelValueFloatNode = doc.CreateElement("r_ValueFloat");
        masterFrontLevelValueFloatNode.InnerText = altiverb.MasterFrontLevelValueFloat.ToString().Replace(",", ".");
        dictNode26.AppendChild(masterFrontLevelValueFloatNode);
        parmsNode.AppendChild(dictNode26);

        XmlElement dictNode27 = doc.CreateElement("dict");
        XmlElement masterRearLevelNode = doc.CreateElement("s_Name");
        masterRearLevelNode.InnerText = "Master Rear Level";
        dictNode27.AppendChild(masterRearLevelNode);
        XmlElement masterRearLevelIDNode = doc.CreateElement("s_ID");
        masterRearLevelIDNode.InnerText = altiverb.MasterRearLevelID;
        dictNode27.AppendChild(masterRearLevelIDNode);
        XmlElement masterRearLevelValueFloatNode = doc.CreateElement("r_ValueFloat");
        masterRearLevelValueFloatNode.InnerText = altiverb.MasterRearLevelValueFloat.ToString().Replace(",", ".");
        dictNode27.AppendChild(masterRearLevelValueFloatNode);
        parmsNode.AppendChild(dictNode27);

        XmlElement dictNode28 = doc.CreateElement("dict");
        XmlElement dryWetMixNode = doc.CreateElement("s_Name");
        dryWetMixNode.InnerText = "Dry/Wet Mix";
        dictNode28.AppendChild(dryWetMixNode);
        XmlElement dryWetMixIDNode = doc.CreateElement("s_ID");
        dryWetMixIDNode.InnerText = altiverb.DryWetMixID;
        dictNode28.AppendChild(dryWetMixIDNode);
        XmlElement dryWetMixValueFloatNode = doc.CreateElement("r_ValueFloat");
        dryWetMixValueFloatNode.InnerText = altiverb.DryWetMixValueFloat.ToString().Replace(",", ".");
        dictNode28.AppendChild(dryWetMixValueFloatNode);
        parmsNode.AppendChild(dictNode28);

        XmlElement dictNode29 = doc.CreateElement("dict");
        XmlElement centerBleedNode = doc.CreateElement("s_Name");
        centerBleedNode.InnerText = "Center Bleed";
        dictNode29.AppendChild(centerBleedNode);
        XmlElement centerBleedIDNode = doc.CreateElement("s_ID");
        centerBleedIDNode.InnerText = altiverb.CenterBleedID;
        dictNode29.AppendChild(centerBleedIDNode);
        XmlElement centerBleedValueFloatNode = doc.CreateElement("r_ValueFloat");
        centerBleedValueFloatNode.InnerText = altiverb.CenterBleedValueFloat.ToString().Replace(",", ".");
        dictNode29.AppendChild(centerBleedValueFloatNode);
        parmsNode.AppendChild(dictNode29);

        XmlElement dictNode30 = doc.CreateElement("dict");
        XmlElement lfeBleedNode = doc.CreateElement("s_Name");
        lfeBleedNode.InnerText = "Lfe Bleed";
        dictNode30.AppendChild(lfeBleedNode);
        XmlElement lfeBleedIDNode = doc.CreateElement("s_ID");
        lfeBleedIDNode.InnerText = altiverb.LfeBleedID;
        dictNode30.AppendChild(lfeBleedIDNode);
        XmlElement lfeBleedValueFloatNode = doc.CreateElement("r_ValueFloat");
        lfeBleedValueFloatNode.InnerText = altiverb.LfeBleedValueFloat.ToString().Replace(",", ".");
        dictNode30.AppendChild(lfeBleedValueFloatNode);
        parmsNode.AppendChild(dictNode30);

        XmlElement dictNode31 = doc.CreateElement("dict");
        XmlElement sampleLevelNode = doc.CreateElement("s_Name");
        sampleLevelNode.InnerText = "Sample Level";
        dictNode31.AppendChild(sampleLevelNode);
        XmlElement sampleLevelIDNode = doc.CreateElement("s_ID");
        sampleLevelIDNode.InnerText = altiverb.SampleLevelID;
        dictNode31.AppendChild(sampleLevelIDNode);
        XmlElement sampleLevelValueFloatNode = doc.CreateElement("r_ValueFloat");
        sampleLevelValueFloatNode.InnerText = altiverb.SampleLevelValueFloat.ToString().Replace(",", ".");
        dictNode31.AppendChild(sampleLevelValueFloatNode);
        parmsNode.AppendChild(dictNode31);

        XmlElement dictNode32 = doc.CreateElement("dict");
        XmlElement speakerLeftXNode = doc.CreateElement("s_Name");
        speakerLeftXNode.InnerText = "Speaker Left X";
        dictNode32.AppendChild(speakerLeftXNode);
        XmlElement speakerLeftXIDNode = doc.CreateElement("s_ID");
        speakerLeftXIDNode.InnerText = altiverb.SpeakerLeftXID;
        dictNode32.AppendChild(speakerLeftXIDNode);
        XmlElement speakerLeftXValueFloatNode = doc.CreateElement("r_ValueFloat");
        speakerLeftXValueFloatNode.InnerText = altiverb.SpeakerLeftXValueFloat.ToString().Replace(",", ".");
        dictNode32.AppendChild(speakerLeftXValueFloatNode);
        parmsNode.AppendChild(dictNode32);

        XmlElement dictNode33 = doc.CreateElement("dict");
        XmlElement speakerRightXNode = doc.CreateElement("s_Name");
        speakerRightXNode.InnerText = "Speaker Right X";
        dictNode33.AppendChild(speakerRightXNode);
        XmlElement speakerRightXIDNode = doc.CreateElement("s_ID");
        speakerRightXIDNode.InnerText = altiverb.SpeakerRightXID;
        dictNode33.AppendChild(speakerRightXIDNode);
        XmlElement speakerRightXValueFloatNode = doc.CreateElement("r_ValueFloat");
        speakerRightXValueFloatNode.InnerText = altiverb.SpeakerRightXValueFloat.ToString().Replace(",", ".");
        dictNode33.AppendChild(speakerRightXValueFloatNode);
        parmsNode.AppendChild(dictNode33);

        XmlElement dictNode34 = doc.CreateElement("dict");
        XmlElement speakerCenterXNode = doc.CreateElement("s_Name");
        speakerCenterXNode.InnerText = "Speaker Center X";
        dictNode34.AppendChild(speakerCenterXNode);
        XmlElement speakerCenterXIDNode = doc.CreateElement("s_ID");
        speakerCenterXIDNode.InnerText = altiverb.SpeakerCenterXID;
        dictNode34.AppendChild(speakerCenterXIDNode);
        XmlElement speakerCenterXValueFloatNode = doc.CreateElement("r_ValueFloat");
        speakerCenterXValueFloatNode.InnerText = altiverb.SpeakerCenterXValueFloat.ToString().Replace(",", ".");
        dictNode34.AppendChild(speakerCenterXValueFloatNode);
        parmsNode.AppendChild(dictNode34);

        XmlElement dictNode35 = doc.CreateElement("dict");
        XmlElement speakerYNode = doc.CreateElement("s_Name");
        speakerYNode.InnerText = "Speaker Y";
        dictNode35.AppendChild(speakerYNode);
        XmlElement speakerYIDNode = doc.CreateElement("s_ID");
        speakerYIDNode.InnerText = altiverb.SpeakerYID;
        dictNode35.AppendChild(speakerYIDNode);
        XmlElement speakerYValueFloatNode = doc.CreateElement("r_ValueFloat");
        speakerYValueFloatNode.InnerText = altiverb.SpeakerYValueFloat.ToString().Replace(",", ".");
        dictNode35.AppendChild(speakerYValueFloatNode);
        parmsNode.AppendChild(dictNode35);

        XmlElement dictNode36 = doc.CreateElement("dict");
        XmlElement eqOnNode = doc.CreateElement("s_Name");
        eqOnNode.InnerText = "EQ On";
        dictNode36.AppendChild(eqOnNode);
        XmlElement eqOnIDNode = doc.CreateElement("s_ID");
        eqOnIDNode.InnerText = altiverb.EQOnID;
        dictNode36.AppendChild(eqOnIDNode);
        XmlElement eqOnValueLongNode = doc.CreateElement("i_ValueLong");
        eqOnValueLongNode.InnerText = altiverb.EQOnValueLong.ToString();
        dictNode36.AppendChild(eqOnValueLongNode);
        parmsNode.AppendChild(dictNode36);

        XmlElement dictNode37 = doc.CreateElement("dict");
        XmlElement sizeNode = doc.CreateElement("s_Name");
        sizeNode.InnerText = "Size";
        dictNode37.AppendChild(sizeNode);
        XmlElement sizeIDNode = doc.CreateElement("s_ID");
        sizeIDNode.InnerText = altiverb.SizeID;
        dictNode37.AppendChild(sizeIDNode);
        XmlElement sizeValueFloatNode = doc.CreateElement("r_ValueFloat");
        sizeValueFloatNode.InnerText = altiverb.SizeValueFloat.ToString().Replace(",", ".");
        dictNode37.AppendChild(sizeValueFloatNode);
        parmsNode.AppendChild(dictNode37);

        XmlElement dictNode38 = doc.CreateElement("dict");
        XmlElement tailCutNode = doc.CreateElement("s_Name");
        tailCutNode.InnerText = "Tail Cut";
        dictNode38.AppendChild(tailCutNode);
        XmlElement tailCutIDNode = doc.CreateElement("s_ID");
        tailCutIDNode.InnerText = altiverb.TailCutID;
        dictNode38.AppendChild(tailCutIDNode);
        XmlElement tailCutValueFloatNode = doc.CreateElement("r_ValueFloat");
        tailCutValueFloatNode.InnerText = altiverb.TailCutValueFloat.ToString().Replace(",", ".");
        dictNode38.AppendChild(tailCutValueFloatNode);
        parmsNode.AppendChild(dictNode38);

        XmlElement dictNode39 = doc.CreateElement("dict");
        XmlElement latencyModeNode = doc.CreateElement("s_Name");
        latencyModeNode.InnerText = "Latency Mode";
        dictNode39.AppendChild(latencyModeNode);
        XmlElement latencyModeIDNode = doc.CreateElement("s_ID");
        latencyModeIDNode.InnerText = altiverb.LatencyModeID;
        dictNode39.AppendChild(latencyModeIDNode);
        XmlElement latencyModeValueLongNode = doc.CreateElement("i_ValueLong");
        latencyModeValueLongNode.InnerText = altiverb.LatencyModeValueLong.ToString();
        dictNode39.AppendChild(latencyModeValueLongNode);
        parmsNode.AppendChild(dictNode39);

        XmlElement dictNode40 = doc.CreateElement("dict");
        XmlElement stagePositionsOnNode = doc.CreateElement("s_Name");
        stagePositionsOnNode.InnerText = "Stage Positions On";
        dictNode40.AppendChild(stagePositionsOnNode);
        XmlElement stagePositionsOnIDNode = doc.CreateElement("s_ID");
        stagePositionsOnIDNode.InnerText = altiverb.StagePositionsOnID;
        dictNode40.AppendChild(stagePositionsOnIDNode);
        XmlElement stagePositionsOnValueLongNode = doc.CreateElement("i_ValueLong");
        stagePositionsOnValueLongNode.InnerText = altiverb.StagePositionsOnValueLong.ToString();
        dictNode40.AppendChild(stagePositionsOnValueLongNode);
        parmsNode.AppendChild(dictNode40);

        XmlElement dictNode41 = doc.CreateElement("dict");
        XmlElement linkEditNode = doc.CreateElement("s_Name");
        linkEditNode.InnerText = "Link Edit";
        dictNode41.AppendChild(linkEditNode);
        XmlElement linkEditIDNode = doc.CreateElement("s_ID");
        linkEditIDNode.InnerText = altiverb.LinkEditID;
        dictNode41.AppendChild(linkEditIDNode);
        XmlElement linkEditValueLongNode = doc.CreateElement("i_ValueLong");
        linkEditValueLongNode.InnerText = altiverb.LinkEditValueLong.ToString();
        dictNode41.AppendChild(linkEditValueLongNode);
        parmsNode.AppendChild(dictNode41);

        XmlElement dictNode42 = doc.CreateElement("dict");
        XmlElement lowCrossoverNode = doc.CreateElement("s_Name");
        lowCrossoverNode.InnerText = "Low Crossover";
        dictNode42.AppendChild(lowCrossoverNode);
        XmlElement lowCrossoverIDNode = doc.CreateElement("s_ID");
        lowCrossoverIDNode.InnerText = altiverb.LowCrossoverID;
        dictNode42.AppendChild(lowCrossoverIDNode);
        XmlElement lowCrossoverValueFloatNode = doc.CreateElement("r_ValueFloat");
        lowCrossoverValueFloatNode.InnerText = altiverb.LowCrossoverValueFloat.ToString().Replace(",", ".");
        dictNode42.AppendChild(lowCrossoverValueFloatNode);
        parmsNode.AppendChild(dictNode42);

        XmlElement dictNode43 = doc.CreateElement("dict");
        XmlElement highCrossoverNode = doc.CreateElement("s_Name");
        highCrossoverNode.InnerText = "High Crossover";
        dictNode43.AppendChild(highCrossoverNode);
        XmlElement highCrossoverIDNode = doc.CreateElement("s_ID");
        highCrossoverIDNode.InnerText = altiverb.HighCrossoverID;
        dictNode43.AppendChild(highCrossoverIDNode);
        XmlElement highCrossoverValueFloatNode = doc.CreateElement("r_ValueFloat");
        highCrossoverValueFloatNode.InnerText = altiverb.HighCrossoverValueFloat.ToString().Replace(",", ".");
        dictNode43.AppendChild(highCrossoverValueFloatNode);
        parmsNode.AppendChild(dictNode43);

        XmlElement dictNode44 = doc.CreateElement("dict");
        XmlElement camAngleNode = doc.CreateElement("s_Name");
        camAngleNode.InnerText = "Cam Angle";
        dictNode44.AppendChild(camAngleNode);
        XmlElement camAngleIDNode = doc.CreateElement("s_ID");
        camAngleIDNode.InnerText = altiverb.CamAngleID;
        dictNode44.AppendChild(camAngleIDNode);
        XmlElement camAngleValueFloatNode = doc.CreateElement("r_ValueFloat");
        camAngleValueFloatNode.InnerText = altiverb.CamAngleValueFloat.ToString().Replace(",", ".");
        dictNode44.AppendChild(camAngleValueFloatNode);
        parmsNode.AppendChild(dictNode44);

        XmlElement dictNode45 = doc.CreateElement("dict");
        XmlElement camYNode = doc.CreateElement("s_Name");
        camYNode.InnerText = "Cam Y";
        dictNode45.AppendChild(camYNode);
        XmlElement camYIDNode = doc.CreateElement("s_ID");
        camYIDNode.InnerText = altiverb.CamYID;
        dictNode45.AppendChild(camYIDNode);
        XmlElement camYValueFloatNode = doc.CreateElement("r_ValueFloat");
        camYValueFloatNode.InnerText = altiverb.CamYValueFloat.ToString().Replace(",", ".");
        dictNode45.AppendChild(camYValueFloatNode);
        parmsNode.AppendChild(dictNode45);

        XmlElement dictNode46 = doc.CreateElement("dict");
        XmlElement camZNode = doc.CreateElement("s_Name");
        camZNode.InnerText = "Cam Z";
        dictNode46.AppendChild(camZNode);
        XmlElement camZIDNode = doc.CreateElement("s_ID");
        camZIDNode.InnerText = altiverb.CamZID;
        dictNode46.AppendChild(camZIDNode);
        XmlElement camZValueFloatNode = doc.CreateElement("r_ValueFloat");
        camZValueFloatNode.InnerText = altiverb.CamZValueFloat.ToString().Replace(",", ".");
        dictNode46.AppendChild(camZValueFloatNode);
        parmsNode.AppendChild(dictNode46);

        XmlElement dictNode47 = doc.CreateElement("dict");
        XmlElement camRHNode = doc.CreateElement("s_Name");
        camRHNode.InnerText = "Cam RH";
        dictNode47.AppendChild(camRHNode);
        XmlElement camRHIDNode = doc.CreateElement("s_ID");
        camRHIDNode.InnerText = altiverb.CamRHID;
        dictNode47.AppendChild(camRHIDNode);
        XmlElement camRHValueFloatNode = doc.CreateElement("r_ValueFloat");
        camRHValueFloatNode.InnerText = altiverb.CamRHValueFloat.ToString().Replace(",", ".");
        dictNode47.AppendChild(camRHValueFloatNode);
        parmsNode.AppendChild(dictNode47);

        XmlElement dictNode48 = doc.CreateElement("dict");
        XmlElement camRVNode = doc.CreateElement("s_Name");
        camRVNode.InnerText = "Cam RV";
        dictNode48.AppendChild(camRVNode);
        XmlElement camRVIDNode = doc.CreateElement("s_ID");
        camRVIDNode.InnerText = altiverb.CamRVID;
        dictNode48.AppendChild(camRVIDNode);
        XmlElement camRVValueFloatNode = doc.CreateElement("r_ValueFloat");
        camRVValueFloatNode.InnerText = altiverb.CamRVValueFloat.ToString().Replace(",", ".");
        dictNode48.AppendChild(camRVValueFloatNode);
        parmsNode.AppendChild(dictNode48);

        XmlElement dictNode49 = doc.CreateElement("dict");
        XmlElement scrollZoomNode = doc.CreateElement("s_Name");
        scrollZoomNode.InnerText = "Scroll Zoom";
        dictNode49.AppendChild(scrollZoomNode);
        XmlElement scrollZoomIDNode = doc.CreateElement("s_ID");
        scrollZoomIDNode.InnerText = altiverb.ScrollZoomID;
        dictNode49.AppendChild(scrollZoomIDNode);
        XmlElement scrollZoomValueFloatNode = doc.CreateElement("r_ValueFloat");
        scrollZoomValueFloatNode.InnerText = altiverb.ScrollZoomValueFloat.ToString().Replace(",", ".");
        dictNode49.AppendChild(scrollZoomValueFloatNode);
        parmsNode.AppendChild(dictNode49);

        XmlElement dictNode50 = doc.CreateElement("dict");
        XmlElement waveZoomNode = doc.CreateElement("s_Name");
        waveZoomNode.InnerText = "Wave Zoom";
        dictNode50.AppendChild(waveZoomNode);
        XmlElement waveZoomIDNode = doc.CreateElement("s_ID");
        waveZoomIDNode.InnerText = altiverb.WaveZoomID;
        dictNode50.AppendChild(waveZoomIDNode);
        XmlElement waveZoomValueFloatNode = doc.CreateElement("r_ValueFloat");
        waveZoomValueFloatNode.InnerText = altiverb.WaveZoomValueFloat.ToString().Replace(",", ".");
        dictNode50.AppendChild(waveZoomValueFloatNode);
        parmsNode.AppendChild(dictNode50);

        XmlElement dictNode51 = doc.CreateElement("dict");
        XmlElement waveOffsetNode = doc.CreateElement("s_Name");
        waveOffsetNode.InnerText = "Wave Offset";
        dictNode51.AppendChild(waveOffsetNode);
        XmlElement waveOffsetIDNode = doc.CreateElement("s_ID");
        waveOffsetIDNode.InnerText = altiverb.WaveOffsetID;
        dictNode51.AppendChild(waveOffsetIDNode);
        XmlElement waveOffsetValueFloatNode = doc.CreateElement("r_ValueFloat");
        waveOffsetValueFloatNode.InnerText = altiverb.WaveOffsetValueFloat.ToString().Replace(",", ".");
        dictNode51.AppendChild(waveOffsetValueFloatNode);
        parmsNode.AppendChild(dictNode51);

        XmlElement dictNode52 = doc.CreateElement("dict");
        XmlElement tabviewNode = doc.CreateElement("s_Name");
        tabviewNode.InnerText = "TabView";
        dictNode52.AppendChild(tabviewNode);
        XmlElement tabviewIDNode = doc.CreateElement("s_ID");
        tabviewIDNode.InnerText = altiverb.TabViewID;
        dictNode52.AppendChild(tabviewIDNode);
        XmlElement tabviewValueLongNode = doc.CreateElement("i_ValueLong");
        tabviewValueLongNode.InnerText = altiverb.TabViewValueLong.ToString();
        dictNode52.AppendChild(tabviewValueLongNode);
        parmsNode.AppendChild(dictNode52);

        XmlElement dictNode53 = doc.CreateElement("dict");
        XmlElement irscreenMouseModeNode = doc.CreateElement("s_Name");
        irscreenMouseModeNode.InnerText = "IRScreen Mouse Mode";
        dictNode53.AppendChild(irscreenMouseModeNode);
        XmlElement irscreenMouseModeIDNode = doc.CreateElement("s_ID");
        irscreenMouseModeIDNode.InnerText = altiverb.IRScreenMouseModeID;
        dictNode53.AppendChild(irscreenMouseModeIDNode);
        XmlElement irscreenMouseModeValueLongNode = doc.CreateElement("i_ValueLong");
        irscreenMouseModeValueLongNode.InnerText = altiverb.IRScreenMouseModeValueLong.ToString();
        dictNode53.AppendChild(irscreenMouseModeValueLongNode);
        parmsNode.AppendChild(dictNode53);

        XmlElement dictNode54 = doc.CreateElement("dict");
        XmlElement controlAdjustmentModeNode = doc.CreateElement("s_Name");
        controlAdjustmentModeNode.InnerText = "Control Adjustment Mode";
        dictNode54.AppendChild(controlAdjustmentModeNode);
        XmlElement controlAdjustmentModeIDNode = doc.CreateElement("s_ID");
        controlAdjustmentModeIDNode.InnerText = altiverb.ControlAdjustmentModeID;
        dictNode54.AppendChild(controlAdjustmentModeIDNode);
        XmlElement controlAdjustmentModeValueLongNode = doc.CreateElement("i_ValueLong");
        controlAdjustmentModeValueLongNode.InnerText = altiverb.ControlAdjustmentModeValueLong.ToString();
        dictNode54.AppendChild(controlAdjustmentModeValueLongNode);
        parmsNode.AppendChild(dictNode54);

        XmlElement dictNode55 = doc.CreateElement("dict");
        XmlElement decayOnNode = doc.CreateElement("s_Name");
        decayOnNode.InnerText = "Decay On";
        dictNode55.AppendChild(decayOnNode);
        XmlElement decayOnIDNode = doc.CreateElement("s_ID");
        decayOnIDNode.InnerText = altiverb.DecayOnID;
        dictNode55.AppendChild(decayOnIDNode);
        XmlElement decayOnValueLongNode = doc.CreateElement("i_ValueLong");
        decayOnValueLongNode.InnerText = altiverb.DecayOnValueLong.ToString();
        dictNode55.AppendChild(decayOnValueLongNode);
        parmsNode.AppendChild(dictNode55);

        XmlElement dictNode56 = doc.CreateElement("dict");
        XmlElement reverseNode = doc.CreateElement("s_Name");
        reverseNode.InnerText = "Reverse";
        dictNode56.AppendChild(reverseNode);
        XmlElement reverseIDNode = doc.CreateElement("s_ID");
        reverseIDNode.InnerText = altiverb.ReverseID;
        dictNode56.AppendChild(reverseIDNode);
        XmlElement reverseValueLongNode = doc.CreateElement("i_ValueLong");
        reverseValueLongNode.InnerText = altiverb.ReverseValueLong.ToString();
        dictNode56.AppendChild(reverseValueLongNode);
        parmsNode.AppendChild(dictNode56);

        XmlElement dictNode57 = doc.CreateElement("dict");
        XmlElement irLoadModeNode = doc.CreateElement("s_Name");
        irLoadModeNode.InnerText = "IR Load Mode";
        dictNode57.AppendChild(irLoadModeNode);
        XmlElement irLoadModeIDNode = doc.CreateElement("s_ID");
        irLoadModeIDNode.InnerText = altiverb.IRLoadModeID;
        dictNode57.AppendChild(irLoadModeIDNode);
        XmlElement irLoadModeValueLongNode = doc.CreateElement("i_ValueLong");
        irLoadModeValueLongNode.InnerText = altiverb.IRLoadModeValueLong.ToString();
        dictNode57.AppendChild(irLoadModeValueLongNode);
        parmsNode.AppendChild(dictNode57);

        XmlElement dictNode58 = doc.CreateElement("dict");
        XmlElement irLoadAsAuxNode = doc.CreateElement("s_Name");
        irLoadAsAuxNode.InnerText = "IR Load as aux";
        dictNode58.AppendChild(irLoadAsAuxNode);
        XmlElement irLoadAsAuxIDNode = doc.CreateElement("s_ID");
        irLoadAsAuxIDNode.InnerText = altiverb.IRLoadAsAuxID;
        dictNode58.AppendChild(irLoadAsAuxIDNode);
        XmlElement irLoadAsAuxValueLongNode = doc.CreateElement("i_ValueLong");
        irLoadAsAuxValueLongNode.InnerText = altiverb.IRLoadAsAuxValueLong.ToString();
        dictNode58.AppendChild(irLoadAsAuxValueLongNode);
        parmsNode.AppendChild(dictNode58);

        XmlElement dictNode59 = doc.CreateElement("dict");
        XmlElement selectedIRNode = doc.CreateElement("s_Name");
        selectedIRNode.InnerText = "Selected IR";
        dictNode59.AppendChild(selectedIRNode);
        XmlElement selectedIRIDNode = doc.CreateElement("s_ID");
        selectedIRIDNode.InnerText = altiverb.SelectedIRID;
        dictNode59.AppendChild(selectedIRIDNode);
        XmlElement selectedIRVersionNode = doc.CreateElement("i_version");
        selectedIRVersionNode.InnerText = altiverb.SelectedIRVersion.ToString();
        dictNode59.AppendChild(selectedIRVersionNode);
        XmlElement selectedIRIrpathNode = doc.CreateElement("s_irpath");
        selectedIRIrpathNode.InnerText = altiverb.SelectedIRIrpath;
        dictNode59.AppendChild(selectedIRIrpathNode);
        XmlElement selectedIRIridNode = doc.CreateElement("s_irid");
        selectedIRIridNode.InnerText = altiverb.SelectedIRIrid;
        dictNode59.AppendChild(selectedIRIridNode);
        parmsNode.AppendChild(dictNode59);

        XmlElement dictNode60 = doc.CreateElement("dict");
        XmlElement bypassNode = doc.CreateElement("s_Name");
        bypassNode.InnerText = "Bypass";
        dictNode60.AppendChild(bypassNode);
        XmlElement bypassIDNode = doc.CreateElement("s_ID");
        bypassIDNode.InnerText = altiverb.BypassID;
        dictNode60.AppendChild(bypassIDNode);
        XmlElement bypassValueLongNode = doc.CreateElement("i_ValueLong");
        bypassValueLongNode.InnerText = altiverb.BypassValueLong.ToString();
        dictNode60.AppendChild(bypassValueLongNode);
        parmsNode.AppendChild(dictNode60);

        XmlElement dictNode61 = doc.CreateElement("dict");
        XmlElement showWaveNode = doc.CreateElement("s_Name");
        showWaveNode.InnerText = "Show Wave";
        dictNode61.AppendChild(showWaveNode);
        XmlElement showWaveIDNode = doc.CreateElement("s_ID");
        showWaveIDNode.InnerText = altiverb.ShowWaveID;
        dictNode61.AppendChild(showWaveIDNode);
        XmlElement showWaveValueLongNode = doc.CreateElement("i_ValueLong");
        showWaveValueLongNode.InnerText = altiverb.ShowWaveValueLong.ToString();
        dictNode61.AppendChild(showWaveValueLongNode);
        parmsNode.AppendChild(dictNode61);

        XmlElement dictNode62 = doc.CreateElement("dict");
        XmlElement audioeaseversionNode = doc.CreateElement("s_Name");
        audioeaseversionNode.InnerText = "AudioEaseVersion";
        dictNode62.AppendChild(audioeaseversionNode);
        XmlElement audioeaseversionIDNode = doc.CreateElement("s_ID");
        audioeaseversionIDNode.InnerText = altiverb.AudioEaseVersionID;
        dictNode62.AppendChild(audioeaseversionIDNode);
        XmlElement audioeaseversionValueLongNode = doc.CreateElement("i_ValueLong");
        audioeaseversionValueLongNode.InnerText = altiverb.AudioEaseVersionValueLong.ToString();
        dictNode62.AppendChild(audioeaseversionValueLongNode);
        parmsNode.AppendChild(dictNode62);

        XmlElement dictNode63 = doc.CreateElement("dict");
        XmlElement irbrowserStatusNode = doc.CreateElement("s_Name");
        irbrowserStatusNode.InnerText = "IRBrowser Status";
        dictNode63.AppendChild(irbrowserStatusNode);
        XmlElement irbrowserStatusIDNode = doc.CreateElement("s_ID");
        irbrowserStatusIDNode.InnerText = altiverb.IRBrowserStatusID;
        dictNode63.AppendChild(irbrowserStatusIDNode);
        // Detected special case where previous node processed was not the last sibling even though this is a first sibling.
        // Append the whole browser-status-tree to the current dictNode
        XmlDocument xmlAltiverbFilesDoc = new XmlDocument();
        xmlAltiverbFilesDoc.Load(xmlAltiverbFilePath);
        XmlNode startNode = xmlAltiverbFilesDoc.DocumentElement;
        StringWriter stringWriter = new StringWriter();
        WriteIRBrowserStatus(startNode, startNode, 0, stringWriter, altiverb.SelectedIRIrpath);
        XmlDocument xImportDoc = new XmlDocument();
        xImportDoc.LoadXml(stringWriter.ToString());
        XmlNode xChildNode = doc.ImportNode(xImportDoc.DocumentElement, true);
        dictNode63.AppendChild(xChildNode);
        parmsNode.AppendChild(dictNode63);

        XmlElement dictNode64 = doc.CreateElement("dict");
        XmlElement displayModeNode = doc.CreateElement("s_Name");
        displayModeNode.InnerText = "Display Mode";
        dictNode64.AppendChild(displayModeNode);
        XmlElement displayModeIDNode = doc.CreateElement("s_ID");
        displayModeIDNode.InnerText = altiverb.DisplayModeID;
        dictNode64.AppendChild(displayModeIDNode);
        XmlElement displayModeValueLongNode = doc.CreateElement("i_ValueLong");
        displayModeValueLongNode.InnerText = altiverb.DisplayModeValueLong.ToString();
        dictNode64.AppendChild(displayModeValueLongNode);
        parmsNode.AppendChild(dictNode64);

        XmlElement dictNode65 = doc.CreateElement("dict");
        XmlElement directOnNode = doc.CreateElement("s_Name");
        directOnNode.InnerText = "Direct On";
        dictNode65.AppendChild(directOnNode);
        XmlElement directOnIDNode = doc.CreateElement("s_ID");
        directOnIDNode.InnerText = altiverb.DirectOnID;
        dictNode65.AppendChild(directOnIDNode);
        XmlElement directOnValueLongNode = doc.CreateElement("i_ValueLong");
        directOnValueLongNode.InnerText = altiverb.DirectOnValueLong.ToString();
        dictNode65.AppendChild(directOnValueLongNode);
        parmsNode.AppendChild(dictNode65);

        XmlElement dictNode66 = doc.CreateElement("dict");
        XmlElement tailOnNode = doc.CreateElement("s_Name");
        tailOnNode.InnerText = "Tail On";
        dictNode66.AppendChild(tailOnNode);
        XmlElement tailOnIDNode = doc.CreateElement("s_ID");
        tailOnIDNode.InnerText = altiverb.TailOnID;
        dictNode66.AppendChild(tailOnIDNode);
        XmlElement tailOnValueLongNode = doc.CreateElement("i_ValueLong");
        tailOnValueLongNode.InnerText = altiverb.TailOnValueLong.ToString();
        dictNode66.AppendChild(tailOnValueLongNode);
        parmsNode.AppendChild(dictNode66);

        //string xmlOutput = doc.OuterXml;
        //Console.WriteLine(xmlOutput);
        return doc;
    }
}

class Altiverb
{
    private string presetName; public string PresetName { get { return presetName; } set { presetName = value; } } // Original Value: s_Name=Preset Name
    private string presetNameID; public string PresetNameID { get { return presetNameID; } set { presetNameID = value; } } // Original Value: s_ID=aa50
    private int presetNameVersion; public int PresetNameVersion { get { return presetNameVersion; } set { presetNameVersion = value; } } // Original Value: i_version=2
    private int presetNameNeedssave; public int PresetNameNeedssave { get { return presetNameNeedssave; } set { presetNameNeedssave = value; } } // Original Value: i_needssave=1
    private int presetNameInpresetfolder; public int PresetNameInpresetfolder { get { return presetNameInpresetfolder; } set { presetNameInpresetfolder = value; } } // Original Value: i_inpresetfolder=0
    private string presetNamePresetpath; public string PresetNamePresetpath { get { return presetNamePresetpath; } set { presetNamePresetpath = value; } } // Original Value: s_presetpath=
    private string allChunks; public string AllChunks { get { return allChunks; } set { allChunks = value; } } // Original Value: s_Name=All Chunks
    private string allChunksID; public string AllChunksID { get { return allChunksID; } set { allChunksID = value; } } // Original Value: s_ID=aa60
    private int allChunksVersion; public int AllChunksVersion { get { return allChunksVersion; } set { allChunksVersion = value; } } // Original Value: i_version=2
    private string snapshot; public string Snapshot { get { return snapshot; } set { snapshot = value; } } // Original Value: s_Name=Snapshot
    private string snapshotID; public string SnapshotID { get { return snapshotID; } set { snapshotID = value; } } // Original Value: s_ID=aa65
    private int snapshotValueLong; public int SnapshotValueLong { get { return snapshotValueLong; } set { snapshotValueLong = value; } } // Original Value: i_ValueLong=0
    private string automationPresetIndex; public string AutomationPresetIndex { get { return automationPresetIndex; } set { automationPresetIndex = value; } } // Original Value: s_Name=Automation Preset Index
    private string automationPresetIndexID; public string AutomationPresetIndexID { get { return automationPresetIndexID; } set { automationPresetIndexID = value; } } // Original Value: s_ID=aa70
    private int automationPresetIndexValueLong; public int AutomationPresetIndexValueLong { get { return automationPresetIndexValueLong; } set { automationPresetIndexValueLong = value; } } // Original Value: i_ValueLong=0
    private string earlyReflectionsOn; public string EarlyReflectionsOn { get { return earlyReflectionsOn; } set { earlyReflectionsOn = value; } } // Original Value: s_Name=Early Reflections On
    private string earlyReflectionsOnID; public string EarlyReflectionsOnID { get { return earlyReflectionsOnID; } set { earlyReflectionsOnID = value; } } // Original Value: s_ID=EROn
    private int earlyReflectionsOnValueLong; public int EarlyReflectionsOnValueLong { get { return earlyReflectionsOnValueLong; } set { earlyReflectionsOnValueLong = value; } } // Original Value: i_ValueLong=1
    private string directGain; public string DirectGain { get { return directGain; } set { directGain = value; } } // Original Value: s_Name=Direct Gain
    private string directGainID; public string DirectGainID { get { return directGainID; } set { directGainID = value; } } // Original Value: s_ID=DrLv
    private float directGainValueFloat; public float DirectGainValueFloat { get { return directGainValueFloat; } set { directGainValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string directColor; public string DirectColor { get { return directColor; } set { directColor = value; } } // Original Value: s_Name=Direct Color
    private string directColorID; public string DirectColorID { get { return directColorID; } set { directColorID = value; } } // Original Value: s_ID=DrCl
    private float directColorValueFloat; public float DirectColorValueFloat { get { return directColorValueFloat; } set { directColorValueFloat = value; } } // Original Value: r_ValueFloat=1
    private string earlyrefGain; public string EarlyRefGain { get { return earlyrefGain; } set { earlyrefGain = value; } } // Original Value: s_Name=EarlyRef Gain
    private string earlyrefGainID; public string EarlyRefGainID { get { return earlyrefGainID; } set { earlyrefGainID = value; } } // Original Value: s_ID=ERLv
    private float earlyrefGainValueFloat; public float EarlyRefGainValueFloat { get { return earlyrefGainValueFloat; } set { earlyrefGainValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string earlyrefDelay; public string EarlyRefDelay { get { return earlyrefDelay; } set { earlyrefDelay = value; } } // Original Value: s_Name=EarlyRef Delay
    private string earlyrefDelayID; public string EarlyRefDelayID { get { return earlyrefDelayID; } set { earlyrefDelayID = value; } } // Original Value: s_ID=ERDl
    private float earlyrefDelayValueFloat; public float EarlyRefDelayValueFloat { get { return earlyrefDelayValueFloat; } set { earlyrefDelayValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string tailGain; public string TailGain { get { return tailGain; } set { tailGain = value; } } // Original Value: s_Name=Tail Gain
    private string tailGainID; public string TailGainID { get { return tailGainID; } set { tailGainID = value; } } // Original Value: s_ID=TlLv
    private float tailGainValueFloat; public float TailGainValueFloat { get { return tailGainValueFloat; } set { tailGainValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string tailDelay; public string TailDelay { get { return tailDelay; } set { tailDelay = value; } } // Original Value: s_Name=Tail Delay
    private string tailDelayID; public string TailDelayID { get { return tailDelayID; } set { tailDelayID = value; } } // Original Value: s_ID=TlDl
    private float tailDelayValueFloat; public float TailDelayValueFloat { get { return tailDelayValueFloat; } set { tailDelayValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string reverbTime; public string ReverbTime { get { return reverbTime; } set { reverbTime = value; } } // Original Value: s_Name=Reverb Time
    private string reverbTimeID; public string ReverbTimeID { get { return reverbTimeID; } set { reverbTimeID = value; } } // Original Value: s_ID=DecM
    private float reverbTimeValueFloat; public float ReverbTimeValueFloat { get { return reverbTimeValueFloat; } set { reverbTimeValueFloat = value; } } // Original Value: r_ValueFloat=1
    private string lowDamp; public string LowDamp { get { return lowDamp; } set { lowDamp = value; } } // Original Value: s_Name=Low Damp
    private string lowDampID; public string LowDampID { get { return lowDampID; } set { lowDampID = value; } } // Original Value: s_ID=Dec1
    private float lowDampValueFloat; public float LowDampValueFloat { get { return lowDampValueFloat; } set { lowDampValueFloat = value; } } // Original Value: r_ValueFloat=1
    private string midDamp; public string MidDamp { get { return midDamp; } set { midDamp = value; } } // Original Value: s_Name=Mid Damp
    private string midDampID; public string MidDampID { get { return midDampID; } set { midDampID = value; } } // Original Value: s_ID=Dec2
    private float midDampValueFloat; public float MidDampValueFloat { get { return midDampValueFloat; } set { midDampValueFloat = value; } } // Original Value: r_ValueFloat=1
    private string highDamp; public string HighDamp { get { return highDamp; } set { highDamp = value; } } // Original Value: s_Name=High Damp
    private string highDampID; public string HighDampID { get { return highDampID; } set { highDampID = value; } } // Original Value: s_ID=Dec3
    private float highDampValueFloat; public float HighDampValueFloat { get { return highDampValueFloat; } set { highDampValueFloat = value; } } // Original Value: r_ValueFloat=1
    private string eqBassLevel; public string EQBassLevel { get { return eqBassLevel; } set { eqBassLevel = value; } } // Original Value: s_Name=EQ Bass Level
    private string eqBassLevelID; public string EQBassLevelID { get { return eqBassLevelID; } set { eqBassLevelID = value; } } // Original Value: s_ID=EQBl
    private float eqBassLevelValueFloat; public float EQBassLevelValueFloat { get { return eqBassLevelValueFloat; } set { eqBassLevelValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string eqTrebleLevel; public string EQTrebleLevel { get { return eqTrebleLevel; } set { eqTrebleLevel = value; } } // Original Value: s_Name=EQ Treble Level
    private string eqTrebleLevelID; public string EQTrebleLevelID { get { return eqTrebleLevelID; } set { eqTrebleLevelID = value; } } // Original Value: s_ID=EQTb
    private float eqTrebleLevelValueFloat; public float EQTrebleLevelValueFloat { get { return eqTrebleLevelValueFloat; } set { eqTrebleLevelValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string eqLoFreq; public string EQLoFreq { get { return eqLoFreq; } set { eqLoFreq = value; } } // Original Value: s_Name=EQ Lo Freq
    private string eqLoFreqID; public string EQLoFreqID { get { return eqLoFreqID; } set { eqLoFreqID = value; } } // Original Value: s_ID=EQ1f
    private float eqLoFreqValueFloat; public float EQLoFreqValueFloat { get { return eqLoFreqValueFloat; } set { eqLoFreqValueFloat = value; } } // Original Value: r_ValueFloat=120
    private string eqLoQ; public string EQLoQ { get { return eqLoQ; } set { eqLoQ = value; } } // Original Value: s_Name=EQ Lo Q
    private string eqLoQID; public string EQLoQID { get { return eqLoQID; } set { eqLoQID = value; } } // Original Value: s_ID=EQ1q
    private float eqLoQValueFloat; public float EQLoQValueFloat { get { return eqLoQValueFloat; } set { eqLoQValueFloat = value; } } // Original Value: r_ValueFloat=1.25
    private string eqLoGain; public string EQLoGain { get { return eqLoGain; } set { eqLoGain = value; } } // Original Value: s_Name=EQ Lo Gain
    private string eqLoGainID; public string EQLoGainID { get { return eqLoGainID; } set { eqLoGainID = value; } } // Original Value: s_ID=EQ1g
    private float eqLoGainValueFloat; public float EQLoGainValueFloat { get { return eqLoGainValueFloat; } set { eqLoGainValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string eqHiFreq; public string EQHiFreq { get { return eqHiFreq; } set { eqHiFreq = value; } } // Original Value: s_Name=EQ Hi Freq
    private string eqHiFreqID; public string EQHiFreqID { get { return eqHiFreqID; } set { eqHiFreqID = value; } } // Original Value: s_ID=EQ2f
    private float eqHiFreqValueFloat; public float EQHiFreqValueFloat { get { return eqHiFreqValueFloat; } set { eqHiFreqValueFloat = value; } } // Original Value: r_ValueFloat=2000
    private string eqHiQ; public string EQHiQ { get { return eqHiQ; } set { eqHiQ = value; } } // Original Value: s_Name=EQ Hi Q
    private string eqHiQID; public string EQHiQID { get { return eqHiQID; } set { eqHiQID = value; } } // Original Value: s_ID=EQ2q
    private float eqHiQValueFloat; public float EQHiQValueFloat { get { return eqHiQValueFloat; } set { eqHiQValueFloat = value; } } // Original Value: r_ValueFloat=1.25
    private string eqHiGain; public string EQHiGain { get { return eqHiGain; } set { eqHiGain = value; } } // Original Value: s_Name=EQ Hi Gain
    private string eqHiGainID; public string EQHiGainID { get { return eqHiGainID; } set { eqHiGainID = value; } } // Original Value: s_ID=EQ2g
    private float eqHiGainValueFloat; public float EQHiGainValueFloat { get { return eqHiGainValueFloat; } set { eqHiGainValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string masterInLevel; public string MasterInLevel { get { return masterInLevel; } set { masterInLevel = value; } } // Original Value: s_Name=Master In Level
    private string masterInLevelID; public string MasterInLevelID { get { return masterInLevelID; } set { masterInLevelID = value; } } // Original Value: s_ID=MsIn
    private float masterInLevelValueFloat; public float MasterInLevelValueFloat { get { return masterInLevelValueFloat; } set { masterInLevelValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string masterOutLevel; public string MasterOutLevel { get { return masterOutLevel; } set { masterOutLevel = value; } } // Original Value: s_Name=Master Out Level
    private string masterOutLevelID; public string MasterOutLevelID { get { return masterOutLevelID; } set { masterOutLevelID = value; } } // Original Value: s_ID=MsOt
    private float masterOutLevelValueFloat; public float MasterOutLevelValueFloat { get { return masterOutLevelValueFloat; } set { masterOutLevelValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string masterFrontLevel; public string MasterFrontLevel { get { return masterFrontLevel; } set { masterFrontLevel = value; } } // Original Value: s_Name=Master Front Level
    private string masterFrontLevelID; public string MasterFrontLevelID { get { return masterFrontLevelID; } set { masterFrontLevelID = value; } } // Original Value: s_ID=FrLv
    private float masterFrontLevelValueFloat; public float MasterFrontLevelValueFloat { get { return masterFrontLevelValueFloat; } set { masterFrontLevelValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string masterRearLevel; public string MasterRearLevel { get { return masterRearLevel; } set { masterRearLevel = value; } } // Original Value: s_Name=Master Rear Level
    private string masterRearLevelID; public string MasterRearLevelID { get { return masterRearLevelID; } set { masterRearLevelID = value; } } // Original Value: s_ID=ReLv
    private float masterRearLevelValueFloat; public float MasterRearLevelValueFloat { get { return masterRearLevelValueFloat; } set { masterRearLevelValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string dryWetMix; public string DryWetMix { get { return dryWetMix; } set { dryWetMix = value; } } // Original Value: s_Name=Dry/Wet Mix
    private string dryWetMixID; public string DryWetMixID { get { return dryWetMixID; } set { dryWetMixID = value; } } // Original Value: s_ID=DrWt
    private float dryWetMixValueFloat; public float DryWetMixValueFloat { get { return dryWetMixValueFloat; } set { dryWetMixValueFloat = value; } } // Original Value: r_ValueFloat=1
    private string centerBleed; public string CenterBleed { get { return centerBleed; } set { centerBleed = value; } } // Original Value: s_Name=Center Bleed
    private string centerBleedID; public string CenterBleedID { get { return centerBleedID; } set { centerBleedID = value; } } // Original Value: s_ID=CtBl
    private float centerBleedValueFloat; public float CenterBleedValueFloat { get { return centerBleedValueFloat; } set { centerBleedValueFloat = value; } } // Original Value: r_ValueFloat=-144
    private string lfeBleed; public string LfeBleed { get { return lfeBleed; } set { lfeBleed = value; } } // Original Value: s_Name=Lfe Bleed
    private string lfeBleedID; public string LfeBleedID { get { return lfeBleedID; } set { lfeBleedID = value; } } // Original Value: s_ID=LfBl
    private float lfeBleedValueFloat; public float LfeBleedValueFloat { get { return lfeBleedValueFloat; } set { lfeBleedValueFloat = value; } } // Original Value: r_ValueFloat=-144
    private string sampleLevel; public string SampleLevel { get { return sampleLevel; } set { sampleLevel = value; } } // Original Value: s_Name=Sample Level
    private string sampleLevelID; public string SampleLevelID { get { return sampleLevelID; } set { sampleLevelID = value; } } // Original Value: s_ID=SmVl
    private float sampleLevelValueFloat; public float SampleLevelValueFloat { get { return sampleLevelValueFloat; } set { sampleLevelValueFloat = value; } } // Original Value: r_ValueFloat=-10
    private string speakerLeftX; public string SpeakerLeftX { get { return speakerLeftX; } set { speakerLeftX = value; } } // Original Value: s_Name=Speaker Left X
    private string speakerLeftXID; public string SpeakerLeftXID { get { return speakerLeftXID; } set { speakerLeftXID = value; } } // Original Value: s_ID=SpLx
    private float speakerLeftXValueFloat; public float SpeakerLeftXValueFloat { get { return speakerLeftXValueFloat; } set { speakerLeftXValueFloat = value; } } // Original Value: r_ValueFloat=-1
    private string speakerRightX; public string SpeakerRightX { get { return speakerRightX; } set { speakerRightX = value; } } // Original Value: s_Name=Speaker Right X
    private string speakerRightXID; public string SpeakerRightXID { get { return speakerRightXID; } set { speakerRightXID = value; } } // Original Value: s_ID=SpRx
    private float speakerRightXValueFloat; public float SpeakerRightXValueFloat { get { return speakerRightXValueFloat; } set { speakerRightXValueFloat = value; } } // Original Value: r_ValueFloat=1
    private string speakerCenterX; public string SpeakerCenterX { get { return speakerCenterX; } set { speakerCenterX = value; } } // Original Value: s_Name=Speaker Center X
    private string speakerCenterXID; public string SpeakerCenterXID { get { return speakerCenterXID; } set { speakerCenterXID = value; } } // Original Value: s_ID=SpCx
    private float speakerCenterXValueFloat; public float SpeakerCenterXValueFloat { get { return speakerCenterXValueFloat; } set { speakerCenterXValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string speakerY; public string SpeakerY { get { return speakerY; } set { speakerY = value; } } // Original Value: s_Name=Speaker Y
    private string speakerYID; public string SpeakerYID { get { return speakerYID; } set { speakerYID = value; } } // Original Value: s_ID=Spky
    private float speakerYValueFloat; public float SpeakerYValueFloat { get { return speakerYValueFloat; } set { speakerYValueFloat = value; } } // Original Value: r_ValueFloat=1
    private string eqOn; public string EQOn { get { return eqOn; } set { eqOn = value; } } // Original Value: s_Name=EQ On
    private string eqOnID; public string EQOnID { get { return eqOnID; } set { eqOnID = value; } } // Original Value: s_ID=EqOn
    private int eqOnValueLong; public int EQOnValueLong { get { return eqOnValueLong; } set { eqOnValueLong = value; } } // Original Value: i_ValueLong=1
    private string size; public string Size { get { return size; } set { size = value; } } // Original Value: s_Name=Size
    private string sizeID; public string SizeID { get { return sizeID; } set { sizeID = value; } } // Original Value: s_ID=PtSh
    private float sizeValueFloat; public float SizeValueFloat { get { return sizeValueFloat; } set { sizeValueFloat = value; } } // Original Value: r_ValueFloat=100
    private string tailCut; public string TailCut { get { return tailCut; } set { tailCut = value; } } // Original Value: s_Name=Tail Cut
    private string tailCutID; public string TailCutID { get { return tailCutID; } set { tailCutID = value; } } // Original Value: s_ID=Endd
    private float tailCutValueFloat; public float TailCutValueFloat { get { return tailCutValueFloat; } set { tailCutValueFloat = value; } } // Original Value: r_ValueFloat=-120
    private string latencyMode; public string LatencyMode { get { return latencyMode; } set { latencyMode = value; } } // Original Value: s_Name=Latency Mode
    private string latencyModeID; public string LatencyModeID { get { return latencyModeID; } set { latencyModeID = value; } } // Original Value: s_ID=latn
    private int latencyModeValueLong; public int LatencyModeValueLong { get { return latencyModeValueLong; } set { latencyModeValueLong = value; } } // Original Value: i_ValueLong=1
    private string stagePositionsOn; public string StagePositionsOn { get { return stagePositionsOn; } set { stagePositionsOn = value; } } // Original Value: s_Name=Stage Positions On
    private string stagePositionsOnID; public string StagePositionsOnID { get { return stagePositionsOnID; } set { stagePositionsOnID = value; } } // Original Value: s_ID=SPOn
    private int stagePositionsOnValueLong; public int StagePositionsOnValueLong { get { return stagePositionsOnValueLong; } set { stagePositionsOnValueLong = value; } } // Original Value: i_ValueLong=0
    private string linkEdit; public string LinkEdit { get { return linkEdit; } set { linkEdit = value; } } // Original Value: s_Name=Link Edit
    private string linkEditID; public string LinkEditID { get { return linkEditID; } set { linkEditID = value; } } // Original Value: s_ID=SPMr
    private int linkEditValueLong; public int LinkEditValueLong { get { return linkEditValueLong; } set { linkEditValueLong = value; } } // Original Value: i_ValueLong=1
    private string lowCrossover; public string LowCrossover { get { return lowCrossover; } set { lowCrossover = value; } } // Original Value: s_Name=Low Crossover
    private string lowCrossoverID; public string LowCrossoverID { get { return lowCrossoverID; } set { lowCrossoverID = value; } } // Original Value: s_ID=DCr1
    private float lowCrossoverValueFloat; public float LowCrossoverValueFloat { get { return lowCrossoverValueFloat; } set { lowCrossoverValueFloat = value; } } // Original Value: r_ValueFloat=320
    private string highCrossover; public string HighCrossover { get { return highCrossover; } set { highCrossover = value; } } // Original Value: s_Name=High Crossover
    private string highCrossoverID; public string HighCrossoverID { get { return highCrossoverID; } set { highCrossoverID = value; } } // Original Value: s_ID=DCr2
    private float highCrossoverValueFloat; public float HighCrossoverValueFloat { get { return highCrossoverValueFloat; } set { highCrossoverValueFloat = value; } } // Original Value: r_ValueFloat=2400
    private string camAngle; public string CamAngle { get { return camAngle; } set { camAngle = value; } } // Original Value: s_Name=Cam Angle
    private string camAngleID; public string CamAngleID { get { return camAngleID; } set { camAngleID = value; } } // Original Value: s_ID=CmAg
    private float camAngleValueFloat; public float CamAngleValueFloat { get { return camAngleValueFloat; } set { camAngleValueFloat = value; } } // Original Value: r_ValueFloat=29.1094
    private string camY; public string CamY { get { return camY; } set { camY = value; } } // Original Value: s_Name=Cam Y
    private string camYID; public string CamYID { get { return camYID; } set { camYID = value; } } // Original Value: s_ID=CmPy
    private float camYValueFloat; public float CamYValueFloat { get { return camYValueFloat; } set { camYValueFloat = value; } } // Original Value: r_ValueFloat=0.14
    private string camZ; public string CamZ { get { return camZ; } set { camZ = value; } } // Original Value: s_Name=Cam Z
    private string camZID; public string CamZID { get { return camZID; } set { camZID = value; } } // Original Value: s_ID=CmPz
    private float camZValueFloat; public float CamZValueFloat { get { return camZValueFloat; } set { camZValueFloat = value; } } // Original Value: r_ValueFloat=-0.2
    private string camRH; public string CamRH { get { return camRH; } set { camRH = value; } } // Original Value: s_Name=Cam RH
    private string camRHID; public string CamRHID { get { return camRHID; } set { camRHID = value; } } // Original Value: s_ID=CmRh
    private float camRHValueFloat; public float CamRHValueFloat { get { return camRHValueFloat; } set { camRHValueFloat = value; } } // Original Value: r_ValueFloat=-34.2
    private string camRV; public string CamRV { get { return camRV; } set { camRV = value; } } // Original Value: s_Name=Cam RV
    private string camRVID; public string CamRVID { get { return camRVID; } set { camRVID = value; } } // Original Value: s_ID=CmRv
    private float camRVValueFloat; public float CamRVValueFloat { get { return camRVValueFloat; } set { camRVValueFloat = value; } } // Original Value: r_ValueFloat=12.7333
    private string scrollZoom; public string ScrollZoom { get { return scrollZoom; } set { scrollZoom = value; } } // Original Value: s_Name=Scroll Zoom
    private string scrollZoomID; public string ScrollZoomID { get { return scrollZoomID; } set { scrollZoomID = value; } } // Original Value: s_ID=CmZm
    private float scrollZoomValueFloat; public float ScrollZoomValueFloat { get { return scrollZoomValueFloat; } set { scrollZoomValueFloat = value; } } // Original Value: r_ValueFloat=1
    private string waveZoom; public string WaveZoom { get { return waveZoom; } set { waveZoom = value; } } // Original Value: s_Name=Wave Zoom
    private string waveZoomID; public string WaveZoomID { get { return waveZoomID; } set { waveZoomID = value; } } // Original Value: s_ID=wcZm
    private float waveZoomValueFloat; public float WaveZoomValueFloat { get { return waveZoomValueFloat; } set { waveZoomValueFloat = value; } } // Original Value: r_ValueFloat=1
    private string waveOffset; public string WaveOffset { get { return waveOffset; } set { waveOffset = value; } } // Original Value: s_Name=Wave Offset
    private string waveOffsetID; public string WaveOffsetID { get { return waveOffsetID; } set { waveOffsetID = value; } } // Original Value: s_ID=wcOs
    private float waveOffsetValueFloat; public float WaveOffsetValueFloat { get { return waveOffsetValueFloat; } set { waveOffsetValueFloat = value; } } // Original Value: r_ValueFloat=0
    private string tabview; public string TabView { get { return tabview; } set { tabview = value; } } // Original Value: s_Name=TabView
    private string tabviewID; public string TabViewID { get { return tabviewID; } set { tabviewID = value; } } // Original Value: s_ID=TbVw
    private int tabviewValueLong; public int TabViewValueLong { get { return tabviewValueLong; } set { tabviewValueLong = value; } } // Original Value: i_ValueLong=0
    private string irscreenMouseMode; public string IRScreenMouseMode { get { return irscreenMouseMode; } set { irscreenMouseMode = value; } } // Original Value: s_Name=IRScreen Mouse Mode
    private string irscreenMouseModeID; public string IRScreenMouseModeID { get { return irscreenMouseModeID; } set { irscreenMouseModeID = value; } } // Original Value: s_ID=MsMd
    private int irscreenMouseModeValueLong; public int IRScreenMouseModeValueLong { get { return irscreenMouseModeValueLong; } set { irscreenMouseModeValueLong = value; } } // Original Value: i_ValueLong=0
    private string controlAdjustmentMode; public string ControlAdjustmentMode { get { return controlAdjustmentMode; } set { controlAdjustmentMode = value; } } // Original Value: s_Name=Control Adjustment Mode
    private string controlAdjustmentModeID; public string ControlAdjustmentModeID { get { return controlAdjustmentModeID; } set { controlAdjustmentModeID = value; } } // Original Value: s_ID=ctla
    private int controlAdjustmentModeValueLong; public int ControlAdjustmentModeValueLong { get { return controlAdjustmentModeValueLong; } set { controlAdjustmentModeValueLong = value; } } // Original Value: i_ValueLong=0
    private string decayOn; public string DecayOn { get { return decayOn; } set { decayOn = value; } } // Original Value: s_Name=Decay On
    private string decayOnID; public string DecayOnID { get { return decayOnID; } set { decayOnID = value; } } // Original Value: s_ID=DcOn
    private int decayOnValueLong; public int DecayOnValueLong { get { return decayOnValueLong; } set { decayOnValueLong = value; } } // Original Value: i_ValueLong=0
    private string reverse; public string Reverse { get { return reverse; } set { reverse = value; } } // Original Value: s_Name=Reverse
    private string reverseID; public string ReverseID { get { return reverseID; } set { reverseID = value; } } // Original Value: s_ID=Revs
    private int reverseValueLong; public int ReverseValueLong { get { return reverseValueLong; } set { reverseValueLong = value; } } // Original Value: i_ValueLong=0
    private string irLoadMode; public string IRLoadMode { get { return irLoadMode; } set { irLoadMode = value; } } // Original Value: s_Name=IR Load Mode
    private string irLoadModeID; public string IRLoadModeID { get { return irLoadModeID; } set { irLoadModeID = value; } } // Original Value: s_ID=irld
    private int irLoadModeValueLong; public int IRLoadModeValueLong { get { return irLoadModeValueLong; } set { irLoadModeValueLong = value; } } // Original Value: i_ValueLong=0
    private string irLoadAsAux; public string IRLoadAsAux { get { return irLoadAsAux; } set { irLoadAsAux = value; } } // Original Value: s_Name=IR Load as aux
    private string irLoadAsAuxID; public string IRLoadAsAuxID { get { return irLoadAsAuxID; } set { irLoadAsAuxID = value; } } // Original Value: s_ID=irlx
    private int irLoadAsAuxValueLong; public int IRLoadAsAuxValueLong { get { return irLoadAsAuxValueLong; } set { irLoadAsAuxValueLong = value; } } // Original Value: i_ValueLong=0
    private string selectedIR; public string SelectedIR { get { return selectedIR; } set { selectedIR = value; } } // Original Value: s_Name=Selected IR
    private string selectedIRID; public string SelectedIRID { get { return selectedIRID; } set { selectedIRID = value; } } // Original Value: s_ID=SlIR
    private int selectedIRVersion; public int SelectedIRVersion { get { return selectedIRVersion; } set { selectedIRVersion = value; } } // Original Value: i_version=2
    private string selectedIRIrpath; public string SelectedIRIrpath { get { return selectedIRIrpath; } set { selectedIRIrpath = value; } } // Original Value: s_irpath=Operas & Theaters/Royal Opera Stockholm/Stereo to stereo 09m70
    private string selectedIRIrid; public string SelectedIRIrid { get { return selectedIRIrid; } set { selectedIRIrid = value; } } // Original Value: s_irid=000-000-000-000-000-000
    private string bypass; public string Bypass { get { return bypass; } set { bypass = value; } } // Original Value: s_Name=Bypass
    private string bypassID; public string BypassID { get { return bypassID; } set { bypassID = value; } } // Original Value: s_ID=byps
    private int bypassValueLong; public int BypassValueLong { get { return bypassValueLong; } set { bypassValueLong = value; } } // Original Value: i_ValueLong=0
    private string showWave; public string ShowWave { get { return showWave; } set { showWave = value; } } // Original Value: s_Name=Show Wave
    private string showWaveID; public string ShowWaveID { get { return showWaveID; } set { showWaveID = value; } } // Original Value: s_ID=irsh
    private int showWaveValueLong; public int ShowWaveValueLong { get { return showWaveValueLong; } set { showWaveValueLong = value; } } // Original Value: i_ValueLong=2
    private string audioeaseversion; public string AudioEaseVersion { get { return audioeaseversion; } set { audioeaseversion = value; } } // Original Value: s_Name=AudioEaseVersion
    private string audioeaseversionID; public string AudioEaseVersionID { get { return audioeaseversionID; } set { audioeaseversionID = value; } } // Original Value: s_ID=vers
    private int audioeaseversionValueLong; public int AudioEaseVersionValueLong { get { return audioeaseversionValueLong; } set { audioeaseversionValueLong = value; } } // Original Value: i_ValueLong=1
    private string irbrowserStatus; public string IRBrowserStatus { get { return irbrowserStatus; } set { irbrowserStatus = value; } } // Original Value: s_Name=IRBrowser Status
    private string irbrowserStatusID; public string IRBrowserStatusID { get { return irbrowserStatusID; } set { irbrowserStatusID = value; } } // Original Value: s_ID=zz50
                                                                                                                                               // Detected special case where previous node processed was not the last sibling even though this is a first sibling.
    private string displayMode; public string DisplayMode { get { return displayMode; } set { displayMode = value; } } // Original Value: s_Name=Display Mode
    private string displayModeID; public string DisplayModeID { get { return displayModeID; } set { displayModeID = value; } } // Original Value: s_ID=DsMd
    private int displayModeValueLong; public int DisplayModeValueLong { get { return displayModeValueLong; } set { displayModeValueLong = value; } } // Original Value: i_ValueLong=0
    private string directOn; public string DirectOn { get { return directOn; } set { directOn = value; } } // Original Value: s_Name=Direct On
    private string directOnID; public string DirectOnID { get { return directOnID; } set { directOnID = value; } } // Original Value: s_ID=DrOn
    private int directOnValueLong; public int DirectOnValueLong { get { return directOnValueLong; } set { directOnValueLong = value; } } // Original Value: i_ValueLong=1
    private string tailOn; public string TailOn { get { return tailOn; } set { tailOn = value; } } // Original Value: s_Name=Tail On
    private string tailOnID; public string TailOnID { get { return tailOnID; } set { tailOnID = value; } } // Original Value: s_ID=TlOn
    private int tailOnValueLong; public int TailOnValueLong { get { return tailOnValueLong; } set { tailOnValueLong = value; } } // Original Value: i_ValueLong=1
}

class FileUtils
{
    public static byte[] readByteArrayFromFile(string fileName)
    {
        byte[] buffer = null;

        if (null == fileName)
        {
            return buffer;
        }

        try
        {
            // Open file for reading
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            // attach filestream to binary reader
            BinaryReader br = new BinaryReader(fs);

            // get total byte length of the file
            long totalBytes = new FileInfo(fileName).Length;

            // read entire file into buffer
            buffer = br.ReadBytes((Int32)totalBytes);

            // close file reader
            fs.Close();
            fs.Dispose();
            br.Close();

        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.ToString());
        }

        return buffer;
    }

    public static bool writeByteArrayToFile(byte[] buff, string fileName)
    {
        bool response = false;

        if (null == buff || buff.Length == 0)
        {
            return response;
        }

        try
        {
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(buff);
            bw.Close();
            response = true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.ToString());
        }

        return response;
    }
}

/*
	Search a directory for files that contain a specific text
	use fileNameFilter's like "*.iri"
*/
public class SearchFile
{
    public static string[] SearchForText(string directory, string fileNameFilter, string regMatch)
    {
        List<string> foundList = new List<string>();
        string[] iriFilePaths = Directory.GetFiles(directory, fileNameFilter, SearchOption.AllDirectories);

        foreach (string filePath in iriFilePaths)
        {
            StreamReader testTxt = new StreamReader(filePath);
            string allRead = testTxt.ReadToEnd(); // Reads the whole text file to the end
            testTxt.Close(); // Closes the text file after it is fully read.					
            if (Regex.IsMatch(allRead, regMatch))
            {
                foundList.Add(filePath);
                //Console.WriteLine("Found '{0}' in '{1}'", regMatch, filePath);
            }
        }
        return foundList.ToArray();
    }
}