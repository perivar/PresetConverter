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
    /// East West Play Preset
    /// </summary>
    public class EastWestPlay : VstPreset
    {
        public EastWestPlay()
        {
            Vst3ClassID = Vst3ClassIDs.EastWestPlay;
            PlugInCategory = "Instrument";
            PlugInName = "Play";
            PlugInVendor = "East West";
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
