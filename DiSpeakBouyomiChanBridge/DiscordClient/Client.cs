using System;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.DiSpeakBouyomiChanBridge.DiscordClient.Services;
using net.boilingwater.DiSpeakBouyomiChanBridge.External;
using net.boilingwater.DiSpeakBouyomiChanBridge.Http;
using net.boilingwater.DiSpeakBouyomiChanBridge.Log;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.DiscordClient
{
    public class Client
    {
        private static DiscordSocketClient client;
        public static CommandService commands;
        public static IServiceProvider services;

        public static async Task InitializeAsync()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug
            });
            client.Log += Log;
            commands = new CommandService();
            services = new ServiceCollection().BuildServiceProvider();

            if (DiscordSetting.Instance.AsBoolean("Use.ReadOut.GuildChannel.Text"))
            {
                client.MessageReceived += MessageReceived;
            }
            if (DiscordSetting.Instance.AsBoolean("Use.ReadOut.GuildChannel.Voice"))
            {
                client.UserVoiceStateUpdated += UserVoiceStatusUpdated;
            }

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            LoggerPool.Logger.Info("Try login to Discord...");
            await client.LoginAsync(TokenType.Bot, DiscordSetting.Instance.AsString("DiscordToken"));
            LoggerPool.Logger.Info("Success login !");

            await Task.CompletedTask;
        }

        public static async Task StartAsync()
        {
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private static Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    LoggerPool.Logger.Error(message.Message);
                    break;
                case LogSeverity.Warning:
                    LoggerPool.Logger.Warn(message.Message);
                    break;
                case LogSeverity.Info:
                    LoggerPool.Logger.Info(message.Message);
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                default:
                    LoggerPool.Logger.Debug(message.Message);
                    break;
            }
            return Task.CompletedTask;
        }

        private static async Task MessageReceived(SocketMessage rawMessage)
        {
            if (!DiscordSetting.Instance.AsBoolean("Use.ReadOut.GuildChannel.Text")) return;

            if (rawMessage is not SocketUserMessage message) return;

            var context = new CommandContext(client, message);

            if (DiscordReceivedMessageService.IsPrivateMessage(context)) return;

            if (!DiscordReceivedMessageService.IsReadOutTargetGuild(context)) return;

            if (!DiscordReceivedMessageService.IsReadOutTargetGuildChannel(context)) return;

            var formattedMessage = DiscordReceivedMessageService.GetFormattedMessage(context);

            CommandHandlingService.Handle(formattedMessage);

            await Task.CompletedTask;
        }



        private static async Task UserVoiceStatusUpdated(SocketUser user, SocketVoiceState sourceVoiceState, SocketVoiceState targetVoiceState)
        {
            if (!DiscordSetting.Instance.AsBoolean("Use.ReadOut.GuildChannel.Voice")) return;

            if (user is not SocketGuildUser guildUser) return;

            if (!DiscordUserVoiceStateUpdatedService.IsReadOutTargetGuild(sourceVoiceState, targetVoiceState)) return;

            if (!DiscordUserVoiceStateUpdatedService.IsReadOutTargetGuildChannel(sourceVoiceState, targetVoiceState)) return;

            var state = DiscordUserVoiceStateUpdatedService.DetectVoiceStateUpdate(sourceVoiceState, targetVoiceState);

            switch (state)
            {
                case DiscordUserVoiceStateUpdatedService.VoiceState.JOIN:
                    HttpClientForBouyomiChan.Instance.SendToBouyomiChan(
                        DiscordUserVoiceStateUpdatedService.GetJoinVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState)
                    );
                    break;
                case DiscordUserVoiceStateUpdatedService.VoiceState.LEAVE:
                    HttpClientForBouyomiChan.Instance.SendToBouyomiChan(
                        DiscordUserVoiceStateUpdatedService.GetLeaveVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState)
                    );
                    break;
                case DiscordUserVoiceStateUpdatedService.VoiceState.MOVE:
                    HttpClientForBouyomiChan.Instance.SendToBouyomiChan(
                        DiscordUserVoiceStateUpdatedService.GetMoveVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState)
                    );
                    break;
                case DiscordUserVoiceStateUpdatedService.VoiceState.START_STREAMING:
                    HttpClientForBouyomiChan.Instance.SendToBouyomiChan(
                        DiscordUserVoiceStateUpdatedService.GetStartStreamingVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState)
                    );
                    break;
                case DiscordUserVoiceStateUpdatedService.VoiceState.END_STREAMING:
                    HttpClientForBouyomiChan.Instance.SendToBouyomiChan(
                        DiscordUserVoiceStateUpdatedService.GetEndStreamingVoiceChannelMessage(guildUser, sourceVoiceState, targetVoiceState)
                    );
                    break;
                default:
                    return;
            }

            await Task.CompletedTask;
        }
    }
}