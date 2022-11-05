using System;
using System.Net.Http;

namespace net.boilingwater.Application.Common.Http
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
    }
}
