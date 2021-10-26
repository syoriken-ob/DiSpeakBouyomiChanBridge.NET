using System.Threading.Tasks;

using net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl;
using net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl.Factory;
using net.boilingwater.DiSpeakBouyomiChanBridge.Http;
using net.boilingwater.Utils.Extention;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.External
{
    internal class CommandHandlingService
    {
        public static void Handle(string message)
        {

            //システムコマンド検出
            var systemCmds = SystemCommandFactory.Factory.CreateExecutableCommands(ref message);
            //コマンド検出
            var cmds = CommandFactory.Factory.CreateExecutableCommands(ref message);
            //棒読みちゃんに送信
            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(message);

            if (systemCmds != null)
            {
                //システムコマンド実行
                systemCmds.ForEach((cmd) => new Task(() => cmd.Execute()).RunSynchronously());
            }
            if (cmds != null)
            {
                //コマンドを実行キューに追加
                cmds.ForEach(cmd => CommandExecutor.AddCommand((Command)cmd));
            }
        }
    }
}
