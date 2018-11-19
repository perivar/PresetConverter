using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using CommonUtils;

namespace AbletonLiveConverter
{
    public class SteinbergCompressor : VstPreset
    {
        public SteinbergCompressor()
        {
            Vst3ID = VstPreset.VstIDs.Compressor;
            PlugInCategory = "Fx|Dynamics";
            PlugInName = "Compressor";
            InitXml();
            InitParameters();

            // set byte positions and sizes within the vstpreset (for writing)
            ListPos = 2731; // position of List chunk
            DataChunkSize = 2304; // data chunk length. i.e. total length minus 4 ('VST3')
            ParameterDataStartPos = 48; // parameter data start position
            ParameterDataSize = 2244; // byte length from parameter data start position up until xml data
            XmlStartPos = 2292; // xml start position
        }

        private void InitParameters()
        {
            AddParameterToDictionary("threshold", 0, -20.00);
            AddParameterToDictionary("ratio", 9, 2.00);
            AddParameterToDictionary("attack", 1, 1.00);
            AddParameterToDictionary("release", 2, 500.00);
            AddParameterToDictionary("autorelease", 14, 0.00);
            AddParameterToDictionary("hold", 3, 1.00);
            AddParameterToDictionary("makeUp", 4, 0.00);
            AddParameterToDictionary("automakeup", 10, 1.00);
            AddParameterToDictionary("softknee", 5, 1.00);
            AddParameterToDictionary("rms", 6, 80.00);
            AddParameterToDictionary("limit", 48, 0.00);
            AddParameterToDictionary("drymix", 49, 0.00);
            AddParameterToDictionary("live", 8, 0.00);
            AddParameterToDictionary("resetMaxGainRed", 42, 0.00);
            AddParameterToDictionary("bypass", 15, 0.00);
            AddParameterToDictionary("makeupMode", 46, 0.00);
        }
    }
}