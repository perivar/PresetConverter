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

        public SteinbergVstPreset(string guid, byte[] presetBytes) : base()
        {
            this.Vst3ID = guid;
            this.CompDataStartPos = 0;
            this.CompDataChunkSize = presetBytes.Length;
            this.ContDataStartPos = presetBytes.Length;
            this.ContDataChunkSize = 0;
            this.InfoXmlStartPos = presetBytes.Length;

            try
            {
                this.ReadData(new BinaryFile(presetBytes, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII), (UInt32)presetBytes.Length, false);
            }
            catch (System.Exception e)
            {
                Log.Error("Failed initializing SteinbergVstPreset with guid: {0}. (Hex dump: {1}) {2}", guid, StringUtils.ToHexEditorString(presetBytes), e.Message);
            }
        }

        public SteinbergVstPreset(string fileName) : base(fileName)
        {
        }

        protected override bool PreparedForWriting()
        {
            InitChunkData();
            InitMetaInfoXml();
            CalculateBytePositions();
            return true;
        }

        public void InitStartBytes(UInt32 value)
        {
            // add the 4 unknown bytes before the parameters start
            this.Parameters.Add("StartBytes", new Parameter("StartBytes", 0, BitConverter.GetBytes(value)));
        }

        public void InitNumberParameter(string name, UInt32 number, double value)
        {
            var parameter = new Parameter(name, number, value);
            this.Parameters.Add(name, parameter);
        }

        public virtual void InitChunkData()
        {
            var memStream = new MemoryStream();
            using (BinaryFile bf = new BinaryFile(memStream, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII))
            {
                // write parameters
                foreach (var parameter in this.Parameters.Values)
                {
                    if (parameter.Type == Parameter.ParameterType.Bytes)
                    {
                        bf.Write(parameter.ByteValue);
                    }
                    else
                    if (parameter.Type == Parameter.ParameterType.Number)
                    {
                        var paramName = parameter.Name.PadRight(128, '\0').Substring(0, 128);
                        bf.Write(paramName);
                        bf.Write(parameter.Number);
                        bf.Write(parameter.NumberValue);
                    }
                    else
                    if (parameter.Type == Parameter.ParameterType.String)
                    {
                        bf.Write(parameter.StringValue);
                    }
                }
            }

            this.SetChunkData(memStream.ToArray());
        }
    }
}