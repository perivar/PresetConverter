using System.Text;
using System.Globalization;
using System.Xml.Linq;
using CommonUtils;
using System.Xml;

namespace PresetConverter
{
    /// <summary>
    /// Waves SSLChannel
    /// </summary>
    public class SSLNativeChannel : VstPreset
    {
        public string PresetName = "";
        public string PresetVersion = "";
        public string PresetType = "";


        #region Public Fields
        public bool Bypass = false; // PARAM
        public bool CompFastAttack = false; // PARAM
        public double CompMix = 100.0; // PARAM
        public double CompPeak = 0.0; // PARAM
        public double CompRatio = 6.0; // PARAM
        public double CompRelease = 0.1000000014901161; // PARAM
        public double CompThreshold = 10.0; // PARAM
        public bool DynamicsIn = true; // PARAM
        public bool DynamicsPreEq = false; // PARAM
        public bool EqE = false; // PARAM
        public bool EqIn = true; // PARAM
        public bool EqToSidechain = false; // PARAM
        public double FaderLevel = 0.0; // PARAM
        public bool FiltersToInput = true; // PARAM
        public bool FiltersToSidechain = false; // PARAM
        public bool GateDisabledExpEnabled = false; // PARAM
        public bool GateFastAttack = false; // PARAM
        public double GateHold = 0.25; // PARAM
        public double GateRange = 0.0; // PARAM
        public double GateRelease = 0.1000000014901161; // PARAM
        public double GateThreshold = 10.0; // PARAM
        public bool HighEqBell = false; // PARAM
        public double HighEqFreq = 7.500000476837158; // PARAM
        public double HighEqGain = 0.0; // PARAM
        public double HighMidEqFreq = 2.5; // PARAM
        public double HighMidEqGain = 0.0; // PARAM
        public double HighMidEqQ = 1.5; // PARAM
        public double HighPassFreq = 20.0; // PARAM
        public double InputTrim = 0.0; // PARAM
        public bool LowEqBell = false; // PARAM
        public double LowEqFreq = 185.0; // PARAM
        public double LowEqGain = 0.0; // PARAM
        public double LowMidEqFreq = 0.800000011920929; // PARAM
        public double LowMidEqGain = 0.0; // PARAM
        public double LowMidEqQ = 1.5; // PARAM
        public double LowPassFreq = 35.0; // PARAM
        public double OutputTrim = 0.0; // PARAM
        public double Pan = 0.0; // PARAM
        public bool PhaseInvert = false; // PARAM
        public bool SidechainListen = false; // PARAM
        public bool UseExternalKey = false; // PARAM
        public double Width = 100.0; // PARAM
        public bool HighQuality = false; // PARAM_NON_AUTO
        #endregion

        public SSLNativeChannel()
        {
            Vst3ClassID = VstClassIDs.SSLNativeChannel2;
            PlugInCategory = "Fx|EQ";
            PlugInName = "SSL Native Channel Strip 2";
            PlugInVendor = "SSL";
        }

        public static SSLNativeChannel LoadFromFile(string filePath)
        {
            var xml = XDocument.Load(filePath);
            return FromXml(xml);
        }

        public void SaveToFile(string filePath)
        {
            var xml = new XDocument(ToXml());
            xml.Save(filePath);
        }

        protected override bool PreparedForWriting()
        {
            InitCompChunkData();
            InitInfoXml();
            CalculateBytePositions();
            return true;
        }

        protected void InitCompChunkData()
        {
            var xml = ToXml();
            var xmlContent = BeautifyXml(xml);
            var xmlBytes = Encoding.UTF8.GetBytes(xmlContent);

            CompChunkData = xmlBytes;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("PresetName: {0}", PresetName));
            sb.AppendLine();

            // Determine the processing order of filter (FLTR), dynamic range processing (DYN),
            // and equalization (EQ) based on the values of the variables FiltersToInput and DynamicsPreEq.
            // The order of processing is determined by the following conditions:
            // 1. If FiltersToInput is 1 and DynamicsPreEq is 1, the order is FLTR -> DYN -> EQ.
            // 2. If FiltersToInput is 1 and DynamicsPreEq is 0, the order is FLTR -> EQ -> DYN.
            // 3. If FiltersToInput is 0 and DynamicsPreEq is 1, the order is DYN -> EQ -> FLTR.
            // 4. If FiltersToInput is 0 and DynamicsPreEq is 0, the default order is EQ -> FLTR -> DYN.
            // The results are appended to a StringBuilder (sb) to represent the determined processing order.
            sb.Append("Routing Diagram: ");
            if (FiltersToInput && DynamicsPreEq)
            {
                sb.AppendLine("FLTR -> DYN -> EQ");
            }
            else if (FiltersToInput && !DynamicsPreEq)
            {
                sb.AppendLine("FLTR -> EQ -> DYN");
            }
            else if (!FiltersToInput && DynamicsPreEq)
            {
                sb.AppendLine("DYN -> EQ -> FLTR");
            }
            else if (!FiltersToInput && !DynamicsPreEq)
            {
                sb.AppendLine("EQ -> FLTR -> DYN (default)");
            }
            sb.AppendLine();

            sb.AppendLine("Low and High Pass Filters:");
            sb.AppendLine(string.Format("\tHP Frequency (18 dB/octave): {0:0.##} Hz (16 - 350 Hz)", HighPassFreq));
            sb.AppendLine(string.Format("\tLP Frequency (12 dB/octave): {0:0.##} KHz (22 - 3 KHz)", LowPassFreq));
            sb.AppendLine();

            sb.AppendLine("Compression:");
            sb.AppendLine(string.Format("\tThreshold: {0:0.##} dB", CompThreshold));
            sb.AppendLine(string.Format("\tRatio: {0:0.##}:1", CompRatio));
            sb.AppendLine(string.Format("\tRelease: {0:0.##} s", CompRelease));
            sb.AppendLine(string.Format("\tMix: {0:0.##} %", CompMix));
            sb.AppendLine(string.Format("\tFast Attack: {0}", CompFastAttack));
            sb.AppendLine(string.Format("\tPeak: {0}", CompPeak));
            sb.AppendLine();

            sb.AppendLine("Expander/Gate:");
            sb.AppendLine(string.Format("\tExpander Enabled (Gate Disabled): {0}", GateDisabledExpEnabled));
            sb.AppendLine(string.Format("\tThreshold: {0:0.##} dB", GateThreshold));
            sb.AppendLine(string.Format("\tRange: {0:0.##} dB", GateRange));
            sb.AppendLine(string.Format("\tHold: {0:0.##} s", GateHold));
            sb.AppendLine(string.Format("\tFast Attack: {0}", GateFastAttack));
            sb.AppendLine(string.Format("\tRelease: {0:0.##} s", GateRelease));
            sb.AppendLine();

            sb.AppendLine("Dynamics/ Routing:");
            sb.AppendLine(string.Format("\tDynamicsIn: {0}", DynamicsIn));
            sb.AppendLine(string.Format("\tDynamicsPreEq: {0}", DynamicsPreEq));
            sb.AppendLine(string.Format("\tFiltersToInput: {0}", FiltersToInput));
            sb.AppendLine(string.Format("\tFiltersToSidechain: {0}", FiltersToSidechain));
            sb.AppendLine();

            sb.AppendLine("EQ Section:");
            sb.AppendLine(string.Format("\tLF Type Bell: {0}", LowEqBell));
            sb.AppendLine(string.Format("\tLF Gain: {0:0.##} dB", LowEqGain));
            sb.AppendLine(string.Format("\tLF Frequency: {0:0.##} Hz", LowEqFreq));

            sb.AppendLine(string.Format("\tLMF Gain: {0:0.##} dB", LowMidEqGain));
            sb.AppendLine(string.Format("\tLMF Frequency: {0:0.##} KHz", LowMidEqFreq));
            sb.AppendLine(string.Format("\tLMF Q: {0:0.##}", LowMidEqQ));

            sb.AppendLine(string.Format("\tHMF Gain: {0:0.##} dB", HighMidEqGain));
            sb.AppendLine(string.Format("\tHMF Frequency: {0:0.##} KHz", HighMidEqFreq));
            sb.AppendLine(string.Format("\tHMF Q: {0:0.##}", HighMidEqQ));

            sb.AppendLine(string.Format("\tHF Type Bell: {0}", HighEqBell));
            sb.AppendLine(string.Format("\tHF Gain: {0:0.##} dB", HighEqGain));
            sb.AppendLine(string.Format("\tHF Frequency: {0:0.##} KHz", HighEqFreq));
            sb.AppendLine(string.Format("\tEqIn: {0}", EqIn));
            sb.AppendLine(string.Format("\tEqE: {0}", EqE));
            sb.AppendLine(string.Format("\tEqToSidechain: {0}", EqToSidechain));
            sb.AppendLine();

            sb.AppendLine("Master Section:");
            sb.AppendLine(string.Format("\tBypass: {0}", Bypass));
            sb.AppendLine(string.Format("\tFader Level: {0:0.##} dB", FaderLevel));
            sb.AppendLine(string.Format("\tInput Trim: {0:0.##} dB", InputTrim));
            sb.AppendLine(string.Format("\tOutput Trim: {0:0.##} dB", OutputTrim));
            sb.AppendLine(string.Format("\tPan: {0}", Pan));
            sb.AppendLine(string.Format("\tPhase Invert: {0}", PhaseInvert));
            sb.AppendLine(string.Format("\tSidechain Listen: {0}", SidechainListen));
            sb.AppendLine(string.Format("\tUse External Key: {0}", UseExternalKey));
            sb.AppendLine(string.Format("\tWidth: {0:0.##} %", Width));
            sb.AppendLine(string.Format("\tHigh Quality: {0}", HighQuality));

            return sb.ToString();
        }

        private static XElement ParamToXml(string paramName, double paramValue, bool IsNonAutoParam = false)
        {
            return new XElement(IsNonAutoParam ? "PARAM_NON_AUTO" : "PARAM",
                    new XAttribute("id", paramName),
                    new XAttribute("value", paramValue.ToString("0.0###", CultureInfo.InvariantCulture))); // keep up to 4 decimal places
        }

        private static double ParamFromXml(XElement? xElement, string paramId, bool IsNonAutoParam = false)
        {
            var paramValue = xElement?
            .Descendants(IsNonAutoParam ? "PARAM_NON_AUTO" : "PARAM")
            .Where(param => param.Attribute("id")?.Value == paramId)
            .Select(param => param.Attribute("value")?.Value)
            .FirstOrDefault();

            return double.Parse(paramValue ?? "0", CultureInfo.InvariantCulture);
        }

        private static SSLNativeChannel FromXml(XDocument xml)
        {
            var processorState = xml.Root?.Element("PROCESSOR_STATE");

            var preset = new SSLNativeChannel
            {
                PlugInName = xml.Root?.Attribute("PluginName")?.Value ?? "",
                PresetVersion = xml.Root?.Attribute("Version")?.Value ?? "",
                PresetType = xml.Root?.Attribute("PresetType")?.Value ?? "",

                // all valus are stored as double
                Bypass = ParamFromXml(processorState, "Bypass") != 0.0,
                CompFastAttack = ParamFromXml(processorState, "CompFastAttack") != 0.0,
                CompMix = ParamFromXml(processorState, "CompMix"),
                CompPeak = ParamFromXml(processorState, "CompPeak"),
                CompRatio = ParamFromXml(processorState, "CompRatio"),
                CompRelease = ParamFromXml(processorState, "CompRelease"),
                CompThreshold = ParamFromXml(processorState, "CompThreshold"),
                DynamicsIn = ParamFromXml(processorState, "DynamicsIn") != 0.0,
                DynamicsPreEq = ParamFromXml(processorState, "DynamicsPreEq") != 0.0,
                EqE = ParamFromXml(processorState, "EqE") != 0.0,
                EqIn = ParamFromXml(processorState, "EqIn") != 0.0,
                EqToSidechain = ParamFromXml(processorState, "EqToSidechain") != 0.0,
                FaderLevel = ParamFromXml(processorState, "FaderLevel"),
                FiltersToInput = ParamFromXml(processorState, "FiltersToInput") != 0.0,
                FiltersToSidechain = ParamFromXml(processorState, "FiltersToSidechain") != 0.0,
                GateDisabledExpEnabled = ParamFromXml(processorState, "GateExpander") != 0.0,
                GateFastAttack = ParamFromXml(processorState, "GateFastAttack") != 0.0,
                GateHold = ParamFromXml(processorState, "GateHold"),
                GateRange = ParamFromXml(processorState, "GateRange"),
                GateRelease = ParamFromXml(processorState, "GateRelease"),
                GateThreshold = ParamFromXml(processorState, "GateThreshold"),
                HighEqBell = ParamFromXml(processorState, "HighEqBell") != 0.0,
                HighEqFreq = ParamFromXml(processorState, "HighEqFreq"),
                HighEqGain = ParamFromXml(processorState, "HighEqGain"),
                HighMidEqFreq = ParamFromXml(processorState, "HighMidEqFreq"),
                HighMidEqGain = ParamFromXml(processorState, "HighMidEqGain"),
                HighMidEqQ = ParamFromXml(processorState, "HighMidEqQ"),
                HighPassFreq = ParamFromXml(processorState, "HighPassFreq"),
                InputTrim = ParamFromXml(processorState, "InputTrim"),
                LowEqBell = ParamFromXml(processorState, "LowEqBell") != 0.0,
                LowEqFreq = ParamFromXml(processorState, "LowEqFreq"),
                LowEqGain = ParamFromXml(processorState, "LowEqGain"),
                LowMidEqFreq = ParamFromXml(processorState, "LowMidEqFreq"),
                LowMidEqGain = ParamFromXml(processorState, "LowMidEqGain"),
                LowMidEqQ = ParamFromXml(processorState, "LowMidEqQ"),
                LowPassFreq = ParamFromXml(processorState, "LowPassFreq"),
                OutputTrim = ParamFromXml(processorState, "OutputTrim"),
                Pan = ParamFromXml(processorState, "Pan"),
                PhaseInvert = ParamFromXml(processorState, "PhaseInvert") != 0.0,
                SidechainListen = ParamFromXml(processorState, "SidechainListen") != 0.0,
                UseExternalKey = ParamFromXml(processorState, "UseExternalKey") != 0.0,
                Width = ParamFromXml(processorState, "Width"),
                HighQuality = ParamFromXml(processorState, "HighQuality", true) != 0.0,
            };

            return preset;
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

        private XElement ToXml()
        {
            var xml =
                new XElement("SSL_PRESET",
                    new XAttribute("PluginName", PlugInName),
                    new XAttribute("Version", PresetVersion),
                    new XAttribute("PresetType", PresetType),
                    new XElement("PROCESSOR_STATE",
                        ParamToXml("Bypass", Bypass ? 1.0 : 0.0),
                        ParamToXml("CompFastAttack", CompFastAttack ? 1.0 : 0.0),
                        ParamToXml("CompMix", CompMix),
                        ParamToXml("CompPeak", CompPeak),
                        ParamToXml("CompRatio", CompRatio),
                        ParamToXml("CompRelease", CompRelease),
                        ParamToXml("CompThreshold", CompThreshold),
                        ParamToXml("DynamicsIn", DynamicsIn ? 1.0 : 0.0),
                        ParamToXml("DynamicsPreEq", DynamicsPreEq ? 1.0 : 0.0),
                        ParamToXml("EqE", EqE ? 1.0 : 0.0),
                        ParamToXml("EqIn", EqIn ? 1.0 : 0.0),
                        ParamToXml("EqToSidechain", EqToSidechain ? 1.0 : 0.0),
                        ParamToXml("FaderLevel", FaderLevel),
                        ParamToXml("FiltersToInput", FiltersToInput ? 1.0 : 0.0),
                        ParamToXml("FiltersToSidechain", FiltersToSidechain ? 1.0 : 0.0),
                        ParamToXml("GateExpander", GateDisabledExpEnabled ? 1.0 : 0.0),
                        ParamToXml("GateFastAttack", GateFastAttack ? 1.0 : 0.0),
                        ParamToXml("GateHold", GateHold),
                        ParamToXml("GateRange", GateRange),
                        ParamToXml("GateRelease", GateRelease),
                        ParamToXml("GateThreshold", GateThreshold),
                        ParamToXml("HighEqBell", HighEqBell ? 1.0 : 0.0),
                        ParamToXml("HighEqFreq", HighEqFreq),
                        ParamToXml("HighEqGain", HighEqGain),
                        ParamToXml("HighMidEqFreq", HighMidEqFreq),
                        ParamToXml("HighMidEqGain", HighMidEqGain),
                        ParamToXml("HighMidEqQ", HighMidEqQ),
                        ParamToXml("HighPassFreq", HighPassFreq),
                        ParamToXml("InputTrim", InputTrim),
                        ParamToXml("LowEqBell", LowEqBell ? 1.0 : 0.0),
                        ParamToXml("LowEqFreq", LowEqFreq),
                        ParamToXml("LowEqGain", LowEqGain),
                        ParamToXml("LowMidEqFreq", LowMidEqFreq),
                        ParamToXml("LowMidEqGain", LowMidEqGain),
                        ParamToXml("LowMidEqQ", LowMidEqQ),
                        ParamToXml("LowPassFreq", LowPassFreq),
                        ParamToXml("OutputTrim", OutputTrim),
                        ParamToXml("Pan", Pan),
                        ParamToXml("PhaseInvert", PhaseInvert ? 1.0 : 0.0),
                        ParamToXml("SidechainListen", SidechainListen ? 1.0 : 0.0),
                        ParamToXml("UseExternalKey", UseExternalKey ? 1.0 : 0.0),
                        ParamToXml("Width", Width),
                        ParamToXml("HighQuality", HighQuality ? 1.0 : 0.0, true)
                    )
                );

            return xml;
        }
    }
}
