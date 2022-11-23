using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using net.boilingwater.BusinessLogic.VoiceReadOut.Service;
using net.boilingwater.Framework.Common;
using net.boilingwater.Framework.Common.Http;
using net.boilingwater.Framework.Common.Logging;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Application.VoiceVoxReverseProxy.Http
{
    /// <summary>
    /// VoiceVoxと各種通信を行うHttpClient
    /// </summary>
    public class HttpClientForVoiceVoxBridge : AbstractHttpClient
    {
        /// <summary>
        /// 利用可能なVOICEVOX話者IDのリスト
        /// </summary>
        public List<string> Speakers { get; private set; }

        /// <summary>
        /// <see cref="VoiceVoxRequestService.CreateRequestSettingDic(string, string, int)"/>で生成した通信用共通設定辞書
        /// </summary>
        private MultiDic RequestSetting { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="host">通信先のホスト名</param>
        /// <param name="port">通信先のポート番号</param>
        public HttpClientForVoiceVoxBridge(string host, int port) : base()
        {
            RequestSetting = VoiceVoxRequestService.CreateRequestSettingDic(
                Settings.AsString("VoiceVox.Application.Scheme"),
                host,
                port
            );

            Speakers = FetchEnableVoiceVoxSpeakers();
            //InitializeVoiceVoxSpeaker();
        }

        /// <summary>
        /// VoiceVoxAPI[speakers]から利用可能な話者のリストを取得します。
        /// </summary>
        /// <returns></returns>
        public MultiDic SendVoiceVoxSpeakersRequest() => VoiceVoxRequestService.SendVoiceVoxSpeakersRequest(Client, RequestSetting);

        /// <summary>
        /// VoiceVoxAPI[initialize_speaker]にリクエストを送信します。
        /// </summary>
        /// <param name="speaker">VoiceVox話者ID</param>
        /// <returns>正常にVoiceVox話者を初期化できたか</returns>
        public bool SendVoiceVoxInitializeSpeakerRequest(string speaker)
        {
            var result = VoiceVoxRequestService.SendVoiceVoxInitializeSpeakerRequest(Client, RequestSetting, speaker);
            if (result.ContainsKey("statusCode"))
            {
                var statusCode = result.GetAsObject<HttpStatusCode>("statusCode");
                Log.Logger.Debug(message: $"Send Initialize Speaker: {(int)statusCode}-{statusCode}");
            }
            if (!result.GetAsBoolean("valid"))
            {
                Log.Logger.Fatal($"Fail to Send Request to VoiceVox[initialize_speaker]: ID {speaker}");
            }
            return result.GetAsBoolean("valid");
        }

        /// <summary>
        /// VoiceVoxAPI[audio_query]にリクエストを送信します。
        /// </summary>
        /// <param name="message">読み上げメッセージ</param>
        /// <param name="speaker">VoiceVox話者ID</param>
        /// <param name="audioQuery">VoiceVoxAPI[audio_query]で生成した音声合成パラメータ</param>
        /// <returns>取得できたかどうか</returns>
        public bool SendVoiceVoxAudioQueryRequest(string message, string speaker, out MultiDic audioQuery)
        {
            var retryCount = 0L;
            while (true)
            {
                var audioQueryResult = VoiceVoxRequestService.SendVoiceVoxAudioQueryRequest(Client, RequestSetting, message, speaker);
                if (audioQueryResult.ContainsKey("statusCode"))
                {
                    var statusCode = audioQueryResult.GetAsObject<HttpStatusCode>("statusCode");
                    Log.Logger.Debug(message: $"Send AudioQuery: {(int)statusCode}-{statusCode}");
                }
                if (!audioQueryResult.GetAsBoolean("valid"))
                {
                    Log.Logger.Fatal($"Fail to Send Message to VoiceVox[audio_query]: {message}");
                    if (!WaitRetry(retryCount++))
                    {
                        audioQuery = new MultiDic();
                        return false;
                    }
                    continue;
                }
                audioQuery = audioQueryResult.GetAsMultiDic("audioQuery");
                return true;
            }
        }

        /// <summary>
        /// VoiceVoxAPI[synthesis]にリクエストを送信します。
        /// </summary>
        /// <param name="audioQuery">VoiceVoxAPI[audio_query]で生成した音声合成パラメータ</param>
        /// <param name="speaker">VoiceVox話者ID</param>
        /// <param name="voice">wave形式の音声データ</param>
        /// <returns>取得できたかどうか</returns>
        public bool SendVoiceVoxSynthesisRequest(MultiDic audioQuery, string speaker, out byte[] voice)
        {
            var retryCount = 0L;
            while (true)
            {
                var synthesisResult = VoiceVoxRequestService.SendVoiceVoxSynthesisRequest(Client, RequestSetting, audioQuery, speaker);
                if (synthesisResult.ContainsKey("statusCode"))
                {
                    var statusCode = synthesisResult.GetAsObject<HttpStatusCode>("statusCode");
                    Log.Logger.Debug($"Send Synthesis: {(int)statusCode}-{statusCode}");
                }
                if (!synthesisResult.GetAsBoolean("valid"))
                {
                    Log.Logger.Fatal($"Fail to Send Message to VoiceVox[synthesis]: {audioQuery.GetAsString("kana")}");
                    if (!WaitRetry(retryCount++))
                    {
                        voice = Array.Empty<byte>();
                        return false;
                    }
                    continue;
                }

                voice = synthesisResult.GetAsObject<byte[]>("voice")!;
                return true;
            }
        }

        #region private

        /// <summary>
        /// VoiceVoxAPI[speakers]から利用可能な話者のリストを取得し、キャッシュします。
        /// </summary>
        /// <returns></returns>
        private List<string> FetchEnableVoiceVoxSpeakers()
        {
            var result = SendVoiceVoxSpeakersRequest();

            if (result.GetAsBoolean("valid"))
            {
                var speakers = result.GetAsMultiList("speakers")
                                 .Select(s => CastUtil.ToObject<MultiDic>(s))
                                 .Where(s => s != null)
                                 .SelectMany(s => s!.GetAsMultiList("styles"))
                                 .Select(s => CastUtil.ToObject<MultiDic>(s))
                                 .Where(s => s != null)
                                 .Select(s => s!.GetAsString("id"))
                                 .ToList();

                speakers.ForEach(pair => Log.Logger.Debug($"話者登録：{pair}"));
                return speakers;
            }
            return new List<string>(0);
        }

        /// <summary>
        /// VoiceVoxAPI[initialize_speaker]に通信し、話者の初期化を行います。
        /// </summary>
        private void InitializeVoiceVoxSpeaker()
        {
            foreach (var id in Speakers)
            {
                Log.Logger.Debug($"VoiceVox話者：{id}を初期化します。");
                var result = VoiceVoxRequestService.SendVoiceVoxInitializeSpeakerRequest(Client, RequestSetting, CastUtil.ToString(id));
                Log.Logger.Debug($"VoiceVox話者：{id}の初期化に{(result.GetAsBoolean("valid") ? "成功" : "失敗")}しました。");
            }
        }

        #endregion private
    }
}
