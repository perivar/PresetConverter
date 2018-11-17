using System;

namespace AbletonLiveConverter
{
    public class AbletonEq8ToSteinbergFrequencyAdapter
    {
        private AbletonEq8 eq;

        public AbletonEq8ToSteinbergFrequencyAdapter(AbletonEq8 eq)
        {
            this.eq = eq;
        }

        public SteinbergFrequency ToSteinbergFrequencyPreset()
        {
            var frequency = new SteinbergFrequency();

            foreach (var band in eq.Bands)
            {
                if (band.IsOn && band.ParameterName.Equals("ParameterA"))
                {
                    Console.WriteLine(band);
                }
            }

            return frequency;
        }
    }
}