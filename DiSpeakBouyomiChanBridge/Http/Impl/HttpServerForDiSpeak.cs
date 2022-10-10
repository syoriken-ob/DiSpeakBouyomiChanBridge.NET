using System;
using System.Net;
using System.Net.Http;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.Application.Common.Utils;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Service;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Http.Impl
{
    /// <summary>
    /// DiSpeakからメッセージを受信するHttpServer
    /// </summary>
    public class HttpServerForDiSpeak : AbstractHttpServer
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static HttpServerForDiSpeak Instance { get; private set; }

        static HttpServerForDiSpeak() => Instance = new();

        /// <inheritdoc/>
        protected override void RegisterListenningUrlPrefix(HttpListenerPrefixCollection prefixs)
        {
            var url = $"http://localhost:{Settings.AsString("ListeningPort")}/";
            prefixs.Add(url);
        }

        /// <inheritdoc/>
        protected override void OnRequestReceived(IAsyncResult result)
        {
            // Listening処理
            var context = GetContextAndResumeListenning(result);
            var request = context.Request;
            var message = "";
            using (var response = context.Response)
            {
                if (request.HttpMethod != HttpMethod.Get.Method)
                {
                    return;
                }

                message = CastUtil.ToString(request.GetDiscordMessage());
                response.StatusCode = 200;
            }

            Log.Logger.DebugFormat("Receive :{0}", message);

            CommandHandlingService.Handle(message);
        }
    }

    internal static partial class HttpListennerRequestExtention
    {
        public static string? GetDiscordMessage(this HttpListenerRequest request) => request.QueryString["text"];
    }
}
