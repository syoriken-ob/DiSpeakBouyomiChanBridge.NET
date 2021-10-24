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
            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.Instance.AsString("ReloadConfig"));
            LoggerPool.Logger.Info("Reload SystemConfig...");
            ApplicationInitializer.CommandInitialize();
        }
    }

    public class ShutdownCommand : SystemCommand
    {
        public override void Execute()
        {
            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.Instance.AsString("DeleteAllExecutionQueues"));
            LoggerPool.Logger.Info("Shutdown CommandThreads...");
            CommandExecutor.ShutdownThreads();
        }
    }
}