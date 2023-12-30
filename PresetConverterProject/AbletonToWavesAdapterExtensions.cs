
namespace PresetConverter
{
    // see http://gigi.nullneuron.net/gigilabs/the-adapter-design-pattern-for-dtos-in-c/
    public static class AbletonToWavesAdapterExtensions
    {
        public static WavesSSLComp ToWavesSSLComp(this AbletonGlueCompressor comp)
        {
            var compressor = new WavesSSLComp
            {
                PresetName = "",
                PresetGenericType = "SLCO",
                PresetGroup = "",
                PresetPluginName = "SSLComp",
                PresetPluginSubComp = "SLCS",
                PresetPluginVersion = "10.0.0.16",
                PresetActiveSetup = "SETUP_A",
                PresetSetupName = "",

                // invert threshold ?!
                Threshold = (float)-comp.Threshold
            };

            WavesSSLComp.RatioType ratio;
            switch (comp.Ratio)
            {
                case AbletonGlueCompressor.RatioType.Ratio_10_1:
                    ratio = WavesSSLComp.RatioType.Ratio_10_1;
                    break;
                default:
                case AbletonGlueCompressor.RatioType.Ratio_4_1:
                    ratio = WavesSSLComp.RatioType.Ratio_4_1;
                    break;
                case AbletonGlueCompressor.RatioType.Ratio_2_1:
                    ratio = WavesSSLComp.RatioType.Ratio_2_1;
                    break;
            }
            compressor.Ratio = ratio;

            // Attack [0 - 5, .1 ms, .3 ms, 1 ms, 3 ms, 10 ms, 30 ms)
            WavesSSLComp.AttackType attack;
            switch (comp.Attack)
            {
                case AbletonGlueCompressor.AttackType.Attack_0_01:
                    attack = WavesSSLComp.AttackType.Attack_0_1;
                    break;
                case AbletonGlueCompressor.AttackType.Attack_0_1:
                    attack = WavesSSLComp.AttackType.Attack_0_1;
                    break;
                case AbletonGlueCompressor.AttackType.Attack_0_3:
                    attack = WavesSSLComp.AttackType.Attack_0_3;
                    break;
                default:
                case AbletonGlueCompressor.AttackType.Attack_1:
                    attack = WavesSSLComp.AttackType.Attack_1;
                    break;
                case AbletonGlueCompressor.AttackType.Attack_3:
                    attack = WavesSSLComp.AttackType.Attack_3;
                    break;
                case AbletonGlueCompressor.AttackType.Attack_10:
                    attack = WavesSSLComp.AttackType.Attack_10;
                    break;
                case AbletonGlueCompressor.AttackType.Attack_30:
                    attack = WavesSSLComp.AttackType.Attack_30;
                    break;
            }
            compressor.Attack = attack;

            // Release: 0 - 4, .1 s, .3 s, .6 s, 1.2 s, Auto (-1)
            WavesSSLComp.ReleaseType release;
            switch (comp.Release)
            {
                case AbletonGlueCompressor.ReleaseType.Release_0_1:
                    release = WavesSSLComp.ReleaseType.Release_0_1;
                    break;
                case AbletonGlueCompressor.ReleaseType.Release_0_2:
                    release = WavesSSLComp.ReleaseType.Release_0_3;
                    break;
                case AbletonGlueCompressor.ReleaseType.Release_0_4:
                    release = WavesSSLComp.ReleaseType.Release_0_3;
                    break;
                default:
                case AbletonGlueCompressor.ReleaseType.Release_0_6:
                    release = WavesSSLComp.ReleaseType.Release_0_6;
                    break;
                case AbletonGlueCompressor.ReleaseType.Release_0_8:
                    release = WavesSSLComp.ReleaseType.Release_0_6;
                    break;
                case AbletonGlueCompressor.ReleaseType.Release_1_2:
                    release = WavesSSLComp.ReleaseType.Release_1_2;
                    break;
                case AbletonGlueCompressor.ReleaseType.Release_Auto:
                    release = WavesSSLComp.ReleaseType.Release_Auto;
                    break;
            }
            compressor.Release = release;

            compressor.MakeupGain = (float)comp.Makeup;
            compressor.RateS = 0;
            compressor.In = true;
            compressor.Analog = false;
            compressor.Fade = WavesSSLComp.FadeType.Off;

            return compressor;
        }
    }
}