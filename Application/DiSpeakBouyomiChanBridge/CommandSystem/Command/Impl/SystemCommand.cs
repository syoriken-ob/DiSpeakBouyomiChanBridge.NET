using System.Collections.Generic;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.PipeLine;
using net.boilingwater.BusinessLogic.VoiceReadOut.Service;
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
            MessageReadOutService.ReadOutMessage(Settings.AsString("Message.ReloadConfig"));
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
            MessageReadOutService.ReadOutMessage(Settings.AsString("Message.DeleteAllExecutionQueues"));
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
            MessageReadOutService.ReadOutMessage(string.Format(Settings.AsString("Message.CommandsCount"), commands.Count));
            for (var i = 0; i < commands.Count; i++)
            {
                MessageReadOutService.ReadOutMessage(
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
