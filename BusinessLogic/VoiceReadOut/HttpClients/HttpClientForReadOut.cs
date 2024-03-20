using System;

using net.boilingwater.BusinessLogic.VoiceReadOut.Dto;
using net.boilingwater.Framework.Common.Http;

namespace net.boilingwater.BusinessLogic.VoiceReadout.HttpClients;

/// <summary>
/// 読み上げ処理用の基底HttpClientクラス
/// </summary>
public abstract class HttpClientForReadOut : AbstractHttpClient
{
    /// <summary>
    /// インスタンスが破棄されているか
    /// </summary>
    public bool IsDisposed { get; protected set; } = false;

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static HttpClientForReadOut? Instance { get; internal set; }

    /// <summary>
    /// メッセージを読み上げます。
    /// </summary>
    /// <param name="message">メッセージと話者キーを含んだDto</param>
    public abstract void ReadOut(MessageDto message);

    /// <summary>
    /// 初期化処理
    /// <para><typeparamref name="T"/>によって読み上げ先が変わります。</para>
    /// </summary>
    /// <typeparam name="T"><see cref="HttpClientForReadOut"/>を継承した型</typeparam>
    /// <returns></returns>
    public static void Initialize<T>() where T : HttpClientForReadOut
    {
        Instance?.Dispose();
        Instance = (HttpClientForReadOut?)Activator.CreateInstance(typeof(T));
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        IsDisposed = true;
        base.Dispose();
    }
}
