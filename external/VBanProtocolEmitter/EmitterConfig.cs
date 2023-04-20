using System;

using net.boilingwater.external.VBanProtocolEmitter.Const;

using static net.boilingwater.external.VBanProtocolEmitter.Stricts.VbanHeader;

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
        }

        private FormatSamplingRate ConvertSamplingRate(int audioSamplingRate)
        {
            return Enum.TryParse<FormatSamplingRate>($"_{audioSamplingRate}Hz", true, out var result) ? result : FormatSamplingRate._24000Hz;
        }

        private FormatBitDepth ConvertBitDepth(int bitDepth)
        {
            return Enum.TryParse<FormatBitDepth>($"Int{bitDepth}", true, out var result) ? result : FormatBitDepth.Int16;
        }

        private BufferSize ConvertBufferSize(string bufferSize)
        {
            return Enum.TryParse<BufferSize>(bufferSize, true, out var result) ? result : BufferSize.Mediam;
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
