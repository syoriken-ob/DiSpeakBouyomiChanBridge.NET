using System;

namespace net.boilingwater.BusinessLogic.VoiceReadOut.VoiceExecutor
{
    public abstract class VoiceVoxReadOutExecutor
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static VoiceVoxReadOutExecutor? Instance { get; private set; }

        /// <summary>
        /// VoiceVoxで生成した音声データのバイト配列を再生キューに追加します。
        /// </summary>
        /// <param name="audioStreamByteArray">音声データ</param>
        public abstract void AddQueue(byte[] audioStreamByteArray);

        /// <summary>
        /// 初期化処理
        /// <para><typeparamref name="T"/>によって読み上げ先が変わります。</para>
        /// </summary>
        /// <typeparam name="T"><see cref="VoiceVoxReadOutExecutor"/>を継承した型</typeparam>
        /// <returns></returns>
        public static void Initialize<T>() where T : VoiceVoxReadOutExecutor => Instance = (VoiceVoxReadOutExecutor?)Activator.CreateInstance(typeof(T));
    }
}
