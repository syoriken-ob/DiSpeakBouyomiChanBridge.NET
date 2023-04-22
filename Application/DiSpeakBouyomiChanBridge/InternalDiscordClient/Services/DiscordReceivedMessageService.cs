using System.Linq;
using System.Text.RegularExpressions;

using Discord.Commands;
using Discord.WebSocket;

using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core.Extensions;
using net.boilingwater.Framework.Core.Utils;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.InternalDiscordClient.Services
{
    /// <summary>
    /// Discordから取得したメッセージに対して行う処理を定義します
    /// </summary>
    internal class DiscordReceivedMessageService
    {
        internal static bool IsPrivateMessage(CommandContext context)
        {
            if (context.Message.Channel is not SocketGuildChannel guildChannel)
            {
                return true;
            }

            if (guildChannel == null)
            {
                return true;
            }

            if (guildChannel.Guild == null)
            {
                return true;
            }

            return false;
        }

        internal static bool IsReadOutTargetGuild(CommandContext context)
        {
            if (context.Message.Channel is not SocketGuildChannel guildChannel)
            {
                return false;
            }

            if (guildChannel == null)
            {
                return false;
            }

            if (Settings.AsStringList("List.ReadOutTarget.Guild").Contains(CastUtil.ToString(guildChannel.Guild.Id)))
            {
                return true;
            }

            return false;
        }

        internal static bool IsReadOutTargetGuildChannel(CommandContext context)
        {
            if (context.Message.Channel is not SocketGuildChannel guildChannel)
            {
                return false;
            }

            if (guildChannel == null)
            {
                return false;
            }

            if (Settings.AsStringList("List.ReadOutTarget.GuildChannel.Text").Contains(CastUtil.ToString(guildChannel.Id)))
            {
                if (Settings.AsBoolean("Use.ReadOutTarget.GuildChannel.Text.WhiteList"))
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (Settings.AsBoolean("Use.ReadOutTarget.GuildChannel.Text.WhiteList"))
                {
                    return false;
                }
                return true;
            }
        }

        internal static string GetFormattedMessage(CommandContext context)
        {
            if (context.Message.Attachments.Any())
            {
                return GetMessageWithFile(context);
            }

            return GetPlainMessage(context);
        }

        private static string ReplaceCommonReceivedInfo(string format, CommandContext context)
        {
            if (format.Contains(Settings.AsString("ReplaceKey.DiscordMessage.Guild")))
            {
                format = format.Replace(
                    Settings.AsString("ReplaceKey.DiscordMessage.Guild"),
                    context.Guild.Name
                );
            }

            if (format.Contains(Settings.AsString("ReplaceKey.DiscordMessage.Channel")))
            {
                format = format.Replace(
                    Settings.AsString("ReplaceKey.DiscordMessage.Channel"),
                    context.Message.Channel.Name
                );
            }

            if (format.Contains(Settings.AsString("ReplaceKey.DiscordMessage.UserName")))
            {
                format = format.Replace(
                    Settings.AsString("ReplaceKey.DiscordMessage.UserName"),
                    context.User.Username
                );
            }

            if (format.Contains(Settings.AsString("ReplaceKey.DiscordMessage.NickName")))
            {
                string? name;
                if (context.Message.Author is SocketGuildUser guildUser && guildUser.Nickname != null)
                {
                    name = guildUser.Nickname;
                }
                else
                {
                    name = context.User.Username;
                }

                format = format.Replace(
                    Settings.AsString("ReplaceKey.DiscordMessage.NickName"),
                    name
                );
            }

            if (format.Contains(Settings.AsString("ReplaceKey.DiscordMessage.Message")))
            {
                var mes = context.Message.Content;

                mes = ReplaceMention(mes, context);

                format = format.Replace(
                    Settings.AsString("ReplaceKey.DiscordMessage.Message"),
                    mes
                );
            }
            return format;
        }

        internal static void ReplaceCommonReceivedInfoAfter(ref string message)
        {
            if (Settings.AsBoolean("Use.ReadOutReplace.Spoiler"))
            {
                message = Regex.Replace(
                    message,
                    Settings.AsString("RegularExpression.Spoiler"),
                    Settings.AsString("Format.Replace.Spoiler"),
                    RegexOptions.Singleline
                );
            }
        }

        private static string ReplaceMention(string input, CommandContext context)
        {
            if (context.Message.MentionedEveryone)
            {
                input = Regex.Replace(
                    input,
                    Settings.AsString("ReplaceKey.System.Mention.EveryOne"),
                    Settings.AsString("Format.Replace.Mention.EveryOne")
                );
            }

            if (input.Contains(Settings.AsString("ReplaceKey.System.Mention.Here")))
            {
                input = Regex.Replace(
                    input,
                    Settings.AsString("ReplaceKey.System.Mention.Here"),
                    Settings.AsString("Format.Replace.Mention.Here")
                );
            }

            if (context.Message.MentionedChannelIds.Any())
            {
                var replaceKey = Settings.AsString("ReplaceKey.System.Mention.ChannelId");
                var channelKey = Settings.AsString("ReplaceKey.DiscordMessage.ChannelId");
                var format = Settings.AsString("Format.Replace.Mention.Channel");
                var channelNameKey = Settings.AsString("ReplaceKey.DiscordMessage.Channel");

                context.Message.MentionedChannelIds.ForEach(channelId =>
                    {
                        var channel = replaceKey.Replace(channelKey, CastUtil.ToString(channelId));
                        var channelName = context.Guild.GetChannelAsync(channelId).GetAwaiter().GetResult().Name;
                        input = input.Replace(channel, format.Replace(channelNameKey, channelName));
                    }
                );
            }

            if (context.Message.MentionedRoleIds.Any())
            {
                var replaceKey = Settings.AsString("ReplaceKey.System.Mention.RoleId");
                var roleKey = Settings.AsString("ReplaceKey.DiscordMessage.RoleId");
                var format = Settings.AsString("Format.Replace.Mention.Role");
                var roleNameKey = Settings.AsString("ReplaceKey.DiscordMessage.Role");

                context.Message.MentionedRoleIds.ForEach(roleId =>
                    {
                        var role = replaceKey.Replace(roleKey, CastUtil.ToString(roleId));
                        var roleName = context.Guild.GetRole(roleId).Name;
                        input = input.Replace(role, format.Replace(roleNameKey, roleName));
                    }
                );
            }

            if (context.Message.MentionedUserIds.Any())
            {
                var replaceKey = Settings.AsString("ReplaceKey.System.Mention.UserId");
                var userKey = Settings.AsString("ReplaceKey.DiscordMessage.UserId");
                var nickNameKey = Settings.AsString("ReplaceKey.DiscordMessage.NickName");
                var format = Settings.AsString("Format.Replace.Mention.NickName");
                context.Message.MentionedUserIds.ForEach(userId =>
                    {
                        var user = replaceKey.Replace(userKey, CastUtil.ToString(userId));
                        Discord.IGuildUser guildUser = context.Guild.GetUserAsync(userId, Discord.CacheMode.AllowDownload, Discord.RequestOptions.Default).GetAwaiter().GetResult();

                        string? name = null;
                        if (guildUser.Nickname != null)
                        {
                            name = guildUser.Nickname;
                        }
                        else
                        {
                            name = guildUser.Username;
                        }

                        input = input.Replace(user, format.Replace(nickNameKey, name));
                    }
                );
            }

            return input;
        }

        private static string GetPlainMessage(CommandContext context)
        {
            var format = Settings.AsString("Format.DiscordMessage.PostPlainMessage");
            return ReplaceCommonReceivedInfo(format, context);
        }

        private static string GetMessageWithFile(CommandContext context)
        {
            var format = Settings.AsString("Format.DiscordMessage.PostMessageWithFile");

            if (format.Contains(Settings.AsString("ReplaceKey.DiscordMessage.FileCount")))
            {
                format = format.Replace(
                    Settings.AsString("ReplaceKey.DiscordMessage.FileCount"),
                    CastUtil.ToString(context.Message.Attachments.Count)
                );
            }

            if (format.Contains(Settings.AsString("ReplaceKey.DiscordMessage.FileNames")))
            {
                format = format.Replace(
                    Settings.AsString("ReplaceKey.DiscordMessage.FileNames"),
                    string.Join(",", context.Message.Attachments.Select(attachment => CastUtil.ToString(attachment.Filename)))
                );
            }

            return ReplaceCommonReceivedInfo(format, context);
        }
    }
}
