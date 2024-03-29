﻿using System.Net;
using System.Net.Http;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Dto;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Service;
using net.boilingwater.Framework.Common.Http;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core.Logging;
using net.boilingwater.Framework.Core.Utils;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.Http.Impl;

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
    protected override void OnRequestReceived(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        var message = "";
        using (HttpListenerResponse response = context.Response)
        {
            if (request.HttpMethod != HttpMethod.Get.Method)
            {
                return;
            }

            message = CastUtil.ToString(request.GetDiscordMessage());
            response.StatusCode = (int)HttpStatusCode.OK;
        }

        Log.Logger.Debug($"Receive({GetType().Name}) :{message}");

        CommandHandlingService.Handle(new CommandHandlingContext(message));
    }
}

internal static partial class HttpListenerRequestExtension
{
    public static string? GetDiscordMessage(this HttpListenerRequest request) => request.QueryString["text"];
}
