using System;

using net.boilingwater.Application.Common.Logging;

namespace net.boilingwater.DiSpeakBouyomiChanBridge
{
    /// <summary>
    /// エントリーポイントのクラス
    /// </summary>
    public class DiSpeakBoouyomiChanBridge
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
}
