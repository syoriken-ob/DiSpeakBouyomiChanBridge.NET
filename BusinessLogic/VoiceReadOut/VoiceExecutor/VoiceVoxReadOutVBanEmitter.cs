﻿using System;

using net.boilingwater.external.VBanProtocolEmitter;
using net.boilingwater.Framework.Common.Setting;

namespace net.boilingwater.BusinessLogic.VoiceReadOut.VoiceExecutor
{
    public class VoiceVoxReadOutVBanEmitter : VoiceVoxReadOutExecutor
    {
        private readonly VBanEmitter _vbanEmitter;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public VoiceVoxReadOutVBanEmitter()
        {
            var config = new EmitterConfig(
                host: Settings.AsString("VBAN.Emitter.Host"),
                port: Settings.AsInteger("VBAN.Emitter.Port"),
                audioSamplingRate: Settings.AsInteger("VBAN.Emitter.AudioSamplingRate"),
                bitDepth: Settings.AsInteger("VBAN.Emitter.BitDepth"),
                audioChannelCount: Settings.AsInteger("VBAN.Emitter.AudioChannelCount"),
                streamName: Settings.AsString("VBAN.Emitter.StreamName")
            );

            _vbanEmitter = new(config);
            _vbanEmitter.Start();
        }

        /// <summary>
        /// VoiceVoxで生成した音声データのバイト配列を再生キューに追加します。
        /// </summary>
        /// <param name="audioStreamByteArray"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void AddQueue(byte[] audioStreamByteArray)
        {
            if (audioStreamByteArray == null)
            {
                throw new ArgumentNullException(nameof(audioStreamByteArray));
            }
            _vbanEmitter.RegisterEmissionData(audioStreamByteArray);
        }
    }
}