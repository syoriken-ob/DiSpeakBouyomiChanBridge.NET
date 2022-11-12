using System;
using System.Net;
using System.Net.Http;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Service;
using net.boilingwater.Framework.Common.Http;
using net.boilingwater.Framework.Common.Logging;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.Http.Impl
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
        protected override void RegisterListeningUrlPrefix(HttpListenerPrefixCollection prefixes)
        {
            var url = $"http://localhost:{Settings.AsString("ListeningPort")}/";
            prefixes.Add(url);
        }

        /// <inheritdoc/>
        protected override void OnRequestReceived(IAsyncResult result)
        {
            // Listening処理
            var context = GetContextAndResumeListening(result);
            if (context == null)
            {
                return;
            }
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

            Log.Logger.Debug($"Receive({GetType().Name}) :{message}");

            CommandHandlingService.Handle(message);
        }
    }

    internal static partial class HttpListenerRequestExtension
    {
        public static string? GetDiscordMessage(this HttpListenerRequest request) => request.QueryString["text"];
    }
}
