using System;
using System.Linq;
using Serilog;

namespace PresetConverter
{
    // see http://gigi.nullneuron.net/gigilabs/the-adapter-design-pattern-for-dtos-in-c/
    public static class FabfilterToSteinbergAdapterExtensions
    {
        public static SteinbergFrequency ToSteinbergFrequency(this FabfilterProQ eq)
        {
            var frequency = new SteinbergFrequency();

            int index = 1;
            foreach (var band in eq.Bands)
            {
                int bandNumber = index++;

                if (bandNumber > 8) break;

                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].NumberValue = band.Enabled ? 1.00 : 0.00;
                frequency.Parameters[String.Format("equalizerAgain{0}", bandNumber)].NumberValue = band.FilterGain;
                frequency.Parameters[String.Format("equalizerAfreq{0}", bandNumber)].NumberValue = band.FilterFreq;
                frequency.Parameters[String.Format("equalizerAq{0}", bandNumber)].NumberValue = band.FilterQ;

                switch (band.FilterType)
                {
                    case ProQFilterType.Bell:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Peak;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.Peak;
                        }
                        break;
                    case ProQFilterType.LowShelf:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.LowShelf;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.LowShelf;
                        }
                        break;
                    case ProQFilterType.LowCut:
                        switch (band.FilterLPHPSlope)
                        {
                            case ProQLPHPSlope.Slope6dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut6;
                                break;
                            case ProQLPHPSlope.Slope12dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut12;
                                break;
                            case ProQLPHPSlope.Slope24dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut24;
                                break;
                            case ProQLPHPSlope.Slope48dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut48;
                                break;
                        }
                        break;
                    case ProQFilterType.HighShelf:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.HighShelf;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.HighShelf;
                        }
                        break;
                    case ProQFilterType.HighCut:
                        switch (band.FilterLPHPSlope)
                        {
                            case ProQLPHPSlope.Slope6dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut6;
                                break;
                            case ProQLPHPSlope.Slope12dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut12;
                                break;
                            case ProQLPHPSlope.Slope24dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut24;
                                break;
                            case ProQLPHPSlope.Slope48dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut48;
                                break;
                        }
                        break;
                    case ProQFilterType.Notch:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Notch;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.Notch;
                        }
                        break;
                }

                Log.Debug(band.ToString());
            }

            return frequency;
        }

        public static SteinbergFrequency ToSteinbergFrequency(this FabfilterProQ2 eq)
        {
            var frequency = new SteinbergFrequency();

            // sort so that the lowcut is at the first elements and the high cut are the last (within only 8 bands)
            ProQ2FilterType[] customSortOrder = new[]
                {
                    ProQ2FilterType.LowCut,
                    ProQ2FilterType.LowShelf,
                    ProQ2FilterType.Bell,
                    ProQ2FilterType.Notch,
                    ProQ2FilterType.BandPass,
                    ProQ2FilterType.TiltShelf,
                    ProQ2FilterType.HighShelf,
                    ProQ2FilterType.HighCut
            };

            var sortedBands = eq.Bands.Where(b => b.Enabled).OrderBy(a => Array.IndexOf(customSortOrder, a.FilterType)).Take(8);

            int index = 1;
            foreach (var band in sortedBands)
            {
                int bandNumber = index++;

                if (bandNumber > 8) break;

                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].NumberValue = band.Enabled ? 1.00 : 0.00;
                frequency.Parameters[String.Format("equalizerAgain{0}", bandNumber)].NumberValue = band.FilterGain;
                frequency.Parameters[String.Format("equalizerAfreq{0}", bandNumber)].NumberValue = band.FilterFreq;
                frequency.Parameters[String.Format("equalizerAq{0}", bandNumber)].NumberValue = band.FilterQ;

                switch (band.FilterType)
                {
                    case ProQ2FilterType.BandPass:
                    case ProQ2FilterType.TiltShelf:
                    case ProQ2FilterType.Bell:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Peak;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.Peak;
                        }
                        break;
                    case ProQ2FilterType.LowShelf:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.LowShelf;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.LowShelf;
                        }
                        break;
                    case ProQ2FilterType.LowCut:
                        switch (band.FilterLPHPSlope)
                        {
                            case ProQ2LPHPSlope.Slope6dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut6;
                                break;
                            case ProQ2LPHPSlope.Slope12dB_oct:
                            case ProQ2LPHPSlope.Slope18dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut12;
                                break;
                            case ProQ2LPHPSlope.Slope24dB_oct:
                            case ProQ2LPHPSlope.Slope30dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut24;
                                break;
                            case ProQ2LPHPSlope.Slope36dB_oct:
                            case ProQ2LPHPSlope.Slope48dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut48;
                                break;
                            case ProQ2LPHPSlope.Slope72dB_oct:
                            case ProQ2LPHPSlope.Slope96dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut96;
                                break;
                        }
                        break;
                    case ProQ2FilterType.HighShelf:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.HighShelf;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.HighShelf;
                        }
                        break;
                    case ProQ2FilterType.HighCut:
                        switch (band.FilterLPHPSlope)
                        {
                            case ProQ2LPHPSlope.Slope6dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut6;
                                break;
                            case ProQ2LPHPSlope.Slope12dB_oct:
                            case ProQ2LPHPSlope.Slope18dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut12;
                                break;
                            case ProQ2LPHPSlope.Slope24dB_oct:
                            case ProQ2LPHPSlope.Slope30dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut24;
                                break;
                            case ProQ2LPHPSlope.Slope36dB_oct:
                            case ProQ2LPHPSlope.Slope48dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut48;
                                break;
                            case ProQ2LPHPSlope.Slope72dB_oct:
                            case ProQ2LPHPSlope.Slope96dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Cut96;
                                break;
                        }
                        break;
                    case ProQ2FilterType.Notch:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode1And8.Notch;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].NumberValue = SteinbergFrequency.BandMode2To7.Notch;
                        }
                        break;
                }

                Log.Debug(band.ToString());
            }

            return frequency;
        }
    }
}