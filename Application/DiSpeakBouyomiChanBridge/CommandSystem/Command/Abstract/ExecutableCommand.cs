namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem
{
    /// <summary>
    /// 文字列中から検出する実行可能なコマンドの基底クラス
    /// </summary>
    public abstract class ExecutableCommand
    {
        /// <summary>
        /// 処理を実行します
        /// </summary>
        public abstract void Execute();
    }
}
