using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using net.boilingwater.BusinessLogic.VoiceReadOut.Dto;
using net.boilingwater.BusinessLogic.VoiceReadOut.Service;
using net.boilingwater.BusinessLogic.VoiceReadOut.VoiceExecutor;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core;
using net.boilingwater.Framework.Core.Extensions;
using net.boilingwater.Framework.Core.Interface;
using net.boilingwater.Framework.Core.Logging;
using net.boilingwater.Framework.Core.Utils;

namespace net.boilingwater.BusinessLogic.VoiceReadout.HttpClients.Impl;

/// <summary>
/// VOICEVOX用の読み上げクライアント
/// </summary>
public class HttpClientForVoiceVox : HttpClientForReadOut
{
    private Task Task { get; init; }
    private BlockingCollection<MessageDto> ReceivedMessages { get; init; } = [];
    private IMultiDic RequestSetting { get; set; }
    private SimpleDic<string> VoiceVoxSpeakers { get; set; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public HttpClientForVoiceVox()
    {
        RequestSetting = VoiceVoxRequestService.CreateRequestSettingDic(
            Settings.AsString("VoiceVox.Application.Scheme"),
            Settings.AsString("VoiceVox.Application.Host"),
            Settings.AsInteger("VoiceVox.Application.Port")
        );

        VoiceVoxSpeakers = FetchEnableVoiceVoxSpeaker();
        InitializeVoiceVoxSpeaker();

        Task = Task.Factory.StartNew((obj) =>
        {
            foreach (MessageDto message in ReceivedMessages.GetConsumingEnumerable())
            {
                try
                {
                    foreach (InlineMessageDto inlineMessage in message.InlineMessages)
                    {
                        var speakerKey = inlineMessage.SpeakerKey.HasValue() ? inlineMessage.SpeakerKey : message.UserDefaultSpeakerKey;

                        if (!VoiceVoxSpeakers.ContainsKey(speakerKey))
                        {
                            //マッピングされた話者がいなかったら既定設定で読み上げ
                            ExecuteReadOut(inlineMessage.Message, Settings.AsString("VoiceVox.DefaultSpeaker"), "");
                            continue;
                        }

                        ExecuteReadOut(inlineMessage.Message, VoiceVoxSpeakers[speakerKey]!, speakerKey);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex);
                }
            }
            Log.Logger.Debug("Finished ReceivedMessages.GetConsumingEnumerable.");
            ReceivedMessages.Dispose();
        }, null, TaskCreationOptions.LongRunning);
    }

    /// <inheritdoc/>
    public override void ReadOut(MessageDto message)
    {
        if (ReceivedMessages.IsAddingCompleted || IsDisposed)
        {
            Log.Logger.WarnFormat("オブジェクトが破棄されているため、読み上げ処理が実行されませんでした。メッセージ：{0}",string.Join(' ', message.InlineMessages));
            return;
        }
        ReceivedMessages.Add(message);
    }

    /// <summary>
    /// VOICEVOXと通信して読み上げます。
    /// </summary>
    /// <param name="message">VOICEVOXに読み上げるメッセージ</param>
    private void ExecuteReadOut(string message, string speakerId, string speakerKey)
    {
        var retryCount = 0L;
        while (true)
        {
            IMultiDic audioQueryResult = VoiceVoxRequestService.SendVoiceVoxAudioQueryRequest(Client, RequestSetting, message, speakerId);
            if (audioQueryResult.ContainsKey("statusCode"))
            {
                HttpStatusCode statusCode = audioQueryResult.GetAsObject<string, HttpStatusCode>("statusCode");
                Log.Logger.Debug($"Send AudioQuery: {(int)statusCode}-{statusCode}");
            }

            if (!audioQueryResult.GetAsBoolean("valid"))
            {
                Log.Logger.Fatal($"Fail to Send Message to VoiceVox[audio_query]: {message}");
                if (!WaitRetry(retryCount++))
                {
                    return;
                }
                continue;
            }

            IMultiDic audioQuery = audioQueryResult.GetAsMultiDic("audioQuery");
            VoiceVoxRequestService.ReplaceAudioQueryJson(audioQuery, speakerKey);

            IMultiDic synthesisResult = VoiceVoxRequestService.SendVoiceVoxSynthesisRequest(Client, RequestSetting, audioQuery, speakerId);
            if (synthesisResult.ContainsKey("statusCode"))
            {
                HttpStatusCode statusCode = synthesisResult.GetAsObject<HttpStatusCode>("statusCode");
                Log.Logger.Debug($"Send Synthesis: {(int)statusCode}-{statusCode}");
            }
            if (!synthesisResult.GetAsBoolean("valid"))
            {
                Log.Logger.Fatal($"Fail to Send Message to VoiceVox[synthesis]: {message}");
                if (!WaitRetry(retryCount++))
                {
                    return;
                }
                continue;
            }

            Log.Logger.Info($"ReadOut Message: {message}");
            var voiceAudio = synthesisResult.GetAsObject<byte[]>("voice");
            if (voiceAudio != null)
            {
                VoiceVoxReadOutExecutor.Instance?.AddQueue(voiceAudio);
            }
            return;
        }
    }

    /// <summary>
    /// 設定値とVoiceVoxAPI[speakers]から利用可能な話者のマッピングを取得します。
    /// </summary>
    /// <returns></returns>
    private SimpleDic<string> FetchEnableVoiceVoxSpeaker()
    {
        var dic = new SimpleDic<string>();

        IMultiDic result = VoiceVoxRequestService.SendVoiceVoxSpeakersRequest(Client, RequestSetting);

        if (result.GetAsBoolean("valid"))
        {
            IMultiDic baseSpeakerDic = Settings.AsMultiDic("VoiceVox.InlineSpeakersMapping");
            var fetchedSpeakerList = result.GetAsMultiList("speakers")
                                           .Select(CastUtil.ToObject<MultiDic>)
                                           .Where(s => s != null).Cast<MultiDic>()
                                           .SelectMany(s => s!.GetAsMultiList("styles"))
                                           .Select(CastUtil.ToObject<MultiDic>)
                                           .Where(s => s != null).Cast<MultiDic>()
                                           .Select(s => s!.GetAsString("id"))
                                           .Intersect(baseSpeakerDic.Keys)
                                           .ToList();

            fetchedSpeakerList.ForEach(id => dic[baseSpeakerDic.GetAsString(id)] = id);
            dic.ForEach(pair => Log.Logger.Debug($"VoiceVox話者登録：{pair.Key}) => {pair.Value}"));
        }

        return dic;
    }

    /// <summary>
    /// VoiceVoxAPI[initialize_speaker]に通信し、話者の初期化を行います。
    /// </summary>
    /// <remarks>
    ///
    /// 既定話者のみ初期化を行います。
    /// </remarks>
    private void InitializeVoiceVoxSpeaker()
    {
        var defaultSpeakerId = Settings.AsString("VoiceVox.DefaultSpeaker");
        Log.Logger.Debug($"VoiceVox既定話者(id={defaultSpeakerId})を初期化します。");
        IMultiDic result = VoiceVoxRequestService.SendVoiceVoxInitializeSpeakerRequest(Client, RequestSetting, defaultSpeakerId);
        Log.Logger.Debug($"VoiceVox既定話者(id={defaultSpeakerId})の初期化に{(result.GetAsBoolean("valid") ? "成功" : "失敗")}しました。");
    }

    public override void Dispose()
    {
        lock (ReceivedMessages)
        {
            ReceivedMessages.CompleteAdding();
            while (ReceivedMessages.Count > 0)
            {
                ReceivedMessages.Take(); //中身を空にするまでTakeする
            }
            base.Dispose();
        }
    }
}
