using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace AbletonLiveConverter
{
    public class AbletonEq8
    {
        public enum BandMode
        {
            LowCut48,
            LowCut12,
            LeftShelf,
            Bell,
            Notch,
            RightShelf,
            HighCut12,
            HighCut48
        }

        public class Band
        {
            public string Parameter;
            public int Number;
            public bool IsOn;
            public BandMode Mode;
            public float Freq;
            public float Gain;
            public float Q;

            public override string ToString()
            {
                return string.Format("{0}: Band: {1}, {2:0.00} Hz, Gain: {3:0.00} dB, Q: {4:0.00}, Mode: {5}, {6}", Parameter, Number, Freq, Gain, Q, Mode, IsOn ? "On" : "Off");
            }
        }

        public List<Band> Bands = new List<Band>();

        public AbletonEq8(XElement xelement)
        {
            var bands = from d in xelement.Descendants()
                        where d.Name.LocalName.Contains("Bands")
                        select d;

            foreach (var band in bands)
            {
                Bands.Add(ParseBand(band, "ParameterA"));
                Bands.Add(ParseBand(band, "ParameterB"));
            }
        }

        private Band ParseBand(XElement bandElement, string parameter)
        {
            var band = new Band();
            band.Parameter = parameter;
            band.Number = int.Parse(bandElement.Name.LocalName.Substring(bandElement.Name.LocalName.LastIndexOf('.') + 1));
            band.IsOn = bandElement.Descendants(parameter).Descendants("IsOn").Descendants("Manual").Attributes("Value").First().Value.Equals("true");
            band.Mode = (BandMode)int.Parse(bandElement.Descendants(parameter).Descendants("Mode").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            band.Freq = float.Parse(bandElement.Descendants(parameter).Descendants("Freq").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            band.Gain = float.Parse(bandElement.Descendants(parameter).Descendants("Gain").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            band.Q = float.Parse(bandElement.Descendants(parameter).Descendants("Q").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            return band;
        }
    }
}