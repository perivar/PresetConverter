
namespace PresetConverter
{
    /// <summary>
    /// Waves SSL To SSL Native Adapter
    /// </summary>
    public static class WavesSSLToSSLNativeAdapterExtensions
    {
        public static SSLNativeChannel ToSSLNativeChannel(this WavesSSLChannel wavesSSLChannel)
        {
            var sslNativeChannel = new SSLNativeChannel
            {
                PresetName = wavesSSLChannel.PresetName,

                HighPassFreq = wavesSSLChannel.HPFrq,
                LowPassFreq = wavesSSLChannel.LPFrq,

                CompThreshold = wavesSSLChannel.CompThreshold,
                CompRatio = wavesSSLChannel.CompRatio,
                CompRelease = wavesSSLChannel.CompRelease,
                CompFastAttack = wavesSSLChannel.CompFastAttack,
                CompMix = 100,
                CompPeak = 0,

                GateDisabledExpEnabled = !wavesSSLChannel.ExpDisabledGateEnabled,

                GateThreshold = wavesSSLChannel.ExpThreshold,
                GateRange = wavesSSLChannel.ExpRange,
                GateHold = 0.25,
                GateFastAttack = wavesSSLChannel.ExpFastAttack,
                GateRelease = wavesSSLChannel.ExpRelease,

                // Dyn To Ch Out (Dynamics to Channel Out) moves the dynamics to the output, making it post-EQ.
                // Filter Split determines whether low pass and high pass filters are placed before the dynamics processors.
                // The routing diagram is determined based on the values of FilterSplit and DynToChannelOut, and the result
                // is appended to a StringBuilder (sb) to represent the routing configuration.
                // The routing options are:
                // 1. If FilterSplit is true and DynToChannelOut is true, the order is FLTR -> EQ -> DYN.
                // 2. If FilterSplit is true and DynToChannelOut is false, the order is FLTR -> DYN -> EQ.
                // 3. If FilterSplit is false, the default order is DYN -> FLTR -> EQ.
                DynamicsPreEq = !wavesSSLChannel.DynToChannelOut,
                FiltersToInput = wavesSSLChannel.FilterSplit,

                DynamicsIn = !wavesSSLChannel.DynToByPass,
                FiltersToSidechain = false,

                EqE = true, // E or G Series characteristics
                EqIn = !wavesSSLChannel.EQToBypass,
                EqToSidechain = wavesSSLChannel.EQToDynSC,

                LowEqBell = wavesSSLChannel.LFTypeBell,
                LowEqGain = wavesSSLChannel.LFGain,
                LowEqFreq = wavesSSLChannel.LFFrq,

                LowMidEqGain = wavesSSLChannel.LMFGain,
                LowMidEqFreq = wavesSSLChannel.LMFFrq,
                LowMidEqQ = wavesSSLChannel.LMFQ,

                HighMidEqGain = wavesSSLChannel.HMFGain,
                HighMidEqFreq = wavesSSLChannel.HMFFrq,
                HighMidEqQ = wavesSSLChannel.HMFQ,

                HighEqBell = wavesSSLChannel.HFTypeBell,
                HighEqGain = wavesSSLChannel.HFGain,
                HighEqFreq = wavesSSLChannel.HFFrq,

                // master section
                Bypass = false,
                InputTrim = wavesSSLChannel.InputTrim,
                OutputTrim = wavesSSLChannel.Gain,
                Pan = 0,
                PhaseInvert = wavesSSLChannel.PhaseReverse,
                SidechainListen = false,
                UseExternalKey = false,
                Width = 100,
                HighQuality = false
            };

            return sslNativeChannel;
        }
    }
}
