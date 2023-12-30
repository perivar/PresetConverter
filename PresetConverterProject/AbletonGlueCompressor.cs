using System.Globalization;
using System.Xml.Linq;

namespace PresetConverter
{
    public class AbletonGlueCompressor
    {
        public enum AttackType // The Attack knob’s values are in milliseconds.
        {
            Attack_0_01,
            Attack_0_1,
            Attack_0_3,
            Attack_1,
            Attack_3,
            Attack_10,
            Attack_30
        }

        public enum ReleaseType // The Release knob’s values are in seconds. When Release is set to A (Auto), the release time will adjust automatically based on the incoming audio. 
        {
            Release_0_1,
            Release_0_2,
            Release_0_4,
            Release_0_6,
            Release_0_8,
            Release_1_2,
            Release_Auto
        }

        public enum RatioType
        {
            Ratio_2_1,
            Ratio_4_1,
            Ratio_10_1
        }

        public double Threshold;
        public double Range;
        public double Makeup;
        public AttackType Attack;
        public RatioType Ratio;
        public ReleaseType Release;
        public double DryWet;
        public bool PeakClipIn;

        public AbletonGlueCompressor(XElement xElement)
        {
            Threshold = double.Parse(xElement?.Element("Threshold")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Range = double.Parse(xElement?.Element("Range")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Makeup = double.Parse(xElement?.Element("Makeup")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Attack = (AttackType)int.Parse(xElement?.Element("Attack")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Ratio = (RatioType)int.Parse(xElement?.Element("Ratio")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Release = (ReleaseType)int.Parse(xElement?.Element("Release")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            DryWet = double.Parse(xElement?.Element("DryWet")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            PeakClipIn = xElement?.Element("PeakClipIn")?.Element("Manual")?.Attribute("Value")?.Value.Equals("true") ?? false;
        }

        public override string ToString()
        {
            return string.Format("Threshold: {0:0.00}, Range: {1:0.00}, Makeup: {2:0.00}, Attack: {3:0.00}, Ratio: {4:0.00}, Release: {5:0.00}, DryWet: {6:0.00}, PeakClipIn: {7}", Threshold, Range, Makeup, Attack, Ratio, Release, DryWet, PeakClipIn);
        }
    }
}