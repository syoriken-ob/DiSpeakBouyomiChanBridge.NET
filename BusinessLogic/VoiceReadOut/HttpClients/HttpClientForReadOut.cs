namespace net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.VoiceReadout.HttpClients
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
        protected HttpClient Client { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public HttpClientForReadOut() => Client = new();

        /// <summary>
        /// 内部Httpクライアントを再生成します。
        /// </summary>
        public void RenewHttpClient()
        {
            ((IDisposable)this).Dispose();
            Client = new HttpClient();
        }

        /// <summary>
        /// メッセージを読み上げます。
        /// </summary>
        /// <param name="text"></param>
        public abstract void ReadOut(string text);

        ///<inheritdoc/>
        public void Dispose()
        {
            Client.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 初期化処理
        /// <para><typeparamref name="T"/>によって読み上げ先が変わります。</para>
        /// </summary>
        /// <typeparam name="T"><see cref="HttpClientForReadOut"/>を継承した型</typeparam>
        /// <returns></returns>
        public static void Initialize<T>() where T : HttpClientForReadOut
        {
            Instance = (HttpClientForReadOut?)Activator.CreateInstance(typeof(T));
        }
    }
}
