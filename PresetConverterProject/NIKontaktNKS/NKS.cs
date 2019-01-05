using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommonUtils;

namespace PresetConverterProject.NIKontaktNKS
{
    public class NKS
    {
        public const UInt32 NKS_MAGIC_DIRECTORY = 0x5e70ac54;
        public const UInt32 NKS_MAGIC_ENCRYPTED_FILE = 0x16ccf80a;
        public const UInt32 NKS_MAGIC_FILE = 0x4916e63c;

        // public delegate bool NksTraverseFunc(T1 nks, T2 entry, T3 user_data);
        // public delegate bool NksTraverseFunc<T1, T2>(T1 ent, T2 ctx);

        // private static bool add_entry_to_list(Nks nks, NksEntry entry, IList list)
        public delegate bool NksTraverseFunc(Nks nks, NksEntry entry, IList list);


        // public static bool nks_find_sub_entry(NksEntry ent, FindEntryContext ctx)
        // public delegate bool NksTraverseFunc(NksEntry ent, FindEntryContext ctx);

        public static void TraverseDirectory(string file_name)
        {
            Nks nks = new Nks();
            NksEntry root_entry = new NksEntry();
            root_entry.name = "";
            root_entry.offset = 0;
            root_entry.type = NksEntryType.NKS_ENT_DIRECTORY;

            int r = nks_open(file_name, nks);

            bool ret = !traverse_directory(nks, root_entry, "");
        }

        private static bool add_entry_to_list(Nks nks, NksEntry entry, IList list)
        {
            list.Add(entry);
            return true;
        }

        private static bool traverse_directory(Nks nks, NksEntry dir_entry, string prefix)
        {
            bool ret = true;
            var list = new ArrayList();

            NksTraverseFunc traverseFunc = add_entry_to_list;
            int r = nks_list_dir_entry(nks, dir_entry, traverseFunc, list);
            if (r != 0)
            {
                ret = false;
            }

            if (!traverse_directories(nks, list, prefix))
                ret = false;

            if (!traverse_files(nks, list, prefix))
                ret = false;

            list.Clear();
            return ret;
        }

        private static bool traverse_directories(Nks nks, IList list, string prefix)
        {
            if (list == null)
                return true;

            bool ret = true;

            foreach (NksEntry entry in list)
            {
                if (entry.type != NksEntryType.NKS_ENT_DIRECTORY)
                    continue;

                string prefix_buffer = Path.Join(prefix, entry.name);

                if (!traverse_directory(nks, entry, prefix_buffer))
                    ret = false;
            }

            return ret;
        }

        private static bool traverse_files(Nks nks, IList list, string prefix)
        {
            if (list == null)
                return true;

            bool ret = true;

            foreach (NksEntry entry in list)
            {
                if (entry.type == NksEntryType.NKS_ENT_DIRECTORY)
                    continue;

                if (!traverse_file(nks, entry, prefix))
                    ret = false;
            }

            return ret;
        }

        private static bool traverse_file(Nks nks, NksEntry file_entry, string prefix)
        {
            bool ret = true;
            int r;
            int extr_count = 0;

            Debug.Assert(file_entry.type != NksEntryType.NKS_ENT_DIRECTORY);

            string buffer = Path.Join(prefix, file_entry.name);

            switch (file_entry.type)
            {
                case NksEntryType.NKS_ENT_FILE:

                    r = nks_extract_file_entry(nks, file_entry, buffer);

                    if (r == 0)
                        extr_count++;

                    ret = (r == 0);
                    break;

                default:
                    break;
            }

            return ret;
        }

        /**
         * Opens an archive.  This must be called first, before anything else can be done
         * with archives.
         *
         * @param file_name name of the file to open
         * @param ret	    pointer to a Nks * pointer, which will be initialised upon
         * 		    success.  It has to be closed with nks_close after it is no
         * 	            longer used.
         *
         * @return 0 on success
         */
        public static int nks_open(string file_name, Nks ret)
        {
            BinaryFile fd = new BinaryFile(file_name, BinaryFile.ByteOrder.LittleEndian, false);

            int r = nks_open_fd(fd, ret);
            if (r != 0)
            {
                fd.Close();
                return r;
            }

            return 0;
        }

        /**
         * Similar to nks_open, but uses a file descriptor instead of a file name.  If
         * this function completes successfully, you must not use fd any more directly.
         * Make sure that fd is open in binary mode on systems that distinguish between
         * text and binary.
         */
        public static int nks_open_fd(BinaryFile fd, Nks ret)
        {
            if (fd == null)
                throw new ArgumentException("-EINVAL");

            Nks nks = new Nks();
            nks.root_entry.name = "/";
            nks.root_entry.type = NksEntryType.NKS_ENT_DIRECTORY;
            nks.root_entry.offset = 0;
            nks.fd = fd;
            nks.set_keys = new Dictionary<string, string>();

            ret = nks;

            return 0;
        }

        /**
         * Closes an archive.
         */
        public static void nks_close(Nks nks)
        {
            nks = null;
        }

        /**
         * Lists the contents of a directory in an archive.  It calls func for each
         * entry in the directory.  If func returns false, then then no more entries
         * will be listed.  Do not modify or free the entries provided to func.
         * If you want to copy any of the entries, you must use nks_entry_copy.
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
        public static int nks_list_dir(Nks nks, string dir, NksTraverseFunc func, IList user_data)
        {
            NksEntry ent = new NksEntry();
            int r;

            r = nks_find_entry(nks, dir, ent);
            if (r != 0)
                return r;

            if (ent.type != NksEntryType.NKS_ENT_DIRECTORY)
                throw new DirectoryNotFoundException("-ENOTDIR");

            r = nks_list_dir_entry(nks, ent, func, user_data);
            nks_entry_free(ent);

            return r;
        }

        /**
         * Lists the contents of a directory in an archive.  It is similar to nks_list,
         * but uses a NksEntry instead of a path.  The entry must correspond to a
         * directory and not a file..
         */
        public static int nks_list_dir_entry(Nks nks, NksEntry entry, NksTraverseFunc func, IList user_data)
        {
            NksDirectoryHeader header = new NksDirectoryHeader();
            int r;

            if (entry.type != NksEntryType.NKS_ENT_DIRECTORY)
                throw new DirectoryNotFoundException("-ENOTDIR");

            if (nks.fd.Seek(entry.offset, SeekOrigin.Begin) < 0)
                throw new IOException("-EIO");

            r = nks_read_directory_header(nks.fd, header);
            if (r != 0)
                return r;

            return list_directory(nks, header, func, user_data);
        }

        /**
         * Returns the size of a file in an archive.
         *
         * @param nks   the archive
         * @param entry the entry corresponding to a file in the archive
         *
         * @return the size, or a negative value on error
         */
        public static int nks_file_size(Nks nks, NksEntry entry)
        {
            NksEncryptedFileHeader header = new NksEncryptedFileHeader();
            int r;

            if (nks.fd.Seek(entry.offset, SeekOrigin.Begin) < 0)
                throw new IOException("-EIO");

            UInt32 magic = nks.fd.ReadUInt32(); // read_u32_le

            switch (magic)
            {
                case NKS_MAGIC_ENCRYPTED_FILE:
                    break;

                case NKS_MAGIC_DIRECTORY:
                    throw new DirectoryNotFoundException("-EISDIR");

                default:
                    throw new NotSupportedException("-ENOTSUP");
            }

            if (nks.fd.Seek(entry.offset, SeekOrigin.Begin) < 0)
                throw new IOException("-EIO");

            r = nks_read_encrypted_file_header(nks.fd, header);
            if (r != 0)
                return r;

            return (int)header.size;
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
        public static int nks_extract_file_entry(Nks nks, NksEntry entry, string out_file)
        {
            BinaryFile fd = new BinaryFile(out_file, BinaryFile.ByteOrder.LittleEndian, true);

            int r = nks_extract_file_entry_to_fd(nks, entry, fd);
            fd.Close();

            return r;
        }

        /**
         * Extracts a file from an archive.  This function is similar to
         * nks_extract_entry, but accepts an output file descriptor instead of a file
         * name.
         */
        public static int nks_extract_file_entry_to_fd(Nks nks, NksEntry entry, BinaryFile out_fd)
        {
            NksEncryptedFileHeader enc_header = new NksEncryptedFileHeader();
            NksFileHeader file_header = new NksFileHeader();
            int r;

            if (nks.fd.Seek(entry.offset, SeekOrigin.Begin) < 0)
                throw new IOException("-EIO");

            UInt32 magic = nks.fd.ReadUInt32(); // read_u32_le
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

            if (nks.fd.Seek(entry.offset, SeekOrigin.Begin) < 0)
                throw new IOException("-EIO");

            if (magic == NKS_MAGIC_ENCRYPTED_FILE)
            {
                r = nks_read_encrypted_file_header(nks.fd, enc_header);
                if (r != 0)
                    return r;

                switch (enc_header.version)
                {
                    case 0x0100:
                    case 0x0110:
                        return extract_encrypted_file_entry_to_fd(nks, enc_header, out_fd);

                    default:
                        throw new NotSupportedException("-ENOTSUP");
                }
            }
            else
            {
                r = nks_read_file_header(nks.fd, file_header);
                if (r != 0)
                    return r;

                switch (file_header.version)
                {
                    case 0x0100:
                    case 0x0110:
                        return extract_file_entry_to_fd(nks, file_header, out_fd);

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
         * 	       disposed of with nks_entry_free.
         *
         * @return 0 on success
         */
        public static int nks_find_entry(Nks nks, string name, NksEntry ret)
        {
            NksEntry entry = new NksEntry();
            NksEntry sub_entry = new NksEntry();
            string buffer = new string(new char[512]);
            int r;

            if (name == null || name[0] == '/')
                throw new ArgumentException("-EINVAL");

            nks_entry_copy(nks.root_entry, entry);

            var path_segments = name.Split('/');
            foreach (var path_segment in path_segments)
            {
                r = nks_get_entry(nks, entry, buffer, sub_entry);
                if (r != 0)
                    throw new ArgumentException("-ENOTDIR");

                if (buffer[0] == 0)
                {
                    if (entry.type != NksEntryType.NKS_ENT_DIRECTORY)
                    {
                        throw new ArgumentException("-ENOTDIR");
                    }

                    break;
                }

                r = nks_get_entry(nks, entry, buffer, sub_entry);
                if (r != 0)
                    throw new ArgumentException("-ENOTDIR");

                nks_entry_free(entry);

                entry = sub_entry;
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
         *              disposed of with nks_entry_free.
         *
         * @return 0 on success
         */
        public static int nks_get_entry(Nks nks, NksEntry entry, string name, NksEntry ret)
        {
            FindEntryContext context = new FindEntryContext();
            int r = 0;

            context.name = name;
            context.entry = ret;
            context.found = false;

            // TODO: fix missing implementation
            throw new NotImplementedException();
            // NksTraverseFunc traverseFunc = nks_find_sub_entry;
            // r = nks_list_dir_entry(nks, entry, traverseFunc, context);

            if (r != 0)
                return r;

            if (!context.found)
                throw new KeyNotFoundException("-ENOENT");

            return 0;
        }

        /**
         * Frees an entry.
         */
        public static void nks_entry_free(NksEntry entry)
        {
            entry = null;
        }

        /**
         * Copies an entry.
         *
         * @param src pointer to the entry to copy from
         * @param dst pointer to the entry to copy to.  dst must point to a block
         *            of allocated memory.  When no longer needed, it must be freed with
         *            nks_entry_free.
         */
        public static void nks_entry_copy(NksEntry src, NksEntry dst)
        {
            dst.name = src.name.ToUpper();
            dst.type = src.type;
            dst.offset = src.offset;
        }

        public static int compare_pu32(int a, int b)
        {
            return (a - b);
        }

        public static bool nks_find_sub_entry(NksEntry ent, FindEntryContext ctx)
        {
            if (String.Equals(ent.name, ctx.name, StringComparison.OrdinalIgnoreCase))
            {
                nks_entry_copy(ent, ctx.entry);
                ctx.found = true;
                return false;
            }

            return true;
        }

        public static int list_directory(Nks nks, NksDirectoryHeader header, NksTraverseFunc func, IList user_data)
        {
            long offset;
            NksEntry ent = new NksEntry();
            UInt32 n;
            int r;

            offset = nks.fd.Seek(0, SeekOrigin.Current);
            if (offset < 0)
                throw new IOException("-EIO");

            for (n = 0; n < header.entry_count; n++)
            {
                if (nks.fd.Seek(offset, SeekOrigin.Begin) < 0)
                    throw new IOException("-EIO");

                switch (header.version)
                {
                    case 0x0100:
                        r = nks_read_0100_nks_entry(nks.fd, header, ent);
                        break;

                    case 0x0110:
                        r = nks_read_0110_nks_entry(nks.fd, header, ent);
                        break;

                    default:
                        throw new NotSupportedException("-ENOTSUP");
                }

                if (r != 0)
                    return r;

                offset = nks.fd.Seek(0, SeekOrigin.Current);
                if (offset < 0)
                {
                    nks_entry_free(ent);
                    throw new IOException("-EIO");
                }

                // https://stackoverflow.com/questions/3682366/method-using-funct-tresult-as-parameters
                var f = func(nks, ent, user_data);
                if (!f)
                {
                    nks_entry_free(ent);
                    break;
                }

                nks_entry_free(ent);
            }

            return 0;
        }

        public static int extract_encrypted_file_entry_to_fd(Nks nks, NksEncryptedFileHeader header, BinaryFile out_fd)
        {
            // string buffer = new string(new char[16384]);
            // byte key;
            // uint count;
            // uint size;
            // uint to_read;
            // uint key_length;
            // int key_pos = new int();
            // uint x;
            // int r;

            // if (header.key_index < 0xff)
            // {
            //     if (nks_get_0100_key(header.key_index, key, key_length) != 0)
            //         throw new ArgumentException("-ENOKEY");

            //     //Debug.Assert(key_length == 0x10);

            //     key_pos = lseek(nks.fd, 0, SEEK_CUR);
            //     if (key_pos < 0)
            //         throw new IOException("-EIO");
            // }
            // else if (header.key_index == 0x100)
            // {
            //     NksSetKey set_key;

            //     set_key = g_tree_lookup(nks.set_keys, header.set_id);
            //     if (set_key == null)
            //     {
            //         NksLibraryDesc lib = NKSLibraries.libraries.Where(a => a.id == header.set_id).FirstOrDefault();
            //         if (lib == null)
            //             throw new ArgumentException("-ENOKEY");

            //         set_key = g_malloc(sizeof(set_key));
            //         set_key.set_id = header.set_id;
            //         r = nks_create_0110_key(lib.gen_key, set_key.data, sizeof(byte));
            //         if (r != 0)
            //         {
            //             g_free(set_key);
            //             return r;
            //         }

            //         g_tree_insert(nks.set_keys, set_key.set_id, set_key);
            //     }

            //     key = set_key.data;
            //     key_length = 0x10000;

            //     key_pos = 0;
            // }
            // else
            //     throw new ArgumentException("-ENOKEY");

            // size = header.size;

            // allocate_file_space(out_fd, size);

            // while (size > 0)
            // {
            //     to_read = Math.Min(sizeof(sbyte), size);
            //     count = read(nks.fd, buffer, to_read);
            //     if (count != to_read)
            //         throw new IOException("-EIO");

            //     for (x = 0; x < to_read; x++)
            //     {
            //         key_pos %= key_length;
            //         buffer[x] ^= key[key_pos];
            //         key_pos++;
            //     }

            //     count = write(out_fd, buffer, to_read);
            //     if (count != to_read)
            //         throw new IOException("-EIO");

            //     size -= to_read;
            // }

            return 0;
        }

        public static int extract_file_entry_to_fd(Nks nks, NksFileHeader header, BinaryFile out_fd)
        {
            char[] buffer = new char[16384];
            int to_read;
            int size = (int)header.size;

            while (size > 0)
            {
                to_read = Math.Min((int)buffer.Length, size);
                var readBytes = nks.fd.ReadBytes(to_read);

                out_fd.Write(readBytes);

                size -= to_read;
            }

            return 0;
        }

        #region Keys
        public static int nks_get_0100_key(int key_index, byte[] key, uint length)
        {
            byte[] ret_key;
            uint ret_length = 0;

            if (key_index >= 0x20)
                throw new ArgumentException("-ENOKEY");

            if (nks_0100_keys[0, 0] == 0)
                generate_0100_keys();

            ret_key = nks_0100_keys.SliceRow(key_index).ToArray();
            ret_length = 0x10;

            if (key != null)
                key = ret_key;

            // if (length != null)
            // length = ret_length;

            return 0;
        }
        public static int nks_create_0110_key(NksGeneratingKey gk, UInt32 buffer, uint len)
        {
            // Aes cipher;
            // byte[] ctr = new byte[16];
            // byte bkp;
            // byte bp;
            // uint n;
            // uint m;
            // int algoKeySize;
            // int r;

            // if (gk.key == null)
            //     throw new ArgumentException("-EINVAL");

            // if (gk.key_len != 16 && gk.key_len != 24 && gk.key_len != 32)
            //     throw new ArgumentException("-EINVAL");

            // if (gk.iv == null || gk.iv_len != 16)
            //     throw new ArgumentException("-EINVAL");

            // if (buffer == null || len < 16 || (len & 15) != 0)
            //     throw new ArgumentException("-EINVAL");

            // if (nks_0110_base_key == null)
            //     generate_0110_base_key();

            // switch (gk.key_len)
            // {
            //     case 16:
            //         algoKeySize = 128;
            //         break;
            //     case 24:
            //         algoKeySize = 192;
            //         break;
            //     case 32:
            //         algoKeySize = 256;
            //         break;
            //     default:
            //         return -1;
            // }

            // bp = buffer;
            // bkp = nks_0110_base_key;

            // for (n = 0; 16 * n < len; n++)
            // {
            //     if (gcry_cipher_encrypt(cipher, bp, 16, ctr, 16) != 0)
            //     {
            //         throw new NotSupportedException("-ENOTSUP");
            //     }

            //     for (m = 0; m < 16; m++)
            //         bp[m] ^= bkp[m];

            //     increment_counter(ctr, 16);

            //     bp += 16;
            //     bkp += 16;
            // }

            return 0;
        }

        public static byte[,] nks_0100_keys = new byte[32, 16];
        public static byte[] nks_0110_base_key;

        public static void generate_0100_keys()
        {
            UInt32 seed = 0x6ee38fe0;
            int key;
            int n;

            for (key = 0; key < 32; key++)
            {
                for (n = 0; n < 16; n++)
                    nks_0100_keys[key, n] = (byte)(rand_ms(seed) & 0xff);
            }
        }

        public static UInt32 rand_ms(UInt32 seedp)
        {
            seedp = seedp * 0x343fd + 0x269ec3;
            return (seedp >> 16);
        }

        public static void generate_0110_base_key()
        {
            int n;
            UInt32 seed;

            if (nks_0110_base_key != null)
                return;

            nks_0110_base_key = new byte[0x10000];
            seed = 0x608da0a2;

            for (n = 0; n < 0x10000; n++)
                nks_0110_base_key[n] = (byte)(rand_ms(seed) & 0xff);
        }

        public static void increment_counter(byte[] num, uint len)
        {
            uint n;

            for (n = len - 1; n > 0; n--)
            {
                if (++num[n] != 0)
                    break;
            }
        }
        #endregion

        #region NKS_IO

        public static int nks_read_directory_header(BinaryFile fd, NksDirectoryHeader header)
        {
            UInt32 magic = fd.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(0x5e70ac54))
                throw new IOException("-EILSEQ;");

            header.version = fd.ReadUInt16(); // read_u16_le

            header.set_id = fd.ReadUInt32(); // read_u32_le

            if ((header.unknown_0 = fd.ReadBytes(4)).Length != 4)
                throw new IOException("-EIO");

            header.entry_count = fd.ReadUInt32(); // read_u32_le

            if ((header.unknown_1 = fd.ReadBytes(4)).Length != 4)
                throw new IOException("-EIO");

            switch (header.version)
            {
                case 0x0100:
                case 0x0110:
                    break;

                default:
                    throw new IOException("-ENOTSUP");
            }

            return 0;
        }
        public static int nks_read_0100_entry_header(BinaryFile fd, Nks0100EntryHeader header)
        {
            header.name = fd.ReadString(129);

            if ((header.unknown = fd.ReadBytes(1)).Length != 1)
                throw new IOException("-EIO");

            header.offset = fd.ReadUInt32(); // read_u32_le

            header.type = fd.ReadUInt16(); // read_u16_le

            if (header.type == (UInt16)NksTypeHint.NKS_TH_ENCRYPTED_FILE)
                header.offset = decode_offset(header.offset);

            return 0;
        }
        public static int nks_read_0110_entry_header(BinaryFile fd, Nks0110EntryHeader header)
        {
            if ((header.unknown = fd.ReadBytes(2)).Length != 2)
                throw new IOException("-EIO");

            header.offset = fd.ReadUInt32(); // read_u32_le

            header.type = fd.ReadUInt16(); // read_u16_le

            header.name = fd.ReadStringNull();

            if (header.type == (UInt16)NksTypeHint.NKS_TH_ENCRYPTED_FILE)
                header.offset = decode_offset(header.offset);

            return 0;
        }
        public static void nks_0110_entry_header_free(Nks0110EntryHeader header)
        {
            header = null;
        }

        public static int nks_read_0100_nks_entry(BinaryFile fd, NksDirectoryHeader dir, NksEntry ent)
        {
            Nks0100EntryHeader hdr = new Nks0100EntryHeader();

            int r = nks_read_0100_entry_header(fd, hdr);
            if (r != 0)
                return r;

            ent.name = hdr.name.ToUpper();
            ent.offset = hdr.offset;
            ent.type = type_hint_to_entry_type(hdr.type);

            return 0;
        }
        public static int nks_read_0110_nks_entry(BinaryFile fd, NksDirectoryHeader dir, NksEntry ent)
        {
            Nks0110EntryHeader hdr = new Nks0110EntryHeader();

            int r = nks_read_0110_entry_header(fd, hdr);
            if (r != 0)
                return r;

            ent.name = hdr.name.ToUpper();
            ent.offset = hdr.offset;
            ent.type = type_hint_to_entry_type(hdr.type);

            nks_0110_entry_header_free(hdr);

            return 0;
        }
        public static int nks_read_encrypted_file_header(BinaryFile fd, NksEncryptedFileHeader ret)
        {
            UInt32 magic = fd.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(0x16ccf80a))
                throw new IOException("-EILSEQ");

            ret.version = fd.ReadUInt16(); // read_u16_le

            ret.set_id = fd.ReadUInt32(); // read_u32_le

            ret.key_index = fd.ReadUInt32(); // read_u32_le

            if ((ret.unknown_1 = fd.ReadBytes(5)).Length != 5)
                throw new IOException("-EIO");

            ret.size = fd.ReadUInt32(); // read_u32_le

            if ((ret.unknown_2 = fd.ReadBytes(8)).Length != 8)
                throw new IOException("-EIO");

            switch (ret.version)
            {
                case 0x0100:
                case 0x0110:
                    break;

                default:
                    throw new IOException("-ENOTSUP");
            }

            return 0;
        }
        public static int nks_read_file_header(BinaryFile fd, NksFileHeader ret)
        {
            UInt32 magic = fd.ReadUInt32(); // read_u32_le

            if (magic != (UInt32)(0x4916e63c))
                throw new IOException("-EILSEQ");

            ret.version = fd.ReadUInt16(); // read_u16_le

            if ((ret.unknown_1 = fd.ReadBytes(13)).Length != 13)
                throw new IOException("-EIO");

            ret.size = fd.ReadUInt32(); // read_u32_le

            if ((ret.unknown_2 = fd.ReadBytes(4)).Length != 4)
                throw new IOException("-EIO");

            return 0;
        }

        public static UInt32 decode_offset(UInt32 offset)
        {
            return (offset ^ (UInt32)(0x1f4e0c8d));
        }

        public static NksEntryType type_hint_to_entry_type(UInt16 type_hint)
        {
            switch (type_hint)
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
        public string name;
        public NksEntryType type;
        public UInt32 offset;
    }

    public class NksSetKey
    {
        public UInt32 set_id;
        public byte[] data = new byte[0x10000];
    }

    public class Nks
    {
        public BinaryFile fd;
        public NksEntry root_entry = new NksEntry();
        public Dictionary<string, string> set_keys;
    }

    public class FindEntryContext
    {
        public string name;
        public NksEntry entry;
        public bool found;
    }

    public class NksDirectoryHeader
    {
        public UInt16 version;
        public UInt32 set_id;
        public byte[] unknown_0 = new byte[4];
        public UInt32 entry_count;
        public byte[] unknown_1 = new byte[4];
    }

    public enum NksTypeHint
    {
        NKS_TH_DIRECTORY = 1,
        NKS_TH_ENCRYPTED_FILE = 2,
        NKS_TH_FILE = 3,
    }

    public class Nks0100EntryHeader
    {
        public string name = new string(new char[129]);
        public byte[] unknown = new byte[1];
        public UInt32 offset;
        public UInt16 type;
    }

    public class Nks0110EntryHeader
    {
        public byte[] unknown = new byte[2];
        public UInt32 offset;
        public UInt16 type;
        public string name;
    }

    public class NksFileHeader
    {
        public UInt16 version;
        public byte[] unknown_1 = new byte[13];
        public UInt32 size;
        public byte[] unknown_2 = new byte[4];
    }

    public class NksEncryptedFileHeader
    {
        public UInt16 version;
        public UInt32 set_id;
        public UInt32 key_index;
        public byte[] unknown_1 = new byte[5];
        public UInt32 size;
        public byte[] unknown_2 = new byte[8];
    }

    static partial class DefineConstants
    {
        public const string PACKAGE_PREFIX = "com.native-instruments";
        public const string REG_PATH = "Software\\Native Instruments";
        public const string SEP = "\\";
        public const int O_BINARY = 0;
    }
}