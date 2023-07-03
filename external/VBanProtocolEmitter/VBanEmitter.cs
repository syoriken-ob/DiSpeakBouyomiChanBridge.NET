using System;
using System.Linq;

using net.boilingwater.external.VBanProtocolEmitter.Structs;

namespace net.boilingwater.external.VBanProtocolEmitter
{
    public class VBanEmitter : IDisposable
    {
        public EmitterConfig Config { get; }

        private readonly UdpEmitter _emitter;

        /* パケット生成 */
        private const int WaveFileHeaderDataLength = 44;

        public VBanEmitter(EmitterConfig config)
        {
            Config = config;
            _emitter = new(Config);
        }

        /// <summary>
        /// 送信を開始する
        /// </summary>
        public void Start() => _emitter.StartEmitting();

        /// <summary>
        /// 送信を終了する
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _emitter.Dispose();
        }

        /// <summary>
        /// 送信する音声データを登録します
        /// </summary>
        /// <param name="audioBytes"></param>
        public void RegisterEmittingData(byte[] audioBytes)
        {
            var arr = audioBytes.Skip(WaveFileHeaderDataLength).Chunk(Config.MaxDataSize).ToArray();
            foreach (var audioByte in arr)
            {
                _emitter.RegisterVBanPacket(new VBanPacket(Config, audioByte));
            }
        }
    }
}
