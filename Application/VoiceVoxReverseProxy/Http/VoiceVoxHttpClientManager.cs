using net.boilingwater.BusinessLogic.VoiceVoxSpeakerCache.Dto;
using net.boilingwater.BusinessLogic.VoiceVoxSpeakerCache.Service;
using net.boilingwater.Framework.Common;
using net.boilingwater.Framework.Common.Extensions;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Application.VoiceVoxReverseProxy.Http
{
    /// <summary>
    /// VOICEVOX連携用HttpClientを管理するマネージャークラス
    /// </summary>
    public static class VoiceVoxHttpClientManager
    {
        private static List<HttpClientForVoiceVoxBridge> HttpClientList { get; set; } = new();
        private static SimpleDic<HttpClientForVoiceVoxBridge> HttpClientDic { get; set; } = new();
        private static SimpleDic<SpeakerRemappingDto> SpeakerRemappingDic { get; set; } = new();

        /// <summary>
        /// 初期化処理
        /// </summary>
        public static void Initialize()
        {
            HttpClientList.Clear();

            CreateHttpClients();
            FetchSpeakerRemappingDic();
            FetchAllVoiceVoxSpeakers(out _);
        }

        /// <summary>
        /// 利用可能なすべてのVOICEVOX互換アプリケーションのVoiceVoxAPI[speakers]から利用可能な話者のリストを取得します。
        /// </summary>
        /// <param name="speakers">VoiceVoxAPI[speakers]のレスポンス</param>
        /// <returns>取得できたかどうか</returns>
        public static bool FetchAllVoiceVoxSpeakers(out MultiList speakers)
        {
            const string CacheKey = "VoiceVoxHttpClientManager#Speakers";

            if (MemoryCacheUtil.TryGetCache(CacheKey, out speakers!))
            {
                return true;
            }

            var succeeded = false;
            speakers = new MultiList();
            HttpClientDic.Clear();

            var uniqueSpeakers = new HashSet<string>(128);
            foreach (var client in HttpClientList)
            {
                var result = client.SendVoiceVoxSpeakersRequest();
                if (!result.GetAsBoolean("valid"))
                {
                    continue;
                }
                succeeded = true;
                var currentClientSpeakers = result.GetAsMultiList("speakers");
                foreach (var speaker in currentClientSpeakers.Cast<MultiDic?>())//VOICEVOX話者ごと
                {
                    foreach (var style in speaker!.GetAsMultiList("styles").Cast<MultiDic?>().Where(d => d is not null))//話者のスタイルこと
                    {
                        var id = style!.GetAsString("id");
                        if (uniqueSpeakers.Add(id))
                        {
                            HttpClientDic[id] = client;
                            continue;
                        }

                        var mappingKey = new SpeakerRemappingDto() { Guid = speaker.GetAsGuid("speaker_uuid"), Id = id };

                        if (SpeakerRemappingDic.ContainsValue(mappingKey))
                        {
                            var remappingKey = SpeakerRemappingDic.Where(p => p.Value.Equals(mappingKey)).First().Key;
                            style["id"] = CastUtil.ToInteger(remappingKey); //VOICEVOXのレスポンスは数値型
                            HttpClientDic[remappingKey] = client;
                            continue;
                        }

                        var newId = RandomUtil.CreateRandomNumber(9);
                        style["id"] = newId; //VOICEVOXのレスポンスは数値型

                        VoiceVoxSpeakerMappingService.InsertMapping(
                            speaker.GetAsGuid("speaker_uuid"),
                            id,
                            newId.ToString()
                        );

                        SpeakerRemappingDic[newId.ToString()] = mappingKey;
                        HttpClientDic[newId.ToString()] = client;
                    }
                }
                speakers.AddRange(currentClientSpeakers);
            }

            MemoryCacheUtil.RegisterCache(CacheKey, speakers, TimeSpan.FromHours(1));
            return succeeded;
        }

        /// <summary>
        /// VoiceVoxAPI[audio_query]にリクエストを送信します。
        /// </summary>
        /// <param name="message">読み上げメッセージ</param>
        /// <param name="speaker">VoiceVox話者ID</param>
        /// <param name="audioQuery">VoiceVoxAPI[audio_query]で生成した音声合成パラメータ</param>
        /// <returns>取得できたかどうか</returns>
        public static bool SendVoiceVoxAudioQueryRequest(string message, string speaker, out MultiDic audioQuery)
        {
            const string CacheKey = "VoiceVoxHttpClientManager#AudioQuery";
            var key = $"{CacheKey}#{speaker}#{message}";
            if (MemoryCacheUtil.TryGetCache(key, out audioQuery!))
            {
                return true;
            }

            var (speakerId, client) = FindActualVoiceVoxSpeakerIdAndHttpClient(speaker);

            var result = client.SendVoiceVoxAudioQueryRequest(message, speakerId, out audioQuery);

            if (result)
            {
                MemoryCacheUtil.RegisterCache(key, audioQuery, TimeSpan.FromHours(1));
            }

            return result;
        }

        /// <summary>
        /// VoiceVoxAPI[synthesis]にリクエストを送信します。
        /// </summary>
        /// <param name="audioQuery">VoiceVoxAPI[audio_query]で生成した音声合成パラメータ</param>
        /// <param name="speaker">VoiceVox話者ID</param>
        /// <param name="voice">wave形式の音声データ</param>
        /// <returns>取得できたかどうか</returns>
        public static bool SendVoiceVoxSynthesisRequest(MultiDic audioQuery, string speaker, out byte[] voice)
        {
            const string CacheKey = "VoiceVoxHttpClientManager#Synthesis";
            var key = $"{CacheKey}#{speaker}#{audioQuery.GetAsString("kana")}";
            if (MemoryCacheUtil.TryGetCache(key, out voice!))
            {
                return true;
            }

            var (speakerId, client) = FindActualVoiceVoxSpeakerIdAndHttpClient(speaker);

            var result = client.SendVoiceVoxSynthesisRequest(audioQuery, speakerId, out voice);

            if (result)
            {
                MemoryCacheUtil.RegisterCache(key, voice, TimeSpan.FromMinutes(10));
            }

            return result;
        }

        #region private

        /// <summary>
        /// VOICEVOX連携用のHttpClientを生成します。
        /// </summary>
        private static void CreateHttpClients()
        {
            var d = Settings.AsStringList("Map.VoiceVox.Application.HostAndPort");

            foreach (var p in d)
            {
                var pair = p.Split(";");
                if (pair.Length <= 1)
                {
                    continue;
                }
                HttpClientList.Add(new HttpClientForVoiceVoxBridge(CastUtil.ToString(pair[0]), CastUtil.ToInteger(pair[1])));
            }
        }

        /// <summary>
        /// VOICEVOX連携用の話者マッピングを生成します。
        /// </summary>
        private static void FetchSpeakerRemappingDic()
        {
            SpeakerRemappingDic.Clear();
            VoiceVoxSpeakerMappingService.GetMapping()
                                         .ForEach(pair => SpeakerRemappingDic.Add(pair.Key, pair.Value));
        }

        /// <summary>
        /// 生成したVoiceVox話者IDから、実際のVoiceVox話者IDと通信に利用するための<see cref="HttpClientForVoiceVoxBridge"/>を取得します。
        /// </summary>
        /// <param name="id">BouyomiChanBrideから送信された話者ID</param>
        /// <returns></returns>
        private static (string speakerId, HttpClientForVoiceVoxBridge client) FindActualVoiceVoxSpeakerIdAndHttpClient(string id)
        {
            if (!HttpClientDic.ContainsKey(id) || HttpClientDic[id] == null)
            {
                throw new ArgumentException("不正なVOICEVOX話者IDが指定されました。");
            }

            if (SpeakerRemappingDic.ContainsKey(id))
            {
                var key = SpeakerRemappingDic[id];
                return (key.Id, HttpClientDic[id]!);
            }

            return (id, HttpClientDic[id]!);
        }

        #endregion private
    }
}
