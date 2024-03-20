using net.boilingwater.BusinessLogic.VoiceReadOut.Service;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core.Logging;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl;

/// <summary>
/// リロード処理を実行します。
/// </summary>
public class ReloadCommand : SystemCommand
{
    /// <summary>
    /// 処理を実行します
    /// </summary>
    public override void Execute()
    {
        ApplicationInitializer.Initialize();
        ApplicationInitializer.Start();
        Log.Logger.Info("Reload SystemConfig...");
        MessageReadOutService.ReadOutMessage(Settings.AsString("Message.ReloadConfig"));
    }
}
