using System.Globalization;
using System.Xml.Linq;

namespace PresetConverter
{
    public class AbletonLimiter
    {
        public enum LookaheadMS
        {
            Lookahead1_5ms = 0,
            Lookahead3ms = 1,
            Lookahead6ms = 2
        }

        public double Gain;
        public double Ceiling;
        public double Release;
        public bool AutoRelease;
        public bool LinkChannels;
        public LookaheadMS Lookahead;

        public AbletonLimiter(XElement xElement)
        {
            Gain = double.Parse(xElement?.Element("Gain")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Ceiling = double.Parse(xElement?.Element("Ceiling")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            Release = double.Parse(xElement?.Element("Release")?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture);
            AutoRelease = xElement?.Element("AutoRelease")?.Element("Manual")?.Attribute("Value")?.Value.Equals("true") ?? false;
            LinkChannels = xElement?.Element("LinkChannels")?.Element("Manual")?.Attribute("Value")?.Value.Equals("true") ?? false;
            Lookahead = (LookaheadMS)int.Parse(xElement?.Element("Lookahead")?.Element("Manual")?.Attribute("Value")?.Value ?? "0");
        }

        public bool HasBeenModified()
        {
            double ceilingTolerance = 0.0001; // Adjust the tolerance as needed
            return Gain != 0 || Math.Abs(Ceiling + 0.3) > ceilingTolerance || Release != 300 || !AutoRelease || !LinkChannels || Lookahead != LookaheadMS.Lookahead3ms;
        }

        public override string ToString()
        {
            return string.Format("Gain: {0:0.00} dB, Ceiling: {1:0.00} dB, Release: {2:0.00} ms, AutoRelease: {3}, LinkChannels: {4}, Lookahead: {5}",
                Gain, Ceiling, Release, AutoRelease, LinkChannels, Lookahead);
        }
    }
}
