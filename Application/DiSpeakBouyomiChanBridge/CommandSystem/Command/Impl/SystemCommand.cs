using System.Collections.Generic;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.PipeLine;
using net.boilingwater.BusinessLogic.VoiceReadout.HttpClients;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core.Logging;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem
{
    /// <summary>
    /// プログラム処理を定義したコマンドの基底クラス
    /// </summary>
    public abstract class SystemCommand : ExecutableCommand
    {
    }
}

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl
{
    /// <summary>
    /// リロード処理を実行します。
    /// </summary>
    public class ReloadCommand : SystemCommand
    {
        /// <summary>
        /// 処理を実行します
        /// </summary>
        public override void Execute()
        {
            ApplicationInitializer.Initialize();
            ApplicationInitializer.Start();
            Log.Logger.Info("Reload SystemConfig...");
            HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.ReloadConfig"));
        }
    }

    /// <summary>
    /// キューに入ったコマンドをすべて停止します。
    /// </summary>
    public class ShutdownCommand : SystemCommand
    {
        /// <summary>
        /// 処理を実行します
        /// </summary>
        public override void Execute()
        {
            CommandExecuteManager.Instance.ShutdownThreads();
            Log.Logger.Info("Shutdown CommandThreads...");
            HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.DeleteAllExecutionQueues"));
        }
    }

    /// <summary>
    /// キューに入ったコマンドを出力します。
    /// </summary>
    public class ReadOutCommand : SystemCommand
    {
        /// <summary>
        /// 処理を実行します
        /// </summary>
        public override void Execute()
        {
            List<Command> commands = CommandExecuteManager.Instance.GetCommandsInQueue();
            HttpClientForReadOut.Instance?.ReadOut(string.Format(Settings.AsString("Message.CommandsCount"), commands.Count));
            for (var i = 0; i < commands.Count; i++)
            {
                HttpClientForReadOut.Instance?.ReadOut(
                    string.Format(
                        Settings.AsString("Message.CommandDetail"),
                        i,
                        commands[i].CommandTitle
                    )
                );
            }
        }
    }
}
