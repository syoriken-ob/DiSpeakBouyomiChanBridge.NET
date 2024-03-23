using System;

using net.boilingwater.Framework.Core.Logging;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge;

/// <summary>
/// DiSpeakBouyomiChanBridge エントリーポイントのクラス
/// </summary>
public class DiSpeakBouyomiChanBridge
{
    /// <summary>
    /// アプリケーションを実行します
    /// </summary>
    /// <param name="args">実行時引数</param>
    public static int Main(string[] args)
    {
        try
        {
            ApplicationInitializer.Initialize();
            ApplicationInitializer.Start();
            return 0;
        }
        catch (Exception e)
        {
            Log.Logger.Fatal("エラーが発生したため、実行を終了します。", e);
            return -1;
        }
    }
}
