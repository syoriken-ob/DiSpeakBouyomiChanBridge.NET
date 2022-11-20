using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;

using net.boilingwater.BusinessLogic.VoiceReadOut.Httpclients.Service;
using net.boilingwater.BusinessLogic.VoiceReadOut.VoiceExecutor;
using net.boilingwater.Framework.Common;
using net.boilingwater.Framework.Common.Extensions;
using net.boilingwater.Framework.Common.Logging;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.BusinessLogic.VoiceReadout.HttpClients.Impl
{
    /// <summary>
    /// VOICEVOX用の読み上げクライアント
    /// </summary>
    public class HttpClientForVoiceVox : HttpClientForReadOut
    {
        private static Regex SpeakerRegex { get; set; } = new Regex(@"^(?<speaker_id>\w{1,2})\)", RegexOptions.Compiled);
        private readonly Thread _thread;
        private readonly BlockingCollection<string> _receivedMessages = new();
        private MultiDic RequestSetting { get; set; }
        private SimpleDic<string> VoiceVoxSpeakers { get; set; }

        public HttpClientForVoiceVox()
        {
            RequestSetting = VoiceVoxRequestService.CreateRequestSettingDic(
                Settings.AsString("VoiceVox.Application.Scheme"),
                Settings.AsString("VoiceVox.Application.Host"),
                Settings.AsInteger("VoiceVox.Application.Port")
            );

            VoiceVoxSpeakers = FetchEnableVoiceVoxSpeaker();
            InitializeVoiceVoxSpeaker();

            _thread = new Thread(() =>
            {
                foreach (var message in _receivedMessages.GetConsumingEnumerable())
                {
                    try
                    {
                        var tempMessage = message;

                        foreach (var splittedMessage in tempMessage.Split(new[] { '\n', '。' }))
                        {
                            var trimmed = splittedMessage.Trim();
                            if (trimmed.HasValue())
                            {
                                ExecuteReadOut(trimmed);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex);
                    }
                }
            })
            {
                IsBackground = true
            };
            _thread.Start();
        }

        /// <summary>
        /// メッセージを読み上げます
        /// <para>読み上げ処理に時間がかかるため、受付以降のVOICEVOXとの通信・メッセージ生成処理・再生処理は別スレッドで実行します。</para>
        /// </summary>
        /// <param name="text">読み上げたいテキスト</param>
        public override void ReadOut(string text)
        {
            var message = text.Trim();
            if (message.HasValue())
            {
                _receivedMessages.Add(message);
            }
        }

        /// <summary>
        /// VOICEVOXと通信して読み上げます。
        /// </summary>
        /// <param name="message">VOICEVOXに読み上げるメッセージ</param>
        private void ExecuteReadOut(string message)
        {
            var retryCount = 0L;
            while (true)
            {
                var speaker = ExtractVoiceVoxSpeaker(ref message);

                var audioQueryResult = VoiceVoxRequestService.SendVoiceVoxAudioQueryRequest(Client, RequestSetting, message, speaker);
                if (audioQueryResult.ContainsKey("statusCode"))
                {
                    var statusCode = audioQueryResult.GetAsObject<HttpStatusCode>("statusCode");
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

                var audioQuery = audioQueryResult.GetAsMultiDic("audioQuery");
                VoiceVoxRequestService.ReplaceAudioQueryJson(audioQuery);

                var synthesisResult = VoiceVoxRequestService.SendVoiceVoxSynthesisRequest(Client, RequestSetting, audioQuery, speaker);
                if (synthesisResult.ContainsKey("statusCode"))
                {
                    var statusCode = synthesisResult.GetAsObject<HttpStatusCode>("statusCode");
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
                    VoiceVoxReadOutAudioPlayExecutor.Instance.AddQueue(voiceAudio);
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

            var result = VoiceVoxRequestService.SendVoiceVoxSpeakersRequest(Client, RequestSetting);

            if (result.GetAsBoolean("valid"))
            {
                var baseSpeakerDic = Settings.AsMultiDic("VoiceVox.InlineSpeakersMapping");
                var fetchedSpeakerList = result.GetAsMultiList("speakers")
                                               .Select(s => CastUtil.ToObject<MultiDic>(s))
                                               .Where(s => s != null)
                                               .SelectMany(s => s!.GetAsMultiList("styles"))
                                               .Select(s => CastUtil.ToObject<MultiDic>(s))
                                               .Where(s => s != null)
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
        private void InitializeVoiceVoxSpeaker()
        {
            foreach (var pair in VoiceVoxSpeakers)
            {
                Log.Logger.Debug($"VoiceVox話者：{pair.Key}を初期化します。");
                var result = VoiceVoxRequestService.SendVoiceVoxInitializeSpeakerRequest(Client, RequestSetting, CastUtil.ToString(pair.Value));
                Log.Logger.Debug($"VoiceVox話者：{pair.Key}の初期化に{(result.GetAsBoolean("valid") ? "成功" : "失敗")}しました。");
            }
        }

        /// <summary>
        /// メッセージ中のVoiceVox話者を抽出します。
        /// </summary>
        /// <param name="message">読み上げメッセージ</param>
        /// <returns>VoiceVox話者ID</returns>
        private string ExtractVoiceVoxSpeaker(ref string message)
        {
            var match = SpeakerRegex.Match(message);
            if (!match.Success)
            {
                return Settings.AsString("VoiceVox.DefaultSpeaker");
            }

            var speakerId = match.Groups["speaker_id"];

            if (speakerId == null)
            {
                return Settings.AsString("VoiceVox.DefaultSpeaker");
            }

            if (!VoiceVoxSpeakers.ContainsKey(speakerId.Value))
            {
                return Settings.AsString("VoiceVox.DefaultSpeaker");
            }

            message = message.Replace(match.Value, "");
            return VoiceVoxSpeakers[speakerId.Value] ?? Settings.AsString("VoiceVox.DefaultSpeaker");
        }
    }
}
