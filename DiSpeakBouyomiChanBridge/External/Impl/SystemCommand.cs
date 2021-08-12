using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.DiSpeakBouyomiChanBridge.Http;
using net.boilingwater.DiSpeakBouyomiChanBridge.Log;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.External
{
    public abstract class SystemCommand : ExecutableCommand
    {
    }
}

namespace net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl
{
    public class ReloadCommand : SystemCommand
    {
        public override void Execute()
        {
            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.AsString("ReloadConfig"));
            LoggerPool.Logger.Info("Reload SystemConfig...");
            HttpServerForBouyomiChan.Instance.Init(false);
        }
    }

    public class ShutdownCommand : SystemCommand
    {
        public override void Execute()
        {
            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.AsString("DeleteAllExecutionQueues"));
            LoggerPool.Logger.Info("Shutdown CommandThreads...");
            CommandExecutor.ShutdownThreads();
        }
    }
}