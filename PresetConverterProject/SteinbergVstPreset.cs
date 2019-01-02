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
    /// A Steinberg .vstpreset file
    /// </summary>
    public class SteinbergVstPreset : VstPreset
    {
        public SteinbergVstPreset() : base()
        {
        }

        public SteinbergVstPreset(string fileName) : base(fileName)
        {
        }

        protected override bool PreparedForWriting()
        {
            InitCompChunkData();
            InitMetaInfoXml();
            CalculateBytePositions();
            return true;
        }

        public void InitStartBytes(int value)
        {
            // add the 4 unknown bytes before the parameters start
            this.Parameters.Add("StartBytes", new Parameter("StartBytes", value, BitConverter.GetBytes(value)));
        }

        public void InitNumberParameter(string name, int index, double value)
        {
            var parameter = new Parameter(name, index, value);
            this.Parameters.Add(name, parameter);
        }

        public virtual void InitCompChunkData()
        {
            var memStream = new MemoryStream();
            using (BinaryFile bf = new BinaryFile(memStream, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII))
            {
                // write parameters
                foreach (var parameter in this.Parameters.Values)
                {
                    if (parameter.Type == Parameter.ParameterType.Bytes)
                    {
                        bf.Write(parameter.Bytes);
                    }
                    else
                    if (parameter.Type == Parameter.ParameterType.Number)
                    {
                        var paramName = parameter.Name.PadRight(128, '\0').Substring(0, 128);
                        bf.Write(paramName);
                        bf.Write(parameter.Index);
                        bf.Write(parameter.Number.Value);
                    }
                    else
                    if (parameter.Type == Parameter.ParameterType.String)
                    {
                        bf.Write(parameter.String);
                    }
                }
            }

            this.CompChunkData = memStream.ToArray();
        }
    }
}