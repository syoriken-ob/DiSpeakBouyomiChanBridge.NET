using NAudio.Wave;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.external.AudioPlay
{
    public static class AudioPlayer
    {
        public static void Play(Stream stream)
        {
            using (var reader = new WaveFileReader(stream))
            {
                using (var waveOut = new WaveOutEvent())
                {
                    waveOut.Init(reader);
                    waveOut.Play();
                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }
    }
}