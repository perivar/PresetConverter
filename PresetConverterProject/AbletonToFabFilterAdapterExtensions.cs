using System;
using Serilog;

namespace PresetConverter
{
    // see http://gigi.nullneuron.net/gigilabs/the-adapter-design-pattern-for-dtos-in-c/
    public static class AbletonToFabFilterAdapterExtensions
    {
        public static FabfilterProQ3 ToFabfilterProQ3(this AbletonEq8 eq)
        {
            var fabfilterProQ3 = new FabfilterProQ3
            {
                Bands = new List<ProQ3Band>(),
                UnknownParameters = new List<float>()
            };

            foreach (var band in eq.Bands)
            {
                if (band.Parameter.Equals("ParameterA"))
                {
                    var proQ3Band = new ProQ3Band
                    {
                        Enabled = band.IsOn,
                        Gain = band.Gain,
                        Frequency = band.Freq,
                        Q = band.Q,
                        Slope = ProQ3Slope.Slope24dB_oct,
                        StereoPlacement = ProQ3StereoPlacement.Stereo
                    };

                    switch (band.Mode)
                    {
                        case AbletonEq8.BandMode.LowCut48:
                            proQ3Band.Shape = ProQ3Shape.LowCut;
                            proQ3Band.Slope = ProQ3Slope.Slope48dB_oct;
                            break;
                        case AbletonEq8.BandMode.LowCut12:
                            proQ3Band.Shape = ProQ3Shape.LowCut;
                            proQ3Band.Slope = ProQ3Slope.Slope12dB_oct;
                            break;
                        case AbletonEq8.BandMode.LeftShelf:
                            proQ3Band.Shape = ProQ3Shape.LowShelf;
                            break;
                        case AbletonEq8.BandMode.Bell:
                            proQ3Band.Shape = ProQ3Shape.Bell;
                            break;
                        case AbletonEq8.BandMode.Notch:
                            proQ3Band.Shape = ProQ3Shape.Notch;
                            break;
                        case AbletonEq8.BandMode.RightShelf:
                            proQ3Band.Shape = ProQ3Shape.HighShelf;
                            break;
                        case AbletonEq8.BandMode.HighCut12:
                            proQ3Band.Shape = ProQ3Shape.HighCut;
                            proQ3Band.Slope = ProQ3Slope.Slope12dB_oct;
                            break;
                        case AbletonEq8.BandMode.HighCut48:
                            proQ3Band.Shape = ProQ3Shape.HighCut;
                            proQ3Band.Slope = ProQ3Slope.Slope48dB_oct;
                            break;
                    }

                    Log.Debug(proQ3Band.ToString());
                    fabfilterProQ3.Bands.Add(proQ3Band);
                }
            }

            return fabfilterProQ3;
        }
    }
}