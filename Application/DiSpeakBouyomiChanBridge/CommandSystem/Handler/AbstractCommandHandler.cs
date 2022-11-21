﻿using System;
using System.Threading.Tasks;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl.Factory;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.PipeLine;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.InternalDiscordClient.Services;
using net.boilingwater.BusinessLogic.MessageReplacer.Service;
using net.boilingwater.BusinessLogic.VoiceReadout.HttpClients;
using net.boilingwater.Framework.Common.Extensions;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Handle
{
    /// <summary>
    /// コマンド検出処理を定義する基底クラス
    /// </summary>
    internal abstract class AbstractCommandHandler
    {
        /// <summary>
        /// コマンド検出前処理を実行します。
        /// </summary>
        /// <param name="message">検出するメッセージ</param>
        internal void ExecutePreProcess(ref string message)
        {
            //メッセージ置換処理(教育・忘却含む)
            MessageReplaceService.ExecuteReplace(ref message);
        }

        /// <summary>
        /// 各種コマンド検出処理を実行します。
        /// </summary>
        /// <param name="message">検出するメッセージ</param>
        internal void Handle(ref string message)
        {
            if (SystemCommandFactory.Factory == null || CommandFactory.Factory == null)
            {
                throw new InvalidOperationException("コマンドファクトリが初期化されていません。");
            }

            //システムコマンド検出
            var systemCommands = SystemCommandFactory.Factory.CreateExecutableCommands(ref message);
            //コマンド検出
            var commands = CommandFactory.Factory.CreateExecutableCommands(ref message);

            //システムコマンド実行
            systemCommands.ForEach((cmd) => new Task(() => cmd.Execute()).RunSynchronously());

            //コマンドを実行キューに追加
            commands.ForEach(cmd => CommandExecuteManager.Instance.AddCommand((Command)cmd));
        }

        /// <summary>
        /// コマンド検出後処理を実行します。
        /// </summary>
        /// <param name="message">検出するメッセージ</param>
        internal void ExecutePostProcess(ref string message)
        {
            //スポイラーなどの処理
            DiscordReceivedMessageService.ReplaceCommonReceivedInfoAfter(ref message);

            MessageReplaceService.ReplaceMessage(ref message);
            MessageReplaceService.ReplaceMessageUrlShortener(ref message);

            //棒読みちゃんに送信
            HttpClientForReadOut.Instance?.ReadOut(message);

            MessageReplaceService.InitializeAfterReadOutIfNeeded();
        }
    }
}