using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CommonUtils;
using Serilog;

namespace PresetConverterProject.NIKontaktNKS
{
    #region Help Sources
    // https://www.silabs.com/content/usergenerated/asi/cloud/attachments/siliconlabs/en/community/groups/interface/knowledge-base/jcr:content/content/primary/blog/executing_c_dll_func-u4Wl/Creating%20a%20C%23%20Module%20From%20a%20DLL%20Header%20File.pdf
    // https://riptutorial.com/csharp/example/17244/dynamic-loading-and-unloading-of-unmanaged-dlls
    // https://stackoverflow.com/questions/13834153/how-to-call-unmanaged-dll-to-populate-struct-in-c-sharp-using-a-pointer
    // https://www.daniweb.com/programming/software-development/threads/406891/marshalling-c-structures-from-a-dynamically-loaded-dll

    // https://stackoverflow.com/questions/17020464/c-sharp-calling-c-dll-function-which-returns-a-struct
    // [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    // public struct FVersionInfo
    // {
    //     public IntPtr Version;
    //     public IntPtr Build_No;
    //     public IntPtr Build_Type;
    //     public IntPtr Build_Date;
    //     public IntPtr Build_Info;
    //     public IntPtr Comment;
    // }
    // So i passed the Marshal.GetDelegateForFunctionPointer without any problems.
    // 
    // I changed my using code to:
    // 
    // GF.FVersionInfo vi = new GF.FVersionInfo();
    // vi = gf.GetVersion();
    // 
    // After that, i could access the strings for example with
    // string MyVersion = Marshal.PtrToStringAnsi(VersionInfos.Version);

    // https://studiofreya.com/2016/02/16/how-to-dynamically-load-native-dlls-from-csharp/
    // Delegate with function signature for the GetVersion function:
    // uint32_t GetVersion(char * buffer, uint32_t length)
    // 
    // [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    // [return: MarshalAs(UnmanagedType.U4)]
    // delegate UInt32 GetVersionDelegate(
    //    [OutAttribute][InAttribute] StringBuilder versionString,
    //    [OutAttribute] UInt32 length);

    // https://stackoverflow.com/questions/2224164/help-me-convert-c-structure-into-c-sharp
    // #define QUE_ADDR_BUF_LENGTH 50
    // #define QUE_POST_BUF_LENGTH 11

    // typedef struct
    // {
    // const WCHAR *streetAddress;
    // const WCHAR *city;
    // const WCHAR *state;
    // const WCHAR *country;
    // const WCHAR *postalCode;
    // } QueSelectAddressType;
    // 
    // typedef struct
    // {
    // WCHAR   streetAddress[QUE_ADDR_BUF_LENGTH + 1];
    // WCHAR   city[QUE_ADDR_BUF_LENGTH + 1];
    // WCHAR   state[QUE_ADDR_BUF_LENGTH + 1];
    // WCHAR   country[QUE_ADDR_BUF_LENGTH + 1];
    // WCHAR   postalCode[QUE_POST_BUF_LENGTH + 1];
    // } QueAddressType;

    // [StructLayout(LayoutKind.Sequential)]
    // public struct QueSelectAddressType
    // {
    //     [MarshalAsAttribute(UnmanagedType.LPWStr)]
    //     public string streetAddress;
    //     [MarshalAsAttribute(UnmanagedType.LPWStr)]
    //     public string city;
    //     [MarshalAsAttribute(UnmanagedType.LPWStr)]
    //     public string state;
    //     [MarshalAsAttribute(UnmanagedType.LPWStr)]
    //     public string country;
    //     [MarshalAsAttribute(UnmanagedType.LPWStr)]
    //     public string postalCode;
    // };

    // [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    // public struct QueAddressType
    // {
    //     [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 51)]
    //     public string streetAddress;
    //     [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 51)]
    //     public string city;
    //     [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 51)]
    //     public string state;
    //     [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 51)]
    //     public string country;
    //     [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 12)]
    //     public string postalCode;
    // };
    #endregion

    public static class NativeMethods
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibrary(string fileName);

        // [DllImport("kernel32", SetLastError = true)]
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr module, string procedureName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        public static string GetLibraryPathName(string filePath)
        {
            // If 64-bit process, load 64-bit DLL
            bool is64bit = System.Environment.Is64BitProcess;

            // default is 32 bit
            // like 7zip.wcx
            string suffix = "";

            if (is64bit)
            {
                // the 64 bit version is 7zip.wcx64
                suffix = "64";
            }

            var libPath = filePath + suffix;
            return libPath;
        }
    }

    #region Structs

    // https://docs.microsoft.com/en-us/dotnet/framework/interop/default-marshaling-for-strings
    // Ansi: [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    // char *  f1;                      => [MarshalAs(UnmanagedType.LPStr)] public string f1;
    // char    f2[256];                 => [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string f2;

    // Unicode: [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    // WCHAR * f1;                      => [MarshalAs(UnmanagedType.LPWStr)] public string f1;
    // WCHAR   f2[256];                 => [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string f2;
    // BSTR    f3;                      => [MarshalAs(UnmanagedType.BStr)] public string f3;

    // Auto: [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    // TCHAR * f1;                      => [MarshalAs(UnmanagedType.LPTStr)] public string f1;
    // TCHAR   f2[256];                 => [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string f2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] // charset is Ansi not Unicode
    public struct tHeaderData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string ArcName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string FileName;
        public int Flags;
        public int PackSize;
        public int UnpSize;
        public int HostOS;
        public int FileCRC;
        public int FileTime;
        public int UnpVer;
        public int Method;
        public int FileAttr;
        [MarshalAs(UnmanagedType.LPStr)] public string CmtBuf;
        public int CmtBufSize;
        public int CmtSize;
        public int CmtState;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct tHeaderDataExW
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)] public string ArcName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)] public string FileName;
        public int Flags;
        public uint PackSize;
        public uint PackSizeHigh;
        public uint UnpSize;
        public uint UnpSizeHigh;
        public int HostOS;
        public int FileCRC;
        public int FileTime;
        public int UnpVer;
        public int Method;
        public int FileAttr;
        [MarshalAs(UnmanagedType.LPStr)] public string CmtBuf;
        public int CmtBufSize;
        public int CmtSize;
        public int CmtState;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)] public string Reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct tOpenArchiveData
    {
        [MarshalAs(UnmanagedType.LPStr)] public string ArcName;
        public int OpenMode;
        public int OpenResult;
        [MarshalAs(UnmanagedType.LPStr)] public string CmtBuf;
        public int CmtBufSize;
        public int CmtSize;
        public int CmtState;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct tOpenArchiveDataW
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string ArcName;
        public int OpenMode;
        public int OpenResult;
        [MarshalAs(UnmanagedType.LPWStr)] public string CmtBuf;
        public int CmtBufSize;
        public int CmtSize;
        public int CmtState;
    };
    #endregion

    #region Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr OpenArchiveDelegate([In, Out] ref tOpenArchiveData ArchiveData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr OpenArchiveDelegateW([In, Out] ref tOpenArchiveDataW ArchiveData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    delegate int GetPackerCapsDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    delegate int ReadHeaderDelegate(IntPtr hArcData, [In, Out] ref tHeaderData HeaderData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    delegate int ReadHeaderDelegateEx(IntPtr hArcData, [In, Out] ref tHeaderDataExW HeaderData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    delegate int ReadHeaderDelegateExW(IntPtr hArcData, [In, Out] ref tHeaderDataExW HeaderData);

    // 
    // Add a [MarshalAs(UnmanagedType.LPWStr)] attribute to the parameter in your delegate declaration in order for String to get converted into wchar_t* :
    //     delegate void MyDelegate([MarshalAs(UnmanagedType.LPWStr)] string foo)
    // 
    // To pass a modifiable string, give a StringBuilder. You need to explicitly reserve space for the unmanaged function to work with:
    //     delegate void MyDelegate([MarshalAs(UnmanagedType.LPWStr)] StringBuilder foo)
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    // delegate int ProcessFileDelegate(IntPtr hArcData, int Operation, [MarshalAs(UnmanagedType.LPStr)] StringBuilder DestPath, [MarshalAs(UnmanagedType.LPStr)] StringBuilder DestName);
    delegate int ProcessFileDelegate(IntPtr hArcData, int Operation, [MarshalAs(UnmanagedType.LPStr)] String DestPath, [MarshalAs(UnmanagedType.LPStr)] String DestName);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    // delegate int ProcessFileDelegateW(IntPtr hArcData, int Operation, IntPtr DestPath, IntPtr DestName);
    // delegate int ProcessFileDelegateW(IntPtr hArcData, int Operation, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder DestPath, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder DestName);
    delegate int ProcessFileDelegateW(IntPtr hArcData, int Operation, [MarshalAs(UnmanagedType.LPWStr)] string DestPath, [MarshalAs(UnmanagedType.LPWStr)] string DestName);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    // delegate int PackFilesDelegateW([MarshalAs(UnmanagedType.LPWStr)] string PackedFile, [MarshalAs(UnmanagedType.LPWStr)] string SubPath, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder SrcPath, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder AddList, int Flags);
    delegate int PackFilesDelegateW([MarshalAs(UnmanagedType.LPWStr)] string PackedFile, [MarshalAs(UnmanagedType.LPWStr)] string SubPath, [MarshalAs(UnmanagedType.LPWStr)] string SrcPath, [MarshalAs(UnmanagedType.LPWStr)] string AddList, int Flags);


    // delegates with call back methods
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int tChangeVolProcW([MarshalAs(UnmanagedType.LPWStr)] string ArcName, int Mode);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int tProcessDataProcW([MarshalAs(UnmanagedType.LPWStr)] string FileName, int Size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void SetChangeVolProcDelegateW(IntPtr hArcData, tChangeVolProcW pChangeVolProc);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void SetProcessDataProcDelegateW(IntPtr hArcData, tProcessDataProcW pProcessDataProc);

    #endregion


    // Most of this is ported from the work by Oleg Bondar <hobo-mts@mail.ru>,
    // wcxtest-0.23
    public static class WCXUtils
    {
        #region Enums        
        // https://github.com/thpatch/thtk/blob/master/contrib/wcxhead.h

        // Error codes returned to calling application
        public enum ErrorCodes
        {
            Success = 0, // Success
            NoMoreFilesInArchive = 10, // No more files in archive
            NotEnoughMemory = 11, // Not enough memory
            DataIsBad = 12, // Data is bad
            CrcErrorInArchiveData = 13, // CRC error in archive data
            ArchiveFormatUnknown = 14, // Archive format unknown
            CannotOpenExistingFile = 15, // Cannot open existing file
            CannotCreateFile = 16, // Cannot create file
            ErrorClosingFile = 17, // Error closing file
            ErrorReadingFromFile = 18, // Error reading from file
            ErrorWritingToFile = 19, // Error writing to file
            BufferTooSmall = 20, // Buffer too small
            FunctionAbortedByUser = 21, // Function aborted by user
            NoFilesFound = 22, // No files found
            TooManyFilesToPack = 23, // Too many files to pack
            FunctionNotSupported = 24, // Function not supported
            HandledError = -32769,   // Handled error
            UnknownError = 32768,   // Unknown error
        }

        // Unpacking flags
        public enum OpenArchiveFlags
        {
            PK_OM_LIST = 0,
            PK_OM_EXTRACT = 1
        }

        // Flags for ProcessFile
        public enum ProcessFileFlags
        {
            PK_SKIP = 0, // Skip file (no unpacking)
            PK_TEST = 1, // Test file integrity
            PK_EXTRACT = 2 // Extract file to disk
        }

        // Flags passed through ChangeVolProc
        public enum ChangeVolProcFlags
        {
            PK_VOL_ASK = 0, // Ask user for location of next volume
            PK_VOL_NOTIFY = 1 // Notify app that next volume will be unpacked
        }

        // Packing flags

        // For PackFiles
        public enum PackFilesFlags
        {
            PK_PACK_MOVE_FILES = 1, // Delete original after packing
            PK_PACK_SAVE_PATHS = 2, // Save path names of files
            PK_PACK_ENCRYPT = 4 // Ask user for password, then encrypt
        }

        // Returned by GetPackCaps
        public enum PackCapsFlags
        {
            PK_CAPS_NEW = 1, // Can create new archives
            PK_CAPS_MODIFY = 2, // Can modify existing archives
            PK_CAPS_MULTIPLE = 4, // Archive can contain multiple files
            PK_CAPS_DELETE = 8, // Can delete files
            PK_CAPS_OPTIONS = 16, // Supports the options dialogbox
            PK_CAPS_MEMPACK = 32, // Supports packing in memory
            PK_CAPS_BY_CONTENT = 64, // Detect archive type by content
            PK_CAPS_SEARCHTEXT = 128, // Allow searching for text in archives, created with this plugin
            PK_CAPS_HIDE = 256, // Show as normal files (hide packer icon) open with Ctrl+PgDn, not Enter 
            PK_CAPS_ENCRYPT = 512 // Plugin supports PK_PACK_ENCRYPT option 
        }

        public const int BACKGROUND_UNPACK = 1; // Which operations are thread-safe? 
        public const int BACKGROUND_PACK = 2;
        public const int BACKGROUND_MEMPACK = 4; // For tar.pluginext in background 

        // Flags for packing in memory
        public const int MEM_OPTIONS_WANTHEADERS = 1; // Return archive headers with packed data

        // Errors returned by PackToMem
        public enum PackToMemErrors
        {
            MEMPACK_OK = 0, // Function call finished OK, but there is more data
            MEMPACK_DONE = 1 // Function call finished OK, there is no more data
        }

        // Flags for PkCryptProc callback
        public enum PkCryptProcFlags
        {
            PK_CRYPT_SAVE_PASSWORD = 1,
            PK_CRYPT_LOAD_PASSWORD = 2,
            PK_CRYPT_LOAD_PASSWORD_NO_UI = 3, // Load password only if master password has already been entered!
            PK_CRYPT_COPY_PASSWORD = 4, // Copy encrypted password to new archive name
            PK_CRYPT_MOVE_PASSWORD = 5, // Move password when renaming an archive
            PK_CRYPT_DELETE_PASSWORD = 6, // Delete password
            PK_CRYPTOPT_MASTERPASS_SET = 1   // The user already has a master password defined
        }

        public enum TodoOperations
        {
            TODO_FLIST = 0, // list plgin functions
            TODO_LIST = 1, // list archive contents
            TODO_TEST = 2, // test archive contents
            TODO_UNPACK = 3, // extract archive contents,
            TODO_PACK = 4 // pack files into archive
        }
        #endregion

        private static int ChangeVol(string ArcName, int Mode)
        {
            int rc = 0; // 1 is success

            switch (Mode)
            {
                case (int)ChangeVolProcFlags.PK_VOL_ASK:

                    string readFromConsole;
                    do
                    {
                        Console.WriteLine("Please change disk and enter Yes (y) or No (n) to stop: ");
                        readFromConsole = Console.ReadLine();
                        var changeDiskLower = readFromConsole?.ToLower();
                        if ((changeDiskLower == "y") || (changeDiskLower == "n"))
                            break;
                    } while (true);

                    rc = (readFromConsole.ToLower() == "y" ? 1 : 0);
                    break;

                case (int)ChangeVolProcFlags.PK_VOL_NOTIFY:
                    Log.Information("Processing next volume/diskette");
                    rc = 1;
                    break;

                default:
                    Log.Error("Unknown ChangeVolProc mode: " + Mode);
                    rc = 0;
                    break;
            }

            return rc;
        }

        private static int ProcessData(string FileName, int Size)
        {
            // always return 1 (success)
            return 1;
        }

        public static bool Call64BitWCXPlugin(string wcxPath, string archiveName, string outputDirectoryPath, TodoOperations openTodo)
        {
            const bool DEBUG = false;
            IntPtr fModuleHandle = IntPtr.Zero;

            // store filename
            string wcxFileName = Path.GetFileName(wcxPath);

            try
            {
                // load library
                fModuleHandle = NativeMethods.LoadLibrary(wcxPath);

                // error handling
                if (fModuleHandle == IntPtr.Zero)
                {
                    Log.Error("Failed opening {0}", wcxPath);
                    return false;
                }
                else
                {
                    if (DEBUG)
                    {
                        Log.Debug("WCX module loaded '{0}' at {1}.", wcxFileName, fModuleHandle);
                    }
                    else
                    {
                        Log.Information("WCX module loaded '{0}'.", wcxFileName);
                    }
                }

                // mandatory functions
                IntPtr pOpenArchive = NativeMethods.GetProcAddress(fModuleHandle, "OpenArchive");
                IntPtr pReadHeader = NativeMethods.GetProcAddress(fModuleHandle, "ReadHeader");
                IntPtr pReadHeaderEx = NativeMethods.GetProcAddress(fModuleHandle, "ReadHeaderEx");
                IntPtr pProcessFile = NativeMethods.GetProcAddress(fModuleHandle, "ProcessFile");
                IntPtr pCloseArchive = NativeMethods.GetProcAddress(fModuleHandle, "CloseArchive");

                // Unicode
                IntPtr pOpenArchiveW = NativeMethods.GetProcAddress(fModuleHandle, "OpenArchiveW");
                IntPtr pReadHeaderExW = NativeMethods.GetProcAddress(fModuleHandle, "ReadHeaderExW");
                IntPtr pProcessFileW = NativeMethods.GetProcAddress(fModuleHandle, "ProcessFileW");

                // Optional functions
                IntPtr pPackFiles = NativeMethods.GetProcAddress(fModuleHandle, "PackFiles");
                IntPtr pDeleteFiles = NativeMethods.GetProcAddress(fModuleHandle, "DeleteFiles");
                IntPtr pGetPackerCaps = NativeMethods.GetProcAddress(fModuleHandle, "GetPackerCaps");
                IntPtr pConfigurePacker = NativeMethods.GetProcAddress(fModuleHandle, "ConfigurePacker");
                IntPtr pSetChangeVolProc = NativeMethods.GetProcAddress(fModuleHandle, "SetChangeVolProc");
                IntPtr pSetProcessDataProc = NativeMethods.GetProcAddress(fModuleHandle, "SetProcessDataProc");
                IntPtr pStartMemPack = NativeMethods.GetProcAddress(fModuleHandle, "StartMemPack");
                IntPtr pPackToMem = NativeMethods.GetProcAddress(fModuleHandle, "PackToMem");
                IntPtr pDoneMemPack = NativeMethods.GetProcAddress(fModuleHandle, "DoneMemPack");
                IntPtr pCanYouHandleThisFile = NativeMethods.GetProcAddress(fModuleHandle, "CanYouHandleThisFile");
                IntPtr pPackSetDefaultParams = NativeMethods.GetProcAddress(fModuleHandle, "PackSetDefaultParams");
                IntPtr pPkSetCryptCallback = NativeMethods.GetProcAddress(fModuleHandle, "PkSetCryptCallback");
                IntPtr pGetBackgroundFlags = NativeMethods.GetProcAddress(fModuleHandle, "GetBackgroundFlags");

                // Unicode
                IntPtr pSetChangeVolProcW = NativeMethods.GetProcAddress(fModuleHandle, "SetChangeVolProcW");
                IntPtr pSetProcessDataProcW = NativeMethods.GetProcAddress(fModuleHandle, "SetProcessDataProcW");
                IntPtr pPackFilesW = NativeMethods.GetProcAddress(fModuleHandle, "PackFilesW");
                IntPtr pDeleteFilesW = NativeMethods.GetProcAddress(fModuleHandle, "DeleteFilesW");
                IntPtr pStartMemPackW = NativeMethods.GetProcAddress(fModuleHandle, "StartMemPackW");
                IntPtr pCanYouHandleThisFileW = NativeMethods.GetProcAddress(fModuleHandle, "CanYouHandleThisFileW");
                IntPtr pPkSetCryptCallbackW = NativeMethods.GetProcAddress(fModuleHandle, "PkSetCryptCallbackW");

                // Extension API
                IntPtr pExtensionInitialize = NativeMethods.GetProcAddress(fModuleHandle, "ExtensionInitialize");
                IntPtr pExtensionFinalize = NativeMethods.GetProcAddress(fModuleHandle, "ExtensionFinalize");

                if (openTodo == TodoOperations.TODO_FLIST)
                {
                    Log.Information("Exported WCX functions in {0}:", wcxFileName);
                    Log.Information("Checking mandatory functions ..");

                    // mandatory functions
                    if (pOpenArchive != IntPtr.Zero) { Log.Information("{0} found at {1}", "OpenArchive", pOpenArchive); }
                    if (pReadHeader != IntPtr.Zero) { Log.Information("{0} found at {1}", "ReadHeader", pReadHeader); }
                    if (pReadHeaderEx != IntPtr.Zero) { Log.Information("{0} found at {1}", "ReadHeaderEx", pReadHeaderEx); }
                    if (pProcessFile != IntPtr.Zero) { Log.Information("{0} found at {1}", "ProcessFile", pProcessFile); }
                    if (pCloseArchive != IntPtr.Zero) { Log.Information("{0} found at {1}", "CloseArchive", pCloseArchive); }

                    // Unicode
                    if (pOpenArchiveW != IntPtr.Zero) { Log.Information("{0} found at {1}", "OpenArchiveW", pOpenArchiveW); }
                    if (pReadHeaderExW != IntPtr.Zero) { Log.Information("{0} found at {1}", "ReadHeaderExW", pReadHeaderExW); }
                    if (pProcessFileW != IntPtr.Zero) { Log.Information("{0} found at {1}", "ProcessFileW", pProcessFileW); }

                    // Optional functions
                    Log.Information("-------------------------------");
                    Log.Information("Checking optional functions ...");
                    if (pPackFiles != IntPtr.Zero) { Log.Information("{0} found at {1}", "PackFiles", pPackFiles); }
                    if (pDeleteFiles != IntPtr.Zero) { Log.Information("{0} found at {1}", "DeleteFiles", pDeleteFiles); }
                    if (pGetPackerCaps != IntPtr.Zero) { Log.Information("{0} found at {1}", "GetPackerCaps", pGetPackerCaps); }
                    if (pConfigurePacker != IntPtr.Zero) { Log.Information("{0} found at {1}", "ConfigurePacker", pConfigurePacker); }
                    if (pSetChangeVolProc != IntPtr.Zero) { Log.Information("{0} found at {1}", "SetChangeVolProc", pSetChangeVolProc); }
                    if (pSetProcessDataProc != IntPtr.Zero) { Log.Information("{0} found at {1}", "SetProcessDataProc", pSetProcessDataProc); }
                    if (pStartMemPack != IntPtr.Zero) { Log.Information("{0} found at {1}", "StartMemPack", pStartMemPack); }
                    if (pPackToMem != IntPtr.Zero) { Log.Information("{0} found at {1}", "PackToMem", pPackToMem); }
                    if (pDoneMemPack != IntPtr.Zero) { Log.Information("{0} found at {1}", "DoneMemPack", pDoneMemPack); }
                    if (pCanYouHandleThisFile != IntPtr.Zero) { Log.Information("{0} found at {1}", "CanYouHandleThisFile", pCanYouHandleThisFile); }
                    if (pPackSetDefaultParams != IntPtr.Zero) { Log.Information("{0} found at {1}", "PackSetDefaultParams", pPackSetDefaultParams); }
                    if (pPkSetCryptCallback != IntPtr.Zero) { Log.Information("{0} found at {1}", "PkSetCryptCallback", pPkSetCryptCallback); }
                    if (pGetBackgroundFlags != IntPtr.Zero) { Log.Information("{0} found at {1}", "GetBackgroundFlags", pGetBackgroundFlags); }

                    // Unicode
                    if (pSetChangeVolProcW != IntPtr.Zero) { Log.Information("{0} found at {1}", "SetChangeVolProcW", pSetChangeVolProcW); }
                    if (pSetProcessDataProcW != IntPtr.Zero) { Log.Information("{0} found at {1}", "SetProcessDataProcW", pSetProcessDataProcW); }
                    if (pPackFilesW != IntPtr.Zero) { Log.Information("{0} found at {1}", "PackFilesW", pPackFilesW); }
                    if (pDeleteFilesW != IntPtr.Zero) { Log.Information("{0} found at {1}", "DeleteFilesW", pDeleteFilesW); }
                    if (pStartMemPackW != IntPtr.Zero) { Log.Information("{0} found at {1}", "StartMemPackW", pStartMemPackW); }
                    if (pCanYouHandleThisFileW != IntPtr.Zero) { Log.Information("{0} found at {1}", "CanYouHandleThisFileW", pCanYouHandleThisFileW); }
                    if (pPkSetCryptCallbackW != IntPtr.Zero) { Log.Information("{0} found at {1}", "PkSetCryptCallbackW", pPkSetCryptCallbackW); }

                    // Extension API
                    if (pExtensionInitialize != IntPtr.Zero) { Log.Information("{0} found at {1}", "ExtensionInitialize", pExtensionInitialize); }
                    if (pExtensionFinalize != IntPtr.Zero) { Log.Information("{0} found at {1}", "ExtensionFinalize", pExtensionFinalize); }

                    Log.Information("-------------------------------");
                    GetPackerCapsDelegate GetPackerCaps = null;
                    if (pGetPackerCaps != IntPtr.Zero)
                    {
                        GetPackerCaps = (GetPackerCapsDelegate)Marshal.GetDelegateForFunctionPointer(
                                pGetPackerCaps,
                                typeof(GetPackerCapsDelegate));
                    }

                    if (GetPackerCaps != null)
                    {
                        int pc = GetPackerCaps();
                        int f = 0;

                        using (var writer = new StringWriter())
                        {
                            writer.Write("PackerCaps: {0} = ", pc);
                            if ((pc & (int)PackCapsFlags.PK_CAPS_NEW) != 0) { writer.Write("{0} PK_CAPS_NEW", f == 1 ? " |" : ""); f = 1; }
                            if ((pc & (int)PackCapsFlags.PK_CAPS_MODIFY) != 0) { writer.Write("{0} PK_CAPS_MODIFY", f == 1 ? " |" : ""); f = 1; }
                            if ((pc & (int)PackCapsFlags.PK_CAPS_MULTIPLE) != 0) { writer.Write("{0} PK_CAPS_MULTIPLE", f == 1 ? " |" : ""); f = 1; }
                            if ((pc & (int)PackCapsFlags.PK_CAPS_DELETE) != 0) { writer.Write("{0} PK_CAPS_DELETE", f == 1 ? " |" : ""); f = 1; }
                            if ((pc & (int)PackCapsFlags.PK_CAPS_OPTIONS) != 0) { writer.Write("{0} PK_CAPS_OPTIONS", f == 1 ? " |" : ""); f = 1; }
                            if ((pc & (int)PackCapsFlags.PK_CAPS_MEMPACK) != 0) { writer.Write("{0} PK_CAPS_MEMPACK", f == 1 ? " |" : ""); f = 1; }
                            if ((pc & (int)PackCapsFlags.PK_CAPS_BY_CONTENT) != 0) { writer.Write("{0} PK_CAPS_BY_CONTENT", f == 1 ? " |" : ""); f = 1; }
                            if ((pc & (int)PackCapsFlags.PK_CAPS_SEARCHTEXT) != 0) { writer.Write("{0} PK_CAPS_SEARCHTEXT", f == 1 ? " |" : ""); f = 1; }
                            if ((pc & (int)PackCapsFlags.PK_CAPS_HIDE) != 0) { writer.Write("{0} PK_CAPS_HIDE", f == 1 ? " |" : ""); f = 1; }

                            Log.Information(writer.ToString());
                        }
                    }

                    GetPackerCaps = null;
                    return true;
                }


                // check that the mandatory methods are in place
                bool Result = (pOpenArchive != IntPtr.Zero) && (pReadHeader != IntPtr.Zero) && (pProcessFile != IntPtr.Zero);
                if (!Result)
                {
                    pOpenArchive = IntPtr.Zero;
                    pReadHeader = IntPtr.Zero;
                    pProcessFile = IntPtr.Zero;
                    Result = (pOpenArchiveW != IntPtr.Zero) && (pReadHeaderExW != IntPtr.Zero) && (pProcessFileW != IntPtr.Zero);
                }

                if (!Result || pCloseArchive == IntPtr.Zero)
                {
                    pOpenArchiveW = IntPtr.Zero;
                    pReadHeaderExW = IntPtr.Zero;
                    pProcessFileW = IntPtr.Zero;
                    pCloseArchive = IntPtr.Zero;
                    Log.Error("Missing mandatory functions (OpenArchive, ReadHeader or ProcessFile)!");
                    return false;
                }

                // add callback methods
                SetChangeVolProcDelegateW SetChangeVolProcW = null;
                if (pSetChangeVolProcW != IntPtr.Zero)
                {
                    SetChangeVolProcW = (SetChangeVolProcDelegateW)Marshal.GetDelegateForFunctionPointer(
                            pSetChangeVolProcW,
                            typeof(SetChangeVolProcDelegateW));
                }

                SetProcessDataProcDelegateW SetProcessDataProcW = null;
                if (pSetProcessDataProcW != IntPtr.Zero)
                {
                    SetProcessDataProcW = (SetProcessDataProcDelegateW)Marshal.GetDelegateForFunctionPointer(
                            pSetProcessDataProcW,
                            typeof(SetProcessDataProcDelegateW));
                }

                tChangeVolProcW pChangeVolProc = new tChangeVolProcW(ChangeVol);
                tProcessDataProcW pProcessDataProc = new tProcessDataProcW(ProcessData);

                // set the callback methods using IntPtr.Zero
                if (SetChangeVolProcW != null) SetChangeVolProcW(IntPtr.Zero, pChangeVolProc);
                if (SetProcessDataProcW != null) SetProcessDataProcW(IntPtr.Zero, pProcessDataProc);

                // pack
                if (openTodo == TodoOperations.TODO_PACK)
                {
                    // PackFiles unicode version
                    PackFilesDelegateW PackFilesW = null;
                    if (pPackFilesW != IntPtr.Zero)
                    {
                        PackFilesW = (PackFilesDelegateW)Marshal.GetDelegateForFunctionPointer(
                                pPackFilesW,
                                typeof(PackFilesDelegateW));
                    }

                    if (PackFilesW != null)
                    {
                        string libraryName = Path.GetFileNameWithoutExtension(archiveName);
                        string outputFileName = libraryName + ".nicnt";
                        string outputFilePath = Path.Combine(outputDirectoryPath, outputFileName);
                        // string outputFilePath = archiveName;
                        Log.Information("Packing into file {0} ...", outputFilePath);

                        // don't use sub path
                        string subPath = null;

                        // ensure that the src path ends with the /
                        // string srcPath = outputDirectoryPath;
                        string srcPath = archiveName;
                        if (!srcPath.EndsWith(Path.DirectorySeparatorChar))
                        {
                            srcPath += Path.DirectorySeparatorChar;
                        }

                        // build add list
                        // SrcPath contains path to the files in AddList. 
                        // SrcPath and AddList together specify files that are to be packed into PackedFile. 
                        // Each string in AddList is zero-delimited (ends in zero), 
                        // and the AddList string ends with an extra zero byte, i.e. there are two zero bytes at the end of AddList.
                        // Example: 
                        // string addList = "ClipExample1.stl" + char.MinValue + "ClipExample2.stl" + char.MinValue + char.MinValue;
                        string addList = "";

                        // add mandatory xml document
                        string libraryXmlFileName = libraryName + ".xml";
                        if (File.Exists(Path.Combine(srcPath, libraryXmlFileName)))
                        {
                            addList += libraryXmlFileName + char.MinValue;
                        }
                        else
                        {
                            // this will fail
                            Log.Error("Failed packing - mandatory library xml file is missing!");
                            return false;
                        }

                        // check if contentversion txt exists
                        string contentVersionFileName = "ContentVersion.txt";
                        if (File.Exists(Path.Combine(srcPath, contentVersionFileName)))
                        {
                            addList += contentVersionFileName + char.MinValue;
                        }

                        // and add the files in the Resources dir 
                        string resourcesDirectoryPath = Path.Combine(srcPath, "Resources");
                        if (Directory.Exists(resourcesDirectoryPath))
                        {
                            var resourcesFilePaths = Directory.GetFiles(resourcesDirectoryPath, "*.*", SearchOption.AllDirectories);
                            foreach (var resourcesFilePath in resourcesFilePaths)
                            {
                                // remove src path
                                string addFilename = resourcesFilePath;
                                if (resourcesFilePath.StartsWith(srcPath))
                                {
                                    addFilename = resourcesFilePath.Substring(srcPath.Length);
                                }
                                addList += addFilename + char.MinValue;
                            }
                        }
                        // ensure to end with another \0
                        addList += char.MinValue;

                        int result = PackFilesW(outputFilePath, subPath, srcPath, addList, (int)PackFilesFlags.PK_PACK_SAVE_PATHS);
                    }
                }
                else
                {
                    // open an archive and process its

                    OpenArchiveDelegateW OpenArchiveW = null;
                    if (pOpenArchiveW != IntPtr.Zero)
                    {
                        OpenArchiveW = (OpenArchiveDelegateW)Marshal.GetDelegateForFunctionPointer(
                                pOpenArchiveW,
                                typeof(OpenArchiveDelegateW));
                    }

                    if (OpenArchiveW != null)
                    {
                        var arcdW = new tOpenArchiveDataW();
                        arcdW.ArcName = archiveName;

                        switch (openTodo)
                        {
                            case TodoOperations.TODO_LIST:
                            case TodoOperations.TODO_PACK:
                                arcdW.OpenMode = (int)OpenArchiveFlags.PK_OM_LIST;
                                break;

                            case TodoOperations.TODO_TEST:
                            case TodoOperations.TODO_UNPACK:
                                arcdW.OpenMode = (int)OpenArchiveFlags.PK_OM_EXTRACT;
                                break;

                            default:
                                Log.Error("Unknown TODO: {0}", openTodo);
                                return false;
                        }

                        IntPtr archW = OpenArchiveW(ref arcdW);
                        if (archW == IntPtr.Zero)
                        {
                            int error = Marshal.GetLastWin32Error();
                            string message = string.Format("OpenArchiveW failed with error {0}", error);
                            Log.Error(message);
                            return false;
                        }
                        else
                        {
                            if (DEBUG) Log.Information("OpenArchiveW: Successfully opened archive at {0}", archW);
                            // Span<byte> byteArray = new Span<byte>(arcdW.ToPointer(), ptrLength);

                            // set callback functions with the archive pointer
                            if (SetChangeVolProcW != null) SetChangeVolProcW(archW, pChangeVolProc);
                            if (SetProcessDataProcW != null) SetProcessDataProcW(archW, pProcessDataProc);

                            // output header
                            switch (openTodo)
                            {
                                case TodoOperations.TODO_LIST:
                                    Log.Information("List of files in {0}", archiveName);
                                    Log.Information(" Length    YYYY/MM/DD HH:MM:SS   Attr   Name");
                                    Log.Information("---------  ---------- --------  ------  ------------");
                                    break;

                                case TodoOperations.TODO_TEST:
                                    Log.Information("Testing files in {0}", archiveName);
                                    break;

                                case TodoOperations.TODO_UNPACK:
                                    Log.Information("Unpacking files from {0} to {1}", archiveName, outputDirectoryPath);
                                    break;

                                default:
                                    Log.Error("Unknown TODO: {0}", openTodo);
                                    return false;
                            }


                            // ------------ Main loop --------------

                            // ReadHeader methods
                            // ReadHeaderEx is always called instead of ReadHeader if present.            
                            // ReadHeaderDelegateEx ReadHeaderEx = null;
                            // if (pReadHeaderEx != IntPtr.Zero)
                            // {
                            //     ReadHeaderEx = (ReadHeaderDelegateEx)Marshal.GetDelegateForFunctionPointer(
                            //             pReadHeaderEx,
                            //             typeof(ReadHeaderDelegateEx));
                            // }

                            // ReadHeaderEx unicode version
                            ReadHeaderDelegateExW ReadHeaderExW = null;
                            if (pReadHeaderExW != IntPtr.Zero)
                            {
                                ReadHeaderExW = (ReadHeaderDelegateExW)Marshal.GetDelegateForFunctionPointer(
                                        pReadHeaderExW,
                                        typeof(ReadHeaderDelegateExW));
                            }

                            // standard ReadHeader ansi version
                            // ReadHeaderDelegate ReadHeader = null;
                            // if (pReadHeader != IntPtr.Zero)
                            // {
                            //     ReadHeader = (ReadHeaderDelegate)Marshal.GetDelegateForFunctionPointer(
                            //             pReadHeader,
                            //             typeof(ReadHeaderDelegate));
                            // }

                            // ProcessFile unicode method
                            ProcessFileDelegateW ProcessFileW = null;
                            if (pProcessFileW != IntPtr.Zero)
                            {
                                ProcessFileW = (ProcessFileDelegateW)Marshal.GetDelegateForFunctionPointer(
                                        pProcessFileW,
                                        typeof(ProcessFileDelegateW));
                            }

                            if (ProcessFileW != null && ReadHeaderExW != null)
                            {
                                // var hdrd = new tHeaderData(); // used by ReadHeader
                                var hdrd = new tHeaderDataExW(); // used by ReadHeaderExW

                                int rc = -1;
                                // while ((rc = ReadHeader(archW, ref hdrd)) == 0)
                                while ((rc = ReadHeaderExW(archW, ref hdrd)) == 0)
                                {
                                    int pfrc = -1;

                                    switch (openTodo)
                                    {
                                        case TodoOperations.TODO_LIST:
                                            Log.Information(string.Format("{1:D9}  {2:D4}/{3:D2}/{4:D2} {5:D2}:{6:D2}:{7:D2}  {8}{9}{10}{11}{12}{13}  {0}", hdrd.FileName, hdrd.UnpSize,
                                                ((hdrd.FileTime >> 25 & 0x7f) + 1980), hdrd.FileTime >> 21 & 0x0f, hdrd.FileTime >> 16 & 0x1f,
                                                hdrd.FileTime >> 11 & 0x1f, hdrd.FileTime >> 5 & 0x3f, (hdrd.FileTime & 0x1F) * 2,
                                                (hdrd.FileAttr & 0x01) != 0 ? 'r' : '-', // Read-only file
                                                (hdrd.FileAttr & 0x02) != 0 ? 'h' : '-', // Hidden file
                                                (hdrd.FileAttr & 0x04) != 0 ? 's' : '-', // System file
                                                (hdrd.FileAttr & 0x08) != 0 ? 'v' : '-', // Volume ID file
                                                (hdrd.FileAttr & 0x10) != 0 ? 'd' : '-', // Directory
                                                (hdrd.FileAttr & 0x20) != 0 ? 'a' : '-')); // Archive file

                                            pfrc = ProcessFileW(archW, (int)ProcessFileFlags.PK_SKIP, null, null);
                                            if (pfrc != 0)
                                            {
                                                var errorString = (ErrorCodes)pfrc;
                                                Log.Error("{0} - ERROR: {1}: {2}", hdrd.FileName, pfrc, errorString);
                                                return false;
                                            }

                                            break;
                                        case TodoOperations.TODO_TEST:
                                            if ((hdrd.FileAttr & 0x10) == 0)
                                            {
                                                pfrc = ProcessFileW(archW, (int)ProcessFileFlags.PK_TEST, null, null);
                                                if (pfrc != 0)
                                                {
                                                    var errorString = (ErrorCodes)pfrc;
                                                    Log.Error("{0} - ERROR: {1}: {2}", hdrd.FileName, pfrc, errorString);
                                                    return false;
                                                }
                                                else
                                                {
                                                    Log.Information("{0} - OK", hdrd.FileName);
                                                }
                                            }
                                            else
                                            {
                                                pfrc = ProcessFileW(archW, (int)ProcessFileFlags.PK_SKIP, null, null);
                                            }
                                            break;

                                        case TodoOperations.TODO_UNPACK:
                                            if ((hdrd.FileAttr & 0x10) == 0)
                                            {
                                                string outputFilePath = Path.Combine(outputDirectoryPath, hdrd.FileName);
                                                IOUtils.CreateDirectoryIfNotExist(Path.GetDirectoryName(outputFilePath));

                                                pfrc = ProcessFileW(archW, (int)ProcessFileFlags.PK_EXTRACT, null, outputFilePath);

                                                // from string to Ptr
                                                // IntPtr destPathPtr = IntPtr.Zero;
                                                // IntPtr destNamePtr = Marshal.StringToHGlobalUni(outputFilePath);
                                                // pfrc = ProcessFileW(archW, (int)ProcessFileFlags.PK_EXTRACT, destPathPtr, destNamePtr);
                                                // remember to unallocate the string
                                                // Marshal.FreeHGlobal(destPathPtr);
                                                // Marshal.FreeHGlobal(destNamePtr);

                                                if (pfrc != 0)
                                                {
                                                    var errorString = (ErrorCodes)pfrc;
                                                    Log.Error("{0} - ERROR: {1}: {2}", outputFilePath, pfrc, errorString);
                                                    return false;
                                                }
                                                else
                                                {
                                                    Log.Information("{0} - OK", outputFilePath);
                                                }
                                            }
                                            else
                                            {
                                                pfrc = ProcessFileW(archW, (int)ProcessFileFlags.PK_SKIP, null, null);
                                            }
                                            break;

                                        default:
                                            Log.Error("Unknown TODO: {0}", openTodo);
                                            return false;
                                    }
                                }
                            }

                            OpenArchiveW = null;
                            ReadHeaderExW = null;
                            ProcessFileW = null;

                            SetChangeVolProcW = null;
                            SetProcessDataProcW = null;
                        }
                    }
                }
            }
            finally
            {
                Log.Information("WCX module finished --------------------------------");

                if (fModuleHandle != IntPtr.Zero)
                {
                    // for some reason the unloading of the wcx causes this to crash!

                    // if (!NativeMethods.FreeLibrary(fModuleHandle))
                    // {
                    //     int error = Marshal.GetLastWin32Error();
                    //     string message = string.Format("FreeLibrary failed with error {0}", error);
                    //     Log.Error(message);
                    // }
                    // else
                    // {
                    //     Log.Information("WCX module unloaded '{0}'.", wcxFileName);
                    // }

                    fModuleHandle = IntPtr.Zero;
                }
            }

            return true;
        }
    }
}