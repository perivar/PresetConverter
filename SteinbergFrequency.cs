using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using CommonUtils;

namespace AbletonLiveConverter
{
    public class SteinbergFrequency : VstPreset
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

        public SteinbergFrequency()
        {
            Vst3ID = VstPreset.VstIDs.SteinbergFrequency;
            PlugInCategory = "Fx|EQ";
            PlugInName = "Frequency";
            InitXml();
            InitParameters();

            // set byte positions and sizes within the vstpreset (for writing)
            ListPos = 19664; // position of List chunk
            DataChunkSize = 19732 - 4; // data chunk length. i.e. total length minus 4 ('VST3')
            ParameterDataStartPos = 48; // parameter data start position
            ParameterDataSize = 19184; // byte length from parameter data start position up until xml data
            XmlStartPos = 19232; // xml start position
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
            AddParameterToDictionary(String.Format("equalizerAon{0}", bandNumber), 100 + increment, 1.00);
            AddParameterToDictionary(String.Format("equalizerAgain{0}", bandNumber), 108 + increment, 0.00);
            AddParameterToDictionary(String.Format("equalizerAfreq{0}", bandNumber), 116 + increment, 100.00 * bandNumber);
            AddParameterToDictionary(String.Format("equalizerAq{0}", bandNumber), 124 + increment, 1.00);
            AddParameterToDictionary(String.Format("equalizerAtype{0}", bandNumber), 132 + increment, bandNumber == 1 || bandNumber == 8 ? BandMode1And8.Cut48 : BandMode2To7.Peak);
            AddParameterToDictionary(String.Format("invert{0}", bandNumber), 1022 + increment, 0.00);
            AddParameterToDictionary(String.Format("equalizerAon{0}Ch2", bandNumber), 260 + increment, 1.00);
            AddParameterToDictionary(String.Format("equalizerAgain{0}Ch2", bandNumber), 268 + increment, 0.00);
            AddParameterToDictionary(String.Format("equalizerAfreq{0}Ch2", bandNumber), 276 + increment, 25.00);
            AddParameterToDictionary(String.Format("equalizerAq{0}Ch2", bandNumber), 284 + increment, 1.00);
            AddParameterToDictionary(String.Format("equalizerAtype{0}Ch2", bandNumber), 292 + increment, 6.00);
            AddParameterToDictionary(String.Format("invert{0}Ch2", bandNumber), 1030 + increment, 0.00);
            AddParameterToDictionary(String.Format("equalizerAeditchannel{0}", bandNumber), 50 + increment, 2.00);
            AddParameterToDictionary(String.Format("equalizerAbandon{0}", bandNumber), 58 + increment, 1.00);
            AddParameterToDictionary(String.Format("linearphase{0}", bandNumber), 66 + increment, 0.00);
        }

        private void InitFrequencyPostParameters()
        {
            AddParameterToDictionary("equalizerAbypass", 1, 0.00);
            AddParameterToDictionary("equalizerAoutput", 2, 0.00);
            AddParameterToDictionary("bypass", 1002, 0.00);
            AddParameterToDictionary("reset", 1003, 0.00);
            AddParameterToDictionary("autoListen", 1005, 0.00);
            AddParameterToDictionary("spectrumonoff", 1007, 1.00);
            AddParameterToDictionary("spectrum2ChMode", 1008, 0.00);
            AddParameterToDictionary("spectrumintegrate", 1010, 40.00);
            AddParameterToDictionary("spectrumPHonoff", 1011, 1.00);
            AddParameterToDictionary("spectrumslope", 1012, 0.00);
            AddParameterToDictionary("draweq", 1013, 1.00);
            AddParameterToDictionary("draweqfilled", 1014, 1.00);
            AddParameterToDictionary("spectrumbargraph", 1015, 0.00);
            AddParameterToDictionary("showPianoRoll", 1019, 1.00);
            AddParameterToDictionary("transparency", 1020, 0.30);
            AddParameterToDictionary("autoGainOutputValue", 1021, 0.00);
            AddParameterToDictionary("", 3, 0.00);
        }
    }
}