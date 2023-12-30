using System.Globalization;
using System.Xml.Linq;

namespace PresetConverter
{
    public class AbletonEq3
    {
        public enum FilterSlope
        {
            Slope24 = 0,
            Slope48 = 1
        }

        public class Band
        {
            public double Freq;
            public double Gain;
            public bool IsOn;
            public FilterSlope Slope;

            public override string ToString()
            {
                return $"Freq: {Freq}, Gain: {Gain}, Slope: {Slope}, On: {(IsOn ? "On" : "Off")}";
            }
        }

        public List<Band> Bands = new List<Band>();

        // Amplitude ratio to dB conversion
        // For amplitude of waves like voltage, current and sound pressure level:
        // GdB = 20 * log10(A2 / A1)
        // A2 is the amplitude level.
        // A1 is the referenced amplitude level.
        // GdB is the amplitude ratio or gain in dB.
        public static double AmplitudeRatio2Decibel(double value)
        {
            return 20 * Math.Log10(value);
        }

        // dB to amplitude ratio conversion
        // A2 = A1 * 10^(GdB / 20)
        // A2 is the amplitude level.
        // A1 is the referenced amplitude level.
        public static double Decibel2AmplitudeRatio(double value)
        {
            return Math.Pow(10, value / 20);
        }

        public AbletonEq3(XElement xElement)
        {
            var bandLow = ParseBand(xElement, "FreqLo", "GainLo", "LowOn", "Slope");
            if (bandLow != null) Bands.Add(bandLow);

            var bandMid = ParseBand(xElement, "FreqMid", "GainMid", "MidOn", "Slope");
            if (bandMid != null) Bands.Add(bandMid);

            var bandHigh = ParseBand(xElement, "FreqHi", "GainHi", "HighOn", "Slope");
            if (bandHigh != null) Bands.Add(bandHigh);
        }

        private Band? ParseBand(XElement? xElement, string freqName, string gainName, string onName, string slopeName)
        {
            if (xElement == null)
                return null;

            return new Band
            {
                Freq = double.Parse(xElement.Element(freqName)?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture),
                Gain = AmplitudeRatio2Decibel(double.Parse(xElement.Element(gainName)?.Element("Manual")?.Attribute("Value")?.Value ?? "0", CultureInfo.InvariantCulture)),
                IsOn = xElement.Element(onName)?.Element("Manual")?.Attribute("Value")?.Value.Equals("true") ?? false,
                Slope = (FilterSlope)int.Parse(xElement.Element(slopeName)?.Element("Manual")?.Attribute("Value")?.Value ?? "0")
            };
        }

        /// <summary>
        /// Checks if the EQ is active due to have been changed from the default values.
        /// </summary>
        /// <returns>True if any band is active (on) and has a non-zero Gain. False otherwise.</returns>
        public bool HasBeenModified()
        {
            foreach (var band in Bands)
            {
                // Check if the band is on and has a non-zero Gain
                if (band.IsOn && band.Gain != 0.0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}