﻿using System.Text;
using System.Globalization;
using System.Xml.Linq;

using CommonUtils;

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
            Vst3ClassID = VstClassIDs.WavesSSLChannelStereo;
            PlugInCategory = "Fx|Channel Strip";
            PlugInName = "SSLChannel Stereo";
            PlugInVendor = "Waves";
        }

        protected override bool ReadRealWorldParameters()
        {
            // Note that the PresetPluginName is "SSLChannel" even if the PlugInName is "SSLChannel Stereo"
            if (PresetPluginName == "SSLChannel")
            {
                // split the parameters text into sections
                string[] splittedPhrase = PresetRealWorldParameters.Split(' ', '\n');

                CompThreshold = float.Parse(splittedPhrase[0], CultureInfo.InvariantCulture); // compression threshold in dB
                CompRatio = float.Parse(splittedPhrase[1], CultureInfo.InvariantCulture); // compression ratio
                CompFastAttack = splittedPhrase[2] == "1"; // compression fast attack
                CompRelease = float.Parse(splittedPhrase[3], CultureInfo.InvariantCulture); // compression release in ms

                string delimiter1 = splittedPhrase[4];

                ExpThreshold = float.Parse(splittedPhrase[5], CultureInfo.InvariantCulture); // expander threshold in dB
                ExpRange = float.Parse(splittedPhrase[6], CultureInfo.InvariantCulture); // expander range in dB
                ExpGate = splittedPhrase[7] == "1"; // expander gate
                ExpFastAttack = splittedPhrase[8] == "1"; // expander fast attack
                ExpRelease = float.Parse(splittedPhrase[9], CultureInfo.InvariantCulture); // expander release in ms

                string delimiter2 = splittedPhrase[10];

                DynToByPass = splittedPhrase[11] == "1"; // Dyn To By Pass
                DynToChannelOut = splittedPhrase[12] == "1"; // Dyn To Channel Out

                LFTypeBell = splittedPhrase[13] == "1"; // Bell
                LFGain = float.Parse(splittedPhrase[14], CultureInfo.InvariantCulture); // dB
                LFFrq = float.Parse(splittedPhrase[15], CultureInfo.InvariantCulture); // Hz

                LMFGain = float.Parse(splittedPhrase[16], CultureInfo.InvariantCulture); // dB
                LMFFrq = float.Parse(splittedPhrase[17], CultureInfo.InvariantCulture); // KHz
                LMFQ = float.Parse(splittedPhrase[18], CultureInfo.InvariantCulture);

                HMFGain = float.Parse(splittedPhrase[19], CultureInfo.InvariantCulture); // dB
                HMFFrq = float.Parse(splittedPhrase[20], CultureInfo.InvariantCulture); // KHz
                HMFQ = float.Parse(splittedPhrase[21], CultureInfo.InvariantCulture);

                HFTypeBell = splittedPhrase[22] == "1"; // Bell
                HFGain = float.Parse(splittedPhrase[23], CultureInfo.InvariantCulture); // dB
                HFFrq = float.Parse(splittedPhrase[24], CultureInfo.InvariantCulture); // KHz

                EQToBypass = splittedPhrase[25] == "1";
                EQToDynSC = splittedPhrase[26] == "1";

                HPFrq = float.Parse(splittedPhrase[27], CultureInfo.InvariantCulture); // Hz
                LPFrq = float.Parse(splittedPhrase[28], CultureInfo.InvariantCulture); // KHz

                FilterSplit = splittedPhrase[29] == "1";

                Gain = float.Parse(splittedPhrase[30], CultureInfo.InvariantCulture); // dB

                Analog = splittedPhrase[31] == "1";

                string delimiter3 = splittedPhrase[32];
                string delimiter4 = splittedPhrase[33];

                VUShowOutput = splittedPhrase[34] == "1";

                string delimiter5 = splittedPhrase[35];
                string delimiter6 = splittedPhrase[36];

                float unknown1 = float.Parse(splittedPhrase[37], CultureInfo.InvariantCulture);
                float unknown2 = float.Parse(splittedPhrase[38], CultureInfo.InvariantCulture);

                PhaseReverse = splittedPhrase[39] == "1";
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

            sb.AppendLine(string.Format("PresetName: {0}", PresetName));
            if (PresetGroup != null)
            {
                sb.AppendLine(string.Format("Group: {0}", PresetGroup));
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
            sb.AppendLine(string.Format("\tHP Frequency (18 dB/octave): {0:0.##} Hz (16 - 350 Hz)", HPFrq));
            sb.AppendLine(string.Format("\tLP Frequency (12 dB/octave): {0:0.##} KHz (22 - 3 KHz)", LPFrq));
            sb.AppendLine(string.Format("\tFilter Split (Filters before Dynamics): {0}", FilterSplit));
            sb.AppendLine();

            sb.AppendLine("Compression:");
            sb.AppendLine(string.Format("\tThreshold: {0:0.##} dB", CompThreshold));
            sb.AppendLine(string.Format("\tRatio: {0}", CompRatio));
            sb.AppendLine(string.Format("\tFast Attack: {0} (Fast=1 ms otherwise Auto-Sense)", CompFastAttack));
            sb.AppendLine(string.Format("\tRelease: {0:0.##} s", CompRelease));
            sb.AppendLine();

            sb.AppendLine("Expander/Gate:");
            sb.AppendLine(string.Format("\tThreshold: {0:0.##} dB", ExpThreshold));
            sb.AppendLine(string.Format("\tRange: {0:0.##} dB", ExpRange));
            sb.AppendLine(string.Format("\tGate: {0}", ExpGate));
            sb.AppendLine(string.Format("\tFast Attack: {0} (Fast=1 ms otherwise Auto-Sense)", ExpFastAttack));
            sb.AppendLine(string.Format("\tRelease: {0:0.##} s", ExpRelease));
            sb.AppendLine();

            sb.AppendLine("Dynamics To:");
            sb.AppendLine(string.Format("\tBypass: {0}", DynToByPass));
            sb.AppendLine(string.Format("\tChannel Out (Dynamics after EQ): {0}", DynToChannelOut));
            sb.AppendLine();

            sb.AppendLine("EQ Section:");
            sb.AppendLine(string.Format("\tLF Type Bell: {0}", LFTypeBell));
            sb.AppendLine(string.Format("\tLF Gain: {0:0.##} dB", LFGain));
            sb.AppendLine(string.Format("\tLF Frequency: {0:0.##} Hz", LFFrq));

            sb.AppendLine(string.Format("\tLMF Gain: {0:0.##} dB", LMFGain));
            sb.AppendLine(string.Format("\tLMF Frequency: {0:0.##} KHz", LMFFrq));
            sb.AppendLine(string.Format("\tLMF Q: {0:0.##}", LMFQ));

            sb.AppendLine(string.Format("\tHMF Gain: {0:0.##} dB", HMFGain));
            sb.AppendLine(string.Format("\tHMF Frequency: {0:0.##} KHz", HMFFrq));
            sb.AppendLine(string.Format("\tHMF Q: {0:0.##}", HMFQ));

            sb.AppendLine(string.Format("\tHF Type Bell: {0}", HFTypeBell));
            sb.AppendLine(string.Format("\tHF Gain: {0:0.##} dB", HFGain));
            sb.AppendLine(string.Format("\tHF Frequency: {0:0.##} KHz", HFFrq));

            sb.AppendLine(string.Format("\tTo Bypass: {0}", EQToBypass));
            sb.AppendLine(string.Format("\tTo Dynamics Side-Chain: {0}", EQToDynSC));
            sb.AppendLine();

            sb.AppendLine("Master Section:");
            sb.AppendLine(string.Format("\tGain: {0:0.##} dB", Gain));
            sb.AppendLine(string.Format("\tAnalog: {0}", Analog));
            sb.AppendLine(string.Format("\tVU Show Output: {0}", VUShowOutput));
            sb.AppendLine(string.Format("\tPhase Reverse: {0}", PhaseReverse));
            sb.AppendLine(string.Format("\tInput Trim : {0:0.##} dB", InputTrim));

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
            string realWorldParameters = GenerateRealWorldParameters();

            // Use Linq XML (XElement) because they are easier to work with
            XElement doc = new XElement("PresetChunkXMLTree", new XAttribute("version", "2"),
                        new XElement("Preset", new XAttribute("Name", PresetName), new XAttribute("GenericType", PresetGenericType),
                        new XElement("PresetHeader",
                            new XElement("PluginName", PresetPluginName),
                            new XElement("PluginSubComp", PresetPluginSubComp),
                            new XElement("PluginVersion", PresetPluginVersion),
                            new XElement("ActiveSetup", PresetActiveSetup),
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

        protected override void InitCompChunkData()
        {
            var xmlContent = GeneratePresetXML();
            var xmlPostContent = "<Bypass Version=\"1.0\" Bypass=\"0\"/>\n";

            var memStream = new MemoryStream();
            using (BinaryFile bf = new BinaryFile(memStream, BinaryFile.ByteOrder.BigEndian, Encoding.ASCII))
            {
                // length of the xml section until xmlPostContent including 12 bytes
                uint xmlContentFullLength = (uint)xmlContent.Length + 32;
                bf.Write((uint)xmlContentFullLength);
                bf.Write((uint)3);
                bf.Write((uint)1);

                bf.Write("SCHS");
                bf.Write("setA");

                uint xmlMainLength = (uint)xmlContent.Length;
                bf.Write(xmlMainLength);

                bf.Write("XPst");
                bf.Write(xmlContent);

                bf.Write("\0\0\0\0");

                bf.Write(xmlPostContent);
            }

            CompChunkData = memStream.ToArray();
        }
    }
}
