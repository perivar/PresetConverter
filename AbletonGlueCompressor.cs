using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace AbletonLiveConverter
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

        public float Threshold;
        public float Range;
        public float Makeup;
        public AttackType Attack;
        public RatioType Ratio;
        public ReleaseType Release;
        public float DryWet;
        public bool PeakClipIn;

        public AbletonGlueCompressor(XElement xelement)
        {
            this.Threshold = float.Parse(xelement.Descendants("Threshold").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Range = float.Parse(xelement.Descendants("Range").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Makeup = float.Parse(xelement.Descendants("Makeup").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Attack = (AttackType)int.Parse(xelement.Descendants("Attack").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Ratio = (RatioType)int.Parse(xelement.Descendants("Ratio").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Release = (ReleaseType)int.Parse(xelement.Descendants("Release").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.DryWet = float.Parse(xelement.Descendants("DryWet").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.PeakClipIn = xelement.Descendants("PeakClipIn").Descendants("Manual").Attributes("Value").First().Value.Equals("true");
        }

        public override string ToString()
        {
            return string.Format("Threshold: {0:0.00}, Range: {1:0.00}, Makeup: {2:0.00}, Attack: {3:0.00}, Ratio: {4:0.00}, Release: {5:0.00}, DryWet: {6:0.00}, PeakClipIn: {7}", Threshold, Range, Makeup, Attack, Ratio, Release, DryWet, PeakClipIn);
        }
    }
}