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
        public static T GetVstPreset<T>(byte[] presetBytes, string guid, string pluginName = null) where T : VstPreset
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
                case VstPreset.VstIDs.FabFilterProQ:
                case VstPreset.VstIDs.FabFilterProQx64:
                    preset = new FabfilterProQ();
                    preset.Vst3ID = guid;
                    break;
                case VstPreset.VstIDs.FabFilterProQ2:
                case VstPreset.VstIDs.FabFilterProQ2x64:
                    preset = new FabfilterProQ2();
                    preset.Vst3ID = guid;
                    break;
                case VstPreset.VstIDs.NIKontakt5:
                    preset = new NIKontakt5();
                    break;
                case VstPreset.VstIDs.NIKontakt6:
                    preset = new NIKontakt6();
                    break;
                case VstPreset.VstIDs.NIKontakt6_64out:
                    preset = new NIKontakt6_64out();
                    break;
                case VstPreset.VstIDs.EastWestPlay:
                    preset = new EastWestPlay();
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
                    reverence.InitFromParameters();
                }

                else if (preset.Vst3ID == VstPreset.VstIDs.FabFilterProQ
                    || preset.Vst3ID == VstPreset.VstIDs.FabFilterProQx64)
                {
                    // init variables from the parameters or FXP object
                    var fabFilterProQ = preset as FabfilterProQ;
                    fabFilterProQ.InitFromParameters();
                }

                else if (preset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2
                    || preset.Vst3ID == VstPreset.VstIDs.FabFilterProQ2x64)
                {
                    // init variables from the parameters or FXP object
                    var fabFilterProQ2 = preset as FabfilterProQ2;
                    fabFilterProQ2.InitFromParameters();
                }

                else if (preset.Vst3ID == VstPreset.VstIDs.FabFilterProQ3)
                {
                    // init variables from the parameters or FXP object
                    var fabFilterProQ3 = preset as FabfilterProQ3;
                    fabFilterProQ3.InitFromParameters();
                }

            }
            catch (System.Exception e)
            {
                Log.Error("Failed initializing VstPreset using guid: {0}{1}. (Hex dump: {2}) {3}", guid, pluginName != null ? " and name " + pluginName : "", StringUtils.ToHexEditorString(presetBytes), e.Message);
            }

            return preset as T;
        }

        /// <summary>
        /// Initialize a VstPreset using a file
        /// </summary>
        /// <param name="file">filename</param>
        /// <returns>a VstPreset object</returns>
        public static T GetVstPreset<T>(string file) where T : VstPreset
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
                    reverence.InitFromParameters();
                    break;
                case VstPreset.VstIDs.FabFilterProQ:
                case VstPreset.VstIDs.FabFilterProQx64:
                    preset = new FabfilterProQ();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;

                    // init variables from the parameters or FXP object
                    var fabFilterProQ = preset as FabfilterProQ;
                    fabFilterProQ.InitFromParameters();
                    break;
                case VstPreset.VstIDs.FabFilterProQ2:
                case VstPreset.VstIDs.FabFilterProQ2x64:
                    preset = new FabfilterProQ2();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;

                    // init variables from the parameters or FXP object
                    var fabFilterProQ2 = preset as FabfilterProQ2;
                    fabFilterProQ2.InitFromParameters();

                    break;
                case VstPreset.VstIDs.FabFilterProQ3:
                    preset = new FabfilterProQ3();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;

                    // init variables from the parameters or FXP object
                    var fabFilterProQ3 = preset as FabfilterProQ3;
                    fabFilterProQ3.InitFromParameters();

                    break;
                case VstPreset.VstIDs.WavesSSLChannelStereo:
                    VstPreset.Parameter sslChannelXml = null;
                    vstPreset.Parameters.TryGetValue("XmlContent", out sslChannelXml);
                    if (sslChannelXml != null && sslChannelXml.String != null)
                    {
                        List<WavesSSLChannel> channelPresetList = WavesPreset.ParseXml<WavesSSLChannel>(sslChannelXml.String);

                        // a single vstpreset likely (?) only contain one waves ssl preset, use the first
                        preset = channelPresetList.FirstOrDefault();
                        preset.Parameters = vstPreset.Parameters;
                        preset.FXP = vstPreset.FXP;
                    }
                    break;
                case VstPreset.VstIDs.WavesSSLCompStereo:
                    VstPreset.Parameter sslCompXml = null;
                    vstPreset.Parameters.TryGetValue("XmlContent", out sslCompXml);
                    if (sslCompXml != null && sslCompXml.String != null)
                    {
                        List<WavesSSLComp> channelPresetList = WavesPreset.ParseXml<WavesSSLComp>(sslCompXml.String);

                        // a single vstpreset likely (?) only contain one waves ssl preset, use the first
                        preset = channelPresetList.FirstOrDefault();
                        preset.Parameters = vstPreset.Parameters;
                        preset.FXP = vstPreset.FXP;
                    }
                    break;
                case VstPreset.VstIDs.NIKontakt5:
                    preset = new NIKontakt5();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;
                    break;

                case VstPreset.VstIDs.EastWestPlay:
                    preset = new EastWestPlay();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;
                    break;

                default:
                    preset = vstPreset;
                    break;
            }

            preset.Vst3ID = vstPreset.Vst3ID;

            return preset as T;
        }
    }
}
