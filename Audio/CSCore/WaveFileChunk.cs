using System;
using System.IO;
using CommonUtils;
using CommonUtils.Audio;
using Serilog;
using Serilog.Events;

namespace CSCore.Codecs.WAV
{
    /// <summary>
    /// Represents a wave file chunk. For more information see
    /// <see href="http://www.sonicspot.com/guide/wavefiles.html#wavefilechunks" />.
    /// </summary>
    public class WaveFileChunk
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileChunk" /> class.
        /// </summary>
        /// <param name="stream"><see cref="Stream" /> which contains the wave file chunk.</param>
        public WaveFileChunk(Stream stream)
            : this(new BinaryReader(stream))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileChunk" /> class.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader" /> which should be used to read the wave file chunk.</param>
        public WaveFileChunk(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            ChunkID = reader.ReadInt32();
            ChunkDataSize = reader.ReadUInt32();

            StartPosition = reader.BaseStream.Position;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileChunk" /> class.
        /// </summary>
        /// <param name="startPosition">Zero-based position inside of the stream at which the audio data starts</param> 
        /// <param name="chunkDataSize">Gets the data size of the chunk.</param>
        public WaveFileChunk(long startPosition, long chunkDataSize)
        {
            StartPosition = startPosition;
            ChunkDataSize = chunkDataSize;
        }

        /// <summary>
        /// Gets the unique ID of the Chunk. Each type of chunk has its own id.
        /// </summary>
        public int ChunkID { get; private set; }

        /// <summary>
        /// Gets the data size of the chunk.
        /// </summary>
        public long ChunkDataSize { get; private set; }

        /// <summary>
        /// Parses the <paramref name="stream" /> and returns a <see cref="WaveFileChunk" />. Note that the position of the
        /// stream has to point to a wave file chunk.
        /// </summary>
        /// <param name="stream"><see cref="Stream" /> which points to a wave file chunk.</param>
        /// <returns>
        /// Instance of the <see cref="WaveFileChunk" /> class or any derived classes. It the stream does not point to a
        /// wave file chunk the instance of the <see cref="WaveFileChunk" /> which gets return will be invalid.
        /// </returns>
        public static WaveFileChunk FromStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (stream.CanRead == false)
                throw new ArgumentException("stream is not readable");

            var reader = new BinaryReader(stream);
            int id = reader.ReadInt32();
            stream.Position -= 4;

            if (StringUtils.IsAsciiPrintable(FourCC.FromFourCC(id)))
            {
                Log.Verbose("Processing chunk: {0}", FourCC.FromFourCC(id));
            }
            else
            {
                if (id == 0)
                {
                    // likely corrupt wav file with alot of crap after the chunk
                    // skip bytes until only 8 bytes are left
                    // stream.Position = stream.Length - 8;
                }
                else
                {
                    // try to fix chunks that are not word-aligned but should have been?!
                    Log.Verbose("Processing chunk: {0}", string.Format("{0} is not FourCC", id));
                    long origPos = stream.Position;

                    // rewind one byte and try again
                    stream.Position -= 1;
                    int id2ndTry = reader.ReadInt32();
                    stream.Position -= 4;

                    if (StringUtils.IsAsciiPrintable(FourCC.FromFourCC(id2ndTry)))
                    {
                        // we believe it worked
                        Log.Verbose("Seem to have fixed non word-aligned chunk: {0}", FourCC.FromFourCC(id2ndTry));
                    }
                    else
                    {
                        // still didn't work
                        // put position back to where it was.
                        stream.Position = origPos;
                    }
                }
            }

            // check https://github.com/michaelwu/libsndfile/blob/master/src/wav.c
            // for all possible chunks ids
            if (id == FmtChunk.FmtChunkID)
                return new FmtChunk(reader);
            if (id == DataChunk.DataChunkID)
                return new DataChunk(reader);
            if (id == ListChunk.ListChunkID)
                return new ListChunk(reader);

            // TODO: add bext metadata tag support?
            // The European Broadcast Union (EBU) has standardized on an extension to the WAVE format that they call Broadcast WAVE format (BWF).  
            // It is aimed at carrying PCM or MPEG audio data. In its simplest form, it adds a <bext> chunk with additional metadata.  
            // https://tech.ebu.ch/docs/tech/tech3285.pdf

            return new WaveFileChunk(reader);
        }

        /// <summary>
        /// Gets the zero-based position inside of the stream at which the chunk data starts.
        /// </summary>
        public long StartPosition { get; private set; }

        /// <summary>
        /// Gets the zero-based position inside of the stream at which the chunk data ends.
        /// </summary>
        public long EndPosition
        {
            get { return StartPosition + ChunkDataSize; }
        }
    }
}