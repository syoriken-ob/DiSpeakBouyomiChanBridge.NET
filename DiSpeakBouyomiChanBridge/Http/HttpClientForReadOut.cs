using System;
using System.Net.Http;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Http
{
    /// <summary>
    /// 読み上げ処理用の基底HttpClientクラス
    /// </summary>
    public abstract class HttpClientForReadOut : IDisposable
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static HttpClientForReadOut? Instance { get; internal set; }

        /// <summary>
        /// 内部処理用HttpClient
        /// </summary>
        protected HttpClient client_;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public HttpClientForReadOut() => client_ = new();

        /// <summary>
        /// 内部Httpクライアントを再生成します。
        /// </summary>
        public void RenewHttpClient()
        {
            ((IDisposable)this).Dispose();
            client_ = new HttpClient();
        }

        /// <summary>
        /// メッセージを読み上げます。
        /// </summary>
        /// <param name="text"></param>
        public abstract void ReadOut(string text);

        /// <summary>
        /// リソースを解放します
        /// </summary>
        public void Dispose()
        {
            client_.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static void Initialize<T>() where T : HttpClientForReadOut
        {
            Instance = (HttpClientForReadOut?)Activator.CreateInstance(typeof(T));
        }
    }
}
