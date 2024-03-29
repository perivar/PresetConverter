﻿
namespace PresetConverter
{
    /// <summary>
    /// Waves SSL To UAD SSL Adapter
    /// </summary>
    public static class WavesSSLToUADSSLAdapterExtensions
    {
        public static UADSSLChannel ToUADSSLChannel(this WavesSSLChannel wavesSSLChannel)
        {
            var uadSSLChannel = new UADSSLChannel();
            uadSSLChannel.PresetName = wavesSSLChannel.PresetName;

            uadSSLChannel.CompThresh = uadSSLChannel.FindClosestValue("CMP Thresh", wavesSSLChannel.CompThreshold);
            uadSSLChannel.CompRatio = uadSSLChannel.FindClosestValue("CMP Ratio", wavesSSLChannel.CompRatio);
            uadSSLChannel.CompAttack = Convert.ToSingle(wavesSSLChannel.CompFastAttack);
            uadSSLChannel.CompRelease = uadSSLChannel.FindClosestValue("CMP Release", wavesSSLChannel.CompRelease);

            uadSSLChannel.ExpThresh = uadSSLChannel.FindClosestValue("EXP Thresh", wavesSSLChannel.ExpThreshold);
            uadSSLChannel.ExpRange = uadSSLChannel.FindClosestValue("EXP Range", wavesSSLChannel.ExpRange);
            if (wavesSSLChannel.ExpDisabledGateEnabled)
            {
                uadSSLChannel.Select = uadSSLChannel.FindClosestValue("Select", 0.5f);
            }
            else
            {
                uadSSLChannel.Select = uadSSLChannel.FindClosestValue("Select", 0.0f);
            }
            uadSSLChannel.ExpAttack = Convert.ToSingle(wavesSSLChannel.ExpFastAttack);
            uadSSLChannel.ExpRelease = uadSSLChannel.FindClosestValue("EXP Release", wavesSSLChannel.ExpRelease);
            uadSSLChannel.ExpIn = 1.0f;

            // Dyn To Ch Out (Dynamics to Channel Out) moves the dynamics to the output, making it post-EQ.
            // Filter Split determines whether low pass and high pass filters are placed before the dynamics processors.
            // The routing diagram is determined based on the values of FilterSplit and DynToChannelOut, and the result
            // is appended to a StringBuilder (sb) to represent the routing configuration.
            // The routing options are:
            // 1. If FilterSplit is true and DynToChannelOut is true, the order is FLTR -> EQ -> DYN.
            // 2. If FilterSplit is true and DynToChannelOut is false, the order is FLTR -> DYN -> EQ.
            // 3. If FilterSplit is false, the default order is DYN -> FLTR -> EQ. 
            //wavesSSLChannel.FilterSplit;
            uadSSLChannel.CompIn = Convert.ToSingle(!wavesSSLChannel.DynToByPass);
            uadSSLChannel.DynIn = Convert.ToSingle(!wavesSSLChannel.DynToByPass);
            uadSSLChannel.PreDyn = Convert.ToSingle(wavesSSLChannel.DynToChannelOut);

            uadSSLChannel.LFBell = Convert.ToSingle(wavesSSLChannel.LFTypeBell);
            uadSSLChannel.LFGain = uadSSLChannel.FindClosestValue("LF Gain", wavesSSLChannel.LFGain);
            uadSSLChannel.LFFreq = uadSSLChannel.FindClosestValue("LF Freq", wavesSSLChannel.LFFrq);

            uadSSLChannel.LMFGain = uadSSLChannel.FindClosestValue("LMF Gain", wavesSSLChannel.LMFGain);
            uadSSLChannel.LMFFreq = uadSSLChannel.FindClosestValue("LMF Freq", wavesSSLChannel.LMFFrq * 1000);
            uadSSLChannel.LMFQ = uadSSLChannel.FindClosestValue("LMF Q", wavesSSLChannel.LMFQ);

            uadSSLChannel.HMFGain = uadSSLChannel.FindClosestValue("HMF Gain", wavesSSLChannel.HMFGain);
            uadSSLChannel.HMFFreq = uadSSLChannel.FindClosestValue("HMF Freq", wavesSSLChannel.HMFFrq * 1000);
            uadSSLChannel.HMFQ = uadSSLChannel.FindClosestValue("HMF Q", wavesSSLChannel.HMFQ);

            uadSSLChannel.HFBell = Convert.ToSingle(wavesSSLChannel.HFTypeBell);
            uadSSLChannel.HFGain = uadSSLChannel.FindClosestValue("HF Gain", wavesSSLChannel.HFGain);
            uadSSLChannel.HFFreq = uadSSLChannel.FindClosestValue("HF Freq", wavesSSLChannel.HFFrq * 1000);

            uadSSLChannel.EQIn = Convert.ToSingle(!wavesSSLChannel.EQToBypass);
            uadSSLChannel.EQDynSC = Convert.ToSingle(wavesSSLChannel.EQToDynSC);

            uadSSLChannel.HPFreq = uadSSLChannel.FindClosestValue("HP Freq", wavesSSLChannel.HPFrq);
            if (wavesSSLChannel.LPFrq == 30)
            {
                uadSSLChannel.LPFreq = 0;
            }
            else
            {
                uadSSLChannel.LPFreq = uadSSLChannel.FindClosestValue("LP Freq", wavesSSLChannel.LPFrq * 1000);
            }

            uadSSLChannel.Output = uadSSLChannel.FindClosestValue("Output", wavesSSLChannel.Gain);
            //wavesSSLChannel.Analog;
            //wavesSSLChannel.VUShowOutput;
            uadSSLChannel.Phase = Convert.ToSingle(wavesSSLChannel.PhaseReverse);
            uadSSLChannel.Input = uadSSLChannel.FindClosestValue("Input", wavesSSLChannel.InputTrim);

            uadSSLChannel.EQType = 0.0f; // Black EQ Type
            uadSSLChannel.StereoLink = 1.0f;
            uadSSLChannel.Power = 1.0f;

            return uadSSLChannel;
        }
    }
}
