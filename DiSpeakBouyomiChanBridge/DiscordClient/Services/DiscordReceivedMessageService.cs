using Discord.Commands;
using Discord.WebSocket;
using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.Utils;
using net.boilingwater.Utils.Extention;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.DiscordClient.Services
{
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

            if (DiscordSetting.Instance.AsStringList("List.ReadOutTargetGuild").Contains(Cast.ToString(guildChannel.Guild.Id)))
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

            if (DiscordSetting.Instance.AsStringList("List.ReadOutTargetGuildChannel").Contains(Cast.ToString(guildChannel.Id)))
            {
                return true;
            }

            return false;
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
                string mes = Regex.Replace(
                    context.Message.Content,
                    DiscordSetting.Instance.AsString("RegularExpression.URLShorter"),
                    MessageSetting.Instance.AsString("URLShorter")
                );

                format = format.Replace(
                    DiscordSetting.Instance.AsString("ReplaceKey.DiscordMessage.Message"),
                    mes
                );
            }
            return ReplaceEmojiSettings(format);
        }

        private static string ReplaceEmojiSettings(string input)
        {
            NameValueCollection emojiReplaceSettings = (NameValueCollection)ConfigurationManager.GetSection("EmojiReplaceSettings");
            if (emojiReplaceSettings == null)
            {
                return input;
            }

            emojiReplaceSettings.AllKeys.ForEach(key => input = input.Replace(key, emojiReplaceSettings[key]));
            return input;
        }

        private static string GetPlainMessage(CommandContext context)
        {
            string format = DiscordSetting.Instance.AsString("Format.DiscordMessage.PostPlainMessage");
            return ReplaceCommonReceivedInfo(format, context);
        }

        private static string GetMessageWithFile(CommandContext context)
        {
            string format = DiscordSetting.Instance.AsString("Format.DiscordMessage.PostMessageWithFile");

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
