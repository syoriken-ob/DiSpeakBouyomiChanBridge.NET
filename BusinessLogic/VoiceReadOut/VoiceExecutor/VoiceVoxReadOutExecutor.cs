using System.Collections.Concurrent;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.DiSpeakBouyomiChanBridge.external.AudioPlay;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.VoiceReadOut.VoiceExecutor
{
    /// <summary>
    /// VoiceVoxで生成した音声を順次再生します。
    /// </summary>
    public class VoiceVoxReadOutAudioPlayExecutor
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static VoiceVoxReadOutAudioPlayExecutor Instance { get; } = new();

        private readonly Thread _thread;
        private readonly BlockingCollection<byte[]> _audioStreamByteArrays = new();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public VoiceVoxReadOutAudioPlayExecutor()
        {
            _thread = new Thread(() =>
            {
                foreach (var voiceStreamByteArr in _audioStreamByteArrays.GetConsumingEnumerable())
                {
                    try
                    {
                        AudioPlayer.Play(voiceStreamByteArr);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex);
                    }
                }
            })
            {
                IsBackground = true
            };
            _thread.Start();
        }

        /// <summary>
        /// VoiceVoxで生成した音声データのバイト配列を再生キューに追加します。
        /// </summary>
        /// <param name="audioStreamByteArray"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddQueue(byte[] audioStreamByteArray)
        {
            if (audioStreamByteArray == null)
            {
                throw new ArgumentNullException(nameof(audioStreamByteArray));
            }
            _audioStreamByteArrays.Add(audioStreamByteArray);
        }
    }
}
