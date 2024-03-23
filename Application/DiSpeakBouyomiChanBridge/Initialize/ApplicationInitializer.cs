using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl.Factory;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Service;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.Http.Impl;
using net.boilingwater.Application.DiSpeakBouyomiChanBridge.InternalDiscordClient;
using net.boilingwater.BusinessLogic.Common.User.Service;
using net.boilingwater.BusinessLogic.MessageReplacer.Service;
using net.boilingwater.BusinessLogic.VoiceReadout.HttpClients;
using net.boilingwater.BusinessLogic.VoiceReadout.HttpClients.Impl;
using net.boilingwater.BusinessLogic.VoiceReadOut.Service;
using net.boilingwater.BusinessLogic.VoiceReadOut.VoiceExecutor;
using net.boilingwater.Framework.Common.Initialize;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core;
using net.boilingwater.Framework.Core.Initialize;
using net.boilingwater.Framework.Core.Logging;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge;

internal class ApplicationInitializer
{
    /// <summary>
    /// システムの初期化を行います
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    internal static void Initialize()
    {
        CoreInitializer.Initialize();
        CommonInitializer.Initialize();
        Log.Logger.Info("アプリケーションの初期化処理を開始します。");

        InitializeBusinessLogic();

        InitializeCommand();
        InitializeHttpServer();

        CommandHandlingService.Initialize();

        MessageReadOutService.ReadOutMessage(Settings.AsString("Message.FinishInitialize"));
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
    /// ビジネスロジック領域の初期化を行います。
    /// </summary>
    private static void InitializeBusinessLogic()
    {
        UserService.Initialize();
        MessageReplaceService.Initialize();
        InitializeReadOutService();
    }

    /// <summary>
    /// 読み上げサービスを初期化します
    /// </summary>
    private static void InitializeReadOutService()
    {
        if (Settings.AsBoolean("Use.VoiceVox"))
        {
            HttpClientForReadOut.Initialize<HttpClientForVoiceVox>();

            if (Settings.AsBoolean("Use.VBANEmitter"))
            {
                VoiceVoxReadOutExecutor.Initialize<VoiceVoxReadOutVBanEmitter>();
            }
            else
            {
                VoiceVoxReadOutExecutor.Initialize<VoiceVoxReadOutAudioPlayExecutor>();
            }
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
        _ = InternalDiscordClientManager.Instance.StopAsync();

        if (Settings.AsBoolean("Use.InternalDiscordClient"))
        {
            MessageReadOutService.ReadOutMessage(Settings.AsString("Message.TryLoginToDiscord"));

            if (!Settings.AsMultiList("List.InternalDiscordClient.ReadOutTarget.Guild").CastMulti<ulong>().All(guild => guild > 0UL))
            {
                throw new ApplicationException("DiscordサーバーIDが間違っています");
            }

            InternalDiscordClientManager.Instance.Initialize();

            MessageReadOutService.ReadOutMessage(Settings.AsString("Message.SuccessLoginToDiscord"));
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
        if (Settings.AsBoolean("Use.InternalDiscordClient"))
        {
            _ = InternalDiscordClientManager.Instance.StartAsync();
        }
        else
        {
            HttpServerForDiSpeak.Instance.Start();
        }

        if (Settings.AsBoolean("Use.CommonVoiceReadoutServer"))
        {
            HttpServerForCommon.Instance.Start();
        }

        MessageReadOutService.ReadOutMessage(Settings.AsString("Message.Welcome"));

        Task.Delay(-1).GetAwaiter().GetResult(); // プログラムが終了しないように無限に待機する
    }
}
