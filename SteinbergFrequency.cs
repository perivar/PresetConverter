using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using CommonUtils;

namespace AbletonLiveConverter
{
    public class SteinbergFrequency : VstPreset
    {
        // cannot use Enums with doubles, struct works
        public struct BandMode1And8
        {
            public const double Cut6 = 0.00;
            public const double Cut12 = 1.00;
            public const double Cut24 = 2.00;
            public const double Cut48 = 3.00;
            public const double Cut96 = 4.00;
            public const double LowShelf = 5.00;
            public const double Peak = 6.00;
            public const double HighShelf = 7.00;
            public const double Notch = 8.00;
        }

        public struct BandMode2To7
        {
            public const double LowShelf = 0.00;
            public const double Peak = 1.00;
            public const double HighShelf = 2.00;
            public const double Notch = 3.00;
        }

        public SteinbergFrequency()
        {
            Vst3ID = VstPreset.VstIDs.Frequency;
            SetParameters();
            SetXml();
        }

        public void Write(string fileName)
        {
            var br = new BinaryFile(fileName, BinaryFile.ByteOrder.LittleEndian, true);

            // Write file header
            br.Write("VST3");

            // Write version
            br.Write((UInt32)1);

            // Write VST3 ID
            br.Write(this.Vst3ID);

            // Write listPos
            UInt32 listPos = 19664;
            br.Write(listPos);

            // Write unknown value
            br.Write((UInt32)0);

            // Write data chunk length. i.e. total length minus 4 ('VST3')
            UInt32 chunkID = 19732 - 4; // 19728
            br.Write(chunkID);

            // write parameters
            foreach (var parameter in this.Parameters.Values)
            {
                var paramName = parameter.Name.PadRight(128, '\0').Substring(0, 128);
                br.Write(paramName);
                br.Write(parameter.Number);
                br.Write(parameter.Value);
            }

            // The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF.
            var xmlBytes = Encoding.UTF8.GetBytes(this.Xml);
            var xmlBytesBOM = Encoding.UTF8.GetPreamble().Concat(xmlBytes).ToArray();
            br.Write(xmlBytesBOM);
            br.Write("\r\n");

            // write LIST and 4 bytes
            br.Write("List");
            br.Write((UInt32)3);

            // write COMP and 16 bytes
            br.Write("Comp");
            br.Write((UInt64)48); // parameter data start position
            br.Write((UInt64)19184); // byte length from parameter data start position up until xml data

            // write Cont and 16 bytes
            br.Write("Cont");
            br.Write((UInt64)19232); // xml start position
            br.Write((UInt64)0);// ?

            // write Info and 16 bytes
            br.Write("Info");
            br.Write((UInt64)19232); // xml start position
            br.Write((UInt64)xmlBytesBOM.Length); // byte length of xml data

            br.Close();
        }

        private void SetParameters()
        {
            for (int i = 1; i <= 8; i++)
            {
                SetFrequencyBandParameters(i);
            }

            SetFrequencyPostParameters();
        }

        private void SetFrequencyBandParameters(int bandNumber)
        {
            uint increment = (uint)bandNumber - 1;
            AddParameterToDictionary(String.Format("equalizerAon{0}", bandNumber), 100 + increment, 1.00);
            AddParameterToDictionary(String.Format("equalizerAgain{0}", bandNumber), 108 + increment, 0.00);
            AddParameterToDictionary(String.Format("equalizerAfreq{0}", bandNumber), 116 + increment, 100.00 * bandNumber);
            AddParameterToDictionary(String.Format("equalizerAq{0}", bandNumber), 124 + increment, 1.00);
            AddParameterToDictionary(String.Format("equalizerAtype{0}", bandNumber), 132 + increment, bandNumber == 1 || bandNumber == 8 ? BandMode1And8.Cut48 : BandMode2To7.Peak);
            AddParameterToDictionary(String.Format("invert{0}", bandNumber), 1022 + increment, 0.00);
            AddParameterToDictionary(String.Format("equalizerAon{0}Ch2", bandNumber), 260 + increment, 1.00);
            AddParameterToDictionary(String.Format("equalizerAgain{0}Ch2", bandNumber), 268 + increment, 0.00);
            AddParameterToDictionary(String.Format("equalizerAfreq{0}Ch2", bandNumber), 276 + increment, 25.00);
            AddParameterToDictionary(String.Format("equalizerAq{0}Ch2", bandNumber), 284 + increment, 1.00);
            AddParameterToDictionary(String.Format("equalizerAtype{0}Ch2", bandNumber), 292 + increment, 6.00);
            AddParameterToDictionary(String.Format("invert{0}Ch2", bandNumber), 1030 + increment, 0.00);
            AddParameterToDictionary(String.Format("equalizerAeditchannel{0}", bandNumber), 50 + increment, 2.00);
            AddParameterToDictionary(String.Format("equalizerAbandon{0}", bandNumber), 58 + increment, 1.00);
            AddParameterToDictionary(String.Format("linearphase{0}", bandNumber), 66 + increment, 0.00);
        }

        private void SetFrequencyPostParameters()
        {
            AddParameterToDictionary("equalizerAbypass", 1, 0.00);
            AddParameterToDictionary("equalizerAoutput", 2, 0.00);
            AddParameterToDictionary("bypass", 1002, 0.00);
            AddParameterToDictionary("reset", 1003, 0.00);
            AddParameterToDictionary("autoListen", 1005, 0.00);
            AddParameterToDictionary("spectrumonoff", 1007, 1.00);
            AddParameterToDictionary("spectrum2ChMode", 1008, 0.00);
            AddParameterToDictionary("spectrumintegrate", 1010, 40.00);
            AddParameterToDictionary("spectrumPHonoff", 1011, 1.00);
            AddParameterToDictionary("spectrumslope", 1012, 0.00);
            AddParameterToDictionary("draweq", 1013, 1.00);
            AddParameterToDictionary("draweqfilled", 1014, 1.00);
            AddParameterToDictionary("spectrumbargraph", 1015, 0.00);
            AddParameterToDictionary("showPianoRoll", 1019, 1.00);
            AddParameterToDictionary("transparency", 1020, 0.30);
            AddParameterToDictionary("autoGainOutputValue", 1021, 0.00);
            AddParameterToDictionary("", 3, 0.00);
        }

        private void AddParameterToDictionary(string name, UInt32 number, double value)
        {
            var parameter = new Parameter(name, number, value);
            this.Parameters.Add(name, parameter);
        }

        private void SetXml()
        {
            XmlDocument xml = new XmlDocument();
            XmlNode docNode = xml.CreateXmlDeclaration("1.0", "utf-8", null);
            xml.AppendChild(docNode);
            XmlElement root = xml.CreateElement("MetaInfo");
            xml.AppendChild(root);

            XmlElement attr1 = xml.CreateElement("Attribute");
            attr1.SetAttribute("id", "MediaType");
            attr1.SetAttribute("value", "VstPreset");
            attr1.SetAttribute("type", "string");
            attr1.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr1);

            XmlElement attr2 = xml.CreateElement("Attribute");
            attr2.SetAttribute("id", "PlugInCategory");
            attr2.SetAttribute("value", "Fx|EQ");
            attr2.SetAttribute("type", "string");
            attr2.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr2);

            XmlElement attr3 = xml.CreateElement("Attribute");
            attr3.SetAttribute("id", "PlugInName");
            attr3.SetAttribute("value", "Frequency");
            attr3.SetAttribute("type", "string");
            attr3.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr3);

            XmlElement attr4 = xml.CreateElement("Attribute");
            attr4.SetAttribute("id", "PlugInVendor");
            attr4.SetAttribute("value", "Steinberg Media Technologies");
            attr4.SetAttribute("type", "string");
            attr4.SetAttribute("flags", "writeProtected");
            root.AppendChild(attr4);

            this.Xml = BeautifyXml(xml);
        }
    }
}