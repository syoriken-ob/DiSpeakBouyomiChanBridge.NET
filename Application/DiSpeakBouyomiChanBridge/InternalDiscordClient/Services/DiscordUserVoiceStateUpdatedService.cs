using System.Collections.Generic;
using System.Text;

using DSharpPlus.Entities;

using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.InternalDiscordClient.Services;

/// <summary>
/// Discordから取得したボイスステータス更新情報に対して行う処理を定義します
/// </summary>
internal class DiscordUserVoiceStateUpdatedService
{
    #region 列挙型

    /// <summary>
    /// ボイスチャンネルでユーザーが行った操作を示す列挙子
    /// </summary>
    internal enum VoiceState
    {
        /// <summary>ボイスチャンネルに参加</summary>
        JOIN,

        /// <summary>ボイスチャンネルから退出</summary>
        LEAVE,

        /// <summary>ボイスチャンネルを移動</summary>
        MOVE,

        /// <summary>画面共有を開始</summary>
        START_STREAMING,

        /// <summary>画面共有を終了</summary>
        END_STREAMING,

        /// <summary>未定義</summary>
        UNKNOWN
    }

    #endregion 列挙型

    #region 判定処理

    /// <summary>
    /// イベントが読み上げ対象のサーバーで発生したかどうか判定します
    /// </summary>
    /// <param name="beforeState">イベント発生前のボイス状態</param>
    /// <param name="afterState">イベント発生後のボイス状態</param>
    /// <returns><see langword="true"/>：読み上げ対象、<see langword="false"/>：読み上げ対象以外</returns>
    internal static bool IsReadOutTargetGuild(DiscordVoiceState beforeState, DiscordVoiceState afterState)
    {
        if (beforeState?.Channel == null && afterState?.Channel == null)
        {
            return false;
        }

        IList<ulong> targetGuilds = Settings.AsMultiList("List.InternalDiscordClient.ReadOutTarget.Guild").CastMulti<ulong>();
        if (beforeState?.Channel != null && !targetGuilds.Contains(beforeState.Channel.Guild.Id))
        {
            return false;
        }
        if (afterState?.Channel != null && !targetGuilds.Contains(afterState.Channel.Guild.Id))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// イベントが読み上げ対象のサーバーのボイスチャンネルで発生したかどうか判定します
    /// </summary>
    /// <param name="beforeState">イベント発生前のボイス状態</param>
    /// <param name="afterState">イベント発生後のボイス状態</param>
    /// <returns><see langword="true"/>：読み上げ対象、<see langword="false"/>：読み上げ対象以外</returns>
    internal static bool IsReadOutTargetGuildChannel(DiscordVoiceState beforeState, DiscordVoiceState afterState)
    {
        if (beforeState?.Channel == null && afterState?.Channel == null)
        {
            return false;
        }

        IList<ulong> targetGuildChannels = Settings.AsMultiList("List.InternalDiscordClient.ReadOutTarget.GuildVoiceChannel").CastMulti<ulong>();
        if (beforeState?.Channel != null && !targetGuildChannels.Contains(beforeState.Channel.Id))
        {
            if (!Settings.AsBoolean("Use.InternalDiscordClient.ReadOut.GuildVoiceChannel.WhiteList"))
            {
                return false;
            }
        }
        if (afterState?.Channel != null && !targetGuildChannels.Contains(afterState.Channel.Id))
        {
            if (!Settings.AsBoolean("Use.InternalDiscordClient.ReadOut.GuildVoiceChannel.WhiteList"))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// ボイス状態からユーザーが行った操作を特定します。
    /// </summary>
    /// <param name="beforeState">イベント発生前のボイス状態</param>
    /// <param name="afterState">イベント発生後のボイス状態</param>
    /// <returns><see cref="VoiceState"/>：特定したユーザー操作列挙子</returns>
    internal static VoiceState DetectVoiceStateUpdate(DiscordVoiceState beforeState, DiscordVoiceState afterState)
    {
        if (beforeState?.Channel == null)
        {
            return VoiceState.JOIN;
        }

        if (afterState?.Channel == null)
        {
            return VoiceState.LEAVE;
        }

        if (beforeState?.Channel?.Id != afterState?.Channel?.Id)
        {
            return VoiceState.MOVE;
        }

        if (beforeState?.IsSelfStream == false && afterState?.IsSelfStream == true)
        {
            return VoiceState.START_STREAMING;
        }

        if (beforeState?.IsSelfStream == true && afterState?.IsSelfStream == false)
        {
            return VoiceState.END_STREAMING;
        }

        return VoiceState.UNKNOWN;
    }

    #endregion 判定処理

    #region メッセージ生成処理

    /// <summary>
    /// ユーザーがボイスチャンネルに参加した時の読み上げメッセージを生成します。
    /// </summary>
    /// <param name="user">Discordユーザー（Discordサーバーメンバー）</param>
    /// <param name="beforeState">イベント発生前のボイス状態</param>
    /// <param name="afterState">イベント発生後のボイス状態</param>
    /// <returns>読み上げメッセージ</returns>
    internal static string GetJoinVoiceChannelMessage(DiscordUser user, DiscordVoiceState beforeState, DiscordVoiceState afterState)
    {
        var format = new StringBuilder(Settings.AsString("Format.InternalDiscordClient.JoinVoiceChannleMessage"));
        return ReplaceCommonVoiceStateInfo(format, user, beforeState, afterState).ToString();
    }

    /// <summary>
    /// ユーザーがボイスチャンネルから退出した時の読み上げメッセージを生成します。
    /// </summary>
    /// <param name="user">Discordユーザー（Discordサーバーメンバー）</param>
    /// <param name="beforeState">イベント発生前のボイス状態</param>
    /// <param name="afterState">イベント発生後のボイス状態</param>
    /// <returns>読み上げメッセージ</returns>
    internal static string GetLeaveVoiceChannelMessage(DiscordUser user, DiscordVoiceState beforeState, DiscordVoiceState afterState)
    {
        var format = new StringBuilder(Settings.AsString("Format.InternalDiscordClient.LeaveVoiceChannleMessage"));
        return ReplaceCommonVoiceStateInfo(format, user, beforeState, afterState).ToString();
    }

    /// <summary>
    /// ユーザーがボイスチャンネルを移動した時の読み上げメッセージを生成します。
    /// </summary>
    /// <param name="user">Discordユーザー（Discordサーバーメンバー）</param>
    /// <param name="beforeState">イベント発生前のボイス状態</param>
    /// <param name="afterState">イベント発生後のボイス状態</param>
    /// <returns>読み上げメッセージ</returns>
    internal static string GetMoveVoiceChannelMessage(DiscordUser user, DiscordVoiceState beforeState, DiscordVoiceState afterState)
    {
        var format = new StringBuilder(Settings.AsString("Format.InternalDiscordClient.MoveVoiceChannleMessage"));
        return ReplaceCommonVoiceStateInfo(format, user, beforeState, afterState).ToString();
    }

    /// <summary>
    /// ユーザーが画面共有を開始した時の読み上げメッセージを生成します。
    /// </summary>
    /// <param name="user">Discordユーザー（Discordサーバーメンバー）</param>
    /// <param name="beforeState">イベント発生前のボイス状態</param>
    /// <param name="afterState">イベント発生後のボイス状態</param>
    /// <returns>読み上げメッセージ</returns>
    internal static string GetStartStreamingVoiceChannelMessage(DiscordUser user, DiscordVoiceState beforeState, DiscordVoiceState afterState)
    {
        var format = new StringBuilder(Settings.AsString("Format.InternalDiscordClient.StartStreamingVoiceChannleMessage"));
        return ReplaceCommonVoiceStateInfo(format, user, beforeState, afterState).ToString();
    }

    /// <summary>
    /// ユーザーが画面共有を終了した時の読み上げメッセージを生成します。
    /// </summary>
    /// <param name="user">Discordユーザー（Discordサーバーメンバー）</param>
    /// <param name="beforeState">イベント発生前のボイス状態</param>
    /// <param name="afterState">イベント発生後のボイス状態</param>
    /// <returns>読み上げメッセージ</returns>
    internal static string GetEndStreamingVoiceChannelMessage(DiscordUser user, DiscordVoiceState beforeState, DiscordVoiceState afterState)
    {
        var format = new StringBuilder(Settings.AsString("Format.InternalDiscordClient.EndStreamingVoiceChannleMessage"));
        return ReplaceCommonVoiceStateInfo(format, user, beforeState, afterState).ToString();
    }

    #endregion メッセージ生成処理

    #region テキスト置換処理

    /// <summary>
    /// テキスト中に含まれる読み上げ用共通パラメータを置換します。
    /// </summary>
    /// <param name="format">置換対象のテキスト</param>
    /// <param name="user">Discordユーザー（Discordサーバーメンバー）</param>
    /// <param name="beforeState">イベント発生前のボイス状態</param>
    /// <param name="afterState">イベント発生後のボイス状態</param>
    /// <returns>置換後のテキスト</returns>
    /// <remarks>
    /// 置換対象
    /// <list type="bullet">
    /// <item>ユーザー名</item>
    /// <item>チャンネル名（イベント発生前）</item>
    /// <item>チャンネル名（イベント発生後）</item>
    /// </list>
    /// </remarks>
    private static StringBuilder ReplaceCommonVoiceStateInfo(StringBuilder format, DiscordUser user, DiscordVoiceState beforeState, DiscordVoiceState afterState)
    {
        format.Replace(Settings.AsString("InternalDiscordClient.FormatReplaceKey.UserName"), user.Username);

        string name;
        if (user is DiscordMember member)
        {
            name = member.DisplayName;
        }
        else
        {
            name = user.Username;
        }

        format.Replace(Settings.AsString("InternalDiscordClient.FormatReplaceKey.NickName"), name);

        format.Replace(Settings.AsString("InternalDiscordClient.FormatReplaceKey.BeforeChannel"), beforeState?.Channel?.Name);

        format.Replace(Settings.AsString("InternalDiscordClient.FormatReplaceKey.AfterChannel"), afterState?.Channel?.Name);

        return format;
    }

    #endregion テキスト置換処理
}
