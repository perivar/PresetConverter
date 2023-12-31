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
            Vst3ClassID = Vst3ClassIDs.SteinbergCompressor;
            PlugInCategory = "Fx|Dynamics";
            PlugInName = "Compressor";
            PlugInVendor = "Steinberg Media Technologies";

            // later presets start with 2304 and contain two more parameters than previous presets (limit and drymix)
            // previous presets start with 2016
            InitStartBytes(2304);

            InitParameters();
        }

        private void InitParameters()
        {
            InitNumberParameter("threshold", 0, -20.00);        // 0.0 to - 60.0
            InitNumberParameter("ratio", 9, 2.00);              // 1.0 to 8.0
            InitNumberParameter("attack", 1, 1.00);             // 0.1 to 100.0
            InitNumberParameter("release", 2, 500.00);          // 10.0 to 1000.0
            InitNumberParameter("autorelease", 14, 0.00);       // on is 1.0
            InitNumberParameter("hold", 3, 1.00);               // 0.0 to 5000.0
            InitNumberParameter("makeUp", 4, 0.00);             // 0.0 to 24.0
            InitNumberParameter("automakeup", 10, 1.00);        // on is 1.0
            InitNumberParameter("softknee", 5, 1.00);           // on is 1.0
            InitNumberParameter("rms", 6, 80.00);               // 0.0 to 100.0
            InitNumberParameter("limit", 48, 0.00);             // max ratio = limit on is 1.0
            InitNumberParameter("drymix", 49, 0.00);            // 0.0 to 100.0
            InitNumberParameter("live", 8, 0.00);               // on is 1.0
            InitNumberParameter("resetMaxGainRed", 42, 0.00);   // ?
            InitNumberParameter("bypass", 15, 0.00);            // on is 1.0
            InitNumberParameter("makeupMode", 46, 0.00);        // on is 1.0
        }
    }
}