using net.boilingwater.Application.VoiceVoxReverseProxy.Http;
using net.boilingwater.Framework.Common.Initialize;
using net.boilingwater.Framework.Common.Logging;

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
            Log.Logger.Info("アプリケーションの初期化処理を開始します。");

            VoiceVoxHttpClientManager.Initialize();
            HttpServerForVoiceVoxBridge.Instance.Initialize();

            Log.Logger.Info("アプリケーションの初期化処理が完了しました。");
        }

        /// <summary>
        /// 処理を開始します
        /// </summary>
        internal static void Start()
        {
            HttpServerForVoiceVoxBridge.Instance.Start();
        }
    }
}
