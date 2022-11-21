using System.Collections.Generic;

using net.boilingwater.Framework.Common;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem
{
    /// <summary>
    /// <see cref="ExecutableCommand"/>を文字列中から作成するファクトリの基底クラス
    /// </summary>
    public abstract class ExecutableCommandFactory
    {
        /// <summary>
        /// コマンドを保持する辞書
        /// </summary>
        public SimpleDic<ExecutableCommand> Dic { get; protected set; } = new();

        /// <summary>
        /// コマンドファクトリの初期化を行います
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// コマンドを検出します。
        /// </summary>
        /// <param name="input">コマンドを含む文字列<br/>検出したコマンドは文字列中から削除されます</param>
        /// <returns>文字列中から検出した<seealso cref="IEnumerable{ExecutableCommand}"/></returns>
        public abstract IEnumerable<ExecutableCommand> CreateExecutableCommands(ref string input);
    }
}
