using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace CommonUtils
{
    /// <summary>
    /// Assorted string methods that might be helpful
    /// </summary>
    public static class StringUtils
    {
        private static readonly string UTF8BOM = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
        // private static readonly string UTF16LEBOM = Encoding.Unicode.GetString(Encoding.Unicode.GetPreamble());
        // private static readonly string UTF16BEBOM = Encoding.BigEndianUnicode.GetString(Encoding.BigEndianUnicode.GetPreamble());
        // private static readonly string UTF32LEBOM = Encoding.UTF32.GetString(Encoding.UTF32.GetPreamble());
        // private static readonly string UTF32BEBOM = Encoding.GetEncoding("utf-32BE").GetString(Encoding.GetEncoding("utf-32BE").GetPreamble());

        /// <summary>
        /// Pascal case
        /// The first letter in the identifier and the first letter of each subsequent concatenated word are capitalized. You can use Pascal case for identifiers of three or more characters. For example:
        /// E.g. "BackColor"
        /// Camel case
        /// The first letter of an identifier is lowercase and the first letter of each subsequent concatenated word is capitalized. For example:
        /// E.g. "backColor"
        /// </summary>
        public enum Case
        {
            PascalCase,
            CamelCase
        }

        /// <summary>
        /// Converts the phrase to specified convention.
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="cases">A Capitalization Style (Pascal or Camel)</param>
        /// <description>
        /// Pascal case
        /// The first letter in the identifier and the first letter of each subsequent concatenated word are capitalized. You can use Pascal case for identifiers of three or more characters. For example:
        /// E.g. "BackColor"
        /// Camel case
        /// The first letter of an identifier is lowercase and the first letter of each subsequent concatenated word is capitalized. For example:
        /// E.g. "backColor"
        /// </description>
        /// <returns>string</returns>
        public static string ConvertCaseString(string phrase, Case cases)
        {
            string[] splittedPhrase = phrase.Split(' ', '-', '.');
            var sb = new StringBuilder();

            if (cases == Case.CamelCase)
            {
                sb.Append(splittedPhrase[0].ToLower());
                splittedPhrase[0] = string.Empty;
            }
            else if (cases == Case.PascalCase)
            {
                sb = new StringBuilder();
            }

            foreach (String s in splittedPhrase)
            {
                char[] splittedPhraseChars = s.ToCharArray();
                if (splittedPhraseChars.Length > 0)
                {
                    splittedPhraseChars[0] = ((new String(splittedPhraseChars[0], 1)).ToUpper().ToCharArray())[0];
                }
                sb.Append(new String(splittedPhraseChars));
            }
            return sb.ToString();
        }


        /// <summary>
        /// Convert the first letter to lowercase and return
        /// </summary>
        /// <param name="phrase">Phrase to convert</param>
        /// <returns>string</returns>
        public static string ConvertFirstLetterLowerCase(string phrase)
        {
            return Char.ToLowerInvariant(phrase[0]) + phrase.Substring(1);
        }

        /// <summary>
        /// Convert a hex string to uint (typically used for ARGB decimal representation)
        /// </summary>
        /// <param name="hexString">hex string (0x00... or just 00...)</param>
        /// <returns>the uint number</returns>
        public static uint HexStringToUint(string hexString)
        {
            if (hexString.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                hexString = hexString.Substring(2);
            }

            bool parsedSuccessfully = uint.TryParse(hexString,
                                                    NumberStyles.HexNumber,
                                                    CultureInfo.CurrentCulture,
                                                    out uint color);
            return color;
        }

        /// <summary>
        /// Convert uint to hex string
        /// </summary>
        /// <param name="c">uint</param>
        /// <returns>hex string on the format 0x00</returns>
        public static string ToHexString(uint c)
        {
            string s = String.Format("0x{0:X}", c);
            return s;
        }

        /// <summary>
        /// Convert byte to hex string
        /// </summary>
        /// <param name="b">byte</param>
        /// <returns>hex string as two characters</returns>
        public static string ToHexString(byte b)
        {
            char c = (char)b;
            string s = ToHexString(c);
            return s;
        }

        /// <summary>
        /// Convert char to hex string
        /// </summary>
        /// <param name="c">char</param>
        /// <returns>hex string as two characters</returns>
        public static string ToHexString(char c)
        {
            string s = String.Format("{0,0:X2}", (int)c);
            return s;
        }

        /// <summary>
        /// Convert byte array to hex and ascii string (like hex editor)
        /// </summary>
        /// <param name="byteData">byte array</param>
        /// <param name="invert">whether to invert the byte array</param>
        /// <param name="maxNumberOfLines">max number of lines to output (defaults to 20)</param>
        /// <returns>a hex editor string</returns>
        public static string ToHexEditorString(byte[] byteData, bool invert = false, int maxNumberOfLines = 20)
        {
            // output like a hex editor
            int splitLength = 16;

            if (byteData != null)
            {
                var sb = new StringBuilder();
                if (maxNumberOfLines * splitLength < byteData.Length)
                {
                    sb.AppendFormat("Byte Data (showing first {0} lines):\n", maxNumberOfLines);
                }
                else
                {
                    sb.AppendLine("Byte Data:");
                }

                foreach (var bytes in byteData.Take(splitLength * maxNumberOfLines).ToArray()
                                            .Split(splitLength))
                {
                    sb.AppendLine(ToHexAndAsciiString(bytes.ToArray(), false));
                }
                return sb.ToString();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Convert byte array to hex and ascii string (like hex editor)
        /// </summary>
        /// <param name="bytes">byte array</param>
        /// <param name="invert">whether to invert the byte array</param>
        /// <returns>string</returns>
        public static string ToHexAndAsciiString(byte[] bytes, bool invert = false)
        {
            var hex = new StringBuilder();
            var text = new StringBuilder();

            if (bytes != null)
            {
                var bytesCloned = (byte[])bytes.Clone();
                if (invert)
                {
                    Array.Reverse(bytesCloned);
                }

                var ch = new char[1];
                for (int x = 0; x < bytesCloned.Length; x++)
                {
                    ch[0] = (char)bytesCloned[x];
                    hex.AppendFormat("{0,0:X2} ", (int)ch[0]);

                    if (((int)ch[0] < 32) || ((int)ch[0] > 127))
                    {
                        ch[0] = '.';
                    }
                    text.Append(ch);
                }

                // append the text chunk after the hex chunk
                // the -48 is based on using the traditional size of 16 bytes per line
                // and each byte takes up 2 characters plus a space = 16*3 = 48
                return string.Format("{0,-48} {1}", hex, text);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Replace invalid characters with empty strings.
        /// only letters, dots, the email 'at' and '-' are allowed
        /// </summary>
        /// <param name="strIn">string</param>
        /// <returns>formatted string</returns>
        public static string RemoveInvalidCharacters(string strIn)
        {
            return Regex.Replace(strIn, @"[^\w\.@-]", string.Empty);
        }

        /// <summary>
        /// Remove non ascii characters from string but allow the space character
        /// </summary>
        /// <param name="strIn">string</param>
        /// <returns>formatted string</returns>
        public static string RemoveInvalidCharactersAllowSpace(string strIn)
        {
            // Replace invalid characters with empty strings.
            // only letters, dots, the email 'at' and '-' are allowed
            return Regex.Replace(strIn, @"[^\w\.\s@-]", string.Empty);
        }

        /// <summary>
        /// Remove non ascii characters from string
        /// </summary>
        /// <param name="strIn">string</param>
        /// <returns>formatted string</returns>
        public static string RemoveNonAsciiCharacters(string strIn)
        {
            return Regex.Replace(strIn, @"[^\u0000-\u007F]", string.Empty);
        }

        /// <summary>
        /// Faster version to remove non ascii characters from string
        /// According to https://stackoverflow.com/questions/3210393/how-do-i-remove-all-non-alphanumeric-characters-from-a-string-except-dash
        /// and
        /// https://rosettacode.org/wiki/Strip_control_codes_and_extended_characters_from_a_string#C.23
        /// </summary>
        /// <param name="strIn">string</param>
        /// <returns>formatted string</returns>
        public static string RemoveNonAsciiCharactersFast(string strIn)
        {
            StringBuilder buffer = new(strIn.Length); //Max length
            foreach (char ch in strIn)
            {
                UInt16 num = Convert.ToUInt16(ch); // In .NET, chars are UTF-16

                // The basic characters have the same code points as ASCII, and the extended characters are bigger
                if ((num >= 32u) && (num <= 126u)) buffer.Append(ch);
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Trim each line of the multi-line string and then trim the result.
        /// </summary>
        /// <param name="multiLineString">untrimmed multi-line string</param>
        /// <returns>trimmed multi-line string</returns>
        public static string TrimMultiLine(string multiLineString)
        {
            string trimmedByLine = string.Join(
                "\n",
                multiLineString.Split('\n').Select(s => s.Trim()).ToArray()).Trim();
            return trimmedByLine;
        }

        /// <summary>
        /// Make sure the passed string can be used as a filename
        /// </summary>
        /// <param name="name">filename to convert</param>
        /// <returns>valid filename</returns>
        public static string MakeValidFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidReStr = string.Format(@"[{0}]+", invalidChars);
            return Regex.Replace(name, invalidReStr, "_");
        }

        #region Escape and Unescape filepath methods        
        static readonly string invalidChars = @"""\/?:<>*|";
        static readonly string escapeChar = "%";

        static readonly Regex escaper = new(
            "[" + Regex.Escape(escapeChar + invalidChars) + "]",
            RegexOptions.Compiled);

        static readonly Regex unescaper = new(
            Regex.Escape(escapeChar) + "([0-9A-Z]{4})",
            RegexOptions.Compiled);

        /// <summary>
        /// Replaces any forbidden character with a % followed by its 16-bit representation in hex
        /// </summary>
        /// <see>https://stackoverflow.com/questions/15087444/c-sharp-file-path-encoding-and-decoding</see>
        /// <param name="path">file path</param>
        /// <returns>the path with forbidden characters replaced with a % followed by its 16-bit representations in hex</returns>
        public static string EscapeHex(string path)
        {
            return escaper.Replace(path,
                m => escapeChar + ((short)(m.Value[0])).ToString("X4"));
        }

        /// <summary>
        /// Replaces any 16-bit representation in hex back to characters that are forbidden in windows file paths
        /// </summary>
        /// <see>https://stackoverflow.com/questions/15087444/c-sharp-file-path-encoding-and-decoding</see>
        /// <param name="path">path with forbidden characters replaced with a % followed by its 16-bit representations in hex</param>
        /// <returns>the path with forbidden characters</returns>
        public static string UnescapeHex(string path)
        {
            return unescaper.Replace(path,
                m => ((char)Convert.ToInt16(m.Groups[1].Value, 16)).ToString());
        }

        private static Dictionary<string, string> EncodeMapping()
        {
            // Following characters are invalid for windows file and folder names.
            // \/:*?"<>|

            Dictionary<string, string> dic = new()
            {
                // use fullwidth character types
                // https://jrgraphix.net/r/Unicode/FF00-FFEF
                { @"\", "＼" },    // FF3C
                { "/", "／" },     // FF0F
                { ":", "：" },     // FF1A
                { "*", "＊" },     // FF0A
                { "?", "？" },     // FF1F
                { @"""", "＂" },   // FF02
                { "<", "＜" },     // FF1C
                { ">", "＞" },     // FF1E
                { "|", "｜" }      // FF5C
            };

            return dic;
        }

        /// <summary>
        /// Replaces any forbidden character with allowed representative characters
        /// </summary>
        /// <see>https://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name</see>
        /// <param name="path">file path</param>
        /// <returns>the path with forbidden characters replaced with allowed representative characters</returns>
        public static string EscapeRepresentative(string path)
        {
            foreach (KeyValuePair<string, string> replace in EncodeMapping())
            {
                path = path.Replace(replace.Key, replace.Value);
            }

            // handle dot at the end
            if (path.EndsWith(".")) path = path.Substring(0, path.Length - 1) + "．";

            return path;
        }

        /// <summary>
        /// Replaces representative characters back to characters that are forbidden in windows file paths
        /// </summary>
        /// <see>https://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name</see>
        /// <param name="path">path with forbidden characters replaced with representative characters</param>
        /// <returns>the path with forbidden characters</returns>
        public static string UnescapeRepresentative(string path)
        {
            foreach (KeyValuePair<string, string> replace in EncodeMapping())
            {
                path = path.Replace(replace.Value, replace.Key);
            }

            // handle dot at the end
            if (path.EndsWith("．")) path = path.Substring(0, path.Length - 1) + ".";

            return path;
        }
        #endregion        

        private static readonly string[] _headerEncodingTable = new string[] {
            "%00", "%01", "%02", "%03", "%04", "%05", "%06", "%07",
            "%08", "%09", "%0a", "%0b", "%0c", "%0d", "%0e", "%0f",
            "%10", "%11", "%12", "%13", "%14", "%15", "%16", "%17",
            "%18", "%19", "%1a", "%1b", "%1c", "%1d", "%1e", "%1f"
        };

        // Returns true if the string contains a control character (other than horizontal tab) or the DEL character.
        private static bool HeaderValueNeedsEncoding(string value)
        {
            foreach (char c in value)
            {
                if ((c < 32 && c != 9) || (c == 127))
                {
                    return true;
                }
            }
            return false;
        }

        // Encode the header if it contains a CRLF pair
        // VSWhidbey 257154
        public static string HeaderEncode(string value)
        {
            string sanitizedHeader = value;

            if (HeaderValueNeedsEncoding(value))
            {
                // DevDiv Bugs 146028
                // Denial Of Service scenarios involving 
                // control characters are possible.
                // We are encoding the following characters:
                // - All CTL characters except HT (horizontal tab)
                // - DEL character (\x7f)
                StringBuilder sb = new();
                foreach (char c in value)
                {
                    if (c < 32 && c != 9)
                    {
                        sb.Append(_headerEncodingTable[c]);
                    }
                    else if (c == 127)
                    {
                        sb.Append("%7f");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                sanitizedHeader = sb.ToString();
            }

            return sanitizedHeader;
        }

        /// <summary>
        /// Split string by length
        /// </summary>
        /// <param name="str">string</param>
        /// <param name="maxLength">max length</param>
        /// <returns>collection of strings</returns>
        public static IEnumerable<string> SplitByLength(this string str, int maxLength)
        {
            int index = 0;
            while (true)
            {
                if (index + maxLength >= str.Length)
                {
                    yield return str.Substring(index);
                    yield break;
                }
                yield return str.Substring(index, maxLength);
                index += maxLength;
            }
        }

        /// <summary>
        /// Remove trailing bytes that has passed value
        /// </summary>
        /// <param name="array">byte array</param>
        /// <param name="valueToCheck">byte value to remove</param>
        /// <returns>shortened byte array</returns>
        public static byte[] RemoveTrailingBytes(byte[] array, byte valueToCheck = 0)
        {
            int i = array.Length - 1;
            while (i >= 0 && array[i] == valueToCheck)
            {
                --i;
            }

            if (i < 0)
            {
                // every entry in the array has the valueToCheck value.
                return array;
            }
            else
            {
                // now array[i] is the last non-zero byte (if checking for zero)
                var fixedArray = new byte[i + 1];
                Array.Copy(array, fixedArray, i + 1);
                return fixedArray;
            }
        }

        /// <summary>
        /// Return current timestamp (i.e. Now)
        /// </summary>
        /// <returns>string representation of current time</returns>
        public static String GetCurrentTimestamp()
        {
            return GetTimestamp(DateTime.Now);
        }

        /// <summary>
        /// Return datetime as string
        /// </summary>
        /// <param name="value">datetime value</param>
        /// <returns>string</returns>
        public static String GetTimestamp(this DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        /// <summary>
        /// Return a number formatted with plus and minus signs
        /// </summary>
        /// <param name="number">double value</param>
        /// <returns>formatted string</returns>
        public static string GetNumberWithPlussAndMinusSign(double number)
        {
            return number.ToString("+#;-#;0");
        }

        /// <summary>
        /// Convert string to Enum type
        /// </summary>
        /// <param name="name">enum string value</param>
        /// <param name="ignoreCase">whether to ignore case</param>
        /// <returns>Enum</returns>
        /// <example>
        /// DaysOfWeek d = StringToEnum<DaysOfWeek>("Monday");
        /// d is now DaysOfWeek.Monday
        ///
        /// MonthsInYear m = StringToEnum<MonthsInYear>("January");
        /// m is now MonthsInYear.January
        /// 
        /// So what happens if you enter a string value that doesn't correspond to an enum? The Enum.Parse will fail with an ArgumentException.
        /// DaysOfWeek d = StringToEnum<DaysOfWeek>("Katillsday");
        /// 	throws an ArgumentException
        /// 	Requested value "Katillsday" was not found.
        ///
        /// We can get around this problem by first checking that the enum exists using Enum.IsDefined.
        /// if(Enum.IsDefined(typeof(DaysOfWeek), "Katillsday"))
        ///   StringToEnum<DaysOfWeek>("Katillsday");
        /// </example>
        public static T StringToEnum<T>(string name, bool ignoreCase = true)
        {
            return (T)Enum.Parse(typeof(T), name, ignoreCase);
        }

        /// <summary>
        /// Return all the enum values as a list
        /// </summary>
        /// <returns>Enum values as List</returns>
        public static List<T> EnumValuesToList<T>()
        {
            Type enumType = typeof(T);

            // Can't use type constraints on value types, so have to do check like this
            if (enumType.BaseType != typeof(Enum))
                throw new ArgumentException("T must be of type System.Enum");

            return new List<T>(Enum.GetValues(enumType) as IEnumerable<T>);
        }

        /// <summary>
        /// Test whether passsed object is a numberic value
        /// </summary>
        /// <param name="expression">object to be evaluated</param>
        /// <returns>true if numeric</returns>
        public static System.Boolean IsNumeric(System.Object expression)
        {
            if (expression == null || expression is DateTime)
                return false;

            if (expression is Int16 || expression is Int32 || expression is Int64 || expression is Decimal || expression is Single || expression is Double || expression is Boolean)
                return true;

            try
            {
                if (expression is string)
                    Double.Parse(expression as string);
                else
                    Double.Parse(expression.ToString());
                return true;
            }
            catch
            {
            } // just dismiss errors but return false
            return false;
        }

        /// <summary>
        /// Convert hex string to byte array
        /// </summary>
        /// <param name="hexString">hex string</param>
        /// <returns>byte array</returns>
        /// <see cref="CommonUtils.BinaryFile.HexStringToByteArray(string)"/>
        /// <seealso cref="ByteArrayToHexString(byte[])"/>
        public static byte[] HexStringToByteArray(string hex)
        {
            // The Linq version is readable but slow
            // return Enumerable.Range(0, hex.Length)
            //                      .Where(x => x % 2 == 0)
            //                      .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            //                      .ToArray();

            // This version is supposedly the fastest way
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        /// <summary>
        /// Return a int from a hex character
        /// </summary>
        /// <param name="hex">hex character</param>
        /// <returns>int value</returns>
        public static int GetHexVal(char hex)
        {
            int val = (int)hex;

            // For uppercase A-F letters:
            // return val - (val < 58 ? 48 : 55);

            // For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);

            // Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        /// <summary>
        /// Convert a byte array to hex string 
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <param name="doUppercase">true if uppercase hex is wanted</param>
        /// <returns>a hex string</returns>
        /// <see cref="HexStringToByteArray(string)"/>
        public static string ByteArrayToHexString(byte[] bytes, bool doUppercase = true)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                if (doUppercase)
                {
                    hex.AppendFormat("{0:X2}", b);
                }
                else
                {
                    hex.AppendFormat("{0:x2}", b);
                }
            }
            return hex.ToString();
        }

        /// <summary>
        /// Convert Binary String to hex representation
        /// </summary>
        /// <param name="binary">binary string</param>
        /// <returns>hexadecimal string representation</returns>
        public static string BinaryStringToHexString(string binary)
        {
            var result = new StringBuilder(binary.Length / 8 + 1);

            // TODO: check all 1's or 0's... Will throw otherwise

            int mod4Len = binary.Length % 8;
            if (mod4Len != 0)
            {
                // pad to length multiple of 8
                binary = binary.PadLeft(((binary.Length / 8) + 1) * 8, '0');
            }

            for (int i = 0; i < binary.Length; i += 8)
            {
                string eightBits = binary.Substring(i, 8);
                result.AppendFormat("{0:x2}", Convert.ToByte(eightBits, 2));
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert integer value to binary string
        /// </summary>
        /// <param name="value">integer value</param>
        /// <returns>string</returns>
        public static string IntegerToBinaryString(int value)
        {
            string binValue = Convert.ToString(value, 2);
            binValue = binValue.PadLeft(32, '0');
            return binValue;
        }

        /// <summary>
        /// Convert Binary String to integer
        /// </summary>
        /// <param name="binary">binary string</param>
        /// <returns>integer value</returns>
        public static int BinaryStringToInteger(string binary)
        {
            return Convert.ToInt32(binary, 2);
        }

        /// <summary>
        /// Convert long value to binary strign
        /// </summary>
        /// <param name="value">long value</param>
        /// <returns>binary string</returns>
        public static string LongToBinaryString(long value)
        {
            string binValue = Convert.ToString(value, 2);
            binValue = binValue.PadLeft(64, '0');
            return binValue;
        }

        /// <summary>
        /// Convert Binary String to ulong
        /// </summary>
        /// <param name="binary">binary string</param>
        /// <returns>ulong value</returns>
        public static ulong BinaryStringToLong(string binary)
        {
            return Convert.ToUInt64(binary, 2);
        }

        /// <summary>
        /// Convert string to byte array
        /// </summary>
        /// <param name="str">string</param>
        /// <returns>byte array</returns>
        public static byte[] GetBytes(string str)
        {
            var bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Convert byte array to string
        /// </summary>
        /// <param name="bytes">byte array</param>
        /// <returns>string</returns>
        public static string GetString(byte[] bytes)
        {
            var chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        /// <summary>
        /// Convert the string to Pascal case. 
        /// </summary>
        /// <param name="the_string">the string</param>
        /// <returns>the case converter to pascal case</returns>
        public static string ToPascalCase(this string the_string)
        {
            // If there are 0 or 1 characters, just return the string.
            if (the_string == null) return the_string;
            if (the_string.Length < 2) return the_string.ToUpper();

            // Split the string into words.
            string[] words = the_string.Split(
                new char[] { },
                StringSplitOptions.RemoveEmptyEntries);

            // Combine the words.
            string result = "";
            foreach (string word in words)
            {
                result +=
                    word.Substring(0, 1).ToUpper() +
                    word.Substring(1);
            }

            return result;
        }

        /// <summary>
        /// Convert the string to Camel case. 
        /// </summary>
        /// <param name="the_string">the string</param>
        /// <returns>the case converter to camel case</returns>
        public static string ToCamelCase(this string the_string)
        {
            // If there are 0 or 1 characters, just return the string.
            if (the_string == null || the_string.Length < 2) return the_string;

            // Split the string into words.
            string[] words = the_string.Split(
                new char[] { },
                StringSplitOptions.RemoveEmptyEntries);

            // Combine the words.
            string result = words[0].ToLower();
            for (int i = 1; i < words.Length; i++)
            {
                result +=
                    words[i].Substring(0, 1).ToUpper() +
                    words[i].Substring(1);
            }

            return result;
        }

        /// <summary>
        /// Capitalize the first character and add a space before 
        /// each capitalized letter (except the first character).
        /// </summary>
        /// <param name="the_string">the string</param>
        /// <returns>the case converter to proper case</returns>
        public static string ToProperCase(this string the_string)
        {
            // If there are 0 or 1 characters, just return the string.
            if (the_string == null) return the_string;
            if (the_string.Length < 2) return the_string.ToUpper();

            // Start with the first character.
            string result = the_string.Substring(0, 1).ToUpper();

            // Add the remaining characters.
            for (int i = 1; i < the_string.Length; i++)
            {
                if (Char.IsUpper(the_string[i])) result += " ";
                result += the_string[i];
            }

            return result;
        }

        /// <summary>
        /// Return everything in a string after a search word is found
        /// </summary>
        /// <param name="fullString">the full string</param>
        /// <param name="searchfor">a word within the string</param>
        /// <returns>everything in a string after a search word is found</returns>
        public static string GetStringAfterSearchWord(string fullString, string searchfor)
        {

            // we could have used regexp but this method is faster and safer
            string stringAfterSearchWord = "";
            int ix = fullString.IndexOf(searchfor, StringComparison.Ordinal);

            if (ix != -1)
            {
                stringAfterSearchWord = fullString.Substring(ix + searchfor.Length);
            }

            return stringAfterSearchWord;
        }

        /// <summary>
        /// Checks if the string contains only ASCII printable characters.
        /// 
        /// code>null</code> will return <code>false</code>.
        /// An empty String ("") will return <code>true</code>.
        /// 
        /// <pre>
        /// StringUtils.IsAsciiPrintable(null)     = false
        /// StringUtils.IsAsciiPrintable("")       = true
        /// StringUtils.IsAsciiPrintable(" ")      = true
        /// StringUtils.IsAsciiPrintable("Ceki")   = true
        /// StringUtils.IsAsciiPrintable("ab2c")   = true
        /// StringUtils.IsAsciiPrintable("!ab-c~") = true
        /// StringUtils.IsAsciiPrintable("\u0020") = true
        /// StringUtils.IsAsciiPrintable("\u0021") = true
        /// StringUtils.IsAsciiPrintable("\u007e") = true
        /// StringUtils.IsAsciiPrintable("\u007f") = false
        /// StringUtils.IsAsciiPrintable("Ceki G\u00fclc\u00fc") = false
        /// </pre>
        /// </summary>
        /// <param name="str">param str the string to check, may be null</param>
        /// <returns>return <code>true</code> if every character is in the range 32 thru 126</returns>
        public static bool IsAsciiPrintable(String str)
        {
            if (str == null)
            {
                return false;
            }
            int sz = str.Length;
            for (int i = 0; i < sz; i++)
            {
                if (IsAsciiPrintable(str[i]) == false)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks whether the character is ASCII 7 bit printable.
        /// <pre>
        ///   StringUtils.IsAsciiPrintable('a')  = true
        ///   StringUtils.IsAsciiPrintable('A')  = true
        ///   StringUtils.IsAsciiPrintable('3')  = true
        ///   StringUtils.IsAsciiPrintable('-')  = true
        ///   StringUtils.IsAsciiPrintable('\n') = false
        ///   StringUtils.IsAsciiPrintable('&copy;') = false
        /// </pre>
        /// </summary>
        /// <param name="ch">the character to check</param>
        /// <returns>true if between 32 and 126 inclusive</returns>
        public static bool IsAsciiPrintable(char ch)
        {
            return ch >= 32 && ch < 127;
        }

        /// <summary>
        /// Create a MD5 string of the input string
        /// </summary>
        /// <param name="input">input string</param>
        /// <returns>MD5 string</returns>
        public static string CreateMD5(string input)
        {
            // byte array representation of that string
            byte[] inputBytes = new UTF8Encoding().GetBytes(input);

            // need MD5 to calculate the hash
            byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(inputBytes);

            // string representation (similar to UNIX format)
            string encoded = BitConverter.ToString(hash)
               // without dashes
               .Replace("-", string.Empty)
               // make lowercase
               .ToLower();

            return encoded;
        }

        /// <summary>
        /// Remove the byte order mark from the passed string
        /// The UTF-8 representation of the Byte order mark is the (hexadecimal) byte sequence 0xEF,0xBB,0xBF
        /// </summary>
        /// <param name="value">string that starts or ends with a BOM</param>
        /// <returns>the string without the BOM</returns>
        public static string RemoveByteOrderMark(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // Check if the string starts with UTF-8 BOM (EF BB BF)
            if (value.StartsWith(UTF8BOM, StringComparison.Ordinal))
            {
                value = value.Remove(0, UTF8BOM.Length);
            }

            // Check if the string ends with UTF-8 BOM (EF BB BF)
            if (value.EndsWith(UTF8BOM, StringComparison.Ordinal))
            {
                value = value.Remove(value.Length - UTF8BOM.Length);
            }

            return value;
        }

        /// <summary>
        /// Get a substring of the first N characters.
        /// </summary>
        /// <param name="source">string</param>
        /// <param name="length">number of characters to truncate at</param>
        /// <param name="post">optional post string if truncating, i.e. " ..."</param>
        /// <returns>a substring of the first N characters.</returns>
        public static string Truncate(this string source, int length, string? post = null)
        {
            if (source.Length > length)
            {
                source = string.Format("{0}{1}", source.Substring(0, length), post ?? "");
            }
            return source;
        }

        /// <summary>
        /// Extracts the substring before the first space in a given string.
        /// If no space is found, returns the original string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The substring before the first space or the original string if no space is found.</returns>
        public static string ExtractBeforeSpace(string input)
        {
            // Find the index of the first space
            int spaceIndex = input.IndexOf(' ');

            // If a space is found, extract the substring before it; otherwise, return the original string
            return spaceIndex != -1 ? input.Substring(0, spaceIndex) : input;
        }
    }
}
