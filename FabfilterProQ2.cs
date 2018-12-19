using System;
using System.Collections.Generic;
using System.IO;
using CommonUtils;

namespace PresetConverter
{
    /// <summary>
    /// FabfilterProQ Preset Class for saving a Fabfilter ProQ Preset file (fft)
    /// </summary>
    public class FabfilterProQ2 : Preset
    {
        public List<ProQBand> ProQBands { get; set; }

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

        public FabfilterProQ2()
        {

        }

        public bool Read(string filePath)
        {
            BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian);

            string header = binFile.ReadString(4);
            int var1 = binFile.ReadInt32();
            int var2 = binFile.ReadInt32();
            float count = binFile.ReadSingle();

            ProQBands = new List<ProQBand>();
            for (int i = 0; i < 24; i++)
            {
                var band = new ProQBand();

                band.FilterFreq = FabfilterProQ.FreqConvertBack(binFile.ReadSingle());
                band.FilterGain = binFile.ReadSingle(); // actual gain in dB
                band.FilterQ = FabfilterProQ.QConvertBack(binFile.ReadSingle());

                // 0 - 5
                switch (binFile.ReadSingle())
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
                switch (binFile.ReadSingle())
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
                switch (binFile.ReadSingle())
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
                        band.FilterStereoPlacement = ProQStereoPlacement.Stereo;
                        break;
                }

                // 0 = Disabled
                band.Enabled = binFile.ReadSingle() == 1 ? true : false;

                ProQBands.Add(band);
            }

            OutputGain = binFile.ReadSingle();      	// -1 to 1 (- Infinity to +36 dB , 0 = 0 dB)
            OutputPan = binFile.ReadSingle();       	// -1 to 1 (0 = middle)
            DisplayRange = binFile.ReadSingle();    	// 0 = 6dB, 1 = 12dB, 2 = 30dB, 3 = 3dB
            ProcessMode = binFile.ReadSingle();     	// 0 = zero latency, 1 = lin.phase.low - medium - high - maximum
            ChannelMode = binFile.ReadSingle();     	// 0 = Left/Right, 1 = Mid/Side
            Bypass = binFile.ReadSingle();           	// 0 = No bypass
            ReceiveMidi = binFile.ReadSingle();     	// 0 = Enabled?
            Analyzer = binFile.ReadSingle();         	// 0 = Off, 1 = Pre, 2 = Post, 3 = Pre+Post
            AnalyzerResolution = binFile.ReadSingle();  // 0 - 3 : low - medium[x] - high - maximum
            AnalyzerSpeed = binFile.ReadSingle();   	// 0 - 3 : very slow, slow, medium[x], fast
            SoloBand = binFile.ReadSingle();        	// -1

            binFile.Close();

            return true;
        }

        public bool Write(string filePath)
        {
            throw new NotImplementedException();
        }
    }
}