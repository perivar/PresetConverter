using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommonUtils;
using Serilog;

namespace PresetConverter
{
    /// <summary>
    /// Base Preset Class for reading and writing a Fabfilter Pro Q (1 or 2) Preset file
    /// </summary>
    public abstract class FabfilterProQBase : VstPreset
    {
        public FabfilterProQBase()
        {
        }

        public abstract bool WriteFFP(string filePath);

        public static float[] ReadFloats(string filePath, string headerExpected)
        {
            BinaryFile binFile = new BinaryFile(filePath, BinaryFile.ByteOrder.LittleEndian);

            string header = binFile.ReadString(4);
            if (header == headerExpected) // "FPQr", "FQ2p" or "FQ3p"
            {
                int version = binFile.ReadInt32();
                int parameterCount = binFile.ReadInt32();

                var floatArray = new float[parameterCount];
                int i = 0;
                try
                {
                    for (i = 0; i < parameterCount; i++)
                    {
                        floatArray[i] = binFile.ReadSingle();
                    }

                }
                catch (System.Exception e)
                {
                    Log.Error("Failed reading floats: {0}", e);
                }

                binFile.Close();
                return floatArray;
            }
            else
            {
                binFile.Close();
                return null;
            }
        }

        /// <summary>
        /// convert a float between 0 and 1 to the fabfilter float equivalent
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float IEEEFloatToFrequencyFloat(float value)
        {
            return 11.5507311008828f * value + 3.32193432374016f;
        }

        // log and inverse log
        // a ^ x = b 
        // x = log(b) / log(a)

        public static double FreqConvert(double value)
        {
            // =LOG(A1)/LOG(2) (default = 1000 Hz)
            return Math.Log10(value) / Math.Log10(2);
        }

        public static double FreqConvertBack(double value)
        {
            // =POWER(2; frequency)
            return Math.Pow(2, value);
        }

        public static double QConvert(double value)
        {
            // =LOG(F1)*0,312098175+0,5 (default = 1)
            return Math.Log10(value) * 0.312098175 + 0.5;
        }

        public static double QConvertBack(double value)
        {
            // =POWER(10;((B3-0,5)/0,312098175))
            return Math.Pow(10, (value - 0.5) / 0.312098175);
        }
    }
}
