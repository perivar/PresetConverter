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

        public bool WriteNKI(string filePath)
        {
            var fxp = this.FXP;
            if (fxp == null || fxp.Content == null) return false;

            var byteArray = new byte[0];
            if (fxp.Content is FXP.FxProgramSet)
            {
                byteArray = ((FXP.FxProgramSet)fxp.Content).ChunkData;
            }
            else if (fxp.Content is FXP.FxChunkSet)
            {
                byteArray = ((FXP.FxChunkSet)fxp.Content).ChunkData;
            }

            using (BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian, true))
            {
                // NKI's are little endian
                // starts with:
                // UInt32: number of preset bytes
                // UInt32: 0
                // UInt32: 1
                // 'hsin'
                binFile.Write(byteArray);
            }

            return true;
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
