using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;

using PresetConverter;
using CommonUtils;
using AbletonLiveConverter;

namespace PresetConverter
{
    /// <summary>
    /// Native Intruments Kontakt 5 Preset
    /// </summary>
    public class NIKontakt5 : VstPreset
    {
        FXP FXP { get; set; }

        public NIKontakt5()
        {
            Vst3ID = VstIDs.NIKontakt5;
            PlugInCategory = "Instrument";
            PlugInName = "Kontakt 5";
            PlugInVendor = "Native Instruments GmbH";
        }

        public NIKontakt5(FXP fxp) : this()
        {
            FXP = fxp;
        }

        #region Read and Write Methods
        protected override bool PreparedForWriting()
        {
            InitChunkData();
            InitMetaInfoXml();
            CalculateBytePositions();
            return true;
        }

        private void InitChunkData()
        {
            SetChunkData(this.FXP);
        }

        #endregion
    }
}
