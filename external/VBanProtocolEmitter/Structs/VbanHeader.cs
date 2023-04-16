namespace net.boilingwater.external.VBanProtocolEmitter.Stricts
{
    public struct VbanHeader
    {
        private readonly char[] vban = { 'V', 'B', 'A', 'N' };

        private readonly FormatSamplingRate samplingRate;
        private readonly SubProtocol subProtocol;
        private readonly byte sampleCount;
        private readonly byte channel;
        private readonly FormatBitDepth bitDepth;
        private readonly Codec codec;
        private readonly char[] streamName;
        private readonly uint counter;

        public VbanHeader(FormatSamplingRate samplingRate, SubProtocol subProtocol, byte sampleCount, byte channel, FormatBitDepth bitDepth, Codec codec, string streamName, uint counter)
        {
            this.samplingRate = samplingRate;
            this.subProtocol = subProtocol;
            this.sampleCount = sampleCount;
            this.channel = channel;
            this.bitDepth = bitDepth;
            this.codec = codec;
            this.streamName = streamName.ToCharArray();
            this.counter = counter;
        }

        public byte[] Decode()
        {
            var output = new byte[28];
            //header.vban START
            for (var i = 0; i < 4; i++)
            {
                output[i] = (byte)vban[i];
            }
            //header.vban END

            //header.format_SR START
            output[4] = (byte)((uint)subProtocol | (uint)samplingRate);
            //header.format_SR END

            //header.format_nbs Start
            output[5] = sampleCount;
            //header.format_nbs End

            //header.format_noc Start
            output[6] = channel;
            //header.format_noc End

            //header.format_bit Start
            output[7] = (byte)((uint)codec | (uint)bitDepth);
            //header.format_bit End

            //header.streamname Start
            for (var i = 0; i < 16; i++)
            {
                output[i + 8] = i < streamName.Length ? (byte)streamName[i] : (byte)0;
            }
            //header.streamname End

            //header.framecounter Start
            for (var i = 0; i < 4; i++)
            {
                output[i + 24] = (byte)(counter >> (3 - i) & 255);
            }
            //header.framecounter End

            return output;
        }

        public enum FormatSamplingRate
        {
            _6000Hz = 0,
            _12000Hz,
            _24000Hz,
            _48000Hz,
            _96000Hz,
            _192000Hz,
            _384000Hz,
            _8000Hz,
            _16000Hz,
            _32000Hz,
            _64000Hz,
            _128000Hz,
            _256000Hz,
            _512000Hz,
            _11025Hz,
            _22050Hz,
            _44100Hz,
            _88200Hz,
            _176400Hz,
            _352800Hz,
            _705600Hz,
        }

        public enum SubProtocol
        {
            Audio = 0x00,
            Serial = 0x20,
            Txt = 0x40,
            Service = 0x60
        }

        public enum FormatBitDepth
        {
            Byte8 = 0x00,
            Int16 = 0x01,
            Int24 = 0x02,
            Int32 = 0x03,
            Float32 = 0x04,
            Float64 = 0x05,
            _12Bits = 0x06,
            _10Bits = 0x07,
        }

        public enum Codec
        {
            PCM = 0x00
        }
    }
}
