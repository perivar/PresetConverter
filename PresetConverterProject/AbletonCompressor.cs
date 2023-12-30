using System.Globalization;
using System.Xml.Linq;
using Serilog;

namespace PresetConverter
{
    public class AbletonCompressor
    {
        public const double MaxFloatMinusEpsilon = 340282326356119260000000000000000000000f;

        public double Threshold;
        public float Ratio;
        public double ExpansionRatio;
        public double Attack;
        public double Release;
        public bool AutoReleaseControlOnOff;
        public double Gain;
        public bool GainCompensation;
        public double DryWet;
        public double Model;
        public double LegacyModel;
        public double Knee;
        public double LookAhead;

        public AbletonCompressor(XElement xElement)
        {
            Threshold = double.Parse(xElement?.Element("Threshold")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Ratio = float.Parse(xElement?.Element("Ratio")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            if (Ratio == MaxFloatMinusEpsilon)
            {
                Log.Debug("AbletonCompressor ratio is set to max: {0}", MaxFloatMinusEpsilon);
            }
            ExpansionRatio = double.Parse(xElement?.Element("ExpansionRatio")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Attack = double.Parse(xElement?.Element("Attack")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Release = double.Parse(xElement?.Element("Release")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            AutoReleaseControlOnOff = xElement?.Element("AutoReleaseControlOnOff")?.Element("Manual")?.Attribute("Value")?.Value.Equals("true") ?? false;
            Gain = double.Parse(xElement?.Element("Gain")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            GainCompensation = xElement?.Element("GainCompensation")?.Element("Manual")?.Attribute("Value")?.Value.Equals("true") ?? false;
            DryWet = double.Parse(xElement?.Element("DryWet")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Model = double.Parse(xElement?.Element("Model")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            LegacyModel = double.Parse(xElement?.Element("LegacyModel")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Knee = double.Parse(xElement?.Element("Knee")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            LookAhead = double.Parse(xElement?.Element("LookAhead")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return string.Format("Threshold: {0:0.00}, Ratio: {1:0.00}, ExpansionRatio: {2:0.00}, Attack: {3:0.00}, Release: {4:0.00}, AutoRelease: {5}, Gain: {6:0.00}, GainCompensation: {7}, DryWet: {8:0.00}, Model: {9:0.00}, LegacyModel: {10:0.00}, Knee: {11:0.00}, LookAhead: {12:0.00}",
            Threshold, Ratio, ExpansionRatio, Attack, Release, AutoReleaseControlOnOff, Gain, GainCompensation, DryWet, Model, LegacyModel, Knee, LookAhead);
        }
    }
}