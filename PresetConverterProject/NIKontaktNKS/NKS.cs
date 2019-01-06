using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        public const string PACKAGE_PREFIX = "com.native-instruments";
        public const string REG_PATH = "Software\\Native Instruments";

        public const UInt32 NKS_MAGIC_DIRECTORY = 0x5e70ac54;
        public const UInt32 NKS_MAGIC_ENCRYPTED_FILE = 0x16ccf80a;
        public const UInt32 NKS_MAGIC_FILE = 0x4916e63c;


        // public delegate bool NksTraverseFunc(T1 nks, T2 entry, T3 user_data);
        // public delegate bool NksTraverseFunc<T1, T2>(T1 entry, T2 ctx);

        // private static bool addEntry_to_list(Nks nks, NksEntry entry, IList list)
        // public delegate bool NksTraverseFunc(Nks nks, NksEntry entry);


        // public static bool nks_find_subEntry(NksEntry entry, FindEntryContext ctx)
        // public delegate bool NksTraverseFunc(NksEntry entry, FindEntryContext ctx);

        #region Read Library Descriptors from Registry
        public static void PrintLibraryInfo(TextWriter writer)
        {
            IList list = new ArrayList();
            NKS.NksGetLibraries(list);
            foreach (NksLibraryDesc entry in list)
            {
                var id = entry.Id;
                var name = entry.Name;
                var keyHex = StringUtils.ToHexEditorString(entry.GenKey.Key);
                var ivHEx = StringUtils.ToHexEditorString(entry.GenKey.IV);

                writer.WriteLine("Id: {0}\nName: {1}\nKey: {2}IV: {3}", id, name, keyHex, ivHEx);
            }
        }

        public static int NksGetLibraries(IList regList)
        {
            NksLibraryDesc ld = null;
            try
            {
                var regPath = string.Format("{0}\\{1}", REG_PATH, "Content");
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regPath))
                {
                    if (key != null)
                    {
                        var subKeys = key.GetValueNames(); // values in current reg folder
                        // var subkeys = key.GetSubKeyNames(); // sub folders

                        foreach (var subKey in subKeys)
                        {
                            ld = CreateLibraryDesc(subKey, key.GetValue(subKey).ToString());
                            if (ld == null)
                                continue;

                            regList.Add(ld);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to open HKLM\\" + REG_PATH + "\\Content", ex);
            }

            return 0;
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

        #region UnNks methods        
        public static void TraverseDirectory(string fileName, string prefix)
        {
            Nks nks = new Nks();
            NksEntry rootEntry = new NksEntry();
            rootEntry.Name = "";
            rootEntry.Offset = 0;
            rootEntry.Type = NksEntryType.NKS_ENT_DIRECTORY;

            int r = NksOpen(fileName, nks);

            bool ret = !TraverseDirectory(nks, rootEntry, prefix);
        }

        public static bool TraverseDirectory(Nks nks, NksEntry dirEntry, string prefix)
        {
            var list = new ArrayList();
            bool ret = true;
            int r = NksListDirEntry(nks, list, dirEntry);
            if (r != 0)
            {
                ret = false;
            }

            if (!TraverseDirectories(nks, list, prefix))
                ret = false;

            // this is where the files are decrypted and extracted
            if (!TraverseFiles(nks, list, prefix))
                ret = false;

            list.Clear();
            return ret;
        }

        private static bool TraverseDirectories(Nks nks, IList list, string prefix)
        {
            if (list == null)
                return true;

            bool ret = true;

            foreach (NksEntry entry in list)
            {
                if (entry.Type != NksEntryType.NKS_ENT_DIRECTORY)
                    continue;

                string prefix_buffer = Path.Join(prefix, entry.Name);

                if (!TraverseDirectory(nks, entry, prefix_buffer))
                    ret = false;
            }

            return ret;
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

            Debug.Assert(fileEntry.Type != NksEntryType.NKS_ENT_DIRECTORY);

            string outFile = Path.Join(prefix, fileEntry.Name);

            switch (fileEntry.Type)
            {
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

        /**
         * Opens an archive.  This must be called first, before anything else can be done
         * with archives.
         *
         * @param fileName name of the file to open
         * @param ret	    pointer to a Nks * pointer, which will be initialised upon
         * 		    success.  It has to be closed with nks_close after it is no
         * 	            longer used.
         *
         * @return 0 on success
         */
        public static int NksOpen(string fileName, Nks nks)
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

        /**
         * Similar to nks_open, but uses a file descriptor instead of a file name.  If
         * this function completes successfully, you must not use bf any more directly.
         * Make sure that bf is open in binary mode on systems that distinguish between
         * text and binary.
         */
        public static int NksOpenBf(BinaryFile bf, Nks nks)
        {
            if (bf == null)
                throw new ArgumentException("-EINVAL");

            nks.RootEntry.Name = "/";
            nks.RootEntry.Type = NksEntryType.NKS_ENT_DIRECTORY;
            nks.RootEntry.Offset = 0;
            nks.BinaryFile = bf;
            nks.SetKeys = new Dictionary<UInt32, NksSetKey>();

            return 0;
        }

        /**
         * Closes an archive.
         */
        public static void NksClose(Nks nks)
        {
            nks = null;
        }

        /**
         * Lists the contents of a directory in an archive.  It calls func for each
         * entry in the directory.  If func returns false, then then no more entries
         * will be listed.  Do not modify or free the entries provided to func.
         * If you want to copy any of the entries, you must use nksEntry_copy.
         *
         * @param nks       the archive
         * @param dir       the path to the directory to list.  It must contain slashes
         *                  ('/') as separators, and must not start with a slash.  If it
         *                  contains non-ASCII characters, it must be encoded in UTF-8.
         * @param func      the function to call for each entry
         * @param user_data an optional argument passed to func
         * 
         * @return 0 on success
         */
        public static int NksListDir(Nks nks, IList list, string dir)
        {
            NksEntry entry = new NksEntry();
            int r;

            r = NksFindEntry(nks, dir, entry);
            if (r != 0)
                return r;

            if (entry.Type != NksEntryType.NKS_ENT_DIRECTORY)
                throw new DirectoryNotFoundException("-ENOTDIR");

            r = NksListDirEntry(nks, list, entry);
            entry = null;

            return r;
        }

        /**
         * Lists the contents of a directory in an archive.  It is similar to nks_list,
         * but uses a NksEntry instead of a path.  The entry must correspond to a
         * directory and not a file..
         */
        public static int NksListDirEntry(Nks nks, IList list, NksEntry entry)
        {
            NksDirectoryHeader header = new NksDirectoryHeader();
            int r;

            if (entry.Type != NksEntryType.NKS_ENT_DIRECTORY)
                throw new DirectoryNotFoundException("-ENOTDIR");

            if (nks.BinaryFile.Seek(entry.Offset, SeekOrigin.Begin) < 0)
                throw new IOException("-EIO");

            r = NksReadDirectoryHeader(nks.BinaryFile, header);
            if (r != 0)
                return r;

            return ListDirectory(nks, list, header);
        }

        /**
         * Returns the size of a file in an archive.
         *
         * @param nks   the archive
         * @param entry the entry corresponding to a file in the archive
         *
         * @return the size, or a negative value on error
         */
        public static int NksFileSize(Nks nks, NksEntry entry)
        {
            NksEncryptedFileHeader header = new NksEncryptedFileHeader();
            int r;

            if (nks.BinaryFile.Seek(entry.Offset, SeekOrigin.Begin) < 0)
                throw new IOException("-EIO");

            UInt32 magic = nks.BinaryFile.ReadUInt32(); // read_u32_le

            switch (magic)
            {
                case NKS_MAGIC_ENCRYPTED_FILE:
                    break;

                case NKS_MAGIC_DIRECTORY:
                    throw new DirectoryNotFoundException("-EISDIR");

                default:
                    throw new NotSupportedException("-ENOTSUP");
            }

            if (nks.BinaryFile.Seek(entry.Offset, SeekOrigin.Begin) < 0)
                throw new IOException("-EIO");

            r = NksReadEncryptedFileHeader(nks.BinaryFile, header);
            if (r != 0)
                return r;

            return (int)header.Size;
        }

        /**
         * Extracts a file from an archive.
         *
         * @param nks      the archive
         * @param entry    the entry corresponding to the file to extract in the archive
         * @param out_file the name of the file to extract to.  The file will be
         *                 created.
         *
         * @return 0 on success
         */
        public static int NksExtractFileEntry(Nks nks, NksEntry entry, string outFile)
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

        /**
         * Extracts a file from an archive.  This function is similar to
         * nks_extractEntry, but accepts an output file descriptor instead of a file
         * name.
         */
        public static int NksExtractFileEntryToBf(Nks nks, NksEntry entry, BinaryFile outbinaryFile)
        {
            NksEncryptedFileHeader encHeader = new NksEncryptedFileHeader();
            NksFileHeader fileHeader = new NksFileHeader();
            int r;

            if (nks.BinaryFile.Seek(entry.Offset, SeekOrigin.Begin) < 0)
                throw new IOException("-EIO");

            UInt32 magic = nks.BinaryFile.ReadUInt32(); // read_u32_le
            switch (magic)
            {
                case NKS_MAGIC_ENCRYPTED_FILE:
                case NKS_MAGIC_FILE:
                    break;

                case NKS_MAGIC_DIRECTORY:
                    throw new DirectoryNotFoundException("-EISDIR");

                default:
                    throw new NotSupportedException("-ENOTSUP");
            }

            if (nks.BinaryFile.Seek(entry.Offset, SeekOrigin.Begin) < 0)
                throw new IOException("-EIO");

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
                        throw new NotSupportedException("-ENOTSUP");
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
                        throw new NotSupportedException("-ENOTSUP");
                }
            }
        }

        /**
         * Finds the entry corresponding to the given path.
         *
         * @param nks  archive handle
         * @param path path to the entry
         * @param ret  pointer to a NksEntry structure to be filled in.  Must be
         * 	       disposed of with nksEntry_free.
         *
         * @return 0 on success
         */
        public static int NksFindEntry(Nks nks, string name, NksEntry ret)
        {
            NksEntry entry = new NksEntry();
            NksEntry subEntry = new NksEntry();
            string buffer = new string(new char[512]);
            int r;

            if (name == null || name[0] == '/')
                throw new ArgumentException("-EINVAL");

            NksEntryCopy(nks.RootEntry, entry);

            var path_segments = name.Split('/');
            foreach (var path_segment in path_segments)
            {
                r = NksGetEntry(nks, entry, buffer, subEntry);
                if (r != 0)
                    throw new ArgumentException("-ENOTDIR");

                if (buffer[0] == 0)
                {
                    if (entry.Type != NksEntryType.NKS_ENT_DIRECTORY)
                    {
                        throw new ArgumentException("-ENOTDIR");
                    }

                    break;
                }

                r = NksGetEntry(nks, entry, buffer, subEntry);
                if (r != 0)
                    throw new ArgumentException("-ENOTDIR");

                entry = null;

                entry = subEntry;
            }

            ret = entry;
            return 0;
        }

        /**
         * Gets an immediate sub-entry of the given directory entry.
         *
         * @param nks   archive handle
         * @param entry directory entry
         * @param name  the name of the sub-entry
         * @param ret   pointer to a NksEntry structure to be filled in.  Must be
         *              disposed of with nksEntry_free.
         *
         * @return 0 on success
         */
        public static int NksGetEntry(Nks nks, NksEntry entry, string name, NksEntry ret)
        {
            FindEntryContext context = new FindEntryContext();
            int r = 0;

            context.Name = name;
            context.Entry = ret;
            context.Found = false;

            // TODO: fix missing implementation
            throw new NotImplementedException();

            // NksTraverseFunc traverseFunc = nks_find_subEntry;
            // r = nks_list_dirEntry(nks, entry, traverseFunc, context);

            if (r != 0)
                return r;

            if (!context.Found)
                throw new KeyNotFoundException("-ENOENT");

            return 0;
        }

        /**
         * Copies an entry.
         *
         * @param src pointer to the entry to copy from
         * @param dst pointer to the entry to copy to.  dst must point to a block
         *            of allocated memory.  When no longer needed, it must be freed with
         *            nksEntry_free.
         */
        public static void NksEntryCopy(NksEntry src, NksEntry dst)
        {
            dst.Name = src.Name.ToUpper();
            dst.Type = src.Type;
            dst.Offset = src.Offset;
        }

        public static bool NksFindSubEntry(NksEntry entry, FindEntryContext ctx)
        {
            if (String.Equals(entry.Name, ctx.Name, StringComparison.OrdinalIgnoreCase))
            {
                NksEntryCopy(entry, ctx.Entry);
                ctx.Found = true;
                return false;
            }

            return true;
        }

        public static int ListDirectory(Nks nks, IList list, NksDirectoryHeader header)
        {
            long offset = nks.BinaryFile.Seek(0, SeekOrigin.Current);
            if (offset < 0)
                throw new IOException("-EIO");

            int r = 0;
            for (int n = 0; n < header.EntryCount; n++)
            {
                if (nks.BinaryFile.Seek(offset, SeekOrigin.Begin) < 0)
                    throw new IOException("-EIO");

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
                        throw new NotSupportedException("-ENOTSUP");
                }

                if (r != 0)
                    return r;

                offset = nks.BinaryFile.Seek(0, SeekOrigin.Current);
                if (offset < 0)
                {
                    entry = null;
                    throw new IOException("-EIO");
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

        public static int ExtractEncryptedFileEntryToBf(Nks nks, NksEncryptedFileHeader header, BinaryFile outBinaryFile)
        {
            byte[] buffer = new byte[16384];
            int toRead;
            int size = (int)header.Size;

            uint keyLength = 0;
            long keyPos = 0;
            byte[] key = null;

            if (header.KeyIndex < 0xff)
            {
                if (NksGet0100Key((int)header.KeyIndex, key, out keyLength) != 0)
                    throw new KeyNotFoundException("-ENOKEY");

                Debug.Assert(keyLength == 0x10);

                keyPos = nks.BinaryFile.Seek(0, SeekOrigin.Current);
                if (keyPos < 0)
                    throw new IOException("-EIO");
            }
            else if (header.KeyIndex == 0x100)
            {
                NksSetKey setKey = nks.SetKeys.Where(a => a.Key == header.SetId).Select(a => a.Value).FirstOrDefault();

                if (setKey == null)
                {
                    NksLibraryDesc lib = NKSLibraries.libraries.Where(a => a.Id == header.SetId).FirstOrDefault();

                    if (lib == null)
                        throw new KeyNotFoundException("-ENOKEY");

                    setKey = new NksSetKey();
                    setKey.SetId = header.SetId;
                    int r = NksCreate0110Key(lib.GenKey, setKey.Data);
                    if (r != 0)
                    {
                        setKey = null;
                        return r;
                    }

                    nks.SetKeys.Add(setKey.SetId, setKey);
                    key = setKey.Data;
                }

                keyLength = 0x10000;
                keyPos = 0;
            }
            else
            {
                throw new KeyNotFoundException("-ENOKEY");
            }

            while (size > 0)
            {
                toRead = Math.Min((int)buffer.Length, size);
                var readBytes = nks.BinaryFile.ReadBytes(toRead);

                for (int x = 0; x < toRead; x++)
                {
                    keyPos %= keyLength;
                    buffer[x] ^= key[keyPos];
                    keyPos++;
                }

                outBinaryFile.Write(buffer);

                size -= toRead;
            }

            return 0;
        }

        public static int ExtractFileEntryToBf(Nks nks, NksFileHeader header, BinaryFile outBinaryFile)
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

        #region Key mehods
        public static int NksGet0100Key(int keyIndex, byte[] key, out uint length)
        {
            byte[] retKey;
            length = 0;
            uint retLength = 0;

            if (keyIndex >= 0x20)
                throw new ArgumentException("-ENOKEY");

            if (Nks0100Keys[0][0] == 0)
                Generate0100Keys();

            retKey = Nks0100Keys[keyIndex];
            retLength = 0x10;

            if (key != null)
            {
                key = retKey;
                length = retLength;
            }

            return 0;
        }

        public static int NksCreate0110Key(NksGeneratingKey gk, byte[] buffer)
        {
            int len = buffer.Length;

            if (gk.Key == null)
                throw new ArgumentException("-EINVAL");

            if (gk.KeyLength != 16 && gk.KeyLength != 24 && gk.KeyLength != 32)
                throw new ArgumentException("-EINVAL");

            if (gk.IV == null || gk.IVLength != 16)
                throw new ArgumentException("-EINVAL");

            if (buffer == null || len < 16 || (len & 15) != 0)
                throw new ArgumentException("-EINVAL");

            if (Nks0110BaseKey == null)
                Generate0110BaseKey();

            // AES cipher
            int algoKeySize = 0;
            switch (gk.KeyLength)
            {
                case 16:
                    algoKeySize = 128;
                    break;
                case 24:
                    algoKeySize = 192;
                    break;
                case 32:
                    algoKeySize = 256;
                    break;
                default:
                    return -1;
            }

            var cipher = new Aes(gk.Key, gk.IV);

            var ctr = gk.IV;

            var bp = buffer;
            var bkp = Nks0110BaseKey;

            for (int n = 0; 16 * n < len; n++)
            {
                // gcry_cipher_encrypt (cipher, bp, 16, ctr, 16)
                // gcry_error_t gcry_cipher_encrypt (gcry_cipher_hd_t h, unsigned char *out, size_t outsize, const unsigned char *in, size_t inlen)
                bp = cipher.EncryptToByte(ctr);

                for (int m = 0; m < 16; m++)
                {
                    bp[m] ^= bkp[m];
                }

                IncrementCounter(ctr, 16);

                // bp += 16;
                // bkp += 16;
            }

            return 0;
        }

        public static byte[][] Nks0100Keys = MathUtils.CreateJaggedArray<byte[][]>(32, 16);
        public static byte[] Nks0110BaseKey;

        public static void Generate0100Keys()
        {
            int seed = 0x6ee38fe0;

            for (int key = 0; key < 32; key++)
            {
                for (int n = 0; n < 16; n++)
                {
                    Nks0100Keys[key][n] = (byte)(RandMs(seed) & 0xff);
                }
            }
        }

        // thanks to Iñigo Quilez
        // from http://www.iquilezles.org/www/articles/sfrand/sfrand.htm
        // https://stackoverflow.com/questions/1026327/what-common-algorithms-are-used-for-cs-rand
        public static int RandMs(int seed)
        {
            // seed = seed * 0x343fd + 0x269ec3;
            // return (seed >> 16);

            Random rnd = new Random();
            return rnd.Next(0, 0xff);
        }

        public static void Generate0110BaseKey()
        {
            int seed;

            if (Nks0110BaseKey != null)
                return;

            Nks0110BaseKey = new byte[0x10000];
            seed = 0x608da0a2;

            for (int n = 0; n < 0x10000; n++)
            {
                Nks0110BaseKey[n] = (byte)(RandMs(seed) & 0xff);
            }
        }

        public static void IncrementCounter(byte[] num, int len)
        {
            for (int n = len - 1; n > 0; n--)
            {
                if (++num[n] != 0)
                    break;
            }
        }
        #endregion

        #region Nks Input Output

        public static int NksReadDirectoryHeader(BinaryFile bf, NksDirectoryHeader header)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(NKS_MAGIC_DIRECTORY)) // 0x5e70ac54
                throw new IOException("-EILSEQ;");

            header.Version = bf.ReadUInt16(); // read_u16_le

            header.SetId = bf.ReadUInt32(); // read_u32_le

            if ((header.Unknown0 = bf.ReadBytes(4)).Length != 4)
                throw new IOException("-EIO");

            header.EntryCount = bf.ReadUInt32(); // read_u32_le

            if ((header.Unknown1 = bf.ReadBytes(4)).Length != 4)
                throw new IOException("-EIO");

            switch (header.Version)
            {
                case 0x0100:
                case 0x0110:
                    break;

                default:
                    throw new IOException("-ENOTSUP");
            }

            return 0;
        }
        public static int NksRead0100EntryHeader(BinaryFile bf, Nks0100EntryHeader header)
        {
            header.Name = bf.ReadString(129);

            if ((header.Unknown = bf.ReadBytes(1)).Length != 1)
                throw new IOException("-EIO");

            header.Offset = bf.ReadUInt32(); // read_u32_le

            header.Type = bf.ReadUInt16(); // read_u16_le

            if (header.Type == (UInt16)NksTypeHint.NKS_TH_ENCRYPTED_FILE)
            {
                header.Offset = DecodeOffset(header.Offset);
            }

            return 0;
        }
        public static int NksRead0110EntryHeader(BinaryFile bf, Nks0110EntryHeader header)
        {
            if ((header.Unknown = bf.ReadBytes(2)).Length != 2)
                throw new IOException("-EIO");

            header.Offset = bf.ReadUInt32(); // read_u32_le

            header.Type = bf.ReadUInt16(); // read_u16_le

            header.Name = bf.ReadStringNull(Encoding.Unicode);

            if (header.Type == (UInt16)NksTypeHint.NKS_TH_ENCRYPTED_FILE)
            {
                header.Offset = DecodeOffset(header.Offset);
            }

            return 0;
        }

        public static int NksRead0100NksEntry(BinaryFile bf, NksDirectoryHeader dir, NksEntry entry)
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
        public static int NksRead0110NksEntry(BinaryFile bf, NksDirectoryHeader dir, NksEntry entry)
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
        public static int NksReadEncryptedFileHeader(BinaryFile bf, NksEncryptedFileHeader ret)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(NKS_MAGIC_ENCRYPTED_FILE)) // 0x16ccf80a
                throw new IOException("-EILSEQ");

            ret.Version = bf.ReadUInt16(); // read_u16_le

            ret.SetId = bf.ReadUInt32(); // read_u32_le

            ret.KeyIndex = bf.ReadUInt32(); // read_u32_le

            if ((ret.Unknown1 = bf.ReadBytes(5)).Length != 5)
                throw new IOException("-EIO");

            ret.Size = bf.ReadUInt32(); // read_u32_le

            if ((ret.Unknown2 = bf.ReadBytes(8)).Length != 8)
                throw new IOException("-EIO");

            switch (ret.Version)
            {
                case 0x0100:
                case 0x0110:
                    break;

                default:
                    throw new IOException("-ENOTSUP");
            }

            return 0;
        }
        public static int NksReadFileHeader(BinaryFile bf, NksFileHeader ret)
        {
            UInt32 magic = bf.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(NKS_MAGIC_FILE)) // 0x4916e63c
                throw new IOException("-EILSEQ");

            ret.Version = bf.ReadUInt16(); // read_u16_le

            if ((ret.Unknown1 = bf.ReadBytes(13)).Length != 13)
                throw new IOException("-EIO");

            ret.Size = bf.ReadUInt32(); // read_u32_le

            if ((ret.Unknown2 = bf.ReadBytes(4)).Length != 4)
                throw new IOException("-EIO");

            return 0;
        }

        public static UInt32 DecodeOffset(UInt32 offset)
        {
            return (offset ^ (UInt32)(0x1f4e0c8d));
        }

        public static NksEntryType TypeHintToEntryType(UInt16 typeHint)
        {
            switch (typeHint)
            {
                case (UInt16)NksTypeHint.NKS_TH_DIRECTORY:
                    return NksEntryType.NKS_ENT_DIRECTORY;

                case (UInt16)NksTypeHint.NKS_TH_ENCRYPTED_FILE:
                case (UInt16)NksTypeHint.NKS_TH_FILE:
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

    public class FindEntryContext
    {
        public string Name { get; set; }
        public NksEntry Entry { get; set; }
        public bool Found { get; set; }
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
        NKS_TH_FILE = 3,
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