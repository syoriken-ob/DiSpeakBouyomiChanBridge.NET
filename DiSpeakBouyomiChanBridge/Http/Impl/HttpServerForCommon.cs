﻿using System;
using System.Net;
using System.Net.Http;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.Application.Common.Utils;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Service;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Http.Impl
{
    /// <summary>
    /// メッセージを受信する共通HttpServer
    /// </summary>
    public class HttpServerForCommon : HttpServerForReadOut
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static HttpServerForCommon Instance { get; private set; }

        static HttpServerForCommon() => Instance = new();

        /// <inheritdoc/>
        protected override void RegisterListeningUrlPrefix(HttpListenerPrefixCollection prefixes)
        {
            var url = $"http://localhost:{Settings.AsString("CommonVoiceReadoutServer.ListeningPort")}/";
            prefixes.Add(url);
        }

        /// <inheritdoc/>
        protected override void OnRequestReceived(IAsyncResult result)
        {
            // Listening処理
            var context = GetContextAndResumeListening(result);
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

            Log.Logger.Debug($"Receive({GetType().Name}) :{message}");

            CommandHandlingService.Handle(message);
        }
    }

    internal static partial class HttpListenerRequestExtension
    {
        public static string? GetTextMessage(this HttpListenerRequest request, string parameterName) => request.QueryString[parameterName];
    }
}