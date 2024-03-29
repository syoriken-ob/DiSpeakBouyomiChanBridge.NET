﻿using System;
using System.Net;
using System.Net.Http;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Dto;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Service;
using net.boilingwater.Framework.Common.Http;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core;
using net.boilingwater.Framework.Core.Extensions;
using net.boilingwater.Framework.Core.Logging;
using net.boilingwater.Framework.Core.Utils;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.Http.Impl;

/// <summary>
/// メッセージを受信する共通HttpServer
/// </summary>
public class HttpServerForCommon : AbstractHttpServer
{
    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static HttpServerForCommon Instance { get; private set; }

    static HttpServerForCommon() => Instance = new();

    /// <inheritdoc/>
    protected override void RegisterListeningUrlPrefix(HttpListenerPrefixCollection prefixes)
    {
        Settings.AsMultiList("List.CommonVoiceReadoutServer.ListeningHost")
                .CastMulti<string>()
                .ForEach(host => prefixes.Add(new UriBuilder()
                {
                    Scheme = "http",
                    Host = host,
                    Port = Settings.AsInteger("CommonVoiceReadoutServer.ListeningPort"),
                    Path = "/"
                }.Uri.ToString()));
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

            message = CastUtil.ToString(request.GetTextMessage("text"));
            response.StatusCode = (int)HttpStatusCode.OK;
        }

        Log.Logger.Debug($"Receive({GetType().Name}) :{message}");

        CommandHandlingService.Handle(new CommandHandlingContext(message));
    }
}

internal static partial class HttpListenerRequestExtension
{
    public static string? GetTextMessage(this HttpListenerRequest request, string parameterName) => request.QueryString[parameterName];
}
