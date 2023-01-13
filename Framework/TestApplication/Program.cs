using net.boilingwater.Framework.Common.Initialize;
using net.boilingwater.Framework.Common.Logging;
using net.boilingwater.Framework.Common.Setting;

namespace TestApplication
{
    internal class Program
    {
        private static void Main()
        {
            CommonInitializer.Initialize();
            Log.Logger.Info(Settings.AsMessage("Message.FinishInitialize"));
            Log.Logger.Info(Settings.AsMessage("Message.Test", typeof(Program).FullName ?? ""));
            Log.Logger.Info(Settings.AsMessage("Message.Welcome"));
        }
    }
}
