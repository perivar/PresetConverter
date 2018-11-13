using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace AbletonLiveConverter
{
    public class Eq
    {
        public Eq(XElement xelement)
        {
            var bands = from d in xelement.Descendants()
                        where d.Name.LocalName.Contains("Bands")
                        select d;

            foreach (var band in bands)
            {
                var isOnA = band.Descendants("ParameterA").Descendants("IsOn").Descendants("Manual").Attributes("Value").First().Value.Equals("true");
                var freqA = float.Parse(band.Descendants("ParameterA").Descendants("Freq").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
                var gainA = float.Parse(band.Descendants("ParameterA").Descendants("Gain").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
                var qA = float.Parse(band.Descendants("ParameterA").Descendants("Q").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);

                if (isOnA && gainA != 0)
                {
                    Console.WriteLine("ParameterA: {0:0.00} Hz, Gain: {1:0.00}, Q: {2:0.00}, On: {3}", freqA, gainA, qA, isOnA);
                }
                else
                {
                    // Console.WriteLine("OFF ParameterA: {0:0.00} Hz, Gain: {1:0.00}, Q: {2:0.00}, On: {3}", freqA, gainA, qA, isOnA);
                }

                var isOnB = band.Descendants("ParameterB").Descendants("IsOn").Descendants("Manual").Attributes("Value").First().Value.Equals("true");
                var freqB = float.Parse(band.Descendants("ParameterB").Descendants("Freq").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
                var gainB = float.Parse(band.Descendants("ParameterB").Descendants("Gain").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);
                var qB = float.Parse(band.Descendants("ParameterB").Descendants("Q").Descendants("Manual").Attributes("Value").First().Value, CultureInfo.InvariantCulture);

                if (isOnB && gainB != 0)
                {
                    Console.WriteLine("ParameterB: {0:0.00} Hz, Gain: {1:0.00}, Q: {2:0.00}, On: {3}", freqB, gainB, qB, isOnB);
                }
                else
                {
                    // Console.WriteLine("OFF ParameterB: {0:0.00} Hz, Gain: {1:0.00}, Q: {2:0.00}, On: {3}", freqB, gainB, qB, isOnB);
                }
            }
            Console.WriteLine();
        }
    }
}