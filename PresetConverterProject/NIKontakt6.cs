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
    /// Native Instruments Kontakt 6 Preset
    /// </summary>
    public class NIKontakt6 : NIKontaktBase
    {
        public NIKontakt6()
        {
            Vst3ClassID = Vst3ClassIDs.NIKontakt6;
            PlugInCategory = "Instrument";
            PlugInName = "Kontakt 6";
            PlugInVendor = "Native Instruments GmbH";
        }
    }
}
