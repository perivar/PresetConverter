using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonUtils;

namespace PresetConverter
{
    /// <summary>
    /// FabfilterProQ Preset Class for saving a Fabfilter Pro Q Preset file (fft)
    /// </summary>
    public class FabfilterProQ
    {
        public List<ProQBand> Bands { get; set; }
        public int Version { get; set; }            // Normally 2
        public int ParameterCount { get; set; }     // Normally 190

        public float OutputGain { get; set; }        // -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
        public float OutputPan { get; set; }         // -1 to 1 (0 = middle)
        public float DisplayRange { get; set; }      // 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
        public float ProcessMode { get; set; }       // 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
        public float ChannelMode { get; set; }       // 0 = Left/Right, 1 = Mid/Side
        public float Bypass { get; set; }            // 0 = No bypass
        public float ReceiveMidi { get; set; }       // 0 = Enabled?
        public float Analyzer { get; set; }          // 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
        public float AnalyzerResolution { get; set; } // 0 - 3 : low - medium[x] - high - maximum
        public float AnalyzerSpeed { get; set; }     // 0 - 3 : very slow, slow, medium[x], fast
        public float SoloBand { get; set; }        	// -1

        public FabfilterProQ()
        {

        }

        public static float[] ReadFloats(string filePath)
        {
            BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian);

            string header = binFile.ReadString(4);
            if (header == "FPQr")
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
                    Console.Error.WriteLine("Failed reading floats: {0}", e);
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

        public static float[] Convert2FabfilterProQFloats(float[] ieeeFloatParameters)
        {
            var floatList = new List<float>();
            int counter = 0;

            // How many bands are enabled?
            floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 24)); // Number of enabled bands

            for (int i = 0; i < 24; i++)
            {
                floatList.Add(IEEEFloatToFrequencyFloat(ieeeFloatParameters[counter++]));                           // value range 10.0 -> 30000.0 Hz
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, -30, 30));    // + or - value in dB
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));       // value range 0.025 -> 40.00
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 5));       // 0 - 5
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 3));       // 0 = 6 dB/oct, 1 = 12 dB/oct, 2 = 24 dB/oct, 3 = 48 dB/oct
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 2));       // 0 = Left, 1 = Right, 2 = Stereo
                floatList.Add(MathUtils.ConvertAndMaintainRatio(ieeeFloatParameters[counter++], 0, 1, 0, 1));       // always 1.0?
            }

            for (int i = counter; i < ieeeFloatParameters.Length; i++)
            {
                floatList.Add(ieeeFloatParameters[i]);
            }

            return floatList.ToArray();
        }

        public static FabfilterProQ Convert2FabfilterProQ(float[] ieeeFloatParameters)
        {
            var preset = new FabfilterProQ();
            preset.Bands = new List<ProQBand>();

            // convert the ieeefloat parameters to fabfilter floats
            var floatArray = Convert2FabfilterProQFloats(ieeeFloatParameters);
            int index = 0;

            // Read in how many bands are enabled
            var enabledBandCount = floatArray[index++];

            for (int i = 0; i < 24; i++)
            {
                var band = new ProQBand();

                band.FilterFreq = FabfilterProQ.FreqConvertBack(floatArray[index++]);
                band.FilterGain = floatArray[index++]; // actual gain in dB
                band.FilterQ = FabfilterProQ.QConvertBack(floatArray[index++]);

                // 0 - 5
                var filterType = floatArray[index++];
                switch (filterType)
                {
                    case (float)ProQFilterType.Bell:
                        band.FilterType = ProQFilterType.Bell;
                        break;
                    case (float)ProQFilterType.LowShelf:
                        band.FilterType = ProQFilterType.LowShelf;
                        break;
                    case (float)ProQFilterType.LowCut:
                        band.FilterType = ProQFilterType.LowCut;
                        break;
                    case (float)ProQFilterType.HighShelf:
                        band.FilterType = ProQFilterType.HighShelf;
                        break;
                    case (float)ProQFilterType.HighCut:
                        band.FilterType = ProQFilterType.HighCut;
                        break;
                    case (float)ProQFilterType.Notch:
                        band.FilterType = ProQFilterType.Notch;
                        break;
                    default:
                        // throw new ArgumentOutOfRangeException(string.Format("Filter type is outside range: {0}", filterType));
                        band.FilterType = ProQFilterType.Bell;
                        break;
                }

                // 0 = 6 dB/oct, 1 = 12 dB/oct, 2 = 24 dB/oct, 3 = 48 dB/oct
                var filterSlope = floatArray[index++];
                switch (filterSlope)
                {
                    case (float)ProQLPHPSlope.Slope6dB_oct:
                        band.FilterLPHPSlope = ProQLPHPSlope.Slope6dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope12dB_oct:
                        band.FilterLPHPSlope = ProQLPHPSlope.Slope12dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope24dB_oct:
                        band.FilterLPHPSlope = ProQLPHPSlope.Slope24dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope48dB_oct:
                        band.FilterLPHPSlope = ProQLPHPSlope.Slope48dB_oct;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter slope is outside range: {0}", filterSlope));
                }

                // 0 = Left, 1 = Right, 2 = Stereo
                var filterStereoPlacement = floatArray[index++];
                switch (filterStereoPlacement)
                {
                    case (float)ProQStereoPlacement.Left:
                        band.FilterStereoPlacement = ProQStereoPlacement.Left;
                        break;
                    case (float)ProQStereoPlacement.Right:
                        band.FilterStereoPlacement = ProQStereoPlacement.Right;
                        break;
                    case (float)ProQStereoPlacement.Stereo:
                        band.FilterStereoPlacement = ProQStereoPlacement.Stereo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter stereo placement is outside range: {0}", filterStereoPlacement));
                }

                // always 1.0 ?
                var unknown = floatArray[index++];

                // check if band is enabled
                if (enabledBandCount > 0 && enabledBandCount > i) band.Enabled = true;

                preset.Bands.Add(band);
            }

            preset.OutputGain = floatArray[index++];      	// -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
            preset.OutputPan = floatArray[index++];       	// -1 to 1 (0 = middle)
            preset.DisplayRange = floatArray[index++];    	// 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
            preset.ProcessMode = floatArray[index++];     	// 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
            preset.ChannelMode = floatArray[index++];     	// 0 = Left/Right, 1 = Mid/Side
            preset.Bypass = floatArray[index++];           	// 0 = No bypass
            preset.ReceiveMidi = floatArray[index++];     	// 0 = Enabled?
            preset.Analyzer = floatArray[index++];         	// 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
            if (index <= floatArray.Length - 4) preset.AnalyzerResolution = floatArray[index++]; // 0 - 3 : low - medium[x] - high - maximum
            if (index <= floatArray.Length - 4) preset.AnalyzerSpeed = floatArray[index++];   	// 0 - 3 : very slow, slow, medium[x], fast
            if (index <= floatArray.Length - 4) preset.SoloBand = floatArray[index++];        	// -1

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
            if (header != "FPQr") return false;

            Version = binFile.ReadInt32();
            ParameterCount = binFile.ReadInt32();

            // Read in how many bands are enabled
            var enabledBandCount = binFile.ReadSingle();

            Bands = new List<ProQBand>();
            for (int i = 0; i < 24; i++)
            {
                var band = new ProQBand();

                band.FilterFreq = FreqConvertBack(binFile.ReadSingle());
                band.FilterGain = binFile.ReadSingle(); // actual gain in dB
                band.FilterQ = QConvertBack(binFile.ReadSingle());

                // 0 - 5
                var filterType = binFile.ReadSingle();
                switch (filterType)
                {
                    case (float)ProQFilterType.Bell:
                        band.FilterType = ProQFilterType.Bell;
                        break;
                    case (float)ProQFilterType.LowShelf:
                        band.FilterType = ProQFilterType.LowShelf;
                        break;
                    case (float)ProQFilterType.LowCut:
                        band.FilterType = ProQFilterType.LowCut;
                        break;
                    case (float)ProQFilterType.HighShelf:
                        band.FilterType = ProQFilterType.HighShelf;
                        break;
                    case (float)ProQFilterType.HighCut:
                        band.FilterType = ProQFilterType.HighCut;
                        break;
                    case (float)ProQFilterType.Notch:
                        band.FilterType = ProQFilterType.Notch;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter type is outside range: {0}", filterType));
                }

                // 0 = 6 dB/oct, 1 = 12 dB/oct, 2 = 24 dB/oct, 3 = 48 dB/oct
                var filterSlope = binFile.ReadSingle();
                switch (filterSlope)
                {
                    case (float)ProQLPHPSlope.Slope6dB_oct:
                        band.FilterLPHPSlope = ProQLPHPSlope.Slope6dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope12dB_oct:
                        band.FilterLPHPSlope = ProQLPHPSlope.Slope12dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope24dB_oct:
                        band.FilterLPHPSlope = ProQLPHPSlope.Slope24dB_oct;
                        break;
                    case (float)ProQLPHPSlope.Slope48dB_oct:
                        band.FilterLPHPSlope = ProQLPHPSlope.Slope48dB_oct;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter slope is outside range: {0}", filterSlope));
                }

                // 0 = Left, 1 = Right, 2 = Stereo
                var filterStereoPlacement = binFile.ReadSingle();
                switch (filterStereoPlacement)
                {
                    case (float)ProQStereoPlacement.Left:
                        band.FilterStereoPlacement = ProQStereoPlacement.Left;
                        break;
                    case (float)ProQStereoPlacement.Right:
                        band.FilterStereoPlacement = ProQStereoPlacement.Right;
                        break;
                    case (float)ProQStereoPlacement.Stereo:
                        band.FilterStereoPlacement = ProQStereoPlacement.Stereo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter stereo placement is outside range: {0}", filterStereoPlacement));
                }

                // always 1.0 ?
                var unknown = binFile.ReadSingle();

                // check if band is enabled
                if (enabledBandCount > 0 && enabledBandCount > i) band.Enabled = true;

                Bands.Add(band);
            }

            OutputGain = binFile.ReadSingle();      	// -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
            OutputPan = binFile.ReadSingle();       	// -1 to 1 (0 = middle)
            DisplayRange = binFile.ReadSingle();    	// 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
            ProcessMode = binFile.ReadSingle();     	// 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
            ChannelMode = binFile.ReadSingle();     	// 0 = Left/Right, 1 = Mid/Side
            Bypass = binFile.ReadSingle();           	// 0 = No bypass
            ReceiveMidi = binFile.ReadSingle();     	// 0 = Enabled?
            Analyzer = binFile.ReadSingle();         	// 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
            if (binFile.Position <= binFile.Length - 4) AnalyzerResolution = binFile.ReadSingle();  // 0 - 3 : low - medium[x] - high - maximum
            if (binFile.Position <= binFile.Length - 4) AnalyzerSpeed = binFile.ReadSingle();   	// 0 - 3 : very slow, slow, medium[x], fast
            if (binFile.Position <= binFile.Length - 4) SoloBand = binFile.ReadSingle();        	// -1

            binFile.Close();

            return true;
        }

        public bool Write(string filePath)
        {
            BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian, true);
            binFile.Write("FPQr");
            binFile.Write((int)Version);
            binFile.Write((int)Bands.Count * 7 + 12);

            // How many bands are enabled?
            var enabledBandCount = Bands.Count(b => b.Enabled);
            binFile.Write((float)enabledBandCount);

            for (int i = 0; i < 24; i++)
            {
                if (i < Bands.Count)
                {
                    binFile.Write((float)FabfilterProQ.FreqConvert(Bands[i].FilterFreq));
                    binFile.Write((float)Bands[i].FilterGain);
                    binFile.Write((float)FabfilterProQ.QConvert(Bands[i].FilterQ));
                    binFile.Write((float)Bands[i].FilterType);
                    binFile.Write((float)Bands[i].FilterLPHPSlope);
                    binFile.Write((float)Bands[i].FilterStereoPlacement);
                    binFile.Write((float)1);
                }
                else
                {
                    binFile.Write((float)FabfilterProQ.FreqConvert(1000));
                    binFile.Write((float)0);
                    binFile.Write((float)FabfilterProQ.QConvert(1));
                    binFile.Write((float)ProQFilterType.Bell);
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

            binFile.Close();

            return true;
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

    public enum ProQFilterType
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
        Left = 0,
        Right = 1,
        Stereo = 2, // (default)
    }

    public class ProQBand
    {
        public ProQFilterType FilterType { get; set; }
        public ProQLPHPSlope FilterLPHPSlope { get; set; }
        public ProQStereoPlacement FilterStereoPlacement { get; set; }
        public bool Enabled { get; set; }
        public double FilterFreq { get; set; }      // value range 10.0 -> 30000.0 Hz
        public double FilterGain { get; set; }      // + or - value in dB
        public double FilterQ { get; set; }         // value range 0.025 -> 40.00

        public override string ToString()
        {
            return String.Format("[{4}] {0}: {1:0.00} Hz, {2:0.00} dB, Q: {3:0.00}, {5}, {6}", FilterType, FilterFreq, FilterGain, FilterQ, Enabled == true ? "On " : "Off", FilterLPHPSlope, FilterStereoPlacement);
        }
    }
}