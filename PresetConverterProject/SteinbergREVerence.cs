using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommonUtils;

namespace PresetConverter
{
    /// <summary>
    /// A Steinberg REVerence Plugin .vstpreset file
    /// </summary>
    public class SteinbergREVerence : SteinbergVstPreset
    {
        public string WavFilePath1;
        public string WavFilePath2;
        public string WavFileName;
        public List<string> Images = new List<string>();

        public SteinbergREVerence()
        {
            Vst3ClassID = Vst3ClassIDs.SteinbergREVerence;
            PlugInCategory = "Fx|Reverb";
            PlugInName = "REVerence";
            PlugInVendor = "Steinberg Media Technologies";

            InitParameters();
        }

        private void InitParameters()
        {
            InitNumberParameter("mix", 0, 100.00);
            InitNumberParameter("predelay", 6, 0.00);
            InitNumberParameter("time", 1, 100.00);
            InitNumberParameter("size", 2, 100.00);
            InitNumberParameter("level", 3, 0.00);
            InitNumberParameter("ertailsplit", 29, 35.00);
            InitNumberParameter("ertailmix", 30, 100.00);
            InitNumberParameter("reverse", 17, 0.00);
            InitNumberParameter("trim", 18, 0.00);
            InitNumberParameter("autolevel", 24, 1.00);
            InitNumberParameter("trimstart", 19, 80.00);
            InitNumberParameter("trimend", 20, 80.00);
            InitNumberParameter("eqon", 38, 0.00);
            InitNumberParameter("lowfilterfreq", 7, 100.00);
            InitNumberParameter("lowfiltergain", 8, 0.00);
            InitNumberParameter("peakfreq", 9, 1000.00);
            InitNumberParameter("peakgain", 10, 6.00);
            InitNumberParameter("highfilterfreq", 11, 15000.00);
            InitNumberParameter("highfiltergain", 12, 0.00);
            InitNumberParameter("lowfilteron", 14, 1.00);
            InitNumberParameter("peakon", 15, 1.00);
            InitNumberParameter("highfilteron", 16, 1.00);
            InitNumberParameter("output", 4, 0.00);
            InitNumberParameter("predelayoffset", 25, 0.00);
            InitNumberParameter("timeoffset", 26, 0.00);
            InitNumberParameter("sizeoffset", 27, 0.00);
            InitNumberParameter("leveloffset", 28, 0.00);
            InitNumberParameter("ertailsplitoffset", 31, 0.00);
            InitNumberParameter("ertailmixoffset", 32, 0.00);
            InitNumberParameter("store", 34, 1.00);
            InitNumberParameter("erase", 35, 0.00);
            InitNumberParameter("autopresetnr", 37, 0.00);
            InitNumberParameter("channelselect", 39, 0.00);
            InitNumberParameter("transProgress", 73, 0.00);
            InitNumberParameter("impulseTrigger", 74, 0.00);
            InitNumberParameter("bypass", 13, 0.00);
            InitNumberParameter("allowFading", 87, 0.00);
        }

        /// <summary>
        /// Initialize the REVerence specific variables using the stored Parameters list
        /// and make sure to delete the parameters afterwards
        /// </summary>
        public void InitFromParameters()
        {
            if (Parameters.ContainsKey("wave-file-path-1"))
            {
                WavFilePath1 = Parameters["wave-file-path-1"].String;
                Parameters.Remove("wave-file-path-1");
            }
            if (Parameters.ContainsKey("wave-count"))
            {
                Parameters.Remove("wave-count");
            }
            if (Parameters.ContainsKey("wave-file-path-2"))
            {
                WavFilePath2 = Parameters["wave-file-path-2"].String;
                Parameters.Remove("wave-file-path-2");
            }
            if (Parameters.ContainsKey("wave-file-name"))
            {
                WavFileName = Parameters["wave-file-name"].String;
                Parameters.Remove("wave-file-name");
            }
            if (Parameters.ContainsKey("image-count"))
            {
                Parameters.Remove("image-count");
            }
            if (Parameters.ContainsKey("parameter-count"))
            {
                Parameters.Remove("parameter-count");
            }

            // and copy the images
            for (int i = 0; i < 10; i++)
            {
                string key = string.Format("image-file-name-{0}", (i + 1));
                if (Parameters.ContainsKey(key))
                {
                    Images.Add(Parameters[key].String);
                    Parameters.Remove(key);
                }
                else
                {
                    break;
                }
            }
        }

        public override void InitCompChunkData()
        {
            int wavCount = (WavFilePath1 != null && !"".Equals(WavFilePath1) ? 1 : 0);
            int imageCount = Images.Count;
            int parameterCount = this.Parameters.Values.Count;

            var memStream = new MemoryStream();
            using (BinaryFile bf = new BinaryFile(memStream, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII))
            {
                // write 1024 bytes
                bf.WriteStringPadded(WavFilePath1, 1024, Encoding.Unicode);

                // write wave count
                bf.Write((UInt32)wavCount);

                if (wavCount > 0)
                {
                    // write unknown
                    bf.Write((UInt32)0);

                    // write 1024 bytes
                    bf.WriteStringPadded(WavFilePath2, 1024, Encoding.Unicode);

                    // write 1024 bytes
                    bf.WriteStringPadded(WavFileName, 1024, Encoding.Unicode);

                    // write image count
                    bf.Write((UInt32)imageCount);

                    // add images
                    for (int i = 0; i < imageCount; i++)
                    {
                        // write 1024 bytes
                        bf.WriteStringPadded(Images[i], 1024, Encoding.Unicode);
                    }

                    // write parameter count
                    bf.Write((UInt32)parameterCount);
                }
                else
                {
                    // write unknown
                    bf.Write((UInt32)5328);
                }

                // write parameters
                foreach (var parameter in this.Parameters.Values)
                {
                    if (parameter.Type == Parameter.ParameterType.Number)
                    {
                        var paramName = parameter.Name.PadRight(128, '\0').Substring(0, 128);
                        bf.Write(paramName);
                        bf.Write(parameter.Index);
                        bf.Write(parameter.Number.Value);
                    }
                }

                if (wavCount > 0)
                {
                    // write unknown
                    bf.Write((UInt32)5328);

                    // write parameters once more
                    foreach (var parameter in this.Parameters.Values)
                    {
                        if (parameter.Type == Parameter.ParameterType.Number)
                        {
                            var paramName = parameter.Name.PadRight(128, '\0').Substring(0, 128);
                            bf.Write(paramName);
                            bf.Write(parameter.Index);
                            bf.Write(parameter.Number.Value);
                        }
                    }
                }
            }

            this.CompChunkData = memStream.ToArray();
        }
    }
}