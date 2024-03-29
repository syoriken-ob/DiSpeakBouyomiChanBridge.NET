﻿using System;

using net.boilingwater.Framework.Core.Logging;

namespace net.boilingwater.Application.VoiceVoxReverseProxy;

/// <summary>
/// VoiceVoxReverseProxy エントリーポイントのクラス
/// </summary>
public class VoiceVoxReverseProxy
{
    /// <summary>
    /// エントリーポイント
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static int Main(string[] args)
    {
        try
        {
            ApplicationInitializer.Initialize();
            ApplicationInitializer.Start();
            while (true)
            {
                Console.ReadLine();
            }
        }
        catch (Exception e)
        {
            Log.Logger.Fatal("エラーが発生したため、実行を終了します。", e);
            return -1;
        }
    }
}
