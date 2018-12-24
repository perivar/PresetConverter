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

        public float[] PostPresetParameters;

        public float ChannelMode { get; set; }       // 0 = Left/Right, 1 = Mid/Side

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
                    case (float)ProQ2StereoPlacement.Left:
                        band.FilterStereoPlacement = ProQ2StereoPlacement.Left;
                        break;
                    case (float)ProQ2StereoPlacement.Right:
                        band.FilterStereoPlacement = ProQ2StereoPlacement.Right;
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
            preset.PostPresetParameters = new float[floatArray.Length - index];
            for (int i = 0, j = index; j < floatArray.Length; i++, j++)
            {
                preset.PostPresetParameters[i] = floatArray[j];

                // the third last is the ChannelMode
                // if (j == floatArray.Length - 3)
                // {
                //     preset.ChannelMode = floatArray[j];
                // }
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
                    case (float)ProQ2StereoPlacement.Left:
                        band.FilterStereoPlacement = ProQ2StereoPlacement.Left;
                        break;
                    case (float)ProQ2StereoPlacement.Right:
                        band.FilterStereoPlacement = ProQ2StereoPlacement.Right;
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
            PostPresetParameters = new float[remainingParameterCount];
            for (int i = 0; i < remainingParameterCount; i++)
            {
                PostPresetParameters[i] = binFile.ReadSingle();

                // the third last is the ChannelMode
                // if (j == floatArray.Length - 3)
                // {
                //     preset.ChannelMode = floatArray[j];
                // }
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

            for (int i = 0; i < PostPresetParameters.Length; i++)
            {
                binFile.Write(PostPresetParameters[i]);
            }

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
        Left = 0,
        Right = 1,
        Stereo = 2, // (default)
    }

    public class ProQ2Band
    {
        public ProQ2FilterType FilterType { get; set; }
        public ProQ2LPHPSlope FilterLPHPSlope { get; set; }
        public ProQ2StereoPlacement FilterStereoPlacement { get; set; }
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