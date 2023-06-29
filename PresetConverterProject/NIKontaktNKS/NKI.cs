using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CommonUtils;
using Serilog;

namespace PresetConverterProject.NIKontaktNKS
{
    public static class NKI
    {
        public static void Unpack(string inputFilePath, string outputDirectoryPath, bool doList, bool doVerbose)
        {
            using (BinaryFile bf = new BinaryFile(inputFilePath, BinaryFile.ByteOrder.LittleEndian, false))
            {
                // check if this is a nki monolith first
                // should start with /\ NI FC MTD  /\
                if (NI.TryReadNIResources(inputFilePath, outputDirectoryPath, bf, doList, doVerbose))
                {
                    Log.Information(inputFilePath + ": Succesfully parsed NI Resources.");
                }
                else
                {
                    bf.Seek(0, SeekOrigin.Begin);

                    UInt32 fileSize = bf.ReadUInt32();
                    Log.Information("File Size: " + fileSize);

                    UInt32 unknown01 = bf.ReadUInt32();
                    if (doVerbose) Log.Debug("unknown01: " + unknown01);

                    UInt32 unknown02 = bf.ReadUInt32();
                    if (doVerbose) Log.Debug("unknown02: " + unknown02);

                    var unknown03 = bf.ReadString(4);
                    if (doVerbose) Log.Debug("unknown03: " + unknown03);

                    UInt32 unknown04 = bf.ReadUInt32();
                    if (doVerbose) Log.Debug("unknown04: " + unknown04);

                    UInt32 unknown05 = bf.ReadUInt32();
                    if (doVerbose) Log.Debug("unknown05: " + unknown05);

                    bf.Seek(350, SeekOrigin.Begin);
                    int snpidNumChars = bf.ReadInt32();
                    string snpid = bf.ReadString(snpidNumChars * 2, Encoding.Unicode);

                    // snpid cannot have more than 4 characters (?!)
                    if (snpidNumChars > 4)
                    {
                        snpidNumChars = 0;
                        snpid = "";
                        bf.Seek(355, SeekOrigin.Begin);
                    }
                    else
                    {
                        bf.ReadBytes(25);
                    }
                    Log.Information("SNPID: " + snpid);

                    int versionNumChars = bf.ReadInt32();
                    if (doVerbose) Log.Debug("versionNumChars: " + versionNumChars);
                    string version = bf.ReadString(versionNumChars * 2, Encoding.Unicode);
                    Log.Information("Version: " + version);

                    bf.ReadBytes(122);

                    int presetNameNumChars = bf.ReadInt32();
                    if (doVerbose) Log.Debug("presetNameNumChars: " + presetNameNumChars);
                    string presetName = bf.ReadString(presetNameNumChars * 2, Encoding.Unicode);
                    Log.Information("Preset Name: " + presetName);
                    int presetNameRest = bf.ReadInt32();
                    if (doVerbose) Log.Debug("presetNameRest: " + presetNameRest);

                    int companyNameNumChars = bf.ReadInt32();
                    if (doVerbose) Log.Debug("companyNameNumChars: " + companyNameNumChars);
                    string companyName = bf.ReadString(companyNameNumChars * 2, Encoding.Unicode);
                    Log.Information("Company Name: " + companyName);
                    int companyNameRest = bf.ReadInt32();
                    if (doVerbose) Log.Debug("companyNameRest: " + companyNameRest);

                    bf.ReadBytes(40);

                    int libraryNameNumChars = bf.ReadInt32();
                    if (doVerbose) Log.Debug("libraryNameNumChars: " + libraryNameNumChars);
                    string libraryName = bf.ReadString(libraryNameNumChars * 2, Encoding.Unicode);
                    Log.Information("Library Name: " + libraryName);
                    int libraryNameRest = bf.ReadInt32();
                    if (doVerbose) Log.Debug("libraryNameRest: " + libraryNameRest);

                    int typeNumChars = bf.ReadInt32();
                    if (doVerbose) Log.Debug("typeNumChars: " + typeNumChars);
                    if (typeNumChars != 0)
                    {
                        string type = bf.ReadString(typeNumChars * 2, Encoding.Unicode);
                        Log.Information("Type: " + type);
                        int typeRest = bf.ReadInt32();
                        if (doVerbose) Log.Debug("typeRest: " + typeRest);
                    }

                    int paramCount = bf.ReadInt32();
                    if (doVerbose) Log.Debug("paramCount: " + paramCount);
                    for (int i = 0; i < paramCount * 2; i++)
                    {
                        int paramNumChars = bf.ReadInt32();
                        if (doVerbose) Log.Debug("paramNumChars: " + paramNumChars);
                        string paramName = bf.ReadString(paramNumChars * 2, Encoding.Unicode);
                        if (doVerbose) Log.Debug(paramName);
                    }

                    bf.ReadBytes(249);

                    UInt32 chunkSize = bf.ReadUInt32();
                    if (doVerbose) Log.Debug("chunkSize: " + chunkSize);

                    Log.Information(String.Format("Resource '{0}' @ position {1} [{2} bytes]", "NKI_CONTENT", bf.Position, chunkSize));
                    var byteArray = bf.ReadBytes((int)chunkSize);

                    UInt32 unknown06 = bf.ReadUInt32();
                    if (doVerbose) Log.Debug("unknown06: " + unknown06);

                    UInt32 unknown07 = bf.ReadUInt32();
                    if (doVerbose) Log.Debug("unknown07: " + unknown07);

                    UInt32 unknown08 = bf.ReadUInt32();
                    if (doVerbose) Log.Debug("unknown08: " + unknown08);
                    bf.Close();

                    // read content
                    var bFileContent = new BinaryFile(byteArray, BinaryFile.ByteOrder.LittleEndian, Encoding.ASCII);

                    string outputFileName = Path.GetFileNameWithoutExtension(inputFilePath);
                    string outputFilePath = Path.Combine(outputDirectoryPath, "NKI_CONTENT", outputFileName + ".bin");
                    IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "NKI_CONTENT"));

                    var nks = new Nks();
                    nks.BinaryFile = bFileContent;
                    nks.SetKeys = new Dictionary<String, NksSetKey>();

                    NksEncryptedFileHeader header = new NksEncryptedFileHeader();

                    header.SetId = snpid.ToUpper();
                    header.KeyIndex = 0x100;
                    header.Size = chunkSize;

                    BinaryFile outBinaryFile = new BinaryFile(outputFilePath, BinaryFile.ByteOrder.LittleEndian, true);

                    if (snpid == "")
                    {
                        Log.Information("Library is not encrypted.");
                        Log.Information("Trying to extract.");
                        if (!doList) NKS.ExtractFileEntryToBf(nks, header, outBinaryFile);
                    }
                    else
                    {
                        Log.Information(string.Format("Library is encrypted using snpid: {0}", snpid));
                        Log.Information("Trying to extract encrypted file.");
                        if (!doList) NKS.ExtractEncryptedFileEntryToBf(nks, header, outBinaryFile);
                    }

                    outBinaryFile.Close();
                }
            }
        }
    }
}