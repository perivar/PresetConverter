
namespace PresetConverter
{
    /// <summary>
    /// Waves SSL To SSL Native Adapter
    /// </summary>
    public static class WavesSSLToSSLNativeAdapterExtensions
    {
        public static SSLNativeChannel ToSSLNativeChannel(this WavesSSLChannel wavesSSLChannel)
        {
            var sslNativeChannel = new SSLNativeChannel();
            sslNativeChannel.PresetName = wavesSSLChannel.PresetName;

            sslNativeChannel.HighPassFreq = wavesSSLChannel.HPFrq;
            sslNativeChannel.LowPassFreq = wavesSSLChannel.LPFrq;

            sslNativeChannel.CompThreshold = wavesSSLChannel.CompThreshold;
            sslNativeChannel.CompRatio = wavesSSLChannel.CompRatio;
            sslNativeChannel.CompRelease = wavesSSLChannel.CompRelease;
            sslNativeChannel.CompFastAttack = wavesSSLChannel.CompFastAttack ? 1 : 0;
            sslNativeChannel.CompMix = 100;
            sslNativeChannel.CompPeak = 0;

            if (wavesSSLChannel.ExpGate)
            {
                // if waves gate is set, then make sure the expander is off
                sslNativeChannel.GateExpander = 0;
            }
            else
            {
                sslNativeChannel.GateExpander = 1;
            }
            sslNativeChannel.GateThreshold = wavesSSLChannel.ExpThreshold;
            sslNativeChannel.GateRange = wavesSSLChannel.ExpRange;
            sslNativeChannel.GateHold = 0.25;
            sslNativeChannel.GateFastAttack = wavesSSLChannel.ExpFastAttack ? 1 : 0;
            sslNativeChannel.GateRelease = wavesSSLChannel.ExpRelease;

            sslNativeChannel.DynamicsIn = !wavesSSLChannel.DynToByPass ? 1 : 0;
            sslNativeChannel.DynamicsPreEq = wavesSSLChannel.DynToChannelOut ? 0 : 1;
            sslNativeChannel.FiltersToInput = wavesSSLChannel.FilterSplit ? 1 : 0;
            sslNativeChannel.FiltersToSidechain = 0;

            sslNativeChannel.EqE = 0.0;
            sslNativeChannel.EqIn = Convert.ToSingle(!wavesSSLChannel.EQToBypass);
            sslNativeChannel.EqToSidechain = Convert.ToSingle(wavesSSLChannel.EQToDynSC);

            sslNativeChannel.LowEqBell = Convert.ToSingle(wavesSSLChannel.LFTypeBell);
            sslNativeChannel.LowEqGain = wavesSSLChannel.LFGain;
            sslNativeChannel.LowEqFreq = wavesSSLChannel.LFFrq;

            sslNativeChannel.LowMidEqGain = wavesSSLChannel.LMFGain;
            sslNativeChannel.LowMidEqFreq = wavesSSLChannel.LMFFrq;
            sslNativeChannel.LowMidEqQ = wavesSSLChannel.LMFQ;

            sslNativeChannel.HighMidEqGain = wavesSSLChannel.HMFGain;
            sslNativeChannel.HighMidEqFreq = wavesSSLChannel.HMFFrq;
            sslNativeChannel.HighMidEqQ = wavesSSLChannel.HMFQ;

            sslNativeChannel.HighEqBell = Convert.ToSingle(wavesSSLChannel.HFTypeBell);
            sslNativeChannel.HighEqGain = wavesSSLChannel.HFGain;
            sslNativeChannel.HighEqFreq = wavesSSLChannel.HFFrq;

            // master section
            sslNativeChannel.Bypass = 0.0;
            sslNativeChannel.InputTrim = wavesSSLChannel.InputTrim;
            sslNativeChannel.OutputTrim = wavesSSLChannel.Gain;
            sslNativeChannel.Pan = 0;
            sslNativeChannel.PhaseInvert = Convert.ToSingle(wavesSSLChannel.PhaseReverse);
            sslNativeChannel.SidechainListen = 0;
            sslNativeChannel.UseExternalKey = 0;
            sslNativeChannel.Width = 100;
            sslNativeChannel.HighQuality = 0;

            return sslNativeChannel;
        }
    }
}
