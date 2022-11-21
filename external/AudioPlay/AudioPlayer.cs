using NAudio.Wave;

namespace net.boilingwater.external.AudioPlay
{
    /// <summary>
    /// 音声再生クラス
    /// </summary>
    public static class AudioPlayer
    {
        /// <summary>
        /// PCM形式のbyte配列から音声を再生します。
        /// </summary>
        /// <param name="streamArray">PCM音声データのbyte配列</param>
        public static void Play(byte[] streamArray)
        {
            using var stream = new MemoryStream(streamArray);
            using var reader = new WaveFileReader(stream);
            using var waveOut = new WaveOutEvent();
            waveOut.Init(reader);
            waveOut.Play();
            while (waveOut.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100);
            }
        }
    }
}
