using System;
using System.Linq;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.Application.Common.Utils;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Handle.Impl;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Impl.Factory;
using net.boilingwater.DiSpeakBouyomiChanBridge.external.DiscordClient;
using net.boilingwater.DiSpeakBouyomiChanBridge.Http;
using net.boilingwater.DiSpeakBouyomiChanBridge.InternalDiscordClient;

namespace net.boilingwater.DiSpeakBouyomiChanBridge
{
    internal class ApplicationInitializer
    {
        private static DiscordDotNetClient? _client;
        /// <summary>
        /// システムの初期化を行います
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        internal static void Initialize()
        {
            Log.Logger.Info("アプリケーションの初期化処理を開始します。");
            SettingHolder.Initialize();
            CommandInitialize();

            if (Settings.AsBoolean("Use.InternalDiscordClient"))
            {
                HttpClientForBouyomiChan.Instance.SendToBouyomiChan(Settings.AsString("Message.TryLoginToDiscord"));

                _client = new();
                _client.InitializeAsync().GetAwaiter().GetResult();

                var discordEventHandler = new DiscordEventHandler(_client.Client);

                if (Settings.AsBoolean("Use.RedirectLogDiscord.Net"))
                {
                    _client.Logging = discordEventHandler.Logging;
                }

                var guilds = Settings.AsStringList("List.ReadOutTarget.Guild");
                if (guilds == null)
                {
                    throw new ApplicationException("DiscordサーバーIDが間違っています");
                }
                if (!guilds.All(guild => CastUtil.ToUnsignedLong(guild) > 0UL))
                {
                    throw new ApplicationException("DiscordサーバーIDが間違っています");
                }

                if (Settings.AsBoolean("Use.ReadOut.GuildChannel.Text"))
                {
                    _client.MessageReceived = discordEventHandler.MessageReceived;
                }

                if (Settings.AsBoolean("Use.ReadOut.GuildChannel.Voice"))
                {
                    _client.UserVoiceStatusUpdated = discordEventHandler.UserVoiceStatusUpdated;
                }

                CommandHandlingService.Initialize(new InternalDiscordClientCommandHandler());
                HttpClientForBouyomiChan.Instance.SendToBouyomiChan(Settings.AsString("Message.SuccessLoginToDiscord"));
            }
            else
            {
                CommandHandlingService.Initialize(new DiSpeakCommandHandler());
                HttpServerForDiSpeak.Instance.Initialize();
            }

            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(Settings.AsString("Message.FinishInitialize"));
            Log.Logger.Info("アプリケーションの初期化処理が完了しました。");
            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(Settings.AsString("Message.Welcome"));
        }

        /// <summary>
        /// コマンドの初期化を行います
        /// </summary>
        public static void CommandInitialize()
        {
            CommandFactory.Factory.Initialize();
            SystemCommandFactory.Factory.Initialize();
        }

        /// <summary>
        /// 処理を開始します
        /// </summary>
        internal static void Start()
        {
            if (Settings.AsBoolean("Use.InternalDiscordClient"))
            {
                if (_client == null)
                {
                    throw new InvalidOperationException("先にアプリケーションの初期化処理を行ってください。");
                }
                _client.StartAsync(Settings.Get("DiscordToken")).GetAwaiter().GetResult();
            }
            else
            {
                HttpServerForDiSpeak.Instance.Start();
            }
        }
    }
}
