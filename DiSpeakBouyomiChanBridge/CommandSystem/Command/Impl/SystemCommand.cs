using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.PipeLine;
using net.boilingwater.DiSpeakBouyomiChanBridge.Http;

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
            HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.ReloadConfig"));
            Log.Logger.Info("Reload SystemConfig...");
            ApplicationInitializer.CommandInitialize();
            SettingHolder.Initialize();
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
            HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.DeleteAllExecutionQueues"));
            Log.Logger.Info("Shutdown CommandThreads...");
            CommandExecuteManager.Instance.ShutdownThreads();
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
