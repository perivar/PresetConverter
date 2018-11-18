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
                if (band.Parameter.Equals("ParameterA"))
                {
                    int bandNumber = band.Number + 1; // zero indexed
                    frequency.Parameters[String.Format("equalizerAon{0}", bandNumber)].Value = band.IsOn ? 1.00 : 0.00;
                    frequency.Parameters[String.Format("equalizerAgain{0}", bandNumber)].Value = band.Gain;
                    frequency.Parameters[String.Format("equalizerAfreq{0}", bandNumber)].Value = band.Freq;
                    frequency.Parameters[String.Format("equalizerAq{0}", bandNumber)].Value = band.Q;
                    frequency.Parameters[String.Format("equalizerAtype{0}", bandNumber)].Value = 0.0;
                    Console.WriteLine(band);
                }
            }

            return frequency;
        }
    }
}