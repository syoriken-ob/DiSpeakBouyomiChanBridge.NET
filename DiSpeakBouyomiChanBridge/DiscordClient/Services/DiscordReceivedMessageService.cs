using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

using Discord.Commands;
using Discord.WebSocket;

using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.Utils;
using net.boilingwater.Utils.Extention;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.DiscordClient.Services
{
    internal class DiscordReceivedMessageService
    {

        internal static bool IsPrivateMessage(CommandContext context)
        {
            if (context.Message.Channel is not SocketGuildChannel guildChannel) return true;

            if (guildChannel == null) return true;

            if (guildChannel.Guild == null) return true;

            return false;
        }

        internal static bool IsReadOutTargetGuild(CommandContext context)
        {
            if (context.Message.Channel is not SocketGuildChannel guildChannel) return false;

            if (guildChannel == null) return false;

            if (DiscordSetting.Instance.AsStringList("List.ReadOutTarget.Guild").Contains(Cast.ToString(guildChannel.Guild.Id)))
            {
                return true;
            }

            return false;
        }

        internal static bool IsReadOutTargetGuildChannel(CommandContext context)
        {
            if (context.Message.Channel is not SocketGuildChannel guildChannel) return false;

            if (guildChannel == null) return false;

            if (DiscordSetting.Instance.AsStringList("List.ReadOutTarget.GuildChannel.Text").Contains(Cast.ToString(guildChannel.Id)))
            {
                if (DiscordSetting.Instance.AsBoolean("Use.ReadOutTarget.GuildChannel.Text.WhiteList"))
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (DiscordSetting.Instance.AsBoolean("Use.ReadOutTarget.GuildChannel.Text.WhiteList"))
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

            if (format.Contains(DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.Guild")))
            {
                format = format.Replace(
                    DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.Guild"),
                    context.Guild.Name
                );
            }

            if (format.Contains(DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.Channel")))
            {
                format = format.Replace(
                    DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.Channel"),
                    context.Message.Channel.Name
                );
            }

            if (format.Contains(DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.UserName")))
            {
                format = format.Replace(
                    DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.UserName"),
                    context.User.Username
                );
            }

            if (format.Contains(DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.NickName")))
            {
                string name;
                if (context.Message.Author is SocketGuildUser guildUser && guildUser.Nickname != null)
                {
                    name = guildUser.Nickname;
                }
                else
                {
                    name = context.User.Username;
                }

                format = format.Replace(
                    DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.NickName"),
                    name
                );
            }

            if (format.Contains(DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.Message")))
            {
                var mes = context.Message.Content;

                mes = ReplaceMention(mes, context);

                format = format.Replace(
                    DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.Message"),
                    mes
                );
            }
            return format;
        }

        internal static void ReplaceCommonReceivedInfoAfter(ref string mes)
        {
            if (!Setting.Instance.AsBoolean("Use.InternalDiscordClient")) return;

            if (DiscordSetting.Instance.AsBoolean("Use.ReadOutReplace.URLShortener"))
            {
                mes = Regex.Replace(
                    mes,
                    DiscordSetting.Instance.AsString("RegularExpression.URLShortener"),
                    DiscordSetting.Instance.AsString("Format.Replace.URLShortener")
                );
            }

            if (DiscordSetting.Instance.AsBoolean("Use.ReadOutReplace.Spoiler"))
            {
                mes = Regex.Replace(
                    mes,
                    DiscordSetting.Instance.AsString("RegularExpression.Spoiler"),
                    DiscordSetting.Instance.AsString("Format.Replace.Spoiler")
                );
            }

            if (DiscordSetting.Instance.AsBoolean("Use.ReadOutReplace.Emoji"))
            {
                mes = ReplaceEmojiSettings(mes);
            }
        }

        private static string ReplaceEmojiSettings(string input)
        {
            var emojiReplaceSettings = (NameValueCollection)ConfigurationManager.GetSection("EmojiReplaceSettings");
            if (emojiReplaceSettings == null) return input;

            emojiReplaceSettings.AllKeys.ForEach(key => input = input.Replace(key, emojiReplaceSettings[key]));
            return input;
        }

        private static string ReplaceMention(string input, CommandContext context)
        {
            if (context.Message.MentionedEveryone)
            {
                input = Regex.Replace(
                    input,
                    DiscordSetting.Instance.AsString("ReplaceKey.System.Mention.EveryOne"),
                    DiscordSetting.Instance.AsString("Format.Replace.Mention.EveryOne")
                );
            }

            if (input.Contains(DiscordSetting.Instance.AsString("ReplaceKey.System.Mention.Here")))
            {
                input = Regex.Replace(
                    input,
                    DiscordSetting.Instance.AsString("ReplaceKey.System.Mention.Here"),
                    DiscordSetting.Instance.AsString("Format.Replace.Mention.Here")
                );
            }

            if (context.Message.MentionedChannelIds.Any())
            {
                var replaceKey = DiscordSetting.Instance.AsString("ReplaceKey.System.Mention.ChannelId");
                var channelKey = DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.ChannelId");
                var format = DiscordSetting.Instance.AsString("Format.Replace.Mention.Channel");
                var channelNameKey = DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.Channel");
                context.Message.MentionedChannelIds
                    .ForEach(channelId =>
                {
                    var channel = replaceKey.Replace(channelKey, Cast.ToString(channelId));
                    var channelName = context.Guild.GetChannelAsync(channelId).GetAwaiter().GetResult().Name;
                    input = input.Replace(channel, format.Replace(channelNameKey, channelName));
                });
            }

            if (context.Message.MentionedRoleIds.Any())
            {
                var replaceKey = DiscordSetting.Instance.AsString("ReplaceKey.System.Mention.RoleId");
                var roleKey = DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.RoleId");
                var format = DiscordSetting.Instance.AsString("Format.Replace.Mention.Role");
                var roleNameKey = DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.Role");
                context.Message.MentionedRoleIds
                    .ForEach(roleId =>
                    {
                        var role = replaceKey.Replace(roleKey, Cast.ToString(roleId));
                        var roleName = context.Guild.GetRole(roleId).Name;
                        input = input.Replace(role, format.Replace(roleNameKey, roleName));
                    });
            }

            if (context.Message.MentionedUserIds.Any())
            {
                var replaceKey = DiscordSetting.Instance.AsString("ReplaceKey.System.Mention.UserId");
                var userKey = DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.UserId");
                var nickNameKey = DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.NickName");
                var format = DiscordSetting.Instance.AsString("Format.Replace.Mention.NickName");
                context.Message.MentionedUserIds
                    .ForEach(userId =>
                    {
                        var user = replaceKey.Replace(userKey, Cast.ToString(userId));
                        var guildUser = context.Guild.GetUserAsync(userId).GetAwaiter().GetResult();
                        string name;
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
            var format = DiscordSetting.Instance.AsString("Format.DiscordMessage.PostPlainMessage");
            return ReplaceCommonReceivedInfo(format, context);
        }

        private static string GetMessageWithFile(CommandContext context)
        {
            var format = DiscordSetting.Instance.AsString("Format.DiscordMessage.PostMessageWithFile");

            if (format.Contains(DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.FileCount")))
            {
                format = format.Replace(
                    DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.FileCount"),
                    Cast.ToString(context.Message.Attachments.Count)
                );
            }

            if (format.Contains(DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.FileNames")))
            {
                format = format.Replace(
                    DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.FileNames"),
                    string.Join(",", context.Message.Attachments.Select(attachment => Cast.ToString(attachment.Filename)))
                );
            }

            return ReplaceCommonReceivedInfo(format, context);
        }

    }
}
