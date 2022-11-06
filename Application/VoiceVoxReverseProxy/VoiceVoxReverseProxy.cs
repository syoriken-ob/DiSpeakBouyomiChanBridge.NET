using net.boilingwater.Framework.Common.Logging;

namespace net.boilingwater.VoiceVoxProxy
{
    public class VoiceVoxReverseProxy
    {
        public static int Main(string[] args)
        {
            try
            {
                ApplicationInitializer.Initialize();
                ApplicationInitializer.Start();
                while (true)
                {
                    Console.ReadLine();
                }
            }
            catch (Exception e)
            {
                Log.Logger.Fatal("エラーが発生したため、実行を終了します。", e);
                return -1;
            }
            return 0;
        }
    }
}
