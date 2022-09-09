using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.DiSpeakBouyomiChanBridge.external.AudioPlay;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Voice
{
    /// <summary>
    /// VoiceVoxで生成した音声を順次再生します。
    /// </summary>
    public class VoiceVoxReadOutExecutor
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static VoiceVoxReadOutExecutor Instance { get; } = new();

        private readonly Thread _thread;
        private readonly BlockingCollection<byte[]> _audioStreamByteArrays = new();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public VoiceVoxReadOutExecutor()
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
