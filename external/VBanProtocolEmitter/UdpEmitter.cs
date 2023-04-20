using System.Collections.Concurrent;
using System.Net.Sockets;

using System.Timers;

using net.boilingwater.external.VBanProtocolEmitter.Const;
using net.boilingwater.external.VBanProtocolEmitter.Structs;

namespace net.boilingwater.external.VBanProtocolEmitter
{
    internal class UdpEmitter : Timer
    {
        private readonly BlockingCollection<VbanPacket> _vbanPacketsBuffers;

        private readonly EmitterConfig _config;

        private readonly UdpClient _udpClient;

        /// <summary>
        ///
        /// </summary>
        /// <param name="config"></param>
        public UdpEmitter(EmitterConfig config)
        {
            _config = config;

            _vbanPacketsBuffers = new(new ConcurrentQueue<VbanPacket>());
            _udpClient = new();

            //タイマーのインターバル時間を計算
            var packetCountsPerSec = (double)_config.AudioSamplingRate / VBanConst.MaxSampleCount;
            var timerInterval = (int)(1000 / (packetCountsPerSec / _config.BufferPacketCount));
            this.Interval = timerInterval;
            this.Elapsed += OnElapsed_EmitVBAN;
        }

        /// <summary>
        /// 送信を開始する
        /// </summary>
        public void StartEmittion()
        {
            _udpClient.Connect(_config.Host, _config.Port);
            Start();
        }

        /// <summary>
        /// 送信用VBANパケットを登録します。
        /// </summary>
        /// <param name="packet"></param>
        public void RegisterVbanPacket(VbanPacket packet)
        {
            _vbanPacketsBuffers.TryAdd(packet, -1);
        }

        /// <summary>
        /// VBANパケットをUDPで送信します。
        /// </summary>
        /// <param name="packet"></param>
        private void SendPacket(VbanPacket packet)
        {
            try
            {
                var emitData = packet.GetBytes();
                _udpClient.Send(emitData, emitData.Length); //UDPで送りつける
            }
            catch { /* エラーは握りつぶす（TODO:どうにかする） */ }
        }

        private void OnElapsed_EmitVBAN(object? sender, ElapsedEventArgs? e)
        {
            lock (this.SynchronizingObject ?? new object())
            {
                for (var i = 0; i < _config.BufferPacketCount; i++)
                {
                    if (!_vbanPacketsBuffers.TryTake(out var packet, 2))
                    {
                        packet = new VbanPacket(_config);
                    }

                    SendPacket(packet);
                }
            }
        }
    }
}
