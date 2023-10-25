using Discord.Commands;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Dto
{
    /// <summary>
    /// コマンド検出コンテキスト
    /// </summary>
    public class CommandHandlingContext
    {
        /// <summary>
        /// ユーザーID
        /// </summary>
        public string User;

        /// <summary>
        /// メッセージ
        /// </summary>
        public string Message;

        /// <summary>
        /// テキスト情報からコマンド検出コンテキストを初期化します。
        /// </summary>
        /// <param name="message">読み上げメッセージ</param>
        public CommandHandlingContext(string message, string userId = "")
        {
            Message = message;
            User = userId;
        }
    }
}
