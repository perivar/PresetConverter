using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommonUtils;
using Serilog;

namespace PresetConverter
{
    /// <summary>
    /// A helper class to get a VstPreset object
    /// </summary>
    public class VstPresetFactory
    {
        public static VstPreset GetVstPreset(string guid, byte[] presetBytes)
        {
            VstPreset preset = null;
            switch (guid)
            {
                case VstPreset.VstIDs.SteinbergCompressor:
                    preset = new SteinbergCompressor();
                    break;
                case VstPreset.VstIDs.SteinbergFrequency:
                    preset = new SteinbergFrequency();
                    break;
                case VstPreset.VstIDs.SteinbergREVerence:
                    preset = new SteinbergREVerence();
                    break;
                default:
                    preset = new SteinbergVstPreset();
                    preset.Vst3ID = guid;
                    break;
            }

            preset.Parameters.Clear();
            preset.CompDataStartPos = 0;
            preset.CompDataChunkSize = presetBytes.Length;
            preset.ContDataStartPos = presetBytes.Length;
            preset.ContDataChunkSize = 0;
            preset.InfoXmlStartPos = presetBytes.Length;

            try
            {
                preset.ReadData(new BinaryFile(presetBytes, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII), (UInt32)presetBytes.Length, false);
            }
            catch (System.Exception e)
            {
                Log.Error("Failed initializing VstPreset with guid: {0}. (Hex dump: {1}) {2}", guid, StringUtils.ToHexEditorString(presetBytes), e.Message);
            }

            return preset;
        }
    }
}
