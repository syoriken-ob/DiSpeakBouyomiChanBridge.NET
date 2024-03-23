using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core;
using net.boilingwater.Framework.Core.Extensions;
using net.boilingwater.Framework.Core.Utils;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.InternalDiscordClient.Services;

/// <summary>
/// Discordから取得したメッセージに対して行う処理を定義します
/// </summary>
internal class DiscordReceivedMessageService
{
    #region 判定処理

    /// <summary>
    /// メッセージがDMかどうか判定します
    /// </summary>
    /// <param name="e">メッセージ生成イベント情報</param>
    /// <returns><see langword="true"/>：DM、<see langword="false"/>：DM以外</returns>
    internal static bool IsPrivateMessage(MessageCreateEventArgs e)
    {
        return e.Message.Channel.IsPrivate;
    }

    /// <summary>
    /// メッセージが読み上げ対象のサーバーに送信されたかどうか判定します
    /// </summary>
    /// <param name="e">メッセージ生成イベント情報</param>
    /// <returns><see langword="true"/>：読み上げ対象、<see langword="false"/>：読み上げ対象以外</returns>
    internal static bool IsReadOutTargetGuild(MessageCreateEventArgs e)
    {
        return Settings.AsMultiList("List.InternalDiscordClient.ReadOutTarget.Guild").CastMulti<ulong>().Contains(e.Guild.Id);
    }

    /// <summary>
    /// メッセージが読み上げ対象のチャンネルに送信されたかどうか判定します
    /// </summary>
    /// <param name="e">メッセージ生成イベント情報</param>
    /// <returns><see langword="true"/>：読み上げ対象、<see langword="false"/>：読み上げ対象以外</returns>
    internal static bool IsReadOutTargetGuildChannel(MessageCreateEventArgs e)
    {
        if (Settings.AsMultiList("List.InternalDiscordClient.ReadOutTarget.GuildTextChannel").CastMulti<ulong>().Contains(e.Message.ChannelId))
        {
            return Settings.AsBoolean("Use.InternalDiscordClient.ReadOut.GuildTextChannel.WhiteList");
        }
        else
        {
            return !Settings.AsBoolean("Use.InternalDiscordClient.ReadOut.GuildTextChannel.WhiteList");
        }
    }

    #endregion 判定処理

    #region メッセージ生成処理

    /// <summary>
    /// 設定に合わせて整形した読み上げ用メッセージを取得します。
    /// </summary>
    /// <param name="e">メッセージ生成イベント情報</param>
    /// <returns>読み上げ用テキスト</returns>
    internal static string GetFormattedMessage(MessageCreateEventArgs e)
    {
        if (e.Message.Attachments.Any())
        {
            return GetMessageWithFile(e);
        }

        return GetPlainMessage(e);
    }

    /// <summary>
    /// 読み上げ用メッセージを生成します。
    /// </summary>
    /// <param name="e">メッセージ生成イベント情報</param>
    /// <returns>読み上げ用テキスト</returns>
    private static string GetPlainMessage(MessageCreateEventArgs e)
    {
        var format = new StringBuilder(Settings.AsString("Format.InternalDiscordClient.PostPlainMessage"));
        return ReplaceCommonReceivedInfo(format, e).ToString();
    }

    /// <summary>
    /// 読み上げ用メッセージを生成します。（添付ファイルありの場合）
    /// </summary>
    /// <param name="e">メッセージ生成イベント情報</param>
    /// <returns>読み上げ用テキスト</returns>
    private static string GetMessageWithFile(MessageCreateEventArgs e)
    {
        var format = new StringBuilder(Settings.AsString("Format.InternalDiscordClient.PostMessageWithFile"));

        format.Replace(
            Settings.AsString("InternalDiscordClient.FormatReplaceKey.FileCount"),
            CastUtil.ToString(e.Message.Attachments.Count)
        );

        format.Replace(
            Settings.AsString("InternalDiscordClient.FormatReplaceKey.FileNames"),
            string.Join(",", e.Message.Attachments.Select(attachment => attachment.FileName))
        );

        return ReplaceCommonReceivedInfo(format, e).ToString();
    }

    #endregion メッセージ生成処理

    #region テキスト置換処理

    /// <summary>
    /// テキスト中に含まれる読み上げ用共通パラメータを置換します。
    /// </summary>
    /// <param name="format">置換対象のテキスト</param>
    /// <param name="e">メッセージ生成イベント情報</param>
    /// <returns>置換後のテキスト</returns>
    /// <remarks>
    /// 置換対象
    /// <list type="bullet">
    /// <item>サーバー名</item>
    /// <item>チャンネル名</item>
    /// <item>ユーザー名</item>
    /// <item>ニックネーム</item>
    /// <item>メッセージ本文</item>
    /// </list>
    /// </remarks>
    private static StringBuilder ReplaceCommonReceivedInfo(StringBuilder format, MessageCreateEventArgs e)
    {
        format.Replace(Settings.AsString("InternalDiscordClient.FormatReplaceKey.Guild"), e.Guild.Name);

        format.Replace(Settings.AsString("InternalDiscordClient.FormatReplaceKey.Channel"), e.Message.Channel.Name);

        format.Replace(Settings.AsString("InternalDiscordClient.FormatReplaceKey.UserName"), e.Author.Username);

        string? name;
        if (e.Message.Author is DiscordMember member)
        {
            name = member.DisplayName;
        }
        else
        {
            name = e.Message.Author.Username;
        }
        format.Replace(Settings.AsString("InternalDiscordClient.FormatReplaceKey.NickName"), name);

        var messsage = ReplaceMention(e.Message.Content, e);
        format.Replace(Settings.AsString("InternalDiscordClient.FormatReplaceKey.Message"), messsage);

        return format;
    }

    /// <summary>
    /// [後処理]テキスト中に含まれる読み上げ用共通パラメータを置換します。
    /// </summary>
    /// <param name="message">置換対象のテキスト</param>
    /// <remarks>
    /// ・スポイラータグ
    /// </remarks>
    internal static void ReplaceCommonReceivedInfoAfter(ref string message)
    {
        if (Settings.AsBoolean("Use.InternalDiscordClient.ReadOutReplace.Spoiler"))
        {
            message = Regex.Replace(
                message,
                Settings.AsString("Regex.InternalDiscordClient.Spoiler"),
                Settings.AsString("Format.InternalDiscordClient.Replacement.Spoiler"),
                RegexOptions.Singleline
            );
        }
    }

    /// <summary>
    /// テキスト中に含まれる読み上げ用共通パラメータを置換します。
    /// </summary>
    /// <param name="input">置換対象のテキスト</param>
    /// <param name="e">メッセージ生成イベント情報</param>
    /// <returns>置換後のテキスト</returns>
    /// <remarks>
    /// 置換対象
    /// <list type="bullet">
    /// <item>@everyone</item>
    /// <item>@here</item>
    /// <item>@{チャンネル名}</item>
    /// <item>@{ロール名}</item>
    /// <item>@{ユーザー名}</item>
    /// </list>
    /// </remarks>
    private static string ReplaceMention(string input, MessageCreateEventArgs e)
    {
        //FIXME: StringBuilder使いたい

        if (e.Message.MentionEveryone)
        {
            input = Regex.Replace(
                input,
                Settings.AsString("InternalDiscordClient.MessageReplaceKey.Mention.EveryOne"),
                Settings.AsString("Format.InternalDiscordClient.Replacement.Mention.EveryOne")
            );
        }

        if (input.Contains(Settings.AsString("InternalDiscordClient.MessageReplaceKey.Mention.Here")))
        {
            input = Regex.Replace(
                input,
                Settings.AsString("InternalDiscordClient.MessageReplaceKey.Mention.Here"),
                Settings.AsString("Format.InternalDiscordClient.Replacement.Mention.Here")
            );
        }

        if (e.Message.MentionedChannels.Any())
        {
            var replaceKey = Settings.AsString("InternalDiscordClient.MessageReplaceKey.Mention.ChannelId");
            var channelKey = Settings.AsString("InternalDiscordClient.FormatReplaceKey.ChannelId");
            var format = Settings.AsString("Format.InternalDiscordClient.Replacement.Mention.Channel");
            var channelNameKey = Settings.AsString("InternalDiscordClient.FormatReplaceKey.Channel");

            e.Message.MentionedChannels.ForEach(channelObj =>
            {
                var channel = replaceKey.Replace(channelKey, CastUtil.ToString(channelObj.Id));
                var channelName = channelObj.Name;
                input = input.Replace(channel, format.Replace(channelNameKey, channelName));
            });
        }

        if (e.Message.MentionedRoles.Any())
        {
            var replaceKey = Settings.AsString("InternalDiscordClient.MessageReplaceKey.Mention.RoleId");
            var roleKey = Settings.AsString("InternalDiscordClient.FormatReplaceKey.RoleId");
            var format = Settings.AsString("Format.InternalDiscordClient.Replacement.Mention.Role");
            var roleNameKey = Settings.AsString("InternalDiscordClient.FormatReplaceKey.Role");

            e.Message.MentionedRoles.ForEach(roleObj =>
                {
                    var role = replaceKey.Replace(roleKey, CastUtil.ToString(roleObj.Id));
                    var roleName = roleObj.Name;
                    input = input.Replace(role, format.Replace(roleNameKey, roleName));
                }
            );
        }

        if (e.Message.MentionedUsers.Any())
        {
            var replaceKey = Settings.AsString("InternalDiscordClient.MessageReplaceKey.Mention.UserId");
            var userKey = Settings.AsString("InternalDiscordClient.FormatReplaceKey.UserId");
            var nickNameKey = Settings.AsString("InternalDiscordClient.FormatReplaceKey.NickName");
            var format = Settings.AsString("Format.InternalDiscordClient.Replacement.Mention.NickName");
            e.Message.MentionedUsers.ForEach(userObj =>
            {
                var user = replaceKey.Replace(userKey, CastUtil.ToString(userObj.Id));

                string? name;
                if (userObj is DiscordMember member)
                {
                    name = member.DisplayName;
                }
                else
                {
                    name = userObj.Username;
                }

                input = input.Replace(user, format.Replace(nickNameKey, name));
            });
        }

        return input;
    }

    #endregion テキスト置換処理
}
