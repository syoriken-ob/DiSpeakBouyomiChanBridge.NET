using System;
using System.Linq;
using System.Runtime.CompilerServices;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl.Factory;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Service;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.Http.Impl;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.InternalDiscordClient;
using net.boilingwater.BusinessLogic.MessageReplacer.Service;
using net.boilingwater.BusinessLogic.VoiceReadout.HttpClients;
using net.boilingwater.BusinessLogic.VoiceReadout.HttpClients.Impl;
using net.boilingwater.external.DiscordClient;
using net.boilingwater.Framework.Common.Initialize;
using net.boilingwater.Framework.Common.Logging;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge
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
            CommonInitializer.Initialize();
            Log.Logger.Info("アプリケーションの初期化処理を開始します。");

            InitializeCommand();
            InitializeReadOutService();
            InitializeHttpServer();
            CommandHandlingService.Initialize();
            MessageReplaceService.Initialize();

            HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.FinishInitialize"));
            Log.Logger.Info("アプリケーションの初期化処理が完了しました。");
        }

        /// <summary>
        /// コマンドの初期化を行います
        /// </summary>
        private static void InitializeCommand()
        {
            CommandFactory.Factory.Initialize();
            SystemCommandFactory.Factory.Initialize();
        }

        /// <summary>
        /// 読み上げサービスを初期化します
        /// </summary>
        private static void InitializeReadOutService()
        {
            if (Settings.AsBoolean("Use.VoiceVox"))
            {
                HttpClientForReadOut.Initialize<HttpClientForVoiceVox>();
            }
            else
            {
                HttpClientForReadOut.Initialize<HttpClientForBouyomiChan>();
            }
        }

        /// <summary>
        /// HttpServerの初期化します
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        private static void InitializeHttpServer()
        {
            if (_client != null && _client.InnerClient != null)
            {
                _client.StopAsync().GetAwaiter().GetResult();
            }

            if (Settings.AsBoolean("Use.InternalDiscordClient"))
            {
                HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.TryLoginToDiscord"));

                _client = new();
                _client.InitializeAsync().GetAwaiter().GetResult();

                var discordEventHandler = new DiscordEventHandler(_client.InnerClient);

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

                HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.SuccessLoginToDiscord"));
            }
            else
            {
                HttpServerForDiSpeak.Instance.Initialize();
            }

            if (Settings.AsBoolean("Use.CommonVoiceReadoutServer"))
            {
                HttpServerForCommon.Instance.Initialize();
            }
        }

        /// <summary>
        /// 処理を開始します
        /// </summary>
        internal static void Start()
        {
            TaskAwaiter? awaiter = null;
            if (Settings.AsBoolean("Use.InternalDiscordClient"))
            {
                if (_client == null)
                {
                    throw new InvalidOperationException("先にアプリケーションの初期化処理を行ってください。");
                }
                awaiter = _client.StartAsync(Settings.AsString("DiscordToken")).GetAwaiter();
            }
            else
            {
                HttpServerForDiSpeak.Instance.Start();
            }

            if (Settings.AsBoolean("Use.CommonVoiceReadoutServer"))
            {
                HttpServerForCommon.Instance.Start();
            }

            HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.Welcome"));
            awaiter?.GetResult();
        }
    }
}
