using System;
using System.Text;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using CommonUtils;
using System.IO;
using AbletonLiveConverter;

namespace PresetConverter
{
    /// <summary>
    /// Waves SSLComp
    /// </summary>
    public class WavesSSLComp : WavesPreset
    {
        // Ratio [2:1=0, 4:1=1, 10:1=2]
        public enum RatioType
        {
            Ratio_2_1 = 0,
            Ratio_4_1 = 1,
            Ratio_10_1 = 2
        }

        // Fade [Off=0 or *, Out=1, In=2]
        public enum FadeType
        {
            Off = 0,
            Out = 1,
            In = 2
        }

        #region Public Fields
        public float Threshold;
        public RatioType Ratio;
        public float Attack;
        public float Release;
        public float MakeupGain;
        public float RateS;
        public bool In;
        public bool Analog;
        public FadeType Fade;
        #endregion

        public WavesSSLComp()
        {
            Vst3ID = VstPreset.VstIDs.WavesSSLComp;
            PlugInCategory = "Fx|Dynamics";
            PlugInName = "SSLComp Stereo";
            PlugInVendor = "Waves";
        }

        protected override bool ReadRealWorldParameters()
        {
            if (PluginName == "SSLComp")
            {
                // <Parameters Type="RealWorld">8 1 * 3 4 3 * 1 1 1
                // 0 0 0.95000000000000006661 1 0.95000000000000006661 </Parameters>

                // split the parameters text into sections
                string[] splittedPhrase = RealWorldParameters.Split(' ', '\n');

                // Threshold (-15 - +15)
                Threshold = float.Parse(splittedPhrase[0], CultureInfo.InvariantCulture); // compression threshold in dB

                // Ratio (2:1=0, 4:1=1, 10:1=2)
                Ratio = (RatioType)Enum.Parse(typeof(RatioType), splittedPhrase[1]);

                // Fade [Off=0 or *, Out=1, In=2]
                if (splittedPhrase[2] != "*")
                {
                    Fade = (FadeType)Enum.Parse(typeof(FadeType), splittedPhrase[2]);
                }
                else
                {
                    Fade = FadeType.Off;
                }

                // Attack [0 - 5, .1 ms, .3 ms, 1 ms, 3 ms, 10 ms, 30 ms)
                int attack = int.Parse(splittedPhrase[3]);
                switch (attack)
                {
                    case 0:
                        Attack = 0.1f;
                        break;
                    case 1:
                        Attack = 0.3f;
                        break;
                    case 2:
                        Attack = 1.0f;
                        break;
                    case 3:
                        Attack = 3.0f;
                        break;
                    case 4:
                        Attack = 10.0f;
                        break;
                    case 5:
                        Attack = 30.0f;
                        break;
                }

                // Release: 0 - 4, .1 s, .3 s, .6 s, 1.2 s, Auto (-1)
                int release = int.Parse(splittedPhrase[4]);
                switch (release)
                {
                    case 0:
                        Release = 0.1f;
                        break;
                    case 1:
                        Release = 0.3f;
                        break;
                    case 2:
                        Release = 0.6f;
                        break;
                    case 3:
                        Release = 1.2f;
                        break;
                    case 4:
                        Release = -1.0f;
                        break;
                }

                // Make-Up Gain (-5 - +15) dB
                MakeupGain = float.Parse(splittedPhrase[5], CultureInfo.InvariantCulture);

                //*
                string Delimiter1 = splittedPhrase[6];

                // Rate-S (1 - +60) seconds
                // Autofade duration. Variable from 1 to 60 seconds
                RateS = float.Parse(splittedPhrase[7], CultureInfo.InvariantCulture);

                // In
                In = (splittedPhrase[8] == "1");

                // Analog
                Analog = (splittedPhrase[9] == "1");

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

            sb.AppendLine("Compression:");
            sb.AppendLine(String.Format("\tThreshold: {0:0.##} dB", Threshold));
            sb.AppendLine(String.Format("\tMake-up Gain: {0:0.##} dB", MakeupGain));
            sb.AppendLine(String.Format("\tAttack: {0:0.##} ms", Attack));
            if (Release == -1.0f)
            {
                sb.AppendLine("\tRelease: Auto");
            }
            else
            {
                sb.AppendLine(String.Format("\tRelease: {0} s", Release));
            }
            sb.AppendLine(String.Format("\tRatio: {0}", Ratio));
            sb.AppendLine(String.Format("\tRate-S (Autofade duration): {0} s", RateS));
            sb.AppendLine(String.Format("\tIn: {0}", In));
            sb.AppendLine(String.Format("\tAnalog: {0}", Analog));
            sb.AppendLine(String.Format("\tFade: {0}", Fade));
            sb.AppendLine();

            return sb.ToString();
        }

        public string GenerateRealWorldParameters()
        {
            var sb = new StringBuilder();

            // <Parameters Type="RealWorld">8 1 * 3 4 3 * 1 1 1
            // 0 0 0.95000000000000006661 1 0.95000000000000006661 </Parameters>

            // Threshold (-15 - +15)
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", Threshold); // compression threshold in dB

            // Ratio (2:1=0, 4:1=1, 10:1=2)
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", (float)Ratio);

            // Fade [Off=0 or *, Out=1, In=2]
            sb.AppendFormat("{0} ", (float)Fade);

            // Attack [0 - 5, .1 ms, .3 ms, 1 ms, 3 ms, 10 ms, 30 ms)
            int attack = 0;
            switch (Attack)
            {
                case 0.1f:
                    attack = 0;
                    break;
                case 0.3f:
                    attack = 1;
                    break;
                case 1.0f:
                    attack = 2;
                    break;
                case 3.0f:
                    attack = 3;
                    break;
                case 10.0f:
                    attack = 4;
                    break;
                case 30.0f:
                    attack = 5;
                    break;
            }
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", attack);

            // Release: 0 - 4, .1 s, .3 s, .6 s, 1.2 s, Auto (-1)
            int release = 0;
            switch (Release)
            {
                case 0.1f:
                    release = 0;
                    break;
                case 0.3f:
                    release = 1;
                    break;
                case 0.6f:
                    release = 2;
                    break;
                case 1.2f:
                    release = 3;
                    break;
                case -1.0f:
                    release = 4;
                    break;
            }
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", release);

            // Make-Up Gain (-5 - +15) dB
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", MakeupGain);

            // *
            sb.Append("* ");

            // Rate-S (1 - +60) seconds
            // Autofade duration. Variable from 1 to 60 seconds
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", RateS);

            // In
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", In ? "1" : "0");

            // Analog
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", Analog ? "1" : "0");

            // New line
            sb.Append("\n");

            // what is the second line?
            // 0 0 0.94999999999999995559 1 0.94999999999999995559 * * * *\n"
            sb.Append("0 0 0.94999999999999995559 1 0.94999999999999995559 * * * *\n");

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
                bf.Write((UInt32)809);
                bf.Write((UInt32)3);
                bf.Write((UInt32)1);

                bf.Write("SLCS");
                bf.Write("setA");

                UInt32 xmlMainLength = (uint)xmlContent.Length;
                bf.Write(xmlMainLength);

                bf.Write("XPst");
                bf.Write(xmlContent);

                bf.Write("Ref\0");

                bf.Write(xmlPostContent);
            }

            this.ChunkData = memStream.ToArray();
        }

    }
}
