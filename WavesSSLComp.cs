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

        public enum AttackType // Attack [0 - 5, .1 ms, .3 ms, 1 ms, 3 ms, 10 ms, 30 ms)
        {
            Attack_0_1,
            Attack_0_3,
            Attack_1,
            Attack_3,
            Attack_10,
            Attack_30
        }

        public enum ReleaseType // Release: 0 - 4, .1 s, .3 s, .6 s, 1.2 s, Auto (-1)
        {
            Release_0_1,
            Release_0_3,
            Release_0_6,
            Release_1_2,
            Release_Auto
        }

        #region Public Fields
        public float Threshold;
        public RatioType Ratio;
        public AttackType Attack;
        public ReleaseType Release;
        public float MakeupGain;
        public float RateS;
        public bool In;
        public bool Analog;
        public FadeType Fade;
        #endregion

        public WavesSSLComp()
        {
            Vst3ID = VstIDs.WavesSSLCompStereo;
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
                Attack = (AttackType)int.Parse(splittedPhrase[3]);

                // Release: 0 - 4, .1 s, .3 s, .6 s, 1.2 s, Auto
                Release = (ReleaseType)int.Parse(splittedPhrase[4]);

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

            float attack = 0;
            switch (Attack)
            {
                case AttackType.Attack_0_1:
                    attack = 0.1f;
                    break;
                case AttackType.Attack_0_3:
                    attack = 0.3f;
                    break;
                case AttackType.Attack_1:
                    attack = 1.0f;
                    break;
                case AttackType.Attack_3:
                    attack = 3.0f;
                    break;
                case AttackType.Attack_10:
                    attack = 10.0f;
                    break;
                case AttackType.Attack_30:
                    attack = 30.0f;
                    break;
            }
            sb.AppendLine(String.Format("\tAttack: {0:0.##} ms", attack));

            if (Release == ReleaseType.Release_Auto)
            {
                sb.AppendLine("\tRelease: Auto");
            }
            else
            {
                float release = 0;
                switch (Release)
                {
                    case ReleaseType.Release_0_1:
                        release = 0.1f;
                        break;
                    case ReleaseType.Release_0_3:
                        release = 0.3f;
                        break;
                    case ReleaseType.Release_0_6:
                        release = 0.6f;
                        break;
                    case ReleaseType.Release_1_2:
                        release = 1.2f;
                        break;
                    case ReleaseType.Release_Auto:
                        release = -1.0f;
                        break;
                }
                sb.AppendLine(String.Format("\tRelease: {0} s", release));
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
            sb.AppendFormat("{0} ", FormatRealWorldParameter(Threshold)); // compression threshold in dB

            // Ratio (2:1=0, 4:1=1, 10:1=2)
            sb.AppendFormat("{0} ", (int)Ratio);

            // Fade [Off=0 or *, Out=1, In=2]
            sb.AppendFormat("{0} ", (int)Fade);

            // Attack [0 - 5, .1 ms, .3 ms, 1 ms, 3 ms, 10 ms, 30 ms)
            sb.AppendFormat("{0} ", (int)Attack);

            // Release: 0 - 4, .1 s, .3 s, .6 s, 1.2 s, Auto
            sb.AppendFormat("{0} ", (int)Release);

            // Make-Up Gain (-5 - +15) dB
            sb.AppendFormat("{0} ", FormatRealWorldParameter(MakeupGain));

            // *
            sb.Append("* ");

            // Rate-S (1 - +60) seconds
            // Autofade duration. Variable from 1 to 60 seconds
            sb.AppendFormat("{0} ", FormatRealWorldParameter(RateS));

            // In
            sb.AppendFormat("{0} ", In ? "1" : "0");

            // Analog
            sb.AppendFormat("{0}", Analog ? "1" : "0");

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
                // length of the xml section until xmlPostContent including 12 bytes
                UInt32 xmlContentFullLength = (uint)xmlContent.Length + 32;
                bf.Write((UInt32)xmlContentFullLength); // 809 ?
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

            this.SetChunkData(memStream.ToArray());
        }

    }
}
