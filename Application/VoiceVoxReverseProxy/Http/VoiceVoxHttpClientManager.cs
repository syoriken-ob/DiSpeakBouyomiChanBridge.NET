using System;
using System.Collections.Generic;
using System.Linq;

using net.boilingwater.BusinessLogic.VoiceVoxSpeakerMapping.Dto;
using net.boilingwater.BusinessLogic.VoiceVoxSpeakerMapping.Service;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Common.Utils;
using net.boilingwater.Framework.Core;
using net.boilingwater.Framework.Core.Extensions;
using net.boilingwater.Framework.Core.Interface;
using net.boilingwater.Framework.Core.Utils;

namespace net.boilingwater.Application.VoiceVoxReverseProxy.Http;

/// <summary>
/// VOICEVOX連携用HttpClientを管理するマネージャークラス
/// </summary>
public static class VoiceVoxHttpClientManager
{
    private static List<HttpClientForVoiceVoxBridge> HttpClientList { get; set; } = [];
    private static SimpleDic<HttpClientForVoiceVoxBridge> HttpClientDic { get; set; } = [];
    private static SimpleDic<SpeakerRemappingDto> SpeakerRemappingDic { get; set; } = [];

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
        speakers = [];
        HttpClientDic.Clear();

        var uniqueSpeakers = new HashSet<string>(128);
        foreach (HttpClientForVoiceVoxBridge client in HttpClientList)
        {
            IMultiDic result = client.SendVoiceVoxSpeakersRequest();
            if (!result.GetAsBoolean("valid"))
            {
                continue;
            }
            succeeded = true;
            IMultiList currentClientSpeakers = result.GetAsMultiList("speakers");
            foreach (MultiDic speaker in currentClientSpeakers.CastMulti<MultiDic>())//VOICEVOX話者ごと
            {
                foreach (MultiDic style in speaker!.GetAsMultiList("styles").CastMulti<MultiDic>())//話者のスタイルこと
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
                    style!["id"] = newId; //VOICEVOXのレスポンスは数値型

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
    /// VoiceVoxAPI[initialize_speaker]にリクエストを送信します。
    /// </summary>
    /// <param name="speaker">VoiceVox話者ID</param>
    /// <returns>正常にVoiceVox話者を初期化できたか</returns>
    public static bool SendVoiceVoxInitializeSpeakerRequest(string speaker)
    {
        (var speakerId, HttpClientForVoiceVoxBridge client) = FindActualVoiceVoxSpeakerIdAndHttpClient(speaker);
        return client.SendVoiceVoxInitializeSpeakerRequest(speakerId);
    }

    /// <summary>
    /// VoiceVoxAPI[audio_query]にリクエストを送信します。
    /// </summary>
    /// <param name="message">読み上げメッセージ</param>
    /// <param name="speaker">VoiceVox話者ID</param>
    /// <param name="audioQuery">VoiceVoxAPI[audio_query]で生成した音声合成パラメータ</param>
    /// <returns>取得できたかどうか</returns>
    public static bool SendVoiceVoxAudioQueryRequest(string message, string speaker, out IMultiDic audioQuery)
    {
        const string CacheKey = "VoiceVoxHttpClientManager#AudioQuery";
        var key = $"{CacheKey}#{speaker}#{message}";
        var cacheExpiration = TimeSpan.FromMinutes(Settings.AsDouble("VoiceVox.Response.AudioQuery.CacheExpiration.Minutes"));

        if (MemoryCacheUtil.TryGetCache(key, out audioQuery!))
        {
            MemoryCacheUtil.ExtendExpiration(key, cacheExpiration);
            return true;
        }

        (var speakerId, HttpClientForVoiceVoxBridge client) = FindActualVoiceVoxSpeakerIdAndHttpClient(speaker);

        var result = client.SendVoiceVoxAudioQueryRequest(message, speakerId, out audioQuery);

        if (result)
        {
            MemoryCacheUtil.RegisterCache(key, audioQuery, cacheExpiration);
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
        var cacheExpiration = TimeSpan.FromMinutes(Settings.AsDouble("VoiceVox.Response.Synthesis.CacheExpiration.Minutes"));

        if (MemoryCacheUtil.TryGetCache(key, out voice!))
        {
            MemoryCacheUtil.ExtendExpiration(key, cacheExpiration);
            return true;
        }

        (var speakerId, HttpClientForVoiceVoxBridge client) = FindActualVoiceVoxSpeakerIdAndHttpClient(speaker);

        var result = client.SendVoiceVoxSynthesisRequest(audioQuery, speakerId, out voice);

        if (result)
        {
            MemoryCacheUtil.RegisterCache(key, voice, cacheExpiration);
        }

        return result;
    }

    #region private

    /// <summary>
    /// VOICEVOX連携用のHttpClientを生成します。
    /// </summary>
    private static void CreateHttpClients()
    {
        IMultiDic mappingDic = Settings.AsMultiDic("Map.VoiceVox.Application.HostAndPort");

        foreach (var key in mappingDic.Keys)
        {
            HttpClientList.Add(new HttpClientForVoiceVoxBridge(key, mappingDic.GetAsInteger(key)));
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
        if (!HttpClientDic.TryGetValue(id, out HttpClientForVoiceVoxBridge? client) || client == null)
        {
            throw new ArgumentException("不正なVOICEVOX話者IDが指定されました。");
        }

        if (SpeakerRemappingDic.TryGetValue(id, out SpeakerRemappingDto dto))
        {
            return (dto.Id, client);
        }

        return (id, client);
    }

    #endregion private
}
