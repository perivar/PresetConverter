using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace AbletonLiveConverter
{
    public class AbletonCompressor
    {
        public float Threshold;
        public float Ratio;
        public float ExpansionRatio;
        public float Attack;
        public float Release;
        public bool AutoReleaseControlOnOff;
        public float Gain;
        public bool GainCompensation;
        public float DryWet;
        public float Model;
        public float LegacyModel;
        public float Knee;
        public float LookAhead;

        public AbletonCompressor(XElement xelement)
        {
            this.Threshold = float.Parse(xelement.Descendants("Threshold").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Ratio = float.Parse(xelement.Descendants("Ratio").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.ExpansionRatio = float.Parse(xelement.Descendants("ExpansionRatio").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Attack = float.Parse(xelement.Descendants("Attack").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Release = float.Parse(xelement.Descendants("Release").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.AutoReleaseControlOnOff = xelement.Descendants("AutoReleaseControlOnOff").Descendants("Manual").Attributes("Value").First().Value.Equals("true");
            this.Gain = float.Parse(xelement.Descendants("Gain").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.GainCompensation = xelement.Descendants("GainCompensation").Descendants("Manual").Attributes("Value").First().Value.Equals("true");
            this.DryWet = float.Parse(xelement.Descendants("DryWet").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Model = float.Parse(xelement.Descendants("Model").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.LegacyModel = float.Parse(xelement.Descendants("LegacyModel").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.Knee = float.Parse(xelement.Descendants("Knee").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            this.LookAhead = float.Parse(xelement.Descendants("LookAhead").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return string.Format("Threshold: {0:0.00}, Ratio: {1:0.00}, ExpansionRatio: {2:0.00}, Attack: {3:0.00}, Release: {4:0.00}, AutoRelease: {5}, Gain: {6:0.00}, GainCompensation: {7}, DryWet: {8:0.00}, Model: {9:0.00}, LegacyModel: {10:0.00}, Knee: {11:0.00}, LookAhead: {12:0.00}",
            Threshold, Ratio, ExpansionRatio, Attack, Release, AutoReleaseControlOnOff, Gain, GainCompensation, DryWet, Model, LegacyModel, Knee, LookAhead);
        }
    }
}