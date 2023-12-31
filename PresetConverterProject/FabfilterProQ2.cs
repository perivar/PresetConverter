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
    /// Preset Class for reading and writing a Fabfilter Pro Q 2 Preset file
    /// </summary>
    public class FabfilterProQ2 : FabfilterProQBase
    {
        public List<ProQ2Band> Bands { get; set; }
        public int Version { get; set; }                        // Normally 2
        public int ParameterCount { get; set; }                 // Normally 190

        // Post Band Parameters
        public float ProcessingMode { get; set; }               // Zero Latency: 0.0, Natural Phase: 1.0, Linear Phase: 2.0
        public float ProcessingResolution { get; set; }         // Medium
        public float ChannelMode { get; set; }                  // 0 = Left/Right, 1 = Mid/Side
        public float GainScale { get; set; }                    // 100%
        public float OutputLevel { get; set; }                  // 0.0 dB, -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
        public float OutputPan { get; set; }                    // Left 0 dB, Right: 0 dB, -1 to 1 (0 = middle)
        public float ByPass { get; set; }                       // Not Bypassed
        public float OutputInvertPhase { get; set; }            // Normal
        public float AutoGain { get; set; }                     // Off
        public float AnalyzerShowPreProcessing { get; set; }    // Disabled - 0: Off, 1: On
        public float AnalyzerShowPostProcessing { get; set; }   // Disabled - 0: Off, 1: On
        public float AnalyzerShowSidechain { get; set; }        // Disabled - 0: Off, 1: On
        public float AnalyzerRange { get; set; }                // Analyzer Range in dB. 0.0: 60dB, 1.0: 90dB, 2.0: 120dB
        public float AnalyzerResolution { get; set; }           // Analyzer Resolution. 0.0: Low, 1.0: Medium, 2.0: High, 3.00: Maximum  
        public float AnalyzerSpeed { get; set; }                // Analyzer Speed. 0.0: Very Slow, 1.0: Slow, 2.0: Medium, 3.0 Fast, 4.0: Very Fast
        public float AnalyzerTilt { get; set; }                 // Analyzer Tilt in dB/oct. 0.0: 0.0, 1.0: 1.5, 2.0: 3.0, 3.0: 4.5, 4.0: 6.0  
        public float AnalyzerFreeze { get; set; }               // 0: Off, 1: On
        public float SpectrumGrab { get; set; }                 // Enabled
        public float DisplayRange { get; set; }                 // 12dB
        public float ReceiveMidi { get; set; }                  // Enabled
        public float SoloBand { get; set; }                     // -1
        public float SoloGain { get; set; }                     // 0.00

        // Ignore the Ex fields
        // public float ExAutoGain { get; set; }                   // (Other): 0.00

        public FabfilterProQ2()
        {
            Version = 2;

            Vst3ClassID = VstClassIDs.FabFilterProQ2;
            PlugInCategory = "Fx|EQ";
            PlugInName = "FabFilter Pro-Q 2";
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
                    if (counter % 7 == 0) sw.WriteLine();
                }
                return sw.ToString();
            }
        }

        public static float[] Convert2FabfilterProQ2Floats(float[] ieeeFloatParameters)
        {
            var floatList = new List<float>();
            int counter = 0;
            for (int i = 0; i < 24; i++)
            {
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 2));       // 1 = Enabled, 2 = Disabled
                floatList.Add(IEEEFloatToFrequencyFloat(ieeeFloatParameters[counter++]));                           // FilterFreq: value range 10.0 -> 30000.0 Hz
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, -30, 30));    // FilterGain: + or - value in dB
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));       // FilterQ: value range 0.025 -> 40.00
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 7));       // filter type: 0 - 7
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 8));       // filter slope: 0 - 8
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 2));       // stereo placement: 0 = Left, 1 = Right, 2 = Stereo
            }

            // convert the remaining floats
            try
            {
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 2));   // ProcessingMode: Zero Latency: 0.0, Natural Phase: 1.0, Linear Phase: 2.0
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 4));   // ProcessingResolution: 0 - 4, Medium
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));   // ChannelMode: 0 = Left/Right, 1 = Mid/Side
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 2));   // GainScale: 100%
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, -1, 1));  // OutputLevel: 0.0 dB, -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, -1, 1));  // OutputPan: Left 0 dB, Right: 0 dB, -1 to 1 (0 = middle)
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));   // ByPass: Not Bypassed
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));   // OutputInvertPhase: Normal
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));   // AutoGain: Off
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));   // AnalyzerShowPreProcessing: Disabled - 0: Off, 1: On
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));   // AnalyzerShowPostProcessing: Disabled - 0: Off, 1: On
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));   // AnalyzerShowSidechain: Disabled - 0: Off, 1: On
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 2));   // AnalyzerRange: Analyzer Range in dB. 0.0: 60dB, 1.0: 90dB, 2.0: 120dB
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 3));   // AnalyzerResolution: Analyzer Resolution. 0.0: Low, 1.0: Medium, 2.0: High, 3.00: Maximum  
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 4));   // AnalyzerSpeed: Analyzer Speed. 0.0: Very Slow, 1.0: Slow, 2.0: Medium, 3.0 Fast, 4.0: Very Fast
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 4));   // AnalyzerTilt: Analyzer Tilt in dB/oct. 0.0: 0.0, 1.0: 1.5, 2.0: 3.0, 3.0: 4.5, 4.0: 6.0
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));   // AnalyzerFreeze: 0: Off, 1: On
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));   // SpectrumGrab: Enabled
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 3));   // DisplayRange: 12dB
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));   // ReceiveMidi: Enabled
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, -1, 1));  // SoloBand: -1, -1 to 1 
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, -1, 1));  // SoloGain: 0.00, -1 to 1 
            }
            catch { }

            return floatList.ToArray();

        }

        public static FabfilterProQ2 Convert2FabfilterProQ2(float[] floatParameters, bool isIEEE = true)
        {
            var preset = new FabfilterProQ2();
            preset.InitFromParameters(floatParameters, isIEEE);
            return preset;
        }

        public bool ReadFFP(string filePath)
        {
            BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian);

            string header = binFile.ReadString(4);
            if (header != "FQ2p") return false;

            Version = (int)binFile.ReadUInt32();
            ParameterCount = (int)binFile.ReadUInt32();

            Bands = new List<ProQ2Band>();
            for (int i = 0; i < 24; i++)
            {
                var band = new ProQ2Band();

                // 1 = Enabled, 2 = Disabled
                band.Enabled = binFile.ReadSingle() == 1 ? true : false;

                band.Frequency = FreqConvertBack(binFile.ReadSingle());
                band.Gain = binFile.ReadSingle(); // actual gain in dB
                band.Q = QConvertBack(binFile.ReadSingle());

                // 0 - 7
                var filterType = binFile.ReadSingle();
                switch (filterType)
                {
                    case (float)ProQ2Shape.Bell:
                        band.Shape = ProQ2Shape.Bell;
                        break;
                    case (float)ProQ2Shape.LowShelf:
                        band.Shape = ProQ2Shape.LowShelf;
                        break;
                    case (float)ProQ2Shape.LowCut:
                        band.Shape = ProQ2Shape.LowCut;
                        break;
                    case (float)ProQ2Shape.HighShelf:
                        band.Shape = ProQ2Shape.HighShelf;
                        break;
                    case (float)ProQ2Shape.HighCut:
                        band.Shape = ProQ2Shape.HighCut;
                        break;
                    case (float)ProQ2Shape.Notch:
                        band.Shape = ProQ2Shape.Notch;
                        break;
                    case (float)ProQ2Shape.BandPass:
                        band.Shape = ProQ2Shape.BandPass;
                        break;
                    case (float)ProQ2Shape.TiltShelf:
                        band.Shape = ProQ2Shape.TiltShelf;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter type is outside range: {0}", filterType));
                }

                // 0 - 8 
                var filterSlope = binFile.ReadSingle();
                switch (filterSlope)
                {
                    case (float)ProQSlope.Slope6dB_oct:
                        band.Slope = ProQSlope.Slope6dB_oct;
                        break;
                    case (float)ProQSlope.Slope12dB_oct:
                        band.Slope = ProQSlope.Slope12dB_oct;
                        break;
                    case (float)ProQSlope.Slope18dB_oct:
                        band.Slope = ProQSlope.Slope18dB_oct;
                        break;
                    case (float)ProQSlope.Slope24dB_oct:
                        band.Slope = ProQSlope.Slope24dB_oct;
                        break;
                    case (float)ProQSlope.Slope30dB_oct:
                        band.Slope = ProQSlope.Slope30dB_oct;
                        break;
                    case (float)ProQSlope.Slope36dB_oct:
                        band.Slope = ProQSlope.Slope36dB_oct;
                        break;
                    case (float)ProQSlope.Slope48dB_oct:
                        band.Slope = ProQSlope.Slope48dB_oct;
                        break;
                    case (float)ProQSlope.Slope72dB_oct:
                        band.Slope = ProQSlope.Slope72dB_oct;
                        break;
                    case (float)ProQSlope.Slope96dB_oct:
                        band.Slope = ProQSlope.Slope96dB_oct;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter slope is outside range: {0}", filterSlope));
                }

                // 0 = Left, 1 = Right, 2 = Stereo
                var filterStereoPlacement = binFile.ReadSingle();
                switch (filterStereoPlacement)
                {
                    case (float)ProQ2StereoPlacement.LeftOrMid:
                        band.StereoPlacement = ProQ2StereoPlacement.LeftOrMid;
                        break;
                    case (float)ProQ2StereoPlacement.RightOrSide:
                        band.StereoPlacement = ProQ2StereoPlacement.RightOrSide;
                        break;
                    case (float)ProQ2StereoPlacement.Stereo:
                        band.StereoPlacement = ProQ2StereoPlacement.Stereo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter stereo placement is outside range: {0}", filterStereoPlacement));
                }

                Bands.Add(band);
            }

            // read the remaining floats
            // int remainingParameterCount = ParameterCount - 7 * Bands.Count;
            try
            {
                ProcessingMode = binFile.ReadSingle();               // Zero Latency: 0.0, Natural Phase: 1.0, Linear Phase: 2.0
                ProcessingResolution = binFile.ReadSingle();         // 0 - 4, Medium
                ChannelMode = binFile.ReadSingle();                  // 0 = Left/Right, 1 = Mid/Side
                GainScale = binFile.ReadSingle();                    // 100%
                OutputLevel = binFile.ReadSingle();                  // 0.0 dB, -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
                OutputPan = binFile.ReadSingle();                    // Left 0 dB, Right: 0 dB, -1 to 1 (0 = middle)
                ByPass = binFile.ReadSingle();                       // Not Bypassed
                OutputInvertPhase = binFile.ReadSingle();            // Normal
                AutoGain = binFile.ReadSingle();                     // Off
                AnalyzerShowPreProcessing = binFile.ReadSingle();    // Disabled - 0: Off, 1: On
                AnalyzerShowPostProcessing = binFile.ReadSingle();   // Disabled - 0: Off, 1: On
                AnalyzerShowSidechain = binFile.ReadSingle();        // Disabled - 0: Off, 1: On
                AnalyzerRange = binFile.ReadSingle();                // Analyzer Range in dB. 0.0: 60dB, 1.0: 90dB, 2.0: 120dB
                AnalyzerResolution = binFile.ReadSingle();           // Analyzer Resolution. 0.0: Low, 1.0: Medium, 2.0: High, 3.00: Maximum  
                AnalyzerSpeed = binFile.ReadSingle();                // Analyzer Speed. 0.0: Very Slow, 1.0: Slow, 2.0: Medium, 3.0 Fast, 4.0: Very Fast
                AnalyzerTilt = binFile.ReadSingle();                 // Analyzer Tilt in dB/oct. 0.0: 0.0, 1.0: 1.5, 2.0: 3.0, 3.0: 4.5, 4.0: 6.0  
                AnalyzerFreeze = binFile.ReadSingle();               // 0: Off, 1: On
                SpectrumGrab = binFile.ReadSingle();                 // Enabled
                DisplayRange = binFile.ReadSingle();                 // 12dB
                ReceiveMidi = binFile.ReadSingle();                  // Enabled
                SoloBand = binFile.ReadSingle();                     // -1
                SoloGain = binFile.ReadSingle();                     // 0.00
            }
            catch { }

            // check if mid/side
            if (ChannelMode == 1)
            {
                Bands.ForEach(b => b.ChannelMode = ProQ2ChannelMode.MidSide);
            }

            binFile.Close();

            return true;
        }

        public override bool WriteFFP(string filePath)
        {
            using (BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian, true))
            {
                binFile.Write("FQ2p");
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
                // write total parameter count
                // 24 bands with 7 parameters each = 168
                // pluss the 22 parameters at the end
                binFile.Write((UInt32)(24 * 7 + 22));

                for (int i = 0; i < 24; i++)
                {
                    if (i < Bands.Count)
                    {
                        binFile.Write((float)(Bands[i].Enabled ? 1 : 2));
                        binFile.Write((float)FreqConvert(Bands[i].Frequency));
                        binFile.Write((float)Bands[i].Gain);
                        binFile.Write((float)QConvert(Bands[i].Q));
                        binFile.Write((float)Bands[i].Shape);
                        binFile.Write((float)Bands[i].Slope);
                        binFile.Write((float)Bands[i].StereoPlacement);
                    }
                    else
                    {
                        binFile.Write((float)2);
                        binFile.Write((float)FreqConvert(1000));
                        binFile.Write((float)0);
                        binFile.Write((float)QConvert(1));
                        binFile.Write((float)ProQ2Shape.Bell);
                        binFile.Write((float)ProQSlope.Slope24dB_oct);
                        binFile.Write((float)ProQ2StereoPlacement.Stereo);
                    }
                }

                // write the remaining floats
                binFile.Write((float)ProcessingMode);               // Zero Latency: 0.0, Natural Phase: 1.0, Linear Phase: 2.0
                binFile.Write((float)ProcessingResolution);         // 0 - 4, Medium
                binFile.Write((float)ChannelMode);                  // 0 = Left/Right, 1 = Mid/Side
                binFile.Write((float)GainScale);                    // 100%
                binFile.Write((float)OutputLevel);                  // 0.0 dB, -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
                binFile.Write((float)OutputPan);                    // Left 0 dB, Right: 0 dB, -1 to 1 (0 = middle)
                binFile.Write((float)ByPass);                       // Not Bypassed
                binFile.Write((float)OutputInvertPhase);            // Normal
                binFile.Write((float)AutoGain);                     // Off
                binFile.Write((float)AnalyzerShowPreProcessing);    // Disabled - 0: Off, 1: On
                binFile.Write((float)AnalyzerShowPostProcessing);   // Disabled - 0: Off, 1: On
                binFile.Write((float)AnalyzerShowSidechain);        // Disabled - 0: Off, 1: On
                binFile.Write((float)AnalyzerRange);                // Analyzer Range in dB. 0.0: 60dB, 1.0: 90dB, 2.0: 120dB
                binFile.Write((float)AnalyzerResolution);           // Analyzer Resolution. 0.0: Low, 1.0: Medium, 2.0: High, 3.00: Maximum  
                binFile.Write((float)AnalyzerSpeed);                // Analyzer Speed. 0.0: Very Slow, 1.0: Slow, 2.0: Medium, 3.0 Fast, 4.0: Very Fast
                binFile.Write((float)AnalyzerTilt);                 // Analyzer Tilt in dB/oct. 0.0: 0.0, 1.0: 1.5, 2.0: 3.0, 3.0: 4.5, 4.0: 6.0  
                binFile.Write((float)AnalyzerFreeze);               // 0: Off, 1: On
                binFile.Write((float)SpectrumGrab);                 // Enabled
                binFile.Write((float)DisplayRange);                 // 12dB
                binFile.Write((float)ReceiveMidi);                  // Enabled
                binFile.Write((float)SoloBand);                     // -1
                binFile.Write((float)SoloGain);                     // 0.00

                // Don't write the ex fields
                // binFile.Write((float)ExAutoGain);                   // (Other)                       
            }

            return memStream.ToArray();
        }

        public override string ToString()
        {
            var writer = new StringWriter();
            writer.WriteLine(string.Format("Vst3ID: {0}", this.Vst3ClassID));

            writer.WriteLine("Bands:");
            foreach (var band in this.Bands)
            {
                writer.WriteLine(band.ToString());
            }

            writer.WriteLine();
            writer.WriteLine("PostPresetParameters:");
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "ProcessingMode", ProcessingMode, "Zero Latency: 0.0, Natural Phase: 1.0, Linear Phase: 2.0"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "ProcessingResolution", ProcessingResolution, "0 - 4, Medium"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "ChannelMode", ChannelMode, "0 = Left/Right, 1 = Mid/Side"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "GainScale", GainScale, "100%"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "OutputLevel", OutputLevel, "0.0 dB, -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "OutputPan", OutputPan, "Left 0 dB, Right: 0 dB, -1 to 1 (0 = middle)"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "ByPass", ByPass, "Not Bypassed"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "OutputInvertPhase", OutputInvertPhase, "Normal"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "AutoGain", AutoGain, "Off"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "AnalyzerShowPreProcessing", AnalyzerShowPreProcessing, "Disabled - 0: Off, 1: On"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "AnalyzerShowPostProcessing", AnalyzerShowPostProcessing, "Disabled - 0: Off, 1: On"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "AnalyzerShowSidechain", AnalyzerShowSidechain, "Disabled - 0: Off, 1: On"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "AnalyzerRange", AnalyzerRange, "Analyzer Range in dB. 0.0: 60dB, 1.0: 90dB, 2.0: 120dB"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "AnalyzerResolution", AnalyzerResolution, "Analyzer Resolution. 0.0: Low, 1.0: Medium, 2.0: High, 3.00: Maximum  "));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "AnalyzerSpeed", AnalyzerSpeed, "Analyzer Speed. 0.0: Very Slow, 1.0: Slow, 2.0: Medium, 3.0 Fast, 4.0: Very Fast"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "AnalyzerTilt", AnalyzerTilt, "Analyzer Tilt in dB/oct. 0.0: 0.0, 1.0: 1.5, 2.0: 3.0, 3.0: 4.5, 4.0: 6.0  "));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "AnalyzerFreeze", AnalyzerFreeze, "0: Off, 1: On"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "SpectrumGrab", SpectrumGrab, "Enabled"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "DisplayRange", DisplayRange, "12dB"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "ReceiveMidi", ReceiveMidi, "Enabled"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "SoloBand", SoloBand, "-1"));
            writer.WriteLine(string.Format("{0,-28} {1,8:0.00}  {2}", "SoloGain", SoloGain, "0.00"));

            return writer.ToString();
        }

        protected override bool PreparedForWriting()
        {
            InitCompChunkData();
            InitContChunkData();
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

                    // add some unknown variables
                    binFile.Write((int)1);
                    binFile.Write((int)1);
                }

                this.CompChunkData = memStream.ToArray();
            }
        }

        public void InitContChunkData()
        {
            if (HasFXP)
            {
                // don't do anything
            }
            else
            {
                var memStream = new MemoryStream();
                using (BinaryFile binFile = new BinaryFile(memStream, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII))
                {
                    binFile.Write("FFed");
                    binFile.Write((float)0.0);
                    binFile.Write((float)1.0);
                }

                this.ContChunkData = memStream.ToArray();
            }
        }

        /// <summary>
        /// Initialize the class specific variables using parameters
        /// </summary>
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
                        PlugInName = "FabFilter Pro-Q 2 x64";
                        PlugInVendor = "FabFilter";
                    }
                }
            }
            else
            {
                // init preset parameters
                // Note that the floats are not stored as IEEE (meaning between 0.0 - 1.0) but as floats representing the real values 
                var fabFilterProQ2Floats = Parameters
                                            .Where(v => v.Value.Type == Parameter.ParameterType.Number)
                                            .Select(v => (float)v.Value.Number.Value).ToArray();
                InitFromParameters(fabFilterProQ2Floats, false);
            }
        }

        /// <summary>
        /// Initialize the class specific variables using float parameters
        /// </summary>
        public void InitFromParameters(float[] floatParameters, bool isIEEE = true)
        {
            this.Bands = new List<ProQ2Band>();

            float[] floatArray;
            if (isIEEE)
            {
                // convert the ieee float parameters to fabfilter floats
                floatArray = Convert2FabfilterProQ2Floats(floatParameters);
            }
            else
            {
                floatArray = floatParameters;
            }

            int index = 0;
            for (int i = 0; i < 24; i++)
            {
                var band = new ProQ2Band();

                // 1 = Enabled, 2 = Disabled
                band.Enabled = floatArray[index++] == 1 ? true : false;

                band.Frequency = FabfilterProQ2.FreqConvertBack(floatArray[index++]);
                band.Gain = floatArray[index++]; // actual gain in dB
                band.Q = FabfilterProQ2.QConvertBack(floatArray[index++]);

                // 0 - 7
                var filterType = floatArray[index++];
                switch (filterType)
                {
                    case (float)ProQ2Shape.Bell:
                        band.Shape = ProQ2Shape.Bell;
                        break;
                    case (float)ProQ2Shape.LowShelf:
                        band.Shape = ProQ2Shape.LowShelf;
                        break;
                    case (float)ProQ2Shape.LowCut:
                        band.Shape = ProQ2Shape.LowCut;
                        break;
                    case (float)ProQ2Shape.HighShelf:
                        band.Shape = ProQ2Shape.HighShelf;
                        break;
                    case (float)ProQ2Shape.HighCut:
                        band.Shape = ProQ2Shape.HighCut;
                        break;
                    case (float)ProQ2Shape.Notch:
                        band.Shape = ProQ2Shape.Notch;
                        break;
                    case (float)ProQ2Shape.BandPass:
                        band.Shape = ProQ2Shape.BandPass;
                        break;
                    case (float)ProQ2Shape.TiltShelf:
                        band.Shape = ProQ2Shape.TiltShelf;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter type is outside range: {0}", filterType));
                }

                // 0 - 8 
                var filterSlope = floatArray[index++];
                switch (filterSlope)
                {
                    case (float)ProQSlope.Slope6dB_oct:
                        band.Slope = ProQSlope.Slope6dB_oct;
                        break;
                    case (float)ProQSlope.Slope12dB_oct:
                        band.Slope = ProQSlope.Slope12dB_oct;
                        break;
                    case (float)ProQSlope.Slope18dB_oct:
                        band.Slope = ProQSlope.Slope18dB_oct;
                        break;
                    case (float)ProQSlope.Slope24dB_oct:
                        band.Slope = ProQSlope.Slope24dB_oct;
                        break;
                    case (float)ProQSlope.Slope30dB_oct:
                        band.Slope = ProQSlope.Slope30dB_oct;
                        break;
                    case (float)ProQSlope.Slope36dB_oct:
                        band.Slope = ProQSlope.Slope36dB_oct;
                        break;
                    case (float)ProQSlope.Slope48dB_oct:
                        band.Slope = ProQSlope.Slope48dB_oct;
                        break;
                    case (float)ProQSlope.Slope72dB_oct:
                        band.Slope = ProQSlope.Slope72dB_oct;
                        break;
                    case (float)ProQSlope.Slope96dB_oct:
                        band.Slope = ProQSlope.Slope96dB_oct;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter slope is outside range: {0}", filterSlope));
                }

                // 0 = Left, 1 = Right, 2 = Stereo
                var filterStereoPlacement = floatArray[index++];
                switch (filterStereoPlacement)
                {
                    case (float)ProQ2StereoPlacement.LeftOrMid:
                        band.StereoPlacement = ProQ2StereoPlacement.LeftOrMid;
                        break;
                    case (float)ProQ2StereoPlacement.RightOrSide:
                        band.StereoPlacement = ProQ2StereoPlacement.RightOrSide;
                        break;
                    case (float)ProQ2StereoPlacement.Stereo:
                        band.StereoPlacement = ProQ2StereoPlacement.Stereo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter stereo placement is outside range: {0}", filterStereoPlacement));
                }

                this.Bands.Add(band);
            }

            // read the remaining floats
            try
            {
                this.ProcessingMode = floatArray[index++];               // Zero Latency: 0.0, Natural Phase: 1.0, Linear Phase: 2.0
                this.ProcessingResolution = floatArray[index++];         // 0 - 4, Medium
                this.ChannelMode = floatArray[index++];                  // 0 = Left/Right, 1 = Mid/Side
                this.GainScale = floatArray[index++];                    // 100%
                this.OutputLevel = floatArray[index++];                  // 0.0 dB, -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
                this.OutputPan = floatArray[index++];                    // Left 0 dB, Right: 0 dB, -1 to 1 (0 = middle)
                this.ByPass = floatArray[index++];                       // Not Bypassed
                this.OutputInvertPhase = floatArray[index++];            // Normal
                this.AutoGain = floatArray[index++];                     // Off
                this.AnalyzerShowPreProcessing = floatArray[index++];    // Disabled - 0: Off, 1: On
                this.AnalyzerShowPostProcessing = floatArray[index++];   // Disabled - 0: Off, 1: On
                this.AnalyzerShowSidechain = floatArray[index++];        // Disabled - 0: Off, 1: On
                this.AnalyzerRange = floatArray[index++];                // Analyzer Range in dB. 0.0: 60dB, 1.0: 90dB, 2.0: 120dB
                this.AnalyzerResolution = floatArray[index++];           // Analyzer Resolution. 0.0: Low, 1.0: Medium, 2.0: High, 3.00: Maximum  
                this.AnalyzerSpeed = floatArray[index++];                // Analyzer Speed. 0.0: Very Slow, 1.0: Slow, 2.0: Medium, 3.0 Fast, 4.0: Very Fast
                this.AnalyzerTilt = floatArray[index++];                 // Analyzer Tilt in dB/oct. 0.0: 0.0, 1.0: 1.5, 2.0: 3.0, 3.0: 4.5, 4.0: 6.0  
                this.AnalyzerFreeze = floatArray[index++];               // 0: Off, 1: On
                this.SpectrumGrab = floatArray[index++];                 // Enabled
                this.DisplayRange = floatArray[index++];                 // 12dB
                this.ReceiveMidi = floatArray[index++];                  // Enabled
                this.SoloBand = floatArray[index++];                     // -1, -1 to 1 
                this.SoloGain = floatArray[index++];                     // 0.00, -1 to 1 
            }
            catch { }

            // check if mid/side
            if (this.ChannelMode == 1)
            {
                this.Bands.ForEach(b => b.ChannelMode = ProQ2ChannelMode.MidSide);
            }
        }
    }

    public enum ProQ2Shape
    {
        Bell = 0, // (default)
        LowShelf = 1,
        LowCut = 2,
        HighShelf = 3,
        HighCut = 4,
        Notch = 5,
        BandPass = 6,
        TiltShelf = 7,
    }

    public enum ProQSlope
    {
        Slope6dB_oct = 0,
        Slope12dB_oct = 1,
        Slope18dB_oct = 2,
        Slope24dB_oct = 3, // (default)
        Slope30dB_oct = 4,
        Slope36dB_oct = 5,
        Slope48dB_oct = 6,
        Slope72dB_oct = 7,
        Slope96dB_oct = 8,
    }

    public enum ProQ2StereoPlacement
    {
        LeftOrMid = 0,
        RightOrSide = 1,
        Stereo = 2, // (default)
    }

    public enum ProQ2ChannelMode
    {
        LeftRight = 0,
        MidSide = 1
    }

    public class ProQ2Band
    {
        public ProQ2ChannelMode ChannelMode { get; set; }   // determine if band is in LS or MS mode
        public bool Enabled { get; set; }
        public double Frequency { get; set; }               // value range 10.0 -> 30000.0 Hz
        public double Gain { get; set; }                    // + or - value in dB
        public double Q { get; set; }                       // value range 0.025 -> 40.00
        public ProQ2Shape Shape { get; set; }
        public ProQSlope Slope { get; set; }
        public ProQ2StereoPlacement StereoPlacement { get; set; }

        public override string ToString()
        {
            string stereoPlacement = "";
            switch (StereoPlacement)
            {
                case ProQ2StereoPlacement.LeftOrMid:
                    if (ChannelMode == ProQ2ChannelMode.LeftRight)
                    {
                        stereoPlacement = "Left";
                    }
                    else
                    {
                        stereoPlacement = "Mid";
                    }
                    break;
                case ProQ2StereoPlacement.RightOrSide:
                    if (ChannelMode == ProQ2ChannelMode.LeftRight)
                    {
                        stereoPlacement = "Right";
                    }
                    else
                    {
                        stereoPlacement = "Side";
                    }
                    break;
                case ProQ2StereoPlacement.Stereo:
                    stereoPlacement = "Stereo";
                    break;
            }

            return String.Format("[{4,-3}] {0}: {1:0.00} Hz, {2:0.00} dB, Q: {3:0.00}, {5}, {6}", Shape, Frequency, Gain, Q, Enabled == true ? "On" : "Off", Slope, stereoPlacement);
        }
    }
}