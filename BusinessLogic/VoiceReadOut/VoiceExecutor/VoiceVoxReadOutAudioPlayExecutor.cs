using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using net.boilingwater.external.AudioPlay;
using net.boilingwater.Framework.Core.Logging;

namespace net.boilingwater.BusinessLogic.VoiceReadOut.VoiceExecutor;

/// <summary>
/// VoiceVoxで生成した音声を順次再生します。
/// </summary>
public class VoiceVoxReadOutAudioPlayExecutor : VoiceVoxReadOutExecutor
{
    private Task Task { get; init; }
    private CancellationTokenSource CancellationTokenSource { get; init; }
    private BlockingCollection<byte[]> AudioStreamBytes { get; init; } = [];

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public VoiceVoxReadOutAudioPlayExecutor()
    {
        CancellationTokenSource = new CancellationTokenSource();

        Task = Task.Factory.StartNew((obj) =>
        {
            foreach (var voiceStreamByteArr in AudioStreamBytes.GetConsumingEnumerable())
            {
                try
                {
                    AudioPlayer.PlayAsync(voiceStreamByteArr, CancellationTokenSource.Token).GetAwaiter().GetResult();

                    if (CancellationTokenSource.IsCancellationRequested) //キャンセル処理
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex);
                }
            }
            Log.Logger.Debug("Finished AudioStreamBytes.GetConsumingEnumerable.");
            AudioStreamBytes.Dispose();
        }, null, TaskCreationOptions.LongRunning);
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
        AudioStreamBytes.Add(audioStreamByteArray);
    }

    public override void Dispose()
    {
        lock (AudioStreamBytes)
        {
            AudioStreamBytes.CompleteAdding();
            while (AudioStreamBytes.Count > 0)
            {
                AudioStreamBytes.Take(); //中身を空にするまでTakeする
            }
            CancellationTokenSource.Cancel();
            GC.SuppressFinalize(this);
        }
    }
}
