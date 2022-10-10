﻿using System;
using System.Net;

using net.boilingwater.Application.Common.Logging;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Http
{
    /// <summary>
    /// リクエストを受け付けるHttpServerの基底クラス
    /// </summary>
    public abstract class AbstractHttpServer : IDisposable
    {
        /// <summary>
        /// Httpリクエストを受け付けるリスナー
        /// </summary>
        protected HttpListener? Listener { get; set; }

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
                        Listener?.Stop();
                    }
                    Listener?.Close();
                }

                Listener = new HttpListener();
                RegisterListenningUrlPrefix(Listener.Prefixes);

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
        protected abstract void RegisterListenningUrlPrefix(HttpListenerPrefixCollection listenningPrefix);

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
        protected HttpListenerContext GetContextAndResumeListenning(IAsyncResult result)
        {
            var context = Listener?.EndGetContext(result) ?? throw new ApplicationException();
            Listener?.BeginGetContext(OnRequestReceived, Listener);

            return context;
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
