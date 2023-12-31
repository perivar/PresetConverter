using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using CommonUtils;

namespace PresetConverter
{
    /// <summary>
    /// A Steinberg Frequency Plugin .vstpreset file
    /// </summary>
    public class SteinbergFrequency : SteinbergVstPreset
    {
        // cannot use Enums with doubles, struct works
        public struct BandMode1And8
        {
            public const double Cut6 = 0.00;
            public const double Cut12 = 1.00;
            public const double Cut24 = 2.00;
            public const double Cut48 = 3.00;
            public const double Cut96 = 4.00;
            public const double LowShelf = 5.00;
            public const double Peak = 6.00;
            public const double HighShelf = 7.00;
            public const double Notch = 8.00;
        }

        public struct BandMode2To7
        {
            public const double LowShelf = 0.00;
            public const double Peak = 1.00;
            public const double HighShelf = 2.00;
            public const double Notch = 3.00;
        }

        public struct ChannelMode
        {
            public const double LeftRightModeLeft = 0.00;
            public const double LeftRightModeRight = 1.00;
            public const double StereoMode = 2.00;
            public const double MidSideModeMid = 3.00;
            public const double MidSideModeSide = 4.00;
        }

        public SteinbergFrequency()
        {
            Vst3ClassID = Vst3ClassIDs.SteinbergFrequency;
            PlugInCategory = "Fx|EQ";
            PlugInName = "Frequency";
            PlugInVendor = "Steinberg Media Technologies";

            InitStartBytes(19728);

            InitParameters();
        }

        private void InitParameters()
        {
            for (int i = 1; i <= 8; i++)
            {
                InitFrequencyBandParameters(i);
            }

            InitFrequencyPostParameters();
        }

        private void InitFrequencyBandParameters(int bandNumber)
        {
            int increment = bandNumber - 1;
            InitNumberParameter(String.Format("equalizerAon{0}", bandNumber), 100 + increment, 1.00);
            InitNumberParameter(String.Format("equalizerAgain{0}", bandNumber), 108 + increment, 0.00);
            InitNumberParameter(String.Format("equalizerAfreq{0}", bandNumber), 116 + increment, 100.00 * bandNumber);
            InitNumberParameter(String.Format("equalizerAq{0}", bandNumber), 124 + increment, 1.00);
            InitNumberParameter(String.Format("equalizerAtype{0}", bandNumber), 132 + increment, bandNumber == 1 || bandNumber == 8 ? BandMode1And8.Cut48 : BandMode2To7.Peak);
            InitNumberParameter(String.Format("invert{0}", bandNumber), 1022 + increment, 0.00);

            InitNumberParameter(String.Format("equalizerAon{0}Ch2", bandNumber), 260 + increment, 1.00);
            InitNumberParameter(String.Format("equalizerAgain{0}Ch2", bandNumber), 268 + increment, 0.00);
            InitNumberParameter(String.Format("equalizerAfreq{0}Ch2", bandNumber), 276 + increment, 25.00);
            InitNumberParameter(String.Format("equalizerAq{0}Ch2", bandNumber), 284 + increment, 1.00);
            InitNumberParameter(String.Format("equalizerAtype{0}Ch2", bandNumber), 292 + increment, bandNumber == 1 || bandNumber == 8 ? BandMode1And8.Cut48 : BandMode2To7.Peak);
            InitNumberParameter(String.Format("invert{0}Ch2", bandNumber), 1030 + increment, 0.00);

            InitNumberParameter(String.Format("equalizerAeditchannel{0}", bandNumber), 50 + increment, ChannelMode.StereoMode);
            InitNumberParameter(String.Format("equalizerAbandon{0}", bandNumber), 58 + increment, 1.00);
            InitNumberParameter(String.Format("linearphase{0}", bandNumber), 66 + increment, 0.00);
        }

        private void InitFrequencyPostParameters()
        {
            InitNumberParameter("equalizerAbypass", 1, 0.00);
            InitNumberParameter("equalizerAoutput", 2, 0.00);
            InitNumberParameter("bypass", 1002, 0.00);
            InitNumberParameter("reset", 1003, 0.00);
            InitNumberParameter("autoListen", 1005, 0.00);
            InitNumberParameter("spectrumonoff", 1007, 1.00);
            InitNumberParameter("spectrum2ChMode", 1008, 0.00);
            InitNumberParameter("spectrumintegrate", 1010, 40.00);
            InitNumberParameter("spectrumPHonoff", 1011, 1.00);
            InitNumberParameter("spectrumslope", 1012, 0.00);
            InitNumberParameter("draweq", 1013, 1.00);
            InitNumberParameter("draweqfilled", 1014, 1.00);
            InitNumberParameter("spectrumbargraph", 1015, 0.00);
            InitNumberParameter("showPianoRoll", 1019, 1.00);
            InitNumberParameter("transparency", 1020, 0.30);
            InitNumberParameter("autoGainOutputValue", 1021, 0.00);
            InitNumberParameter("", 3, 0.00);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Vst3ID: {0}\n", this.Vst3ClassID);
            sb.AppendLine("Bands:");

            for (int bandNumber = 1; bandNumber <= 8; bandNumber++)
            {
                double stereoPlacementType = Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number.Value;
                bool isBandEnabled = Parameters[String.Format("equalizerAbandon{0}", bandNumber)].Number.Value == 1.0;
                bool isLinearPhase = Parameters[String.Format("linearphase{0}", bandNumber)].Number.Value == 1.0;

                string channel = ""; // empty for main channel (channel 1). 'Ch2' for secondary channel 
                string bandInfo = GetBandInfo(bandNumber, channel);
                if (!bandInfo.Equals("")) sb.AppendFormat("[{0,-3}] {1}\n", isBandEnabled == true ? "On" : "Off", bandInfo, isLinearPhase ? ", Linear phase" : "");

                channel = "Ch2"; // empty for main channel (channel 1). 'Ch2' for secondary channel 
                bandInfo = GetBandInfo(bandNumber, channel);
                if (!bandInfo.Equals("")) sb.AppendFormat("[{0,-3}] {1}\n", isBandEnabled == true ? "On" : "Off", bandInfo, isLinearPhase ? ", Linear phase" : "");
            }

            sb.AppendLine();
            sb.AppendLine(Parameters["equalizerAbypass"].ToString());
            sb.AppendLine(Parameters["autoListen"].ToString());
            sb.AppendLine(Parameters["bypass"].ToString());
            sb.AppendLine(Parameters["reset"].ToString());
            sb.AppendLine(Parameters["spectrumonoff"].ToString());
            sb.AppendLine(Parameters["spectrum2ChMode"].ToString());
            sb.AppendLine(Parameters["spectrumintegrate"].ToString());
            sb.AppendLine(Parameters["spectrumPHonoff"].ToString());
            sb.AppendLine(Parameters["spectrumslope"].ToString());
            sb.AppendLine(Parameters["draweq"].ToString());
            sb.AppendLine(Parameters["draweqfilled"].ToString());
            sb.AppendLine(Parameters["spectrumbargraph"].ToString());
            sb.AppendLine(Parameters["showPianoRoll"].ToString());
            sb.AppendLine(Parameters["transparency"].ToString());
            sb.AppendLine(Parameters["autoGainOutputValue"].ToString());

            return sb.ToString();
        }

        /// <summary>
        /// Get Band information
        /// </summary>
        /// <param name="bandNumber">band number from 1 to 8</param>
        /// <param name="channel">empty for main channel (channel 1). 'Ch2' for secondary channel</param>
        private string GetBandInfo(int bandNumber, string channel)
        {
            var sb = new StringBuilder();

            bool isChannelOn = Parameters[String.Format("equalizerAon{0}{1}", bandNumber, channel)].Number.Value == 1.0;
            double gain = Parameters[String.Format("equalizerAgain{0}{1}", bandNumber, channel)].Number.Value;
            double frequency = Parameters[String.Format("equalizerAfreq{0}{1}", bandNumber, channel)].Number.Value;
            double q = Parameters[String.Format("equalizerAq{0}{1}", bandNumber, channel)].Number.Value;
            bool isInverted = Parameters[String.Format("invert{0}{1}", bandNumber, channel)].Number.Value == 1.0;

            string shape = "";
            if (bandNumber == 1 || bandNumber == 8)
            {
                switch (Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number.Value)
                {
                    case SteinbergFrequency.BandMode1And8.Cut6:
                        shape = "Cut6";
                        break;
                    case SteinbergFrequency.BandMode1And8.Cut12:
                        shape = "Cut12";
                        break;
                    case SteinbergFrequency.BandMode1And8.Cut24:
                        shape = "Cut24";
                        break;
                    case SteinbergFrequency.BandMode1And8.Cut48:
                        shape = "Cut48";
                        break;
                    case SteinbergFrequency.BandMode1And8.Cut96:
                        shape = "Cut96";
                        break;
                    case SteinbergFrequency.BandMode1And8.LowShelf:
                        shape = "LowShelf";
                        break;
                    case SteinbergFrequency.BandMode1And8.Peak:
                        shape = "Peak";
                        break;
                    case SteinbergFrequency.BandMode1And8.HighShelf:
                        shape = "HighShelf";
                        break;
                    case SteinbergFrequency.BandMode1And8.Notch:
                        shape = "Notch";
                        break;
                }
            }
            else
            {
                switch (Parameters[String.Format("equalizerAtype{0}{1}", bandNumber, channel)].Number.Value)
                {
                    case SteinbergFrequency.BandMode2To7.LowShelf:
                        shape = "LowShelf";
                        break;
                    case SteinbergFrequency.BandMode2To7.Peak:
                        shape = "Peak";
                        break;
                    case SteinbergFrequency.BandMode2To7.HighShelf:
                        shape = "HighShelf";
                        break;
                    case SteinbergFrequency.BandMode2To7.Notch:
                        shape = "Notch";
                        break;
                }
            }

            // stereo placement
            string stereoPlacement = "";
            switch (Parameters[String.Format("equalizerAeditchannel{0}", bandNumber)].Number.Value)
            {
                case SteinbergFrequency.ChannelMode.LeftRightModeLeft:
                    stereoPlacement = "LR: Left";
                    break;
                case SteinbergFrequency.ChannelMode.LeftRightModeRight:
                    stereoPlacement = "LR: Right";
                    break;
                case SteinbergFrequency.ChannelMode.StereoMode:
                    stereoPlacement = "Stereo";
                    break;
                case SteinbergFrequency.ChannelMode.MidSideModeMid:
                    stereoPlacement = "MS: Mid";
                    break;
                case SteinbergFrequency.ChannelMode.MidSideModeSide:
                    stereoPlacement = "MS: Side";
                    break;
            }

            if (stereoPlacement == "Stereo" && channel != "")
            {
                // ignore isChannelOn
                isChannelOn = false;
            }

            if (isChannelOn) sb.Append(String.Format("{7} {0}: {1:0.00} Hz, {2:0.00} dB, Q: {3:0.00}, {4}, {5}{6}", shape, frequency, gain, q, isChannelOn ? "Ch: On" : "Ch: Off", isInverted ? "Inverted " : "", stereoPlacement, bandNumber));

            return sb.ToString();
        }
    }
}