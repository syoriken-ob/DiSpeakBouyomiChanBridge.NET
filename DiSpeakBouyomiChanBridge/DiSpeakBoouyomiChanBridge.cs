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

            HttpServerForBouyomiChan.Instance.Start();
        }
    }
}