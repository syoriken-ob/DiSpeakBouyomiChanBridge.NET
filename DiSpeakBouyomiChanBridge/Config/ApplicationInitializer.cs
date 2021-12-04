using net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl.Factory;
using net.boilingwater.DiSpeakBouyomiChanBridge.Http;
using net.boilingwater.DiSpeakBouyomiChanBridge.Log;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Config
{
    internal class ApplicationInitializer
    {
        public static void Initialize(bool useDiscordClient)
        {
            LoggerPool.Logger.Info("Start Application Initialization!");
            CommandInitialize();
            if (useDiscordClient)
            {
                HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.Instance.AsString("TryLoginToDiscord"));
                DiscordClient.Client.InitializeAsync().GetAwaiter().GetResult();
                HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.Instance.AsString("SuccessLoginToDiscord"));
            }
            else
            {
                HttpServerForBouyomiChan.Instance.Init();
            }
            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.Instance.AsString("FinishInitialize"));
            LoggerPool.Logger.Info("Finish Application Initialization!");
            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.Instance.AsString("Welcome"));
        }

        public static void CommandInitialize()
        {
            CommandFactory.Factory.Reload();
            SystemCommandFactory.Factory.Reload();
        }
    }
}
