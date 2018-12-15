using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using CommonUtils;

namespace CSCore.Codecs.WAV
{
    /// <summary>
    /// Represents the <see cref="ListChunk" /> of a wave file.
    /// </summary>
    public class ListChunk : WaveFileChunk
    {
        /// <summary>
        /// Chunk ID of the <see cref="ListChunk" />.
        /// </summary>
        public const int ListChunkID = 0x5453494c;

        private readonly Dictionary<string, string> _infoTags;


        /// <summary>
        /// Initializes a new instance of the <see cref="ListChunk" /> class.
        /// </summary>
        /// <param name="stream"><see cref="Stream" /> which contains the fmt chunk.</param>
        public ListChunk(Stream stream)
            : this(new BinaryReader(stream))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListChunk" /> class.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader" /> which should be used to read the fmt chunk.</param>
        public ListChunk(BinaryReader reader)
            : base(reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            if (ChunkID == ListChunkID) //"list"
            {
                long listStartPosition = reader.BaseStream.Position;
                long listEndPosition = listStartPosition + ChunkDataSize;

                String listtype = new String(reader.ReadChars(4));
                if (listtype == "INFO")
                {
                    _infoTags = new Dictionary<string, string>();
                    while (reader.BaseStream.Position < listEndPosition)
                    {
                        var infoKey = new String(reader.ReadChars(4));
                        var infoValueChunkSize = reader.ReadUInt32();

                        // ensure the text size is word aligned (2 bytes)
                        infoValueChunkSize += infoValueChunkSize % 2;

                        // always read binary text as bytes and then create a string
                        // otherwise (e.g. by using reader.ReadChars() you can get a error like:
                        // The output char buffer is too small to contain the decoded characters, encoding 'Unicode (UTF-8)'
                        var bytes = reader.ReadBytes((int)infoValueChunkSize);
                        var infoValue = ASCIIEncoding.ASCII.GetString(bytes);

                        // remove the non printable characters
                        infoValue = StringUtils.RemoveNonAsciiCharactersFast(infoValue);

                        _infoTags.Add(infoKey, infoValue);
                    }
                }

                // make sure to reset the position to the beginning
                // since the ReadChunks will seek until the end of this chunk using the ChunkDataSize variable
                reader.BaseStream.Position = listStartPosition;
            }
        }

        /// <summary>
        /// Gets a list of all found info elements.
        /// </summary>
        public IReadOnlyDictionary<string, string> InfoTags
        {
            get { return _infoTags; }
        }
    }
}