using System;

namespace net.boilingwater.BusinessLogic.VoiceVoxSpeakerCache.Dto
{
    /// <summary>
    /// VoiceVox話者Dto
    /// </summary>
    public struct SpeakerRemappingDto
    {
        /// <summary>
        /// VoiceVox話者UUID
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// VoiceVox話者ID
        /// </summary>
        public string Id { get; set; }
    }
}
