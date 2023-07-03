using System;

using net.boilingwater.external.VBanProtocolEmitter.Const;

using static net.boilingwater.external.VBanProtocolEmitter.Stricts.VBanHeader;

namespace net.boilingwater.external.VBanProtocolEmitter
{
    /// <summary>
    /// VBANProtocol送信用設定Dto
    /// </summary>
    public class EmitterConfig
    {
        public string Host { get; private set; }
        public int Port { get; private set; }
        public int AudioSamplingRate { get; private set; }
        public int BytePerSample { get; private set; }
        public FormatSamplingRate FormatSamplingRate { get; private set; }
        public FormatBitDepth FormatBitDepth { get; private set; }
        public byte AudioChannelCount { get; private set; }
        public string StreamName { get; private set; }
        public int MaxDataSize { get; private set; }
        public int BufferPacketCount { get; private set; }
        public TimeSpan EmitInterval { get; private set; }

        public EmitterConfig(string host, int port, int audioSamplingRate, int bitDepth, int audioChannelCount, string streamName, string bufferSize)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            AudioSamplingRate = audioSamplingRate;
            BytePerSample = bitDepth / 8;
            FormatSamplingRate = ConvertSamplingRate(audioSamplingRate);
            FormatBitDepth = ConvertBitDepth(bitDepth);
            AudioChannelCount = (byte)(audioChannelCount - 1);
            StreamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
            MaxDataSize = VBanConst.MaxSampleCount * BytePerSample;
            BufferPacketCount = (int)ConvertBufferSize(bufferSize);
            EmitInterval = CalculateEmitInterval();
        }

        private FormatSamplingRate ConvertSamplingRate(int audioSamplingRate) => Enum.TryParse($"_{audioSamplingRate}Hz", true, out FormatSamplingRate result) ? result : FormatSamplingRate._24000Hz;

        private FormatBitDepth ConvertBitDepth(int bitDepth) => Enum.TryParse($"Int{bitDepth}", true, out FormatBitDepth result) ? result : FormatBitDepth.Int16;

        private BufferSize ConvertBufferSize(string bufferSize) => Enum.TryParse(bufferSize, true, out BufferSize result) ? result : BufferSize.Mediam;

        /// <summary>
        /// 送信インターバル時間を計算します。
        /// </summary>
        /// <returns></returns>
        private TimeSpan CalculateEmitInterval()
        {
            var packetCountsPerSec = (double)AudioSamplingRate / VBanConst.MaxSampleCount;
            var timerInterval = (int)(1000 / (packetCountsPerSec / BufferPacketCount));
            return TimeSpan.FromMilliseconds(timerInterval);
        }

        public enum BufferSize
        {
            Optimal = 2,
            Fast = 4,
            Mediam = 8,
            Slow = 16,
            VerySlow = 32
        }
    }
}
