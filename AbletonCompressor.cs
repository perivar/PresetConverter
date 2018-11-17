using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace AbletonLiveConverter
{
    public class AbletonCompressor
    {
        public AbletonCompressor(XElement xelement)
        {
            var threshold = float.Parse(xelement.Descendants("Threshold").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var ratio = float.Parse(xelement.Descendants("Ratio").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var expansionRatio = float.Parse(xelement.Descendants("ExpansionRatio").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var attack = float.Parse(xelement.Descendants("Attack").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var release = float.Parse(xelement.Descendants("Release").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var autoReleaseControlOnOff = xelement.Descendants("AutoReleaseControlOnOff").Descendants("Manual").Attributes("Value").First().Value.Equals("true");
            var gain = float.Parse(xelement.Descendants("Gain").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var gainCompensation = xelement.Descendants("GainCompensation").Descendants("Manual").Attributes("Value").First().Value.Equals("true");
            var dryWet = float.Parse(xelement.Descendants("DryWet").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);

            Console.WriteLine("Threshold: {0:0.00}, Ratio: {1:0.00}, Attack: {2:0.00}, Release: {3:0.00}, AutoRelease: {4}, Gain: {5:0.00}, GainCompensation: {6}, DryWet: {7:0.00}", threshold, ratio, attack, release, autoReleaseControlOnOff, gain, gainCompensation, dryWet);
            Console.WriteLine();
        }
    }
}