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
    /// Native Instruments Kontakt 6 64-out Preset
    /// </summary>
    public class NIKontakt6_64out : NIKontaktBase
    {
        public NIKontakt6_64out()
        {
            Vst3ClassID = VstClassIDs.NIKontakt6_64out;
            PlugInCategory = "Instrument";
            PlugInName = "Kontakt 6";
            PlugInVendor = "Native Instruments GmbH";
        }
    }
}
