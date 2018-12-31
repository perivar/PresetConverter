using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CommonUtils;
using CommonUtils.Audio;
using Serilog;

namespace CommonUtils.Audio.RIFF
{
    /// <summary>
    /// Provides a decoder for reading RIFF files.
    /// </summary>
    public class RIFFFileReader
    {
        private readonly List<RIFFFileChunk> _chunks;
        private readonly object _lockObj = new object();
        private bool _disposed;
        private BinaryFile _binaryFile;
        private readonly bool _closeBinaryFile;
        private readonly bool _useWordAlignment;

        public BinaryFile BinaryFile
        {
            get { return _binaryFile; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RIFFFileReader" /> class.
        /// </summary>
        /// <param name="fileName">Filename which points to a wave file.</param>
        /// <param name="useWordAlignment">whether to ensure word-alignment (defaults to false)</param>
        public RIFFFileReader(string fileName, bool useWordAlignment = false)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");

            _useWordAlignment = useWordAlignment;

            _binaryFile = new BinaryFile(fileName, BinaryFile.ByteOrder.BigEndian);

            var firstChunkId = new String(_binaryFile.ReadChars(4));
            if (firstChunkId == "RIFF")
            {
                // read RIFF data size
                var chunkSize = _binaryFile.ReadInt32();

                // read form-type (WAVE etc)
                var field = new string(_binaryFile.ReadChars(4));

                Log.Verbose("Processing RIFF. Data size: {0}, field: {1}", chunkSize, field);

                _chunks = ReadChunks(_binaryFile);
            }
            else
            {
                // unrecognized file format, not a RIFF File
                _chunks = new List<RIFFFileChunk>(2);
                Log.Error("Unknown format (not RIFF). First chunk Id: {0}", firstChunkId);
            }

            Log.Verbose(GetRIFFFileChunkInformation(Chunks));

            _binaryFile.Position = 0;
        }

        /// <summary>
        /// Gets a list of all found chunks.
        /// </summary>
        public ReadOnlyCollection<RIFFFileChunk> Chunks
        {
            get { return _chunks.AsReadOnly(); }
        }

        /// <summary>
        /// Disposes the <see cref="RIFFFileReader" /> and the underlying stream.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private List<RIFFFileChunk> ReadChunks(BinaryFile binaryFile)
        {
            var chunks = new List<RIFFFileChunk>(2);
            do
            {
                var tmp = RIFFFileChunk.FromBinaryFile(binaryFile);
                chunks.Add(tmp);

                if (_useWordAlignment)
                {
                    // One tricky thing about RIFF file chunks is that they must be word aligned. 
                    // This means that their total size must be a multiple of 2 bytes (ie. 2, 4, 6, 8, and so on). 
                    // If a chunk contains an odd number of data bytes, causing it not to be word aligned, 
                    // an extra padding byte with a value of zero must follow the last data byte. 
                    // This extra padding byte is not counted in the chunk size, therefore a program must always 
                    // word align a chunk headers size value in order to calculate the offset of the following chunk.
                    // ensure the text size is word aligned (2 bytes)
                    var wordAlignedChunkSize = tmp.ChunkDataSize + tmp.ChunkDataSize % 2;
                    binaryFile.Seek(wordAlignedChunkSize, SeekOrigin.Current);
                }
                else
                {
                    // seek on actual chunk size 
                    binaryFile.Seek(tmp.ChunkDataSize, SeekOrigin.Current);
                }

            } while (binaryFile.Length - binaryFile.Position > 8); // 8 bytes = size of chunk header

            return chunks;
        }

        private void CheckForDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Disposes the <see cref="RIFFFileReader" /> and the underlying stream.
        /// </summary>
        /// <param name="disposing">
        /// True to release both managed and unmanaged resources; false to release only unmanaged
        /// resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                lock (_lockObj)
                {
                    if (_binaryFile != null && _closeBinaryFile)
                    {
                        _binaryFile.Dispose();
                        _binaryFile = null;
                    }
                }
            }
            _disposed = true;
        }

        /// <summary>
        /// Destructor which calls the <see cref="Dispose(bool)"/> method.
        /// </summary>
        ~RIFFFileReader()
        {
            Dispose(false);
        }

        /// <summary>
        /// Return a string describing the chunks found in the file
        /// </summary>
        /// <param name="chunks"></param>
        /// <returns></returns>
        public static string GetRIFFFileChunkInformation(ReadOnlyCollection<RIFFFileChunk> chunks)
        {
            var writer = new StringWriter();
            foreach (var chunk in chunks)
            {
                int id = chunk.ChunkID;
                writer.Write("Unknown chunk \"{0}\"", StringUtils.IsAsciiPrintable(FourCC.FromFourCC(id)) ? FourCC.FromFourCC(id) : string.Format("int {0} is not FourCC", id));
                writer.Write(", Data size: {0}", chunk.ChunkDataSize);
                writer.Write(", Start pos: {0}", chunk.StartPosition);
                writer.Write(", End pos: {0}\n", chunk.EndPosition);
            }
            return writer.ToString();
        }

    }
}