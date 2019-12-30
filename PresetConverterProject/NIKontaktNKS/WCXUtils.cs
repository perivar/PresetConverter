using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CommonUtils;

namespace PresetConverterProject.NIKontaktNKS
{
    // https://studiofreya.com/2016/02/16/how-to-dynamically-load-native-dlls-from-csharp/
    // https://riptutorial.com/csharp/example/17244/dynamic-loading-and-unloading-of-unmanaged-dlls


    // https://gist.githubusercontent.com/b0urb4k1/da912b4e047583fdb56af9fe37c3047d/raw/b74c06b206f01ee7d0bf76bddef0e0d0f5a50813/Functionpointer%2520in%2520C%2523
    // 
    // Header file:
    // extern "C" __declspec(dllexport) int MultiplyByTen(int numberToMultiply);
    // 
    // Source code file:
    // #include "DynamicDLLToCall.h"
    // 
    // int MultiplyByTen(int numberToMultiply)
    // {
    // int returnValue = numberToMultiply * 10;
    // return returnValue;
    // } 
    // 
    // As you can probably infer from the function name, an int is passed into this function and it will return the number passed in multiplied by ten. Told you it would be simple.
    // 
    // Now comes the more interesting part, actually calling this dll dynamically from your C# source code. There are two Win32 functions that are going to help us do this:
    // 
    // 1) LoadLibrary – returns a IntPtr to the dll in question
    // 2) GetProcAddress – obtain the address of an exported function within the previously loaded dll
    // 
    // The rest is rather simple. We use LoadLibrary and GetProcAddress to get the address of the function within the dll we want to call, and then we use the GetDelegateForFunctionPointer static method within the Marshal class to assign this address to a C# delegate that we define. Take a look at the following C# code:

    public static class NativeMethods
    {
        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string fileName);

        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr module, string procedureName);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr module);

        public static string GetLibraryPathname(string filename)
        {
            // If 64-bit process, load 64-bit DLL
            bool is64bit = System.Environment.Is64BitProcess;

            string prefix = "Win32";

            if (is64bit)
            {
                prefix = "x64";
            }

            var lib1 = prefix + @"\" + filename;

            return lib1;
        }
    }

    // https://stackoverflow.com/questions/13834153/how-to-call-unmanaged-dll-to-populate-struct-in-c-sharp-using-a-pointer
    // https://stackoverflow.com/questions/32229536/porting-c-structure-into-c-sharp-from-an-unmanaged-dll
    // https://stackoverflow.com/questions/17020464/c-sharp-calling-c-dll-function-which-returns-a-struct
    // https://www.daniweb.com/programming/software-development/threads/406891/marshalling-c-structures-from-a-dynamically-loaded-dll


    // Delegate with function signature for the GetVersion function
    // uint32_t GetVersion(char * buffer, uint32_t length)
    // [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    // [return: MarshalAs(UnmanagedType.U4)]
    // delegate UInt32 GetVersionDelegate(
    //     [OutAttribute][InAttribute] StringBuilder versionString,
    //     [OutAttribute] UInt32 length);

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
    //      [MarshalAsAttribute(UnmanagedType.LPWStr)]
    //      public string streetAddress;
    //      [MarshalAsAttribute(UnmanagedType.LPWStr)]
    //      public string city;
    //      [MarshalAsAttribute(UnmanagedType.LPWStr)]
    //      public string state;
    //      [MarshalAsAttribute(UnmanagedType.LPWStr)]
    //      public string country;
    //      [MarshalAsAttribute(UnmanagedType.LPWStr)]
    //      public string postalCode;
    // };

    // [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    // public struct QueAddressType
    // {
    //      [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 51)]
    //      public string streetAddress;
    //      [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 51)]
    //      public string city;
    //      [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 51)]
    //      public string state;
    //      [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 51)]
    //      public string country;
    //      [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 12)]
    //      public string postalCode;
    // };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] // charset is Ansi not Unicode
    public struct tHeaderData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ArcName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string FileName;
        [MarshalAs(UnmanagedType.U4)]
        public int Flags;
        [MarshalAs(UnmanagedType.U4)]
        public int PackSize;
        [MarshalAs(UnmanagedType.U4)]
        public int UnpSize;
        [MarshalAs(UnmanagedType.U4)]
        public int HostOS;
        [MarshalAs(UnmanagedType.U4)]
        public int FileCRC;
        [MarshalAs(UnmanagedType.U4)]
        public int FileTime;
        [MarshalAs(UnmanagedType.U4)]
        public int UnpVer;
        [MarshalAs(UnmanagedType.U4)]
        public int Method;
        [MarshalAs(UnmanagedType.U4)]
        public int FileAttr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string CmtBuf;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtBufSize;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtSize;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtState;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct tOpenArchiveData
    {
        [MarshalAsAttribute(UnmanagedType.LPStr)]
        public string ArcName;
        [MarshalAs(UnmanagedType.U4)]
        public int OpenMode;
        [MarshalAs(UnmanagedType.U4)]
        public int OpenResult;
        [MarshalAsAttribute(UnmanagedType.LPStr)]
        public string CmtBuf;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtBufSize;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtSize;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtState;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct tOpenArchiveDataW
    {
        [MarshalAsAttribute(UnmanagedType.LPWStr)]
        public string ArcName;
        [MarshalAs(UnmanagedType.U4)]
        public int OpenMode;
        [MarshalAs(UnmanagedType.U4)]
        public int OpenResult;
        [MarshalAsAttribute(UnmanagedType.LPWStr)]
        public string CmtBuf;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtBufSize;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtSize;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtState;
    };

    // https://www.silabs.com/content/usergenerated/asi/cloud/attachments/siliconlabs/en/community/groups/interface/knowledge-base/jcr:content/content/primary/blog/executing_c_dll_func-u4Wl/Creating%20a%20C%23%20Module%20From%20a%20DLL%20Header%20File.pdf

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr OpenArchiveDelegate([In, Out] ref tOpenArchiveData ArchiveData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr OpenArchiveDelegateW([In, Out] ref tOpenArchiveDataW ArchiveData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    delegate int GetPackerCapsDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    delegate int ReadHeaderDelegate([In] IntPtr hArcData, [In, Out] ref tHeaderData HeaderData);

    // Add a [MarshalAs(UnmanagedType.LPWStr)] attribute to the parameter in your delegate declaration in order for String to get converted into wchar_t* :
    //      delegate void MyDelegate([MarshalAs(UnmanagedType.LPWStr)] string foo)
    // To pass a modifiable string, give a StringBuilder. You need to explicitly reserve space for the unmanaged function to work with :
    //      delegate void MyDelegate([MarshalAs(UnmanagedType.LPWStr)] StringBuilder foo)
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    delegate int ProcessFileDelegate([In] IntPtr hArcData, [In] UInt32 Operation, [In, Out, MarshalAs(UnmanagedType.LPStr)] ref StringBuilder DestPath, [In, Out, MarshalAs(UnmanagedType.LPStr)] ref StringBuilder DestName);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U4)]
    delegate int ProcessFileDelegateW([In] IntPtr hArcData, [In] UInt32 Operation, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder DestPath, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder DestName);

    public static class WCXUtils
    {
        // https://github.com/thpatch/thtk/blob/master/contrib/wcxhead.h

        // Error codes returned to calling application
        public const int E_SUCCESS = 0; // Success
        public const int E_END_ARCHIVE = 10; // No more files in archive
        public const int E_NO_MEMORY = 11; // Not enough memory
        public const int E_BAD_DATA = 12; // Data is bad
        public const int E_BAD_ARCHIVE = 13; // CRC error in archive data
        public const int E_UNKNOWN_FORMAT = 14; // Archive format unknown
        public const int E_EOPEN = 15; // Cannot open existing file
        public const int E_ECREATE = 16; // Cannot create file
        public const int E_ECLOSE = 17; // Error closing file
        public const int E_EREAD = 18; // Error reading from file
        public const int E_EWRITE = 19; // Error writing to file
        public const int E_SMALL_BUF = 20; // Buffer too small
        public const int E_EABORTED = 21; // Function aborted by user
        public const int E_NO_FILES = 22; // No files found
        public const int E_TOO_MANY_FILES = 23; // Too many files to pack
        public const int E_NOT_SUPPORTED = 24; // Function not supported
        public const int E_HANDLED = -32769;   // Handled error
        public const int E_UNKNOWN = 32768;   // Unknown error

        // Unpacking flags
        public const int PK_OM_LIST = 0;
        public const int PK_OM_EXTRACT = 1;

        // Flags for ProcessFile
        public const int PK_SKIP = 0; // Skip file (no unpacking)
        public const int PK_TEST = 1; // Test file integrity
        public const int PK_EXTRACT = 2; // Extract file to disk

        // Flags passed through ChangeVolProc
        public const int PK_VOL_ASK = 0; // Ask user for location of next volume
        public const int PK_VOL_NOTIFY = 1; // Notify app that next volume will be unpacked

        // Packing flags

        // For PackFiles
        public const int PK_PACK_MOVE_FILES = 1; // Delete original after packing
        public const int PK_PACK_SAVE_PATHS = 2; // Save path names of files
        public const int PK_PACK_ENCRYPT = 4; // Ask user for password, then encrypt

        // Returned by GetPackCaps
        public const int PK_CAPS_NEW = 1; // Can create new archives
        public const int PK_CAPS_MODIFY = 2; // Can modify exisiting archives
        public const int PK_CAPS_MULTIPLE = 4; // Archive can contain multiple files
        public const int PK_CAPS_DELETE = 8; // Can delete files
        public const int PK_CAPS_OPTIONS = 16; // Supports the options dialogbox
        public const int PK_CAPS_MEMPACK = 32; // Supports packing in memory
        public const int PK_CAPS_BY_CONTENT = 64; // Detect archive type by content
        public const int PK_CAPS_SEARCHTEXT = 128; // Allow searching for text in archives

        // created with this plugin
        public const int PK_CAPS_HIDE = 256; //  Show as normal files (hide packer icon) open with Ctrl+PgDn, not Enter 
        public const int PK_CAPS_ENCRYPT = 512; //  Plugin supports PK_PACK_ENCRYPT option 
        public const int BACKGROUND_UNPACK = 1; //  Which operations are thread-safe? 
        public const int BACKGROUND_PACK = 2;
        public const int BACKGROUND_MEMPACK = 4; //  For tar.pluginext in background 

        // Flags for packing in memory
        public const int MEM_OPTIONS_WANTHEADERS = 1; // Return archive headers with packed data

        // Errors returned by PackToMem
        public const int MEMPACK_OK = 0; // Function call finished OK, but there is more data
        public const int MEMPACK_DONE = 1; // Function call finished OK, there is no more data

        // Flags for PkCryptProc callback
        public const int PK_CRYPT_SAVE_PASSWORD = 1;
        public const int PK_CRYPT_LOAD_PASSWORD = 2;
        public const int PK_CRYPT_LOAD_PASSWORD_NO_UI = 3; //  Load password only if master password has already been entered!
        public const int PK_CRYPT_COPY_PASSWORD = 4; //  Copy encrypted password to new archive name
        public const int PK_CRYPT_MOVE_PASSWORD = 5; //  Move password when renaming an archive
        public const int PK_CRYPT_DELETE_PASSWORD = 6; //  Delete password
        public const int PK_CRYPTOPT_MASTERPASS_SET = 1;   // The user already has a master password defined

        public const int TODO_FLIST = 0;
        public const int TODO_LIST = 1;
        public const int TODO_TEST = 2;
        public const int TODO_EXTRACT = 3;

        public static bool CallPlugin()
        {
            // string wcxPath = @"C:\Users\perner\Downloads\TotalCommander.Plugins.[DEV][VST]\[inNKX]\inNKX.x64.dll";
            // string wcxPath = @"C:\Users\perner\Downloads\TotalCommander.Plugins.[DEV][VST]\[inNKX]\inNKX.wcx64";
            string wcxPath = @"C:\Users\perner\Downloads\wcx_7zip\7zip.wcx64";
            string wcxFileName = Path.GetFileName(wcxPath);
            string outputDirectoryPath = @"C:\Users\perner\My Projects\Temp";

            // string archiveName = @"C:\Users\perner\Amazon Drive\Documents\My Projects\Native Instruments GmbH\Instruments\LA Scoring Strings_info.nkx";
            string archiveName = @"C:\Users\perner\Downloads\ClipExample.7z";

            // what to do?
            var openTodo = TODO_EXTRACT;

            // load library
            IntPtr fModuleHandle = NativeMethods.LoadLibrary(wcxPath);

            // error handling
            if (fModuleHandle == IntPtr.Zero)
            {
                Console.Error.WriteLine("Failed opening {0}", wcxPath);
                return false;
            }
            else
            {
                Console.Out.WriteLine("WCX module loaded {0} at {1}", wcxFileName, fModuleHandle);
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

            if (openTodo == TODO_FLIST)
            {
                Console.Out.WriteLine("Exported WCX functions in {0}:", wcxFileName);
                Console.Out.WriteLine("Checking mandatory functions ..");

                // mandatory functions
                if (pOpenArchive != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "OpenArchive", pOpenArchive); }
                if (pReadHeader != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "ReadHeader", pReadHeader); }
                if (pReadHeaderEx != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "ReadHeaderEx", pReadHeaderEx); }
                if (pProcessFile != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "ProcessFile", pProcessFile); }
                if (pCloseArchive != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "CloseArchive", pCloseArchive); }
                if (pOpenArchiveW != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "OpenArchiveW", pOpenArchiveW); }
                if (pReadHeaderExW != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "ReadHeaderExW", pReadHeaderExW); }
                if (pProcessFileW != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "ProcessFileW", pProcessFileW); }

                // Optional functions
                Console.Out.WriteLine();
                Console.Out.WriteLine("Checking optional functions ...");
                if (pPackFiles != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "PackFiles", pPackFiles); }
                if (pDeleteFiles != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "DeleteFiles", pDeleteFiles); }
                if (pGetPackerCaps != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "GetPackerCaps", pGetPackerCaps); }
                if (pConfigurePacker != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "ConfigurePacker", pConfigurePacker); }
                if (pSetChangeVolProc != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "SetChangeVolProc", pSetChangeVolProc); }
                if (pSetProcessDataProc != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "SetProcessDataProc", pSetProcessDataProc); }
                if (pStartMemPack != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "StartMemPack", pStartMemPack); }
                if (pPackToMem != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "PackToMem", pPackToMem); }
                if (pDoneMemPack != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "DoneMemPack", pDoneMemPack); }
                if (pCanYouHandleThisFile != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "CanYouHandleThisFile", pCanYouHandleThisFile); }
                if (pPackSetDefaultParams != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "PackSetDefaultParams", pPackSetDefaultParams); }
                if (pPkSetCryptCallback != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "PkSetCryptCallback", pPkSetCryptCallback); }
                if (pGetBackgroundFlags != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "GetBackgroundFlags", pGetBackgroundFlags); }

                // Unicode
                if (pSetChangeVolProcW != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "SetChangeVolProcW", pSetChangeVolProcW); }
                if (pSetProcessDataProcW != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "SetProcessDataProcW", pSetProcessDataProcW); }
                if (pPackFilesW != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "PackFilesW", pPackFilesW); }
                if (pDeleteFilesW != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "DeleteFilesW", pDeleteFilesW); }
                if (pStartMemPackW != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "StartMemPackW", pStartMemPackW); }
                if (pCanYouHandleThisFileW != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "CanYouHandleThisFileW", pCanYouHandleThisFileW); }
                if (pPkSetCryptCallbackW != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "PkSetCryptCallbackW", pPkSetCryptCallbackW); }

                // Extension API
                if (pExtensionInitialize != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "ExtensionInitialize", pExtensionInitialize); }
                if (pExtensionFinalize != IntPtr.Zero) { Console.Out.WriteLine("{0} found at {1}", "ExtensionFinalize", pExtensionFinalize); }

                Console.Out.WriteLine();
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

                    Console.Out.Write("PackerCaps: {0} = ", pc);
                    if ((pc & PK_CAPS_NEW) != 0) { Console.Out.Write("{0} PK_CAPS_NEW", f == 1 ? " |" : ""); f = 1; }
                    if ((pc & PK_CAPS_MODIFY) != 0) { Console.Out.Write("{0} PK_CAPS_MODIFY", f == 1 ? " |" : ""); f = 1; }
                    if ((pc & PK_CAPS_MULTIPLE) != 0) { Console.Out.Write("{0} PK_CAPS_MULTIPLE", f == 1 ? " |" : ""); f = 1; }
                    if ((pc & PK_CAPS_DELETE) != 0) { Console.Out.Write("{0} PK_CAPS_DELETE", f == 1 ? " |" : ""); f = 1; }
                    if ((pc & PK_CAPS_OPTIONS) != 0) { Console.Out.Write("{0} PK_CAPS_OPTIONS", f == 1 ? " |" : ""); f = 1; }
                    if ((pc & PK_CAPS_MEMPACK) != 0) { Console.Out.Write("{0} PK_CAPS_MEMPACK", f == 1 ? " |" : ""); f = 1; }
                    if ((pc & PK_CAPS_BY_CONTENT) != 0) { Console.Out.Write("{0} PK_CAPS_BY_CONTENT", f == 1 ? " |" : ""); f = 1; }
                    if ((pc & PK_CAPS_SEARCHTEXT) != 0) { Console.Out.Write("{0} PK_CAPS_SEARCHTEXT", f == 1 ? " |" : ""); f = 1; }
                    if ((pc & PK_CAPS_HIDE) != 0) { Console.Out.Write("{0} PK_CAPS_HIDE", f == 1 ? " |" : ""); f = 1; }
                    Console.Out.WriteLine();
                }

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
                Console.Error.WriteLine("Missing mandatory functions!");
                return false;
            }

            // OpenArchiveDelegate OpenArchive = null;
            // if (pOpenArchive != IntPtr.Zero)
            // {
            //     OpenArchive = (OpenArchiveDelegate)Marshal.GetDelegateForFunctionPointer(
            //             pOpenArchive,
            //             typeof(OpenArchiveDelegate));
            // }

            // if (OpenArchive != null)
            // {
            //     tOpenArchiveData arcd = new tOpenArchiveData();
            //     arcd.ArcName = archiveName;
            //     arcd.OpenMode = PK_OM_LIST;

            //     IntPtr arch = OpenArchive(ref arcd);
            //     if (arch == IntPtr.Zero)
            //     {
            //         int error = Marshal.GetLastWin32Error();
            //         string message = string.Format("OpenArchive failed with error {0}", error);
            //         Console.Error.WriteLine(message);
            //     }
            //     else
            //     {
            //         Console.Out.WriteLine("OpenArchive: Successfully opened archive at {0}", arch);
            //     }
            // }

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
                arcdW.OpenMode = PK_OM_LIST;

                IntPtr archW = OpenArchiveW(ref arcdW);
                if (archW == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    string message = string.Format("OpenArchiveW failed with error {0}", error);
                    Console.Error.WriteLine(message);
                }
                else
                {
                    Console.Out.WriteLine("OpenArchiveW: Successfully opened archive at {0}", archW);

                    // main loop
                    ReadHeaderDelegate ReadHeader = null;
                    if (pReadHeader != IntPtr.Zero)
                    {
                        ReadHeader = (ReadHeaderDelegate)Marshal.GetDelegateForFunctionPointer(
                                pReadHeader,
                                typeof(ReadHeaderDelegate));
                    }

                    ProcessFileDelegateW ProcessFileW = null;
                    if (pProcessFileW != IntPtr.Zero)
                    {
                        ProcessFileW = (ProcessFileDelegateW)Marshal.GetDelegateForFunctionPointer(
                                pProcessFileW,
                                typeof(ProcessFileDelegateW));
                    }

                    if (ReadHeader != null && ProcessFileW != null)
                    {
                        var hdrd = new tHeaderData();

                        int rc = -1;
                        while ((rc = ReadHeader(archW, ref hdrd)) == 0)
                        {
                            StringBuilder destPath = new StringBuilder(260);
                            StringBuilder destName = new StringBuilder(260);
                            int pfrc = -1;

                            switch (openTodo)
                            {
                                case TODO_LIST:
                                    Console.Out.WriteLine("{1:D9}  {2:D4}/{3:D2}/{4:D2} {5:D2}:{6:D2}:{7:D2} {8}{9}{10}{11}{12}{13}  {0}", hdrd.FileName, hdrd.UnpSize,
                                        ((hdrd.FileTime >> 25 & 0x7f) + 1980), hdrd.FileTime >> 21 & 0x0f, hdrd.FileTime >> 16 & 0x1f,
                                        hdrd.FileTime >> 11 & 0x1f, hdrd.FileTime >> 5 & 0x3f, (hdrd.FileTime & 0x1F) * 2,
                                        (hdrd.FileAttr & 0x01) != 0 ? 'r' : '-',
                                        (hdrd.FileAttr & 0x02) != 0 ? 'h' : '-',
                                        (hdrd.FileAttr & 0x04) != 0 ? 's' : '-',
                                        (hdrd.FileAttr & 0x08) != 0 ? 'v' : '-',
                                        (hdrd.FileAttr & 0x10) != 0 ? 'd' : '-',
                                        (hdrd.FileAttr & 0x20) != 0 ? 'a' : '-');

                                    pfrc = ProcessFileW(archW, PK_SKIP, null, null);
                                    if (pfrc != 0)
                                    {
                                        Console.Error.WriteLine(" - ERROR: {0}\n", pfrc);
                                        return false;
                                    }

                                    break;
                                case TODO_TEST:
                                    if ((hdrd.FileAttr & 0x10) == 0)
                                    {
                                        Console.Out.Write("{0}", hdrd.FileName);
                                        pfrc = ProcessFileW(archW, PK_TEST, null, null);
                                        if (pfrc != 0)
                                        {
                                            Console.Error.WriteLine(" - ERROR: {0}\n", pfrc);
                                            return false;
                                        }
                                        else
                                        {
                                            Console.Out.Write(" - OK\n");
                                        }
                                    }
                                    else
                                    {
                                        pfrc = ProcessFileW(archW, PK_SKIP, null, null);
                                    }
                                    break;

                                case TODO_EXTRACT:
                                    if ((hdrd.FileAttr & 0x10) == 0)
                                    {
                                        string outputFilePath = Path.Combine(outputDirectoryPath, hdrd.FileName);
                                        IOUtils.CreateDirectoryIfNotExist(Path.GetDirectoryName(outputFilePath));
                                        destName.Append(outputFilePath);

                                        Console.Out.Write("{0}", outputFilePath);
                                        pfrc = ProcessFileW(archW, PK_EXTRACT, null, destName);
                                        if (pfrc != 0)
                                        {
                                            Console.Error.WriteLine(" - ERROR: {0}\n", pfrc);
                                            return false;
                                        }
                                        else
                                        {
                                            Console.Out.Write(" - OK\n");
                                        }
                                    }
                                    else
                                    {
                                        pfrc = ProcessFileW(archW, PK_SKIP, null, null);
                                    }
                                    break;

                                default:
                                    Console.Error.WriteLine("Unknown TODO: {0}", openTodo);
                                    return false;
                            }
                        }
                    }
                }
            }

            bool result = NativeMethods.FreeLibrary(fModuleHandle);
            return result;
        }
    }
}