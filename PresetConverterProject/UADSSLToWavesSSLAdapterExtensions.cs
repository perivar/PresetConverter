
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

                CompThreshold = uadSSLChannel.FindClosestParameterValue("CMP Thresh", uadSSLChannel.CompThresh),
                CompRatio = uadSSLChannel.FindClosestParameterValue("CMP Ratio", uadSSLChannel.CompRatio),
                CompFastAttack = uadSSLChannel.CompAttack == 1,
                CompRelease = uadSSLChannel.FindClosestParameterValue("CMP Release", uadSSLChannel.CompRelease),

                ExpThreshold = uadSSLChannel.FindClosestParameterValue("EXP Thresh", uadSSLChannel.ExpThresh),
                ExpRange = uadSSLChannel.FindClosestParameterValue("EXP Range", uadSSLChannel.ExpRange),

                // A value above 0.25 it's Gate 1, for value above 2,00 it's Gate 2
                ExpDisabledGateEnabled = uadSSLChannel.Select >= 0.25,

                // Auto = 0, Fast = 1
                ExpFastAttack = uadSSLChannel.ExpAttack == 1,
                ExpRelease = uadSSLChannel.FindClosestParameterValue("EXP Release", uadSSLChannel.ExpRelease),

                // Dyn To Ch Out (Dynamics to Channel Out) moves the dynamics to the output, making it post-EQ.
                // Filter Split determines whether low pass and high pass filters are placed before the dynamics processors.
                // The routing diagram is determined based on the values of FilterSplit and DynToChannelOut, and the result
                // is appended to a StringBuilder (sb) to represent the routing configuration.
                // The routing options are:
                // 1. If FilterSplit is true and DynToChannelOut is true, the order is FLTR -> EQ -> DYN.
                // 2. If FilterSplit is true and DynToChannelOut is false, the order is FLTR -> DYN -> EQ.
                // 3. If FilterSplit is false, the default order is DYN -> FLTR -> EQ.
                DynToChannelOut = uadSSLChannel.PreDyn == 1,
                FilterSplit = false,
                DynToByPass = uadSSLChannel.DynIn == 0,

                LFTypeBell = uadSSLChannel.LFBell == 1,
                LFGain = uadSSLChannel.FindClosestParameterValue("LF Gain", uadSSLChannel.LFGain),
                LFFrq = uadSSLChannel.FindClosestParameterValue("LF Freq", uadSSLChannel.LFFreq),

                LMFGain = uadSSLChannel.FindClosestParameterValue("LMF Gain", uadSSLChannel.LMFGain),
                LMFFrq = uadSSLChannel.FindClosestParameterValue("LMF Freq", uadSSLChannel.LMFFreq) / 1000,
                LMFQ = uadSSLChannel.FindClosestParameterValue("LMF Q", uadSSLChannel.LMFQ),

                HMFGain = uadSSLChannel.FindClosestParameterValue("HMF Gain", uadSSLChannel.HMFGain),
                HMFFrq = uadSSLChannel.FindClosestParameterValue("HMF Freq", uadSSLChannel.HMFFreq) / 1000,
                HMFQ = uadSSLChannel.FindClosestParameterValue("HMF Q", uadSSLChannel.HMFQ),

                HFTypeBell = uadSSLChannel.HFBell == 1,
                HFGain = uadSSLChannel.FindClosestParameterValue("HF Gain", uadSSLChannel.HFGain),
                HFFrq = uadSSLChannel.FindClosestParameterValue("HF Freq", uadSSLChannel.HFFreq) / 1000,

                EQToBypass = uadSSLChannel.EQIn == 0,
                EQToDynSC = uadSSLChannel.EQDynSC == 1,

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

            wavesSSLChannel.Gain = uadSSLChannel.FindClosestParameterValue("Output", uadSSLChannel.Output);
            wavesSSLChannel.Analog = false;
            wavesSSLChannel.VUShowOutput = true;
            wavesSSLChannel.PhaseReverse = uadSSLChannel.Phase == 1;
            wavesSSLChannel.InputTrim = uadSSLChannel.FindClosestParameterValue("Input", uadSSLChannel.Input);

            return wavesSSLChannel;
        }
    }
}
