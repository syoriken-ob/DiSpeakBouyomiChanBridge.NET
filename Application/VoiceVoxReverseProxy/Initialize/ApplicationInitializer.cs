using net.boilingwater.Application.VoiceVoxReverseProxy.Http;
using net.boilingwater.Framework.Common.Initialize;
using net.boilingwater.Framework.Common.Logging;
using net.boilingwater.Framework.Common.Setting;

namespace net.boilingwater.Application.VoiceVoxReverseProxy
{
    internal class ApplicationInitializer
    {
        /// <summary>
        /// システムの初期化を行います
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        internal static void Initialize()
        {
            CommonInitializer.Initialize();
            Log.Logger.Info(Settings.AsString("Message.Log.Initialize.Start"));

            VoiceVoxHttpClientManager.Initialize();
            HttpServerForVoiceVoxBridge.Instance.Initialize();

            Log.Logger.Info(Settings.AsString("Message.Log.Initialize.Finish"));
        }

        /// <summary>
        /// 処理を開始します
        /// </summary>
        internal static void Start()
        {
            Log.Logger.Info(Settings.AsString("Message.Log.Welcome"));
            HttpServerForVoiceVoxBridge.Instance.Start();
        }
    }
}
