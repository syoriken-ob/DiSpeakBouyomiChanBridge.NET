using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Timers;

using net.boilingwater.external.VBanProtocolEmitter.Const;
using net.boilingwater.external.VBanProtocolEmitter.Stricts;

namespace net.boilingwater.external.VBanProtocolEmitter
{
    public class VBanEmitter : IDisposable
    {
        public EmitterConfig Config { get; }

        private readonly BlockingCollection<byte[]> _emittablePackets;
        private readonly Thread _thread;
        private readonly UdpClient _udpClient;

        /* パケット生成 */
        private const int WaveFileHeaderDataLength = 44;
        private readonly int _maxDataSize;
        private readonly int _timerInterval;
        private uint PacketCounter = 0;

        public VBanEmitter(EmitterConfig config)
        {
            Config = config;
            _emittablePackets = new(new ConcurrentQueue<byte[]>(), 512);
            _udpClient = new();

            //1パケットで送信可能な最大Byteサイズ（256サンプル）
            _maxDataSize = VBanConst.MaxSampleCount * Config.BitPerSample;

            //タイマーに必要なクロック時間を計算
            var packetCountsPerSec = Config.AudioSamplingRate / _maxDataSize;
            _timerInterval = Math.Min(1000 / packetCountsPerSec, 10);

            _thread = new Thread(() =>
            {
                while (true)
                {
                    OnElapsed_EmitVBAN(null, null);
                    Thread.Sleep(_timerInterval);
                }
            })
            { IsBackground = true };
        }

        /// <summary>
        /// 送信を開始する
        /// </summary>
        public void Start()
        {
            _udpClient.Connect(Config.Host, Config.Port);
            _thread.Start();
        }

        /// <summary>
        /// 送信を終了する
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _thread?.Interrupt();
            _udpClient.Close();
            _udpClient.Dispose();
        }

        /// <summary>
        /// 送信する音声データを登録します
        /// </summary>
        /// <param name="audioBytes"></param>
        public void RegisterEmissionData(byte[] audioBytes)
        {
            foreach (var audioByte in audioBytes.Skip(WaveFileHeaderDataLength).Chunk(_maxDataSize))
            {
                if (audioByte.Length < _maxDataSize)
                {
                    var wapper = new byte[_maxDataSize];
                    audioByte.CopyTo(wapper, 0);
                    _emittablePackets.TryAdd(wapper, -1);
                }
                else
                {
                    _emittablePackets.TryAdd(audioByte, -1);
                }
            }
        }

        private void OnElapsed_EmitVBAN(object? sender, ElapsedEventArgs? e)
        {
            _emittablePackets.TryTake(out var data, 3);

            if (data == null || data.Length == 0)
            {
                data = new byte[_maxDataSize];
            }

            var sampleCount = CalculateSampleCount(data.Length);

            var header = CreateVBANHeader(sampleCount);

            SendPacket(header, data);

            PacketCounter++; //VBANHeaderで利用するカウンターのカウントアップ
            if (PacketCounter >= uint.MaxValue)
            {
                PacketCounter = 0U;
            }
        }

        /// <summary>
        /// 設定からバイト配列に含まれるサンプル数を計算します。
        /// </summary>
        /// <param name="length"></param>
        /// <returns>（0 = 1Sampleのため）-1補正したサンプル数</returns>
        private byte CalculateSampleCount(int length)
        {
            return (byte)((length / Config.BitPerSample) - 1);
        }

        /// <summary>
        /// 送信用のVBANProtocolHeader構造体を生成します。
        /// </summary>
        /// <param name="sampleCount"></param>
        /// <returns></returns>
        private VbanHeader CreateVBANHeader(byte sampleCount)
        {
            return new VbanHeader(
                Config.FormatSamplingRate,
                VbanHeader.SubProtocol.Audio,
                sampleCount,
                Config.AudioChannelCount,
                Config.FormatBitDepth,
                VbanHeader.Codec.PCM,
                Config.StreamName,
                PacketCounter
            );
        }

        private void SendPacket(VbanHeader header, byte[] data)
        {
            var emitData = new byte[VBanConst.HeaderSize + data.Length];
            Buffer.BlockCopy(header.Decode(), 0, emitData, 0, VBanConst.HeaderSize); //Headerのデータを送信用バイト配列にコピー(28byte)
            Buffer.BlockCopy(data, 0, emitData, VBanConst.HeaderSize, data.Length); //音声データを送信用バイト配列にコピー

            try
            {
                _udpClient.Send(emitData, emitData.Length); //UDPで送りつける
            }
            catch { /* エラーは握りつぶす（TODO:どうにかする） */ }
        }
    }
}
