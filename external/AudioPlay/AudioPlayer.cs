using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using NAudio.Wave;

namespace net.boilingwater.external.AudioPlay;

/// <summary>
/// 音声再生クラス
/// </summary>
public static class AudioPlayer
{
    /// <summary>
    /// PCM形式のbyte配列から音声を再生します。
    /// </summary>
    /// <param name="streamArray">PCM音声データのbyte配列</param>
    public static async Task PlayAsync(byte[] streamArray, CancellationToken? cancellation = null)
    {
        using var stream = new MemoryStream(streamArray);
        using var reader = new WaveFileReader(stream);
        using var waveOut = new WaveOutEvent();
        waveOut.Init(reader);
        waveOut.Play();
        while (!(cancellation?.IsCancellationRequested ?? false) && waveOut.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }
}
