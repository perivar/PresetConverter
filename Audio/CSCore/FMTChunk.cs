using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;

namespace CSCore.Codecs.WAV
{
    /// <summary>
    /// Represents the <see cref="FmtChunk" /> of a wave file.
    /// </summary>
    public class FmtChunk : WaveFileChunk
    {
        /// <summary>
        /// Chunk ID of the <see cref="FmtChunk" />.
        /// </summary>
        public const int FmtChunkID = 0x20746D66;

        private readonly WaveFormat _waveFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="FmtChunk" /> class.
        /// </summary>
        /// <param name="stream"><see cref="Stream" /> which contains the fmt chunk.</param>
        public FmtChunk(Stream stream)
            : this(new BinaryReader(stream))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FmtChunk" /> class.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader" /> which should be used to read the fmt chunk.</param>
        public FmtChunk(BinaryReader reader)
            : base(reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            if (ChunkID == FmtChunkID) //"fmt "
            {
                var encoding = (AudioEncoding)reader.ReadInt16();
                short channels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                int avgBps = reader.ReadInt32();
                short blockAlign = reader.ReadInt16();
                short bitsPerSample = reader.ReadInt16();

                short extraSize = 0;
                if (ChunkDataSize > 16)
                {
                    extraSize = reader.ReadInt16();

                    if (extraSize != ChunkDataSize - 18)
                    {
                        Log.Verbose("Format chunk extra size mismatch. Reported: {0}, but really {1} bytes.", extraSize, (short)(ChunkDataSize - 18));
                        extraSize = (short)(ChunkDataSize - 18);
                    }

                    if (extraSize == 22)
                    {
                        // we haave an Extensible wave format
                        short numberOfValidBits = reader.ReadInt16();
                        if (numberOfValidBits == 0)
                        {
                            numberOfValidBits = bitsPerSample;
                        }

                        var channelMask = (ChannelMask)reader.ReadUInt32();
                        if ((uint)channelMask == 0)
                        {
                            // no mask given
                            channelMask = ChannelMasks.GetChannelMask(channels);
                        }

                        // read GUID, including the data format code 
                        // The first two bytes of the GUID form the sub-code specifying the data format code, e.g. WAVE_FORMAT_PCM. 
                        // The remaining 14 bytes contain a fixed string, 
                        // «\x00\x00\x00\x00\x10\x00\x80\x00\x00\xAA\x00\x38\x9B\x71».
                        // var subEncoding = (AudioEncoding)reader.ReadInt16();
                        // var subEncodingFixed = reader.ReadBytes(14);
                        var guidData = reader.ReadBytes(16); // complete 16 byte guid data

                        // if all bytes are zero - force AudioSubTypes.Pcm
                        bool hasAllZeroes = guidData.All(singleByte => singleByte == 0);
                        var guid = AudioSubTypes.Pcm;
                        if (!hasAllZeroes)
                        {
                            guid = new Guid(guidData);
                        }

                        var waveFormatExtensible = new WaveFormatExtensible(sampleRate, bitsPerSample, channels, guid, channelMask);
                        waveFormatExtensible.AverageBytesPerSecond = avgBps;
                        waveFormatExtensible.BlockAlign = blockAlign;
                        waveFormatExtensible.ValidBitsPerSample = numberOfValidBits;
                        _waveFormat = waveFormatExtensible;
                        return;
                    }
                    else
                    {
                        // ignore extra size bytes
                        if (extraSize > 0)
                        {
                            reader.BaseStream.Position += extraSize;
                        }
                    }
                }
                _waveFormat = new WaveFormat(sampleRate, (short)bitsPerSample, (short)channels, encoding, avgBps, (short)blockAlign, extraSize);
            }
        }

        /// <summary>
        /// Gets the <see cref="WaveFormat" /> specified by the <see cref="FmtChunk" />.
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return _waveFormat; }
        }
    }
}