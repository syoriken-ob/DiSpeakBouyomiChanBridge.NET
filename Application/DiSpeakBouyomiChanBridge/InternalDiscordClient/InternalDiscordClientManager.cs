using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.EventArgs;

using Microsoft.Extensions.Logging;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Dto;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Service;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.InternalDiscordClient.Services;
using net.boilingwater.Framework.Common.Setting;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.InternalDiscordClient;

/// <summary>
/// Discordクライアント管理クラス
/// </summary>
public class InternalDiscordClientManager
{
    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static InternalDiscordClientManager Instance { get; private set; } = new();

    /// <summary>
    /// Discord接続用トークン
    /// </summary>
    public DiscordClient? Client { get; private set; }

    /// <summary>
    /// コンストラクタ処理
    /// </summary>
    private InternalDiscordClientManager()
    { }

    /// <summary>
    /// 初期化処理
    /// </summary>
    public void Initialize()
    {
        var config = new DiscordConfiguration()
        {
            Token = Settings.AsString("InternalDiscordClient.DiscordToken"),
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
            AutoReconnect = true,
            AlwaysCacheMembers = true,
        };

        if (Settings.AsBoolean("Use.InternalDiscordClient.RedirectLog"))
        {
            if (Enum.TryParse(Settings.AsString("InternalDiscordClient.RedirectLog.LogSeverity"), out LogLevel logLevel))
            {
                config.MinimumLogLevel = logLevel;
            }

            config.LoggerFactory = new LoggerFactory().AddLog4Net(new Log4NetProviderOptions //Log.Loggerの設定を共通にする
            {
                ExternalConfigurationSetup = true,
            });
        }

        Client = new DiscordClient(config);

        if (Settings.AsBoolean("Use.InternalDiscordClient.ReadOut.GuildTextChannel"))
        {
            Client.MessageCreated += OnMessageReceived;
        }

        if (Settings.AsBoolean("Use.InternalDiscordClient.ReadOut.GuildVoiceChannel"))
        {
            Client.VoiceStateUpdated += OnUserVoiceStatusUpdated;
        }
    }

    /// <summary>
    /// 非同期で開始処理を行います
    /// </summary>
    /// <returns></returns>
    public async Task StartAsync()
    {
        if (Client == null)
        {
            throw new InvalidOperationException("先に初期化を行ってください。");
        }

        await Client.ConnectAsync();
    }

    /// <summary>
    /// 非同期で終了処理を行います
    /// </summary>
    /// <returns></returns>
    public async Task StopAsync()
    {
        if (Client != null)
        {
            await Client.DisconnectAsync();
        }
        await Task.CompletedTask;
    }

    #region EventHandler

    /// <summary>
    /// Discordからメッセージを受信した時の処理を定義します。
    /// </summary>
    /// <param name="client">Discordクライアントインスタンス</param>
    /// <param name="e">メッセージ生成イベント情報</param>
    /// <returns></returns>
    protected static async Task OnMessageReceived(DiscordClient client, MessageCreateEventArgs e)
    {
        if (DiscordReceivedMessageService.IsPrivateMessage(e))
        {
            await Task.CompletedTask;
            return;
        }

        if (!DiscordReceivedMessageService.IsReadOutTargetGuild(e))
        {
            await Task.CompletedTask;
            return;
        }

        if (!DiscordReceivedMessageService.IsReadOutTargetGuildChannel(e))
        {
            await Task.CompletedTask;
            return;
        }

        var formattedMessage = DiscordReceivedMessageService.GetFormattedMessage(e);

        //TODO:おいおいサービスクラスはDIしたいね
        CommandHandlingService.Handle(new CommandHandlingContext(formattedMessage, e.Author.Id.ToString()));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Discordからユーザーのステータス変更イベントを受信した時の処理を定義します。
    /// </summary>
    /// <param name="client">Discordクライアントインスタンス</param>
    /// <param name="e">ボイスステータス更新イベント情報</param>
    /// <returns></returns>
    protected static async Task OnUserVoiceStatusUpdated(DiscordClient client, VoiceStateUpdateEventArgs e)
    {
        if (!DiscordUserVoiceStateUpdatedService.IsReadOutTargetGuild(e.Before, e.After))
        {
            await Task.CompletedTask;
            return;
        }

        if (!DiscordUserVoiceStateUpdatedService.IsReadOutTargetGuildChannel(e.Before, e.After))
        {
            await Task.CompletedTask;
            return;
        }

        DiscordUserVoiceStateUpdatedService.VoiceState state = DiscordUserVoiceStateUpdatedService.DetectVoiceStateUpdate(e.Before, e.After);

        switch (state) //TODO: おいおいストラテジーパターンを適用したいね
        {
            case DiscordUserVoiceStateUpdatedService.VoiceState.JOIN:
                CommandHandlingService.Handle(new CommandHandlingContext(
                    DiscordUserVoiceStateUpdatedService.GetJoinVoiceChannelMessage(e.User, e.Before, e.After),
                    e.User.Id.ToString()
                ));
                break;

            case DiscordUserVoiceStateUpdatedService.VoiceState.LEAVE:
                CommandHandlingService.Handle(new CommandHandlingContext(
                    DiscordUserVoiceStateUpdatedService.GetLeaveVoiceChannelMessage(e.User, e.Before, e.After),
                    e.User.Id.ToString()
                ));
                break;

            case DiscordUserVoiceStateUpdatedService.VoiceState.MOVE:
                CommandHandlingService.Handle(new CommandHandlingContext(
                    DiscordUserVoiceStateUpdatedService.GetMoveVoiceChannelMessage(e.User, e.Before, e.After),
                    e.User.Id.ToString()
                ));
                break;

            case DiscordUserVoiceStateUpdatedService.VoiceState.START_STREAMING:
                CommandHandlingService.Handle(new CommandHandlingContext(
                    DiscordUserVoiceStateUpdatedService.GetStartStreamingVoiceChannelMessage(e.User, e.Before, e.After),
                    e.User.Id.ToString()
                ));
                break;

            case DiscordUserVoiceStateUpdatedService.VoiceState.END_STREAMING:
                CommandHandlingService.Handle(new CommandHandlingContext(
                    DiscordUserVoiceStateUpdatedService.GetEndStreamingVoiceChannelMessage(e.User, e.Before, e.After),
                    e.User.Id.ToString()
                ));
                break;

            default:
                break;
        }
        await Task.CompletedTask;
    }

    #endregion EventHandler
}
