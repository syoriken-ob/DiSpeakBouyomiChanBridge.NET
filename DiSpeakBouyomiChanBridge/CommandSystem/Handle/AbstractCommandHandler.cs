using System;
using System.Threading.Tasks;

using net.boilingwater.Application.Common.Extentions;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Impl;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Impl.Factory;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.PipeLine;
using net.boilingwater.DiSpeakBouyomiChanBridge.Http;
using net.boilingwater.DiSpeakBouyomiChanBridge.InternalDiscordClient.Services;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Handle
{
    internal abstract class AbstractCommandHandler
    {
        internal void ExecutePreProcess(ref string message) { }

        internal void Handle(ref string message)
        {
            if (SystemCommandFactory.Factory == null || CommandFactory.Factory == null)
            {
                throw new InvalidOperationException("コマンドファクトリが初期化されていません。");
            }

            //システムコマンド検出
            var systemCmds = SystemCommandFactory.Factory.CreateExecutableCommands(ref message);
            //コマンド検出
            var cmds = CommandFactory.Factory.CreateExecutableCommands(ref message);

            if (systemCmds != null)
            {
                //システムコマンド実行
                systemCmds.ForEach((cmd) => new Task(() => cmd.Execute()).RunSynchronously());
            }
            if (cmds != null)
            {
                //コマンドを実行キューに追加
                cmds.ForEach(cmd => CommandExecuteManager.Instance.AddCommand((Command)cmd));
            }
        }

        internal void ExecutePostProcess(ref string message)
        {
            //URL省略などの処理
            DiscordReceivedMessageService.ReplaceCommonReceivedInfoAfter(ref message);

            //棒読みちゃんに送信
            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(message);
        }
    }
}
