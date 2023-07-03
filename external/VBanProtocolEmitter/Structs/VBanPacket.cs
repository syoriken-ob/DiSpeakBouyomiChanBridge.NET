using System;

using net.boilingwater.external.VBanProtocolEmitter.Const;
using net.boilingwater.external.VBanProtocolEmitter.Stricts;

namespace net.boilingwater.external.VBanProtocolEmitter.Structs
{
    /// <summary>
    /// VBANパケットの構造体
    /// </summary>
    internal class VBanPacket
    {
        public VBanHeader Header { get; private set; }

        public byte[] Data { get; private set; }

        ///<summary>
        /// 送信用設定からデータ部が空のVBANパケットを作成します。
        ///</summary>
        /// <param name="config">VBANProtocol送信用設定</param>
        public VBanPacket(EmitterConfig config)
        {
            Data = new byte[config.MaxDataSize];
            Header = CreateVBanHeader(config, Data.Length);
        }

        ///<summary>
        /// 送信用設定とオーディオデータ配列からVBANパケットを作成します。
        ///</summary>
        /// <param name="config">VBANProtocol送信用設定</param>
        /// <param name="audioBytes">送信オーディオデータ配列</param>
        public VBanPacket(EmitterConfig config, byte[] audioBytes)
        {
            Data = new byte[config.MaxDataSize];
            Buffer.BlockCopy(audioBytes, 0, Data, 0, audioBytes.Length);
            Header = CreateVBanHeader(config, Data.Length);
        }

        /// <summary>
        /// パケットをネットワーク送信可能なバイト配列に変換して取得します。
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            var emitData = new byte[VBanConst.HeaderSize + Data.Length];
            Buffer.BlockCopy(Header.Decode(), 0, emitData, 0, VBanConst.HeaderSize); //Headerのデータを送信用バイト配列にコピー(28byte)
            Buffer.BlockCopy(Data, 0, emitData, VBanConst.HeaderSize, Data.Length); //音声データを送信用バイト配列にコピー
            return emitData;
        }

        #region StaticMember

        /// <summary>
        /// パケットカウンター[連番]
        /// </summary>
        public static uint PacketCounter { get; private set; } = 0;

        /// <summary>
        /// 送信用のVBANProtocolHeader構造体を生成します。
        /// </summary>
        /// <param name="config">VBANProtocol送信用設定</param>
        /// <param name="dataLength">データ部パケット長さ</param>
        /// <returns></returns>
        private static VBanHeader CreateVBanHeader(EmitterConfig config, int dataLength)
        {
            return new VBanHeader(
                config.FormatSamplingRate,
                VBanHeader.SubProtocol.Audio,
                CalculateSampleCount(config, dataLength),
                config.AudioChannelCount,
                config.FormatBitDepth,
                VBanHeader.Codec.PCM,
                config.StreamName,
                GetCounterValue()
            );
        }

        /// <summary>
        /// 設定からバイト配列に含まれるサンプル数を計算します。
        /// </summary>
        /// <param name="length"></param>
        /// <returns>（0 = 1Sampleのため）-1補正したサンプル数</returns>
        private static byte CalculateSampleCount(EmitterConfig config, int length) => (byte)((length / config.BytePerSample) - 1);

        /// <summary>
        /// パケットカウンターの現在値を取得します。
        /// </summary>
        /// <returns></returns>
        /// <remarks>取得時に値をインクリメントします。</remarks>
        public static uint GetCounterValue() => unchecked(PacketCounter++);

        #endregion StaticMember
    }
}
