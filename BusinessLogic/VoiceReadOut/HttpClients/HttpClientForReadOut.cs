using System;

using net.boilingwater.Framework.Common.Http;

namespace net.boilingwater.BusinessLogic.VoiceReadout.HttpClients
{
    /// <summary>
    /// 読み上げ処理用の基底HttpClientクラス
    /// </summary>
    public abstract class HttpClientForReadOut : AbstractHttpClient
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static HttpClientForReadOut? Instance { get; internal set; }

        /// <summary>
        /// メッセージを読み上げます。
        /// </summary>
        /// <param name="text"></param>
        public abstract void ReadOut(string text);

        /// <summary>
        /// 初期化処理
        /// <para><typeparamref name="T"/>によって読み上げ先が変わります。</para>
        /// </summary>
        /// <typeparam name="T"><see cref="HttpClientForReadOut"/>を継承した型</typeparam>
        /// <returns></returns>
        public static void Initialize<T>() where T : HttpClientForReadOut => Instance = (HttpClientForReadOut?)Activator.CreateInstance(typeof(T));
    }
}
