
namespace PresetConverter
{
    /// <summary>
    /// UAD SSL To Waves SSL Adapter
    /// </summary>
    public static class UADSSLToWavesSSLAdapterExtensions
    {
        public static WavesSSLChannel ToWavesSSLChannel(this UADSSLChannel uadSSLChannel)
        {
            var wavesSSLChannel = new WavesSSLChannel
            {
                PresetName = uadSSLChannel.PresetName,
                PresetGenericType = "SLCH",
                PresetGroup = null,
                PresetPluginName = "SSLChannel",
                PresetPluginSubComp = "SCHS",
                PresetPluginVersion = "9.92.0.45",
                PresetActiveSetup = "SETUP_A",
                PresetSetupName = "",

                CompThreshold = uadSSLChannel.FindClosestParameterValue("CMP Thresh", uadSSLChannel.CMPThresh),
                CompRatio = uadSSLChannel.FindClosestParameterValue("CMP Ratio", uadSSLChannel.CMPRatio),
                CompFastAttack = uadSSLChannel.CMPAttack == 1 ? true : false,
                CompRelease = uadSSLChannel.FindClosestParameterValue("CMP Release", uadSSLChannel.CMPRelease),

                ExpThreshold = uadSSLChannel.FindClosestParameterValue("EXP Thresh", uadSSLChannel.EXPThresh),
                ExpRange = uadSSLChannel.FindClosestParameterValue("EXP Range", uadSSLChannel.EXPRange),

                ExpGate = uadSSLChannel.Select >= 0.25 ? true : false,

                ExpFastAttack = uadSSLChannel.EXPAttack == 1 ? true : false,
                ExpRelease = uadSSLChannel.FindClosestParameterValue("EXP Release", uadSSLChannel.EXPRelease),

                DynToByPass = uadSSLChannel.DYNIn == 0 ? true : false,
                DynToChannelOut = uadSSLChannel.PreDyn == 1 ? true : false,

                LFTypeBell = uadSSLChannel.LFBell == 1 ? true : false,
                LFGain = uadSSLChannel.FindClosestParameterValue("LF Gain", uadSSLChannel.LFGain),
                LFFrq = uadSSLChannel.FindClosestParameterValue("LF Freq", uadSSLChannel.LFFreq),

                LMFGain = uadSSLChannel.FindClosestParameterValue("LMF Gain", uadSSLChannel.LMFGain),
                LMFFrq = uadSSLChannel.FindClosestParameterValue("LMF Freq", uadSSLChannel.LMFFreq) / 1000,
                LMFQ = uadSSLChannel.FindClosestParameterValue("LMF Q", uadSSLChannel.LMFQ),

                HMFGain = uadSSLChannel.FindClosestParameterValue("HMF Gain", uadSSLChannel.HMFGain),
                HMFFrq = uadSSLChannel.FindClosestParameterValue("HMF Freq", uadSSLChannel.HMFFreq) / 1000,
                HMFQ = uadSSLChannel.FindClosestParameterValue("HMF Q", uadSSLChannel.HMFQ),

                HFTypeBell = uadSSLChannel.HFBell == 1 ? true : false,
                HFGain = uadSSLChannel.FindClosestParameterValue("HF Gain", uadSSLChannel.HFGain),
                HFFrq = uadSSLChannel.FindClosestParameterValue("HF Freq", uadSSLChannel.HFFreq) / 1000,

                EQToBypass = uadSSLChannel.EQIn == 0 ? true : false,
                EQToDynSC = uadSSLChannel.EQDynSC == 1 ? true : false,

                HPFrq = uadSSLChannel.FindClosestParameterValue("HP Freq", uadSSLChannel.HPFreq)
            };

            if (uadSSLChannel.LPFreq == 0)
            {
                wavesSSLChannel.LPFrq = 30;
            }
            else
            {
                wavesSSLChannel.LPFrq = uadSSLChannel.FindClosestParameterValue("LP Freq", uadSSLChannel.LPFreq) / 1000;
            }

            wavesSSLChannel.FilterSplit = true;

            wavesSSLChannel.Gain = uadSSLChannel.FindClosestParameterValue("Output", uadSSLChannel.Output);
            wavesSSLChannel.Analog = false;
            wavesSSLChannel.VUShowOutput = true;
            wavesSSLChannel.PhaseReverse = uadSSLChannel.Phase == 1 ? true : false;
            wavesSSLChannel.InputTrim = uadSSLChannel.FindClosestParameterValue("Input", uadSSLChannel.Input);

            return wavesSSLChannel;
        }
    }
}
