using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace AbletonLiveConverter
{
    public class Eq
    {
        public enum BandMode
        {
            LowCut4,
            LowCut,
            HighShelf,
            Peak,
            Notch,
            LowShelf,
            HighCut,
            HighCut4
        }

        public Eq(XElement xelement)
        {
            var bands = from d in xelement.Descendants()
                        where d.Name.LocalName.Contains("Bands")
                        select d;

            foreach (var band in bands)
            {
                ParseBand(band, "ParameterA");
                // ParseBand(band, "ParameterB");
            }
            Console.WriteLine();
        }

        private void ParseBand(XElement band, string parameter)
        {
            var isOn = band.Descendants(parameter).Descendants("IsOn").Descendants("Manual").Attributes("Value").First().Value.Equals("true");
            var mode = (BandMode) int.Parse(band.Descendants(parameter).Descendants("Mode").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var freq = float.Parse(band.Descendants(parameter).Descendants("Freq").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var gain = float.Parse(band.Descendants(parameter).Descendants("Gain").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
            var q = float.Parse(band.Descendants(parameter).Descendants("Q").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);

            if (isOn)
            {
                Console.WriteLine(parameter + ": {0:0.00} Hz, Gain: {1:0.00} dB, Q: {2:0.00}, Mode: {3}", freq, gain, q, mode);
            }
        }
    }
}