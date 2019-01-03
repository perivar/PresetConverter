using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommonUtils;
using Serilog;

namespace PresetConverter
{
    /// <summary>
    /// Preset Class for reading and writing a Fabfilter Pro Q Preset file
    /// </summary>
    public class FabfilterProQ : FabfilterProQBase
    {
        public List<ProQBand> Bands { get; set; }
        public int Version { get; set; }                // Normally 2
        public int ParameterCount { get; set; }         // Normally 190

        // Post Band Parameters
        public float OutputGain { get; set; }           // -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
        public float OutputPan { get; set; }            // -1 to 1 (0 = middle)
        public float DisplayRange { get; set; }         // 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
        public float ProcessMode { get; set; }          // 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
        public float ChannelMode { get; set; }          // 0 = Left/Right, 1 = Mid/Side
        public float Bypass { get; set; }               // 0 = No bypass
        public float ReceiveMidi { get; set; }          // 0 = Enabled?
        public float Analyzer { get; set; }             // 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
        public float AnalyzerResolution { get; set; }   // 0 - 3 : low - medium[x] - high - maximum
        public float AnalyzerSpeed { get; set; }        // 0 - 3 : very slow, slow, medium[x], fast
        public float SoloBand { get; set; }        	    // -1

        public FabfilterProQ()
        {
            Version = 2;

            Vst3ID = VstIDs.FabFilterProQ;
            PlugInCategory = "Fx|EQ";
            PlugInName = "FabFilter Pro-Q";
            PlugInVendor = "FabFilter";
        }

        public static string ToString(float[] parameters)
        {
            using (var sw = new StringWriter())
            {
                int counter = 0;
                foreach (var f in parameters)
                {
                    sw.WriteLine("{0:0.0000}", f);
                    counter++;
                    if ((counter - 1) % 7 == 0) sw.WriteLine();
                }
                return sw.ToString();
            }
        }

        public static float[] Convert2FabfilterProQFloats(float[] ieeeFloatParameters)
        {
            var floatList = new List<float>();
            int counter = 0;

            // How many bands are enabled?
            floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 24));          // Number of active bands

            for (int i = 0; i < 24; i++)
            {
                floatList.Add(IEEEFloatToFrequencyFloat(ieeeFloatParameters[counter++]));                           // FilterFreq: value range 10.0 -> 30000.0 Hz
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, -30, 30));    // FilterGain: + or - value in dB
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));       // FilterQ: value range 0.025 -> 40.00
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 4));       // filter type: 0 - 5 (seems to be a bug that cuts off the notch filter, so only 0 - 4?!)
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 3));       // filter slope: 0 - 3
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 2));       // stereo placement: 0 = Left, 1 = Right, 2 = Stereo
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));       // unknown: always 1.0?
            }

            // convert the remaining floats
            try
            {
                // OutputGain: -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, -1, 1));
                // OutputPan: -1 to 1 (0 = middle)
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, -1, 1));
                // DisplayRange: 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 3));
                // ProcessMode: 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 4));
                // ChannelMode: 0 = Left/Right, 1 = Mid/Side
                floatList.Add(ieeeFloatParameters[counter++]);
                // Bypass: 0 = No bypass
                floatList.Add(ieeeFloatParameters[counter++]);
                // ReceiveMidi: 0 = Enabled?
                floatList.Add(ieeeFloatParameters[counter++]);
                // Analyzer: 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 3));
                // AnalyzerResolution: 0 - 3 (low - medium[x] - high - maximum)
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 3));
                // AnalyzerSpeed: 0 - 3 (very slow, slow, medium[x], fast)
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 3));
                // SoloBand: -1
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, -1, 1));
            }
            catch { }

            return floatList.ToArray();
        }

        public static FabfilterProQ Convert2FabfilterProQ(float[] floatParameters, bool isIEEE = true)
        {
            var preset = new FabfilterProQ();
            preset.InitFromParameters(floatParameters, isIEEE);
            return preset;
        }


        public bool ReadFFP(string filePath)
        {
            BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian);

            string header = binFile.ReadString(4);
            if (header != "FPQr") return false;

            Version = binFile.ReadInt32();
            ParameterCount = binFile.ReadInt32();

            // Read in how many bands are enabled
            var numActiveBands = binFile.ReadSingle();

            Bands = new List<ProQBand>();
            for (int i = 0; i < 24; i++)
            {
                var band = new ProQBand();

                band.Frequency = FreqConvertBack(binFile.ReadSingle());
                band.Gain = binFile.ReadSingle(); // actual gain in dB
                band.Q = QConvertBack(binFile.ReadSingle());

                // 0 - 5
                var filterType = binFile.ReadSingle();
                switch (filterType)
                {
                    case (float)ProQShape.Bell:
                        band.Shape = ProQShape.Bell;
                        break;
                    case (float)ProQShape.LowShelf:
                        band.Shape = ProQShape.LowShelf;
                        break;
                    case (float)ProQShape.LowCut:
                        band.Shape = ProQShape.LowCut;
                        break;
                    case (float)ProQShape.HighShelf:
                        band.Shape = ProQShape.HighShelf;
                        break;
                    case (float)ProQShape.HighCut:
                        band.Shape = ProQShape.HighCut;
                        break;
                    case (float)ProQShape.Notch:
                        band.Shape = ProQShape.Notch;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter type is outside range: {0}", filterType));
                }

                // 0 = 6 dB/oct, 1 = 12 dB/oct, 2 = 24 dB/oct, 3 = 48 dB/oct
                var filterSlope = binFile.ReadSingle();
                switch (filterSlope)
                {
                    case (float)ProQLPHPSlope.Slope6dB_oct:
                        band.LPHPSlope = ProQLPHPSlope.Slope6dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope12dB_oct:
                        band.LPHPSlope = ProQLPHPSlope.Slope12dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope24dB_oct:
                        band.LPHPSlope = ProQLPHPSlope.Slope24dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope48dB_oct:
                        band.LPHPSlope = ProQLPHPSlope.Slope48dB_oct;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter slope is outside range: {0}", filterSlope));
                }

                // 0 = Left, 1 = Right, 2 = Stereo
                var filterStereoPlacement = binFile.ReadSingle();
                switch (filterStereoPlacement)
                {
                    case (float)ProQStereoPlacement.LeftOrMid:
                        band.StereoPlacement = ProQStereoPlacement.LeftOrMid;
                        break;
                    case (float)ProQStereoPlacement.RightOrSide:
                        band.StereoPlacement = ProQStereoPlacement.RightOrSide;
                        break;
                    case (float)ProQStereoPlacement.Stereo:
                        band.StereoPlacement = ProQStereoPlacement.Stereo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter stereo placement is outside range: {0}", filterStereoPlacement));
                }

                // always 1.0 ?
                var unknown = binFile.ReadSingle();

                // check if band is enabled
                if (numActiveBands > 0 && numActiveBands > i) band.Enabled = true;

                Bands.Add(band);
            }

            // read the remaining floats
            try
            {
                OutputGain = binFile.ReadSingle();           // -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
                OutputPan = binFile.ReadSingle();            // -1 to 1 (0 = middle)
                DisplayRange = binFile.ReadSingle();         // 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
                ProcessMode = binFile.ReadSingle();          // 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
                ChannelMode = binFile.ReadSingle();          // 0 = Left/Right, 1 = Mid/Side
                Bypass = binFile.ReadSingle();               // 0 = No bypass
                ReceiveMidi = binFile.ReadSingle();          // 0 = Enabled?
                Analyzer = binFile.ReadSingle();             // 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
                AnalyzerResolution = binFile.ReadSingle();   // 0 - 3 : low - medium[x] - high - maximum
                AnalyzerSpeed = binFile.ReadSingle();        // 0 - 3 : very slow, slow, medium[x], fast
                SoloBand = binFile.ReadSingle();             // -1
            }
            catch { }

            // check if mid/side
            if (ChannelMode == 1)
            {
                Bands.ForEach(b => b.ChannelMode = ProQChannelMode.MidSide);
            }

            binFile.Close();

            return true;
        }

        public override string ToString()
        {
            var writer = new StringWriter();
            writer.WriteLine(string.Format("Vst3ID: {0}", this.Vst3ID));

            writer.WriteLine("Bands:");
            foreach (var band in this.Bands)
            {
                writer.WriteLine(band.ToString());
            }

            writer.WriteLine();
            writer.WriteLine("PostPresetParameters:");
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "OutputGain", OutputGain, "-1 to 1 (- Infinity to +36 dB , 0 = 0 dB)"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "OutputPan", OutputPan, "-1 to 1 (0 = middle)"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "DisplayRange", DisplayRange, "0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "ProcessMode", ProcessMode, "0 = zero latency, 1 = lin.phase.low - medium - high - maximum"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "ChannelMode", ChannelMode, "0 = Left/Right, 1 = Mid/Side"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "Bypass", Bypass, "0 = No bypass"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "ReceiveMidi", ReceiveMidi, "0 = Enabled?"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "Analyzer", Analyzer, "0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "AnalyzerResolution", AnalyzerResolution, "0 - 3 (low - medium[x] - high - maximum)"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "AnalyzerSpeed", AnalyzerSpeed, "0 - 3 (very slow, slow, medium[x], fast)"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "SoloBand", SoloBand, "-1"));

            return writer.ToString();
        }

        public override bool WriteFFP(string filePath)
        {
            using (BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian, true))
            {
                binFile.Write("FPQr");
                binFile.Write((int)Version);
                binFile.Write(GetBandsContent());
            }

            return true;
        }

        private byte[] GetBandsContent()
        {
            var memStream = new MemoryStream();
            using (BinaryFile binFile = new BinaryFile(memStream, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII))
            {
                binFile.Write((int)Bands.Count * 7 + 12);

                // How many bands are enabled?
                var enabledBandCount = Bands.Count(b => b.Enabled);
                binFile.Write((float)enabledBandCount);

                for (int i = 0; i < 24; i++)
                {
                    if (i < Bands.Count)
                    {
                        binFile.Write((float)FabfilterProQ.FreqConvert(Bands[i].Frequency));
                        binFile.Write((float)Bands[i].Gain);
                        binFile.Write((float)FabfilterProQ.QConvert(Bands[i].Q));
                        binFile.Write((float)Bands[i].Shape);
                        binFile.Write((float)Bands[i].LPHPSlope);
                        binFile.Write((float)Bands[i].StereoPlacement);
                        binFile.Write((float)1);
                    }
                    else
                    {
                        binFile.Write((float)FabfilterProQ.FreqConvert(1000));
                        binFile.Write((float)0);
                        binFile.Write((float)FabfilterProQ.QConvert(1));
                        binFile.Write((float)ProQShape.Bell);
                        binFile.Write((float)ProQLPHPSlope.Slope24dB_oct);
                        binFile.Write((float)ProQStereoPlacement.Stereo);
                        binFile.Write((float)1);
                    }
                }

                binFile.Write((float)OutputGain);           // -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
                binFile.Write((float)OutputPan);            // -1 to 1 (0 = middle)
                binFile.Write((float)DisplayRange);         // 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
                binFile.Write((float)ProcessMode);          // 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
                binFile.Write((float)ChannelMode);          // 0 = Left/Right, 1 = Mid/Side
                binFile.Write((float)Bypass);               // 0 = No bypass
                binFile.Write((float)ReceiveMidi);          // 0 = Enabled?
                binFile.Write((float)Analyzer);             // 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
                binFile.Write((float)AnalyzerResolution);   // float ;  // 0 - 3 : low - medium[x] - high - maximum
                binFile.Write((float)AnalyzerSpeed);        // 0 - 3 : very slow, slow, medium[x], fast
                binFile.Write((float)SoloBand);             // -1                   
            }

            return memStream.ToArray();
        }

        protected override bool PreparedForWriting()
        {
            InitCompChunkData();
            InitInfoXml();
            CalculateBytePositions();
            return true;
        }

        public void InitCompChunkData()
        {
            if (HasFXP)
            {
                SetCompChunkData(this.FXP);
            }
            else
            {
                var memStream = new MemoryStream();
                using (BinaryFile binFile = new BinaryFile(memStream, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII))
                {
                    binFile.Write("FabF");
                    binFile.Write((UInt32)Version);

                    var presetName = GetStringParameter("PresetName");
                    if (presetName == null)
                    {
                        presetName = "Default Setting";
                    }
                    binFile.Write((UInt32)presetName.Length);
                    binFile.Write(presetName);

                    binFile.Write((UInt32)0); // unknown

                    binFile.Write(GetBandsContent());
                }

                this.CompChunkData = memStream.ToArray();
            }
        }

        public void InitFromParameters()
        {
            if (HasFXP)
            {
                var fxp = FXP;

                if (fxp.Content is FXP.FxSet)
                {
                    var set = (FXP.FxSet)fxp.Content;

                    // only use the parameters from the first program
                    if (set.NumPrograms > 0)
                    {
                        var program = set.Programs[0];
                        var parameters = program.Parameters;

                        // Note that the floats are stored as IEEE (meaning between 0.0 - 1.0)
                        InitFromParameters(parameters);

                        // and set the correct params
                        PlugInCategory = "Fx";
                        PlugInName = "FabFilter Pro-Q x64";
                        PlugInVendor = "FabFilter";
                    }
                }
            }
            else
            {
                // init preset parameters
                // Note that the floats are not stored as IEEE (meaning between 0.0 - 1.0) but as floats representing the real values 
                var fabFilterProQFloats = Parameters
                                            .Where(v => v.Value.Type == Parameter.ParameterType.Number)
                                            .Select(v => (float)v.Value.Number.Value).ToArray();
                InitFromParameters(fabFilterProQFloats, false);
            }
        }

        /// <summary>
        /// Initialize the class specific variables using float parameters
        /// </summary>
        public void InitFromParameters(float[] floatParameters, bool isIEEE = true)
        {
            this.Bands = new List<ProQBand>();

            float[] floatArray;
            if (isIEEE)
            {
                // convert the ieee float parameters to fabfilter floats
                floatArray = Convert2FabfilterProQFloats(floatParameters);
            }
            else
            {
                floatArray = floatParameters;
            }

            int index = 0;

            // Read in how many bands are enabled
            var numActiveBands = floatArray[index++]; // Number of active bands

            for (int i = 0; i < 24; i++)
            {
                var band = new ProQBand();

                band.Frequency = FabfilterProQ.FreqConvertBack(floatArray[index++]);
                band.Gain = floatArray[index++]; // actual gain in dB
                band.Q = FabfilterProQ.QConvertBack(floatArray[index++]);

                // filter type: 0 - 5
                var filterType = floatArray[index++];
                switch (filterType)
                {
                    case (float)ProQShape.Bell:
                        band.Shape = ProQShape.Bell;
                        break;
                    case (float)ProQShape.LowShelf:
                        band.Shape = ProQShape.LowShelf;
                        break;
                    case (float)ProQShape.LowCut:
                        band.Shape = ProQShape.LowCut;
                        break;
                    case (float)ProQShape.HighShelf:
                        band.Shape = ProQShape.HighShelf;
                        break;
                    case (float)ProQShape.HighCut:
                        band.Shape = ProQShape.HighCut;
                        break;
                    case (float)ProQShape.Notch:
                        band.Shape = ProQShape.Notch;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter type is outside range: {0}", filterType));
                }

                // filterSlope: 0 - 3
                var filterSlope = floatArray[index++];
                switch (filterSlope)
                {
                    case (float)ProQLPHPSlope.Slope6dB_oct:
                        band.LPHPSlope = ProQLPHPSlope.Slope6dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope12dB_oct:
                        band.LPHPSlope = ProQLPHPSlope.Slope12dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope24dB_oct:
                        band.LPHPSlope = ProQLPHPSlope.Slope24dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope48dB_oct:
                        band.LPHPSlope = ProQLPHPSlope.Slope48dB_oct;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter slope is outside range: {0}", filterSlope));
                }

                // stereo placement: 0 = Left, 1 = Right, 2 = Stereo
                var filterStereoPlacement = floatArray[index++];
                switch (filterStereoPlacement)
                {
                    case (float)ProQStereoPlacement.LeftOrMid:
                        band.StereoPlacement = ProQStereoPlacement.LeftOrMid;
                        break;
                    case (float)ProQStereoPlacement.RightOrSide:
                        band.StereoPlacement = ProQStereoPlacement.RightOrSide;
                        break;
                    case (float)ProQStereoPlacement.Stereo:
                        band.StereoPlacement = ProQStereoPlacement.Stereo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter stereo placement is outside range: {0}", filterStereoPlacement));
                }

                // enabled band: always 1.0
                var unknown = floatArray[index++];

                // check if band is enabled
                if (numActiveBands > 0 && numActiveBands > i) band.Enabled = true;

                this.Bands.Add(band);
            }

            // read the remaining floats
            try
            {
                this.OutputGain = floatArray[index++];           // -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
                this.OutputPan = floatArray[index++];            // -1 to 1 (0 = middle)
                this.DisplayRange = floatArray[index++];         // 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
                this.ProcessMode = floatArray[index++];          // 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
                this.ChannelMode = floatArray[index++];          // 0 = Left/Right, 1 = Mid/Side
                this.Bypass = floatArray[index++];               // 0 = No bypass
                this.ReceiveMidi = floatArray[index++];          // 0 = Enabled?
                this.Analyzer = floatArray[index++];             // 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
                this.AnalyzerResolution = floatArray[index++];   // 0 - 3 : low - medium[x] - high - maximum
                this.AnalyzerSpeed = floatArray[index++];        // 0 - 3 : very slow, slow, medium[x], fast
                this.SoloBand = floatArray[index++];             // -1
            }
            catch { }

            // check if mid/side
            if (this.ChannelMode == 1)
            {
                this.Bands.ForEach(b => b.ChannelMode = ProQChannelMode.MidSide);
            }
        }
    }

    public enum ProQShape
    {
        Bell = 0, // (default)
        LowShelf = 1,
        LowCut = 2,
        HighShelf = 3,
        HighCut = 4,
        Notch = 5,
    }

    public enum ProQLPHPSlope
    {
        Slope6dB_oct = 0,
        Slope12dB_oct = 1,
        Slope24dB_oct = 2, // (default)
        Slope48dB_oct = 3,
    }

    public enum ProQStereoPlacement
    {
        LeftOrMid = 0,
        RightOrSide = 1,
        Stereo = 2, // (default)
    }

    public enum ProQChannelMode
    {
        LeftRight = 0,
        MidSide = 1
    }

    public class ProQBand
    {
        public ProQChannelMode ChannelMode { get; set; }
        public double Frequency { get; set; }      // value range 10.0 -> 30000.0 Hz
        public double Gain { get; set; }      // + or - value in dB
        public double Q { get; set; }         // value range 0.025 -> 40.00
        public ProQShape Shape { get; set; }
        public ProQLPHPSlope LPHPSlope { get; set; }
        public ProQStereoPlacement StereoPlacement { get; set; }
        public bool Enabled { get; set; }

        public override string ToString()
        {
            string stereoPlacement = "";
            switch (StereoPlacement)
            {
                case ProQStereoPlacement.LeftOrMid:
                    if (ChannelMode == ProQChannelMode.LeftRight)
                    {
                        stereoPlacement = "Left";
                    }
                    else
                    {
                        stereoPlacement = "Mid";
                    }
                    break;
                case ProQStereoPlacement.RightOrSide:
                    if (ChannelMode == ProQChannelMode.LeftRight)
                    {
                        stereoPlacement = "Right";
                    }
                    else
                    {
                        stereoPlacement = "Side";
                    }
                    break;
                case ProQStereoPlacement.Stereo:
                    stereoPlacement = "Stereo";
                    break;
            }

            return String.Format("[{4,-3}] {0}: {1:0.00} Hz, {2:0.00} dB, Q: {3:0.00}, {5}, {6}", Shape, Frequency, Gain, Q, Enabled == true ? "On" : "Off", LPHPSlope, stereoPlacement);
        }
    }
}
