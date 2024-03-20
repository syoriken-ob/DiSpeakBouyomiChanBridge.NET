namespace net.boilingwater.BusinessLogic.VoiceReadOut.Dto;

public class InlineMessageDto(string message, string speakerId = "")
{
    /// <summary>
    /// 話者IDを指定します。
    /// </summary>
    /// <remarks>現時点ではVoiceVoxでのみ利用されます</remarks>
    public string SpeakerKey { get; private set; } = speakerId;

    /// <summary>
    /// 読み上げるメッセージを指定します。
    /// </summary>
    public string Message { get; private set; } = message;
}
