using System.Net;

using net.boilingwater.Application.Common.Extensions;
using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Setting;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.VoiceReadout.HttpClients.Impl
{
    /// <summary>
    /// 棒読みちゃんにメッセージを送信するHttpClient
    /// </summary>
    public class HttpClientForBouyomiChan : HttpClientForReadOut
    {
        /// <summary>
        /// 棒読みちゃんにメッセージを送信します。
        /// </summary>
        /// <param name="text">送信するメッセージ</param>
        public override void ReadOut(string text)
        {
            var sendMessage = text.Trim();
            if (sendMessage.HasValue())
            {
                return;
            }
            Log.Logger.Info($"ReadOut Message: {sendMessage}");
            var retryCount = 0L;
            var isValid = false;
            while (true)
            {
                try
                {
                    var responseMessage = Client.Send(CreateBouyomiChanHttpRequest(sendMessage));
                    if (responseMessage.StatusCode == HttpStatusCode.OK)
                    {
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
                if (Settings.Get("RetryCount").HasValue() || retryCount++ < Settings.AsLong("RetryCount"))
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

        private static HttpRequestMessage CreateBouyomiChanHttpRequest(string text) => new()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"http://{Settings.AsString("BouyomiChanHost")}:{Settings.AsString("BouyomiChanPort")}/talk?text={Uri.EscapeDataString(text)}")
        };
    }
}
