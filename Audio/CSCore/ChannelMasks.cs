using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CSCore
{

    /// <summary>
    /// Defines common channelmasks.
    /// </summary>
    public static class ChannelMasks
    {
        /// <summary>
        /// Mono.
        /// </summary>
        public const ChannelMask MonoMask = ChannelMask.SpeakerFrontCenter;
        /// <summary>
        /// Stereo.
        /// </summary>
        public const ChannelMask StereoMask = ChannelMask.SpeakerFrontLeft | ChannelMask.SpeakerFrontRight;
        /// <summary>
        /// 5.1 surround with rear speakers.
        /// </summary>
        public const ChannelMask FiveDotOneWithRearMask = ChannelMask.SpeakerFrontLeft | ChannelMask.SpeakerFrontRight | ChannelMask.SpeakerFrontCenter | ChannelMask.SpeakerLowFrequency | ChannelMask.SpeakerBackLeft | ChannelMask.SpeakerBackRight;
        /// <summary>
        /// 5.1 surround with side speakers.
        /// </summary>
        public const ChannelMask FiveDotOneWithSideMask = ChannelMask.SpeakerFrontLeft | ChannelMask.SpeakerFrontRight | ChannelMask.SpeakerFrontCenter | ChannelMask.SpeakerLowFrequency | ChannelMask.SpeakerSideLeft | ChannelMask.SpeakerSideRight;
        /// <summary>
        /// 7.1 surround.
        /// </summary>
        public const ChannelMask SevenDotOneMask = ChannelMask.SpeakerFrontLeft | ChannelMask.SpeakerFrontRight | ChannelMask.SpeakerFrontCenter | ChannelMask.SpeakerLowFrequency | ChannelMask.SpeakerSideLeft | ChannelMask.SpeakerSideRight | ChannelMask.SpeakerBackLeft | ChannelMask.SpeakerBackRight;

        /// <summary>
        /// Return a ChannelMask based on the number of channels
        /// </summary>
        /// <param name="channelCount">number of channels</param>
        /// <returns>a ChannelMask</returns>
        public static ChannelMask GetChannelMask(int channelCount)
        {
            // Assume a setup of: FL, FR, FC, LFE, BL, BR, SL & SR. 
            // Otherwise, MCL will use: FL, FR, FC, LFE, BL, BR, FLoC & FRoC.
            if (channelCount == 0)
            {
                throw new Exception("Channels cannot be zero.");
            }
            else if (channelCount == 8)
            {
                return SevenDotOneMask;
            }

            // Otherwise follow MCL.
            ChannelMask mask = 0;
            var channels = new ChannelMask[18];
            Enum.GetValues(typeof(ChannelMask)).CopyTo(channels, 0);

            for (var i = 0; i < channelCount; i++)
            {
                mask |= channels[i];
            }

            return mask;
        }

        /// <summary>
        /// Return Channel Information
        /// </summary>
        /// <param name="channelMask">channel mask</param>
        /// <returns>channel information</returns>
        public static string GetChannelInformation(ChannelMask channelMask)
        {
            var writer = new StringWriter();
            writer.Write("SpeakerPositions: ");
            foreach (var ch in Enum.GetValues(typeof(ChannelMask)))
            {
                if (((uint)channelMask & (uint)ch) == (uint)ch)
                {
                    writer.Write((ChannelMask)ch);
                    writer.Write(",");
                }
            }
            return writer.ToString();
        }

    }
}
