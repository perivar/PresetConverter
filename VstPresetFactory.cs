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
        /// <summary>
        /// Initialize a VstPreset using a byte array and guid
        /// </summary>
        /// <param name="presetBytes">preset bytes</param>
        /// <param name="guid">plugin guid</param>
        /// <param name="pluginName">optional plugin name (only used for error messages)</param>
        /// <returns>a VstPreset object</returns>
        public static VstPreset GetVstPreset(byte[] presetBytes, string guid, string pluginName = null)
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
                case VstPreset.VstIDs.NIKontakt5:
                    preset = new NIKontakt5();
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

                if (preset.Vst3ID == VstPreset.VstIDs.SteinbergREVerence)
                {
                    // init wave paths and images from the parameters
                    var reverence = preset as SteinbergREVerence;
                    reverence.InitPathsAndImagesFromParameters();
                }
            }
            catch (System.Exception e)
            {
                Log.Error("Failed initializing VstPreset using guid: {0}{1}. (Hex dump: {2}) {3}", guid, pluginName != null ? " and name " + pluginName : "", StringUtils.ToHexEditorString(presetBytes), e.Message);
            }

            return preset;
        }

        /// <summary>
        /// Initialize a VstPreset using a file
        /// </summary>
        /// <param name="file">filename</param>
        /// <returns>a VstPreset object</returns>
        public static VstPreset GetVstPreset(string file)
        {
            VstPreset vstPreset = new SteinbergVstPreset(file);

            VstPreset preset = null;
            switch (vstPreset.Vst3ID)
            {
                case VstPreset.VstIDs.SteinbergCompressor:
                    preset = new SteinbergCompressor();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;
                    break;
                case VstPreset.VstIDs.SteinbergFrequency:
                    preset = new SteinbergFrequency();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;
                    break;
                case VstPreset.VstIDs.SteinbergREVerence:
                    preset = new SteinbergREVerence();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;

                    // init wave paths and images from the parameters
                    var reverence = preset as SteinbergREVerence;
                    reverence.InitPathsAndImagesFromParameters();

                    break;
                case VstPreset.VstIDs.NIKontakt5:
                    preset = new NIKontakt5();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;
                    break;
                default:
                    preset = vstPreset;
                    break;
            }

            return preset;
        }
    }
}
