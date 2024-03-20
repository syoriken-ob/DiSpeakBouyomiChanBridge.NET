using System.Collections.Generic;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.PipeLine;
using net.boilingwater.BusinessLogic.VoiceReadOut.Service;
using net.boilingwater.Framework.Common.Setting;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl;

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
