using System;

namespace PresetConverter
{
    /// <summary>
    /// UAD SSL To Waves SSL Adapter
    /// </summary>
    public static class UADSSLToWavesSSLAdapterExtensions
    {
        public static WavesSSLChannel ToWavesSSLChannel(this UADSSLChannel uadSSLChannel)
        {
            var wavesSSLChannel = new WavesSSLChannel();
            wavesSSLChannel.PresetName = uadSSLChannel.PresetName;
            wavesSSLChannel.PresetGenericType = "SLCH";
            wavesSSLChannel.PresetGroup = "";
            wavesSSLChannel.PluginName = "SSLChannel";
            wavesSSLChannel.PluginSubComp = "SCHS";
            wavesSSLChannel.PluginVersion = "9.92.0.45";
            wavesSSLChannel.ActiveSetup = "SETUP_A";
            wavesSSLChannel.SetupName = "";

            wavesSSLChannel.CompThreshold = uadSSLChannel.FindClosestParameterValue("CMP Thresh", uadSSLChannel.CMPThresh);
            wavesSSLChannel.CompRatio = uadSSLChannel.FindClosestParameterValue("CMP Ratio", uadSSLChannel.CMPRatio);
            wavesSSLChannel.CompFastAttack = uadSSLChannel.CMPAttack == 1 ? true : false;
            wavesSSLChannel.CompRelease = uadSSLChannel.FindClosestParameterValue("CMP Release", uadSSLChannel.CMPRelease);
            wavesSSLChannel.ExpThreshold = uadSSLChannel.FindClosestParameterValue("EXP Thresh", uadSSLChannel.EXPThresh);
            wavesSSLChannel.ExpRange = uadSSLChannel.FindClosestParameterValue("EXP Range", uadSSLChannel.EXPRange);
            wavesSSLChannel.ExpGate = uadSSLChannel.Select == 1 ? true : false;
            wavesSSLChannel.ExpFastAttack = uadSSLChannel.EXPAttack == 1 ? true : false;
            wavesSSLChannel.ExpRelease = uadSSLChannel.FindClosestParameterValue("EXP Release", uadSSLChannel.EXPRelease);
            wavesSSLChannel.DynToByPass = uadSSLChannel.DYNIn == 0 ? true : false;
            wavesSSLChannel.DynToChannelOut = uadSSLChannel.PreDyn == 1 ? true : false;
            wavesSSLChannel.LFTypeBell = uadSSLChannel.LFBell == 1 ? true : false;
            wavesSSLChannel.LFGain = uadSSLChannel.FindClosestParameterValue("LF Gain", uadSSLChannel.LFGain);
            wavesSSLChannel.LFFrq = uadSSLChannel.FindClosestParameterValue("LF Freq", uadSSLChannel.LFFreq);
            wavesSSLChannel.LMFGain = uadSSLChannel.FindClosestParameterValue("LMF Gain", uadSSLChannel.LMFGain);
            wavesSSLChannel.LMFFrq = uadSSLChannel.FindClosestParameterValue("LMF Freq", uadSSLChannel.LMFFreq);
            wavesSSLChannel.LMFQ = uadSSLChannel.FindClosestParameterValue("LMF Q", uadSSLChannel.LMFQ);
            wavesSSLChannel.HMFGain = uadSSLChannel.FindClosestParameterValue("HMF Gain", uadSSLChannel.HMFGain);
            wavesSSLChannel.HMFFrq = uadSSLChannel.FindClosestParameterValue("HF Freq", uadSSLChannel.HFFreq);
            wavesSSLChannel.HMFQ = uadSSLChannel.FindClosestParameterValue("HMF Q", uadSSLChannel.HMFQ);
            wavesSSLChannel.HFTypeBell = uadSSLChannel.HFBell == 1 ? true : false;
            wavesSSLChannel.HFGain = uadSSLChannel.FindClosestParameterValue("HF Gain", uadSSLChannel.HFGain);
            wavesSSLChannel.HFFrq = uadSSLChannel.FindClosestParameterValue("HF Freq", uadSSLChannel.HFFreq);
            wavesSSLChannel.EQToBypass = uadSSLChannel.EQIn == 0 ? true : false;
            wavesSSLChannel.EQToDynSC = uadSSLChannel.EQDynSC == 1 ? true : false;
            wavesSSLChannel.HPFrq = uadSSLChannel.FindClosestParameterValue("HP Freq", uadSSLChannel.HPFreq);
            wavesSSLChannel.LPFrq = uadSSLChannel.FindClosestParameterValue("LP Freq", uadSSLChannel.LPFreq);
            wavesSSLChannel.FilterSplit = true;
            wavesSSLChannel.Gain = uadSSLChannel.FindClosestParameterValue("Output", uadSSLChannel.Output);
            wavesSSLChannel.Analog = true;
            wavesSSLChannel.VUShowOutput = true;
            wavesSSLChannel.PhaseReverse = uadSSLChannel.Phase == 1 ? true : false;
            wavesSSLChannel.InputTrim = uadSSLChannel.FindClosestParameterValue("Input", uadSSLChannel.Input);

            return wavesSSLChannel;
        }
    }
}
