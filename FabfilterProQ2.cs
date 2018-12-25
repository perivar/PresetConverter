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
        public float Phase { get; set; }        // Natural Phase: 0.5, Linear Phase: 1
        public float Unknown2 { get; set; }     // Unknown field
        public float ChannelMode { get; set; }      // 0 = Left/Right, 1 = Mid/Side
        public float Unknown4 { get; set; }         // Unknown field
        public float Unknown5 { get; set; }         // Unknown field
        public float Unknown6 { get; set; }         // Unknown field
        public float Unknown7 { get; set; }         // Unknown field
        public float Unknown8 { get; set; }         // Unknown field
        public float Unknown9 { get; set; }         // Unknown field
        public float AnalyzerPre { get; set; }      // 0: Off, 1: On
        public float AnalyzerPost { get; set; }     // 0: Off, 1: On
        public float AnalyzerSC { get; set; }       // 0: Off, 1: On
        public float AnalyzerRange { get; set; }    // Analyzer Range in dB. 0.0: 60dB, 0.5: 90dB, 1.0: 120dB
        public float AnalyzerResolution { get; set; } // Analyzer Resolution. 0.0: Low, 0.333: Medium, 0,666: High, 1.00 Maximum  
        public float AnalyzerSpeed { get; set; }    // Analyzer Speed. 0.0: Very Slow, 0.25: Slow, 0.5: Medium, 0.75: Fast, 1.0: Very Fast
        public float AnalyzerTilt { get; set; }     // Analyzer Tilt. 0.0: 0.0, 0.25: 1.5, 0.5: 3.0, 0.75: 4.5, 1.0: 6.0  
        public float Freeze { get; set; }           // 0: Off, 1: On
        public float Unknown18 { get; set; }        // Unknown field
        public float Unknown19 { get; set; }        // Unknown field
        public float Unknown20 { get; set; }        // Unknown field
        public float Unknown21 { get; set; }        // Unknown field
        public float Unknown22 { get; set; }        // Unknown field
        public float Unknown23 { get; set; }        // Unknown field

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

                band.FilterFreq = FabfilterProQ2.FreqConvertBack(floatArray[index++]);
                band.FilterGain = floatArray[index++]; // actual gain in dB
                band.FilterQ = FabfilterProQ2.QConvertBack(floatArray[index++]);

                // 0 - 7
                var filterType = floatArray[index++];
                switch (filterType)
                {
                    case (float)ProQ2FilterType.Bell:
                        band.FilterType = ProQ2FilterType.Bell;
                        break;
                    case (float)ProQ2FilterType.LowShelf:
                        band.FilterType = ProQ2FilterType.LowShelf;
                        break;
                    case (float)ProQ2FilterType.LowCut:
                        band.FilterType = ProQ2FilterType.LowCut;
                        break;
                    case (float)ProQ2FilterType.HighShelf:
                        band.FilterType = ProQ2FilterType.HighShelf;
                        break;
                    case (float)ProQ2FilterType.HighCut:
                        band.FilterType = ProQ2FilterType.HighCut;
                        break;
                    case (float)ProQ2FilterType.Notch:
                        band.FilterType = ProQ2FilterType.Notch;
                        break;
                    case (float)ProQ2FilterType.BandPass:
                        band.FilterType = ProQ2FilterType.BandPass;
                        break;
                    case (float)ProQ2FilterType.TiltShelf:
                        band.FilterType = ProQ2FilterType.TiltShelf;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter type is outside range: {0}", filterType));
                }

                // 0 - 8 
                var filterSlope = floatArray[index++];
                switch (filterSlope)
                {
                    case (float)ProQ2LPHPSlope.Slope6dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope6dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope12dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope12dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope18dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope18dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope24dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope24dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope30dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope30dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope36dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope36dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope48dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope48dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope72dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope72dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope96dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope96dB_oct;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter slope is outside range: {0}", filterSlope));
                }

                // 0 = Left, 1 = Right, 2 = Stereo
                var filterStereoPlacement = floatArray[index++];
                switch (filterStereoPlacement)
                {
                    case (float)ProQ2StereoPlacement.LeftOrMid:
                        band.FilterStereoPlacement = ProQ2StereoPlacement.LeftOrMid;
                        break;
                    case (float)ProQ2StereoPlacement.RightOrSide:
                        band.FilterStereoPlacement = ProQ2StereoPlacement.RightOrSide;
                        break;
                    case (float)ProQ2StereoPlacement.Stereo:
                        band.FilterStereoPlacement = ProQ2StereoPlacement.Stereo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter stereo placement is outside range: {0}", filterStereoPlacement));
                }

                preset.Bands.Add(band);
            }

            // read the remaining floats
            preset.Phase = floatArray[index++];             // Natural Phase: 0.5, Linear Phase: 1
            preset.Unknown2 = floatArray[index++];          // Unknown field
            preset.ChannelMode = floatArray[index++];       // 0 = Left/Right, 1 = Mid/Side
            preset.Unknown4 = floatArray[index++];          // Unknown field
            preset.Unknown5 = floatArray[index++];          // Unknown field
            preset.Unknown6 = floatArray[index++];          // Unknown field
            preset.Unknown7 = floatArray[index++];          // Unknown field
            preset.Unknown8 = floatArray[index++];          // Unknown field
            preset.Unknown9 = floatArray[index++];          // Unknown field
            preset.AnalyzerPre = floatArray[index++];       // 0: Off, 1: On
            preset.AnalyzerPost = floatArray[index++];      // 0: Off, 1: On
            preset.AnalyzerSC = floatArray[index++];        // 0: Off, 1: On
            preset.AnalyzerRange = floatArray[index++];     // Analyzer Range in dB. 0.0: 60dB, 0.5: 90dB, 1.0: 120dB
            preset.AnalyzerResolution = floatArray[index++];// Analyzer Resolution. 0.0: Low, 0.333: Medium, 0,666: High, 1.00 Maximum  
            preset.AnalyzerSpeed = floatArray[index++];     // Analyzer Speed. 0.0: Very Slow, 0.25: Slow, 0.5: Medium, 0.75: Fast, 1.0: Very Fast
            preset.AnalyzerTilt = floatArray[index++];      // Analyzer Tilt. 0.0: 0.0, 0.25: 1.5, 0.5: 3.0, 0.75: 4.5, 1.0: 6.0  
            preset.Freeze = floatArray[index++];            // 0: Off, 1: On
            preset.Unknown18 = floatArray[index++];         // Unknown field
            preset.Unknown19 = floatArray[index++];         // Unknown field
            preset.Unknown20 = floatArray[index++];         // Unknown field
            preset.Unknown21 = floatArray[index++];         // Unknown field
            preset.Unknown22 = floatArray[index++];         // Unknown field
            preset.Unknown23 = floatArray[index++];         // Unknown field

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

                band.FilterFreq = FreqConvertBack(binFile.ReadSingle());
                band.FilterGain = binFile.ReadSingle(); // actual gain in dB
                band.FilterQ = QConvertBack(binFile.ReadSingle());

                // 0 - 7
                var filterType = binFile.ReadSingle();
                switch (filterType)
                {
                    case (float)ProQ2FilterType.Bell:
                        band.FilterType = ProQ2FilterType.Bell;
                        break;
                    case (float)ProQ2FilterType.LowShelf:
                        band.FilterType = ProQ2FilterType.LowShelf;
                        break;
                    case (float)ProQ2FilterType.LowCut:
                        band.FilterType = ProQ2FilterType.LowCut;
                        break;
                    case (float)ProQ2FilterType.HighShelf:
                        band.FilterType = ProQ2FilterType.HighShelf;
                        break;
                    case (float)ProQ2FilterType.HighCut:
                        band.FilterType = ProQ2FilterType.HighCut;
                        break;
                    case (float)ProQ2FilterType.Notch:
                        band.FilterType = ProQ2FilterType.Notch;
                        break;
                    case (float)ProQ2FilterType.BandPass:
                        band.FilterType = ProQ2FilterType.BandPass;
                        break;
                    case (float)ProQ2FilterType.TiltShelf:
                        band.FilterType = ProQ2FilterType.TiltShelf;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter type is outside range: {0}", filterType));
                }

                // 0 - 8 
                var filterSlope = binFile.ReadSingle();
                switch (filterSlope)
                {
                    case (float)ProQ2LPHPSlope.Slope6dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope6dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope12dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope12dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope18dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope18dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope24dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope24dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope30dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope30dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope36dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope36dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope48dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope48dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope72dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope72dB_oct;
                        break;
                    case (float)ProQ2LPHPSlope.Slope96dB_oct:
                        band.FilterLPHPSlope = ProQ2LPHPSlope.Slope96dB_oct;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter slope is outside range: {0}", filterSlope));
                }

                // 0 = Left, 1 = Right, 2 = Stereo
                var filterStereoPlacement = binFile.ReadSingle();
                switch (filterStereoPlacement)
                {
                    case (float)ProQ2StereoPlacement.LeftOrMid:
                        band.FilterStereoPlacement = ProQ2StereoPlacement.LeftOrMid;
                        break;
                    case (float)ProQ2StereoPlacement.RightOrSide:
                        band.FilterStereoPlacement = ProQ2StereoPlacement.RightOrSide;
                        break;
                    case (float)ProQ2StereoPlacement.Stereo:
                        band.FilterStereoPlacement = ProQ2StereoPlacement.Stereo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter stereo placement is outside range: {0}", filterStereoPlacement));
                }

                Bands.Add(band);
            }

            // read the remaining floats
            int remainingParameterCount = ParameterCount - 7 * Bands.Count;
            Phase = binFile.ReadSingle();             // Natural Phase: 0.5, Linear Phase: 1
            Unknown2 = binFile.ReadSingle();          // Unknown field
            ChannelMode = binFile.ReadSingle();       // 0 = Left/Right, 1 = Mid/Side
            Unknown4 = binFile.ReadSingle();          // Unknown field
            Unknown5 = binFile.ReadSingle();          // Unknown field
            Unknown6 = binFile.ReadSingle();          // Unknown field
            Unknown7 = binFile.ReadSingle();          // Unknown field
            Unknown8 = binFile.ReadSingle();          // Unknown field
            Unknown9 = binFile.ReadSingle();          // Unknown field
            AnalyzerPre = binFile.ReadSingle();       // 0: Off, 1: On
            AnalyzerPost = binFile.ReadSingle();      // 0: Off, 1: On
            AnalyzerSC = binFile.ReadSingle();        // 0: Off, 1: On
            AnalyzerRange = binFile.ReadSingle();     // Analyzer Range in dB. 0.0: 60dB, 0.5: 90dB, 1.0: 120dB
            AnalyzerResolution = binFile.ReadSingle();// Analyzer Resolution. 0.0: Low, 0.333: Medium, 0,666: High, 1.00 Maximum  
            AnalyzerSpeed = binFile.ReadSingle();     // Analyzer Speed. 0.0: Very Slow, 0.25: Slow, 0.5: Medium, 0.75: Fast, 1.0: Very Fast
            AnalyzerTilt = binFile.ReadSingle();      // Analyzer Tilt. 0.0: 0.0, 0.25: 1.5, 0.5: 3.0, 0.75: 4.5, 1.0: 6.0  
            Freeze = binFile.ReadSingle();            // 0: Off, 1: On
            Unknown18 = binFile.ReadSingle();         // Unknown field
            Unknown19 = binFile.ReadSingle();         // Unknown field
            Unknown20 = binFile.ReadSingle();         // Unknown field
            Unknown21 = binFile.ReadSingle();         // Unknown field
            Unknown22 = binFile.ReadSingle();         // Unknown field
            Unknown23 = binFile.ReadSingle();         // Unknown field

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
                    binFile.Write((float)FabfilterProQ2.FreqConvert(Bands[i].FilterFreq));
                    binFile.Write((float)Bands[i].FilterGain);
                    binFile.Write((float)FabfilterProQ2.QConvert(Bands[i].FilterQ));
                    binFile.Write((float)Bands[i].FilterType);
                    binFile.Write((float)Bands[i].FilterLPHPSlope);
                    binFile.Write((float)Bands[i].FilterStereoPlacement);
                }
                else
                {
                    binFile.Write((float)2);
                    binFile.Write((float)FabfilterProQ2.FreqConvert(1000));
                    binFile.Write((float)0);
                    binFile.Write((float)FabfilterProQ2.QConvert(1));
                    binFile.Write((float)ProQ2FilterType.Bell);
                    binFile.Write((float)ProQ2LPHPSlope.Slope24dB_oct);
                    binFile.Write((float)ProQ2StereoPlacement.Stereo);
                }
            }

            // write the remaining floats
            binFile.Write((float)Phase);             // Natural Phase: 0.5, Linear Phase: 1
            binFile.Write((float)Unknown2);          // Unknown field
            binFile.Write((float)ChannelMode);       // 0 = Left/Right, 1 = Mid/Side
            binFile.Write((float)Unknown4);          // Unknown field
            binFile.Write((float)Unknown5);          // Unknown field
            binFile.Write((float)Unknown6);          // Unknown field
            binFile.Write((float)Unknown7);          // Unknown field
            binFile.Write((float)Unknown8);          // Unknown field
            binFile.Write((float)Unknown9);          // Unknown field
            binFile.Write((float)AnalyzerPre);       // 0: Off, 1: On
            binFile.Write((float)AnalyzerPost);      // 0: Off, 1: On
            binFile.Write((float)AnalyzerSC);        // 0: Off, 1: On
            binFile.Write((float)AnalyzerRange);     // Analyzer Range in dB. 0.0: 60dB, 0.5: 90dB, 1.0: 120dB
            binFile.Write((float)AnalyzerResolution);// Analyzer Resolution. 0.0: Low, 0.333: Medium, 0,666: High, 1.00 Maximum  
            binFile.Write((float)AnalyzerSpeed);     // Analyzer Speed. 0.0: Very Slow, 0.25: Slow, 0.5: Medium, 0.75: Fast, 1.0: Very Fast
            binFile.Write((float)AnalyzerTilt);      // Analyzer Tilt. 0.0: 0.0, 0.25: 1.5, 0.5: 3.0, 0.75: 4.5, 1.0: 6.0  
            binFile.Write((float)Freeze);            // 0: Off, 1: On
            binFile.Write((float)Unknown18);         // Unknown field
            binFile.Write((float)Unknown19);         // Unknown field
            binFile.Write((float)Unknown20);         // Unknown field
            binFile.Write((float)Unknown21);         // Unknown field
            binFile.Write((float)Unknown22);         // Unknown field
            binFile.Write((float)Unknown23);         // Unknown field

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
            writer.WriteLine("Phase: {0} \t\t\tNatural Phase: 0.5, Linear Phase: 1", Phase);             // Natural Phase: 0.5, Linear Phase: 1
            writer.WriteLine("Unknown2: {0} \t\t\tUnknown field", Unknown2);          // Unknown field
            writer.WriteLine("ChannelMode: {0} \t\t\t0 = Left/Right, 1 = Mid/Side", ChannelMode);       // 0 = Left/Right, 1 = Mid/Side
            writer.WriteLine("Unknown4: {0} \t\t\tUnknown field", Unknown4);          // Unknown field
            writer.WriteLine("Unknown5: {0} \t\t\tUnknown field", Unknown5);          // Unknown field
            writer.WriteLine("Unknown6: {0} \t\t\tUnknown field", Unknown6);          // Unknown field
            writer.WriteLine("Unknown7: {0} \t\t\tUnknown field", Unknown7);          // Unknown field
            writer.WriteLine("Unknown8: {0} \t\t\tUnknown field", Unknown8);          // Unknown field
            writer.WriteLine("Unknown9: {0} \t\t\tUnknown field", Unknown9);          // Unknown field
            writer.WriteLine("AnalyzerPre: {0} \t\t\t0: Off, 1: On", AnalyzerPre);       // 0: Off, 1: On
            writer.WriteLine("AnalyzerPost: {0} \t\t\t0: Off, 1: On", AnalyzerPost);      // 0: Off, 1: On
            writer.WriteLine("AnalyzerSC: {0} \t\t\t0: Off, 1: On", AnalyzerSC);        // 0: Off, 1: On
            writer.WriteLine("AnalyzerRange: {0} \t\t\tAnalyzer Range in dB. 0.0: 60dB, 0.5: 90dB, 1.0: 120dB", AnalyzerRange);     // Analyzer Range in dB. 0.0: 60dB, 0.5: 90dB, 1.0: 120dB
            writer.WriteLine("AnalyzerResolution: {0} \t\t\tResolution. 0.0: Low, 0.333: Medium, 0,666: High, 1.00 Maximum  ", AnalyzerResolution);// Analyzer Resolution. 0.0: Low, 0.333: Medium, 0,666: High, 1.00 Maximum  
            writer.WriteLine("AnalyzerSpeed: {0} \t\t\tAnalyzer Speed. 0.0: Very Slow, 0.25: Slow, 0.5: Medium, 0.75: Fast, 1.0: Very Fast", AnalyzerSpeed);     // Analyzer Speed. 0.0: Very Slow, 0.25: Slow, 0.5: Medium, 0.75: Fast, 1.0: Very Fast
            writer.WriteLine("AnalyzerTilt: {0} \t\t\tAnalyzer Tilt. 0.0: 0.0, 0.25: 1.5, 0.5: 3.0, 0.75: 4.5, 1.0: 6.0  ", AnalyzerTilt);      // Analyzer Tilt. 0.0: 0.0, 0.25: 1.5, 0.5: 3.0, 0.75: 4.5, 1.0: 6.0  
            writer.WriteLine("Freeze: {0} \t\t\t0: Off, 1: On", Freeze);            // 0: Off, 1: On
            writer.WriteLine("Unknown18: {0} \t\t\tUnknown field", Unknown18);         // Unknown field
            writer.WriteLine("Unknown19: {0} \t\t\tUnknown field", Unknown19);         // Unknown field
            writer.WriteLine("Unknown20: {0} \t\t\tUnknown field", Unknown20);         // Unknown field
            writer.WriteLine("Unknown21: {0} \t\t\tUnknown field", Unknown21);         // Unknown field
            writer.WriteLine("Unknown22: {0} \t\t\tUnknown field", Unknown22);         // Unknown field
            writer.WriteLine("Unknown23: {0} \t\t\tUnknown field", Unknown23);         // Unknown field

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

    public enum ProQ2FilterType
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

    public enum ProQ2LPHPSlope
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
        public ProQ2ChannelMode ChannelMode { get; set; }
        public ProQ2FilterType FilterType { get; set; }
        public ProQ2LPHPSlope FilterLPHPSlope { get; set; }
        public ProQ2StereoPlacement FilterStereoPlacement { get; set; }
        public bool Enabled { get; set; }
        public double FilterFreq { get; set; }      // value range 10.0 -> 30000.0 Hz
        public double FilterGain { get; set; }      // + or - value in dB
        public double FilterQ { get; set; }         // value range 0.025 -> 40.00

        public override string ToString()
        {
            string stereoPlacement = "";
            switch (FilterStereoPlacement)
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

            return String.Format("[{4}] {0}: {1:0.00} Hz, {2:0.00} dB, Q: {3:0.00}, {5}, {6}", FilterType, FilterFreq, FilterGain, FilterQ, Enabled == true ? "On " : "Off", FilterLPHPSlope, stereoPlacement);
        }
    }
}