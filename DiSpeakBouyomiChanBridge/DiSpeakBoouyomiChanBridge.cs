using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.DiSpeakBouyomiChanBridge.DiscordClient;
using net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl.Factory;
using net.boilingwater.DiSpeakBouyomiChanBridge.Http;
using System.Runtime.CompilerServices;

namespace net.boilingwater.DiSpeakBouyomiChanBridge
{
    public class DiSpeakBoouyomiChanBridge
    {
        public static void Main(string[] args)
        {
            RuntimeHelpers.RunClassConstructor(typeof(CommandFactory).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(SystemCommandFactory).TypeHandle);

            ApplicationInitializer.Initialize(Setting.Instance.AsBoolean("Use.InternalDiscordClient"));

            if (Setting.Instance.AsBoolean("Use.InternalDiscordClient"))
            {
                Client.StartAsync().GetAwaiter().GetResult();
            }
            else
            {
                HttpServerForBouyomiChan.Instance.Start();
            }
        }
    }
}