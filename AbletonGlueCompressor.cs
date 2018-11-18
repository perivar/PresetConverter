using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace AbletonLiveConverter
{
    public class AbletonGlueCompressor
    {
        public float Threshold;
        public float Range;
        public float Makeup;
        public float Attack;
        public float Ratio;
        public float Release;
        public float DryWet;
        public bool PeakClipIn;

        public AbletonGlueCompressor(XElement xelement)
        {
            this.Threshold = float.Parse(xelement.Descendants("Threshold").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Range = float.Parse(xelement.Descendants("Range").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Makeup = float.Parse(xelement.Descendants("Makeup").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Attack = float.Parse(xelement.Descendants("Attack").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Ratio = float.Parse(xelement.Descendants("Ratio").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Release = float.Parse(xelement.Descendants("Release").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.DryWet = float.Parse(xelement.Descendants("DryWet").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.PeakClipIn = xelement.Descendants("PeakClipIn").Descendants("Manual").Attributes("Value").First().Value.Equals("true");
        }

        public override string ToString()
        {
            return string.Format("Threshold: {0:0.00}, Range: {1:0.00}, Makeup: {2:0.00}, Attack: {3:0.00}, Ratio: {4:0.00}, Release: {5:0.00}, DryWet: {6:0.00}, PeakClipIn: {7}", Threshold, Range, Makeup, Attack, Ratio, Release, DryWet, PeakClipIn);
        }
    }
}