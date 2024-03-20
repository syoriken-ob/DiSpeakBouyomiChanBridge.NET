using net.boilingwater.Application.VoiceVoxReverseProxy.Http;
using net.boilingwater.Framework.Common.Initialize;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core.Initialize;
using net.boilingwater.Framework.Core.Logging;

namespace net.boilingwater.Application.VoiceVoxReverseProxy;

internal class ApplicationInitializer
{
    /// <summary>
    /// システムの初期化を行います
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    internal static void Initialize()
    {
        CoreInitializer.Initialize();
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
