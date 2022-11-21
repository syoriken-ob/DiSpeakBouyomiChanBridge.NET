﻿using System;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Handle;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Handle.Impl;
using net.boilingwater.BusinessLogic.VoiceReadout.HttpClients;
using net.boilingwater.Framework.Common.Logging;
using net.boilingwater.Framework.Common.Setting;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Service
{
    /// <summary>
    /// コマンド検出サービス
    /// </summary>
    public class CommandHandlingService
    {
        private static AbstractCommandHandler? _handler;

        /// <summary>
        /// コマンド検出サービスの初期化を行います
        /// </summary>
        public static void Initialize()
        {
            _handler = Settings.AsBoolean("Use.InternalDiscordClient") ? new InternalDiscordClientCommandHandler() : new DiSpeakCommandHandler();
        }

        /// <summary>
        /// 渡される文字列からコマンドを検出します。
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="Initialize()"/>を先に呼び出していない場合発生します
        /// </exception>
        public static void Handle(string message)
        {
            if (_handler == null)
            {
                throw new InvalidOperationException("コマンドハンドラの初期化が行われていません。");
            }
            try
            {
                _handler.ExecutePreProcess(ref message);
                _handler.Handle(ref message);
                _handler.ExecutePostProcess(ref message);
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Error！", ex);
                HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.ErrorOccurrence.CommandHandle"));
            }
        }
    }
}