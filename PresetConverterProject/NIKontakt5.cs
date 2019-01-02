using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;

using CommonUtils;

namespace PresetConverter
{
    /// <summary>
    /// Native Intruments Kontakt 5 Preset
    /// </summary>
    public class NIKontakt5 : VstPreset
    {
        public NIKontakt5()
        {
            Vst3ID = VstIDs.NIKontakt5;
            PlugInCategory = "Instrument";
            PlugInName = "Kontakt 5";
            PlugInVendor = "Native Instruments GmbH";
        }

        protected override bool PreparedForWriting()
        {
            SetCompChunkData(this.FXP);
            InitInfoXml();
            CalculateBytePositions();
            return true;
        }
    }
}
