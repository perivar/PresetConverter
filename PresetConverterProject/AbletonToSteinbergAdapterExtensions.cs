using Serilog;

namespace PresetConverter
{
    // see http://gigi.nullneuron.net/gigilabs/the-adapter-design-pattern-for-dtos-in-c/
    public static class AbletonToSteinbergAdapterExtensions
    {
        public static SteinbergFrequency ToSteinbergFrequency(this AbletonEq8 eq)
        {
            var frequency = new SteinbergFrequency();

            if (eq.Mode != AbletonEq8.ChannelMode.Stereo)
            {
                throw new NotImplementedException($"Only Stereo conversion is supported. ChannelMode was {eq.Mode}!");
            }

            foreach (var band in eq.Bands)
            {
                if (band.Parameter.Equals("ParameterA"))
                {
                    int bandNumber = band.Number + 1; // zero indexed
                    frequency.Parameters[string.Format("equalizerAbandon{0}", bandNumber)].Number = band.IsOn ? 1.00 : 0.00;
                    frequency.Parameters[string.Format("equalizerAgain{0}", bandNumber)].Number = band.Gain;
                    frequency.Parameters[string.Format("equalizerAfreq{0}", bandNumber)].Number = band.Freq;
                    frequency.Parameters[string.Format("equalizerAq{0}", bandNumber)].Number = band.Q;

                    switch (band.Mode)
                    {
                        case AbletonEq8.BandMode.LowCut48:
                            frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode1And8.Cut48;
                            break;
                        case AbletonEq8.BandMode.LowCut12:
                            frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode1And8.Cut12;
                            break;
                        case AbletonEq8.BandMode.LeftShelf:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode1And8.LowShelf;
                            }
                            else
                            {
                                frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode2To7.LowShelf;
                            }
                            break;
                        case AbletonEq8.BandMode.Bell:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode1And8.Peak;
                            }
                            else
                            {
                                frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode2To7.Peak;
                            }
                            break;
                        case AbletonEq8.BandMode.Notch:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode1And8.Notch;
                            }
                            else
                            {
                                frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode2To7.Notch;
                            }
                            break;
                        case AbletonEq8.BandMode.RightShelf:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode1And8.HighShelf;
                            }
                            else
                            {
                                frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode2To7.HighShelf;
                            }
                            break;
                        case AbletonEq8.BandMode.HighCut12:
                            frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode1And8.Cut12;
                            break;
                        case AbletonEq8.BandMode.HighCut48:
                            frequency.Parameters[string.Format("equalizerAtype{0}", bandNumber)].Number = SteinbergFrequency.BandMode1And8.Cut48;
                            break;
                    }

                    Log.Debug(band.ToString());
                }
            }

            return frequency;
        }

        public static SteinbergCompressor ToSteinbergCompressor(this AbletonCompressor comp)
        {
            var compressor = new SteinbergCompressor();

            compressor.Parameters["threshold"].Number = 20 * Math.Log10(comp.Threshold); // 0.454823315 = -6.84dB, 0.0150452089 = -36.5dB, 0.110704564 = -19.1dB, 1 = 0.0dB, 0.151618019 = -16.4dB

            if (comp.Ratio == AbletonCompressor.MaxFloatMinusEpsilon)
            {
                compressor.Parameters["ratio"].Number = 2.0f;
                compressor.Parameters["limit"].Number = 1.0f;
            }
            else
            {
                compressor.Parameters["ratio"].Number = comp.Ratio;
                compressor.Parameters["limit"].Number = 0.0f;
            }

            compressor.Parameters["attack"].Number = comp.Attack;
            compressor.Parameters["release"].Number = comp.Release;
            compressor.Parameters["autorelease"].Number = comp.AutoReleaseControlOnOff == true ? 1.00 : 0.00;
            compressor.Parameters["hold"].Number = 0.00;
            compressor.Parameters["makeUp"].Number = comp.Gain;
            compressor.Parameters["automakeup"].Number = comp.GainCompensation == true ? 1.00 : 0.00;
            compressor.Parameters["softknee"].Number = comp.Knee > 6.00 ? 1.00 : 0.00;  // Knee value of 0.00 is hard knee, up to 18.00 dB (default 6.00 dB)
            compressor.Parameters["rms"].Number = comp.Model == 1 ? 100.00 : 00.00;     // 0.00 - 100.00 - Model: 0 = Peak, 1 = RMS, 2 = Expand
            compressor.Parameters["drymix"].Number = (1 - comp.DryWet) * 100;           // 0.00 - 100.00

            Log.Debug(comp.ToString());

            return compressor;
        }

    }
}