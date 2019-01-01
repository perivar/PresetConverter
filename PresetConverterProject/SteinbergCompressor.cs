using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using CommonUtils;

namespace PresetConverter
{
    /// <summary>
    /// A Steinberg Compressor Plugin .vstpreset file
    /// </summary>
    public class SteinbergCompressor : SteinbergVstPreset
    {
        public SteinbergCompressor()
        {
            Vst3ID = VstIDs.SteinbergCompressor;
            PlugInCategory = "Fx|Dynamics";
            PlugInName = "Compressor";
            PlugInVendor = "Steinberg Media Technologies";

            InitStartBytes(2304);
            InitParameters();
        }
        
        private void InitParameters()
        {
            InitNumberParameter("threshold", 0, -20.00);
            InitNumberParameter("ratio", 9, 2.00);
            InitNumberParameter("attack", 1, 1.00);
            InitNumberParameter("release", 2, 500.00);
            InitNumberParameter("autorelease", 14, 0.00);
            InitNumberParameter("hold", 3, 1.00);
            InitNumberParameter("makeUp", 4, 0.00);
            InitNumberParameter("automakeup", 10, 1.00);
            InitNumberParameter("softknee", 5, 1.00);
            InitNumberParameter("rms", 6, 80.00);
            InitNumberParameter("limit", 48, 0.00);
            InitNumberParameter("drymix", 49, 0.00);
            InitNumberParameter("live", 8, 0.00);
            InitNumberParameter("resetMaxGainRed", 42, 0.00);
            InitNumberParameter("bypass", 15, 0.00);
            InitNumberParameter("makeupMode", 46, 0.00);
        }
    }
}