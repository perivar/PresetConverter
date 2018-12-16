using System;
using System.IO;
using CommonUtils;
using CommonUtils.Audio;
using Serilog;
using Serilog.Events;

namespace CSCore.Codecs.RIFF
{
    /// <summary>
    /// Represents a riff file chunk.
    /// </summary>
    public class RIFFFileChunk
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RIFFFileChunk" /> class.
        /// </summary>
        /// <param name="binaryFile"><see cref="BinaryFile" /> which contains the riff file chunk.</param>
        public RIFFFileChunk(BinaryFile binaryFile)
        {
            if (binaryFile == null)
                throw new ArgumentNullException("binaryFile");

            ChunkID = binaryFile.ReadInt32(BinaryFile.ByteOrder.LittleEndian); // Steinberg CPR files have LittleEndian FourCCs
            ChunkDataSize = binaryFile.ReadUInt32();
            StartPosition = binaryFile.Position;

            // Log.Verbose("Processing '{0}'. Start position: {1}, Data size: {2}, End position: {3}", StringUtils.IsAsciiPrintable(FourCC.FromFourCC(ChunkID)) ? FourCC.FromFourCC(ChunkID) : string.Format("int {0} is not FourCC", ChunkID), StartPosition, ChunkDataSize, EndPosition);
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
        /// Parses the <paramref name="binaryFile" /> and returns a <see cref="RIFFFileChunk" />. Note that the position of the
        /// stream has to point to a riff file chunk.
        /// </summary>
        /// <param name="binaryFile"><see cref="BinaryFile" /> which contains the riff file chunk.</param>
        /// <returns>
        /// Instance of the <see cref="RIFFFileChunk" /> class or any derived classes. It the stream does not point to a
        /// wave file chunk the instance of the <see cref="RIFFFileChunk" /> which gets return will be invalid.
        /// </returns>
        public static RIFFFileChunk FromBinaryFile(BinaryFile binaryFile)
        {
            if (binaryFile == null)
                throw new ArgumentNullException("binaryFile");
            if (binaryFile.CanRead == false)
                throw new ArgumentException("binaryFile is not readable");

            int id = binaryFile.ReadInt32(BinaryFile.ByteOrder.LittleEndian); // Steinberg CPR files have LittleEndian FourCCs
            binaryFile.Position -= 4;

            if (StringUtils.IsAsciiPrintable(FourCC.FromFourCC(id)))
            {
                Log.Verbose("Processing chunk: '{0}'", FourCC.FromFourCC(id));
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
                    long origPos = binaryFile.Position;

                    // rewind one byte and try again
                    binaryFile.Position -= 1;
                    int id2ndTry = binaryFile.ReadInt32();
                    binaryFile.Position -= 4;

                    if (StringUtils.IsAsciiPrintable(FourCC.FromFourCC(id2ndTry)))
                    {
                        // we believe it worked
                        Log.Verbose("Seem to have fixed non word-aligned chunk: {0}", FourCC.FromFourCC(id2ndTry));
                    }
                    else
                    {
                        // still didn't work
                        // put position back to where it was.
                        binaryFile.Position = origPos;
                    }
                }
            }

            return new RIFFFileChunk(binaryFile);
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