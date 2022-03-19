using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Discord.WebSocket;

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
        /// <summary>
        /// システムの初期化を行います
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        internal static void Initialize()
        {
            Log.Logger.Info("Start Application Initialization!");
            SettingHolder.Initialize();
            CommandInitialize();

            if (Settings.AsBoolean("Use.InternalDiscordClient"))
            {
                HttpClientForBouyomiChan.Instance.SendToBouyomiChan(Settings.AsString("Message.TryLoginToDiscord"));

                DiscordDotNetClient.InitializeAsync().GetAwaiter().GetResult();

                var discordEventHandler = new DiscordEventHandler(DiscordDotNetClient.Client);

                if (Settings.AsBoolean("Use.RedirectLogDiscord.Net"))
                {
                    DiscordDotNetClient.Logging = discordEventHandler.Logging;
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
                    DiscordDotNetClient.MessageReceived = discordEventHandler.MessageReceived;
                }

                if (Settings.AsBoolean("Use.ReadOut.GuildChannel.Voice"))
                {
                    DiscordDotNetClient.UserVoiceStatusUpdated = discordEventHandler.UserVoiceStatusUpdated;
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
            Log.Logger.Info("Finish Application Initialization!");
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
                DiscordDotNetClient.StartAsync(Settings.Get("DiscordToken")).GetAwaiter().GetResult();
            }
            else
            {
                HttpServerForDiSpeak.Instance.Start();
            }
        }
    }
}
