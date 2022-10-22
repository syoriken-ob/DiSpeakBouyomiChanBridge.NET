using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.VoiceReadout.HttpClients;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.PipeLine;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem
{
    /// <summary>
    /// プログラム処理を定義したコマンドの基底クラス
    /// </summary>
    public abstract class SystemCommand : ExecutableCommand
    {
    }
}

namespace net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Impl
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
            var commands = CommandExecuteManager.Instance.GetCommandsInQueue();
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
