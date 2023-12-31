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
                case VstPreset.Vst3ClassIDs.SteinbergCompressor:
                    preset = new SteinbergCompressor();
                    break;
                case VstPreset.Vst3ClassIDs.SteinbergFrequency:
                    preset = new SteinbergFrequency();
                    break;
                case VstPreset.Vst3ClassIDs.SteinbergREVerence:
                    preset = new SteinbergREVerence();
                    break;
                case VstPreset.Vst3ClassIDs.FabFilterProQ:
                case VstPreset.Vst3ClassIDs.FabFilterProQx64:
                    preset = new FabfilterProQ();
                    preset.Vst3ClassID = guid;
                    break;
                case VstPreset.Vst3ClassIDs.FabFilterProQ2:
                case VstPreset.Vst3ClassIDs.FabFilterProQ2x64:
                    preset = new FabfilterProQ2();
                    preset.Vst3ClassID = guid;
                    break;
                case VstPreset.Vst3ClassIDs.NIKontakt5:
                    preset = new NIKontakt5();
                    break;
                case VstPreset.Vst3ClassIDs.NIKontakt6:
                    preset = new NIKontakt6();
                    break;
                case VstPreset.Vst3ClassIDs.NIKontakt6_64out:
                    preset = new NIKontakt6_64out();
                    break;
                case VstPreset.Vst3ClassIDs.EastWestPlay:
                    preset = new EastWestPlay();
                    break;
                default:
                    preset = new SteinbergVstPreset();
                    preset.Vst3ClassID = guid;
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

                if (preset.Vst3ClassID == VstPreset.Vst3ClassIDs.SteinbergREVerence)
                {
                    // init wave paths and images from the parameters
                    var reverence = preset as SteinbergREVerence;
                    reverence.InitFromParameters();
                }

                else if (preset.Vst3ClassID == VstPreset.Vst3ClassIDs.FabFilterProQ
                    || preset.Vst3ClassID == VstPreset.Vst3ClassIDs.FabFilterProQx64)
                {
                    // init variables from the parameters or FXP object
                    var fabFilterProQ = preset as FabfilterProQ;
                    fabFilterProQ.InitFromParameters();
                }

                else if (preset.Vst3ClassID == VstPreset.Vst3ClassIDs.FabFilterProQ2
                    || preset.Vst3ClassID == VstPreset.Vst3ClassIDs.FabFilterProQ2x64)
                {
                    // init variables from the parameters or FXP object
                    var fabFilterProQ2 = preset as FabfilterProQ2;
                    fabFilterProQ2.InitFromParameters();
                }

                else if (preset.Vst3ClassID == VstPreset.Vst3ClassIDs.FabFilterProQ3)
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
            switch (vstPreset.Vst3ClassID)
            {
                case VstPreset.Vst3ClassIDs.SteinbergCompressor:
                    preset = new SteinbergCompressor();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;
                    break;
                case VstPreset.Vst3ClassIDs.SteinbergFrequency:
                    preset = new SteinbergFrequency();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;
                    break;
                case VstPreset.Vst3ClassIDs.SteinbergREVerence:
                    preset = new SteinbergREVerence();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;

                    // init wave paths and images from the parameters
                    var reverence = preset as SteinbergREVerence;
                    reverence.InitFromParameters();
                    break;
                case VstPreset.Vst3ClassIDs.FabFilterProQ:
                case VstPreset.Vst3ClassIDs.FabFilterProQx64:
                    preset = new FabfilterProQ();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;

                    // init variables from the parameters or FXP object
                    var fabFilterProQ = preset as FabfilterProQ;
                    fabFilterProQ.InitFromParameters();
                    break;
                case VstPreset.Vst3ClassIDs.FabFilterProQ2:
                case VstPreset.Vst3ClassIDs.FabFilterProQ2x64:
                    preset = new FabfilterProQ2();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;

                    // init variables from the parameters or FXP object
                    var fabFilterProQ2 = preset as FabfilterProQ2;
                    fabFilterProQ2.InitFromParameters();

                    break;
                case VstPreset.Vst3ClassIDs.FabFilterProQ3:
                    preset = new FabfilterProQ3();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;

                    // init variables from the parameters or FXP object
                    var fabFilterProQ3 = preset as FabfilterProQ3;
                    fabFilterProQ3.InitFromParameters();

                    break;
                case VstPreset.Vst3ClassIDs.WavesSSLChannelStereo:
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
                case VstPreset.Vst3ClassIDs.WavesSSLCompStereo:
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
                case VstPreset.Vst3ClassIDs.NIKontakt5:
                    preset = new NIKontakt5();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;
                    break;

                case VstPreset.Vst3ClassIDs.EastWestPlay:
                    preset = new EastWestPlay();
                    preset.Parameters = vstPreset.Parameters;
                    preset.FXP = vstPreset.FXP;
                    break;

                default:
                    preset = vstPreset;
                    break;
            }

            preset.Vst3ClassID = vstPreset.Vst3ClassID;

            return preset as T;
        }
    }
}
