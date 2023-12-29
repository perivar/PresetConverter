using System.Globalization;
using System.Xml.Linq;

namespace PresetConverter
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

        public enum ChannelMode
        {
            Stereo = 0, // Default
            LeftRight = 1,
            MidSide = 2
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

        public ChannelMode Mode = ChannelMode.Stereo;
        public List<Band> Bands = new List<Band>();

        public AbletonEq8(XElement xElement)
        {
            // check mode: <Mode Value="0" />
            XElement? xMode = xElement?.Element("Mode");
            Mode = (ChannelMode)int.Parse(xMode.Attribute("Value")?.Value ?? "0");

            var bands = from d in xElement.Descendants()
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
            var band = new Band
            {
                Parameter = parameter,
                Number = int.Parse(bandElement.Name.LocalName.Substring(bandElement.Name.LocalName.LastIndexOf('.') + 1)),
                IsOn = bandElement.Descendants(parameter).Descendants("IsOn").Descendants("Manual").Attributes("Value").First().Value.Equals("true"),
                Mode = (BandMode)int.Parse(bandElement.Descendants(parameter).Descendants("Mode").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture),
                Freq = float.Parse(bandElement.Descendants(parameter).Descendants("Freq").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture),
                Gain = float.Parse(bandElement.Descendants(parameter).Descendants("Gain").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture),
                Q = float.Parse(bandElement.Descendants(parameter).Descendants("Q").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture)
            };

            return band;
        }

        /// <summary>
        /// Checks if the EQ is active due to have been changed from the default values.
        /// </summary>
        /// <returns>True if any band is active (on), has a specified BandMode of LowCut48, LowCut12, HighCut12, or HighCut48, and has a non-zero Gain. False otherwise.</returns>
        public bool IsEQActive()
        {
            foreach (var band in Bands)
            {
                // Check if the band is on and has a non-zero Gain
                if (band.IsOn && band.Gain != 0.0)
                {
                    return true;
                }
                // Check if the band is on and has a specified BandMode
                else if (band.IsOn && (band.Mode == BandMode.LowCut48 || band.Mode == BandMode.LowCut12 || band.Mode == BandMode.HighCut12 || band.Mode == BandMode.HighCut48))
                {
                    return true;
                }
            }

            return false;
        }
    }
}