using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Dto;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl.Factory;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.PipeLine;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.InternalDiscordClient.Services;
using net.boilingwater.BusinessLogic.Common.User.Service;
using net.boilingwater.BusinessLogic.MessageReplacer.Service;
using net.boilingwater.BusinessLogic.VoiceReadOut.Service;
using net.boilingwater.Framework.Core.Extensions;

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
        /// <param name="context">コマンド検出コンテキスト</param>
        internal virtual void ExecutePreProcess(CommandHandlingContext context)
        {
            //メッセージ置換処理(教育・忘却含む)
            MessageReplaceService.ExecuteReplace(ref context.Message);

            //ユーザーの既定話者登録解除処理
            UserService.ExecuteUserSpeakerProcess(ref context.Message, context.User);
        }

        /// <summary>
        /// 各種コマンド検出処理を実行します。
        /// </summary>
        /// <param name="context">コマンド検出コンテキスト</param>
        internal virtual void Handle(CommandHandlingContext context)
        {
            if (SystemCommandFactory.Factory == null || CommandFactory.Factory == null)
            {
                throw new InvalidOperationException("コマンドファクトリが初期化されていません。");
            }

            //システムコマンド検出
            IEnumerable<ExecutableCommand> systemCommands = SystemCommandFactory.Factory.CreateExecutableCommands(ref context.Message);
            //コマンド検出
            IEnumerable<ExecutableCommand> commands = CommandFactory.Factory.CreateExecutableCommands(ref context.Message);

            //システムコマンド実行
            systemCommands.ForEach((cmd) => new Task(() => cmd.Execute()).RunSynchronously());

            //コマンドを実行キューに追加
            commands.ForEach(cmd => CommandExecuteManager.Instance.AddCommand((Command)cmd));
        }

        /// <summary>
        /// コマンド検出後処理を実行します。
        /// </summary>
        /// <param name="context">コマンド検出コンテキスト</param>
        internal virtual void ExecutePostProcess(CommandHandlingContext context)
        {
            //スポイラーなどの処理
            DiscordReceivedMessageService.ReplaceCommonReceivedInfoAfter(ref context.Message);

            MessageReplaceService.ReplaceMessage(ref context.Message);
            MessageReplaceService.ReplaceMessageUrlShortener(ref context.Message);

            //棒読みちゃんに送信
            MessageReadOutService.ReadOutMessage(context.Message, context.User);

            MessageReplaceService.InitializeAfterReadOutIfNeeded();
        }
    }
}
