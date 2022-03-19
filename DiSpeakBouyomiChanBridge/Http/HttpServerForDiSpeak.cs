using System;
using System.Net;
using System.Net.Http;
using System.Threading;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.Application.Common.Utils;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Http
{
    /// <summary>
    /// DiSpeakからメッセージを受信するHttpServer
    /// </summary>
    public class HttpServerForDiSpeak
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static HttpServerForDiSpeak Instance { get; private set; }

        static HttpServerForDiSpeak() => Instance = new();

        private HttpListener? _listener;

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <param name="refreshHttpListener">HttpListenerを初期化するか</param>
        public void Initialize(bool refreshHttpListener = true)
        {

            if (refreshHttpListener)
            {
                var retryCount = 0L;
                var isValid = false;
                do
                {
                    try
                    {
                        if (_listener != null)
                        {
                            _listener.Stop();
                        }
                        _listener = new HttpListener();
                        _listener.Prefixes.Add($"http://localhost:{Settings.AsString("ListeningPort")}/");

                        _listener.Start();
                        HttpClientForBouyomiChan.Instance.SendToBouyomiChan(Settings.AsString("Message.Connecting"));
                        isValid = true;
                        _listener.Stop();
                        break;
                    }
                    catch (Exception)
                    {
                        Log.Logger.FatalFormat("Fail to Open ListeningPort:{0} !", Settings.AsString("ListeningPort"));
                        Log.Logger.DebugFormat("Retry Connect:{0}/{1} !", retryCount, Settings.AsLong("RetryCount"));
                        Thread.Sleep(Settings.AsInteger("RetrySleepTime.Milliseconds"));
                    }
                } while (string.IsNullOrEmpty(Settings.AsString("RetryCount")) || retryCount++ < Settings.AsInteger("RetryCount"));

                if (!isValid)
                {
                    Log.Logger.Fatal("Exit Program!");
                }
            }
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <exception cref="InvalidOperationException">HttpListenerの初期化に失敗していた場合発生します</exception>
        public void Start()
        {
            if (_listener == null)
            {
                throw new InvalidOperationException("初期化されていません");
            }

            _listener.Start();
            Handle();
        }

        /// <summary>
        /// コマンドの検出処理
        /// </summary>
        /// <exception cref="InvalidOperationException">HttpListenerの初期化に失敗していた場合発生します</exception>
        private void Handle()
        {
            if (_listener == null)
            {
                throw new InvalidOperationException("初期化されていません");
            }

            while (true)
            {
                // Listening処理
                var context = _listener.GetContext();
                var request = context.Request;
                var message = "";
                using (var response = context.Response)
                {
                    if (request.HttpMethod != HttpMethod.Get.Method)
                    {
                        continue;
                    }

                    message = CastUtil.ToString(request.GetDiscordMessage());
                    response.StatusCode = 200;
                }

                Log.Logger.DebugFormat("Receive :{0}", message);

                CommandHandlingService.Handle(message);
            }
        }
    }

    internal static class DiscordRequestExtention
    {
        public static string? GetDiscordMessage(this HttpListenerRequest request) => request.QueryString["text"];
    }
}
