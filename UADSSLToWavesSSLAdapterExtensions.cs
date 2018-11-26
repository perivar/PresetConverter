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

            wavesSSLChannel.CompThreshold = uadSSLChannel.CMPThresh;
            wavesSSLChannel.CompRatio = uadSSLChannel.CMPRatio;
            wavesSSLChannel.CompFastAttack = uadSSLChannel.CMPAttack == 1 ? true : false;
            wavesSSLChannel.CompRelease = uadSSLChannel.CMPRelease;
            wavesSSLChannel.ExpThreshold = uadSSLChannel.EXPThresh;
            wavesSSLChannel.ExpRange = uadSSLChannel.EXPRange;
            wavesSSLChannel.ExpGate = uadSSLChannel.Select == 1 ? true : false;
            wavesSSLChannel.ExpFastAttack = uadSSLChannel.EXPAttack == 1 ? true : false;
            wavesSSLChannel.ExpRelease = uadSSLChannel.EXPRelease;
            wavesSSLChannel.DynToByPass = uadSSLChannel.DYNIn == 0 ? true : false;
            wavesSSLChannel.DynToChannelOut = uadSSLChannel.PreDyn == 1 ? true : false;
            wavesSSLChannel.LFTypeBell = uadSSLChannel.LFBell == 1 ? true : false;
            wavesSSLChannel.LFGain = uadSSLChannel.LFGain;
            wavesSSLChannel.LFFrq = uadSSLChannel.LFFreq;
            wavesSSLChannel.LMFGain = uadSSLChannel.LMFGain;
            wavesSSLChannel.LMFFrq = uadSSLChannel.LMFFreq;
            wavesSSLChannel.LMFQ = uadSSLChannel.LMFQ;
            wavesSSLChannel.HMFGain = uadSSLChannel.HMFGain;
            wavesSSLChannel.HMFFrq = uadSSLChannel.HFFreq;
            wavesSSLChannel.HMFQ = uadSSLChannel.HMFQ;
            wavesSSLChannel.HFTypeBell = uadSSLChannel.HFBell == 1 ? true : false;
            wavesSSLChannel.HFGain = uadSSLChannel.HFGain;
            wavesSSLChannel.HFFrq = uadSSLChannel.HFFreq;
            wavesSSLChannel.EQToBypass = uadSSLChannel.EQIn == 0 ? true : false;
            wavesSSLChannel.EQToDynSC = uadSSLChannel.EQDynSC == 1 ? true : false;
            wavesSSLChannel.HPFrq = uadSSLChannel.HPFreq;
            wavesSSLChannel.LPFrq = uadSSLChannel.LPFreq;
            wavesSSLChannel.FilterSplit = true;
            wavesSSLChannel.Gain = uadSSLChannel.Output;
            wavesSSLChannel.Analog = true;
            wavesSSLChannel.VUShowOutput = true;
            wavesSSLChannel.PhaseReverse = uadSSLChannel.Phase == 1 ? true : false;
            wavesSSLChannel.InputTrim = uadSSLChannel.Input;

            return wavesSSLChannel;
        }
    }
}
