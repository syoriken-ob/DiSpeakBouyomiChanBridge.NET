using System;
using System.Data;
using System.Linq;
using System.Reflection;

using net.boilingwater.Application.Common.Extensions;
using net.boilingwater.Application.Common.Extentions;
using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.Application.Common.SQLite;
using net.boilingwater.Application.Common.Utils;
using net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.VoiceReadout.HttpClients;
using net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.VoiceReadout.HttpClients.Impl;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Impl.Factory;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Service;
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

            Settings.Initialize();
            DBInitialize();
            CommandInitialize();
            ReadOutServiceInitialize();
            HttpServerInitialize();
            CommandHandlingService.Initialize();

            HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.FinishInitialize"));
            Log.Logger.Info("アプリケーションの初期化処理が完了しました。");
            HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.Welcome"));
        }

        /// <summary>
        /// DB処理の初期化を行います
        /// </summary>
        private static void DBInitialize()
        {
            SQLiteDBDao.CreateDataBase();

            Assembly.GetExecutingAssembly()
                    .CollectReferencedAssemblies(assemblyName => assemblyName.Name != null && assemblyName.Name.StartsWith("net.boilingwater"))
                    .ForEach(assembly => assembly?.GetTypes()
                                                  .Where(t => t.IsSubclassOf(typeof(SQLiteDBDao)) && !t.IsAbstract)
                                                  .Select(type => (SQLiteDBDao?)Activator.CreateInstance(type))
                                                  .ForEach(dao => dao?.InitializeTable()));
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
        /// 読み上げサービスを初期化します
        /// </summary>
        private static void ReadOutServiceInitialize()
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
        private static void HttpServerInitialize()
        {
            if (Settings.AsBoolean("Use.InternalDiscordClient"))
            {
                HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.TryLoginToDiscord"));

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

                HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.SuccessLoginToDiscord"));
            }
            else
            {
                HttpServerForDiSpeak.Instance.Initialize();
            }
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
