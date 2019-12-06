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
    /// Native Instruments Kontakt Preset Base Class
    /// </summary>
    public abstract class NIKontaktBase : VstPreset
    {
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
