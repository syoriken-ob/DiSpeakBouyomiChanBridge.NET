using System.Reflection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

using net.boilingwater.Application.Common.Utils;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.external.DiscordClient
{
    /// <summary>
    /// Discordクライアント実装クラス
    /// </summary>
    public class DiscordDotNetClient
    {
        /// <summary>
        /// 読み上げ対象のサーバー
        /// </summary>
        public IEnumerable<string>? TargetGuild { private get; set; }

        public Func<SocketMessage, Task>? MessageReceived { private get; set; }
        public Func<SocketUser, SocketVoiceState, SocketVoiceState, Task>? UserVoiceStatusUpdated { private get; set; }
        public Func<LogMessage, Task>? Logging { private get; set; }
        public DiscordSocketClient? InnerClient { get; private set; }
        internal CommandService? Commands { get; set; }
        internal IServiceProvider? ServicesProvider { get; set; }

        /// <summary>
        /// 非同期で初期化処理を行います
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            InnerClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                ConnectionTimeout = int.MaxValue,
                AlwaysDownloadUsers = true,
            });
            Commands = new CommandService();
            ServicesProvider = new ServiceCollection().BuildServiceProvider();

            _ = await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), ServicesProvider);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 非同期で開始処理を行います
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task StartAsync(string token)
        {
            if (InnerClient == null)
            {
                throw new InvalidOperationException();
            }
            //ハンドラ関数のセット
            if (Logging != null)
            {
                InnerClient.Log += Logging;
            }
            if (MessageReceived != null)
            {
                InnerClient.MessageReceived += MessageReceived;
            }
            if (UserVoiceStatusUpdated != null)
            {
                InnerClient.UserVoiceStateUpdated += UserVoiceStatusUpdated;
            }

            await InnerClient.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);

            if (TargetGuild != null)
            {
                var guilds = TargetGuild.Select(guild => InnerClient.GetGuild(CastUtil.ToUnsignedLong(guild)));
                if (guilds.Any())
                {
                    await InnerClient.DownloadUsersAsync(guilds).ConfigureAwait(false);
                }
            }

            await InnerClient.StartAsync();

            await Task.Delay(-1);
        }

        /// <summary>
        /// 非同期で終了処理を行います
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            if (InnerClient == null)
            {
                await Task.CompletedTask;
                return;
            }
            await InnerClient.StopAsync();
            InnerClient.LogoutAsync();

            await Task.CompletedTask;
        }
    }
}
