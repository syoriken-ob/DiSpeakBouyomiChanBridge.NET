using System.Collections.Generic;

namespace net.boilingwater.BusinessLogic.VoiceReadOut.Dto
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="inlineMessages">メッセージを読み上げる単位で分割して登録します</param>
    /// <param name="userDefaultSpeakerKey">ユーザーのデフォルト話者ID</param>
    public class MessageDto(List<InlineMessageDto> inlineMessages, string userDefaultSpeakerKey = "")
    {
        /// <summary>
        ///ユーザーのデフォルト話者キー
        /// </summary>
        /// <remarks>VoiceVoxでのみ利用されます</remarks>
        public string UserDefaultSpeakerKey { get; private set; } = userDefaultSpeakerKey;

        /// <summary>
        /// 読み上げメッセージ
        /// </summary>
        public List<InlineMessageDto> InlineMessages { get; private set; } = inlineMessages;
    }
}
