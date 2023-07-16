using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.IO.Compression;

namespace CommonUtils
{
    /// <summary>
    /// Utils for input output (IO).
    /// </summary>
    public static class IOUtils
    {
        static readonly Encoding _isoLatin1Encoding = Encoding.GetEncoding("ISO-8859-1");
        const string columnSeparator = ",";

        #region Get/ Search for Files
        /// <summary>
        /// Return all files by their extension in ONE Directory (not recursive)
        /// </summary>
        /// <param name="dir">Directory Path</param>
        /// <param name="extensions">extensions, e.g. ".jpg",".exe",".gif"</param>
        /// <returns></returns>
        /// <example>dInfo.GetFilesByExtensions(".jpg",".exe",".gif");</example>
        public static IEnumerable<FileInfo> GetFilesByExtensions(this DirectoryInfo dir, params string[] extensions)
        {
            if (extensions == null)
                throw new ArgumentNullException(nameof(extensions));

            IEnumerable<FileInfo> files = dir.EnumerateFiles();
            return files.Where(f => extensions.Contains(f.Extension));
        }

        /// <summary>
        /// Get Files using regexp pattern like \.mp3|\.mp4\.wav\.ogg
        /// By using SearchOption.AllDirectories, you can make it recursive
        /// </summary>
        /// <param name="path">Directory Path</param>
        /// <param name="searchPatternExpression">Regexp pattern like \.mp3|\.mp4\.wav\.ogg</param>
        /// <param name="searchOption">SearchOption like SearchOption.AllDirectories</param>
        /// <returns>IEnumerable array of filenames</returns>
        /// <example>var files = IOUtils.GetFiles(path, "\\.mp3|\\.mp4\\.wav\\.ogg", SearchOption.AllDirectories);</example>
        public static IEnumerable<string> GetFiles(string path, string searchPatternExpression = "", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var reSearchPattern = new Regex(searchPatternExpression);
            return Directory.EnumerateFiles(path, "*", searchOption).Where(file => reSearchPattern.IsMatch(Path.GetExtension(file).ToLower()));
        }

        /// <summary>
        /// Get Files using array of extensions and executes in parallel
        /// By using SearchOption.AllDirectories, you can make it recursive
        /// </summary>
        /// <param name="path">Directory Path</param>
        /// <param name="searchPatterns">Array of extensions like: string[] extensions = { "*.mp3", "*.wav", "*.ogg" };</param>
        /// <param name="searchOption">SearchOption like SearchOption.AllDirectories</param>
        /// <returns>IEnumerable array of filenames</returns>
        /// <example>
        /// string[] extensions = { "*.mp3", "*.wma", "*.mp4", "*.wav", "*.ogg" };
        /// var files = IOUtils.GetFiles(path, extensions, SearchOption.AllDirectories);
        /// </example>
        public static IEnumerable<string> GetFiles(string path, string[] searchPatterns, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return searchPatterns.AsParallel().SelectMany(searchPattern => Directory.EnumerateFiles(path, searchPattern, searchOption));
        }

        /// <summary>
        /// Get files recursively using a search pattern
        /// </summary>
        /// <param name="path">Directory Path</param>
        /// <param name="searchPattern">Search pattern like: "*.mp3" or "one_specific_file.wav"</param>
        /// <returns>IEnumerable array of filenames</returns>
        public static IEnumerable<string> GetFilesRecursive(string path, string searchPattern)
        {
            return Directory.EnumerateFiles(path, searchPattern, SearchOption.AllDirectories);
        }

        /// <summary>
        /// Get files recursively using an array of extensions
        /// </summary>
        /// <param name="path">Directory Path</param>
        /// <param name="extensions">Array of extensions like: string[] extensions = { ".mp3", ".wav", ".ogg" };</param>
        /// <returns>IEnumerable array of filenames</returns>
        /// <example>
        /// string[] extensions = { ".mp3", ".wma", ".mp4", ".wav", ".ogg" };
        /// var files = IOUtils.GetFilesRecursive(path, extensions);
        /// </example>
        public static IEnumerable<string> GetFilesRecursive(string path, string[] extensions)
        {
            IEnumerable<string> filesAll =
                Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()));
            return filesAll;
        }

        /// <summary>
        /// Get all files recursively
        /// </summary>
        /// <param name="path">Directory Path</param>
        /// <returns>IEnumerable array of filenames</returns>
        public static IEnumerable<string> GetFilesRecursive(string path)
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        }
        #endregion

        /// <summary>
        /// Backup a file to a filename.bak or filename.bak_number etc
        /// </summary>
        /// <param name="fileName">filename to backup</param>
        public static void MakeBackupOfFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                string destinationBackupFileName = fileName + ".bak";

                // make sure to create a new backup if the backup file already exist
                int backupFileCount = -1;
                do
                {
                    backupFileCount++;
                }
                while (File.Exists(destinationBackupFileName + (backupFileCount > 0 ? "_" + backupFileCount.ToString() : "")));

                destinationBackupFileName += (backupFileCount > 0 ? "_" + (backupFileCount).ToString() : "");
                File.Copy(fileName, destinationBackupFileName);
            }
        }

        /// <summary>
        /// Get Next Available Filename using passed filepath
        /// E.g. _001.ext, 002_ext unt so weiter
        /// </summary>
        /// <param name="filePath">file path to check</param>
        /// <returns>a unique filepath</returns>
        public static string NextAvailableFilename(string filePath)
        {
            // Short-cut if already available
            if (!File.Exists(filePath))
                return filePath;

            // build up filename
            var fileInfo = new FileInfo(filePath);

            DirectoryInfo folder = fileInfo.Directory;
            string folderName = folder.FullName;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);

            int version = 0;
            do
            {
                version++;
            }
            while (File.Exists(
                string.Format("{0}{1}{2}_{3:000}.h2p", folderName, Path.DirectorySeparatorChar, fileNameWithoutExtension, version)
            ));

            return string.Format("{0}{1}{2}_{3:000}.h2p", folderName, Path.DirectorySeparatorChar, fileNameWithoutExtension, version);
        }

        /// <summary>
        /// Determine whether a path is a file or a directory
        /// </summary>
        /// <param name="fileOrDirectoryPath">path</param>
        /// <returns>true if the path is a directory, false if a file, null if nothing</returns>
        public static bool? IsDirectory(string fileOrDirectoryPath)
        {
            if (Directory.Exists(fileOrDirectoryPath))
            {
                return true; // is a directory
            }
            else if (File.Exists(fileOrDirectoryPath))
            {
                return false; // is a file
            }
            else
            {
                return null; // is a nothing 
            }
        }

        /// <summary>
        /// Create Directory if it doesn't already exist
        /// </summary>
        /// <param name="filePath">directory path</param>
        public static void CreateDirectoryIfNotExist(string filePath)
        {
            try
            {
                Directory.CreateDirectory(filePath);
            }
            catch (Exception)
            {
                // handle them here
            }
        }

        /// <summary>
        /// Log a message to file (e.g. a log file)
        /// </summary>
        /// <param name="file">filename to use</param>
        /// <param name="msg">message to log</param>
        public static void LogMessageToFile(FileInfo file, string msg)
        {
            // Make sure to support Multithreaded write access
            using (var fs = new FileStream(file.FullName, FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (var sw = new StreamWriter(fs))
                {
                    string logLine = String.Format(
                        "{0:G}: {1}", DateTime.Now, msg);
                    sw.WriteLine(logLine);
                }
            }
        }

        /// <summary>
        /// Read everything from a file as text (string)
        /// </summary>
        /// <param name="filePath">file</param>
        /// <returns>string</returns>
        public static string ReadTextFromFile(string filePath)
        {
            string text = "";
            try
            {
                if (File.Exists(filePath))
                {
                    text = File.ReadAllText(filePath);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
            return text;
        }

        /// <summary>
        /// Write text to a file
        /// </summary>
        /// <param name="filePath">file</param>
        /// <param name="text">text to write</param>
        /// <returns>true if successful</returns>
        public static bool WriteTextToFile(string filePath, string text)
        {
            try
            {
                File.WriteAllText(filePath, text);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Print the passed array to the TextWriter (e.g. Console.out)
        /// </summary>
        /// <param name="pw">a textwriter, e.g. Console.Out</param>
        /// <param name="data">array to output</param>
        /// <example>IOUtils.Print(Console.Out, data);</example>
        public static void Print(TextWriter pw, double[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                pw.Write("{0}", data[i].ToString("F4", CultureInfo.InvariantCulture).PadLeft(10) + " ");
                pw.Write("\r");
            }
            pw.WriteLine();
            pw.Close();
        }

        /// <summary>
        /// Print the passed array to the TextWriter (e.g. Console.out)
        /// </summary>
        /// <param name="pw">a textwriter, e.g. Console.Out</param>
        /// <param name="data">array to output</param>
        /// <example>IOUtils.Print(Console.Out, data);</example>
        public static void Print(TextWriter pw, float[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                pw.Write("{0}", data[i].ToString("F3", CultureInfo.InvariantCulture).PadLeft(10) + " ");
                pw.Write("\r");
            }
            pw.WriteLine();
            pw.Close();
        }

        /// <summary>
        /// Return a temporary file name
        /// </summary>
        /// <param name="extension">extension without the dot e.g. wav or csv</param>
        /// <returns>filepath to the temporary file</returns>
        public static string GetTempFilePathWithExtension(string extension)
        {
            var path = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString() + "." + extension;
            return Path.Combine(path, fileName);
        }

        /// <summary>
        /// Return the right part of the path after a given base path if found
        /// </summary>
        /// <param name="path">long path</param>
        /// <param name="startAfterPart">base path</param>
        /// <returns></returns>
        public static string? GetRightPartOfPath(string path, string startAfterPart)
        {
            int startAfter = path.LastIndexOf(startAfterPart, StringComparison.Ordinal);

            if (startAfter == -1)
            {
                // path path not found
                return null;
            }

            return path.Substring(startAfterPart.Length);
        }

        /// <summary>
        /// Return the full file path without the extension
        /// </summary>
        /// <param name="fullPath">full path with extension</param>
        /// <returns>full path without extension</returns>
        public static string GetFullPathWithoutExtension(string fullPath)
        {
            return Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath));
        }

        /// <summary>
        /// Make sure the file path has a given extension
        /// </summary>
        /// <param name="fullPath">full path with our without extension</param>
        /// <param name="extension">extension in the format .ext e.g. '.png', '.wav'</param>
        /// <returns></returns>
        public static string EnsureExtension(string fullPath, string extension)
        {
            if (!fullPath.EndsWith(extension, StringComparison.Ordinal))
            {
                return fullPath + extension;
            }
            else
            {
                return fullPath;
            }
        }

        /// <summary>
        /// Remove the unsupported files from the passed file array
        /// </summary>
        /// <param name="files">all files</param>
        /// <param name="supportedExtensions">supported extensions</param>
        /// <returns>an array of supported files</returns>
        public static string[] FilterOutUnsupportedFiles(string[] files, string[] supportedExtensions)
        {

            var correctFiles = new List<string>();
            foreach (string inputFilePath in files)
            {
                string fileExtension = Path.GetExtension(inputFilePath);
                int pos = Array.IndexOf(supportedExtensions, fileExtension);
                if (pos > -1)
                {
                    correctFiles.Add(inputFilePath);
                }
            }
            return correctFiles.ToArray();
        }

        #region Read and Write CSV files
        public delegate object MyParser(int lineCounter, string[] splittedLine, Dictionary<int, string>? lookupDictionary);
        public delegate string MyFormatter(object line, int lineCounter, string columnSeparator);
        public delegate string MyFormatterHeader(string columnSeparator);

        /// <summary>
        /// Read a CSV file and use delegate method to parse the lines
        /// </summary>
        /// <example>
        /// public static object CsvDoubleParser(string[] splittedLine)
        /// {
        /// 	// only store the second element (the first is a counter)
        /// 	return double.Parse(splittedLine[1]);
        /// }
        /// var objects = IOUtils.ReadCSV("input.csv", false, CsvDoubleParser);
        /// var doubles = objects.Cast<double>().ToArray();
        /// </example>
        /// <param name="filePath">file path</param>
        /// <param name="hasHeader">whether we should skip the first header row</param>
        /// <param name="parser">a parser delegate method</param>
        /// <param name="columnSeparator">column seperator, default ","</param>
        /// <param name="doRemoveEmptyEntries">whether to remove empty entries when parsing lines, default TRUE</param>
        /// <returns>a list of objects that can be casted to whatever</returns>
        public static List<object> ReadCSV(string filePath, bool hasHeader, MyParser parser, Dictionary<int, string>? lookupDictionary = null, string columnSeparator = columnSeparator, bool doRemoveEmptyEntries = true)
        {
            int lineCounter = 0;
            var list = new List<object>();

            // read in the dictionary file in the csv format
            foreach (var line in File.ReadLines(filePath, _isoLatin1Encoding))
            {
                lineCounter++;

                // skip header
                if (hasHeader && lineCounter == 1) continue;

                // ignore blank lines
                if (string.IsNullOrEmpty(line))
                    continue;

                // parse
                var elements = line.Split(new String[] {
                                              columnSeparator
                                          }, doRemoveEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);

                list.Add(parser(lineCounter, elements, lookupDictionary));
            }

            return list;
        }

        /// <summary>
        /// Write a CSV file and and use delegate method to format the lines
        /// </summary>
        /// <example>
        /// public static string CvsComplexFormatter(object line, int lineCounter, string columnSeparator)
        /// {
        ///     var elements = new List<string>();
        ///     var complex = (CommonUtils.MathLib.FFT.Complex) line;
        ///
        ///     elements.Add(String.Format("{0,4}", lineCounter));
        ///     elements.Add(String.Format("{0,12:N6}", complex.Re));
        ///     elements.Add(String.Format("{0,12:N6}", complex.Im));
        ///
        ///     return string.Join(columnSeparator, elements);
        /// }
        /// 
        /// Complex[] spectrum = SpectrogramUtils.padded_FFT(ref signal);
        /// List<object> lines = spectrum.Cast<object>().ToList();
        /// IOUtils.WriteCSV("output.csv", lines, CvsComplexFormatter);
        /// </example>
        /// <param name="filePath">file path</param>
        /// <param name="lines">a list of objects</param>
        /// <param name="formatter">a formatter delegate method</param>
        /// <param name="columnSeparator">column seperator, default ","</param>
        public static void WriteCSV(string filePath, List<object> lines, MyFormatter formatter, MyFormatterHeader? header = null, string columnSeparator = columnSeparator)
        {
            int lineCounter = 0;
            TextWriter pw = new StreamWriter(filePath, false, _isoLatin1Encoding);

            if (header != null)
            {
                pw.WriteLine(header(columnSeparator));
            }

            // rows and columns
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    lineCounter++;

                    // write data
                    var columns = formatter(line, lineCounter, columnSeparator);
                    pw.Write("{0}\r\n", columns);
                }
            }
            pw.Close();
        }

        #endregion

        /// <summary>
        /// Return the path where the executable is running from
        /// </summary>
        /// <see cref="http://codebuckets.com/2017/10/19/getting-the-root-directory-path-for-net-core-applications/"></see>
        /// <see cref="https://www.hanselman.com/blog/how-do-i-find-which-directory-my-net-core-console-application-was-started-in-or-is-running-from"/> 
        /// <see cref="https://www.softwaredeveloper.blog/executing-assembly-location-in-a-single-file-app"/> 
        /// <returns>the path where the executable is running from</returns>
        public static string GetApplicationExecutionPath()
        {
            // get the application execution path

            // this solution does not work any longer when publishing to an executable (single file app)
            // var executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // instead use AppContext.BaseDirectory
            var executableDirectory = AppContext.BaseDirectory;

            if (string.IsNullOrEmpty(executableDirectory))
            {
                throw new Exception("Could not find out executable directory!");
            }

            return executableDirectory;
        }

        /// <summary>
        /// Decompress gzipped byte array
        /// </summary>
        /// <param name="gzip">gzipped byte array</param>
        /// <returns>decompressed bytes</returns>
        public static byte[] Decompress(byte[] gzip)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
                CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
    }
}

