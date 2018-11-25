using System;
using PresetConverter;

namespace AbletonLiveConverter
{
    // see http://gigi.nullneuron.net/gigilabs/the-adapter-design-pattern-for-dtos-in-c/
    public static class AbletonToWavesAdapterExtensions
    {
        public static WavesSSLComp ToWavesSSLComp(this AbletonGlueCompressor comp)
        {
            var compressor = new WavesSSLComp();

            compressor.PresetName = "";
            compressor.PresetGenericType = "SLCO";
            compressor.PresetGroup = "";
            compressor.PluginName = "SSLComp";
            compressor.PluginSubComp = "SLCS";
            compressor.PluginVersion = "10.0.0.16";
            compressor.ActiveSetup = "SETUP_A";
            compressor.SetupName = "";

            // invert threshold ?!
            compressor.Threshold = -comp.Threshold;

            var ratio = WavesSSLComp.RatioType.Ratio_4_1;
            switch (comp.Ratio)
            {
                case 10:
                    ratio = WavesSSLComp.RatioType.Ratio_10_1;
                    break;
                case 2:
                    ratio = WavesSSLComp.RatioType.Ratio_2_1;
                    break;
                case 4:
                default:
                    ratio = WavesSSLComp.RatioType.Ratio_4_1;
                    break;
            }
            compressor.Ratio = ratio;

            // Attack [0 - 5, .1 ms, .3 ms, 1 ms, 3 ms, 10 ms, 30 ms)
            // Release: 0 - 4, .1 s, .3 s, .6 s, 1.2 s, Auto (-1)
            compressor.Attack = comp.Attack;
            compressor.Release = comp.Release;

            compressor.MakeupGain = comp.Makeup;
            compressor.RateS = 0;
            compressor.In = true;
            compressor.Analog = false;
            compressor.Fade = WavesSSLComp.FadeType.Off;

            return compressor;
        }
    }
}