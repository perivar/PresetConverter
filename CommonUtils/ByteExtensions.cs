using System;
using System.Collections.Generic;
using System.IO;

namespace CommonUtils
{
    public static class ByteExtensions
    {
        /// <summary>
        /// Search with an array of bytes to find a specific pattern
        /// </summary>
        /// <param name="byteArray">byte array</param>
        /// <param name="bytePattern">byte array pattern</param>
        /// <param name="startIndex">index to start searching at</param>
        /// <param name="count">how many elements to look through</param>
        /// <returns>position</returns>
        /// <example>
        /// find the last 'List' entry	
        /// reading all bytes at once is not very performant, but works for these relatively small files	
        /// byte[] allBytes = File.ReadAllBytes(fileName);
        /// reading from the end of the file by reversing the array	
        /// byte[] reversed = allBytes.Reverse().ToArray();
        /// find 'List' backwards	
        /// int reverseIndex = IndexOfBytes(reversed, Encoding.UTF8.GetBytes("tsiL"), 0, reversed.Length);
        /// if (reverseIndex < 0)
        /// {
        /// reverseIndex = 64;
        /// }
        /// int index = allBytes.Length - reverseIndex - 4; // length of List is 4	
        /// Log.Debug("File length: {0}, 'List' found at index: {1}", allBytes.Length, index);
        /// </example>
        public static int IndexOf(this byte[] byteArray, byte[] bytePattern, int startIndex = -1, int count = -1)
        {
            if (byteArray == null || byteArray.Length == 0 || bytePattern == null || bytePattern.Length == 0 || count == 0)
            {
                return -1;
            }

            int i = startIndex > 0 ? startIndex : 0;
            int endIndex = count > 0 ? Math.Min(startIndex + count, byteArray.Length) : byteArray.Length;
            int foundIndex = 0;
            int lastFoundIndex = 0;

            while (i < endIndex)
            {
                lastFoundIndex = foundIndex;
                foundIndex = (byteArray[i] == bytePattern[foundIndex]) ? ++foundIndex : 0;
                if (foundIndex == bytePattern.Length)
                {
                    return i - foundIndex + 1;
                }
                if (lastFoundIndex > 0 && foundIndex == 0)
                {
                    i = i - lastFoundIndex;
                    lastFoundIndex = 0;
                }
                i++;
            }
            return -1;
        }

        /// <summary>
        /// Find all occurrences of byte pattern in byte array
        /// </summary>
        /// <param name="byteArray">byte array</param>
        /// <param name="bytePattern">byte array pattern</param>
        /// <param name="startIndex">optional start index within the byte array</param>
        /// <param name="maxEndIndex">optional end index within the byte array</param>
        /// <returns>positions</returns>
        /// <example>
        /// foreach (int i in FindAll(byteArray, bytePattern))
        /// {
        ///    Console.WriteLine(i);
        /// }
        /// </example>
        public static IEnumerable<int> FindAll(this byte[] byteArray, byte[] bytePattern, int startIndex = -1, int maxEndIndex = -1)
        {
            startIndex = startIndex > 0 ? startIndex : 0;
            maxEndIndex = maxEndIndex > 0 ? maxEndIndex : byteArray.Length;

            for (int startSearchIndex = startIndex; startSearchIndex < maxEndIndex - bytePattern.Length;)
            {
                int count = maxEndIndex - startSearchIndex;
                int i = IndexOf(byteArray, bytePattern, startSearchIndex, count);

                if (i < 0) break;
                yield return i;

                startSearchIndex = i + 1;
            }
        }

        /// <summary>
        /// Seek in BinaryFile until pattern is found, return start index
        /// </summary>
        /// <param name="binaryFile">binaryFile</param>
        /// <param name="pattern">byte pattern to find</param>
        /// <param name="offset">offset to seek to (if > 0)</param>
        /// <param name="maxEndIndex">optional end index within the BinaryFile</param>
        /// <returns>index where found (BinaryFile position will be at this index)</returns>
        public static int IndexOf(this BinaryFile binaryFile, byte[] pattern, int offset, int maxEndIndex = -1)
        {
            // seek to offset
            if (offset > 0 && offset < binaryFile.Length)
            {
                binaryFile.Seek(offset, SeekOrigin.Begin);
            }

            int success = 0;
            long remainingBytes = binaryFile.Length - binaryFile.Position;

            if (maxEndIndex > 0)
            {
                remainingBytes = Math.Min(maxEndIndex, remainingBytes);
            }

            for (int i = 0; i < (int)remainingBytes; i++)
            {
                var b = binaryFile.ReadByte();
                if (b == pattern[success])
                {
                    success++;
                }
                else
                {
                    success = 0;
                }

                if (pattern.Length == success)
                {
                    int index = (int)(binaryFile.Position - pattern.Length);
                    binaryFile.Seek(index, SeekOrigin.Begin);
                    return index;
                }
            }
            return -1;
        }
    }
}