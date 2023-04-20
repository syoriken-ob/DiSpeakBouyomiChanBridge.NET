using System;

using static net.boilingwater.external.VBanProtocolEmitter.Stricts.VbanHeader;

namespace net.boilingwater.external.VBanProtocolEmitter
{
    public class EmitterConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int AudioSamplingRate { get; set; }
        public int BytePerSample { get; set; }
        public FormatSamplingRate FormatSamplingRate { get; set; }
        public FormatBitDepth FormatBitDepth { get; set; }
        public byte AudioChannelCount { get; set; }
        public string StreamName { get; set; }

        public EmitterConfig(string host, int port, int audioSamplingRate, int bitDepth, int audioChannelCount, string streamName)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            AudioSamplingRate = audioSamplingRate;
            BytePerSample = bitDepth / 8;
            FormatSamplingRate = ConvertSamplingRate(audioSamplingRate);
            FormatBitDepth = ConvertBitDepth(bitDepth);
            AudioChannelCount = (byte)(audioChannelCount - 1);
            StreamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
        }

        private FormatSamplingRate ConvertSamplingRate(int audioSamplingRate)
        {
            return Enum.TryParse<FormatSamplingRate>($"_{audioSamplingRate}Hz", true, out var result) ? result : FormatSamplingRate._24000Hz;
        }

        private FormatBitDepth ConvertBitDepth(int bitDepth)
        {
            return Enum.TryParse<FormatBitDepth>($"Int{bitDepth}", true, out var result) ? result : FormatBitDepth.Int16;
        }
    }
}
