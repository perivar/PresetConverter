using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

using Microsoft.Win32; // for registry access

using CommonUtils;
using Serilog;

namespace PresetConverterProject.NIKontaktNKS
{
    public class NKS
    {
        public const string REG_PATH = "Software\\Native Instruments";

        public const UInt32 NKS_MAGIC_DIRECTORY = 0x5E70AC54;               // 54 AC 70 5E = 0x5E70AC54 = 1584442452
        public const UInt32 NKS_MAGIC_ENCRYPTED_FILE = 0x16CCF80A;          // 0A F8 CC 16 = 0x16CCF80A = 382531594

        public const UInt32 NKS_MAGIC_FILE = 0x4916E63C;                    // 3C E6 16 49 = 0x4916E63C = 1226237500


        // Magic content is files like tga, txt, xml, png, cache       
        public const UInt32 NKS_MAGIC_CONTENT_FILE = 0x2AE905FA;            // FA 05 E9 2A = 0x2AE905FA = 719914490


        // Some nkx files that contain both NKI and NCW files start with this
        public const UInt32 NKS_MAGIC_NKI = 0x7FA89012;                     // 12 90 A8 7F = 0x7FA89012  = 2141753362        


        // NCW files start with this
        public const UInt32 NKS_MAGIC_NCW_AUDIO_FILE = 0xD69EA801;          // 01 A8 9E D6 = 0xD69EA801  = 3600721921

        // Number used in SNPID Base36 conversion
        const int SNPID_CONST = 4080;

        public static void NksReadLibrariesInfo(string nksSettingsPath, bool includeNonEncryptedLibs = false)
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
            var settingsList = NksGetSettingsLibraries(nksSettingsPath, includeNonEncryptedLibs);
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
        public static void PrintSettingsLibraryInfo(TextWriter writer, bool includeNonEncryptedLibs = false)
        {
            var list = NKS.NksGetSettingsLibraries("Settings.cfg", includeNonEncryptedLibs);

            if (list != null)
            {
                foreach (NksLibraryDesc entry in list)
                {
                    var id = entry.Id;
                    var name = entry.Name;
                    var keyHex = StringUtils.ByteArrayToHexString(entry.GenKey.Key);
                    var ivHEx = StringUtils.ByteArrayToHexString(entry.GenKey.IV);

                    writer.WriteLine("Id: {0}\nName: {1}\nKey: {2}\nIV: {3}", id, name, keyHex, ivHEx);
                }
            }
        }

        private static List<NksLibraryDesc> NksGetSettingsLibraries(string nksSettingsPath, bool includeNonEncryptedLibs = false)
        {
            Regex sectionRegex = new Regex(@"\[([\w\d\s\.\-]+)\]");
            Regex elementRegex = new Regex(@"(.*?)=sz\:(.*?)$");

            var keyElements = new List<string>();
            keyElements.Add("Name");
            keyElements.Add("SNPID");
            keyElements.Add("Company");
            keyElements.Add("ContentDir");
            keyElements.Add("JDX");
            keyElements.Add("HU");

            List<NksLibraryDesc> settingsList = null;

            using (var reader = new StreamReader(nksSettingsPath))
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
                                        libDesc.Id = value.ToUpper();
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
                        && libDesc.Id != null
                        && ((!includeNonEncryptedLibs && libDesc.GenKey.KeyLength != 0 && libDesc.GenKey.IVLength != 0) || includeNonEncryptedLibs)
                        )
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

            if (list != null)
            {
                foreach (NksLibraryDesc entry in list)
                {
                    var id = entry.Id;
                    var name = entry.Name;
                    var keyHex = StringUtils.ByteArrayToHexString(entry.GenKey.Key);
                    var ivHEx = StringUtils.ByteArrayToHexString(entry.GenKey.IV);

                    writer.WriteLine("Id: {0}\nName: {1}\nKey: {2}\nIV: {3}", id, name, keyHex, ivHEx);
                }
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
                        libDesc.Id = id.ToString();
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
        /// <returns>true on success</returns>
        private static bool NksOpen(string fileName, Nks nks)
        {
            BinaryFile bf = new BinaryFile(fileName, BinaryFile.ByteOrder.LittleEndian, false);

            var r = NksOpenBf(bf, nks);
            if (!r)
            {
                bf.Close();
                return r;
            }

            return true;
        }

        private static bool NksOpenBf(BinaryFile bf, Nks nks)
        {
            if (bf == null)
                throw new ArgumentNullException("BinaryFile cannot be null");

            nks.RootEntry.Name = "";
            nks.RootEntry.Type = NksEntryType.NKS_ENT_DIRECTORY;
            nks.RootEntry.Offset = 0;
            nks.BinaryFile = bf;
            nks.SetKeys = new Dictionary<String, NksSetKey>();

            return true;
        }

        /// <summary>
        /// List all files in the NKS file archive.
        /// </summary>
        /// <param name="fileName">file to list</param>
        public static bool ListArchive(string fileName)
        {
            Nks nks = new Nks();
            if (NksOpen(fileName, nks))
            {
                if (ListArchiveRecursive(nks, nks.RootEntry, 0))
                {
                    if (!"".Equals(nks.RootEntry.SetId))
                    {
                        Log.Information(string.Format("Library is encrypted using snpid: {0}", nks.RootEntry.SetId));
                    }
                    else
                    {
                        Log.Information("Library is not encrypted");
                    }

                    // recursively print the archive tree
                    PrintArchiveRecursive(nks, nks.RootEntry);

                    return true;
                }
            }

            return false;
        }

        private static void PrintArchiveRecursive(Nks nks, NksEntry entry)
        {
            string indent = GetIndentStrings(entry.Level);

            if (entry.Type == NksEntryType.NKS_ENT_DIRECTORY)
            {
                Log.Information(string.Format("{0}{1}  [{2} elements]", indent + "+- ", entry.Name.Length > 0 ? entry.Name : "ROOT", entry.Children != null ? entry.Children.Count : 0));
            }
            else
            {
                int fileSize = -1;
                try
                {
                    fileSize = NksFileSize(nks, entry);

                }
                catch (System.Exception e)
                {
                    Log.Error("Failed reading file size for entry " + entry, e);
                }

                Log.Information(string.Format("{0}{1}  ({2} bytes)", indent + "- ", entry.Name, fileSize));
            }

            if (entry.Children != null)
            {
                foreach (NksEntry child in entry.Children)
                {
                    PrintArchiveRecursive(nks, child);
                }
            }
        }

        private static bool ListArchiveRecursive(Nks nks, NksEntry dirEntry, int level)
        {
            var children = new List<NksEntry>();
            bool isSuccessfull = true;

            if (!NksAddDirEntriesToList(nks, children, dirEntry))
                isSuccessfull = false;

            // if successfull, add the children list to the current node
            if (isSuccessfull)
            {
                // toList() copies the elements
                dirEntry.Children = children.ToList();
            }

            if (!ListDirectory(nks, children, level + 1))
                isSuccessfull = false;

            // reset list
            children.Clear();

            return isSuccessfull;
        }

        private static bool ListDirectory(Nks nks, IList list, int level)
        {
            if (list == null)
                return true;

            bool isSuccessfull = true;

            foreach (NksEntry entry in list)
            {
                // store the level
                entry.Level = level;

                // only care about directories, skip the rest
                if (entry.Type != NksEntryType.NKS_ENT_DIRECTORY)
                    continue;

                // recursively find the files in the directories
                if (!ListArchiveRecursive(nks, entry, level))
                    isSuccessfull = false;
            }

            return isSuccessfull;
        }

        /// <summary>
        /// Extract all files in the NKS file archive.
        /// </summary>
        /// <param name="fileName">file to extract from</param>
        /// <param name="prefix">directory path to use</param>
        public static bool ExtractArchive(string fileName, string prefix)
        {
            Nks nks = new Nks();
            if (NksOpen(fileName, nks))
            {
                if (ListArchiveRecursive(nks, nks.RootEntry, 0))
                {
                    if (!"".Equals(nks.RootEntry.SetId))
                    {
                        Log.Information(string.Format("Library is encrypted using snpid: {0}", nks.RootEntry.SetId));
                    }
                    else
                    {
                        Log.Information("Library is not encrypted");
                    }

                    // recursively extract the archive tree
                    ExtractArchiveRecursive(nks, nks.RootEntry, prefix);

                    return true;
                }
            }

            return false;
        }

        private static void ExtractArchiveRecursive(Nks nks, NksEntry entry, string prefix)
        {
            Log.Information(string.Format("Extracting {0,-100} {1,-18} {2}", Path.Join(prefix, entry.Name), entry.Type, entry.Offset));

            ExtractNksEntry(nks, entry, prefix);

            if (entry.Children != null)
            {
                foreach (NksEntry child in entry.Children)
                {
                    ExtractArchiveRecursive(nks, child, Path.Join(prefix, entry.Name));
                }
            }
        }

        private static bool ExtractNksEntry(Nks nks, NksEntry fileEntry, string prefix)
        {
            bool isSuccessfull = true;
            int extractedCount = 0;

            string outFile = Path.Join(prefix, fileEntry.Name);

            switch (fileEntry.Type)
            {
                case NksEntryType.NKS_ENT_UNKNOWN:
                case NksEntryType.NKS_ENT_FILE:

                    var r = NksExtractFileEntry(nks, fileEntry, outFile);
                    if (r)
                    {
                        extractedCount++;
                    }

                    isSuccessfull = r;
                    break;

                default:
                    break;
            }

            return isSuccessfull;
        }

        /// <summary>
        /// Lists the contents of a directory in an archive and add to the passed list.
        /// The entry must correspond to a directory and not a file.
        /// </summary>
        private static bool NksAddDirEntriesToList(Nks nks, IList list, NksEntry entry)
        {
            NksDirectoryHeader header = new NksDirectoryHeader();
            var r = false;

            if (entry.Type != NksEntryType.NKS_ENT_DIRECTORY)
                throw new ArgumentException("Type is not a directory");

            if (nks.BinaryFile.Seek(entry.Offset, SeekOrigin.Begin) < 0)
                throw new IOException("Failed reading from stream");

            r = NksReadDirectoryHeader(nks.BinaryFile, header);
            if (!r) return false;

            // add setId
            entry.SetId = GetSetIdString(header);

            return NksAddDirEntriesToList(nks, list, header);
        }

        /// <summary>
        /// Lists the contents of a directory in an archive and add to the passed list.
        /// The header must be a NksDirectoryHeader object
        /// </summary>
        private static bool NksAddDirEntriesToList(Nks nks, IList list, NksDirectoryHeader header)
        {
            long offset = nks.BinaryFile.Seek(0, SeekOrigin.Current);
            if (offset < 0)
                throw new IOException("Failed reading from stream");

            var r = false;
            for (int n = 0; n < header.EntryCount; n++)
            {
                if (nks.BinaryFile.Seek(offset, SeekOrigin.Begin) < 0)
                    throw new IOException("Failed reading from stream");

                NksEntry entry = new NksEntry();

                switch (header.Version)
                {
                    case 0x0100:
                        r = NksRead0100Entry(nks.BinaryFile, header, entry);
                        break;

                    case 0x0111:
                    case 0x0110:
                        r = NksRead0110Entry(nks.BinaryFile, header, entry);
                        break;

                    default:
                        throw new NotSupportedException("Header version not supported " + header.Version);
                }

                if (!r) return false;

                offset = nks.BinaryFile.Seek(0, SeekOrigin.Current);
                if (offset < 0)
                {
                    entry = null;
                    throw new IOException("Failed reading from stream");
                }

                // add entry to list    
                var f = list.Add(entry);
                if (f == -1)
                {
                    entry = null;
                    break;
                }

                entry = null;
            }

            return true;
        }

        /// <summary>
        /// Returns the size of a file in an archive.
        /// </summary>
        /// <param name="nks">the archive</param>
        /// <param name="entry">the entry corresponding to a file in the archive</param>
        /// <returns>the size, or a negative value on error</returns>
        public static int NksFileSize(Nks nks, NksEntry entry)
        {
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
                NksEncryptedFileHeader header = new NksEncryptedFileHeader();
                if (NksReadEncryptedFileHeader(nks.BinaryFile, header))
                {
                    return (int)header.Size;
                }
            }
            else if (magic == NKS_MAGIC_CONTENT_FILE)
            {
                NksEncryptedContentFileHeader header = new NksEncryptedContentFileHeader();
                if (NksReadContentFileHeader(nks.BinaryFile, header))
                {
                    return (int)header.Size;
                }
            }
            else
            {
                NksFileHeader header = new NksFileHeader();
                if (NksReadFileHeader(nks.BinaryFile, header))
                {
                    return (int)header.Size;
                }
            }

            return -1;
        }

        /// <summary>
        /// Extracts a file from an archive.
        /// </summary>
        /// <param name="nks">the archive</param>
        /// <param name="entry">the entry corresponding to the file to extract in the archive</param>
        /// <param name="outFile">the name of the file to extract to. The file will be created.</param>
        /// <returns>true on success</returns>
        private static bool NksExtractFileEntry(Nks nks, NksEntry entry, string outFile)
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

            var r = NksExtractFileEntryToBf(nks, entry, bf);
            bf.Close();

            return r;
        }

        /// <summary>
        /// Extracts a file from an archive. This function is similar to
        /// NksExtractFileEntry, but accepts an output BinaryFile instead of a file name.
        /// </summary>
        /// <returns>true on success</returns>
        private static bool NksExtractFileEntryToBf(Nks nks, NksEntry entry, BinaryFile outbinaryFile)
        {
            var r = false;

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
                NksEncryptedFileHeader encHeader = new NksEncryptedFileHeader();
                r = NksReadEncryptedFileHeader(nks.BinaryFile, encHeader);
                if (!r) return false;

                switch (encHeader.Version)
                {
                    case 0x0100:
                    case 0x0110:
                    case 0x0111:
                        return ExtractEncryptedFileEntryToBf(nks, encHeader, outbinaryFile);

                    default:
                        throw new NotSupportedException("Encrypted header version not supported " + encHeader.Version);
                }
            }
            else if (magic == NKS_MAGIC_CONTENT_FILE)
            {
                NksEncryptedContentFileHeader encHeader = new NksEncryptedContentFileHeader();
                r = NksReadContentFileHeader(nks.BinaryFile, encHeader);
                if (!r) return false;

                if (encHeader.SetId != null)
                {
                    // likely encoded content
                    switch (encHeader.Version)
                    {
                        case 0x0100:
                        case 0x0110:
                        case 0x0111:
                            return ExtractEncryptedFileEntryToBf(nks, encHeader, outbinaryFile);

                        default:
                            throw new NotSupportedException("File header version not supported " + encHeader.Version);
                    }
                }
                else
                {
                    // non encoded content
                    switch (encHeader.Version)
                    {
                        case 0x0100:
                        case 0x0110:
                        case 0x0111:
                            return ExtractFileEntryToBf(nks, encHeader, outbinaryFile);

                        default:
                            throw new NotSupportedException("Encrypted header version not supported " + encHeader.Version);
                    }
                }
            }
            else
            {
                NksFileHeader fileHeader = new NksFileHeader();
                r = NksReadFileHeader(nks.BinaryFile, fileHeader);
                if (!r) return false;

                switch (fileHeader.Version)
                {
                    case 0x0100:
                    case 0x0110:
                    case 0x0111:
                        return ExtractFileEntryToBf(nks, fileHeader, outbinaryFile);

                    default:
                        throw new NotSupportedException("File header version not supported " + fileHeader.Version);
                }
            }
        }

        public static bool ExtractEncryptedFileEntryToBf(Nks nks, NksEncryptedHeader header, BinaryFile outBinaryFile)
        {
            int bufferLength = 16384;
            int toRead;
            int size = (int)header.Size;

            int keyLength = 0;
            long keyPos = 0;
            byte[] key = null;

            if (header.KeyIndex < 0xff)
            {
                if (!NksGet0100Key((int)header.KeyIndex, out key, out keyLength))
                {
                    throw new KeyNotFoundException("Could not find key");
                }

                if (keyLength != 0x10)
                {
                    throw new InvalidDataException("Key is not 16 bytes but " + keyLength);
                }

                keyPos = nks.BinaryFile.Seek(0, SeekOrigin.Current);
                if (keyPos < 0)
                {
                    throw new IOException("Failed reading from stream");
                }
            }
            else if (header.KeyIndex == 0x100)
            {
                NksSetKey setKey = nks.SetKeys.Where(a => a.Key == header.SetId).Select(a => a.Value).FirstOrDefault();

                if (setKey == null)
                {
                    NksLibraryDesc lib = NKSLibraries.Libraries.Where(a => a.Key == header.SetId).FirstOrDefault().Value;
                    if (lib == null)
                    {
                        // try again - this time converting the number to Base36 (alphanumeric)             
                        long base10Id = long.Parse(header.SetId);

                        if ((base10Id - SNPID_CONST) < 0)
                        {
                            // this is wrong and likely means the Kontakt library has not been installed
                            throw new KeyNotFoundException(String.Format("Lib could not be found using id: {0}. Has it been added to Kontakt?", base10Id));
                        }

                        string base36Key = Base36Converter.Encode(base10Id - SNPID_CONST);

                        lib = NKSLibraries.Libraries.Where(a => a.Key == base36Key).FirstOrDefault().Value;
                        if (lib == null)
                        {
                            throw new KeyNotFoundException(String.Format("Lib could not be found neither using id: {0} or key: {1}. Has it been added to Kontakt?", base10Id, base36Key));
                        }
                    }

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
                if (header.SetId == null)
                {
                    // this is a unencoded file
                    return ExtractFileEntryToBf(nks, header, outBinaryFile);
                }
                else
                {
                    throw new KeyNotFoundException("Could not find key");
                }
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

            return true;
        }

        /// <summary>
        /// Extract non encoded file to output file
        /// </summary>
        /// <param name="nks">nks</param>
        /// <param name="header">unencoded header information</param>
        /// <param name="outBinaryFile">output file</param>
        /// <returns>true if successfull</returns>
        private static bool ExtractFileEntryToBf(Nks nks, NksHeader header, BinaryFile outBinaryFile)
        {
            byte[] buffer = new byte[16384];
            int size = (int)header.Size;
            int toRead;

            while (size > 0)
            {
                toRead = Math.Min((int)buffer.Length, size);
                var readBytes = nks.BinaryFile.ReadBytes(toRead);

                outBinaryFile.Write(readBytes);

                size -= toRead;
            }

            return true;
        }

        /// <summary>
        /// Ignore SetId and extract as if the file is not encrypted to output file
        /// </summary>
        /// <param name="nks">nks</param>
        /// <param name="header">encrypted header information</param>
        /// <param name="outBinaryFile">output file</param>
        /// <returns>true if successfull</returns>
        public static bool ExtractFileEntryToBf(Nks nks, NksEncryptedFileHeader header, BinaryFile outBinaryFile)
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

            return true;
        }

        #endregion

        #region Scan Files

        /// <summary>
        /// Scan all files in the NKS file archive.
        /// </summary>
        /// <param name="fileName">file to scan</param>
        public static void ScanArchive(string fileName)
        {
            Nks nks = new Nks();
            var r = NksOpen(fileName, nks);

            ScanChunk(nks, "/", 0);
        }

        private static bool ScanChunk(Nks nks, string name, int indentCount)
        {
            UInt32 magic = 0;
            long offset = -1;

            if ((offset = nks.BinaryFile.Seek(0, SeekOrigin.Current)) < 0)
            {
                return false;
            }

            try
            {
                magic = nks.BinaryFile.ReadUInt32(); // read_u32_le

            }
            catch (System.Exception)
            {
                return false;
            }

            // backtrack 4 bytes
            if ((nks.BinaryFile.Seek(-4, SeekOrigin.Current)) < 0)
            {
                return false;
            }

            switch (magic)
            {
                case NKS_MAGIC_ENCRYPTED_FILE:
                    return ScanEncryptedFile(nks, name, indentCount);

                case NKS_MAGIC_DIRECTORY:
                    return ScanDirectory(nks, name, indentCount);

                case NKS_MAGIC_FILE:
                    return ScanFile(nks, name, indentCount);

                case NKS_MAGIC_CONTENT_FILE: // like tga, txt, xml, png, cache
                    return ScanContentFile(nks, name, indentCount);

                case NKS_MAGIC_NKI:
                    return ScanNKIFile(nks, name, indentCount);

                default:
                    Log.Debug(GetIndentStrings(indentCount) + "[{0}] unknown 0x{1}:{2}", offset, magic.ToString("X"), name);
                    return true;
            }
        }

        private static bool ScanEncryptedFile(Nks nks, string name, int indentCount)
        {
            NksEncryptedFileHeader header = new NksEncryptedFileHeader();
            long offset = -1;
            byte[] data = new byte[16];

            if ((offset = nks.BinaryFile.Seek(0, SeekOrigin.Current)) < 0)
            {
                return false;
            }

            var r = NksReadEncryptedFileHeader(nks.BinaryFile, header);
            if (!r) return false;

            try
            {
                data = nks.BinaryFile.ReadBytes(16);
            }
            catch (System.Exception)
            {
                return false;
            }

            Log.Information(GetIndentStrings(indentCount++) + "[{0}] encrypted_file 0x{1}:{2}:{3}:{4}:0x{5}:0x{6}", offset, header.Version.ToString("X4"), GetSetIdString(header), header.KeyIndex, header.Size, StringUtils.ByteArrayToHexString(header.Unknown1), StringUtils.ByteArrayToHexString(header.Unknown2));
            Log.Information(GetIndentStrings(indentCount) + StringUtils.ToHexAndAsciiString(data));

            return true;
        }

        private static bool ScanFile(Nks nks, string name, int indentCount)
        {
            NksFileHeader header = new NksFileHeader();
            long offset = -1;

            if ((offset = nks.BinaryFile.Seek(0, SeekOrigin.Current)) < 0)
            {
                return false;
            }

            var r = NksReadFileHeader(nks.BinaryFile, header);
            if (!r) return false;

            Log.Information(GetIndentStrings(indentCount++) + "[{0}] file 0x{1:X4}:0x{2}:{3}:0x{4}:{5}", offset, header.Version, StringUtils.ByteArrayToHexString(header.Unknown1), header.Size, StringUtils.ByteArrayToHexString(header.Unknown2), name);

            return true;
        }

        private static bool ScanContentFile(Nks nks, string name, int indentCount)
        {
            NksEncryptedContentFileHeader header = new NksEncryptedContentFileHeader();
            long offset = -1;
            byte[] data = new byte[16];

            if ((offset = nks.BinaryFile.Seek(0, SeekOrigin.Current)) < 0)
            {
                return false;
            }

            var r = NksReadContentFileHeader(nks.BinaryFile, header);
            if (!r) return false;

            try
            {
                data = nks.BinaryFile.ReadBytes(16);
            }
            catch (System.Exception)
            {
                return false;
            }

            Log.Information(GetIndentStrings(indentCount++) + "[{0}] content_file 0x{1}:{2}:{3}:{4}:0x{5}", offset, header.Version.ToString("X4"), GetSetIdString(header), header.KeyIndex, header.Size, StringUtils.ByteArrayToHexString(header.Unknown));
            Log.Information(GetIndentStrings(indentCount) + StringUtils.ToHexAndAsciiString(data));

            return true;
        }

        private static bool ScanNKIFile(Nks nks, string name, int indentCount)
        {
            NksBundleHeader header = new NksBundleHeader();
            long offset = -1;

            if ((offset = nks.BinaryFile.Seek(0, SeekOrigin.Current)) < 0)
            {
                return false;
            }

            var r = NksReadNKIHeader(nks.BinaryFile, header);
            if (!r) return false;

            Log.Information(GetIndentStrings(indentCount++) + "[{0}] nki_file", offset);

            return ScanDirectory(nks, name, indentCount);
        }

        private static bool ScanDirectory(Nks nks, string name, int indentCount)
        {
            NksDirectoryHeader header = new NksDirectoryHeader();
            var r = false;
            long offset = -1;

            if ((offset = nks.BinaryFile.Seek(0, SeekOrigin.Current)) < 0)
            {
                return false;
            }

            r = NksReadDirectoryHeader(nks.BinaryFile, header);
            if (!r) return false;

            Log.Information(GetIndentStrings(indentCount++) + "[{0}] directory 0x{1}:{2}:0x{3}:0x{4}:{5}:{6}", offset, header.Version.ToString("X4"), GetSetIdString(header), StringUtils.ByteArrayToHexString(header.Unknown0), StringUtils.ByteArrayToHexString(header.Unknown1), header.EntryCount, name);

            switch (header.Version)
            {
                case 0x0100:
                    if (!Scan0100Entries(nks, header.EntryCount, indentCount))
                        return false;
                    break;

                case 0x0111:
                case 0x0110:
                    if (!Scan0110Entries(nks, header.EntryCount, indentCount))
                        return false;
                    break;

                default:
                    throw new NotSupportedException("Header version not supported " + header.Version);
            }

            return true;
        }

        private static bool Scan0100Entry(Nks nks, long offset, Nks0100EntryHeader header, int indentCount)
        {
            Log.Information(GetIndentStrings(indentCount) + "[{0}] entry_0100 {1}:0x{2}:0x{3}:{4}", offset, header.Offset, header.Type.ToString("X4"), StringUtils.ByteArrayToHexString(header.Unknown), header.Name);

            if (nks.BinaryFile.Seek(header.Offset, SeekOrigin.Begin) < 0)
                return false;

            return ScanChunk(nks, header.Name, indentCount);
        }

        private static bool Scan0100Entries(Nks nks, uint count, int indentCount)
        {
            long[] offsets = new long[count];
            Nks0100EntryHeader[] entries = new Nks0100EntryHeader[count];
            int n;
            var r = false;

            for (n = 0; n < count; n++)
            {
                offsets[n] = nks.BinaryFile.Seek(0, SeekOrigin.Current);
                if (offsets[n] < 0)
                {
                    return false;
                }

                entries[n] = new Nks0100EntryHeader();
                r = NksRead0100EntryHeader(nks.BinaryFile, entries[n]);
                if (!r) return false;
            }

            for (n = 0; n < count; n++)
            {
                if (!Scan0100Entry(nks, offsets[n], entries[n], indentCount))
                    return false;
            }

            return true;
        }

        private static bool Scan0110Entry(Nks nks, long offset, Nks0110EntryHeader header, int indentCount)
        {
            Log.Information(GetIndentStrings(indentCount) + "[{0}] entry_0110 {1}:0x{2}:0x{3}:{4}", offset, header.Offset, header.Type.ToString("X4"), StringUtils.ByteArrayToHexString(header.Unknown), header.Name);

            if (nks.BinaryFile.Seek(header.Offset, SeekOrigin.Begin) < 0)
                return false;

            return ScanChunk(nks, header.Name, indentCount);
        }

        private static bool Scan0110Entries(Nks nks, uint count, int indentCount)
        {
            long[] offsets = new long[count];
            Nks0110EntryHeader[] entries = new Nks0110EntryHeader[count];
            int n;
            var r = false;

            for (n = 0; n < count; n++)
            {
                offsets[n] = nks.BinaryFile.Seek(0, SeekOrigin.Current);
                if (offsets[n] < 0)
                {
                    return false;
                }

                entries[n] = new Nks0110EntryHeader();
                r = NksRead0110EntryHeader(nks.BinaryFile, entries[n]);
                if (!r) return false;
            }

            for (n = 0; n < count; n++)
            {
                if (!Scan0110Entry(nks, offsets[n], entries[n], indentCount))
                    return false;
            }

            return true;
        }

        private static string GetSetIdString(NksDirectoryHeader header)
        {
            if (header.SetId == 0) return "";

            long base10SetId = header.SetId;
            string setIdKey = base10SetId.ToString();

            // check if this is a base36 or base10 id?
            if ((base10SetId - SNPID_CONST) > 0)
            {
                // convert the number to Base36 (alphanumeric)             
                setIdKey = Base36Converter.Encode(base10SetId - SNPID_CONST);
            }

            return setIdKey;
        }

        private static string GetSetIdString(NksEncryptedHeader header)
        {
            if (header.SetId == null) return "";

            long base10SetId = long.Parse(header.SetId);
            string setIdKey = base10SetId.ToString();

            // check if this is a base36 or base10 id?
            if ((base10SetId - SNPID_CONST) > 0)
            {
                // convert the number to Base36 (alphanumeric)             
                setIdKey = Base36Converter.Encode(base10SetId - SNPID_CONST);
            }

            return setIdKey;
        }

        private static string GetIndentStrings(int indentCount)
        {
            string indent = "";
            for (int n = 0; n < indentCount; n++)
            {
                indent += "   ";
            }
            return indent;
        }

        #endregion

        #region Generate and Read Keys mehods
        private static byte[][] Nks0100Keys = MathUtils.CreateJaggedArray<byte[][]>(32, 16);
        private static byte[] Nks0110BaseKey;

        private static bool NksGet0100Key(int keyIndex, out byte[] key, out int length)
        {
            key = null;
            length = 0;

            if (keyIndex >= 0x20)
                throw new ArgumentException("Could not find key");

            if (Nks0100Keys[0][0] == 0)
                Generate0100Keys();

            var retKey = Nks0100Keys[keyIndex];

            if (retKey != null)
            {
                key = retKey;
                length = retKey.Length;
            }

            return true;
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
            int seed = 0x6EE38FE0;
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
            seed = seed * 0x343FD + 0x269EC3;
            return (byte)((seed >> 0x10) & 0x7FFF);
        }

        private static void Generate0110BaseKey()
        {
            if (Nks0110BaseKey != null)
                return;

            Nks0110BaseKey = new byte[0x10000];

            // Fill array with pseudo random bytes
            int seed = 0x608DA0A2;
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
        private static bool NksReadDirectoryHeader(BinaryFile bf, NksDirectoryHeader header)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic == (UInt32)(NKS_MAGIC_NKI)) // 12 90 A8 7F = 0x7FA89012  = 2141753362
            {
                Log.Debug("Detected Magic 0x7FA89012 (NKS_MAGIC_NKI) instead of 0x5E70AC54 (NKS_MAGIC_DIRECTORY). Skipping to where Directory is expected.");
                bf.ReadBytes(218);
                magic = bf.ReadUInt32(); // read_u32_le
            }

            if (magic != (UInt32)(NKS_MAGIC_DIRECTORY)) // 54 AC 70 5E = 0x5E70AC54 = 1584442452
                throw new IOException("Magic not as expected (0x5E70AC54) but " + magic);

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
                case 0x0111:
                    break;

                default:
                    throw new ArgumentException("Header version not valid: " + header.Version);
            }

            return true;
        }

        private static bool NksReadNKIHeader(BinaryFile bf, NksBundleHeader header)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(NKS_MAGIC_NKI)) // 12 90 A8 7F = 0x7FA89012  = 2141753362        
                throw new IOException("Magic not as expected (0x7FA89012) but " + magic);

            if ((header.Unknown = bf.ReadBytes(218)).Length != 218)
                throw new IOException("Failed reading from stream");

            return true;
        }

        private static bool NksRead0100EntryHeader(BinaryFile bf, Nks0100EntryHeader header)
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

            return true;
        }

        private static bool NksRead0110EntryHeader(BinaryFile bf, Nks0110EntryHeader header)
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

            return true;
        }

        private static bool NksRead0100Entry(BinaryFile bf, NksDirectoryHeader dir, NksEntry entry)
        {
            Nks0100EntryHeader hdr = new Nks0100EntryHeader();

            var r = NksRead0100EntryHeader(bf, hdr);
            if (!r) return false;

            entry.Name = hdr.Name.ToUpper();
            entry.Offset = hdr.Offset;
            entry.Type = TypeHintToEntryType(hdr.Type);

            return true;
        }

        private static bool NksRead0110Entry(BinaryFile bf, NksDirectoryHeader dir, NksEntry entry)
        {
            Nks0110EntryHeader hdr = new Nks0110EntryHeader();

            var r = NksRead0110EntryHeader(bf, hdr);
            if (!r) return false;

            entry.Name = hdr.Name.ToUpper();
            entry.Offset = hdr.Offset;
            entry.Type = TypeHintToEntryType(hdr.Type);

            return true;
        }

        private static bool NksReadEncryptedFileHeader(BinaryFile bf, NksEncryptedFileHeader ret)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(NKS_MAGIC_ENCRYPTED_FILE)) // 0A F8 CC 16 = 0x16CCF80A
                throw new IOException("Magic not as expected (0x16CCF80A) but " + magic);

            ret.Version = bf.ReadUInt16(); // read_u16_le
            uint setId = bf.ReadUInt32(); // read_u32_le
            if (setId > 0)
            {
                ret.SetId = setId.ToString("000");
            }
            else
            {
                ret.SetId = null;
            }

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
                case 0x0111:
                    break;

                default:
                    throw new NotSupportedException("Unexpected version number: " + ret.Version);
            }

            return true;
        }

        private static bool NksReadContentFileHeader(BinaryFile bf, NksEncryptedContentFileHeader ret)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != NKS_MAGIC_CONTENT_FILE) // 0x2AE905FA
                throw new IOException("Magic not as expected (0x2AE905FA) but " + magic);

            ret.Version = bf.ReadUInt16(); // read_u16_le
            uint setId = bf.ReadUInt32(); // read_u32_le
            if (setId > 0)
            {
                ret.SetId = setId.ToString();
            }
            else
            {
                ret.SetId = null;
            }

            ret.KeyIndex = bf.ReadUInt32(); // read_u32_le
            ret.Size = bf.ReadUInt32(); // read_u32_le

            if ((ret.Unknown = bf.ReadBytes(4)).Length != 4)
                throw new IOException("Failed reading from stream");

            return true;
        }

        private static bool NksReadFileHeader(BinaryFile bf, NksFileHeader ret)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(NKS_MAGIC_FILE)) // 0x4916E63C
                throw new IOException("Magic not as expected (0x4916E63C) but " + magic);

            ret.Version = bf.ReadUInt16(); // read_u16_le

            if ((ret.Unknown1 = bf.ReadBytes(13)).Length != 13)
                throw new IOException("Failed reading from stream");

            ret.Size = bf.ReadUInt32(); // read_u32_le

            if ((ret.Unknown2 = bf.ReadBytes(4)).Length != 4)
                throw new IOException("Failed reading from stream");

            return true;
        }

        private static bool NksReadNcwHeader(BinaryFile bf, NcwAudioHeader header)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(NKS_MAGIC_NCW_AUDIO_FILE)) // 01 A8 9E D6 = 0xD69EA801  = 3600721921
                throw new IOException("Magic not as expected (0xD69EA801) but " + magic);

            header.Version = bf.ReadUInt32();
            header.Channels = bf.ReadUInt16();
            header.BitDepth = bf.ReadUInt16();
            header.SampleRate = bf.ReadUInt32();

            return true;
        }

        private static UInt32 DecodeOffset(UInt32 offset)
        {
            return (offset ^ (UInt32)(0x1F4E0C8D));
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
        public String Name { get; set; }
        public NksEntryType Type { get; set; }
        public UInt32 Offset { get; set; }

        // added to support a recursive tree
        public int Level { get; set; }
        public IList<NksEntry> Children { get; set; }
        public String SetId { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: '{1}' [{2}] #{3} ({4} elements)", Level, Name, Type, Offset, Children != null ? Children.Count : 0);
        }
    }

    public class NksSetKey
    {
        public String SetId { get; set; }
        public byte[] Data = new byte[0x10000];
    }

    public class Nks
    {
        public BinaryFile BinaryFile { get; set; }
        public NksEntry RootEntry = new NksEntry();
        public Dictionary<String, NksSetKey> SetKeys { get; set; }
    }

    public enum NksTypeHint
    {
        NKS_TH_DIRECTORY = 1,
        NKS_TH_ENCRYPTED_FILE = 2, // like ncw
        NKS_TH_FILE = 3, // not sure what files these are
        NKS_TH_CONTENT_FILE = 4 // like tga, txt, xml, png, cache
    }

    public class NksDirectoryHeader
    {
        public UInt16 Version { get; set; }
        public UInt32 SetId { get; set; }
        public byte[] Unknown0 = new byte[4];
        public UInt32 EntryCount { get; set; }
        public byte[] Unknown1 = new byte[4];
    }

    public abstract class NksEntryHeader
    {
        public string Name { get; set; }
        public UInt32 Offset { get; set; }
        public UInt16 Type { get; set; }
    }

    public class Nks0100EntryHeader : NksEntryHeader
    {
        public byte[] Unknown = new byte[1];
    }

    public class Nks0110EntryHeader : NksEntryHeader
    {
        public byte[] Unknown = new byte[2];
    }

    public abstract class NksHeader
    {
        public UInt16 Version { get; set; }
        public UInt32 Size { get; set; }
    }

    public abstract class NksEncryptedHeader : NksHeader
    {
        public String SetId { get; set; }
        public UInt32 KeyIndex { get; set; }
    }

    public class NksFileHeader : NksHeader
    {
        public byte[] Unknown1 = new byte[13];
        public byte[] Unknown2 = new byte[4];
    }

    public class NksEncryptedFileHeader : NksEncryptedHeader
    {
        public byte[] Unknown1 = new byte[5];
        public byte[] Unknown2 = new byte[8];
    }

    public class NksEncryptedContentFileHeader : NksEncryptedHeader
    {
        public byte[] Unknown = new byte[4];
    }

    public class NksBundleHeader
    {
        public byte[] Unknown = new byte[218];
    }

    public class NcwAudioHeader
    {
        public UInt32 Version { get; set; }
        public UInt16 Channels { get; set; }
        public UInt16 BitDepth { get; set; }
        public UInt32 SampleRate { get; set; }
    }
}