using System;
using System.Collections.Generic;
using System.IO;
using CommonUtils;

namespace PresetConverter
{
    /// <summary>
    /// Class for converting a REW file to a Fabfilter ProQ Preset file
    /// </summary>
    public static class REWToFabfilterAdapterExtensions
    {
        public static FabfilterProQ ToFabfilterProQ(this REWEQFilters filters)
        {
            var preset = new FabfilterProQ();

            preset.Version = 2;
            preset.Bands = new List<ProQBand>();

            foreach (REWEQBand filter in filters)
            {
                var band = new ProQBand();
                band.Frequency = filter.FilterFreq;
                band.Gain = filter.FilterGain;
                band.Q = filter.FilterQ;
                band.Enabled = filter.Enabled;
                switch (filter.FilterType)
                {
                    case REWEQFilterType.PK:
                        band.Shape = ProQShape.Bell;
                        break;
                    case REWEQFilterType.LP:
                        band.Shape = ProQShape.HighCut;
                        break;
                    case REWEQFilterType.HP:
                        band.Shape = ProQShape.LowCut;
                        break;
                    case REWEQFilterType.LS:
                        band.Shape = ProQShape.LowShelf;
                        break;
                    case REWEQFilterType.HS:
                        band.Shape = ProQShape.HighShelf;
                        break;
                    default:
                        band.Shape = ProQShape.Bell;
                        break;
                }
                band.LPHPSlope = ProQLPHPSlope.Slope24dB_oct;
                band.StereoPlacement = ProQStereoPlacement.Stereo;

                preset.Bands.Add(band);
            }

            // Add empty bands
            for (int i = preset.Bands.Count; i < 24; i++)
            {
                var band = new ProQBand();

                band.Frequency = FabfilterProQ.FreqConvert(1000);
                band.Gain = 0;
                band.Q = FabfilterProQ.QConvert(1);
                band.Enabled = true;
                band.Shape = ProQShape.Bell;
                band.LPHPSlope = ProQLPHPSlope.Slope24dB_oct;
                band.StereoPlacement = ProQStereoPlacement.Stereo;

                preset.Bands.Add(band);
            }

            preset.OutputGain = 0;      	// -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
            preset.OutputPan = 0;       	// -1 to 1 (0 = middle)
            preset.DisplayRange = 2;    	// 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
            preset.ProcessMode = 0;     	// 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
            preset.ChannelMode = 0;     	// 0 = Left/Right, 1 = Mid/Side
            preset.Bypass = 0;           	// 0 = No bypass
            preset.ReceiveMidi = 0;     	// 0 = Enabled?
            preset.Analyzer = 3;         	// 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
            preset.AnalyzerResolution = 1;  // 0 - 3 : low - medium[x] - high - maximum
            preset.AnalyzerSpeed = 2;   	// 0 - 3 : very slow, slow, medium[x], fast
            preset.SoloBand = -1;        	// -1

            return preset;
        }
    }
}