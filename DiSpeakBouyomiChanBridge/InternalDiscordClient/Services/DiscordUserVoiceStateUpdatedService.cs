using Discord.WebSocket;

using net.boilingwater.Application.Common.Setting;
using net.boilingwater.Application.Common.Utils;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.InternalDiscordClient.Services
{
    internal class DiscordUserVoiceStateUpdatedService
    {
        internal enum VoiceState
        {
            JOIN, LEAVE, MOVE, START_STREAMING, END_STREAMING, UNKNOWN
        }

        internal static bool IsReadOutTargetGuild(SocketVoiceState sourceVoiceState, SocketVoiceState targetVoiceState)
        {
            if (sourceVoiceState.VoiceChannel == null && targetVoiceState.VoiceChannel == null)
            {
                return false;
            }

            if (sourceVoiceState.VoiceChannel != null &&
                !Settings.AsStringList("List.ReadOutTarget.Guild").Contains(CastUtil.ToString(sourceVoiceState.VoiceChannel.Guild.Id)))
            {
                return false;
            }
            if (targetVoiceState.VoiceChannel != null &&
                !Settings.AsStringList("List.ReadOutTarget.Guild").Contains(CastUtil.ToString(targetVoiceState.VoiceChannel.Guild.Id)))
            {
                return false;
            }

            return true;
        }

        internal static bool IsReadOutTargetGuildChannel(SocketVoiceState sourceVoiceState, SocketVoiceState targetVoiceState)
        {
            if (sourceVoiceState.VoiceChannel == null && targetVoiceState.VoiceChannel == null)
            {
                return false;
            }

            if (sourceVoiceState.VoiceChannel != null &&
                !Settings.AsStringList("List.ReadOutTarget.GuildChannel.Voice").Contains(CastUtil.ToString(sourceVoiceState.VoiceChannel.Id)))
            {
                if (!Settings.AsBoolean("Use.ReadOutTarget.GuildChannel.Voice.WhiteList"))
                {
                    return false;
                }
            }
            if (targetVoiceState.VoiceChannel != null &&
                !Settings.AsStringList("List.ReadOutTarget.GuildChannel.Voice").Contains(CastUtil.ToString(targetVoiceState.VoiceChannel.Id)))
            {
                if (!Settings.AsBoolean("Use.ReadOutTarget.GuildChannel.Voice.WhiteList"))
                {
                    return false;
                }
            }

            return true;
        }

        internal static VoiceState DetectVoiceStateUpdate(SocketVoiceState sourceVoiceState, SocketVoiceState targetVoiceState)
        {
            if (sourceVoiceState.VoiceChannel == null)
            {
                return VoiceState.JOIN;
            }

            if (targetVoiceState.VoiceChannel == null)
            {
                return VoiceState.LEAVE;
            }

            if (sourceVoiceState.VoiceChannel.Id != targetVoiceState.VoiceChannel.Id)
            {
                return VoiceState.MOVE;
            }

            if (sourceVoiceState.IsStreaming == false && targetVoiceState.IsStreaming == true)
            {
                return VoiceState.START_STREAMING;
            }

            if (sourceVoiceState.IsStreaming == true && targetVoiceState.IsStreaming == false)
            {
                return VoiceState.END_STREAMING;
            }

            return VoiceState.UNKNOWN;
        }

        private static string ReplaceCommonVoiceStateInfo(string format, SocketGuildUser guildUser, SocketVoiceState sourceVoiceState, SocketVoiceState targetVoiceState)
        {
            if (format.Contains(Settings.AsString("ReplaceKey.DiscordMessage.UserName")))
            {
                format = format.Replace(
                    Settings.AsString("ReplaceKey.DiscordMessage.UserName"),
                    guildUser.Username
                );
            }

            if (format.Contains(Settings.AsString("ReplaceKey.DiscordMessage.NickName")))
            {
                string name;
                if (guildUser.Nickname != null)
                {
                    name = guildUser.Nickname;
                }
                else
                {
                    name = guildUser.Username;
                }

                format = format.Replace(
                    Settings.AsString("ReplaceKey.DiscordMessage.NickName"),
                    name
                );
            }

            if (format.Contains(Settings.AsString("ReplaceKey.DiscordMessage.SourceChannel")))
            {
                format = format.Replace(
                    Settings.AsString("ReplaceKey.DiscordMessage.SourceChannel"),
                    sourceVoiceState.VoiceChannel.Name
                );
            }

            if (format.Contains(Settings.AsString("ReplaceKey.DiscordMessage.TargetChannel")))
            {
                format = format.Replace(
                    Settings.AsString("ReplaceKey.DiscordMessage.TargetChannel"),
                    targetVoiceState.VoiceChannel.Name
                );
            }
            return format;
        }

        internal static string GetJoinVoiceChannelMessage(SocketGuildUser guildUser, SocketVoiceState sourceVoiceState, SocketVoiceState targetVoiceState)
        {
            var format = Settings.AsString("Format.DiscordMessage.JoinVoiceChannleMessage");
            return ReplaceCommonVoiceStateInfo(format, guildUser, sourceVoiceState, targetVoiceState);
        }

        internal static string GetLeaveVoiceChannelMessage(SocketGuildUser guildUser, SocketVoiceState sourceVoiceState, SocketVoiceState targetVoiceState)
        {
            var format = Settings.AsString("Format.DiscordMessage.LeaveVoiceChannleMessage");
            return ReplaceCommonVoiceStateInfo(format, guildUser, sourceVoiceState, targetVoiceState);
        }

        internal static string GetMoveVoiceChannelMessage(SocketGuildUser guildUser, SocketVoiceState sourceVoiceState, SocketVoiceState targetVoiceState)
        {
            var format = Settings.AsString("Format.DiscordMessage.MoveVoiceChannleMessage");
            return ReplaceCommonVoiceStateInfo(format, guildUser, sourceVoiceState, targetVoiceState);
        }

        internal static string GetStartStreamingVoiceChannelMessage(SocketGuildUser guildUser, SocketVoiceState sourceVoiceState, SocketVoiceState targetVoiceState)
        {
            var format = Settings.AsString("Format.DiscordMessage.StartStreamingVoiceChannleMessage");
            return ReplaceCommonVoiceStateInfo(format, guildUser, sourceVoiceState, targetVoiceState);
        }

        internal static string GetEndStreamingVoiceChannelMessage(SocketGuildUser guildUser, SocketVoiceState sourceVoiceState, SocketVoiceState targetVoiceState)
        {
            var format = Settings.AsString("Format.DiscordMessage.EndStreamingVoiceChannleMessage");
            return ReplaceCommonVoiceStateInfo(format, guildUser, sourceVoiceState, targetVoiceState);
        }
    }
}
