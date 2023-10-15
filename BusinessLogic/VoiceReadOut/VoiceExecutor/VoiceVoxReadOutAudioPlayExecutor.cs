using System;
using System.Collections.Concurrent;
using System.Threading;

using net.boilingwater.external.AudioPlay;
using net.boilingwater.Framework.Core.Logging;

namespace net.boilingwater.BusinessLogic.VoiceReadOut.VoiceExecutor
{
    /// <summary>
    /// VoiceVoxで生成した音声を順次再生します。
    /// </summary>
    public class VoiceVoxReadOutAudioPlayExecutor : VoiceVoxReadOutExecutor
    {
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
        public override void AddQueue(byte[] audioStreamByteArray)
        {
            if (audioStreamByteArray == null)
            {
                throw new ArgumentNullException(nameof(audioStreamByteArray));
            }
            _audioStreamByteArrays.Add(audioStreamByteArray);
        }

        public override void Dispose()
        {
            try
            {
                _thread.Interrupt();
            }
            catch
            {
                //何もしない
            }
            finally
            {
                _audioStreamByteArrays.Dispose();
            }
        }
    }
}
