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

            // Frequency only support lowcut on the 1st band and highcut on the 8th band
            bool hasLowCutBand = eq.Bands.Any(band => band.FilterType == ProQFilterType.LowCut);
            bool hasHighCutBand = eq.Bands.Any(band => band.FilterType == ProQFilterType.HighCut);

            // get remaining bands that are not lowcut or highcut and sort by frequency
            var band2To7 = eq.Bands.Where(b => b.Enabled)
                                   .Where(b => b.FilterType != ProQFilterType.LowCut)
                                   .Where(b => b.FilterType != ProQFilterType.HighCut)
                                   .OrderBy(s => s.FilterFreq);

            if (hasLowCutBand)
            {
                int bandNumber = 1;
                var lowCutBand = eq.Bands.Where(band => band.FilterType == ProQFilterType.LowCut)
                                            .OrderBy(s => s.FilterFreq).ElementAt(0);

                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].NumberValue = lowCutBand.Enabled ? 1.00 : 0.00;
                frequency.Parameters[String.Format("equalizerAgain{0}", bandNumber)].NumberValue = lowCutBand.FilterGain;
                frequency.Parameters[String.Format("equalizerAfreq{0}", bandNumber)].NumberValue = lowCutBand.FilterFreq;
                frequency.Parameters[String.Format("equalizerAq{0}", bandNumber)].NumberValue = lowCutBand.FilterQ;

                switch (lowCutBand.FilterLPHPSlope)
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

                Log.Debug(lowCutBand.ToString());
            }

            if (hasHighCutBand)
            {
                int bandNumber = 8;
                var highCutBand = eq.Bands.Where(band => band.FilterType == ProQFilterType.HighCut)
                                            .OrderByDescending(s => s.FilterFreq).ElementAt(0);

                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].NumberValue = highCutBand.Enabled ? 1.00 : 0.00;
                frequency.Parameters[String.Format("equalizerAgain{0}", bandNumber)].NumberValue = highCutBand.FilterGain;
                frequency.Parameters[String.Format("equalizerAfreq{0}", bandNumber)].NumberValue = highCutBand.FilterFreq;
                frequency.Parameters[String.Format("equalizerAq{0}", bandNumber)].NumberValue = highCutBand.FilterQ;

                switch (highCutBand.FilterLPHPSlope)
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

                Log.Debug(highCutBand.ToString());
            }

            // rest of the bands (2-7)
            int startIndex = hasLowCutBand ? 2 : 1;
            int endIndex = hasHighCutBand ? 7 : 8;
            int index = 0;
            for (int bandNumber = startIndex; bandNumber <= endIndex; bandNumber++, index++)
            {
                var band = band2To7.ElementAtOrDefault(index);
                if (band != null)
                {
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
                else
                {
                    // disable band
                    frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].NumberValue = 0.00;
                }
            }

            return frequency;
        }

        public static SteinbergFrequency ToSteinbergFrequency(this FabfilterProQ2 eq)
        {
            var frequency = new SteinbergFrequency();

            // Frequency only support lowcut on the 1st band and highcut on the 8th band
            bool hasLowCutBand = eq.Bands.Any(band => band.FilterType == ProQ2FilterType.LowCut);
            bool hasHighCutBand = eq.Bands.Any(band => band.FilterType == ProQ2FilterType.HighCut);

            // get remaining bands that are not lowcut or highcut and sort by frequency
            var band2To7 = eq.Bands.Where(b => b.Enabled)
                                   .Where(b => b.FilterType != ProQ2FilterType.LowCut)
                                   .Where(b => b.FilterType != ProQ2FilterType.HighCut)
                                   .OrderBy(s => s.FilterFreq);

            if (hasLowCutBand)
            {
                int bandNumber = 1;
                var lowCutBand = eq.Bands.Where(band => band.FilterType == ProQ2FilterType.LowCut)
                                            .OrderBy(s => s.FilterFreq).ElementAt(0);

                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].NumberValue = lowCutBand.Enabled ? 1.00 : 0.00;
                frequency.Parameters[String.Format("equalizerAgain{0}", bandNumber)].NumberValue = lowCutBand.FilterGain;
                frequency.Parameters[String.Format("equalizerAfreq{0}", bandNumber)].NumberValue = lowCutBand.FilterFreq;
                frequency.Parameters[String.Format("equalizerAq{0}", bandNumber)].NumberValue = lowCutBand.FilterQ;

                switch (lowCutBand.FilterLPHPSlope)
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

                Log.Debug(lowCutBand.ToString());
            }

            if (hasHighCutBand)
            {
                int bandNumber = 8;
                var highCutBand = eq.Bands.Where(band => band.FilterType == ProQ2FilterType.HighCut)
                                            .OrderByDescending(s => s.FilterFreq).ElementAt(0);

                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].NumberValue = highCutBand.Enabled ? 1.00 : 0.00;
                frequency.Parameters[String.Format("equalizerAgain{0}", bandNumber)].NumberValue = highCutBand.FilterGain;
                frequency.Parameters[String.Format("equalizerAfreq{0}", bandNumber)].NumberValue = highCutBand.FilterFreq;
                frequency.Parameters[String.Format("equalizerAq{0}", bandNumber)].NumberValue = highCutBand.FilterQ;

                switch (highCutBand.FilterLPHPSlope)
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

                Log.Debug(highCutBand.ToString());
            }

            // rest of the bands (2-7)
            int startIndex = hasLowCutBand ? 2 : 1;
            int endIndex = hasHighCutBand ? 7 : 8;
            int index = 0;
            for (int bandNumber = startIndex; bandNumber <= endIndex; bandNumber++, index++)
            {
                var band = band2To7.ElementAtOrDefault(index);
                if (band != null)
                {
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
                else
                {
                    // disable band
                    frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].NumberValue = 0.00;
                }
            }

            return frequency;
        }
    }
}