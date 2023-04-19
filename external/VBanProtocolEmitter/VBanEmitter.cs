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

            //1パケットで送信可能な最大Byteサイズ（256サンプル）
            _maxDataSize = VBanConst.MaxSampleCount * Config.BytePerSample;

            _emittablePackets = new(new ConcurrentQueue<byte[]>(), _maxDataSize);
            _udpClient = new();

            //タイマーに必要なクロック時間を計算
            var packetCountsPerSec = Config.AudioSamplingRate / VBanConst.MaxSampleCount;
            _timerInterval = 1000 / packetCountsPerSec;
            _thread = new Thread(() =>
            {
                DateTime startAt;
                double elapsedTime;
                while (true)
                {
                    startAt = DateTime.UtcNow;
                    elapsedTime = 0;
                    OnElapsed_EmitVBAN(null, null);
                    do
                    {
                        Thread.SpinWait(10);
                        elapsedTime = (DateTime.UtcNow - startAt).TotalMilliseconds;
                    } while (_timerInterval > elapsedTime);
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
                    Array.Fill<byte>(wapper, 0);
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
                Array.Fill<byte>(data, 0);
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
            return (byte)((length / Config.BytePerSample) - 1);
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
