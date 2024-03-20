using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

using net.boilingwater.external.VBanProtocolEmitter.Structs;
using net.boilingwater.Framework.Core.Timers;

namespace net.boilingwater.external.VBanProtocolEmitter;

internal class UdpEmitter : HighResolutionTimer
{
    private readonly BlockingCollection<VBanPacket> _vbanPacketsBuffers;

    private readonly EmitterConfig _config;

    private readonly UdpClient _udpClient;

    /// <summary>
    ///
    /// </summary>
    /// <param name="config"></param>
    public UdpEmitter(EmitterConfig config) : base(config.EmitInterval)
    {
        _config = config;

        _vbanPacketsBuffers = new(new ConcurrentQueue<VBanPacket>());
        _udpClient = new();

        this.Elapsed += OnElapsed_EmitVBan;
    }

    /// <summary>
    /// 送信を開始する
    /// </summary>
    public void StartEmitting()
    {
        _udpClient.Connect(_config.Host, _config.Port);
        Start(DateTime.UtcNow);
    }

    /// <summary>
    /// 送信用VBANパケットを登録します。
    /// </summary>
    /// <param name="packet"></param>
    public void RegisterVBanPacket(VBanPacket packet) => _vbanPacketsBuffers.TryAdd(packet, -1);

    /// <summary>
    /// VBANパケットをUDPで送信します。
    /// </summary>
    /// <param name="packet"></param>
    private void SendPacket(VBanPacket packet)
    {
        try
        {
            var emitData = packet.GetBytes();
            _udpClient.Send(emitData, emitData.Length); //UDPで送りつける
        }
        catch { /* エラーは握りつぶす（TODO:どうにかする） */ }
    }

    private void OnElapsed_EmitVBan(object? sender, EventArgs? e)
    {
        lock (_vbanPacketsBuffers)
        {
            for (var i = 0; i < _config.BufferPacketCount; i++)
            {
                if (!_vbanPacketsBuffers.TryTake(out VBanPacket? packet, 2))
                {
                    packet = new VBanPacket(_config);
                }

                SendPacket(packet);
            }
        }
    }

    public override void Dispose()
    {
        try
        {
            base.Dispose();
            _udpClient.Dispose();
            _udpClient.Close();
            lock (_vbanPacketsBuffers)
            {
                _vbanPacketsBuffers.Dispose();
            }
        }
        catch { }
    }
}
