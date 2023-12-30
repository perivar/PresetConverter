using System.Globalization;
using System.Xml.Linq;

namespace PresetConverter
{
    public class AbletonAutoPan
    {
        public enum LfoWaveformType
        {
            Sine = 0,
            Triangle = 1,
            SawtoothDown = 2,
            Random = 3
        }

        public enum LFORateType
        {
            Hertz = 0,
            TempoSync = 1
        }

        public enum LFOStereoMode
        {
            Phase = 0,
            Spin = 1
        }

        public enum LFOBeatRate
        {
            Rate_1_64 = 0,
            Rate_1_48 = 1,
            Rate_1_32 = 2,
            Rate_1_24 = 3,
            Rate_1_16 = 4,
            Rate_1_12 = 5,
            Rate_1_8 = 6,
            Rate_1_6 = 7,
            Rate_3_16 = 8,
            Rate_1_4 = 9,
            Rate_5_16 = 10,
            Rate_1_3 = 11,
            Rate_3_8 = 12,
            Rate_1_2 = 13,
            Rate_3_4 = 14,
            Rate_1 = 15,
            Rate_1p5 = 16,
            Rate_2 = 17,
            Rate_3 = 18,
            Rate_4 = 19,
            Rate_6 = 20,
            Rate_8 = 21,
        }

        public LfoWaveformType Type;
        public double Frequency; // Hertz, Not used if TempoSync is TempoSync
        public LFORateType RateType;
        public LFOBeatRate BeatRate;
        public LFOStereoMode StereoMode;
        public double Spin; // Used if StereoMode is Spin

        // https://www.iconcollective.edu/ableton-live-auto-pan-tips
        // For starters, if we turn the Phase control all the way down to 0 ° (or, conversely, all the way up to 360 °), 
        // you’ll see that the two LFO phases align with each other. 
        // In this scenario, the single is LFO modulating the volume of both left and right channels simultaneously. 
        // In other words, we’re dealing with a full-on volume LFO. 
        public double Phase; // Used if StereoMode is Phase

        public double Offset;
        public bool IsOn;
        public bool Quantize;
        public double BeatQuantize;
        public double NoiseWidth;
        public double LfoAmount; // 0,00 - 1,00 = 0 - 100% 
        public bool LfoInvert;
        public double LfoShape; // 0,00 - 1,00 = 0 - 100% 

        public AbletonAutoPan(XElement xElement)
        {
            Type = (LfoWaveformType)int.Parse(xElement?.Element("Lfo")?.Element("Type")?.Element("Manual")?.Attribute("Value")?.Value ?? "0");
            Frequency = double.Parse(xElement?.Element("Lfo")?.Element("Frequency")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            RateType = (LFORateType)int.Parse(xElement?.Element("Lfo")?.Element("RateType")?.Element("Manual")?.Attribute("Value")?.Value ?? "0");
            BeatRate = (LFOBeatRate)int.Parse(xElement?.Element("Lfo")?.Element("BeatRate")?.Element("Manual")?.Attribute("Value")?.Value ?? "0");
            StereoMode = (LFOStereoMode)int.Parse(xElement?.Element("Lfo")?.Element("StereoMode")?.Element("Manual")?.Attribute("Value")?.Value ?? "0");
            Spin = double.Parse(xElement?.Element("Lfo")?.Element("Spin")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Phase = double.Parse(xElement?.Element("Lfo")?.Element("Phase")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Offset = double.Parse(xElement?.Element("Lfo")?.Element("Offset")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            IsOn = bool.Parse(xElement?.Element("Lfo")?.Element("IsOn")?.Element("Manual")?.Attribute("Value")?.Value ?? "false");
            Quantize = bool.Parse(xElement?.Element("Lfo")?.Element("Quantize")?.Element("Manual")?.Attribute("Value")?.Value ?? "false");
            BeatQuantize = double.Parse(xElement?.Element("Lfo")?.Element("BeatQuantize")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            NoiseWidth = double.Parse(xElement?.Element("Lfo")?.Element("NoiseWidth")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            LfoAmount = double.Parse(xElement?.Element("Lfo")?.Element("LfoAmount")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            LfoInvert = bool.Parse(xElement?.Element("Lfo")?.Element("LfoInvert")?.Element("Manual")?.Attribute("Value")?.Value ?? "false");
            LfoShape = double.Parse(xElement?.Element("Lfo")?.Element("LfoShape")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
        }

        public bool HasBeenModified()
        {
            double ceilingTolerance = 0.0001; // Adjust the tolerance as needed
            return Type != LfoWaveformType.Sine || Math.Abs(Frequency - 1.0) > ceilingTolerance || RateType != LFORateType.Hertz || BeatRate != LFOBeatRate.Rate_1_16 || StereoMode != LFOStereoMode.Phase || Spin != 0.0 || Phase != 180 || Offset != 0 || !IsOn || Quantize || BeatQuantize != 2.0 || NoiseWidth != 0.5 || LfoAmount != 0.0 || LfoInvert || LfoShape != 0.0;
        }

        public override string ToString()
        {
            return string.Format("Type: {0}\nFrequency: {1:0.00} Hz \nRateType: {2}\nBeatRate: {3}\nStereoMode: {4}\nSpin: {5:0.00} %\nPhase: {6:0.00} °\nOffset: {7:0.00} °\nIsOn: {8}\nQuantize: {9}\nBeatQuantize: {10:0.00}\nNoiseWidth: {11:0.00} %\nLfoAmount: {12:0.00} %\nLfoInvert: {13}\nLfoShape: {14:0.00} %",
                    Type, Frequency, RateType, BeatRate, StereoMode, Spin, Phase, Offset, IsOn, Quantize, BeatQuantize, NoiseWidth, LfoAmount, LfoInvert, LfoShape);
        }
    }
}
