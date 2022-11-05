using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using net.boilingwater.Application.Common;
using net.boilingwater.Application.Common.Extensions;
using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Setting;
using net.boilingwater.Application.Common.Utils;
using net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.MessageReplacer.Service;
using net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.VoiceReadOut.VoiceExecutor;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.VoiceReadout.HttpClients.Impl
{
    /// <summary>
    /// VOICEVOX用の読み上げクライアント
    /// </summary>
    public class HttpClientForVoiceVox : HttpClientForReadOut
    {
        private static Regex SpeakerRegex { get; set; } = new Regex(@"^(?<speaker_id>\w{1,2})\)", RegexOptions.Compiled);
        private readonly Thread _thread;
        private readonly BlockingCollection<string> _receivedMessages = new();
        private SimpleDic<string> VoiceVoxSpeakers { get; set; }

        public HttpClientForVoiceVox()
        {
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
                var speaker = ExtactVoiceVoxSpeaker(ref message);

                var audioQueryResult = SendVoiceVoxAudioQueryRequst(message, speaker);
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
                ReplaceAudioQueryJson(audioQuery);

                var synthesisResult = SendVoiceVoxSynthesisRequest(audioQuery, speaker);
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

            var result = SendVoiceVoxSpeakersRequst();

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
                var result = SendVoiceVoxInitializeSpeakerRequst(CastUtil.ToString(pair.Value));
                Log.Logger.Debug($"VoiceVox話者：{pair.Key}の初期化に{(result.GetAsBoolean("valid") ? "成功" : "失敗")}しました。");
            }
        }

        /// <summary>
        /// メッセージ中のVoiceVox話者を抽出します。
        /// </summary>
        /// <param name="message">読み上げメッセージ</param>
        /// <returns>VoiceVox話者ID</returns>
        private string ExtactVoiceVoxSpeaker(ref string message)
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

        #region SendRequest

        /// <summary>
        /// VoiceVoxAPI[speakers]にリクエストを送信します。
        /// </summary>
        /// <returns></returns>
        private MultiDic SendVoiceVoxSpeakersRequst()
        {
            var result = new MultiDic();
            try
            {
                using (var speakersResponse = Client.Send(CreateVoiceVoxSpeakersHttpRequest()))
                {
                    result["valid"] = speakersResponse.IsSuccessStatusCode;
                    result["statusCode"] = speakersResponse.StatusCode;
                    if (!result.GetAsBoolean("valid"))
                    {
                        return result;
                    }
                    using var reader = new StreamReader(speakersResponse.Content.ReadAsStream(), Encoding.UTF8);
                    result["speakers"] = SerializeUtil.JsonToMultiList(reader.ReadToEnd());
                }
                return result;
            }
            catch (WebException ex)
            {
                result["valid"] = false;
                if (ex.Response == null)
                {
                    Log.Logger.Error(ex);
                }
                else
                {
                    using var error = ex.Response.GetResponseStream();
                    using var streamReader = new StreamReader(error);
                    Log.Logger.Error(streamReader.ReadToEnd(), ex);
                }
            }
            catch (Exception ex)
            {
                result["valid"] = false;
                Log.Logger.Error(ex);
            }
            return result;
        }

        /// <summary>
        /// VoiceVoxAPI[initialize_speaker]にリクエストを送信します。
        /// </summary>
        /// <param name="speaker">初期化するVoiceVox話者ID</param>
        /// <returns></returns>
        private MultiDic SendVoiceVoxInitializeSpeakerRequst(string speaker)
        {
            var result = new MultiDic();
            try
            {
                using (var speakersResponse = Client.Send(CreateVoiceVoxInitializeSpeakerHttpRequest(speaker)))
                {
                    result["valid"] = speakersResponse.IsSuccessStatusCode;
                    result["statusCode"] = speakersResponse.StatusCode;
                }
                return result;
            }
            catch (WebException ex)
            {
                result["valid"] = false;
                if (ex.Response == null)
                {
                    Log.Logger.Error(ex);
                }
                else
                {
                    using var error = ex.Response.GetResponseStream();
                    using var streamReader = new StreamReader(error);
                    Log.Logger.Error(streamReader.ReadToEnd(), ex);
                }
            }
            catch (Exception ex)
            {
                result["valid"] = false;
                Log.Logger.Error(ex);
            }
            return result;
        }

        /// <summary>
        /// VoiceVoxAPI[audio_query]にリクエストを送信します。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="speaker"></param>
        /// <returns></returns>
        private MultiDic SendVoiceVoxAudioQueryRequst(string message, string speaker)
        {
            var result = new MultiDic();
            try
            {
                using (var audioQueryResponse = Client.Send(CreateVoiceVoxAudioQueryHttpRequest(message, speaker)))
                {
                    result["valid"] = audioQueryResponse.IsSuccessStatusCode;
                    result["statusCode"] = audioQueryResponse.StatusCode;
                    if (!result.GetAsBoolean("valid"))
                    {
                        return result;
                    }
                    using var reader = new StreamReader(audioQueryResponse.Content.ReadAsStream(), Encoding.UTF8);
                    result["audioQuery"] = SerializeUtil.JsonToMultiDic(reader.ReadToEnd());
                }
                return result;
            }
            catch (WebException ex)
            {
                result["valid"] = false;
                if (ex.Response == null)
                {
                    Log.Logger.Error(ex);
                }
                else
                {
                    using var error = ex.Response.GetResponseStream();
                    using var streamReader = new StreamReader(error);
                    Log.Logger.Error(streamReader.ReadToEnd(), ex);
                }
            }
            catch (Exception ex)
            {
                result["valid"] = false;
                Log.Logger.Error(ex);
            }
            return result;
        }

        /// <summary>
        /// VoiceVoxAPI[synthesis]にリクエストを送信します。
        /// </summary>
        /// <param name="audioQueryDic"></param>
        /// <param name="speaker"></param>
        /// <returns></returns>
        private MultiDic SendVoiceVoxSynthesisRequest(MultiDic audioQueryDic, string speaker)
        {
            var result = new MultiDic();
            try
            {
                using (var synthesisResponse = Client.Send(CreateVoiceVoxSynthesisHttpRequest(audioQueryDic, speaker)))
                {
                    result["valid"] = synthesisResponse.IsSuccessStatusCode;
                    result["statusCode"] = synthesisResponse.StatusCode;

                    if (!result.GetAsBoolean("valid"))
                    {
                        return result;
                    }

                    result["voice"] = synthesisResponse.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                }
                return result;
            }
            catch (WebException ex)
            {
                result["valid"] = false;
                if (ex.Response == null)
                {
                    Log.Logger.Error(ex);
                }
                else
                {
                    using var error = ex.Response.GetResponseStream();
                    using var streamReader = new StreamReader(error);
                    Log.Logger.Error(streamReader.ReadToEnd(), ex);
                }
            }
            catch (Exception ex)
            {
                result["valid"] = false;
                Log.Logger.Error(ex);
            }
            return result;
        }

        #endregion SendRequest

        #region CreateHttpRequest

        /// <summary>
        /// VoiceVoxAPI[speakers]に送信する<see cref="HttpRequestMessage"/>を作成します。
        /// </summary>
        /// <returns></returns>
        private static HttpRequestMessage CreateVoiceVoxSpeakersHttpRequest()
        {
            return new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new UriBuilder()
                {
                    Host = Settings.AsString("VoiceVox.Application.Host"),
                    Scheme = Settings.AsString("VoiceVox.Application.Scheme"),
                    Port = Settings.AsInteger("VoiceVox.Application.Port"),
                    Path = Settings.AsString("VoiceVox.Request.Speakers.Path"),
                }.Uri
            };
        }

        /// <summary>
        /// VoiceVoxAPI[initialize_speaker]に送信する<see cref="HttpRequestMessage"/>を作成します。
        /// </summary>
        /// <param name="speaker">初期化を行うVoiceVox話者ID</param>
        /// <returns></returns>
        private static HttpRequestMessage CreateVoiceVoxInitializeSpeakerHttpRequest(string speaker)
        {
            var query = HttpUtility.ParseQueryString("");
            query.Add(Settings.AsString("VoiceVox.Request.InitializeSpeaker.ParamName.Speaker"), speaker);

            return new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new UriBuilder()
                {
                    Host = Settings.AsString("VoiceVox.Application.Host"),
                    Scheme = Settings.AsString("VoiceVox.Application.Scheme"),
                    Port = Settings.AsInteger("VoiceVox.Application.Port"),
                    Path = Settings.AsString("VoiceVox.Request.InitializeSpeaker.Path"),
                    Query = query.ToString()
                }.Uri
            };
        }

        /// <summary>
        /// VoiceVoxAPI[audio_query]に送信する<see cref="HttpRequestMessage"/>を作成します。
        /// </summary>
        /// <param name="text">読み上げメッセージ</param>
        /// <returns></returns>
        private static HttpRequestMessage CreateVoiceVoxAudioQueryHttpRequest(string text, string speaker)
        {
            var query = HttpUtility.ParseQueryString("");
            query.Add(Settings.AsString("VoiceVox.Request.AudioQuery.ParamName.Text"), text);
            query.Add(Settings.AsString("VoiceVox.Request.AudioQuery.ParamName.Speaker"), speaker);

            return new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new UriBuilder()
                {
                    Host = Settings.AsString("VoiceVox.Application.Host"),
                    Scheme = Settings.AsString("VoiceVox.Application.Scheme"),
                    Port = Settings.AsInteger("VoiceVox.Application.Port"),
                    Path = Settings.AsString("VoiceVox.Request.AudioQuery.Path"),
                    Query = query.ToString()
                }.Uri
            };
        }

        /// <summary>
        /// VoiceVoxAPI[synthesis]に送信する<see cref="HttpRequestMessage"/>を作成します。
        /// </summary>
        /// <param name="audioQueryDic">APIにPOST送信するパラメータ</param>
        /// <returns></returns>
        private static HttpRequestMessage CreateVoiceVoxSynthesisHttpRequest(MultiDic audioQueryDic, string speaker)
        {
            var query = System.Web.HttpUtility.ParseQueryString("");
            query.Add(Settings.AsString("VoiceVox.Request.Synthesis.ParamName.Speaker"), speaker);

            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                Content = new StringContent(CastUtil.ToString(audioQueryDic), Encoding.UTF8, Settings.AsString("VoiceVox.Request.Synthesis.Content-Type")),
                RequestUri = new UriBuilder()
                {
                    Host = Settings.AsString("VoiceVox.Application.Host"),
                    Scheme = Settings.AsString("VoiceVox.Application.Scheme"),
                    Port = Settings.AsInteger("VoiceVox.Application.Port"),
                    Path = Settings.AsString("VoiceVox.Request.Synthesis.Path"),
                    Query = query.ToString()
                }.Uri
            };

            foreach (var pair in Settings.AsMultiDic("VoiceVox.Request.Synthesis.Header"))
            {
                message.Headers.Add(pair.Key, CastUtil.ToString(pair.Value));
            }

            return message;
        }

        #endregion CreateHttpRequest

        /// <summary>
        /// VoiceVoxで生成する音声生成クエリを調整します。
        /// </summary>
        /// <param name="audioQueryDic"></param>
        private static void ReplaceAudioQueryJson(MultiDic audioQueryDic)
        {
            var paramList = Settings.AsMultiDic("VoiceVox.Request.AudioQuery.ReplaceJsonParam");
            foreach (var paramKey in paramList.Keys)
            {
                var splits = paramList.GetAsString(paramKey).Split("/").Select(param => param.Trim()).ToList();
                if (splits.Count <= 1)
                {
                    continue;
                }
                switch (splits[1])
                {
                    case "int":
                        audioQueryDic[paramKey] = CastUtil.ToInteger(splits[0]);
                        break;

                    case "double":
                        audioQueryDic[paramKey] = CastUtil.ToDouble(splits[0]);
                        break;

                    case "bool":
                        audioQueryDic[paramKey] = CastUtil.ToBoolean(splits[0]);
                        break;

                    default:
                        audioQueryDic[paramKey] = splits[0];
                        break;
                }
            }
        }

        /// <summary>
        /// エラー発生時の待機処理
        /// </summary>
        /// <param name="retryCount">リトライ回数</param>
        /// <returns></returns>
        private static bool WaitRetry(long retryCount)
        {
            if (Settings.Get("RetryCount").HasValue() || retryCount < Settings.AsLong("RetryCount"))
            {
                Log.Logger.DebugFormat("Retry Connect:{0}/{1}", retryCount, Settings.AsLong("RetryCount"));
                Thread.Sleep(Settings.AsInteger("RetrySleepTime.Milliseconds"));
                return true;
            }
            return false;
        }
    }
}
