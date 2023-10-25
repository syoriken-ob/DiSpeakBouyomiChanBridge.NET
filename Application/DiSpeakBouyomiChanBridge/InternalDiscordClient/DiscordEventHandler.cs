using System;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Dto;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Service;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.InternalDiscordClient.Services;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core.Extensions;
using net.boilingwater.Framework.Core.Logging;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.InternalDiscordClient
{
    /// <summary>
    /// Discord.NETのClientにアタッチする各種イベントのハンドラ処理を定義したクラス
    /// </summary>
    internal class DiscordEventHandler
    {
        internal IDiscordClient? Client { get; private set; }
        internal LogSeverity RedirectLogSeverity { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="client"></param>
        /// <exception cref="ArgumentException"></exception>
        internal DiscordEventHandler(IDiscordClient? client)
        {
            Client = client;

            //リダイレクトするログレベルの設定
            if (!Settings.AsString("RedirectLogDiscord.Net.LogSeverity").HasValue())
            {
                RedirectLogSeverity = LogSeverity.Debug;
                return;
            }
            var levels = Enum.GetNames<LogSeverity>()
                             .Where(s => s == Settings.AsString("RedirectLogDiscord.Net.LogSeverity"))
                             .ToList();
            if (levels.Any())
            {
                RedirectLogSeverity = Enum.Parse<LogSeverity>(levels.First());
            }
        }

        /// <summary>
        /// Discordからメッセージを受信した時の処理を定義します。
        /// </summary>
        /// <param name="rawMessage"></param>
        /// <returns></returns>
        internal async Task MessageReceived(SocketMessage rawMessage)
        {
            if (rawMessage is not SocketUserMessage message)
            {
                return;
            }

            var context = new CommandContext(Client, message);

            if (DiscordReceivedMessageService.IsPrivateMessage(context))
            {
                return;
            }

            if (!DiscordReceivedMessageService.IsReadOutTargetGuild(context))
            {
                return;
            }

            if (!DiscordReceivedMessageService.IsReadOutTargetGuildChannel(context))
            {
                return;
            }

            var formattedMessage = DiscordReceivedMessageService.GetFormattedMessage(context);

            CommandHandlingService.Handle(new CommandHandlingContext(formattedMessage, context.User.Id.ToString()));

            await Task.CompletedTask;
        }

        /// <summary>
        /// Discordからユーザーのステータス変更イベントを受信した時の処理を定義します。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="sourceVoiceState"></param>
        /// <param name="targetVoiceState"></param>
        /// <returns></returns>
        internal async Task UserVoiceStatusUpdated(SocketUser user, SocketVoiceState sourceVoiceState, SocketVoiceState targetVoiceState)
        {
            if (!Settings.AsBoolean("Use.ReadOut.GuildChannel.Voice"))
            {
                return;
            }

            if (user is not SocketGuildUser guildUser)
            {
                return;
            }

            if (!DiscordUserVoiceStateUpdatedService.IsReadOutTargetGuild(sourceVoiceState, targetVoiceState))
            {
                return;
            }

            if (!DiscordUserVoiceStateUpdatedService.IsReadOutTargetGuildChannel(sourceVoiceState, targetVoiceState))
            {
                return;
            }

            DiscordUserVoiceStateUpdatedService.VoiceState state = DiscordUserVoiceStateUpdatedService.DetectVoiceStateUpdate(sourceVoiceState, targetVoiceState);

            switch (state)
            {
                case DiscordUserVoiceStateUpdatedService.VoiceState.JOIN:
                    CommandHandlingService.Handle(new CommandHandlingContext(
                        DiscordUserVoiceStateUpdatedService.GetJoinVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState),
                        guildUser.Id.ToString()
                    ));
                    break;

                case DiscordUserVoiceStateUpdatedService.VoiceState.LEAVE:
                    CommandHandlingService.Handle(new CommandHandlingContext(
                        DiscordUserVoiceStateUpdatedService.GetLeaveVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState),
                        guildUser.Id.ToString()
                    ));
                    break;

                case DiscordUserVoiceStateUpdatedService.VoiceState.MOVE:
                    CommandHandlingService.Handle(new CommandHandlingContext(
                        DiscordUserVoiceStateUpdatedService.GetMoveVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState),
                        guildUser.Id.ToString()
                    ));
                    break;

                case DiscordUserVoiceStateUpdatedService.VoiceState.START_STREAMING:
                    CommandHandlingService.Handle(new CommandHandlingContext(
                        DiscordUserVoiceStateUpdatedService.GetStartStreamingVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState),
                        guildUser.Id.ToString()
                    ));
                    break;

                case DiscordUserVoiceStateUpdatedService.VoiceState.END_STREAMING:
                    CommandHandlingService.Handle(new CommandHandlingContext(
                        DiscordUserVoiceStateUpdatedService.GetEndStreamingVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState),
                        guildUser.Id.ToString()
                    ));
                    break;

                default:
                    break;
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Discord.NETでログ出力された場合の処理を定義します。
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal Task Logging(LogMessage message)
        {
            if (message.Severity > RedirectLogSeverity)
            {
                return Task.CompletedTask;
            }

            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Log.Logger.Error(message.Message);
                    break;

                case LogSeverity.Warning:
                    Log.Logger.Warn(message.Message);
                    break;

                case LogSeverity.Info:
                    Log.Logger.Info(message.Message);
                    break;

                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Log.Logger.Debug(message.Message);
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
