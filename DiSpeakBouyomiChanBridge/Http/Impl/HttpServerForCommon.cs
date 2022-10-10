using System;
using System.Net;
using System.Net.Http;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.Application.Common.Utils;
using net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.VoiceReadout.HttpClients;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Http.Impl
{
    /// <summary>
    /// メッセージを受信する共通HttpServer
    /// </summary>
    /// <remarks>コマンド検出処理を行わず、そのまま読み上げ処理を行います。</remarks>
    public class HttpServerForCommon : AbstractHttpServer
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static HttpServerForCommon Instance { get; private set; }

        static HttpServerForCommon() => Instance = new();

        /// <inheritdoc/>
        protected override void RegisterListenningUrlPrefix(HttpListenerPrefixCollection prefixs)
        {
            var url = $"http://localhost:{Settings.AsString("CommonVoiceReadoutServer.ListeningPort")}/";
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

                message = CastUtil.ToString(request.GetTextMessage("text"));
                response.StatusCode = 200;
            }

            Log.Logger.DebugFormat("Receive :{0}", message);

            HttpClientForReadOut.Instance?.ReadOut(message);
        }
    }

    internal static partial class HttpListennerRequestExtention
    {
        public static string? GetTextMessage(this HttpListenerRequest request, string parameterName) => request.QueryString[parameterName];
    }
}
