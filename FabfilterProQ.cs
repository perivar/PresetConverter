using System;
using System.Collections.Generic;
using System.IO;
using CommonUtils;

namespace PresetConverter
{
    /// <summary>
    /// FabfilterProQ Preset Class for saving a Fabfilter ProQ Preset file (fft)
    /// </summary>
    public static class FabfilterProQ
    {
        public static bool Convert2FabfilterProQ(float[] parameters)
        {
            List<ProQBand> proQBands = new List<ProQBand>();

            int index = 0;
            for (int i = 0; i < 24; i++)
            {
                var band = new ProQBand();

                band.FilterFreq = FabfilterProQ2.FreqConvertBack(parameters[index++]);
                band.FilterGain = parameters[index++]; // actual gain in dB
                band.FilterQ = FabfilterProQ2.QConvertBack(parameters[index++]);

                // 0 - 5
                switch (parameters[index++])
                {
                    case (float)ProQFilterType.Bell:
                        band.FilterType = ProQFilterType.Bell;
                        break;
                    case (float)ProQFilterType.HighCut:
                        band.FilterType = ProQFilterType.HighCut;
                        break;
                    case (float)ProQFilterType.LowCut:
                        band.FilterType = ProQFilterType.LowCut;
                        break;
                    case (float)ProQFilterType.LowShelf:
                        band.FilterType = ProQFilterType.LowShelf;
                        break;
                    case (float)ProQFilterType.HighShelf:
                        band.FilterType = ProQFilterType.HighShelf;
                        break;
                    default:
                        band.FilterType = ProQFilterType.Bell;
                        break;
                }

                // 0 = 6 dB/oct, 1 = 12 dB/oct, 2 = 24 dB/oct, 3 = 48 dB/oct
                switch (parameters[index++])
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
                        band.FilterLPHPSlope = ProQLPHPSlope.Slope24dB_oct;
                        break;
                }

                // 0 = Left, 1 = Right, 2 = Stereo
                band.FilterStereoPlacement = parameters[index++] == 0.125 ? ProQStereoPlacement.Stereo : ProQStereoPlacement.Left;

                // 0 = Disabled
                band.Enabled = parameters[index++] == 1 ? true : false;

                proQBands.Add(band);
            }

            float outputGain = parameters[index++];      	// -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
            float outputPan = parameters[index++];       	// -1 to 1 (0 = middle)
            float displayRange = parameters[index++];    	// 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
            float processMode = parameters[index++];     	// 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
            float channelMode = parameters[index++];     	// 0 = Left/Right, 1 = Mid/Side
            float bypass = parameters[index++];           	// 0 = No bypass
            float receiveMidi = parameters[index++];     	// 0 = Enabled?
            float analyzer = parameters[index++];         	// 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
            float analyzerResolution = parameters[index++]; // 0 - 3 : low - medium[x] - high - maximum
            float analyzerSpeed = parameters[index++];   	// 0 - 3 : very slow, slow, medium[x], fast
            float soloBand = parameters[index++];        	// -1

            return false;
        }

        public static bool Convert2FabfilterProQ(REWEQFilters filters, string filePath)
        {
            List<ProQBand> proQBands = new List<ProQBand>();
            foreach (REWEQBand filter in filters)
            {
                ProQBand band = new ProQBand();
                band.FilterFreq = filter.FilterFreq;
                band.FilterGain = filter.FilterGain;
                band.FilterQ = filter.FilterQ;
                band.Enabled = filter.Enabled;
                switch (filter.FilterType)
                {
                    case REWEQFilterType.PK:
                        band.FilterType = ProQFilterType.Bell;
                        break;
                    case REWEQFilterType.LP:
                        band.FilterType = ProQFilterType.HighCut;
                        break;
                    case REWEQFilterType.HP:
                        band.FilterType = ProQFilterType.LowCut;
                        break;
                    case REWEQFilterType.LS:
                        band.FilterType = ProQFilterType.LowShelf;
                        break;
                    case REWEQFilterType.HS:
                        band.FilterType = ProQFilterType.HighShelf;
                        break;
                    default:
                        band.FilterType = ProQFilterType.Bell;
                        break;
                }
                band.FilterLPHPSlope = ProQLPHPSlope.Slope24dB_oct;
                band.FilterStereoPlacement = ProQStereoPlacement.Stereo;

                proQBands.Add(band);
            }

            BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian, true);
            binFile.Write("FPQr");
            binFile.Write((int)2);
            binFile.Write((int)proQBands.Count);

            for (int i = 0; i < 24; i++)
            {
                if (i < proQBands.Count)
                {
                    binFile.Write((float)(proQBands[i].Enabled ? 1 : 2));
                    binFile.Write((float)FabfilterProQ2.FreqConvert(proQBands[i].FilterFreq));
                    binFile.Write((float)proQBands[i].FilterGain);
                    binFile.Write((float)FabfilterProQ2.QConvert(proQBands[i].FilterQ));
                    binFile.Write((float)proQBands[i].FilterType);
                    binFile.Write((float)proQBands[i].FilterLPHPSlope);
                    binFile.Write((float)proQBands[i].FilterStereoPlacement);
                }
                else
                {
                    binFile.Write((float)1);
                    binFile.Write((float)FabfilterProQ2.FreqConvert(1000));
                    binFile.Write((float)0);
                    binFile.Write((float)FabfilterProQ2.QConvert(1));
                    binFile.Write((float)ProQFilterType.Bell);
                    binFile.Write((float)ProQLPHPSlope.Slope24dB_oct);
                    binFile.Write((float)ProQStereoPlacement.Stereo);
                }
            }

            binFile.Write((float)0); // float output_gain;      // -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
            binFile.Write((float)0); // float output_pan;       // -1 to 1 (0 = middle)
            binFile.Write((float)2); // float display_range;    // 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
            binFile.Write((float)0); // float process_mode;     // 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
            binFile.Write((float)0); // float channel_mode;     // 0 = Left/Right, 1 = Mid/Side
            binFile.Write((float)0); // float bypass;           // 0 = No bypass
            binFile.Write((float)0); // float receive_midi;     // 0 = Enabled?
            binFile.Write((float)3); // float analyzer;         // 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
            binFile.Write((float)1); // float analyzer_resolution;  // 0 - 3 : low - medium[x] - high - maximum
            binFile.Write((float)2); // float analyzer_speed;   // 0 - 3 : very slow, slow, medium[x], fast
            binFile.Write((float)-1); // float solo_band;        // -1
            binFile.Close();

            return true;
        }
    }
}