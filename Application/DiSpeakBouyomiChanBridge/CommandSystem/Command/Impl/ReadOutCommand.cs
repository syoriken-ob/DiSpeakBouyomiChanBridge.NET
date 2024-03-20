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
        MessageReadOutService.ReadOutMessage(Settings.AsMessage("Message.CommandsCount", commands.Count.ToString()));
        for (var i = 0; i < commands.Count; i++)
        {
            MessageReadOutService.ReadOutMessage(Settings.AsMessage("Message.CommandDetail", i.ToString(), commands[i].CommandTitle));
        }
    }
}
