using System;
using System.Collections.Generic;
using System.IO;
using CommonUtils;
using Serilog;

namespace PresetConverter
{
    /// <summary>
    /// FabfilterProQ Preset Class for saving a Fabfilter Pro Q 2 Preset file (fft)
    /// </summary>
    public class FabfilterProQ2
    {
        public List<ProQ2Band> Bands { get; set; }
        public int Version { get; set; }            // Normally 2
        public int ParameterCount { get; set; }     // Normally 190

        // Post Band Parameters
        public float ProcessingMode { get; set; }               // Zero Latency: 0.0, Natural Phase: 0.5, Linear Phase: 1.0
        public float ProcessingResolution { get; set; }         // Medium
        public float ChannelMode { get; set; }                  // 0 = Left/Right, 1 = Mid/Side
        public float GainScale { get; set; }                    // 100%
        public float OutputLevel { get; set; }                  // 0.0 dB
        public float OutputPan { get; set; }                    // Left 0 dB, Right: 0 dB
        public float ByPass { get; set; }                       // Not Bypassed
        public float OutputInvertPhase { get; set; }            // Normal
        public float AutoGain { get; set; }                     // Off
        public float AnalyzerShowPreProcessing { get; set; }    // Disabled - 0: Off, 1: On
        public float AnalyzerShowPostProcessing { get; set; }   // Disabled - 0: Off, 1: On
        public float AnalyzerShowSidechain { get; set; }        // Disabled - 0: Off, 1: On
        public float AnalyzerRange { get; set; }                // Analyzer Range in dB. 0.0: 60dB, 0.5: 90dB, 1.0: 120dB
        public float AnalyzerResolution { get; set; }           // Analyzer Resolution. 0.0: Low, 0.333: Medium, 0,666: High, 1.00 Maximum  
        public float AnalyzerSpeed { get; set; }                // Analyzer Speed. 0.0: Very Slow, 0.25: Slow, 0.5: Medium, 0.75: Fast, 1.0: Very Fast
        public float AnalyzerTilt { get; set; }                 // Analyzer Tilt in dB/oct. 0.0: 0.0, 0.25: 1.5, 0.5: 3.0, 0.75: 4.5, 1.0: 6.0  
        public float AnalyzerFreeze { get; set; }               // 0: Off, 1: On
        public float SpectrumGrab { get; set; }                 // Enabled
        public float DisplayRange { get; set; }                 // 12dB
        public float ReceiveMidi { get; set; }                  // Enabled
        public float SoloBand { get; set; }                     // -1
        public float SoloGain { get; set; }                     // 0.00
        public float ExAutoGain { get; set; }                   // (Other)

        public FabfilterProQ2()
        {

        }

        public static float[] ReadFloats(string filePath)
        {
            BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian);

            string header = binFile.ReadString(4);
            if (header == "FQ2p")
            {
                int version = binFile.ReadInt32();
                int parameterCount = binFile.ReadInt32();

                var floatArray = new float[parameterCount];
                int i = 0;
                try
                {
                    for (i = 0; i < parameterCount; i++)
                    {
                        floatArray[i] = binFile.ReadSingle();
                    }

                }
                catch (System.Exception e)
                {
                    Log.Error("Failed reading floats: {0}", e);
                }

                binFile.Close();
                return floatArray;
            }
            else
            {
                binFile.Close();
                return null;
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

            // TODO: have to get the right list of parameters
            for (int i = counter; i < ieeeFloatParameters.Length; i++)
            {
                floatList.Add(ieeeFloatParameters[i]);
            }

            return floatList.ToArray();

        }

        public static FabfilterProQ2 Convert2FabfilterProQ(float[] floatParameters, bool isIEEE = true)
        {
            var preset = new FabfilterProQ2();
            preset.Bands = new List<ProQ2Band>();

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

                preset.Bands.Add(band);
            }

            // read the remaining floats
            try
            {
                preset.ProcessingMode = floatArray[index++];               // Zero Latency: 0.0, Natural Phase: 0.5, Linear Phase: 1.0
                preset.ProcessingResolution = floatArray[index++];         // Medium
                preset.ChannelMode = floatArray[index++];                  // 0 = Left/Right, 1 = Mid/Side
                preset.GainScale = floatArray[index++];                    // 100%
                preset.OutputLevel = floatArray[index++];                  // 0.0 dB
                preset.OutputPan = floatArray[index++];                    // Left 0 dB, Right: 0 dB
                preset.ByPass = floatArray[index++];                       // Not Bypassed
                preset.OutputInvertPhase = floatArray[index++];            // Normal
                preset.AutoGain = floatArray[index++];                     // Off
                preset.AnalyzerShowPreProcessing = floatArray[index++];    // Disabled - 0: Off, 1: On
                preset.AnalyzerShowPostProcessing = floatArray[index++];   // Disabled - 0: Off, 1: On
                preset.AnalyzerShowSidechain = floatArray[index++];        // Disabled - 0: Off, 1: On
                preset.AnalyzerRange = floatArray[index++];                // Analyzer Range in dB. 0.0: 60dB, 0.5: 90dB, 1.0: 120dB
                preset.AnalyzerResolution = floatArray[index++];           // Analyzer Resolution. 0.0: Low, 0.333: Medium, 0,666: High, 1.00 Maximum  
                preset.AnalyzerSpeed = floatArray[index++];                // Analyzer Speed. 0.0: Very Slow, 0.25: Slow, 0.5: Medium, 0.75: Fast, 1.0: Very Fast
                preset.AnalyzerTilt = floatArray[index++];                 // Analyzer Tilt in dB/oct. 0.0: 0.0, 0.25: 1.5, 0.5: 3.0, 0.75: 4.5, 1.0: 6.0  
                preset.AnalyzerFreeze = floatArray[index++];               // 0: Off, 1: On
                preset.SpectrumGrab = floatArray[index++];                 // Enabled
                preset.DisplayRange = floatArray[index++];                 // 12dB
                preset.ReceiveMidi = floatArray[index++];                  // Enabled
                preset.SoloBand = floatArray[index++];                     // -1
                preset.SoloGain = floatArray[index++];                     // 0.00
                preset.ExAutoGain = floatArray[index++];                   // (Other)
            }
            catch { }

            // check if mid/side
            if (preset.ChannelMode == 1)
            {
                preset.Bands.ForEach(b => b.ChannelMode = ProQ2ChannelMode.MidSide);
            }

            return preset;
        }

        /// <summary>
        /// convert a float between 0 and 1 to the fabfilter float equivalent
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static float IEEEFloatToFrequencyFloat(float value)
        {
            return 11.5507311008828f * value + 3.32193432374016f;
        }

        public bool Read(string filePath)
        {
            BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian);

            string header = binFile.ReadString(4);
            if (header != "FQ2p") return false;

            Version = binFile.ReadInt32();
            ParameterCount = binFile.ReadInt32();

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
            try
            {
                int remainingParameterCount = ParameterCount - 7 * Bands.Count;
                ProcessingMode = binFile.ReadSingle();               // Zero Latency: 0.0, Natural Phase: 0.5, Linear Phase: 1.0
                ProcessingResolution = binFile.ReadSingle();         // Medium
                ChannelMode = binFile.ReadSingle();                  // 0 = Left/Right, 1 = Mid/Side
                GainScale = binFile.ReadSingle();                    // 100%
                OutputLevel = binFile.ReadSingle();                  // 0.0 dB
                OutputPan = binFile.ReadSingle();                    // Left 0 dB, Right: 0 dB
                ByPass = binFile.ReadSingle();                       // Not Bypassed
                OutputInvertPhase = binFile.ReadSingle();            // Normal
                AutoGain = binFile.ReadSingle();                     // Off
                AnalyzerShowPreProcessing = binFile.ReadSingle();    // Disabled - 0: Off, 1: On
                AnalyzerShowPostProcessing = binFile.ReadSingle();   // Disabled - 0: Off, 1: On
                AnalyzerShowSidechain = binFile.ReadSingle();        // Disabled - 0: Off, 1: On
                AnalyzerRange = binFile.ReadSingle();                // Analyzer Range in dB. 0.0: 60dB, 0.5: 90dB, 1.0: 120dB
                AnalyzerResolution = binFile.ReadSingle();           // Analyzer Resolution. 0.0: Low, 0.333: Medium, 0,666: High, 1.00 Maximum  
                AnalyzerSpeed = binFile.ReadSingle();                // Analyzer Speed. 0.0: Very Slow, 0.25: Slow, 0.5: Medium, 0.75: Fast, 1.0: Very Fast
                AnalyzerTilt = binFile.ReadSingle();                 // Analyzer Tilt in dB/oct. 0.0: 0.0, 0.25: 1.5, 0.5: 3.0, 0.75: 4.5, 1.0: 6.0  
                AnalyzerFreeze = binFile.ReadSingle();               // 0: Off, 1: On
                SpectrumGrab = binFile.ReadSingle();                 // Enabled
                DisplayRange = binFile.ReadSingle();                 // 12dB
                ReceiveMidi = binFile.ReadSingle();                  // Enabled
                SoloBand = binFile.ReadSingle();                     // -1
                SoloGain = binFile.ReadSingle();                     // 0.00
                ExAutoGain = binFile.ReadSingle();                   // (Other)
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

        public bool Write(string filePath)
        {
            BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian, true);
            binFile.Write("FQ2p");
            binFile.Write((int)Version);
            binFile.Write((int)Bands.Count * 7 + 22);

            for (int i = 0; i < 24; i++)
            {
                if (i < Bands.Count)
                {
                    binFile.Write((float)(Bands[i].Enabled ? 1 : 2));
                    binFile.Write((float)FabfilterProQ2.FreqConvert(Bands[i].Frequency));
                    binFile.Write((float)Bands[i].Gain);
                    binFile.Write((float)FabfilterProQ2.QConvert(Bands[i].Q));
                    binFile.Write((float)Bands[i].Shape);
                    binFile.Write((float)Bands[i].Slope);
                    binFile.Write((float)Bands[i].StereoPlacement);
                }
                else
                {
                    binFile.Write((float)2);
                    binFile.Write((float)FabfilterProQ2.FreqConvert(1000));
                    binFile.Write((float)0);
                    binFile.Write((float)FabfilterProQ2.QConvert(1));
                    binFile.Write((float)ProQ2Shape.Bell);
                    binFile.Write((float)ProQSlope.Slope24dB_oct);
                    binFile.Write((float)ProQ2StereoPlacement.Stereo);
                }
            }

            // write the remaining floats
            binFile.Write((float)ProcessingMode);               // Zero Latency: 0.0, Natural Phase: 0.5, Linear Phase: 1.0
            binFile.Write((float)ProcessingResolution);         // Medium
            binFile.Write((float)ChannelMode);                  // 0 = Left/Right, 1 = Mid/Side
            binFile.Write((float)GainScale);                    // 100%
            binFile.Write((float)OutputLevel);                  // 0.0 dB
            binFile.Write((float)OutputPan);                    // Left 0 dB, Right: 0 dB
            binFile.Write((float)ByPass);                       // Not Bypassed
            binFile.Write((float)OutputInvertPhase);            // Normal
            binFile.Write((float)AutoGain);                     // Off
            binFile.Write((float)AnalyzerShowPreProcessing);    // Disabled - 0: Off, 1: On
            binFile.Write((float)AnalyzerShowPostProcessing);   // Disabled - 0: Off, 1: On
            binFile.Write((float)AnalyzerShowSidechain);        // Disabled - 0: Off, 1: On
            binFile.Write((float)AnalyzerRange);                // Analyzer Range in dB. 0.0: 60dB, 0.5: 90dB, 1.0: 120dB
            binFile.Write((float)AnalyzerResolution);           // Analyzer Resolution. 0.0: Low, 0.333: Medium, 0,666: High, 1.00 Maximum  
            binFile.Write((float)AnalyzerSpeed);                // Analyzer Speed. 0.0: Very Slow, 0.25: Slow, 0.5: Medium, 0.75: Fast, 1.0: Very Fast
            binFile.Write((float)AnalyzerTilt);                 // Analyzer Tilt in dB/oct. 0.0: 0.0, 0.25: 1.5, 0.5: 3.0, 0.75: 4.5, 1.0: 6.0  
            binFile.Write((float)AnalyzerFreeze);               // 0: Off, 1: On
            binFile.Write((float)SpectrumGrab);                 // Enabled
            binFile.Write((float)DisplayRange);                 // 12dB
            binFile.Write((float)ReceiveMidi);                  // Enabled
            binFile.Write((float)SoloBand);                     // -1
            binFile.Write((float)SoloGain);                     // 0.00
            binFile.Write((float)ExAutoGain);                   // (Other)

            binFile.Close();

            return true;
        }

        public override string ToString()
        {
            var writer = new StringWriter();

            writer.WriteLine("Bands:");
            foreach (var band in this.Bands)
            {
                writer.WriteLine(band.ToString());
            }

            writer.WriteLine();
            writer.WriteLine("PostPresetParameters:");
            writer.WriteLine("ProcessingMode: {0}", ProcessingMode);
            writer.WriteLine("ProcessingResolution: {0}", ProcessingResolution);
            writer.WriteLine("ChannelMode: {0}", ChannelMode);
            writer.WriteLine("GainScale: {0}", GainScale);
            writer.WriteLine("OutputLevel: {0}", OutputLevel);
            writer.WriteLine("OutputPan: {0}", OutputPan);
            writer.WriteLine("ByPass: {0}", ByPass);
            writer.WriteLine("OutputInvertPhase: {0}", OutputInvertPhase);
            writer.WriteLine("AutoGain: {0}", AutoGain);
            writer.WriteLine("AnalyzerShowPreProcessing: {0}", AnalyzerShowPreProcessing);
            writer.WriteLine("AnalyzerShowPostProcessing: {0}", AnalyzerShowPostProcessing);
            writer.WriteLine("AnalyzerShowSidechain: {0}", AnalyzerShowSidechain);
            writer.WriteLine("AnalyzerRange: {0}", AnalyzerRange);
            writer.WriteLine("AnalyzerResolution: {0}", AnalyzerResolution);
            writer.WriteLine("AnalyzerSpeed: {0}", AnalyzerSpeed);
            writer.WriteLine("AnalyzerTilt: {0}", AnalyzerTilt);
            writer.WriteLine("AnalyzerFreeze: {0}", AnalyzerFreeze);
            writer.WriteLine("SpectrumGrab: {0}", SpectrumGrab);
            writer.WriteLine("DisplayRange: {0}", DisplayRange);
            writer.WriteLine("ReceiveMidi: {0}", ReceiveMidi);
            writer.WriteLine("SoloBand: {0}", SoloBand);
            writer.WriteLine("SoloGain: {0}", SoloGain);
            writer.WriteLine("ExAutoGain: {0}", ExAutoGain);

            return writer.ToString();
        }


        // log and inverse log
        // a ^ x = b 
        // x = log(b) / log(a)

        public static double FreqConvert(double value)
        {
            // =LOG(A1)/LOG(2) (default = 1000 Hz)
            return Math.Log10(value) / Math.Log10(2);
        }

        public static double FreqConvertBack(double value)
        {
            // =POWER(2; frequency)
            return Math.Pow(2, value);
        }

        public static double QConvert(double value)
        {
            // =LOG(F1)*0,312098175+0,5 (default = 1)
            return Math.Log10(value) * 0.312098175 + 0.5;
        }

        public static double QConvertBack(double value)
        {
            // =POWER(10;((B3-0,5)/0,312098175))
            return Math.Pow(10, (value - 0.5) / 0.312098175);
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

            return String.Format("[{4}] {0}: {1:0.00} Hz, {2:0.00} dB, Q: {3:0.00}, {5}, {6}", Shape, Frequency, Gain, Q, Enabled == true ? "On " : "Off", Slope, stereoPlacement);
        }
    }
}