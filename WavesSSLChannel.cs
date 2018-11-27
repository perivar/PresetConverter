using System;
using System.Text;
using System.Globalization;
using AbletonLiveConverter;
using CommonUtils;
using System.IO;
using System.Xml.Linq;

namespace PresetConverter
{
    /// <summary>
    /// Waves SSLChannel
    /// </summary>
    public class WavesSSLChannel : WavesPreset
    {
        #region Public Fields
        public float CompThreshold;
        public float CompRatio;
        public bool CompFastAttack;
        public float CompRelease;

        public float ExpThreshold;
        public float ExpRange;
        public bool ExpGate;
        public bool ExpFastAttack;
        public float ExpRelease;

        public bool DynToByPass;
        public bool DynToChannelOut;

        public bool LFTypeBell;
        public float LFGain;
        public float LFFrq;

        public float LMFGain;
        public float LMFFrq;
        public float LMFQ;

        public float HMFGain;
        public float HMFFrq;
        public float HMFQ;

        public bool HFTypeBell;
        public float HFGain;
        public float HFFrq;

        public bool EQToBypass;
        public bool EQToDynSC;

        public float HPFrq;
        public float LPFrq;
        public bool FilterSplit;

        public float Gain;
        public bool Analog;
        public bool VUShowOutput;
        public bool PhaseReverse;
        public float InputTrim;
        #endregion

        public WavesSSLChannel()
        {
            Vst3ID = VstPreset.VstIDs.WavesSSLChannel;
            PlugInCategory = "Fx|Channel Strip";
            PlugInName = "SSLChannel Stereo";
            PlugInVendor = "Waves";
        }

        protected override bool ReadRealWorldParameters()
        {
            if (PluginName == "SSLChannel")
            {
                // split the parameters text into sections
                string[] splittedPhrase = RealWorldParameters.Split(' ', '\n');

                CompThreshold = float.Parse(splittedPhrase[0], CultureInfo.InvariantCulture); // compression threshold in dB
                CompRatio = float.Parse(splittedPhrase[1], CultureInfo.InvariantCulture); // compression ratio
                CompFastAttack = (splittedPhrase[2] == "1"); // compression fast attack
                CompRelease = float.Parse(splittedPhrase[3], CultureInfo.InvariantCulture); // compression release in ms

                string Delimiter1 = splittedPhrase[4];

                ExpThreshold = float.Parse(splittedPhrase[5], CultureInfo.InvariantCulture); // expander threshold in dB
                ExpRange = float.Parse(splittedPhrase[6], CultureInfo.InvariantCulture); // expander range in dB
                ExpGate = (splittedPhrase[7] == "1"); // expander gate
                ExpFastAttack = (splittedPhrase[8] == "1"); // expander fast attack
                ExpRelease = float.Parse(splittedPhrase[9], CultureInfo.InvariantCulture); // expander release in ms

                string Delimiter2 = splittedPhrase[10];

                DynToByPass = (splittedPhrase[11] == "1"); // Dyn To By Pass
                DynToChannelOut = (splittedPhrase[12] == "1"); // Dyn To Channel Out

                LFTypeBell = (splittedPhrase[13] == "1"); // Bell
                LFGain = float.Parse(splittedPhrase[14], CultureInfo.InvariantCulture); // dB
                LFFrq = float.Parse(splittedPhrase[15], CultureInfo.InvariantCulture); // Hz

                LMFGain = float.Parse(splittedPhrase[16], CultureInfo.InvariantCulture); // dB
                LMFFrq = float.Parse(splittedPhrase[17], CultureInfo.InvariantCulture); // KHz
                LMFQ = float.Parse(splittedPhrase[18], CultureInfo.InvariantCulture);

                HMFGain = float.Parse(splittedPhrase[19], CultureInfo.InvariantCulture); // dB
                HMFFrq = float.Parse(splittedPhrase[20], CultureInfo.InvariantCulture); // KHz
                HMFQ = float.Parse(splittedPhrase[21], CultureInfo.InvariantCulture);

                HFTypeBell = (splittedPhrase[22] == "1"); // Bell
                HFGain = float.Parse(splittedPhrase[23], CultureInfo.InvariantCulture); // dB
                HFFrq = float.Parse(splittedPhrase[24], CultureInfo.InvariantCulture); // KHz

                EQToBypass = (splittedPhrase[25] == "1");
                EQToDynSC = (splittedPhrase[26] == "1");

                HPFrq = float.Parse(splittedPhrase[27], CultureInfo.InvariantCulture); // Hz
                LPFrq = float.Parse(splittedPhrase[28], CultureInfo.InvariantCulture); // KHz

                FilterSplit = (splittedPhrase[29] == "1");

                Gain = float.Parse(splittedPhrase[30], CultureInfo.InvariantCulture); // dB

                Analog = (splittedPhrase[31] == "1");

                string Delimiter3 = splittedPhrase[32];
                string Delimiter4 = splittedPhrase[33];

                VUShowOutput = (splittedPhrase[34] == "1");

                string Delimiter5 = splittedPhrase[35];
                string Delimiter6 = splittedPhrase[36];

                float Unknown1 = float.Parse(splittedPhrase[37], CultureInfo.InvariantCulture);
                float Unknown2 = float.Parse(splittedPhrase[38], CultureInfo.InvariantCulture);

                PhaseReverse = (splittedPhrase[39] == "1");
                InputTrim = float.Parse(splittedPhrase[40], CultureInfo.InvariantCulture); // dB

                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(String.Format("PresetName: {0}", PresetName));
            if (PresetGroup != null)
            {
                sb.AppendLine(String.Format("Group: {0}", PresetGroup));
            }
            sb.AppendLine();

            sb.Append("Routing Diagram: ");
            if (!FilterSplit && !DynToChannelOut)
            {
                sb.AppendLine("DYN -> FLTR -> EQ (default)");
            }
            else if (FilterSplit && !DynToChannelOut)
            {
                sb.AppendLine("FLTR -> DYN -> EQ");
            }
            else if (DynToChannelOut)
            {
                sb.AppendLine("FLTR -> EQ -> DYN");
            }
            sb.AppendLine();

            sb.AppendLine("Low and High Pass Filters:");
            sb.AppendLine(String.Format("\tHP Frequency (18 dB/octave): {0:0.##} Hz (16 - 350 Hz)", HPFrq));
            sb.AppendLine(String.Format("\tLP Frequency (12 dB/octave): {0:0.##} KHz (22 - 3 KHz)", LPFrq));
            sb.AppendLine(String.Format("\tFilter Split (Filters before Dynamics): {0}", FilterSplit));
            sb.AppendLine();

            sb.AppendLine("Compression:");
            sb.AppendLine(String.Format("\tThreshold: {0:0.##} dB", CompThreshold));
            sb.AppendLine(String.Format("\tRatio: {0}", CompRatio));
            sb.AppendLine(String.Format("\tFast Attack: {0} (Fast=1 ms otherwise Auto-Sense)", CompFastAttack));
            sb.AppendLine(String.Format("\tRelease: {0:0.##} s", CompRelease));
            sb.AppendLine();

            sb.AppendLine("Expander/Gate:");
            sb.AppendLine(String.Format("\tThreshold: {0:0.##} dB", ExpThreshold));
            sb.AppendLine(String.Format("\tRange: {0:0.##} dB", ExpRange));
            sb.AppendLine(String.Format("\tGate: {0}", ExpGate));
            sb.AppendLine(String.Format("\tFast Attack: {0} (Fast=1 ms otherwise Auto-Sense)", ExpFastAttack));
            sb.AppendLine(String.Format("\tRelease: {0:0.##} s", ExpRelease));
            sb.AppendLine();

            sb.AppendLine("Dynamics To:");
            sb.AppendLine(String.Format("\tBypass: {0}", DynToByPass));
            sb.AppendLine(String.Format("\tChannel Out (Dynamics after EQ): {0}", DynToChannelOut));
            sb.AppendLine();

            sb.AppendLine("EQ Section:");
            sb.AppendLine(String.Format("\tLF Type Bell: {0}", LFTypeBell));
            sb.AppendLine(String.Format("\tLF Gain: {0:0.##} dB", LFGain));
            sb.AppendLine(String.Format("\tLF Frequency: {0:0.##} Hz", LFFrq));

            sb.AppendLine(String.Format("\tLMF Gain: {0:0.##} dB", LMFGain));
            sb.AppendLine(String.Format("\tLMF Frequency: {0:0.##} KHz", LMFFrq));
            sb.AppendLine(String.Format("\tLMF Q: {0:0.##}", LMFQ));

            sb.AppendLine(String.Format("\tHMF Gain: {0:0.##} dB", HMFGain));
            sb.AppendLine(String.Format("\tHMF Frequency: {0:0.##} KHz", HMFFrq));
            sb.AppendLine(String.Format("\tHMF Q: {0:0.##}", HMFQ));

            sb.AppendLine(String.Format("\tHF Type Bell: {0}", HFTypeBell));
            sb.AppendLine(String.Format("\tHF Gain: {0:0.##} dB", HFGain));
            sb.AppendLine(String.Format("\tHF Frequency: {0:0.##} KHz", HFFrq));

            sb.AppendLine(String.Format("\tTo Bypass: {0}", EQToBypass));
            sb.AppendLine(String.Format("\tTo Dynamics Side-Chain: {0}", EQToDynSC));
            sb.AppendLine();

            sb.AppendLine("Master Section:");
            sb.AppendLine(String.Format("\tGain: {0:0.##} dB", Gain));
            sb.AppendLine(String.Format("\tAnalog: {0}", Analog));
            sb.AppendLine(String.Format("\tVU Show Output: {0}", VUShowOutput));
            sb.AppendLine(String.Format("\tPhase Reverse: {0}", PhaseReverse));
            sb.AppendLine(String.Format("\tInput Trim : {0:0.##} dB", InputTrim));

            return sb.ToString();
        }

        public string GenerateRealWorldParameters()
        {
            var sb = new StringBuilder();

            // -0.5 2.7999999999999998224 1 0.27000000000000001776 * -30 0 0 0 0.2000000000000000111
            // * 0 0 0 0 52 2.3999999999999999112 1.8000000000000000444 1.1499999999999999112
            // 1.3999999999999999112 5.5199999999999995737 0.85999999999999998668 0 1.1999999999999999556 9.2799999999999993605 0 0 79
            // 30 1 0 1 * * 1 * *
            // 7 -18 0 0 * * * * *
            // * -18 -18 -18 -18 -18 -18 -18 -18
            // 0 0 

            sb.AppendFormat("{0} ", FormatRealWorldParameter(CompThreshold)); // compression threshold in dB
            sb.AppendFormat("{0} ", FormatRealWorldParameter(CompRatio)); // compression ratio
            sb.AppendFormat("{0} ", CompFastAttack ? 1 : 0); // compression fast attack
            sb.AppendFormat("{0} ", FormatRealWorldParameter(CompRelease)); // compression release in ms

            sb.AppendFormat("* ");

            sb.AppendFormat("{0} ", FormatRealWorldParameter(ExpThreshold)); // expander threshold in dB
            sb.AppendFormat("{0} ", FormatRealWorldParameter(ExpRange)); // expander range in dB
            sb.AppendFormat("{0} ", ExpGate ? 1 : 0); // expander gate
            sb.AppendFormat("{0} ", ExpFastAttack ? 1 : 0); // expander fast attack
            sb.AppendFormat("{0} ", FormatRealWorldParameter(ExpRelease)); // expander release in ms
            sb.Append("\n");

            sb.AppendFormat("* ");

            sb.AppendFormat("{0} ", DynToByPass ? 1 : 0); // Dyn To By Pass
            sb.AppendFormat("{0} ", DynToChannelOut ? 1 : 0); // Dyn To Channel Out

            sb.AppendFormat("{0} ", LFTypeBell ? 1 : 0); // Bell
            sb.AppendFormat("{0} ", FormatRealWorldParameter(LFGain)); // dB
            sb.AppendFormat("{0} ", FormatRealWorldParameter(LFFrq)); // Hz

            sb.AppendFormat("{0} ", FormatRealWorldParameter(LMFGain)); // dB
            sb.AppendFormat("{0} ", FormatRealWorldParameter(LMFFrq)); // KHz
            sb.AppendFormat("{0} ", FormatRealWorldParameter(LMFQ));
            sb.Append("\n");

            sb.AppendFormat("{0} ", FormatRealWorldParameter(HMFGain)); // dB
            sb.AppendFormat("{0} ", FormatRealWorldParameter(HMFFrq)); // KHz
            sb.AppendFormat("{0} ", FormatRealWorldParameter(HMFQ));

            sb.AppendFormat("{0} ", HFTypeBell ? 1 : 0); // Bell
            sb.AppendFormat("{0} ", FormatRealWorldParameter(HFGain)); // dB
            sb.AppendFormat("{0} ", FormatRealWorldParameter(HFFrq)); // KHz

            sb.AppendFormat("{0} ", EQToBypass ? 1 : 0);
            sb.AppendFormat("{0} ", EQToDynSC ? 1 : 0);

            sb.AppendFormat("{0} ", FormatRealWorldParameter(HPFrq)); // Hz
            sb.Append("\n");

            sb.AppendFormat("{0} ", FormatRealWorldParameter(LPFrq)); // KHz

            sb.AppendFormat("{0} ", FilterSplit ? 1 : 0);

            sb.AppendFormat("{0} ", FormatRealWorldParameter(Gain)); // dB

            sb.AppendFormat("{0} ", Analog ? 1 : 0);

            sb.AppendFormat("* ");
            sb.AppendFormat("* ");

            sb.AppendFormat("{0} ", VUShowOutput ? 1 : 0);

            sb.AppendFormat("* ");
            sb.AppendFormat("*\n");

            sb.AppendFormat("{0} ", 7); // unknown
            sb.AppendFormat("{0} ", -18); // unknown

            sb.AppendFormat("{0} ", PhaseReverse ? 1 : 0);
            sb.AppendFormat("{0} ", FormatRealWorldParameter(InputTrim)); // dB

            // append end
            sb.AppendFormat("* * * * *\n* -18 -18 -18 -18 -18 -18 -18 -18\n0 0 ");

            return sb.ToString();
        }

        private string GeneratePresetXML()
        {
            // string realWorldParameters = RealWorldParameters + '\n';
            string realWorldParameters = GenerateRealWorldParameters();

            // Use Linq XML (XElement) because they are easier to work with
            XElement doc = new XElement("PresetChunkXMLTree", new XAttribute("version", "2"),
                        new XElement("Preset", new XAttribute("Name", PresetName), new XAttribute("GenericType", PresetGenericType),
                        new XElement("PresetHeader",
                            new XElement("PluginName", PluginName),
                            new XElement("PluginSubComp", PluginSubComp),
                            new XElement("PluginVersion", PluginVersion),
                            new XElement("ActiveSetup", ActiveSetup),
                            new XElement("ReadOnly", "true")
                            ),
                        new XElement("PresetData", new XAttribute("Setup", "SETUP_A"),
                            new XElement("Parameters", realWorldParameters,
                            new XAttribute("Type", "RealWorld"))),
                        new XElement("PresetData", new XAttribute("Setup", "SETUP_B"),
                            new XElement("Parameters", realWorldParameters,
                            new XAttribute("Type", "RealWorld")))
                        ));

            return BeautifyXml(doc);
        }

        protected override void InitChunkData()
        {
            var xmlContent = GeneratePresetXML();
            var xmlPostContent = "<Bypass Version=\"1.0\" Bypass=\"0\"/>\n";

            var memStream = new MemoryStream();
            using (BinaryFile bf = new BinaryFile(memStream, BinaryFile.ByteOrder.BigEndian, Encoding.ASCII))
            {
                // length of the xml section until xmlPostContent including 12 bytes
                UInt32 xmlContentFullLength = (uint)xmlContent.Length + 32;
                bf.Write((UInt32)xmlContentFullLength);
                bf.Write((UInt32)3);
                bf.Write((UInt32)1);

                bf.Write("SCHS");
                bf.Write("setA");

                UInt32 xmlMainLength = (uint)xmlContent.Length;
                bf.Write(xmlMainLength);

                bf.Write("XPst");
                bf.Write(xmlContent);

                bf.Write("\0\0\0\0");

                bf.Write(xmlPostContent);
            }

            this.ChunkData = memStream.ToArray();
        }
    }
}
