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
            Vst3ID = VstIDs.SteinbergFrequency;
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
            uint increment = (uint)bandNumber - 1;
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
    }
}