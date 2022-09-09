using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

using net.boilingwater.Application.Common;
using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.Application.Common.Utils;
using net.boilingwater.DiSpeakBouyomiChanBridge.Voice;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Http.Impl
{
    /// <summary>
    /// VOICEVOX用の読み上げクライアント
    /// </summary>
    public class HttpClientForVoiceVox : HttpClientForReadOut
    {
        /// <summary>
        /// メッセージを読み上げます
        /// </summary>
        /// <param name="text"></param>
        public override void ReadOut(string text)
        {
            var message = text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var retryCount = 0L;
            while (true)
            {
                var audioQueryResult = SendVoiceVoxAudioQueryRequst(message);
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

                var synthesisResult = SendVoiceVoxSynthesisRequest(audioQuery);
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

                Log.Logger.Debug($"ReadOut Message: {message}");
                VoiceVoxReadOutExecutor.Instance.AddQueue(synthesisResult.GetAsObject<byte[]>("voice"));
                return;
            }
        }

        /// <summary>
        /// VoiceVoxAPI[audio_query]にリクエストを送信します。
        /// </summary>
        /// <returns></returns>
        private MultiDic SendVoiceVoxAudioQueryRequst(string message)
        {
            var result = new MultiDic();
            try
            {
                using (var audioQueryResponse = client_.Send(CreateVoiceVoxAudioQueryHttpRequest(message)))
                {
                    result["valid"] = audioQueryResponse.IsSuccessStatusCode;
                    result["statusCode"] = audioQueryResponse.StatusCode;
                    if (!result.GetAsBoolean("valid"))
                    {
                        return result;
                    }
                    using (var reader = new StreamReader(audioQueryResponse.Content.ReadAsStream(), Encoding.UTF8))
                    {
                        result["audioQuery"] = CastUtil.JsonToMultiDic(reader.ReadToEnd());
                    }
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
                    using (var error = ex.Response.GetResponseStream())
                    {
                        using (var streamReader = new StreamReader(error))
                        {
                            Log.Logger.Error(streamReader.ReadToEnd(), ex);
                        }
                    }
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
        /// <returns></returns>
        private MultiDic SendVoiceVoxSynthesisRequest(MultiDic audioQueryDic)
        {
            var result = new MultiDic();
            try
            {
                using (var synthesisResponse = client_.Send(CreateVoiceVoxSynthesisHttpRequest(audioQueryDic)))
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
                    using (var error = ex.Response.GetResponseStream())
                    {
                        using (var streamReader = new StreamReader(error))
                        {
                            Log.Logger.Error(streamReader.ReadToEnd(), ex);
                        }
                    }
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
        /// VoiceVoxAPI[audio_query]に送信する<see cref="HttpRequestMessage"/>を作成します。
        /// </summary>
        /// <param name="text">読み上げメッセージ</param>
        /// <returns></returns>
        private static HttpRequestMessage CreateVoiceVoxAudioQueryHttpRequest(string text)
        {
            var query = System.Web.HttpUtility.ParseQueryString("");
            query.Add(Settings.AsString("VoiceVox.Request.AudioQuery.ParamName.Text"), text);
            query.Add(Settings.AsString("VoiceVox.Request.AudioQuery.ParamName.Speaker"), Settings.AsString("VoiceVox.Speaker"));

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
        private static HttpRequestMessage CreateVoiceVoxSynthesisHttpRequest(MultiDic audioQueryDic)
        {
            var query = System.Web.HttpUtility.ParseQueryString("");
            query.Add(Settings.AsString("VoiceVox.Request.Synthesis.ParamName.Speaker"), Settings.AsString("VoiceVox.Speaker"));

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
            if (string.IsNullOrEmpty(Settings.Get("RetryCount")) || retryCount < Settings.AsLong("RetryCount"))
            {
                Log.Logger.DebugFormat("Retry Connect:{0}/{1}", retryCount, Settings.AsLong("RetryCount"));
                Thread.Sleep(Settings.AsInteger("RetrySleepTime.Milliseconds"));
                return true;
            }
            return false;
        }
    }
}