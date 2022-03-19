using System;
using System.Net;
using System.Net.Http;
using System.Threading;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Http
{
    /// <summary>
    /// 棒読みちゃんにメッセージを送信するHttpClient
    /// </summary>
    public class HttpClientForBouyomiChan : IDisposable
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static HttpClientForBouyomiChan Instance { get; } = new();

        private HttpClient _client;

        private HttpClientForBouyomiChan() => _client = new();

        /// <summary>
        /// 内部Httpクライアントを再生成します。
        /// </summary>
        public void RenewHttpClient()
        {
            ((IDisposable)this).Dispose();
            _client = new();
        }

        /// <summary>
        /// 棒読みちゃんにメッセージを送信します。
        /// </summary>
        /// <param name="text">送信するメッセージ</param>
        public void SendToBouyomiChan(string text)
        {
            var sendMessage = text.Trim();
            if (string.IsNullOrEmpty(sendMessage))
            {
                return;
            }

            var retryCount = 0L;
            var isValid = false;
            while (true)
            {
                try
                {
                    var responseMessage = _client.Send(CreateBouyomiChanHttpRequest(sendMessage));
                    if (responseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        Log.Logger.Debug($"Send:{sendMessage}");
                        isValid = true;
                    }
                }
                catch (Exception)
                {
                    isValid = false;
                }

                if (isValid)
                {
                    return;
                }

                Log.Logger.Fatal($"Fail to Send Message to BouyomiChan(http://{Settings.AsString("BouyomiChanHost")}:{Settings.AsString("BouyomiChanPort")}) : {sendMessage}");
                if (string.IsNullOrEmpty(Settings.Get("RetryCount")) || retryCount++ < Settings.AsLong("RetryCount"))
                {
                    Log.Logger.DebugFormat("Retry Connect:{0}/{1}", retryCount, Settings.AsLong("RetryCount"));
                    Thread.Sleep(Settings.AsInteger("RetrySleepTime.Milliseconds"));
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// リソースを解放します
        /// </summary>
        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }

        private static HttpRequestMessage CreateBouyomiChanHttpRequest(string text) => new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"http://{Settings.AsString("BouyomiChanHost")}:{Settings.AsString("BouyomiChanPort")}/talk?text={Uri.EscapeDataString(text)}")
        };
    }
}
