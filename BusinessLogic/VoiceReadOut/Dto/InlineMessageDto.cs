namespace net.boilingwater.BusinessLogic.VoiceReadOut.Dto
{
    public class InlineMessageDto
    {
        public InlineMessageDto(string message, string speakerId = "")
        {
            Message = message;
            SpeakerKey = speakerId;
        }

        /// <summary>
        /// 話者IDを指定します。
        /// </summary>
        /// <remarks>現時点ではVoiceVoxでのみ利用されます</remarks>
        public string SpeakerKey { get; private set; }

        /// <summary>
        /// 読み上げるメッセージを指定します。
        /// </summary>
        public string Message { get; private set; }
    }
}
