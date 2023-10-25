using System;
using System.Threading.Tasks;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Dto;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Handle;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Handle.Impl;
using net.boilingwater.BusinessLogic.VoiceReadOut.Service;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core.Logging;

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
        public static void Initialize() => _handler = Settings.AsBoolean("Use.InternalDiscordClient") ? new InternalDiscordClientCommandHandler() : new DiSpeakCommandHandler();

        /// <summary>
        /// 渡される文字列からコマンドを検出します。
        /// </summary>
        /// <param name="context">コマンド検出コンテキスト</param>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="Initialize()"/>を先に呼び出していない場合発生します
        /// </exception>
        public static void Handle(CommandHandlingContext context)
        {
            if (_handler == null)
            {
                throw new InvalidOperationException("コマンドハンドラの初期化が行われていません。");
            }

            // コマンドハンドリング処理は別スレッドにて非同期実行
            Task.Run(() =>
            {
                try
                {
                    _handler.ExecutePreProcess(context);
                    _handler.Handle(context);
                    _handler.ExecutePostProcess(context);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error("Error！", ex);
                    MessageReadOutService.ReadOutMessage(Settings.AsString("Message.ErrorOccurrence.CommandHandle"));
                }
            });
        }
    }
}
