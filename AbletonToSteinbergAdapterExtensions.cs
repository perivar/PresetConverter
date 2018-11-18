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
                    frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].Value = band.IsOn ? 1.00 : 0.00;
                    frequency.Parameters[String.Format("equalizerAgain{0}", bandNumber)].Value = band.Gain;
                    frequency.Parameters[String.Format("equalizerAfreq{0}", bandNumber)].Value = band.Freq;
                    frequency.Parameters[String.Format("equalizerAq{0}", bandNumber)].Value = band.Q;

                    switch (band.Mode)
                    {
                        case AbletonEq8.BandMode.LowCut48:
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode1And8.Cut48;
                            break;
                        case AbletonEq8.BandMode.LowCut12:
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode1And8.Cut12;
                            break;
                        case AbletonEq8.BandMode.LeftShelf:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode1And8.LowShelf;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode2To7.LowShelf;
                            }
                            break;
                        case AbletonEq8.BandMode.Bell:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode1And8.Peak;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode2To7.Peak;
                            }
                            break;
                        case AbletonEq8.BandMode.Notch:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode1And8.Notch;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode2To7.Notch;
                            }
                            break;
                        case AbletonEq8.BandMode.RightShelf:
                            if (bandNumber == 1 || bandNumber == 8)
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode1And8.HighShelf;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode2To7.HighShelf;
                            }
                            break;
                        case AbletonEq8.BandMode.HighCut12:
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode1And8.Cut12;
                            break;
                        case AbletonEq8.BandMode.HighCut48:
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = SteinbergFrequency.BandMode1And8.Cut48;
                            break;
                    }

                    Console.WriteLine(band);
                }
            }

            return frequency;
        }
    }
}