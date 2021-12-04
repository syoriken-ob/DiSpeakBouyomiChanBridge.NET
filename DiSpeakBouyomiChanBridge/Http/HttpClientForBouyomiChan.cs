using System;
using System.Net;
using System.Net.Http;
using System.Threading;

using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.DiSpeakBouyomiChanBridge.Log;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Http
{
    public class HttpClientForBouyomiChan : IDisposable
    {
        public static HttpClientForBouyomiChan Instance { get; } = new();

        private HttpClient _client;

        private HttpClientForBouyomiChan() => _client = new();

        public void RenewHttpClient()
        {
            ((IDisposable)this).Dispose();
            _client = new();
        }

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
                        LoggerPool.Logger.Debug($"Send:{sendMessage}");
                        isValid = true;
                    }
                }
                catch (Exception) { isValid = false; }

                if (isValid)
                {
                    return;
                }

                LoggerPool.Logger.Fatal($"Fail to Send Message to BouyomiChan(http://{Setting.Instance.AsString("BouyomiChanHost")}:{Setting.Instance.AsString("BouyomiChanPort")}) : {sendMessage}");
                if (string.IsNullOrEmpty(Setting.Instance.Get("RetryCount")) || retryCount++ < Setting.Instance.AsLong("RetryCount"))
                {
                    LoggerPool.Logger.DebugFormat("Retry Connect:{0}/{1}", retryCount, Setting.Instance.AsLong("RetryCount"));
                    Thread.Sleep(Setting.Instance.AsInteger("RetrySleepTime.Milliseconds"));
                }
                else
                {
                    return;
                }
            }
        }

        void IDisposable.Dispose() => _client.Dispose();

        private static HttpRequestMessage CreateBouyomiChanHttpRequest(string text) => new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"http://{Setting.Instance.AsString("BouyomiChanHost")}:{Setting.Instance.AsString("BouyomiChanPort")}/talk?text={Uri.EscapeUriString(text)}")
        };
    }
}