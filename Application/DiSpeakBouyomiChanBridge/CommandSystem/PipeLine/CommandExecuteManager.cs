using System.Collections.Generic;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl;
using net.boilingwater.Framework.Core.Logging;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.PipeLine;

internal class CommandExecuteManager
{
    internal static CommandExecuteManager Instance { get; private set; } = new();

    //------------------------------------------------//
    private readonly CommandExecutor CommonCommandExecutor;

    private readonly CommandExecutor ImmediateCommandExecutor;

    internal CommandExecuteManager()
    {
        CommonCommandExecutor = new();
        ImmediateCommandExecutor = new();
    }

    /// <summary>
    /// 実行キューに<paramref name="command"/> を追加します
    /// </summary>
    /// <param name="command">追加したいコマンド</param>
    internal void AddCommand(Command command)
    {
        if (command.Immediate)
        {
            ImmediateCommandExecutor.Add(command);
            Log.Logger.Debug("Add Command Emergency Execute Queue.");
        }
        else
        {
            CommonCommandExecutor.Add(command);
            Log.Logger.Debug("Add Command Execute Queue.");
        }
    }

    /// <summary>
    /// キューに入っているコマンドを取得します
    /// </summary>
    /// <returns></returns>
    internal List<Command> GetCommandsInQueue() => CommonCommandExecutor.GetCommandsInQueue();

    /// <summary>
    /// コマンドを停止し、実行キューをクリアします
    /// </summary>
    internal void ShutdownThreads()
    {
        Command.KillProcess();
        ImmediateCommandExecutor?.ClearTask();
        CommonCommandExecutor?.ClearTask();
    }
}
