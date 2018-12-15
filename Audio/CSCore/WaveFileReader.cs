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

namespace CSCore.Codecs.WAV
{
    /// <summary>
    /// Provides a decoder for reading wave files.
    /// </summary>
    public class WaveFileReader : IWaveSource
    {
        private readonly List<WaveFileChunk> _chunks;

        private readonly object _lockObj = new object();

        private bool _disposed;
        private Stream _stream;
        private WaveFormat _waveFormat;
        private readonly DataChunk _dataChunk;
        private readonly bool _closeStream;
        public DataChunk DataChunk { get { return _dataChunk; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileReader" /> class.
        /// </summary>
        /// <param name="fileName">Filename which points to a wave file.</param>
        public WaveFileReader(string fileName)
            : this(File.OpenRead(fileName))
        {
            _closeStream = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileReader" /> class.
        /// </summary>
        /// <param name="stream">Stream which contains wave file data.</param>
        public WaveFileReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanRead)
                throw new ArgumentException("stream is not readable");

            _stream = stream;

            var reader = new BinaryReader(stream);

            var firstChunkId = new String(reader.ReadChars(4));
            if (firstChunkId == "RIFF")
            {
                // read RIFF data size
                var chunkSize = reader.ReadInt32();

                // read form-type (WAVE etc)
                var field = new string(reader.ReadChars(4));

                Log.Verbose("Processing RIFF. Data size: {0}, field: {1}", chunkSize, field);

                _chunks = ReadChunks(stream);
            }

            // Support more wav file formats
            // https://ccrma.stanford.edu/software/snd/snd/headers.c
            else if (firstChunkId == "IMPS")
            {
                /* ------------------------------------ Impulse Tracker -------------------------------------
                 * data from its2raw.c by Ben Collver
                 * 0:  IMPS
                 * 4:  filename (12 bytes)
                 * 17: global vol
                 * 18: flags (1: 16-bit or 8(0), 2: stereo or mono(0)
                 * 19: default vol
                 * 20: sample name (26 bytes)
                 * 46: convert
                 * 47: default pan
                 * 48: length (samps)
                 * 52: loop start
                 * 56: loop end
                 * 60: srate
                 * 64: sustain loop start
                 * 68: sustain loop end
                 * 72: data location
                 * 76: vib speed
                 * 77: vib depth
                 * 78: vib wave
                 * 79: vib rate
                 */
                string fileName = Encoding.ASCII.GetString(reader.ReadBytes(12));
                reader.ReadByte(); // skip bytes
                byte globalVol = reader.ReadByte();
                byte flags = reader.ReadByte();
                int channels = 0;
                int bitsPerSample = 0;

                // check if flags contain: (value & checkBit) == checkBit
                if ((flags & 4) == 4) channels = 2; else channels = 1;
                if ((flags & 2) == 2) bitsPerSample = 16; else bitsPerSample = 8;

                byte defaultVol = reader.ReadByte();
                string sampleName = Encoding.ASCII.GetString(reader.ReadBytes(26)).TrimEnd('\0');
                byte convert = reader.ReadByte();
                byte defaultPan = reader.ReadByte();
                int lengthInSamps = reader.ReadInt32();
                int loopStart = reader.ReadInt32();
                int loopEnd = reader.ReadInt32();
                int sampleRate = reader.ReadInt32();
                int sustainLoopStart = reader.ReadInt32();
                int sustainLoopEnd = reader.ReadInt32();
                int dataLocation = reader.ReadInt32();
                byte vibSpeed = reader.ReadByte();
                byte vibDepth = reader.ReadByte();
                byte vibWave = reader.ReadByte();
                byte vibRate = reader.ReadByte();

                _waveFormat = new WaveFormat(sampleRate, (short)bitsPerSample, (short)channels, AudioEncoding.Pcm);

                _chunks = new List<WaveFileChunk>(2);
                var tmp = new DataChunk(dataLocation, lengthInSamps * channels);
                _chunks.Add(tmp);
            }

            else
            {
                // unrecognized wave file
                _chunks = new List<WaveFileChunk>(2);
                Log.Warning("Unknown wave format. First chunk Id: {0}", firstChunkId);
            }

            Log.Verbose(GetWaveFileChunkInformation(Chunks));

            _dataChunk = (DataChunk)_chunks.FirstOrDefault(x => x is DataChunk);
            if (_dataChunk == null)
                throw new ArgumentException("The specified stream does not contain any data chunks.");

            Position = 0;
        }

        /// <summary>
        /// Gets a list of all found chunks.
        /// </summary>
        public ReadOnlyCollection<WaveFileChunk> Chunks
        {
            get { return _chunks.AsReadOnly(); }
        }

        /// <summary>
        /// Reads a sequence of bytes from the <see cref="WaveFileReader" /> and advances the position within the stream by the
        /// number of bytes read.
        /// </summary>
        /// <param name="buffer">
        /// An array of bytes. When this method returns, the <paramref name="buffer" /> contains the specified
        /// byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> +
        /// <paramref name="count" /> - 1) replaced by the bytes read from the current source.
        /// </param>
        /// <param name="offset">
        /// The zero-based byte offset in the <paramref name="buffer" /> at which to begin storing the data
        /// read from the current stream.
        /// </param>
        /// <param name="count">The maximum number of bytes to read from the current source.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            lock (_lockObj)
            {
                CheckForDisposed();

                count = (int)Math.Min(count, _dataChunk.DataEndPosition - _stream.Position);
                count -= count % WaveFormat.BlockAlign;
                if (count <= 0)
                    return 0;

                return _stream.Read(buffer, offset, count);
            }
        }

        /// <summary>
        /// Gets the wave format of the wave file. This property gets specified by the <see cref="FmtChunk" />.
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return _waveFormat; }
        }

        /// <summary>
        /// Gets or sets the position of the <see cref="WaveFileReader" /> in bytes.
        /// </summary>
        public long Position
        {
            get { return _stream != null ? _stream.Position - _dataChunk.DataStartPosition : 0; }
            set
            {
                lock (_lockObj)
                {
                    CheckForDisposed();

                    if (value > Length || value < 0)
                        throw new ArgumentOutOfRangeException("value", "The position must not be bigger than the length or less than zero.");

                    if (WaveFormat.BlockAlign > 0) value -= (value % WaveFormat.BlockAlign);
                    _stream.Position = value + _dataChunk.DataStartPosition;
                }
            }
        }

        /// <summary>
        /// Gets the length of the <see cref="WaveFileReader" /> in bytes.
        /// </summary>
        public long Length
        {
            get { return _dataChunk != null ? _dataChunk.ChunkDataSize : 0; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="WaveFileReader"/> supports seeking.
        /// </summary>
        public bool CanSeek
        {
            get { return true; }
        }

        /// <summary>
        /// Disposes the <see cref="WaveFileReader" /> and the underlying stream.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private List<WaveFileChunk> ReadChunks(Stream stream)
        {
            var chunks = new List<WaveFileChunk>(2);
            do
            {
                var tmp = WaveFileChunk.FromStream(stream);
                chunks.Add(tmp);

                var fmtChunk = tmp as FmtChunk;
                if (fmtChunk != null)
                {
                    _waveFormat = fmtChunk.WaveFormat;
                }
                else
                {
                    // One tricky thing about RIFF file chunks is that they must be word aligned. 
                    // This means that their total size must be a multiple of 2 bytes (ie. 2, 4, 6, 8, and so on). 
                    // If a chunk contains an odd number of data bytes, causing it not to be word aligned, 
                    // an extra padding byte with a value of zero must follow the last data byte. 
                    // This extra padding byte is not counted in the chunk size, therefore a program must always 
                    // word align a chunk headers size value in order to calculate the offset of the following chunk.
                    // ensure the text size is word aligned (2 bytes)
                    var wordAlignedChunkSize = tmp.ChunkDataSize + tmp.ChunkDataSize % 2;

                    stream.Seek(wordAlignedChunkSize, SeekOrigin.Current);
                }

            } while (stream.Length - stream.Position > 8); //8 bytes = size of chunk header

            return chunks;
        }

        private void CheckForDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Disposes the <see cref="WaveFileReader" /> and the underlying stream.
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
                    if (_stream != null && _closeStream)
                    {
                        _stream.Dispose();
                        _stream = null;
                    }
                }
            }
            _disposed = true;
        }

        /// <summary>
        /// Destructor which calls the <see cref="Dispose(bool)"/> method.
        /// </summary>
        ~WaveFileReader()
        {
            Dispose(false);
        }

        /// <summary>
        /// Return a string describing the chunks found in the file
        /// </summary>
        /// <param name="chunks"></param>
        /// <returns></returns>
        public static string GetWaveFileChunkInformation(ReadOnlyCollection<WaveFileChunk> chunks)
        {
            var writer = new StringWriter();
            foreach (var chunk in chunks)
            {
                if (chunk is FmtChunk)
                {
                    writer.Write("Format chunk: \"{0}\": ", FourCC.FromFourCC(chunk.ChunkID));
                    writer.Write(" Format: {0}", ((FmtChunk)chunk).WaveFormat);
                    writer.Write(", Data size: {0}", ((FmtChunk)chunk).ChunkDataSize);
                    writer.Write(", Start pos: {0}", ((FmtChunk)chunk).StartPosition);
                    writer.Write(", End pos: {0}\n", ((FmtChunk)chunk).EndPosition);
                }
                else if (chunk is DataChunk)
                {
                    writer.Write("Data chunk \"{0}\"", FourCC.FromFourCC(chunk.ChunkID));
                    writer.Write(", Data size: {0}", ((DataChunk)chunk).ChunkDataSize);
                    writer.Write(", Data start pos: {0}", ((DataChunk)chunk).DataStartPosition);
                    writer.Write(", Data end pos: {0}\n", ((DataChunk)chunk).DataEndPosition);
                }
                else if (chunk is ListChunk)
                {
                    writer.Write("List chunk \"{0}\"", FourCC.FromFourCC(chunk.ChunkID));
                    writer.Write(", Data size: {0}", ((ListChunk)chunk).ChunkDataSize);
                    writer.Write(", Start pos: {0}", ((ListChunk)chunk).StartPosition);
                    writer.Write(", End pos: {0}", ((ListChunk)chunk).EndPosition);
                    if (((ListChunk)chunk).InfoTags != null)
                    {
                        foreach (var infoTag in ((ListChunk)chunk).InfoTags)
                        {
                            writer.Write(", {0} = {1}", infoTag.Key, infoTag.Value);
                        }
                    }
                    writer.Write("\n");
                }
                else
                {
                    int id = chunk.ChunkID;
                    writer.Write("Unknown chunk \"{0}\"", StringUtils.IsAsciiPrintable(FourCC.FromFourCC(id)) ? FourCC.FromFourCC(id) : string.Format("int {0} is not FourCC", id));
                    writer.Write(", Data size: {0}", chunk.ChunkDataSize);
                    writer.Write(", Start pos: {0}", chunk.StartPosition);
                    writer.Write(", End pos: {0}\n", chunk.EndPosition);
                }
            }
            return writer.ToString();
        }

    }
}