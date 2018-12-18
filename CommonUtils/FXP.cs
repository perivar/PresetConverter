using System;
using System.Xml;
using System.IO;
using System.Text;

namespace CommonUtils
{
    /// <summary>
    /// Class for reading and writing Steinberg Preset files and Bank files (fxp and fxb files).
    /// Per Ivar Nerseth, 2011 - 2018
    /// perivar@nerseth.com
    /// </summary>
    public class FXP
    {
        public FxContent Content { get; set; }
        public XmlDocument XmlDocument { get; set; }

        public class FxContent
        {
            public string ChunkMagic { get; set; }     // 'CcnK'
            public int ByteSize { get; set; }          // of this chunk, excl. magic + byteSize
            public string FxMagic { get; set; }        // 'FPCh', 'FBCh', 'FxCk', 'FxBk'
            public int Version { get; set; }
            public string FxID { get; set; }           // fx unique id
            public int FxVersion { get; set; }

        }

        // Preset (Program) (.fxp) with chunk (magic = 'FPCh')
        public class FxProgramSet : FxContent
        {
            public int NumPrograms { get; set; }
            public string Name { get; set; }            // 28 chars
            public int ChunkSize { get; set; }
            public string ChunkData { get; set; }       // chunkSize
            public byte[] ChunkDataByteArray { get; set; } // chunkSize
        }

        // Bank (.fxb) with chunk (magic = 'FBCh')
        public class FxChunkSet : FxContent
        {
            public int NumPrograms { get; set; }
            public string Future { get; set; }         // 128
            public int ChunkSize { get; set; }
            public string ChunkData { get; set; }      // chunkSize
            public byte[] ChunkDataByteArray { get; set; } // chunkSize
        }

        // For Preset (Program) (.fxp) without chunk (magic = 'FxCk')
        public class FxProgram : FxContent
        {
            public int NumParameters { get; set; }     // FxCk = numParams
            public string ProgramName { get; set; }    // 28
            public float[] Parameters { get; set; }    // FxCk = float[numParameters]
        };

        // For Bank (.fxb) without chunk (magic = 'FxBk')
        public class FxSet : FxContent
        {
            public int NumPrograms { get; set; }
            public string Future { get; set; }         // 128
            public FxProgram[] Programs { get; set; }
        }

        /*
        // Preset (Program) (.fxp) with chunk (magic = 'FPCh')
        typedef struct
        {
            char chunkMagic[4];     // 'CcnK'
            long byteSize;          // of this chunk, excl. magic + byteSize
            char fxMagic[4];        // 'FPCh'
            long version;
            char fxID[4];           // fx unique id
            long fxVersion;
            long numPrograms;
            char name[28];
            long chunkSize;
            unsigned char chunkData[chunkSize];
        } fxProgramSet;

        // Bank (.fxb) with chunk (magic = 'FBCh')
        typedef struct
        {
            char chunkMagic[4];     // 'CcnK'
            long byteSize;          // of this chunk, excl. magic + byteSize
            char fxMagic[4];        // 'FBCh'
            long version;
            char fxID[4];           // fx unique id
            long fxVersion;
            long numPrograms;
            char future[128];
            long chunkSize;
            unsigned char chunkData[chunkSize];
        } fxChunkSet;

        // For Preset (Program) (.fxp) without chunk (magic = 'FxCk')
        typedef struct {
            char chunkMagic[4];     // 'CcnK'
            long byteSize;          // of this chunk, excl. magic + byteSize
            char fxMagic[4];        // 'FxCk'
            long version;
            char fxID[4];           // fx unique id
            long fxVersion;
            long numParams;
            char prgName[28];
            float params[numParams];        // variable no. of parameters
        } fxProgram;

        // For Bank (.fxb) without chunk (magic = 'FxBk')
        typedef struct {
            char chunkMagic[4];     // 'CcnK'
            long byteSize;          // of this chunk, excl. magic + byteSize
            char fxMagic[4];        // 'FxBk'
            long version;
            char fxID[4];           // fx unique id
            long fxVersion;
            long numPrograms;
            char future[128];
            fxProgram programs[numPrograms];  // variable no. of programs
        } fxSet;
         */

        // default constructor
        public FXP()
        {

        }

        // constructor with byte array
        public FXP(byte[] values)
        {
            BinaryFile bf = new BinaryFile(values, BinaryFile.ByteOrder.BigEndian, Encoding.ASCII);
            Content = ReadFXP(bf);
        }

        public void WriteFile(string filePath)
        {
            BinaryFile bf = new BinaryFile(filePath, BinaryFile.ByteOrder.BigEndian, true, Encoding.ASCII);
            WriteFXP(bf);
            bf.Close();
        }

        public void WriteFXP(BinaryFile bf)
        {
            if (Content == null)
            {
                Console.Error.WriteLine("Error writing file. Missing preset content.");
                return;
            }

            // determine if the chunkdata is saved as XML
            bool writeXMLChunkData = false;
            string xmlChunkData = "";
            if (XmlDocument != null)
            {
                StringWriter stringWriter = new StringWriter();
                XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);
                XmlDocument.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                xmlChunkData = stringWriter.ToString().Replace("'", "&apos;");
                writeXMLChunkData = true;

                if (Content is FxProgramSet)
                {
                    ((FxProgramSet)Content).ChunkSize = xmlChunkData.Length;
                }
                else if (Content is FxChunkSet)
                {
                    ((FxChunkSet)Content).ChunkSize = xmlChunkData.Length;
                }
            }

            if (Content.ChunkMagic != "CcnK")
            {
                Console.Out.WriteLine("Cannot save the preset file. Missing preset header information.");
                return;
            }

            bf.Write(Content.ChunkMagic);                           // chunkMagic, 4

            // check what preset type we are saving
            if (Content.FxMagic == "FBCh")
            {
                var chunkSet = (FxChunkSet)Content;

                // Bank with Chunk Data
                chunkSet.ByteSize = 152 + chunkSet.ChunkSize;

                bf.Write(chunkSet.ByteSize);                         // byteSize = 4
                bf.Write(chunkSet.FxMagic);                          // fxMagic, 4
                bf.Write(chunkSet.Version);                          // version, 4
                bf.Write(chunkSet.FxID);                             // fxID, 4
                bf.Write(chunkSet.FxVersion);                        // fxVersion, 4
                bf.Write(chunkSet.NumPrograms);                      // numPrograms, 4
                bf.Write(chunkSet.Future, 128);                      // future, 128
                bf.Write(chunkSet.ChunkSize);                        // chunkSize, 4

                if (writeXMLChunkData)
                {
                    bf.Write(xmlChunkData);                          // chunkData, <chunkSize>
                }
                else
                {
                    // Even though the main FXP is BigEndian format the preset chunk is saved in LittleEndian format
                    bf.Write(chunkSet.ChunkDataByteArray, BinaryFile.ByteOrder.LittleEndian);
                }
            }
            else if (Content.FxMagic == "FPCh")
            {
                var programSet = (FxProgramSet)Content;

                // Preset with Chunk Data
                programSet.ByteSize = 52 + programSet.ChunkSize;

                bf.Write(programSet.ByteSize);                         // byteSize = 4
                bf.Write(programSet.FxMagic);                          // fxMagic, 4
                bf.Write(programSet.Version);                          // version, 4
                bf.Write(programSet.FxID);                             // fxID, 4
                bf.Write(programSet.FxVersion);                        // fxVersion, 4
                bf.Write(programSet.NumPrograms);                      // numPrograms, 4
                bf.Write(programSet.Name, 28);                         // name, 28
                bf.Write(programSet.ChunkSize);                        // chunkSize, 4

                if (writeXMLChunkData)
                {
                    bf.Write(xmlChunkData);                            // chunkData, <chunkSize>
                }
                else
                {
                    // Even though the main FXP is BigEndian format the preset chunk is saved in LittleEndian format
                    bf.Write(programSet.ChunkDataByteArray, BinaryFile.ByteOrder.LittleEndian);
                }
            }
            else if (Content.FxMagic == "FxCk")
            {
                // For Preset (Program) (.fxp) without chunk (magic = 'FxCk')
                // Bank with Chunk Data
                var program = (FxProgram)Content;

                program.ByteSize = 48 + (4 * program.NumParameters);

                bf.Write(program.ByteSize);                         // byteSize = 4
                bf.Write(program.FxMagic);                          // fxMagic, 4
                bf.Write(program.Version);                          // version, 4
                bf.Write(program.FxID);                             // fxID, 4
                bf.Write(program.FxVersion);                        // fxVersion, 4
                bf.Write(program.NumParameters);                    // numParameters, 4
                bf.Write(program.ProgramName, 28);                  // name, 28

                // variable no. of parameters
                for (int i = 0; i < program.NumParameters; i++)
                {
                    bf.Write((float)program.Parameters[i]);
                }
            }
        }

        public void ReadFile(string filePath)
        {
            BinaryFile bf = new BinaryFile(filePath, BinaryFile.ByteOrder.BigEndian, false, Encoding.ASCII);
            Content = ReadFXP(bf);
        }

        public FxContent ReadFXP(BinaryFile bf)
        {
            string ChunkMagic = bf.ReadString(4);
            if (ChunkMagic != "CcnK")
            {
                Console.Error.WriteLine("Error reading file. Missing preset header information.");
            }

            int ByteSize = bf.ReadInt32();
            string FxMagic = bf.ReadString(4);

            if (FxMagic == "FBCh")
            {
                // Bank with Chunk Data
                var chunkSet = new FxChunkSet();
                chunkSet.ChunkMagic = ChunkMagic;
                chunkSet.ByteSize = ByteSize;
                chunkSet.FxMagic = FxMagic;

                chunkSet.Version = bf.ReadInt32();
                chunkSet.FxID = bf.ReadString(4);
                chunkSet.FxVersion = bf.ReadInt32();

                chunkSet.NumPrograms = bf.ReadInt32();
                chunkSet.Future = bf.ReadString(128).TrimEnd('\0');
                chunkSet.ChunkSize = bf.ReadInt32();

                // Even though the main FXP is BigEndian format the preset chunk is saved in LittleEndian format
                chunkSet.ChunkDataByteArray = bf.ReadBytes(0, chunkSet.ChunkSize, BinaryFile.ByteOrder.LittleEndian);
                chunkSet.ChunkData = BinaryFile.ByteArrayToString(chunkSet.ChunkDataByteArray);

                // read the xml chunk into memory
                XmlDocument = new XmlDocument();
                try
                {
                    if (chunkSet.ChunkData != null) XmlDocument.LoadXml(chunkSet.ChunkData);
                }
                catch (XmlException)
                {
                    //Console.Out.WriteLine("No XML found");
                }

                Content = chunkSet;
            }
            else if (FxMagic == "FPCh")
            {
                // Preset with Chunk Data
                var programSet = new FxProgramSet();
                programSet.ChunkMagic = ChunkMagic;
                programSet.ByteSize = ByteSize;
                programSet.FxMagic = FxMagic;

                programSet.Version = bf.ReadInt32();
                programSet.FxID = bf.ReadString(4);
                programSet.FxVersion = bf.ReadInt32();

                programSet.NumPrograms = bf.ReadInt32();
                programSet.Name = bf.ReadString(28).TrimEnd('\0');
                programSet.ChunkSize = bf.ReadInt32();

                // Even though the main FXP is BigEndian format the preset chunk is saved in LittleEndian format
                programSet.ChunkDataByteArray = bf.ReadBytes(0, programSet.ChunkSize, BinaryFile.ByteOrder.LittleEndian);
                programSet.ChunkData = BinaryFile.ByteArrayToString(programSet.ChunkDataByteArray);

                // read the xml chunk into memory
                XmlDocument = new XmlDocument();
                try
                {
                    if (programSet.ChunkData != null) XmlDocument.LoadXml(programSet.ChunkData);
                }
                catch (XmlException)
                {
                    //Console.Out.WriteLine("No XML found");
                }

                Content = programSet;
            }
            else if (FxMagic == "FxCk")
            {
                // For Preset (Program) (.fxp) without chunk (magic = 'FxCk')        
                var program = new FxProgram();
                program.ChunkMagic = ChunkMagic;
                program.ByteSize = ByteSize;
                program.FxMagic = FxMagic;

                program.Version = bf.ReadInt32();
                program.FxID = bf.ReadString(4);
                program.FxVersion = bf.ReadInt32();

                program.NumParameters = bf.ReadInt32();
                program.ProgramName = bf.ReadString(28).TrimEnd('\0');

                // variable no. of parameters
                program.Parameters = new float[program.NumParameters];
                for (int i = 0; i < program.NumParameters; i++)
                {
                    program.Parameters[i] = bf.ReadSingle();
                }

                Content = program;
            }
            else if (FxMagic == "FxBk")
            {
                // For bank (.fxb) without chunk (magic = 'FxBk')        
                var set = new FxSet();
                set.ChunkMagic = ChunkMagic;
                set.ByteSize = ByteSize;
                set.FxMagic = FxMagic;

                set.Version = bf.ReadInt32();
                set.FxID = bf.ReadString(4);
                set.FxVersion = bf.ReadInt32();

                set.NumPrograms = bf.ReadInt32();
                set.Future = bf.ReadString(128).TrimEnd('\0');

                // variable no. of programs
                set.Programs = new FxProgram[set.NumPrograms];
                for (int p = 0; p < set.NumPrograms; p++)
                {
                    var content = ReadFXP(bf);
                    if (content is FxProgram)
                    {
                        set.Programs[p] = (FxProgram)content;
                    }
                }

                Content = set;
            }

            bf.Close();

            return Content;
        }
    }
}