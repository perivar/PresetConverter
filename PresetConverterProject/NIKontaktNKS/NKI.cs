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
        public static void Parse(string file, string outputDirectoryPath, bool doList, bool doVerbose)
        {
            using (BinaryFile bf = new BinaryFile(file, BinaryFile.ByteOrder.LittleEndian, false))
            {
                UInt32 fileSize = bf.ReadUInt32();
                Log.Debug("fileSize: " + fileSize);

                bf.Seek(350, SeekOrigin.Begin);
                int snpidCount = bf.ReadInt32();
                string snpid = bf.ReadString(snpidCount * 2, Encoding.Unicode);

                // snpid cannot have more than 4 characters (?!)
                if (snpidCount > 4)
                {
                    snpidCount = 0;
                    snpid = "";
                    bf.Seek(355, SeekOrigin.Begin);
                }
                else
                {
                    bf.ReadBytes(25);
                }
                Log.Debug("snpid: " + snpid);

                int versionCount = bf.ReadInt32();
                string version = bf.ReadString(versionCount * 2, Encoding.Unicode);
                Log.Debug("version: " + version);

                bf.ReadBytes(122);
                int presetNameCount = bf.ReadInt32();
                string presetName = bf.ReadString(presetNameCount * 2, Encoding.Unicode);
                int presetNameRest = bf.ReadInt32();
                Log.Debug("presetName: " + presetName);

                int companyNameCount = bf.ReadInt32();
                string companyName = bf.ReadString(companyNameCount * 2, Encoding.Unicode);
                int companyNameRest = bf.ReadInt32();
                Log.Debug("companyName: " + companyName);

                bf.ReadBytes(40);

                int libraryNameCount = bf.ReadInt32();
                string libraryName = bf.ReadString(libraryNameCount * 2, Encoding.Unicode);
                int libraryNameRest = bf.ReadInt32();
                Log.Debug("libraryName: " + libraryName);

                int typeCount = bf.ReadInt32();
                if (typeCount != 0)
                {
                    string type = bf.ReadString(typeCount * 2, Encoding.Unicode);
                    int typeRest = bf.ReadInt32();
                    Log.Debug("type: " + type);
                }

                int number = bf.ReadInt32();

                for (int i = 0; i < number * 2; i++)
                {
                    int sCount = bf.ReadInt32();
                    string s = bf.ReadString(sCount * 2, Encoding.Unicode);
                    Log.Debug(s);
                }

                bf.ReadBytes(249);

                UInt32 chunkSize = bf.ReadUInt32();
                Log.Debug("chunkSize: " + chunkSize);

                string outputFileName = Path.GetFileNameWithoutExtension(file);
                string outputFilePath = Path.Combine(outputDirectoryPath, "NKI_CONTENT", outputFileName + ".bin");
                IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "NKI_CONTENT"));

                var nks = new Nks();
                nks.BinaryFile = bf;
                nks.SetKeys = new Dictionary<String, NksSetKey>();

                NksEncryptedFileHeader header = new NksEncryptedFileHeader();

                header.SetId = snpid.ToUpper();
                header.KeyIndex = 0x100;
                header.Size = chunkSize;

                BinaryFile outBinaryFile = new BinaryFile(outputFilePath, BinaryFile.ByteOrder.LittleEndian, true);

                if (snpid == "")
                {
                    if (!doList) NKS.ExtractFileEntryToBf(nks, header, outBinaryFile);
                }
                else
                {
                    if (!doList) NKS.ExtractEncryptedFileEntryToBf(nks, header, outBinaryFile);
                }

                outBinaryFile.Close();
            }
        }
    }
}