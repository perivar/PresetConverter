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
    /// Native Instruments Kontakt 5 Preset
    /// </summary>
    public class NIKontakt5 : NIKontaktBase
    {
        public NIKontakt5()
        {
            Vst3ClassID = Vst3ClassIDs.NIKontakt5;
            PlugInCategory = "Instrument";
            PlugInName = "Kontakt 5";
            PlugInVendor = "Native Instruments GmbH";
        }
    }
}
