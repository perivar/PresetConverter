using System;

namespace AbletonLiveConverter
{
    // see http://gigi.nullneuron.net/gigilabs/the-adapter-design-pattern-for-dtos-in-c/
    public static class AbletonToSteinbergAdapterExtensions
    {
        public static SteinbergFrequency ToSteinbergFrequency(this AbletonEq8 eq)
        {
            var frequency = new SteinbergFrequency();

            foreach (var band in eq.Bands)
            {
                if (band.Parameter.Equals("ParameterA"))
                {
                    int bandNumber = band.Number + 1; // zero indexed
                    frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].NumberValue = band.IsOn ? 1.00 : 0.00;
                    frequency.Parameters[String.Format("equalizerAgain{0}", bandNumber)].NumberValue = band.Gain;
                    frequency.Parameters[String.Format("equalizerAfreq{0}", bandNumber)].NumberValue = band.Freq;
                    frequency.Parameters[String.Format("equalizerAq{0}", bandNumber)].NumberValue = band.Q;

                    switch (band.Mode)
                    {
                        case AbletonEq8.BandMode.LowCut48:
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut48;
                            break;
                        case AbletonEq8.BandMode.LowCut12:
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut12;
                            break;
                        case AbletonEq8.BandMode.LeftShelf:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.LowShelf;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.LowShelf;
                            }
                            break;
                        case AbletonEq8.BandMode.Bell:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Peak;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.Peak;
                            }
                            break;
                        case AbletonEq8.BandMode.Notch:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Notch;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.Notch;
                            }
                            break;
                        case AbletonEq8.BandMode.RightShelf:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.HighShelf;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.HighShelf;
                            }
                            break;
                        case AbletonEq8.BandMode.HighCut12:
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut12;
                            break;
                        case AbletonEq8.BandMode.HighCut48:
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut48;
                            break;
                    }

                    Console.WriteLine(band);
                }
            }

            return frequency;
        }

        public static SteinbergCompressor ToSteinbergCompressor(this AbletonCompressor comp)
        {
            var compressor = new SteinbergCompressor();

            compressor.Parameters["threshold"].NumberValue = 20 * Math.Log10(comp.Threshold); // 0.454823315 = -6.84dB, 0.0150452089 = -36.5dB, 0.110704564 = -19.1dB, 1 = 0.0dB, 0.151618019 = -16.4dB
            compressor.Parameters["ratio"].NumberValue = comp.Ratio;
            compressor.Parameters["attack"].NumberValue = comp.Attack;
            compressor.Parameters["release"].NumberValue = comp.Release;
            compressor.Parameters["autorelease"].NumberValue = comp.AutoReleaseControlOnOff == true ? 1.00 : 0.00;
            compressor.Parameters["hold"].NumberValue = 0.00;
            compressor.Parameters["makeUp"].NumberValue = comp.Gain;
            compressor.Parameters["automakeup"].NumberValue = comp.GainCompensation == true ? 1.00 : 0.00;
            compressor.Parameters["softknee"].NumberValue = comp.Knee > 6.00 ? 1.00 : 0.00; // Knee value of 0.00 is hard knee, up to 18.00 dB (default 6.00 dB)
            compressor.Parameters["rms"].NumberValue = comp.Model == 1 ? 100.00 : 00.00; // 0.00 - 100.00 - Model: 0 = Peak, 1 = RMS, 2 = Expand
            compressor.Parameters["drymix"].NumberValue = (1 - comp.DryWet) * 100; // 0.00 - 100.00
            Console.WriteLine(comp);

            return compressor;
        }

    }
}