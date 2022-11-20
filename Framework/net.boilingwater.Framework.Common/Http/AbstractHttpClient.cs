using System;
using System.Net.Http;
using System.Threading;

using net.boilingwater.Framework.Common.Extensions;
using net.boilingwater.Framework.Common.Logging;

using net.boilingwater.Framework.Common.Setting;

namespace net.boilingwater.Framework.Common.Http
{
    /// <summary>
    /// 読み上げ処理用の基底HttpClientクラス
    /// </summary>
    public abstract class AbstractHttpClient : IDisposable
    {
        /// <summary>
        /// 内部処理用HttpClient
        /// </summary>
        protected HttpClient Client { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AbstractHttpClient() => Client = new();

        /// <summary>
        /// 内部Httpクライアントを再生成します。
        /// </summary>
        public void RenewHttpClient()
        {
            ((IDisposable)this).Dispose();
            Client = new HttpClient();
        }

        ///<inheritdoc/>
        public void Dispose()
        {
            Client.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// エラー発生時の待機処理
        /// </summary>
        /// <param name="retryCount">リトライ回数</param>
        /// <returns></returns>
        protected static bool WaitRetry(long retryCount)
        {
            if (!Settings.Get("RetryCount").HasValue() || retryCount < Settings.AsLong("RetryCount"))
            {
                Log.Logger.DebugFormat("Retry Connect:{0}/{1}", retryCount, Settings.AsLong("RetryCount"));
                Thread.Sleep(Settings.AsInteger("RetrySleepTime.Milliseconds"));
                return true;
            }
            return false;
        }
    }
}
