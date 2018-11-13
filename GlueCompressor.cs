using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace AbletonLiveConverter
{
    public class GlueCompressor
    {
        public GlueCompressor(XElement xelement)
        {
            var threshold = float.Parse(xelement.Descendants("Threshold").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var range = float.Parse(xelement.Descendants("Range").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var makeup = float.Parse(xelement.Descendants("Makeup").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var attack = float.Parse(xelement.Descendants("Attack").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var ratio = float.Parse(xelement.Descendants("Ratio").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var release = float.Parse(xelement.Descendants("Release").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var dryWet = float.Parse(xelement.Descendants("DryWet").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var PeakClipIn = xelement.Descendants("PeakClipIn").Descendants("Manual").Attributes("Value").First().Value.Equals("true");

            Console.WriteLine("Threshold: {0:0.00}, Range: {1:0.00}, Makeup: {2:0.00}, Attack: {3:0.00}, Ratio: {4:0.00}, Release: {5:0.00}, DryWet: {6:0.00}, PeakClipIn: {7}", threshold, range, makeup, attack, ratio, release, dryWet, PeakClipIn);
            Console.WriteLine();
        }
    }
}