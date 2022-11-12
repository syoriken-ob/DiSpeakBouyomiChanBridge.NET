using System;

using net.boilingwater.Framework.Common.Logging;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:未使用のパラメーターを削除します", Justification = "<保留中>")]
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
            return 0;
        }
    }
}
