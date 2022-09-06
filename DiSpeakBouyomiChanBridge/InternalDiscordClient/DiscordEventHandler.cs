using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem;
using net.boilingwater.DiSpeakBouyomiChanBridge.Http;
using net.boilingwater.DiSpeakBouyomiChanBridge.InternalDiscordClient.Services;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.InternalDiscordClient
{
    internal class DiscordEventHandler
    {
        private readonly IDiscordClient? client;

        internal DiscordEventHandler(IDiscordClient? client) => this.client = client;

        internal async Task MessageReceived(SocketMessage rawMessage)
        {
            if (rawMessage is not SocketUserMessage message)
            {
                return;
            }

            var context = new CommandContext(client, message);

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

            CommandHandlingService.Handle(formattedMessage);

            await Task.CompletedTask;
        }

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

            var state = DiscordUserVoiceStateUpdatedService.DetectVoiceStateUpdate(sourceVoiceState, targetVoiceState);

            switch (state)
            {
                case DiscordUserVoiceStateUpdatedService.VoiceState.JOIN:
                    HttpClientForReadOut.Instance?.ReadOut(
                        DiscordUserVoiceStateUpdatedService.GetJoinVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState)
                    );
                    break;
                case DiscordUserVoiceStateUpdatedService.VoiceState.LEAVE:
                    HttpClientForReadOut.Instance?.ReadOut(
                        DiscordUserVoiceStateUpdatedService.GetLeaveVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState)
                    );
                    break;
                case DiscordUserVoiceStateUpdatedService.VoiceState.MOVE:
                    HttpClientForReadOut.Instance?.ReadOut(
                        DiscordUserVoiceStateUpdatedService.GetMoveVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState)
                    );
                    break;
                case DiscordUserVoiceStateUpdatedService.VoiceState.START_STREAMING:
                    HttpClientForReadOut.Instance?.ReadOut(
                        DiscordUserVoiceStateUpdatedService.GetStartStreamingVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState)
                    );
                    break;
                case DiscordUserVoiceStateUpdatedService.VoiceState.END_STREAMING:
                    HttpClientForReadOut.Instance?.ReadOut(
                        DiscordUserVoiceStateUpdatedService.GetEndStreamingVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState)
                    );
                    break;
                default:
                    return;
            }
            await Task.CompletedTask;
        }

        internal Task Logging(LogMessage message)
        {
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
                default:
                    Log.Logger.Debug(message.Message);
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
