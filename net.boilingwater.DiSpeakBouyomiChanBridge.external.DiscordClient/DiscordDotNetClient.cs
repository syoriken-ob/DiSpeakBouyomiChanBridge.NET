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
        public DiscordSocketClient? Client { get; private set; }
        internal CommandService? commands;
        internal IServiceProvider? services;

        /// <summary>
        /// 読み上げ対象のサーバー
        /// </summary>
        public IEnumerable<string>? TargetGuild { private get; set; }

        public Func<SocketMessage, Task>? MessageReceived { private get; set; }
        public Func<SocketUser, SocketVoiceState, SocketVoiceState, Task>? UserVoiceStatusUpdated { private get; set; }
        public Func<LogMessage, Task>? Logging { private get; set; }

        /// <summary>
        /// 非同期で初期化処理を行います
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                ConnectionTimeout = int.MaxValue
            });
            commands = new CommandService();
            services = new ServiceCollection().BuildServiceProvider();

            _ = await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

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
            if (Client == null)
            {
                throw new InvalidOperationException();
            }
            //ハンドラ関数のセット
            if (Logging != null)
            {
                Client.Log += Logging;
            }
            if (MessageReceived != null)
            {
                Client.MessageReceived += MessageReceived;
            }
            if (UserVoiceStatusUpdated != null)
            {
                Client.UserVoiceStateUpdated += UserVoiceStatusUpdated;
            }

            await Client.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);

            if (TargetGuild != null)
            {
                var guilds = TargetGuild.Select(guild => Client.GetGuild(CastUtil.ToUnsignedLong(guild)));
                if (guilds.Any())
                {
                    await Client.DownloadUsersAsync(guilds).ConfigureAwait(false);
                }
            }

            await Client.StartAsync();

            await Task.Delay(-1);
        }
    }
}