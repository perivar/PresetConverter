using System.Text;
using CommonUtils;
using Serilog;

namespace PresetConverter
{
    /// <summary>
    /// Preset Class for reading and writing a Fabfilter Pro Q 3 Preset file
    /// </summary>
    public class FabfilterProQ3 : FabfilterProQBase
    {
        public List<ProQ3Band> Bands { get; set; }
        public int Version { get; set; }                        // Normally 4
        public int ParameterCount { get; set; }                 // Normally 334

        public List<float> UnknownParameters { get; set; }     // store the parameters we don't understand

        public FabfilterProQ3()
        {
            Version = 4;

            Vst3ID = VstIDs.FabFilterProQ3;
            PlugInCategory = "Fx";
            PlugInName = "FabFilter Pro-Q 3";
            PlugInVendor = "FabFilter";
        }

        public static string ToString(float[] parameters)
        {
            using (var sw = new StringWriter())
            {
                int counter = 0;
                foreach (var f in parameters)
                {
                    sw.WriteLine("{0:0.0000}", f);
                    counter++;
                    if (counter % 7 == 0) sw.WriteLine();
                }
                return sw.ToString();
            }
        }

        public bool ReadFFP(string filePath)
        {
            BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian);

            try
            {
                string header = binFile.ReadString(4);
                if (header != "FQ3p") return false;

                return ReadFFP(binFile);
            }
            finally
            {
                binFile.Close();
            }
        }

        public bool ReadFFP(BinaryFile binFile)
        {
            Version = (int)binFile.ReadUInt32();
            ParameterCount = (int)binFile.ReadUInt32();

            // parametercount = 334
            // 24 bands with 13 parameters each = 312
            // and then 22 parameters at the end

            Bands = new List<ProQ3Band>();
            for (int i = 0; i < 24; i++)
            {
                var band = new ProQ3Band();

                // 1 = Enabled, 0 = Disabled
                var fEnabled = binFile.ReadSingle();
                band.Enabled = fEnabled == 1 ? true : false;
                // Log.Debug("Band: {0} => enabled: {1}", i + 1, fEnabled);

                // unknown 1
                var fUnknown1 = binFile.ReadSingle();
                // Log.Debug("Band: {0} => unknown 1: {1}", i + 1, fUnknown1);

                // frequency
                var fFreq = binFile.ReadSingle();
                band.Frequency = FreqConvertBack(fFreq);
                // Log.Debug("Band: {0} => freq: {1} => {2}", i + 1, fFreq, band.Frequency);

                // gain
                var fGain = binFile.ReadSingle();
                band.Gain = fGain; // actual gain in dB
                // Log.Debug("Band: {0} => gain: {1}", i + 1, fGain);

                // dynamic range (if band is dynamic)
                var fDynamicRange = binFile.ReadSingle();
                band.DynamicRange = fDynamicRange;
                // Log.Debug("Band: {0} => dynamic range: {1}", i + 1, fDynamicRange);

                // unknown 3
                var fUnknown3 = binFile.ReadSingle();
                // Log.Debug("Band: {0} => unknown 3: {1}", i + 1, fUnknown3);

                // dynamic threshold in dB (1 = auto) - don't know how to convert this to dB
                // example numbers:
                // -1 dbFS      0.9833333
                // -90 dbFS     0
                // -20 dbFS     0.6666667
                // -54 dbFS     0.17500602
                var fDynamicThreshold = binFile.ReadSingle();
                band.DynamicThreshold = fDynamicThreshold;
                // Log.Debug("Band: {0} => dynamic threshold: {1} => {2}", i + 1, fDynamicThreshold, band.DynamicThreshold);

                // Q
                var fQ = binFile.ReadSingle();
                band.Q = QConvertBack(fQ);
                // Log.Debug("Band: {0} => Q: {1} => {2}", i + 1, fQ, band.Q);

                // 0 - 8
                var fFilterType = binFile.ReadSingle();
                switch (fFilterType)
                {
                    case (float)ProQ3Shape.Bell:
                        band.Shape = ProQ3Shape.Bell;
                        break;
                    case (float)ProQ3Shape.LowShelf:
                        band.Shape = ProQ3Shape.LowShelf;
                        break;
                    case (float)ProQ3Shape.LowCut:
                        band.Shape = ProQ3Shape.LowCut;
                        break;
                    case (float)ProQ3Shape.HighShelf:
                        band.Shape = ProQ3Shape.HighShelf;
                        break;
                    case (float)ProQ3Shape.HighCut:
                        band.Shape = ProQ3Shape.HighCut;
                        break;
                    case (float)ProQ3Shape.Notch:
                        band.Shape = ProQ3Shape.Notch;
                        break;
                    case (float)ProQ3Shape.BandPass:
                        band.Shape = ProQ3Shape.BandPass;
                        break;
                    case (float)ProQ3Shape.TiltShelf:
                        band.Shape = ProQ3Shape.TiltShelf;
                        break;
                    case (float)ProQ3Shape.FlatTilt:
                        band.Shape = ProQ3Shape.FlatTilt;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter type is outside range: {0}", fFilterType));
                }
                // Log.Debug("Band: {0} => filterType: {1} => {2}", i + 1, fFilterType, band.Shape);

                // 0 - 9
                var fFilterSlope = binFile.ReadSingle();
                switch (fFilterSlope)
                {
                    case (float)ProQ3Slope.Slope6dB_oct:
                        band.Slope = ProQ3Slope.Slope6dB_oct;
                        break;
                    case (float)ProQ3Slope.Slope12dB_oct:
                        band.Slope = ProQ3Slope.Slope12dB_oct;
                        break;
                    case (float)ProQ3Slope.Slope18dB_oct:
                        band.Slope = ProQ3Slope.Slope18dB_oct;
                        break;
                    case (float)ProQ3Slope.Slope24dB_oct:
                        band.Slope = ProQ3Slope.Slope24dB_oct;
                        break;
                    case (float)ProQ3Slope.Slope30dB_oct:
                        band.Slope = ProQ3Slope.Slope30dB_oct;
                        break;
                    case (float)ProQ3Slope.Slope36dB_oct:
                        band.Slope = ProQ3Slope.Slope36dB_oct;
                        break;
                    case (float)ProQ3Slope.Slope48dB_oct:
                        band.Slope = ProQ3Slope.Slope48dB_oct;
                        break;
                    case (float)ProQ3Slope.Slope72dB_oct:
                        band.Slope = ProQ3Slope.Slope72dB_oct;
                        break;
                    case (float)ProQ3Slope.Slope96dB_oct:
                        band.Slope = ProQ3Slope.Slope96dB_oct;
                        break;
                    case (float)ProQ3Slope.SlopeBrickwall:
                        band.Slope = ProQ3Slope.SlopeBrickwall;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter slope is outside range: {0}", fFilterSlope));
                }
                // Log.Debug("Band: {0} => filterSlope: {1} => {2}", i + 1, fFilterSlope, band.Slope);

                // 0 = Left, 1 = Right, 2 = Stereo, 3 = Mid, 4 = Side
                var fFilterStereoPlacement = binFile.ReadSingle();
                switch (fFilterStereoPlacement)
                {
                    case (float)ProQ3StereoPlacement.Left:
                        band.StereoPlacement = ProQ3StereoPlacement.Left;
                        break;
                    case (float)ProQ3StereoPlacement.Right:
                        band.StereoPlacement = ProQ3StereoPlacement.Right;
                        break;
                    case (float)ProQ3StereoPlacement.Stereo:
                        band.StereoPlacement = ProQ3StereoPlacement.Stereo;
                        break;
                    case (float)ProQ3StereoPlacement.Mid:
                        band.StereoPlacement = ProQ3StereoPlacement.Mid;
                        break;
                    case (float)ProQ3StereoPlacement.Side:
                        band.StereoPlacement = ProQ3StereoPlacement.Side;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Filter stereo placement is outside range: {0}", fFilterStereoPlacement));
                }
                // Log.Debug("Band: {0} => filterStereoPlacement: {1} => {2}", i + 1, fFilterStereoPlacement, band.StereoPlacement);

                // unknown band parameters
                for (int j = 0; j < 2; j++)
                {
                    var fUnknown = binFile.ReadSingle();
                    // Log.Debug("Band: {0} => unknown {1}: {2}", i + 1, j + 5, fUnknown);
                }

                Bands.Add(band);
            }

            // read the remaining floats
            UnknownParameters = new List<float>();
            int remainingParameterCount = ParameterCount - 13 * Bands.Count;
            for (int i = 0; i < remainingParameterCount; i++)
            {
                var fUnknown = binFile.ReadSingle();
                // Log.Debug("Param unknown {0}: {1}", i + 1, fUnknown);
                UnknownParameters.Add(fUnknown);
            }

            return true;
        }

        public override bool WriteFFP(string filePath)
        {
            using (BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian, true))
            {
                binFile.Write("FQ3p");
                binFile.Write((UInt32)Version);
                binFile.Write(GetBandsContent());
            }

            return true;
        }

        public bool WriteFXP(string filePath)
        {
            // Note, even if a DAW adds all these default parameters when saving a fxp
            // they does not seem to be needed when saving the FXP
            // add default unknown parameters
            // UnknownParameters.Add(0.0f);
            // UnknownParameters.Add(1.0f);
            // UnknownParameters.Add(1.0f);
            // UnknownParameters.Add(0.0f);
            // UnknownParameters.Add(0.0f);
            // UnknownParameters.Add(0.0f);
            // UnknownParameters.Add(0.0f);
            // UnknownParameters.Add(0.0f);
            // UnknownParameters.Add(0.0f);
            // UnknownParameters.Add(1.0f);
            // UnknownParameters.Add(1.0f);
            // UnknownParameters.Add(-1.0f);
            // UnknownParameters.Add(1.0f);
            // UnknownParameters.Add(2.0f);
            // UnknownParameters.Add(2.0f);
            // UnknownParameters.Add(3.0f);
            // UnknownParameters.Add(0.0f);
            // UnknownParameters.Add(1.0f);
            // UnknownParameters.Add(1.0f);
            // UnknownParameters.Add(2.0f);
            // UnknownParameters.Add(0.0f);
            // UnknownParameters.Add(0.0f);
            // for (int i = 0; i < 24; i++)
            // {
            //     UnknownParameters.Add(0.0f);
            // }

            var memStream = new MemoryStream();
            using (BinaryFile binFile = new BinaryFile(memStream, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII))
            {
                binFile.Write("FFBS");
                binFile.Write((UInt32)1); // this seems to always be a 1, not Version ?!
                binFile.Write(GetBandsContent());

                // add bottom bytes which seems to be mandatory to make the preset actually active
                // if this is not added, the preset seems to load, but stays inactive
                binFile.Write("FQ3p");
                binFile.Write(1);
                var presetName = Path.GetFileNameWithoutExtension(filePath);
                binFile.Write(presetName.Length);
                binFile.Write(presetName);
                binFile.Write(-1);
                binFile.Write(1);
                var pluginName = "Pro-Q";
                binFile.Write(pluginName.Length);
                binFile.Write(pluginName);
            }

            FXP.WriteRaw2FXP(filePath, memStream.ToArray(), "FQ3p");

            return true;
        }

        private byte[] GetBandsContent()
        {
            var memStream = new MemoryStream();
            using (BinaryFile binFile = new BinaryFile(memStream, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII))
            {
                // write total parameter count
                // 24 bands with 13 parameters each = 312
                // pluss the optional parameters at the end
                binFile.Write((UInt32)(24 * 13 + UnknownParameters.Count));

                for (int i = 0; i < 24; i++)
                {
                    if (i < Bands.Count)
                    {
                        binFile.Write((float)(Bands[i].Enabled ? 1 : 0));
                        binFile.Write((float)1); // unknown 1
                        binFile.Write((float)FreqConvert(Bands[i].Frequency));
                        binFile.Write((float)Bands[i].Gain);
                        binFile.Write((float)Bands[i].DynamicRange);
                        binFile.Write((float)1); // unknown 3
                        binFile.Write((float)Bands[i].DynamicThreshold);
                        binFile.Write((float)QConvert(Bands[i].Q));
                        binFile.Write((float)Bands[i].Shape);
                        binFile.Write((float)Bands[i].Slope);
                        binFile.Write((float)Bands[i].StereoPlacement);
                        binFile.Write((float)1); // unknown 5
                        binFile.Write((float)0); // unknown 6
                    }
                    else
                    {
                        binFile.Write((float)0);
                        binFile.Write((float)1);  // unknown 1
                        binFile.Write((float)FreqConvert(1000));
                        binFile.Write((float)0);  // gain
                        binFile.Write((float)0);  // dynamic range
                        binFile.Write((float)1);  // unknown 3
                        binFile.Write((float)1);  // dynamic threshold
                        binFile.Write((float)QConvert(1));
                        binFile.Write((float)ProQ2Shape.Bell);
                        binFile.Write((float)ProQSlope.Slope24dB_oct);
                        binFile.Write((float)ProQ2StereoPlacement.Stereo);
                        binFile.Write((float)1);  // unknown 5
                        binFile.Write((float)0);  // unknown 6                        
                    }
                }

                // write the remaining floats
                foreach (var fUnknown in UnknownParameters)
                {
                    binFile.Write((float)fUnknown); // unknown
                }
            }

            return memStream.ToArray();
        }

        public override string ToString()
        {
            var writer = new StringWriter();
            writer.WriteLine(string.Format("Vst3ID: {0}", this.Vst3ID));

            writer.WriteLine("Bands:");
            foreach (var band in this.Bands)
            {
                writer.WriteLine(band.ToString());
            }

            return writer.ToString();
        }

        protected override bool PreparedForWriting()
        {
            InitCompChunkData();
            InitContChunkData();
            InitInfoXml();
            CalculateBytePositions();
            return true;
        }

        public void InitCompChunkData()
        {
            // TODO: Haven't checked if this works!
            if (HasFXP)
            {
                SetCompChunkData(this.FXP);
            }
            else
            {
                var memStream = new MemoryStream();
                using (BinaryFile binFile = new BinaryFile(memStream, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII))
                {
                    binFile.Write("FabF");
                    binFile.Write((UInt32)Version);

                    var presetName = GetStringParameter("PresetName");
                    if (presetName == null)
                    {
                        presetName = "Default Setting";
                    }
                    binFile.Write((UInt32)presetName.Length);
                    binFile.Write(presetName);

                    binFile.Write((UInt32)0); // unknown

                    binFile.Write(GetBandsContent());

                    // add some unknown variables
                    binFile.Write(1);
                    binFile.Write(1);
                }

                this.CompChunkData = memStream.ToArray();
            }
        }

        public void InitContChunkData()
        {
            // TODO: Haven't checked if this works!
            if (HasFXP)
            {
                // don't do anything
            }
            else
            {
                var memStream = new MemoryStream();
                using (BinaryFile binFile = new BinaryFile(memStream, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII))
                {
                    binFile.Write("FFed");
                    binFile.Write((float)0.0);
                    binFile.Write((float)1.0);
                }

                this.ContChunkData = memStream.ToArray();
            }
        }

        /// <summary>
        /// Initialize the class specific variables using parameters
        /// </summary>
        public void InitFromParameters()
        {
            if (HasFXP)
            {
                var fxp = FXP;

                var byteArray = new byte[0];
                if (fxp.Content is FXP.FxProgramSet)
                {
                    byteArray = ((FXP.FxProgramSet)fxp.Content).ChunkData;
                }
                else if (fxp.Content is FXP.FxChunkSet)
                {
                    byteArray = ((FXP.FxChunkSet)fxp.Content).ChunkData;
                }

                var binFile = new BinaryFile(byteArray, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII);

                try
                {
                    string header = binFile.ReadString(4);
                    if (header != "FFBS") return;

                    ReadFFP(binFile);
                }
                finally
                {
                    binFile.Close();
                }
            }
            else
            {
                // init preset parameters
                // Note that the floats are not stored as IEEE (meaning between 0.0 - 1.0) but as floats representing the real values 
                var fabFilterProQ3Floats = Parameters
                                            .Where(v => v.Value.Type == Parameter.ParameterType.Number)
                                            .Select(v => (float)v.Value.Number.Value).ToArray();
                InitFromParameters(fabFilterProQ3Floats, false);
            }
        }

        /// <summary>
        /// Initialize the class specific variables using float parameters
        /// </summary>
        public void InitFromParameters(float[] floatParameters, bool isIEEE = true)
        {
            // NOT IMPLEMENTED
            throw new NotImplementedException();
        }
    }

    public enum ProQ3Shape
    {
        Bell = 0, // (default)
        LowShelf = 1,
        LowCut = 2,
        HighShelf = 3,
        HighCut = 4,
        Notch = 5,
        BandPass = 6,
        TiltShelf = 7,
        FlatTilt = 8
    }

    public enum ProQ3Slope
    {
        Slope6dB_oct = 0,
        Slope12dB_oct = 1,
        Slope18dB_oct = 2,
        Slope24dB_oct = 3, // (default)
        Slope30dB_oct = 4,
        Slope36dB_oct = 5,
        Slope48dB_oct = 6,
        Slope72dB_oct = 7,
        Slope96dB_oct = 8,
        SlopeBrickwall = 9,
    }

    public enum ProQ3StereoPlacement
    {
        Left = 0,
        Right = 1,
        Stereo = 2, // (default)
        Mid = 3,
        Side = 4,
    }

    public class ProQ3Band
    {
        public bool Enabled { get; set; }
        public double Frequency { get; set; }               // value range 10.0 -> 30000.0 Hz
        public double Gain { get; set; }                    // + or - value in dB
        public double DynamicRange { get; set; }            // + or - value in dB
        public double DynamicThreshold { get; set; }        // 1 = auto, or value in dB
        public double Q { get; set; }                       // value range 0.025 -> 40.00
        public ProQ3Shape Shape { get; set; }
        public ProQ3Slope Slope { get; set; }
        public ProQ3StereoPlacement StereoPlacement { get; set; }

        public override string ToString()
        {
            return string.Format("[{4,-3}] {0}: {1:0.00} Hz, {2:0.00} dB, Q: {3:0.00}, {5}, {6}", Shape, Frequency, Gain, Q, Enabled == true ? "On" : "Off", Slope, StereoPlacement);
        }
    }
}