namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Dto
{
    /// <summary>
    /// コマンド検出コンテキスト
    /// </summary>
    /// <remarks>
    /// テキスト情報からコマンド検出コンテキストを初期化します。
    /// </remarks>
    /// <param name="message">読み上げメッセージ</param>
    public class CommandHandlingContext(string message, string userId = "")
    {
        /// <summary>
        /// ユーザーID
        /// </summary>
        public string User = userId;

        /// <summary>
        /// メッセージ
        /// </summary>
        public string Message = message;
    }
}
