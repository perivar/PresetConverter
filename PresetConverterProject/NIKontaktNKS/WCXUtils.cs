using System;
using System.Runtime.InteropServices;

namespace PresetConverterProject.NIKontaktNKS
{
    // https://studiofreya.com/2016/02/16/how-to-dynamically-load-native-dlls-from-csharp/


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
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);

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


    // [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    // delegate int MultiplyByTen(int numberToMultiply);

    // Delegate with function signature for the GetVersion function
    // [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    // [return: MarshalAs(UnmanagedType.U4)]
    // delegate UInt32 GetVersionDelegate(
    //     [OutAttribute][InAttribute] StringBuilder versionString,
    //     [OutAttribute] UInt32 length);


    // https://stackoverflow.com/questions/13834153/how-to-call-unmanaged-dll-to-populate-struct-in-c-sharp-using-a-pointer
    // https://stackoverflow.com/questions/32229536/porting-c-structure-into-c-sharp-from-an-unmanaged-dll

    // https://stackoverflow.com/questions/17020464/c-sharp-calling-c-dll-function-which-returns-a-struct

    // https://www.daniweb.com/programming/software-development/threads/406891/marshalling-c-structures-from-a-dynamically-loaded-dll


    // [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct tHeaderData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        string ArcName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        string FileName;
        [MarshalAs(UnmanagedType.U4)]
        int Flags;
        [MarshalAs(UnmanagedType.U4)]
        int PackSize;
        [MarshalAs(UnmanagedType.U4)]
        int UnpSize;
        [MarshalAs(UnmanagedType.U4)]
        int HostOS;
        [MarshalAs(UnmanagedType.U4)]
        int FileCRC;
        [MarshalAs(UnmanagedType.U4)]
        int FileTime;
        [MarshalAs(UnmanagedType.U4)]
        int UnpVer;
        [MarshalAs(UnmanagedType.U4)]
        int Method;
        [MarshalAs(UnmanagedType.U4)]
        int FileAttr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string CmtBuf; // char*
        // public IntPtr CmtBuf;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtBufSize;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtSize;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtState;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct tOpenArchiveData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
        public string ArcName; // char*
        // public IntPtr ArcName;
        [MarshalAs(UnmanagedType.U4)]
        public int OpenMode;
        [MarshalAs(UnmanagedType.U4)]
        public int OpenResult;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
        public string CmtBuf; // char*
        // public IntPtr CmtBuf;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtBufSize;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtSize;
        [MarshalAs(UnmanagedType.U4)]
        public int CmtState;
    }

    // mandatory functions
    // static HANDLE	(__stdcall *pOpenArchive)(tOpenArchiveData *ArchiveData) = NULL;
    // static int		(__stdcall *pReadHeader)(HANDLE hArcData, tHeaderData *HeaderData) = NULL;
    // static int		(__stdcall *pProcessFile)(HANDLE hArcData, int Operation, char *DestPath, char *DestName) = NULL;
    // static int		(__stdcall *pCloseArchive)(HANDLE hArcData) = NULL;

    // optional functions
    // static int		(__stdcall *pPackFiles)(char *PackedFile, char *SubPath, char *SrcPath, char *AddList, int Flags) = NULL;
    // static int		(__stdcall *pDeleteFiles)(char *PackedFile, char *DeleteList) = NULL;
    // static int		(__stdcall *pGetPackerCaps)(void) = NULL;
    // static void		(__stdcall *pConfigurePacker)(HWND Parent, HINSTANCE DllInstance);
    // static void		(__stdcall *pSetChangeVolProc)(HANDLE hArcData, tChangeVolProc pChangeVolProc1) = NULL;		 // NOT quite
    // static void		(__stdcall *pSetProcessDataProc)(HANDLE hArcData, tProcessDataProc pProcessDataProc) = NULL; // NOT quite

    // packing into memory
    // static int		(__stdcall *pStartMemPack)(int Options, char *FileName) = NULL;
    // static int		(__stdcall *pPackToMem)(int hMemPack, char* BufIn, int InLen, int* Taken,
    // 										char* BufOut, int OutLen, int* Written, int SeekBy) = NULL;
    // static int		(__stdcall *pDoneMemPack)(int hMemPack) = NULL;

    // static bool		(__stdcall *pCanYouHANDLEThisFile)(char *FileName) = NULL;
    // static void		(__stdcall *pPackSetDefaultParams)(PackDefaultParamStruct* dps) = NULL;


    // https://www.silabs.com/content/usergenerated/asi/cloud/attachments/siliconlabs/en/community/groups/interface/knowledge-base/jcr:content/content/primary/blog/executing_c_dll_func-u4Wl/Creating%20a%20C%23%20Module%20From%20a%20DLL%20Header%20File.pdf

    // Description
    // OpenArchive should return a unique handle representing the archive.
    // The handle should remain valid until CloseArchive is called.
    // If an error occurs, you should return zero, and specify the error by setting OpenResult member of ArchiveData.
    // You can use the ArchiveData to query information about the archive being open,
    // and store the information in ArchiveData to some location that can be accessed via the handle.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    // delegate IntPtr OpenArchiveDelegate([OutAttribute][InAttribute] tOpenArchiveData ArchiveData);
    // delegate IntPtr OpenArchiveDelegate([OutAttribute][InAttribute] [MarshalAs(UnmanagedType.Struct)] tOpenArchiveData ArchiveData);
    // delegate IntPtr OpenArchiveDelegate([MarshalAs(UnmanagedType.Struct)] ref tOpenArchiveData ArchiveData);
    // delegate IntPtr OpenArchiveDelegate(ref tOpenArchiveData ArchiveData);
    // delegate IntPtr OpenArchiveDelegate([In, Out, MarshalAs(UnmanagedType.LPStruct)] tOpenArchiveData ArchiveData);
    delegate IntPtr OpenArchiveDelegate([In, Out, MarshalAs(UnmanagedType.Struct)] tOpenArchiveData ArchiveData);


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int GetPackerCapsDelegate();

    public static class WCXUtils
    {

        // https://github.com/thpatch/thtk/blob/master/contrib/wcxhead.h

        // flags for unpacking
        const int PK_OM_LIST = 0;
        const int PK_OM_EXTRACT = 1;

        // flags for ProcessFile
        const int PK_SKIP = 0;                 // Skip this file
        const int PK_TEST = 1;                 // Test file integrity
        const int PK_EXTRACT = 2;              // Extract to disk

        // Flags passed through ChangeVolProc
        const int PK_VOL_ASK = 0;              // Ask user for location of next volume
        const int PK_VOL_NOTIFY = 1;           // Notify app that next volume will be unpacked

        // For PackFiles
        const int PK_PACK_MOVE_FILES = 1;       // Delete original after packing       
        const int PK_PACK_SAVE_PATHS = 2;       // Save path names of files            

        const int PK_PACK_ENCRYPT = 4;          // Ask user for password, then encrypt 

        // Returned by GetPackCaps
        const int PK_CAPS_NEW = 1;              // Can create new archives              
        const int PK_CAPS_MODIFY = 2;           // Can modify exisiting archives        
        const int PK_CAPS_MULTIPLE = 4;         // Archive can contain multiple files   
        const int PK_CAPS_DELETE = 8;           // Can delete files                     
        const int PK_CAPS_OPTIONS = 16;         // Has options dialog                   
        const int PK_CAPS_MEMPACK = 32;         // Supports packing in memory           
        const int PK_CAPS_BY_CONTENT = 64;      // Detect archive type by content       
        const int PK_CAPS_SEARCHTEXT = 128;     // Allow searching for text in archives created with this plugin 
        const int PK_CAPS_HIDE = 256;           // Show as normal files (hide packer icon), open with Ctrl+PgDn, not Enter 
        const int PK_CAPS_ENCRYPT = 512;        // Plugin supports PK_PACK_ENCRYPT option


        public static bool CallPlugin()
        {
            IntPtr hwcx = IntPtr.Zero;
            IntPtr arch = IntPtr.Zero;
            tOpenArchiveData arcd;
            tHeaderData hdrd;

            // string wcxPath = @"C:\Users\perner\Downloads\TotalCommander.Plugins.[DEV][VST]\[inNKX]\inNKX.x64.dll";
            // string wcxPath = @"C:\Users\perner\Downloads\TotalCommander.Plugins.[DEV][VST]\[inNKX]\inNKX.wcx64";
            string wcxPath = @"C:\Users\perner\Downloads\wcx_7zip\7zip.wcx64";
            hwcx = NativeMethods.LoadLibrary(wcxPath);

            // error handling
            if (hwcx == IntPtr.Zero)
            {
                Console.Error.WriteLine("Failed opening {0}", wcxPath);
                return false;
            }

            // mandatory
            IntPtr pOpenArchive = NativeMethods.GetProcAddress(hwcx, "OpenArchive");
            IntPtr pReadHeader = NativeMethods.GetProcAddress(hwcx, "ReadHeader");
            IntPtr pProcessFile = NativeMethods.GetProcAddress(hwcx, "ProcessFile");
            IntPtr pCloseArchive = NativeMethods.GetProcAddress(hwcx, "CloseArchive");

            // optional
            IntPtr pPackFiles = NativeMethods.GetProcAddress(hwcx, "PackFiles");
            IntPtr pDeleteFiles = NativeMethods.GetProcAddress(hwcx, "DeleteFiles");
            IntPtr pGetPackerCaps = NativeMethods.GetProcAddress(hwcx, "GetPackerCaps");
            IntPtr pConfigurePacker = NativeMethods.GetProcAddress(hwcx, "ConfigurePacker");

            // NOT optional
            IntPtr pSetChangeVolProc = NativeMethods.GetProcAddress(hwcx, "SetChangeVolProc");
            IntPtr pSetProcessDataProc = NativeMethods.GetProcAddress(hwcx, "SetProcessDataProc");

            // optional
            IntPtr pStartMemPack = NativeMethods.GetProcAddress(hwcx, "StartMemPack");
            IntPtr pPackToMem = NativeMethods.GetProcAddress(hwcx, "PackToMem");
            IntPtr pDoneMemPack = NativeMethods.GetProcAddress(hwcx, "DoneMemPack");
            IntPtr pCanYouIntPtrThisFile = NativeMethods.GetProcAddress(hwcx, "CanYouIntPtrThisFile");
            IntPtr pPackSetDefaultParams = NativeMethods.GetProcAddress(hwcx, "PackSetDefaultParams");


            Console.Out.WriteLine("Exported WCX functions in {0}:", wcxPath);
            OpenArchiveDelegate OpenArchive = null;
            if (pOpenArchive != IntPtr.Zero)
            {
                Console.Out.WriteLine("OpenArchive");
                OpenArchive = (OpenArchiveDelegate)Marshal.GetDelegateForFunctionPointer(
                        pOpenArchive,
                        typeof(OpenArchiveDelegate));
            }
            if (pReadHeader != IntPtr.Zero)
            {
                Console.Out.WriteLine("ReadHeader");
            }
            if (pProcessFile != IntPtr.Zero)
            {
                Console.Out.WriteLine("ProcessFile");
            }
            if (pCloseArchive != IntPtr.Zero)
            {
                Console.Out.WriteLine("CloseArchive");
            }
            if (pPackFiles != IntPtr.Zero)
            {
                Console.Out.WriteLine("PackFiles");
            }
            if (pDeleteFiles != IntPtr.Zero)
            {
                Console.Out.WriteLine("DeleteFiles");
            }
            GetPackerCapsDelegate GetPackerCaps = null;
            if (pGetPackerCaps != IntPtr.Zero)
            {
                Console.Out.WriteLine("GetPackerCaps");
                GetPackerCaps = (GetPackerCapsDelegate)Marshal.GetDelegateForFunctionPointer(
                        pGetPackerCaps,
                        typeof(GetPackerCapsDelegate));
            }
            if (pConfigurePacker != IntPtr.Zero)
            {
                Console.Out.WriteLine("ConfigurePacker");
            }
            if (pSetChangeVolProc != IntPtr.Zero)
            {
                Console.Out.WriteLine("SetChangeVolProc");
            }
            if (pSetProcessDataProc != IntPtr.Zero)
            {
                Console.Out.WriteLine("SetProcessDataProc");
            }
            if (pStartMemPack != IntPtr.Zero)
            {
                Console.Out.WriteLine("StartMemPack");
            }
            if (pPackToMem != IntPtr.Zero)
            {
                Console.Out.WriteLine("PackToMem");
            }
            if (pDoneMemPack != IntPtr.Zero)
            {
                Console.Out.WriteLine("DoneMemPack");
            }
            if (pCanYouIntPtrThisFile != IntPtr.Zero)
            {
                Console.Out.WriteLine("CanYouIntPtrThisFile");
            }
            if (pPackSetDefaultParams != IntPtr.Zero)
            {
                Console.Out.WriteLine("PackSetDefaultParams");
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

            if (OpenArchive != null)
            {
                arcd = new tOpenArchiveData();
                // arcd.ArcName = @"C:\Users\perner\Amazon Drive\Documents\My Projects\Native Instruments GmbH\Instruments\LA Scoring Strings_info.nkx";
                arcd.ArcName = @"C:\Users\perner\Downloads\ClipExample.7z";
                arcd.OpenMode = PK_OM_LIST;

                // arcd.CmtBuf = Marshal.AllocHGlobal(100);

                arch = OpenArchive(arcd);
                if (arch == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    string message = string.Format("Error {0}", error);
                    Console.Error.WriteLine(message);
                }
            }


            bool result = NativeMethods.FreeLibrary(hwcx);

            return false;

        }
    }
}