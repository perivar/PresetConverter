﻿using System.Text;
using System.Xml.Linq;
using System.Globalization;

using CommonUtils;

namespace PresetConverter
{
    /// <summary>
    /// UAD SSLChannel
    /// </summary>
    public class UADSSLChannel : VstPreset
    {
        public string FilePath;
        public string PresetName;
        public int PresetHeaderVar1 = 3;
        public int PresetHeaderVar2 = 2;

        #region Parameter Variable Names
        public float Input;      // (-20.0 dB -> 20.0 dB)
        public float Phase;      // (Normal -> Inverted)
        public float HPFreq;     // (Out -> 304 Hz)
        public float LPFreq;     // (Out -> 3.21 k)
        public float HP_LPDynSC; // (Off -> On)
        public float CompRatio;   // (1.00:1 -> Limit)
        public float CompThresh;  // (10.0 dB -> -20.0 dB)
        public float CompRelease; // (0.10 s -> 4.00 s)
        public float CompAttack;  // (Auto -> Fast)
        public float StereoLink; // (UnLink -> Link)
        public float Select;     // (Expand -> Gate 2)
        public float ExpThresh;  // (-30.0 dB -> 10.0 dB)
        public float ExpRange;   // (0.0 dB -> 40.0 dB)
        public float ExpRelease; // (0.10 s -> 4.00 s)
        public float ExpAttack;  // (Auto -> Fast)
        public float DynIn;      // (Out -> In)
        public float CompIn;     // (Out -> In)
        public float ExpIn;      // (Out -> In)
        public float LFGain;     // (-10.0 dB -> 10.0 dB)
        public float LFFreq;     // (36.1 Hz -> 355 Hz)
        public float LFBell;     // (Shelf -> Bell)
        public float LMFGain;    // (-15.6 dB -> 15.6 dB)
        public float LMFFreq;    // (251 Hz -> 2.17 k)
        public float LMFQ;       // (2.50 -> 2.50)
        public float HMFQ;       // (4.00 -> 0.40)
        public float HMFGain;    // (-16.5 dB -> 16.5 dB)
        public float HMFFreq;    // (735 Hz -> 6.77 k)
        public float HFGain;     // (-16.0 dB -> 16.1 dB)
        public float HFFreq;     // (6.93 k -> 21.7 k)
        public float HFBell;     // (Shelf -> Bell)
        public float EQIn;       // (Out -> In)
        public float EQDynSC;    // (Off -> On)
        public float PreDyn;     // (Off -> On)
        public float Output;     // (-20.0 dB -> 20.0 dB)
        public float EQType;     // (Black -> Brown)
        public float Power;      // (Off -> On)
        #endregion

        // lists to store lookup values
        Dictionary<string, List<string>> displayTextDict = new Dictionary<string, List<string>>();
        Dictionary<string, List<float>> displayNumbersDict = new Dictionary<string, List<float>>();
        Dictionary<string, List<float>> valuesDict = new Dictionary<string, List<float>>();

        public UADSSLChannel()
        {
            InitializeMappingTables("PresetConverterProject\\UADSSLChannelParametersMap.xml");
            Vst3ClassID = VstClassIDs.UADSSLEChannel;
            PlugInCategory = "Fx|Channel Strip";
            PlugInName = "UAD SSL E Channel Strip";
            PlugInVendor = "Universal Audio, Inc.";
        }

        #region FindClosest Example Methods
        /*
			var numbers = new List<float> { 10f, 20f, 22f, 30f };
			var target = 21f;

			//gets single number which is closest
			var closest = numbers.Select( n => new { n, distance = Math.Abs( n - target ) } )
				.OrderBy( p => p.distance )
				.First().n;

			//get two closest
			var take = 2;
			var closest = numbers.Select( n => new { n, distance = Math.Abs( n - target ) } )
				.OrderBy( p => p.distance )
				.Select( p => p.n )
				.Take( take );

			//gets any that are within x of target
			var within = 1;
			var withins = numbers.Select( n => new { n, distance = Math.Abs( n - target ) } )
				.Where( p => p.distance <= within )
				.Select( p => p.n );
		 */
        #endregion

        #region FindClosest and Dependent Methods
        private void InitializeMappingTables(string xmlfilename)
        {
            XDocument xmlDoc = XDocument.Load(xmlfilename);

            var entries = from entry in xmlDoc.Descendants("Entry")
                          group entry by (string)entry.Parent.Attribute("name").Value into g
                          select new
                          {
                              g.Key,
                              Value = g.ToList()
                          };

            displayTextDict = entries.ToDictionary(o => o.Key, o => o.Value.Elements("DisplayText").Select(p => p.Value).ToList());
            displayNumbersDict = entries.ToDictionary(o => o.Key, o => o.Value.Elements("DisplayNumber").Select(p => (float)GetDouble(p.Value, 0)).ToList());
            valuesDict = entries.ToDictionary(o => o.Key, o => o.Value.Elements("Value").Select(p => float.Parse(p.Value)).ToList());
        }

        /// <summary>
        /// Search for the display value that is closest to the passed parameter and return the float value
        /// </summary>
        /// <param name="paramName">parameter to search within</param>
        /// <param name="searchDisplayValue">display value (e.g. '2500' from the 2.5 kHz)</param>
        /// <returns>the float value (between 0 - 1) that corresponds to the closest match</returns>
        public float FindClosestValue(string paramName, float searchDisplayValue)
        {
            // find closest float value
            float foundClosest = displayNumbersDict[paramName].Aggregate((x, y) => Math.Abs(x - searchDisplayValue) < Math.Abs(y - searchDisplayValue) ? x : y);
            int foundIndex = displayNumbersDict[paramName].IndexOf(foundClosest);
            string foundClosestDisplayText = displayTextDict[paramName][foundIndex];
            float foundParameterValue = valuesDict[paramName][foundIndex];

            return foundParameterValue;
        }

        /// <summary>
        /// Search for the float value that is closest to the passed parameter and return the display text
        /// </summary>
        /// <param name="paramName">parameter to search within</param>
        /// <param name="searchParamValue">float value (between 0 - 1)</param>
        /// <returns>the display text that corresponds to the closest match</returns>
        public string FindClosestDisplayText(string paramName, float searchParamValue)
        {
            // find closest display text
            float foundClosest = valuesDict[paramName].Aggregate((x, y) => Math.Abs(x - searchParamValue) < Math.Abs(y - searchParamValue) ? x : y);
            int foundIndex = valuesDict[paramName].IndexOf(foundClosest);
            string foundClosestDisplayText = displayTextDict[paramName][foundIndex];

            return foundClosestDisplayText;
        }

        /// <summary>
        /// Search for the float value that is closest to the passed parameter and return the display text
        /// </summary>
        /// <param name="paramName">parameter to search within</param>
        /// <param name="searchParamValue">float value (between 0 - 1)</param>
        /// <returns>the display text that corresponds to the closest match</returns>
        public float FindClosestParameterValue(string paramName, float searchParamValue)
        {
            // find closest display text
            float foundClosest = valuesDict[paramName].Aggregate((x, y) => Math.Abs(x - searchParamValue) < Math.Abs(y - searchParamValue) ? x : y);
            int foundIndex = valuesDict[paramName].IndexOf(foundClosest);
            string foundClosestDisplayText = displayTextDict[paramName][foundIndex];
            float foundParameterValue = displayNumbersDict[paramName][foundIndex];

            return foundParameterValue;
        }

        private static double GetDouble(string value, double defaultValue)
        {
            double result;

            // Try parsing in the current culture
            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result) &&
                // Then try in US english
                !double.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result) &&
                // Then in neutral language
                !double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                result = defaultValue;
            }

            // special case
            if (Math.Abs(result) > 0 && Math.Abs(result) < 10e-10)
            {
                result = 0;
            }

            return result;
        }
        #endregion

        #region Read and Write Methods
        protected override bool PreparedForWriting()
        {
            InitCompChunkData();
            InitInfoXml();
            CalculateBytePositions();
            return true;
        }

        private void InitCompChunkData()
        {
            this.FXP = GenerateFXP(true);
            SetCompChunkData(this.FXP);
        }

        public bool ReadFXP(string filePath)
        {
            // store filepath
            FilePath = filePath;

            FXP fxp = new FXP();
            fxp.ReadFile(filePath);

            if (!ReadFXP(fxp, filePath))
            {
                return false;
            }
            return true;
        }

        public bool ReadFXP(FXP fxp, string filePath = "")
        {
            if (fxp == null || fxp.Content == null) return false;

            // store filepath
            FilePath = filePath;

            var byteArray = new byte[0];
            if (fxp.Content is FXP.FxProgramSet)
            {
                byteArray = ((FXP.FxProgramSet)fxp.Content).ChunkData;
            }
            else if (fxp.Content is FXP.FxChunkSet)
            {
                byteArray = ((FXP.FxChunkSet)fxp.Content).ChunkData;
            }

            var bFile = new BinaryFile(byteArray, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII);

            // Read UAD Preset Header information
            PresetHeaderVar1 = bFile.ReadInt32();
            PresetHeaderVar2 = bFile.ReadInt32();
            PresetName = bFile.ReadString(32).Trim('\0');

            // Read Parameters
            Input = bFile.ReadSingle();
            Phase = bFile.ReadSingle();
            HPFreq = bFile.ReadSingle();
            LPFreq = bFile.ReadSingle();
            HP_LPDynSC = bFile.ReadSingle();
            CompRatio = bFile.ReadSingle();
            CompThresh = bFile.ReadSingle();
            CompRelease = bFile.ReadSingle();
            CompAttack = bFile.ReadSingle();
            StereoLink = bFile.ReadSingle();
            Select = bFile.ReadSingle();
            ExpThresh = bFile.ReadSingle();
            ExpRange = bFile.ReadSingle();
            ExpRelease = bFile.ReadSingle();
            ExpAttack = bFile.ReadSingle();
            DynIn = bFile.ReadSingle();
            CompIn = bFile.ReadSingle();
            ExpIn = bFile.ReadSingle();
            LFGain = bFile.ReadSingle();
            LFFreq = bFile.ReadSingle();
            LFBell = bFile.ReadSingle();
            LMFGain = bFile.ReadSingle();
            LMFFreq = bFile.ReadSingle();
            LMFQ = bFile.ReadSingle();
            HMFQ = bFile.ReadSingle();
            HMFGain = bFile.ReadSingle();
            HMFFreq = bFile.ReadSingle();
            HFGain = bFile.ReadSingle();
            HFFreq = bFile.ReadSingle();
            HFBell = bFile.ReadSingle();
            EQIn = bFile.ReadSingle();
            EQDynSC = bFile.ReadSingle();
            PreDyn = bFile.ReadSingle();
            Output = bFile.ReadSingle();
            EQType = bFile.ReadSingle();
            Power = bFile.ReadSingle();

            return true;
        }

        public bool WriteFXP(string filePath)
        {
            FXP fxp = GenerateFXP(false);
            fxp.Write(filePath);
            return true;
        }

        private FXP GenerateFXP(bool isBank)
        {
            FXP.FxContent fxpContent;
            FXP fxp = new FXP();
            if (isBank)
            {
                // FBCh = FXB (bank)
                fxpContent = new FXP.FxChunkSet();
                ((FXP.FxChunkSet)fxpContent).NumPrograms = 1; // I.e. number of programs (number of presets in one file)
                ((FXP.FxChunkSet)fxpContent).Future = new string('\0', 128); // 128 bytes long
            }
            else
            {
                // FPCh = FXP (preset)
                fxpContent = new FXP.FxProgramSet();
                ((FXP.FxProgramSet)fxpContent).NumPrograms = 1; // I.e. number of programs (number of presets in one file)
                ((FXP.FxProgramSet)fxpContent).Name = PresetName;
            }

            fxp.Content = fxpContent;
            fxpContent.ChunkMagic = "CcnK";
            fxpContent.ByteSize = 0; // will be set correctly by FXP class

            // Preset (Program) (.fxp) with chunk (magic = 'FPCh')
            fxpContent.FxMagic = isBank ? "FBCh" : "FPCh";
            fxpContent.Version = 1; //isBank ? 2 : 1; // Format Version (should be 1)
            fxpContent.FxID = "J9AU";
            fxpContent.FxVersion = 1;

            byte[] chunkData = GetChunkData(fxpContent.FxMagic);

            if (fxp.Content is FXP.FxProgramSet)
            {
                ((FXP.FxProgramSet)fxp.Content).ChunkSize = chunkData.Length;
                ((FXP.FxProgramSet)fxp.Content).ChunkData = chunkData;
            }
            else if (fxp.Content is FXP.FxChunkSet)
            {
                ((FXP.FxChunkSet)fxp.Content).ChunkSize = chunkData.Length;
                ((FXP.FxChunkSet)fxp.Content).ChunkData = chunkData;
            }

            return fxp;
        }

        private byte[] GetChunkData(string fxMagic)
        {
            var memStream = new MemoryStream();
            var bf = new BinaryFile(memStream, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII);

            if (fxMagic.Equals("FBCh"))
            {
                bf.Write((uint)3);
                bf.Write((uint)0);
                bf.Write((uint)32);
            }

            // Write UAD Preset Header information
            bf.Write((int)PresetHeaderVar1);
            bf.Write((int)PresetHeaderVar2);
            bf.Write(PresetName, 32);

            // Write Parameters
            bf.Write((float)Input); // (-20.0 dB -> 20.0 dB)
            bf.Write((float)Phase); // (Normal -> Inverted)
            bf.Write((float)HPFreq); // (Out -> 304 Hz)
            bf.Write((float)LPFreq); // (Out -> 3.21 k)
            bf.Write((float)HP_LPDynSC); // (Off -> On)
            bf.Write((float)CompRatio); // (1.00:1 -> Limit)
            bf.Write((float)CompThresh); // (10.0 dB -> -20.0 dB)
            bf.Write((float)CompRelease); // (0.10 s -> 4.00 s)
            bf.Write((float)CompAttack); // (Auto -> Fast)
            bf.Write((float)StereoLink); // (UnLink -> Link)
            bf.Write((float)Select); // (Expand -> Gate 2)
            bf.Write((float)ExpThresh); // (-30.0 dB -> 10.0 dB)
            bf.Write((float)ExpRange); // (0.0 dB -> 40.0 dB)
            bf.Write((float)ExpRelease); // (0.10 s -> 4.00 s)
            bf.Write((float)ExpAttack); // (Auto -> Fast)
            bf.Write((float)DynIn); // (Out -> In)
            bf.Write((float)CompIn); // (Out -> In)
            bf.Write((float)ExpIn); // (Out -> In)
            bf.Write((float)LFGain); // (-10.0 dB -> 10.0 dB)
            bf.Write((float)LFFreq); // (36.1 Hz -> 355 Hz)
            bf.Write((float)LFBell); // (Shelf -> Bell)
            bf.Write((float)LMFGain); // (-15.6 dB -> 15.6 dB)
            bf.Write((float)LMFFreq); // (251 Hz -> 2.17 k)
            bf.Write((float)LMFQ); // (2.50 -> 2.50)
            bf.Write((float)HMFQ); // (4.00 -> 0.40)
            bf.Write((float)HMFGain); // (-16.5 dB -> 16.5 dB)
            bf.Write((float)HMFFreq); // (735 Hz -> 6.77 k)
            bf.Write((float)HFGain); // (-16.0 dB -> 16.1 dB)
            bf.Write((float)HFFreq); // (6.93 k -> 21.7 k)
            bf.Write((float)HFBell); // (Shelf -> Bell)
            bf.Write((float)EQIn); // (Out -> In)
            bf.Write((float)EQDynSC); // (Off -> On)
            bf.Write((float)PreDyn); // (Off -> On)
            bf.Write((float)Output); // (-20.0 dB -> 20.0 dB)
            bf.Write((float)EQType); // (Black -> Brown)
            bf.Write((float)Power); // (Off -> On)

            byte[] chunkData = memStream.ToArray();
            memStream.Close();

            return chunkData;
        }
        #endregion

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("PresetName: {0}", PresetName));
            sb.Append("Input:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", Input).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("Input", Input), "-20.0 dB -> 20.0 dB");
            sb.Append("Phase:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", Phase).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("Phase", Phase), "Normal -> Inverted");
            sb.Append("HP Freq:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", HPFreq).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("HP Freq", HPFreq), "Out -> 304 Hz");
            sb.Append("LP Freq:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", LPFreq).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("LP Freq", LPFreq), "Out -> 3.21 k");
            sb.Append("HP/LP Dyn SC:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", HP_LPDynSC).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("HP/LP Dyn SC", HP_LPDynSC), "Off -> On");
            sb.Append("CMP Ratio:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", CompRatio).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("CMP Ratio", CompRatio), "1.00:1 -> Limit");
            sb.Append("CMP Thresh:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", CompThresh).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("CMP Thresh", CompThresh), "10.0 dB -> -20.0 dB");
            sb.Append("CMP Release:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", CompRelease).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("CMP Release", CompRelease), "0.10 s -> 4.00 s");
            sb.Append("CMP Attack:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", CompAttack).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("CMP Attack", CompAttack), "Auto -> Fast");
            sb.Append("Stereo Link:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", StereoLink).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("Stereo Link", StereoLink), "UnLink -> Link");
            sb.Append("Select:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", Select).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("Select", Select), "Expand -> Gate 2");
            sb.Append("EXP Thresh:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", ExpThresh).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("EXP Thresh", ExpThresh), "-30.0 dB -> 10.0 dB");
            sb.Append("EXP Range:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", ExpRange).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("EXP Range", ExpRange), "0.0 dB -> 40.0 dB");
            sb.Append("EXP Release:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", ExpRelease).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("EXP Release", ExpRelease), "0.10 s -> 4.00 s");
            sb.Append("EXP Attack:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", ExpAttack).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("EXP Attack", ExpAttack), "Auto -> Fast");
            sb.Append("DYN In:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", DynIn).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("DYN In", DynIn), "Out -> In");
            sb.Append("Comp In:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", CompIn).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("Comp In", CompIn), "Out -> In");
            sb.Append("Exp In:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", ExpIn).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("Exp In", ExpIn), "Out -> In");
            sb.Append("LF Gain:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", LFGain).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("LF Gain", LFGain), "-10.0 dB -> 10.0 dB");
            sb.Append("LF Freq:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", LFFreq).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("LF Freq", LFFreq), "36.1 Hz -> 355 Hz");
            sb.Append("LF Bell:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", LFBell).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("LF Bell", LFBell), "Shelf -> Bell");
            sb.Append("LMF Gain:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", LMFGain).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("LMF Gain", LMFGain), "-15.6 dB -> 15.6 dB");
            sb.Append("LMF Freq:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", LMFFreq).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("LMF Freq", LMFFreq), "251 Hz -> 2.17 k");
            sb.Append("LMF Q:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", LMFQ).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("LMF Q", LMFQ), "2.50 -> 2.50");
            sb.Append("HMF Q:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", HMFQ).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("HMF Q", HMFQ), "4.00 -> 0.40");
            sb.Append("HMF Gain:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", HMFGain).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("HMF Gain", HMFGain), "-16.5 dB -> 16.5 dB");
            sb.Append("HMF Freq:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", HMFFreq).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("HMF Freq", HMFFreq), "735 Hz -> 6.77 k");
            sb.Append("HF Gain:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", HFGain).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("HF Gain", HFGain), "-16.0 dB -> 16.1 dB");
            sb.Append("HF Freq:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", HFFreq).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("HF Freq", HFFreq), "6.93 k -> 21.7 k");
            sb.Append("HF Bell:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", HFBell).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("HF Bell", HFBell), "Shelf -> Bell");
            sb.Append("EQ In:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", EQIn).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("EQ In", EQIn), "Out -> In");
            sb.Append("EQ Dyn SC:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", EQDynSC).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("EQ Dyn SC", EQDynSC), "Off -> On");
            sb.Append("Pre Dyn:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", PreDyn).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("Pre Dyn", PreDyn), "Off -> On");
            sb.Append("Output:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", Output).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("Output", Output), "-20.0 dB -> 20.0 dB");
            sb.Append("EQ Type:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", EQType).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("EQ Type", EQType), "Black -> Brown");
            sb.Append("Power:".PadRight(15)).AppendFormat(string.Format("{0:0.00}", Power).PadRight(5)).AppendFormat("= {0} ({1})\n", FindClosestDisplayText("Power", Power), "Off -> On");
            return sb.ToString();
        }
    }
}
