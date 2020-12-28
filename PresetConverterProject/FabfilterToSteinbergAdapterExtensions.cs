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
            bool hasLowCutBand = eq.Bands.Any(band => band.Shape == ProQShape.LowCut);
            bool hasHighCutBand = eq.Bands.Any(band => band.Shape == ProQShape.HighCut);

            // get remaining bands that are not lowcut or highcut and sort by frequency
            var band2To7 = eq.Bands.Where(b => b.Enabled)
                                   .Where(b => b.Shape != ProQShape.LowCut)
                                   .Where(b => b.Shape != ProQShape.HighCut)
                                   .OrderBy(s => s.Frequency);

            if (hasLowCutBand)
            {
                int bandNumber = 1;
                var lowCutBand = eq.Bands.Where(band => band.Shape == ProQShape.LowCut)
                                            .OrderBy(s => s.Frequency).ElementAt(0);

                SetBand(lowCutBand, bandNumber, frequency);
            }

            if (hasHighCutBand)
            {
                int bandNumber = 8;
                var highCutBand = eq.Bands.Where(band => band.Shape == ProQShape.HighCut)
                                            .OrderByDescending(s => s.Frequency).ElementAt(0);

                SetBand(highCutBand, bandNumber, frequency);
            }

            // rest of the bands (2-7)
            int startIndex = hasLowCutBand ? 2 : 1;
            int endIndex = hasHighCutBand ? 7 : 8;
            int index = 0;
            for (int bandNumber = startIndex; bandNumber <= endIndex; bandNumber++, index++)
            {
                var band = band2To7.ElementAtOrDefault(index);
                SetBand(band, bandNumber, frequency);
            }

            return frequency;
        }

        private static void SetBand(ProQBand band, int bandNumber, SteinbergFrequency frequency)
        {
            if (band != null)
            {
                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].Number = band.Enabled ? 1.00 : 0.00;

                string channel = ""; // empty for main channel (channel 1). 'Ch2' for secondary channel 

                // due to the way fabfilter have only one stereo placement per band (frequency has two) we need to modify both channels in frequency
                // we could have in theory instead updated both channels per band in frequency
                if (band.StereoPlacement != ProQStereoPlacement.Stereo)
                {
                    switch (band.StereoPlacement)
                    {
                        case ProQStereoPlacement.LeftOrMid:
                            frequency.Parameters[String.Format("equalizerAon{0}", bandNumber)].Number = 1.0;
                            frequency.Parameters[String.Format("equalizerAon{0}Ch2", bandNumber)].Number = 0.0;

                            if (band.ChannelMode == ProQChannelMode.LeftRight)
                            {
                                frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.LeftRightModeLeft;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.MidSideModeMid;
                            }
                            break;
                        case ProQStereoPlacement.RightOrSide:
                            frequency.Parameters[String.Format("equalizerAon{0}", bandNumber)].Number = 0.0;
                            frequency.Parameters[String.Format("equalizerAon{0}Ch2", bandNumber)].Number = 1.0;
                            channel = "Ch2";

                            if (band.ChannelMode == ProQChannelMode.LeftRight)
                            {
                                frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.LeftRightModeRight;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.MidSideModeSide;
                            }
                            break;
                        case ProQStereoPlacement.Stereo:
                            // don't change - this is the default
                            break;
                    }
                }

                frequency.Parameters[String.Format("equalizerAgain{0}{1}", bandNumber, channel)].Number = band.Gain;
                frequency.Parameters[String.Format("equalizerAfreq{0}{1}", bandNumber, channel)].Number = band.Frequency;
                frequency.Parameters[String.Format("equalizerAq{0}{1}", bandNumber, channel)].Number = band.Q;

                switch (band.Shape)
                {
                    case ProQShape.Bell:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Peak;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.Peak;
                        }
                        break;
                    case ProQShape.LowShelf:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.LowShelf;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.LowShelf;
                        }
                        break;
                    case ProQShape.LowCut:
                        switch (band.LPHPSlope)
                        {
                            case ProQLPHPSlope.Slope6dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut6;
                                break;
                            case ProQLPHPSlope.Slope12dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut12;
                                break;
                            case ProQLPHPSlope.Slope24dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut24;
                                break;
                            case ProQLPHPSlope.Slope48dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut48;
                                break;
                        }
                        break;
                    case ProQShape.HighShelf:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.HighShelf;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.HighShelf;
                        }
                        break;
                    case ProQShape.HighCut:
                        switch (band.LPHPSlope)
                        {
                            case ProQLPHPSlope.Slope6dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut6;
                                break;
                            case ProQLPHPSlope.Slope12dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut12;
                                break;
                            case ProQLPHPSlope.Slope24dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut24;
                                break;
                            case ProQLPHPSlope.Slope48dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut48;
                                break;
                        }
                        break;
                    case ProQShape.Notch:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Notch;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.Notch;
                        }
                        break;
                }

                Log.Debug(band.ToString());
            }
            else
            {
                // disable band
                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].Number = 0.00;
            }
        }

        public static SteinbergFrequency ToSteinbergFrequency(this FabfilterProQ2 eq)
        {
            var frequency = new SteinbergFrequency();

            // Frequency only support lowcut on the 1st band and highcut on the 8th band
            bool hasLowCutBand = eq.Bands.Any(band => band.Shape == ProQ2Shape.LowCut);
            bool hasHighCutBand = eq.Bands.Any(band => band.Shape == ProQ2Shape.HighCut);

            // get remaining bands that are not lowcut or highcut and sort by frequency
            var band2To7 = eq.Bands.Where(b => b.Enabled)
                                   .Where(b => b.Shape != ProQ2Shape.LowCut)
                                   .Where(b => b.Shape != ProQ2Shape.HighCut)
                                   .OrderBy(s => s.Frequency);

            if (hasLowCutBand)
            {
                int bandNumber = 1;
                var lowCutBand = eq.Bands.Where(band => band.Shape == ProQ2Shape.LowCut)
                                            .OrderBy(s => s.Frequency).ElementAt(0);

                SetBand(lowCutBand, bandNumber, frequency);
            }

            if (hasHighCutBand)
            {
                int bandNumber = 8;
                var highCutBand = eq.Bands.Where(band => band.Shape == ProQ2Shape.HighCut)
                                            .OrderByDescending(s => s.Frequency).ElementAt(0);

                SetBand(highCutBand, bandNumber, frequency);
            }

            // rest of the bands (2-7)
            int startIndex = hasLowCutBand ? 2 : 1;
            int endIndex = hasHighCutBand ? 7 : 8;
            int index = 0;
            for (int bandNumber = startIndex; bandNumber <= endIndex; bandNumber++, index++)
            {
                var band = band2To7.ElementAtOrDefault(index);
                SetBand(band, bandNumber, frequency);
            }

            return frequency;
        }

        private static void SetBand(ProQ2Band band, int bandNumber, SteinbergFrequency frequency)
        {
            if (band != null)
            {
                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].Number = band.Enabled ? 1.00 : 0.00;

                string channel = ""; // empty for main channel (channel 1). 'Ch2' for secondary channel 

                // due to the way fabfilter have only one stereo placement per band (frequency has two) we need to modify both channels in frequency
                // we could have in theory instead updated both channels per band in frequency
                if (band.StereoPlacement != ProQ2StereoPlacement.Stereo)
                {
                    switch (band.StereoPlacement)
                    {
                        case ProQ2StereoPlacement.LeftOrMid:
                            frequency.Parameters[String.Format("equalizerAon{0}", bandNumber)].Number = 1.0;
                            frequency.Parameters[String.Format("equalizerAon{0}Ch2", bandNumber)].Number = 0.0;

                            if (band.ChannelMode == ProQ2ChannelMode.LeftRight)
                            {
                                frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.LeftRightModeLeft;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.MidSideModeMid;
                            }
                            break;
                        case ProQ2StereoPlacement.RightOrSide:
                            frequency.Parameters[String.Format("equalizerAon{0}", bandNumber)].Number = 0.0;
                            frequency.Parameters[String.Format("equalizerAon{0}Ch2", bandNumber)].Number = 1.0;
                            channel = "Ch2";

                            if (band.ChannelMode == ProQ2ChannelMode.LeftRight)
                            {
                                frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.LeftRightModeRight;
                            }
                            else
                            {
                                frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.MidSideModeSide;
                            }
                            break;
                        case ProQ2StereoPlacement.Stereo:
                            // don't change - this is the default
                            break;
                    }
                }

                frequency.Parameters[String.Format("equalizerAgain{0}{1}", bandNumber, channel)].Number = band.Gain;
                frequency.Parameters[String.Format("equalizerAfreq{0}{1}", bandNumber, channel)].Number = band.Frequency;
                frequency.Parameters[String.Format("equalizerAq{0}{1}", bandNumber, channel)].Number = band.Q;

                switch (band.Shape)
                {
                    case ProQ2Shape.BandPass:
                    case ProQ2Shape.TiltShelf:
                    case ProQ2Shape.Bell:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Peak;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.Peak;
                        }
                        break;
                    case ProQ2Shape.LowShelf:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.LowShelf;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.LowShelf;
                        }
                        break;
                    case ProQ2Shape.LowCut:
                        switch (band.Slope)
                        {
                            case ProQSlope.Slope6dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut6;
                                break;
                            case ProQSlope.Slope12dB_oct:
                            case ProQSlope.Slope18dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut12;
                                break;
                            case ProQSlope.Slope24dB_oct:
                            case ProQSlope.Slope30dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut24;
                                break;
                            case ProQSlope.Slope36dB_oct:
                            case ProQSlope.Slope48dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut48;
                                break;
                            case ProQSlope.Slope72dB_oct:
                            case ProQSlope.Slope96dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut96;
                                break;
                        }
                        break;
                    case ProQ2Shape.HighShelf:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.HighShelf;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.HighShelf;
                        }
                        break;
                    case ProQ2Shape.HighCut:
                        switch (band.Slope)
                        {
                            case ProQSlope.Slope6dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut6;
                                break;
                            case ProQSlope.Slope12dB_oct:
                            case ProQSlope.Slope18dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut12;
                                break;
                            case ProQSlope.Slope24dB_oct:
                            case ProQSlope.Slope30dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut24;
                                break;
                            case ProQSlope.Slope36dB_oct:
                            case ProQSlope.Slope48dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut48;
                                break;
                            case ProQSlope.Slope72dB_oct:
                            case ProQSlope.Slope96dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut96;
                                break;
                        }
                        break;
                    case ProQ2Shape.Notch:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Notch;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.Notch;
                        }
                        break;
                }

                Log.Debug(band.ToString());
            }
            else
            {
                // disable band
                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].Number = 0.00;
            }
        }

        public static SteinbergFrequency ToSteinbergFrequency(this FabfilterProQ3 eq)
        {
            var frequency = new SteinbergFrequency();

            // Frequency only support lowcut on the 1st band and highcut on the 8th band
            bool hasLowCutBand = eq.Bands.Any(band => band.Shape == ProQ3Shape.LowCut);
            bool hasHighCutBand = eq.Bands.Any(band => band.Shape == ProQ3Shape.HighCut);

            // get remaining bands that are not lowcut or highcut and sort by frequency
            var band2To7 = eq.Bands.Where(b => b.Enabled)
                                   .Where(b => b.Shape != ProQ3Shape.LowCut)
                                   .Where(b => b.Shape != ProQ3Shape.HighCut)
                                   .OrderBy(s => s.Frequency);

            if (hasLowCutBand)
            {
                int bandNumber = 1;
                var lowCutBand = eq.Bands.Where(band => band.Shape == ProQ3Shape.LowCut)
                                            .OrderBy(s => s.Frequency).ElementAt(0);

                SetBand(lowCutBand, bandNumber, frequency);
            }

            if (hasHighCutBand)
            {
                int bandNumber = 8;
                var highCutBand = eq.Bands.Where(band => band.Shape == ProQ3Shape.HighCut)
                                            .OrderByDescending(s => s.Frequency).ElementAt(0);

                SetBand(highCutBand, bandNumber, frequency);
            }

            // rest of the bands (2-7)
            int startIndex = hasLowCutBand ? 2 : 1;
            int endIndex = hasHighCutBand ? 7 : 8;
            int index = 0;
            for (int bandNumber = startIndex; bandNumber <= endIndex; bandNumber++, index++)
            {
                var band = band2To7.ElementAtOrDefault(index);
                SetBand(band, bandNumber, frequency);
            }

            return frequency;
        }

        private static void SetBand(ProQ3Band band, int bandNumber, SteinbergFrequency frequency)
        {
            if (band != null)
            {
                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].Number = band.Enabled ? 1.00 : 0.00;

                string channel = ""; // empty for main channel (channel 1). 'Ch2' for secondary channel 

                // due to the way fabfilter have only one stereo placement per band (frequency has two) we need to modify both channels in frequency
                // we could have in theory instead updated both channels per band in frequency
                if (band.StereoPlacement != ProQ3StereoPlacement.Stereo)
                {
                    switch (band.StereoPlacement)
                    {
                        case ProQ3StereoPlacement.Left:
                            frequency.Parameters[String.Format("equalizerAon{0}", bandNumber)].Number = 1.0;
                            frequency.Parameters[String.Format("equalizerAon{0}Ch2", bandNumber)].Number = 0.0;
                            frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.LeftRightModeLeft;
                            break;
                        case ProQ3StereoPlacement.Mid:
                            frequency.Parameters[String.Format("equalizerAon{0}", bandNumber)].Number = 1.0;
                            frequency.Parameters[String.Format("equalizerAon{0}Ch2", bandNumber)].Number = 0.0;
                            frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.MidSideModeMid;
                            break;
                        case ProQ3StereoPlacement.Right:
                            frequency.Parameters[String.Format("equalizerAon{0}", bandNumber)].Number = 0.0;
                            frequency.Parameters[String.Format("equalizerAon{0}Ch2", bandNumber)].Number = 1.0;
                            channel = "Ch2";
                            frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.LeftRightModeRight;
                            break;
                        case ProQ3StereoPlacement.Side:
                            frequency.Parameters[String.Format("equalizerAon{0}", bandNumber)].Number = 0.0;
                            frequency.Parameters[String.Format("equalizerAon{0}Ch2", bandNumber)].Number = 1.0;
                            channel = "Ch2";
                            frequency.Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number = SteinbergFrequency.ChannelMode.MidSideModeSide;
                            break;
                        case ProQ3StereoPlacement.Stereo:
                            // don't change - this is the default
                            break;
                    }
                }

                frequency.Parameters[String.Format("equalizerAgain{0}{1}", bandNumber, channel)].Number = band.Gain;
                frequency.Parameters[String.Format("equalizerAfreq{0}{1}", bandNumber, channel)].Number = band.Frequency;
                frequency.Parameters[String.Format("equalizerAq{0}{1}", bandNumber, channel)].Number = band.Q;

                switch (band.Shape)
                {
                    case ProQ3Shape.BandPass:
                    case ProQ3Shape.TiltShelf:
                    case ProQ3Shape.Bell:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Peak;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.Peak;
                        }
                        break;
                    case ProQ3Shape.LowShelf:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.LowShelf;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.LowShelf;
                        }
                        break;
                    case ProQ3Shape.LowCut:
                        switch (band.Slope)
                        {
                            case ProQ3Slope.Slope6dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut6;
                                break;
                            case ProQ3Slope.Slope12dB_oct:
                            case ProQ3Slope.Slope18dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut12;
                                break;
                            case ProQ3Slope.Slope24dB_oct:
                            case ProQ3Slope.Slope30dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut24;
                                break;
                            case ProQ3Slope.Slope36dB_oct:
                            case ProQ3Slope.Slope48dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut48;
                                break;
                            case ProQ3Slope.Slope72dB_oct:
                            case ProQ3Slope.Slope96dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut96;
                                break;
                        }
                        break;
                    case ProQ3Shape.HighShelf:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.HighShelf;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.HighShelf;
                        }
                        break;
                    case ProQ3Shape.HighCut:
                        switch (band.Slope)
                        {
                            case ProQ3Slope.Slope6dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut6;
                                break;
                            case ProQ3Slope.Slope12dB_oct:
                            case ProQ3Slope.Slope18dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut12;
                                break;
                            case ProQ3Slope.Slope24dB_oct:
                            case ProQ3Slope.Slope30dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut24;
                                break;
                            case ProQ3Slope.Slope36dB_oct:
                            case ProQ3Slope.Slope48dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut48;
                                break;
                            case ProQ3Slope.Slope72dB_oct:
                            case ProQ3Slope.Slope96dB_oct:
                                frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Cut96;
                                break;
                        }
                        break;
                    case ProQ3Shape.Notch:
                        if (bandNumber == 1 || bandNumber == 8)
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode1And8.Notch;
                        }
                        else
                        {
                            frequency.Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number = SteinbergFrequency.BandMode2To7.Notch;
                        }
                        break;
                }

                Log.Debug(band.ToString());
            }
            else
            {
                // disable band
                frequency.Parameters[String.Format("equalizerAbandon{0}", bandNumber)].Number = 0.00;
            }
        }
    }
}