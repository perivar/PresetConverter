using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Win32;

using CommonUtils;
using System.Text.RegularExpressions;
using System.Text;

namespace PresetConverterProject.NIKontaktNKS
{
    public class NKS
    {
        public const string REG_PATH = "Software\\Native Instruments";
        public const string SETTINGS_PATH = "Settings.cfg";

        public const UInt32 NKS_MAGIC_DIRECTORY = 0x5e70ac54;
        public const UInt32 NKS_MAGIC_ENCRYPTED_FILE = 0x16ccf80a;
        public const UInt32 NKS_MAGIC_FILE = 0x4916e63c;
        public const UInt32 NKS_MAGIC_CONTENT_FILE = 0x2AE905FA; // like tga, txt, xml, png, cache       

        public static void NksReadLibrariesInfo()
        {
            // read in all libraries
            var regList = NksGetRegistryLibraries();
            if (regList != null)
            {
                foreach (var regEntry in regList)
                {
                    // ignore if duplicates are silently eliminated
                    NKSLibraries.Libraries[regEntry.Id] = regEntry;
                }
            }
            var settingsList = NksGetSettingsLibraries();
            if (settingsList != null)
            {
                foreach (var settingsEntry in settingsList)
                {
                    // ignore if duplicates are silently eliminated
                    NKSLibraries.Libraries[settingsEntry.Id] = settingsEntry;
                }
            }
        }

        #region Read Library Descriptors from Settings.cfg
        public static void PrintSettingsLibraryInfo(TextWriter writer)
        {
            var list = NKS.NksGetSettingsLibraries();

            foreach (NksLibraryDesc entry in list)
            {
                var id = entry.Id;
                var name = entry.Name;
                var keyHex = StringUtils.ToHexEditorString(entry.GenKey.Key);
                var ivHEx = StringUtils.ToHexEditorString(entry.GenKey.IV);

                writer.WriteLine("Id: {0}\nName: {1}\nKey: {2}IV: {3}", id, name, keyHex, ivHEx);
            }
        }

        private static List<NksLibraryDesc> NksGetSettingsLibraries()
        {
            Regex sectionRegex = new Regex(@"\[([\w\d\s]+)\]");
            Regex elementRegex = new Regex(@"(.*?)=sz\:(.*?)$");

            var keyElements = new List<string>();
            keyElements.Add("Name");
            keyElements.Add("SNPID");
            keyElements.Add("Company");
            keyElements.Add("ContentDir");
            keyElements.Add("JDX");
            keyElements.Add("HU");

            List<NksLibraryDesc> settingsList = null;

            using (var reader = new StreamReader(SETTINGS_PATH))
            {
                string line = null;
                string sectionName = null;
                bool isProcessingSection = false;
                NksLibraryDesc libDesc = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isProcessingSection)
                    {
                        Match elementMatch = elementRegex.Match(line);

                        // found new section
                        if (elementMatch.Success)
                        {
                            string key = elementMatch.Groups[1].Value;
                            string value = elementMatch.Groups[2].Value;

                            if (keyElements.Contains(key))
                            {
                                if (libDesc == null) libDesc = new NksLibraryDesc();

                                switch (key)
                                {
                                    case "Name":
                                        libDesc.Name = value.ToUpper();
                                        break;
                                    case "SNPID":
                                        uint id = 0;
                                        UInt32.TryParse(value, out id);

                                        if (id != 0)
                                        {
                                            libDesc.Id = id;

                                        }
                                        else
                                        {
                                            try
                                            {
                                                // is it hex?
                                                id = Convert.ToUInt32(value, 16);
                                                libDesc.Id = id;
                                            }
                                            catch (System.Exception)
                                            {
                                                // ignore invalid (?) ids
                                            }
                                        }
                                        break;
                                    case "Company":
                                        break;
                                    case "ContentDir":
                                        break;
                                    case "JDX":
                                        NksGeneratingKeySetKeyStr(libDesc.GenKey, value);
                                        break;
                                    case "HU":
                                        NksGeneratingKeySetIvStr(libDesc.GenKey, value);
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException("Key not supported: " + key);
                                }
                            }
                        }
                    }

                    Match sectionMatch = sectionRegex.Match(line);

                    // found new section
                    if (sectionMatch.Success)
                    {
                        sectionName = sectionMatch.Groups[1].Value;
                        isProcessingSection = true;

                        // store previously finished libDesc if found new section
                        if (libDesc != null
                        && libDesc.Id != 0
                        && libDesc.GenKey.KeyLength != 0 && libDesc.GenKey.IVLength != 0)
                        {
                            if (settingsList == null) settingsList = new List<NksLibraryDesc>();
                            settingsList.Add(libDesc);

                            libDesc = null;
                        }
                    }
                }
            }

            return settingsList;
        }

        #endregion

        #region Read Library Descriptors from Registry
        public static void PrintRegistryLibraryInfo(TextWriter writer)
        {
            var list = NKS.NksGetRegistryLibraries();
            foreach (NksLibraryDesc entry in list)
            {
                var id = entry.Id;
                var name = entry.Name;
                var keyHex = StringUtils.ToHexEditorString(entry.GenKey.Key);
                var ivHEx = StringUtils.ToHexEditorString(entry.GenKey.IV);

                writer.WriteLine("Id: {0}\nName: {1}\nKey: {2}IV: {3}", id, name, keyHex, ivHEx);
            }
        }

        private static List<NksLibraryDesc> NksGetRegistryLibraries()
        {
            NksLibraryDesc ld = null;
            try
            {
                var regPath = string.Format("{0}\\{1}", REG_PATH, "Content");
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regPath))
                {
                    if (key != null)
                    {
                        var regList = new List<NksLibraryDesc>();

                        var subKeys = key.GetValueNames(); // values in current reg folder
                        // var subkeys = key.GetSubKeyNames(); // sub folders

                        foreach (var subKey in subKeys)
                        {
                            ld = CreateLibraryDesc(subKey, key.GetValue(subKey).ToString());
                            if (ld == null)
                                continue;

                            regList.Add(ld);
                        }

                        return regList;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to open HKLM\\" + REG_PATH + "\\Content", ex);
            }

            return null;
        }

        private static NksLibraryDesc CreateLibraryDesc(string keyName, string name)
        {
            NksLibraryDesc libDesc = null;
            uint id = 0;

            Regex re = new Regex(@"k2lib(\d+)");
            Match m = re.Match(keyName);

            if (m.Success)
            {
                string sId = m.Groups[1].Value;
                id = UInt32.Parse(sId);
            }
            else
            {
                // throw new KeyNotFoundException("Unexpected library key" + keyName);
                return libDesc;
            }

            string keyPath = string.Format("{0}\\{1}", REG_PATH, name);
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        libDesc = new NksLibraryDesc();
                        libDesc.Id = id;
                        libDesc.Name = name.ToUpper();

                        var jdx = key.GetValue("JDX");
                        if (jdx != null)
                        {
                            NksGeneratingKeySetKeyStr(libDesc.GenKey, jdx.ToString());
                        }

                        var hu = key.GetValue("HU");
                        if (hu != null)
                        {
                            NksGeneratingKeySetIvStr(libDesc.GenKey, hu.ToString());
                        }

                        if (libDesc.GenKey.KeyLength == 0 || libDesc.GenKey.IVLength == 0)
                        {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to open HKLM\\" + keyPath, ex);
            }

            return libDesc;
        }

        private static bool NksGeneratingKeySetKeyStr(NksGeneratingKey generatingKey, string key)
        {
            byte[] data = null;
            int len = 0;
            if (!HexStringToBinary(key, out data, out len))
                return false;

            if (len != 16 && len != 24 && len != 32)
            {
                return false;
            }

            generatingKey.Key = data;
            return true;
        }

        private static bool NksGeneratingKeySetIvStr(NksGeneratingKey generatingKey, string key)
        {
            byte[] data = null;
            int len = 0;
            if (!HexStringToBinary(key, out data, out len))
                return false;

            if (len != 16 && len != 24 && len != 32)
            {
                return false;
            }

            generatingKey.IV = data;
            return true;
        }

        private static bool HexStringToBinary(string str, out byte[] returnData, out int returnLength)
        {
            int len = str.Length;

            if ((len % 2) != 0)
            {
                returnData = null;
                returnLength = 0;
                return false;
            }

            byte[] data = StringUtils.HexStringToByteArray(str);

            returnData = data;
            returnLength = data.Length;

            return true;
        }
        #endregion

        #region List, traverse and extract methods     

        /// <summary>Opens an archive. This must be called first, before anything else can be done
        /// with archives.</summary>
        /// <param name="fileName">name of the file to open</param>
        /// <param name="nks">Nks object, which will be initialized upon
        /// success.</param>
        /// <returns>0 on success</returns>
        private static int NksOpen(string fileName, Nks nks)
        {
            BinaryFile bf = new BinaryFile(fileName, BinaryFile.ByteOrder.LittleEndian, false);

            int r = NksOpenBf(bf, nks);
            if (r != 0)
            {
                bf.Close();
                return r;
            }

            return 0;
        }

        private static int NksOpenBf(BinaryFile bf, Nks nks)
        {
            if (bf == null)
                throw new ArgumentNullException("BinaryFile cannot be null");

            nks.RootEntry.Name = "/";
            nks.RootEntry.Type = NksEntryType.NKS_ENT_DIRECTORY;
            nks.RootEntry.Offset = 0;
            nks.BinaryFile = bf;
            nks.SetKeys = new Dictionary<UInt32, NksSetKey>();

            return 0;
        }

        public static void TraverseDirectory(string fileName, string prefix)
        {
            Nks nks = new Nks();
            NksEntry rootEntry = new NksEntry();
            rootEntry.Name = "";
            rootEntry.Offset = 0;
            rootEntry.Type = NksEntryType.NKS_ENT_DIRECTORY;

            int r = NksOpen(fileName, nks);

            bool isSuccessfull = !TraverseDirectory(nks, rootEntry, prefix);
        }

        private static bool TraverseDirectory(Nks nks, NksEntry dirEntry, string prefix)
        {
            var list = new ArrayList();
            bool isSuccessfull = true;
            int r = NksListDirEntry(nks, list, dirEntry);
            if (r != 0)
            {
                isSuccessfull = false;
            }

            if (!TraverseDirectories(nks, list, prefix))
                isSuccessfull = false;

            // decrypt and extract files
            if (!TraverseFiles(nks, list, prefix))
                isSuccessfull = false;

            list.Clear();
            return isSuccessfull;
        }

        private static bool TraverseDirectories(Nks nks, IList list, string prefix)
        {
            if (list == null)
                return true;

            bool isSuccessfull = true;

            foreach (NksEntry entry in list)
            {
                if (entry.Type != NksEntryType.NKS_ENT_DIRECTORY)
                    continue;

                string prefix_buffer = Path.Join(prefix, entry.Name);

                if (!TraverseDirectory(nks, entry, prefix_buffer))
                    isSuccessfull = false;
            }

            return isSuccessfull;
        }

        private static bool TraverseFiles(Nks nks, IList list, string prefix)
        {
            if (list == null)
                return true;

            foreach (NksEntry entry in list)
            {
                Console.WriteLine("Traversing file: {0, -30}{1}", prefix, entry);

                if (entry.Type == NksEntryType.NKS_ENT_DIRECTORY)
                    continue;

                if (!TraverseFile(nks, entry, prefix))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TraverseFile(Nks nks, NksEntry fileEntry, string prefix)
        {
            bool isSuccessfull = true;
            int extractedCount = 0;

            string outFile = Path.Join(prefix, fileEntry.Name);

            switch (fileEntry.Type)
            {
                case NksEntryType.NKS_ENT_UNKNOWN:
                case NksEntryType.NKS_ENT_FILE:

                    int r = NksExtractFileEntry(nks, fileEntry, outFile);

                    if (r == 0)
                    {
                        extractedCount++;
                    }

                    isSuccessfull = (r == 0);
                    break;

                default:
                    break;
            }

            return isSuccessfull;
        }

        /// <summary>Lists the contents of a directory in an archive. It is similar to nks_list,
        /// but uses a NksEntry instead of a path.The entry must correspond to a
        /// directory and not a file..
        /// </summary>
        private static int NksListDirEntry(Nks nks, IList list, NksEntry entry)
        {
            NksDirectoryHeader header = new NksDirectoryHeader();
            int r;

            if (entry.Type != NksEntryType.NKS_ENT_DIRECTORY)
                throw new ArgumentException("Type is not a directory");

            if (nks.BinaryFile.Seek(entry.Offset, SeekOrigin.Begin) < 0)
                throw new IOException("Failed reading from stream");

            r = NksReadDirectoryHeader(nks.BinaryFile, header);
            if (r != 0)
                return r;

            return ListDirectory(nks, list, header);
        }

        private static int ListDirectory(Nks nks, IList list, NksDirectoryHeader header)
        {
            long offset = nks.BinaryFile.Seek(0, SeekOrigin.Current);
            if (offset < 0)
                throw new IOException("Failed reading from stream");

            int r = 0;
            for (int n = 0; n < header.EntryCount; n++)
            {
                if (nks.BinaryFile.Seek(offset, SeekOrigin.Begin) < 0)
                    throw new IOException("Failed reading from stream");

                NksEntry entry = new NksEntry();

                switch (header.Version)
                {
                    case 0x0100:
                        r = NksRead0100NksEntry(nks.BinaryFile, header, entry);
                        break;

                    case 0x0110:
                        r = NksRead0110NksEntry(nks.BinaryFile, header, entry);
                        break;

                    default:
                        throw new NotSupportedException("Header version not supported " + header.Version);
                }

                if (r != 0)
                    return r;

                offset = nks.BinaryFile.Seek(0, SeekOrigin.Current);
                if (offset < 0)
                {
                    entry = null;
                    throw new IOException("Failed reading from stream");
                }

                // add to list    
                var f = list.Add(entry);
                if (f == -1)
                {
                    entry = null;
                    break;
                }

                entry = null;
            }

            return 0;
        }

        /// <summary>Returns the size of a file in an archive.
        /// </summary>
        /// <param name="nks">the archive</param>
        /// <param name="entry">the entry corresponding to a file in the archive</param>
        /// <returns>the size, or a negative value on error</returns>
        public static int NksFileSize(Nks nks, NksEntry entry)
        {
            NksEncryptedFileHeader header = new NksEncryptedFileHeader();
            int r;

            if (nks.BinaryFile.Seek(entry.Offset, SeekOrigin.Begin) < 0)
                throw new IOException("Failed reading from stream");

            UInt32 magic = nks.BinaryFile.ReadUInt32(); // read_u32_le

            switch (magic)
            {
                case NKS_MAGIC_ENCRYPTED_FILE:
                    break;

                case NKS_MAGIC_DIRECTORY:
                    throw new ArgumentException("Magic is a directory");

                default:
                    throw new NotSupportedException("Magic not supported " + magic);
            }

            if (nks.BinaryFile.Seek(entry.Offset, SeekOrigin.Begin) < 0)
                throw new IOException("Failed reading from stream");

            r = NksReadEncryptedFileHeader(nks.BinaryFile, header);
            if (r != 0)
                return r;

            return (int)header.Size;
        }

        /// <summary>Extracts a file from an archive.
        /// </summary>
        /// <param name="nks">the archive</param>
        /// <param name="entry">the entry corresponding to the file to extract in the archive</param>
        /// <param name="out_file">the name of the file to extract to. The file will be
        /// created.</param>
        /// <returns>0 on success</returns>
        private static int NksExtractFileEntry(Nks nks, NksEntry entry, string outFile)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(outFile);
                Directory.CreateDirectory(directoryPath);
            }
            catch (Exception)
            {
                // handle them here
            }

            BinaryFile bf = new BinaryFile(outFile, BinaryFile.ByteOrder.LittleEndian, true);

            int r = NksExtractFileEntryToBf(nks, entry, bf);
            bf.Close();

            return r;
        }

        /// <summary>Extracts a file from an archive. This function is similar to
        /// NksExtractEntry, but accepts an output BinaryFile instead of a file
        /// name.
        /// </summary>
        private static int NksExtractFileEntryToBf(Nks nks, NksEntry entry, BinaryFile outbinaryFile)
        {
            NksEncryptedFileHeader encHeader = new NksEncryptedFileHeader();
            NksFileHeader fileHeader = new NksFileHeader();
            int r;

            if (nks.BinaryFile.Seek(entry.Offset, SeekOrigin.Begin) < 0)
                throw new IOException("Failed reading from stream");

            UInt32 magic = nks.BinaryFile.ReadUInt32(); // read_u32_le
            switch (magic)
            {
                case NKS_MAGIC_ENCRYPTED_FILE:
                case NKS_MAGIC_FILE:
                case NKS_MAGIC_CONTENT_FILE: // like tga, txt, xml, png, cache
                    break;

                case NKS_MAGIC_DIRECTORY:
                    throw new NotSupportedException("Magic is a directory");

                default:
                    throw new NotSupportedException("Magic not supported " + magic);
            }

            if (nks.BinaryFile.Seek(entry.Offset, SeekOrigin.Begin) < 0)
                throw new IOException("Failed reading from stream");

            if (magic == NKS_MAGIC_ENCRYPTED_FILE)
            {
                r = NksReadEncryptedFileHeader(nks.BinaryFile, encHeader);
                if (r != 0)
                    return r;

                switch (encHeader.Version)
                {
                    case 0x0100:
                    case 0x0110:
                        return ExtractEncryptedFileEntryToBf(nks, encHeader, outbinaryFile);

                    default:
                        throw new NotSupportedException("Encrypted header version not supported " + encHeader.Version);
                }
            }
            else if (magic == NKS_MAGIC_CONTENT_FILE)
            {
                r = NksReadContentFileHeader(nks.BinaryFile, encHeader);
                if (r != 0)
                    return r;

                if (encHeader.SetId != 0)
                {
                    // likely encoded content
                    switch (encHeader.Version)
                    {
                        case 0x0100:
                        case 0x0110:
                            return ExtractEncryptedFileEntryToBf(nks, encHeader, outbinaryFile);

                        default:
                            throw new NotSupportedException("File header version not supported " + fileHeader.Version);
                    }
                }
                else
                {
                    // non encoded content
                    switch (encHeader.Version)
                    {
                        case 0x0100:
                        case 0x0110:
                            return ExtractFileEntryToBf(nks, encHeader, outbinaryFile);

                        default:
                            throw new NotSupportedException("Encrypted header version not supported " + encHeader.Version);
                    }
                }
            }
            else
            {
                r = NksReadFileHeader(nks.BinaryFile, fileHeader);
                if (r != 0)
                    return r;

                switch (fileHeader.Version)
                {
                    case 0x0100:
                    case 0x0110:
                        return ExtractFileEntryToBf(nks, fileHeader, outbinaryFile);

                    default:
                        throw new NotSupportedException("File header version not supported " + fileHeader.Version);
                }
            }
        }

        private static int ExtractEncryptedFileEntryToBf(Nks nks, NksEncryptedFileHeader header, BinaryFile outBinaryFile)
        {
            int bufferLength = 16384;
            int toRead;
            int size = (int)header.Size;

            int keyLength = 0;
            long keyPos = 0;
            byte[] key = null;

            if (header.KeyIndex < 0xff)
            {
                if (NksGet0100Key((int)header.KeyIndex, key, out keyLength) != 0)
                    throw new KeyNotFoundException("Could not find key");

                if (keyLength != 0x10) throw new InvalidDataException("Key is not 16 bytes but " + keyLength);

                keyPos = nks.BinaryFile.Seek(0, SeekOrigin.Current);
                if (keyPos < 0)
                    throw new IOException("Failed reading from stream");
            }
            else if (header.KeyIndex == 0x100)
            {
                NksSetKey setKey = nks.SetKeys.Where(a => a.Key == header.SetId).Select(a => a.Value).FirstOrDefault();

                if (setKey == null)
                {
                    NksLibraryDesc lib = NKSLibraries.Libraries.Where(a => a.Key == header.SetId).FirstOrDefault().Value;

                    if (lib == null)
                        throw new KeyNotFoundException("lib could not be found");

                    setKey = new NksSetKey();
                    setKey.SetId = header.SetId;
                    setKey.Data = NksCreate0110Key(lib.GenKey, setKey.Data.Length);
                    if (setKey.Data == null)
                    {
                        setKey = null;
                        throw new ArgumentNullException("Could not create 0110 Key.");
                    }

                    nks.SetKeys.Add(setKey.SetId, setKey);
                }

                key = setKey.Data;
                keyLength = setKey.Data.Length; // 0x10000
                keyPos = 0;
            }
            else
            {
                throw new KeyNotFoundException("Could not find key");
            }

            while (size > 0)
            {
                toRead = Math.Min(bufferLength, size);
                var buffer = nks.BinaryFile.ReadBytes(toRead);

                for (int x = 0; x < toRead; x++)
                {
                    keyPos %= keyLength;

                    // ^=	Bitwise exclusive OR and assignment operator.	
                    // C ^= 2 is same as C = C ^ 2
                    buffer[x] ^= key[keyPos];

                    keyPos++;
                }

                outBinaryFile.Write(buffer);

                size -= toRead;
            }

            return 0;
        }

        private static int ExtractFileEntryToBf(Nks nks, NksFileHeader header, BinaryFile outBinaryFile)
        {
            byte[] buffer = new byte[16384];
            int toRead;
            int size = (int)header.Size;

            while (size > 0)
            {
                toRead = Math.Min((int)buffer.Length, size);
                var readBytes = nks.BinaryFile.ReadBytes(toRead);

                outBinaryFile.Write(readBytes);

                size -= toRead;
            }

            return 0;
        }

        private static int ExtractFileEntryToBf(Nks nks, NksEncryptedFileHeader header, BinaryFile outBinaryFile)
        {
            byte[] buffer = new byte[16384];
            int toRead;
            int size = (int)header.Size;

            while (size > 0)
            {
                toRead = Math.Min((int)buffer.Length, size);
                var readBytes = nks.BinaryFile.ReadBytes(toRead);

                outBinaryFile.Write(readBytes);

                size -= toRead;
            }

            return 0;
        }

        #endregion

        #region Generate and Read Keys mehods
        private static byte[][] Nks0100Keys = MathUtils.CreateJaggedArray<byte[][]>(32, 16);
        private static byte[] Nks0110BaseKey;

        private static int NksGet0100Key(int keyIndex, byte[] key, out int length)
        {
            byte[] retKey;
            length = 0;
            int retLength = 0;

            if (keyIndex >= 0x20)
                throw new ArgumentException("Could not find key");

            if (Nks0100Keys[0][0] == 0)
                Generate0100Keys();

            retKey = Nks0100Keys[keyIndex];
            retLength = retKey.Length;

            if (key != null)
            {
                key = retKey;
                length = retLength;
            }

            return 0;
        }

        private static byte[] NksCreate0110Key(NksGeneratingKey gk, int len)
        {
            if (gk.Key == null)
                throw new ArgumentException("Missing key");

            if (gk.KeyLength != 16 && gk.KeyLength != 24 && gk.KeyLength != 32)
                throw new ArgumentException("Wrong key length");

            if (len < 16 || (len & 15) != 0)
                throw new ArgumentException("Wrong buffer length");

            if (gk.IV == null || gk.IVLength != 16)
                throw new ArgumentException("Missing iv");

            if (Nks0110BaseKey == null)
            {
                Generate0110BaseKey();

                // write base key to file
                // string outDumpFile = @"0110_base_key_port.bin";
                // using (BinaryFile bfdump = new BinaryFile(outDumpFile, BinaryFile.ByteOrder.LittleEndian, true))
                // {
                //     bfdump.Write(Nks0110BaseKey);
                // }
            }

            var cipher = new Aes(gk.Key, gk.IV);
            var ctr = gk.IV;
            var bkp = Nks0110BaseKey;

            var bufferList = new List<byte>();

            for (int n = 0; 16 * n < len; n++)
            {
                var bp = cipher.EncryptToByte(ctr);

                for (int m = 0; m < 16; m++)
                {
                    // ^=	Bitwise exclusive OR and assignment operator.	
                    // C ^= 2 is same as C = C ^ 2
                    bp[m] ^= bkp[16 * n + m];
                }

                // store within buffer
                bufferList.AddRange(bp);

                IncrementCounter(ctr);
            }

            return bufferList.ToArray();
        }

        private static void Generate0100Keys()
        {
            // Fill arrays with pseudo random bytes
            int seed = 0x6ee38fe0;
            for (int key = 0; key < 32; key++)
            {
                for (int n = 0; n < 16; n++)
                {
                    Nks0100Keys[key][n] = RandMs(ref seed);
                }
            }
        }

        // Thanks to Iñigo Quilez
        // see http://www.iquilezles.org/www/articles/sfrand/sfrand.htm
        // https://stackoverflow.com/questions/1026327/what-common-algorithms-are-used-for-cs-rand
        private static byte RandMs(ref int seed)
        {
            seed = seed * 0x343fd + 0x269ec3;
            return (byte)((seed >> 16) & 0xff);
        }

        private static void Generate0110BaseKey()
        {
            int seed;

            if (Nks0110BaseKey != null)
                return;

            Nks0110BaseKey = new byte[0x10000];

            // Fill array with pseudo random bytes
            seed = 0x608da0a2;
            for (int n = 0; n < 0x10000; n++)
            {
                Nks0110BaseKey[n] = RandMs(ref seed);
            }
        }

        public static void IncrementCounter(byte[] num)
        {
            for (int n = num.Length - 1; n > 0; n--)
            {
                if (++num[n] != 0)
                    break;
            }
        }
        #endregion

        #region Read file header methods
        private static int NksReadDirectoryHeader(BinaryFile bf, NksDirectoryHeader header)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(NKS_MAGIC_DIRECTORY)) // 0x5e70ac54
                throw new IOException("Magic not as expected (0x5e70ac54) but " + magic);

            header.Version = bf.ReadUInt16(); // read_u16_le

            header.SetId = bf.ReadUInt32(); // read_u32_le

            if ((header.Unknown0 = bf.ReadBytes(4)).Length != 4)
                throw new IOException("Failed reading from stream");

            header.EntryCount = bf.ReadUInt32(); // read_u32_le

            if ((header.Unknown1 = bf.ReadBytes(4)).Length != 4)
                throw new IOException("Failed reading from stream");

            switch (header.Version)
            {
                case 0x0100:
                case 0x0110:
                    break;

                default:
                    throw new ArgumentException("Header version not valid: " + header.Version);
            }

            return 0;
        }

        private static int NksRead0100EntryHeader(BinaryFile bf, Nks0100EntryHeader header)
        {
            header.Name = bf.ReadString(129);

            if ((header.Unknown = bf.ReadBytes(1)).Length != 1)
                throw new IOException("Failed reading from stream");

            header.Offset = bf.ReadUInt32(); // read_u32_le

            header.Type = bf.ReadUInt16(); // read_u16_le

            if (header.Type == (UInt16)NksTypeHint.NKS_TH_ENCRYPTED_FILE)
            {
                header.Offset = DecodeOffset(header.Offset);
            }

            return 0;
        }

        private static int NksRead0110EntryHeader(BinaryFile bf, Nks0110EntryHeader header)
        {
            if ((header.Unknown = bf.ReadBytes(2)).Length != 2)
                throw new IOException("Failed reading from stream");

            header.Offset = bf.ReadUInt32(); // read_u32_le

            header.Type = bf.ReadUInt16(); // read_u16_le

            header.Name = bf.ReadStringNull(Encoding.Unicode);

            if (header.Type == (UInt16)NksTypeHint.NKS_TH_ENCRYPTED_FILE)
            {
                header.Offset = DecodeOffset(header.Offset);
            }

            return 0;
        }

        private static int NksRead0100NksEntry(BinaryFile bf, NksDirectoryHeader dir, NksEntry entry)
        {
            Nks0100EntryHeader hdr = new Nks0100EntryHeader();

            int r = NksRead0100EntryHeader(bf, hdr);
            if (r != 0)
                return r;

            entry.Name = hdr.Name.ToUpper();
            entry.Offset = hdr.Offset;
            entry.Type = TypeHintToEntryType(hdr.Type);

            return 0;
        }

        private static int NksRead0110NksEntry(BinaryFile bf, NksDirectoryHeader dir, NksEntry entry)
        {
            Nks0110EntryHeader hdr = new Nks0110EntryHeader();

            int r = NksRead0110EntryHeader(bf, hdr);
            if (r != 0)
                return r;

            entry.Name = hdr.Name.ToUpper();
            entry.Offset = hdr.Offset;
            entry.Type = TypeHintToEntryType(hdr.Type);

            hdr = null;

            return 0;
        }

        private static int NksReadEncryptedFileHeader(BinaryFile bf, NksEncryptedFileHeader ret)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(NKS_MAGIC_ENCRYPTED_FILE)) // 0x16ccf80a
                throw new IOException("Magic not as expected (0x16ccf80a) but " + magic);

            ret.Version = bf.ReadUInt16(); // read_u16_le

            ret.SetId = bf.ReadUInt32(); // read_u32_le

            ret.KeyIndex = bf.ReadUInt32(); // read_u32_le

            if ((ret.Unknown1 = bf.ReadBytes(5)).Length != 5)
                throw new IOException("Failed reading from stream");

            ret.Size = bf.ReadUInt32(); // read_u32_le

            if ((ret.Unknown2 = bf.ReadBytes(8)).Length != 8)
                throw new IOException("Failed reading from stream");

            switch (ret.Version)
            {
                case 0x0100:
                case 0x0110:
                    break;

                default:
                    throw new NotSupportedException("Unexpected version number: " + ret.Version);
            }

            return 0;
        }

        private static int NksReadContentFileHeader(BinaryFile bf, NksEncryptedFileHeader ret)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != NKS_MAGIC_CONTENT_FILE) // 0x2AE905FA
                throw new IOException("Magic not as expected (0x2AE905FA) but " + magic);

            ret.Version = bf.ReadUInt16(); // read_u16_le

            ret.SetId = bf.ReadUInt32(); // read_u32_le

            ret.KeyIndex = bf.ReadUInt32(); // read_u32_le

            ret.Size = bf.ReadUInt32(); // read_u32_le

            if ((ret.Unknown2 = bf.ReadBytes(4)).Length != 4)
                throw new IOException("Failed reading from stream");

            return 0;
        }

        private static int NksReadFileHeader(BinaryFile bf, NksFileHeader ret)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(NKS_MAGIC_FILE)) // 0x4916e63c
                throw new IOException("Magic not as expected (0x4916e63c) but " + magic);

            ret.Version = bf.ReadUInt16(); // read_u16_le

            if ((ret.Unknown1 = bf.ReadBytes(13)).Length != 13)
                throw new IOException("Failed reading from stream");

            ret.Size = bf.ReadUInt32(); // read_u32_le

            if ((ret.Unknown2 = bf.ReadBytes(4)).Length != 4)
                throw new IOException("Failed reading from stream");

            return 0;
        }

        private static UInt32 DecodeOffset(UInt32 offset)
        {
            return (offset ^ (UInt32)(0x1f4e0c8d));
        }

        private static NksEntryType TypeHintToEntryType(UInt16 typeHint)
        {
            switch (typeHint)
            {
                case (UInt16)NksTypeHint.NKS_TH_DIRECTORY:
                    return NksEntryType.NKS_ENT_DIRECTORY;

                case (UInt16)NksTypeHint.NKS_TH_ENCRYPTED_FILE:
                    return NksEntryType.NKS_ENT_FILE;

                case (UInt16)NksTypeHint.NKS_TH_FILE:
                    return NksEntryType.NKS_ENT_FILE;

                case (UInt16)NksTypeHint.NKS_TH_CONTENT_FILE:
                    return NksEntryType.NKS_ENT_FILE;

                default:
                    return NksEntryType.NKS_ENT_UNKNOWN;
            }
        }
        #endregion
    }

    public enum NksEntryType
    {
        NKS_ENT_UNKNOWN,
        NKS_ENT_DIRECTORY,
        NKS_ENT_FILE,
    }

    public class NksEntry
    {
        public string Name { get; set; }
        public NksEntryType Type { get; set; }
        public UInt32 Offset { get; set; }

        public override string ToString()
        {
            return string.Format("{0,-30} {1,-20} {2}", Name, Type, Offset);
        }
    }

    public class NksSetKey
    {
        public UInt32 SetId { get; set; }
        public byte[] Data = new byte[0x10000];
    }

    public class Nks
    {
        public BinaryFile BinaryFile { get; set; }
        public NksEntry RootEntry = new NksEntry();
        public Dictionary<UInt32, NksSetKey> SetKeys { get; set; }
    }


    public class NksDirectoryHeader
    {
        public UInt16 Version { get; set; }
        public UInt32 SetId { get; set; }
        public byte[] Unknown0 = new byte[4];
        public UInt32 EntryCount { get; set; }
        public byte[] Unknown1 = new byte[4];
    }

    public enum NksTypeHint
    {
        NKS_TH_DIRECTORY = 1,
        NKS_TH_ENCRYPTED_FILE = 2, // like ncw
        NKS_TH_FILE = 3, // not sure what files these are
        NKS_TH_CONTENT_FILE = 4 // like tga, txt, xml, png, cache
    }

    public class Nks0100EntryHeader
    {
        public string Name { get; set; }
        public byte[] Unknown = new byte[1];
        public UInt32 Offset { get; set; }
        public UInt16 Type { get; set; }
    }

    public class Nks0110EntryHeader
    {
        public byte[] Unknown = new byte[2];
        public UInt32 Offset { get; set; }
        public UInt16 Type { get; set; }
        public string Name { get; set; }
    }

    public class NksFileHeader
    {
        public UInt16 Version { get; set; }
        public byte[] Unknown1 = new byte[13];
        public UInt32 Size { get; set; }
        public byte[] Unknown2 = new byte[4];
    }

    public class NksEncryptedFileHeader
    {
        public UInt16 Version { get; set; }
        public UInt32 SetId { get; set; }
        public UInt32 KeyIndex { get; set; }
        public byte[] Unknown1 = new byte[5];
        public UInt32 Size { get; set; }
        public byte[] Unknown2 = new byte[8];
    }
}