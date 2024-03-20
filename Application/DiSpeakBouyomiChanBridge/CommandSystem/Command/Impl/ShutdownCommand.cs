using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.PipeLine;
using net.boilingwater.BusinessLogic.VoiceReadOut.Service;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core.Logging;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl;

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
