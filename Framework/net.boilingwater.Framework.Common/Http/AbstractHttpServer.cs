using System;
using System.Net;

using net.boilingwater.Framework.Common.Logging;

namespace net.boilingwater.Framework.Common.Http
{
    /// <summary>
    /// Httpリクエスト処理を受け付ける基底HttpServerクラス
    /// </summary>
    public abstract class AbstractHttpServer : IDisposable
    {
        /// <summary>
        /// Httpリクエストを受け付けるリスナー
        /// </summary>
        protected HttpListener? Listener { get; set; }

        /// <summary>
        /// リクエスト受付時のAsyncResult
        /// </summary>
        private IAsyncResult? CurrentAsyncResult { get; set; }

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Initialize()
        {
            try
            {
                if (Listener != null)
                {
                    if (Listener.IsListening)
                    {
                        Listener.Stop();
                    }
                    Listener?.Close();
                }

                Listener = new HttpListener();
                RegisterListeningUrlPrefix(Listener.Prefixes);

                foreach (var prefix in Listener.Prefixes)
                {
                    Log.Logger.Debug($"Listener({GetType().Name}) Add Prefix: {prefix}");
                }
                //接続テストをする
                Log.Logger.Info($"Attempt Listening({GetType().Name})...");
                Listener.Start();
                Log.Logger.Info($"Succeed Listening({GetType().Name}) !");
                Listener.Stop();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal($"Listener({GetType().Name}) Fail to Open Port !", ex);
                throw;
            }
        }

        /// <summary>
        /// リクエストを受け付けるURLのプレフィックスを設定します。
        /// </summary>
        protected abstract void RegisterListeningUrlPrefix(HttpListenerPrefixCollection listenerPrefix);

        /// <summary>
        /// リクエスト受付を開始します。
        /// </summary>
        /// <exception cref="InvalidOperationException">HttpListenerの初期化に失敗していた場合発生します</exception>
        public void Start()
        {
            if (Listener == null)
            {
                throw new InvalidOperationException("初期化されていません");
            }

            Listener.Start();
            Listener.BeginGetContext(OnRequestReceived, null);
        }

        /// <summary>
        /// HttpListenerContextを取得し、リクエスト受付を再開します。
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"/>
        protected HttpListenerContext? GetContextAndResumeListening(IAsyncResult result)
        {
            if (CurrentAsyncResult != result)
            {
                return null;
            }
            var context = Listener?.EndGetContext(result) ?? throw new ApplicationException();
            Listener?.BeginGetContext(OnRequestReceivedWrapper, Listener);

            return context;
        }

        /// <summary>
        /// リクエスト受付時の処理(内部処理用)
        /// </summary>
        private void OnRequestReceivedWrapper(IAsyncResult result)
        {
            CurrentAsyncResult = result;
            OnRequestReceived(result);
        }

        /// <summary>
        /// リクエスト受付時の処理
        /// </summary>
        protected abstract void OnRequestReceived(IAsyncResult result);

        ///<inheritdoc/>
        public void Dispose()
        {
            Listener?.Stop();
            Listener?.Close();
            GC.SuppressFinalize(this);
        }
    }
}
