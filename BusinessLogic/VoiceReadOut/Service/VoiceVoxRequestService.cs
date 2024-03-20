using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;

using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core;
using net.boilingwater.Framework.Core.Extensions;
using net.boilingwater.Framework.Core.Interface;
using net.boilingwater.Framework.Core.Logging;
using net.boilingwater.Framework.Core.Utils;

namespace net.boilingwater.BusinessLogic.VoiceReadOut.Service
{
    /// <summary>
    /// VOICEVOXにリクエストを送信するためのサービスクラス
    /// </summary>
    public static class VoiceVoxRequestService
    {
        #region SendRequest

        /// <summary>
        /// VoiceVoxAPI[speakers]にリクエストを送信します。
        /// </summary>
        /// <returns></returns>
        public static IMultiDic SendVoiceVoxSpeakersRequest(HttpClient client, IMultiDic setting)
        {
            var result = new MultiDic();
            try
            {
                using (HttpResponseMessage speakersResponse = client.Send(CreateVoiceVoxSpeakersHttpRequest(setting)))
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
                    using Stream error = ex.Response.GetResponseStream();
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
        /// <param name="client">Httpクライアント</param>
        /// <param name="setting"><see cref="CreateRequestSettingDic(string, string, int)"/>で生成した通信用共通設定辞書</param>
        /// <param name="speaker">初期化するVoiceVox話者ID</param>
        /// <returns></returns>
        public static IMultiDic SendVoiceVoxInitializeSpeakerRequest(HttpClient client, IMultiDic setting, string speaker)
        {
            var result = new MultiDic();
            try
            {
                using (HttpResponseMessage speakersResponse = client.Send(CreateVoiceVoxInitializeSpeakerHttpRequest(setting, speaker)))
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
                    using Stream error = ex.Response.GetResponseStream();
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
        /// <param name="client">Httpクライアント</param>
        /// <param name="setting"><see cref="CreateRequestSettingDic(string, string, int)"/>で生成した通信用共通設定辞書</param>
        /// <param name="message">読み上げメッセージ</param>
        /// <param name="speaker">VoiceVox話者ID</param>
        /// <returns></returns>
        public static IMultiDic SendVoiceVoxAudioQueryRequest(HttpClient client, IMultiDic setting, string message, string speaker)
        {
            var result = new MultiDic();
            try
            {
                using (HttpResponseMessage audioQueryResponse = client.Send(CreateVoiceVoxAudioQueryHttpRequest(setting, message, speaker)))
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
                    using Stream error = ex.Response.GetResponseStream();
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
        /// <param name="client">Httpクライアント</param>
        /// <param name="setting"><see cref="CreateRequestSettingDic(string, string, int)"/>で生成した通信用共通設定辞書</param>
        /// <param name="audioQueryDic">VoiceVoxAPI[audio_query]で生成した音声合成パラメータ</param>
        /// <param name="speaker">VoiceVox話者ID</param>
        /// <returns></returns>
        public static IMultiDic SendVoiceVoxSynthesisRequest(HttpClient client, IMultiDic setting, IMultiDic audioQueryDic, string speaker)
        {
            var result = new MultiDic();
            try
            {
                using (HttpResponseMessage synthesisResponse = client.Send(CreateVoiceVoxSynthesisHttpRequest(setting, audioQueryDic, speaker)))
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
                    using Stream error = ex.Response.GetResponseStream();
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
        /// <param name="setting"><see cref="CreateRequestSettingDic(string, string, int)"/>で生成した通信用共通設定辞書</param>
        /// <returns></returns>
        public static HttpRequestMessage CreateVoiceVoxSpeakersHttpRequest(IMultiDic setting)
        {
            return new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new UriBuilder()
                {
                    Host = setting.GetAsString("host"),
                    Scheme = setting.GetAsString("schema"),
                    Port = setting.GetAsInteger("port"),
                    Path = Settings.AsString("VoiceVox.Request.Speakers.Path"),
                }.Uri
            };
        }

        /// <summary>
        /// VoiceVoxAPI[initialize_speaker]に送信する<see cref="HttpRequestMessage"/>を作成します。
        /// </summary>
        /// <param name="setting"><see cref="CreateRequestSettingDic(string, string, int)"/>で生成した通信用共通設定辞書</param>
        /// <param name="speaker">初期化を行うVoiceVox話者ID</param>
        /// <returns></returns>
        public static HttpRequestMessage CreateVoiceVoxInitializeSpeakerHttpRequest(IMultiDic setting, string speaker)
        {
            NameValueCollection query = HttpUtility.ParseQueryString("");
            query.Add(Settings.AsString("VoiceVox.Request.InitializeSpeaker.ParamName.Speaker"), speaker);

            return new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new UriBuilder()
                {
                    Host = setting.GetAsString("host"),
                    Scheme = setting.GetAsString("schema"),
                    Port = setting.GetAsInteger("port"),
                    Path = Settings.AsString("VoiceVox.Request.InitializeSpeaker.Path"),
                    Query = query.ToString()
                }.Uri
            };
        }

        /// <summary>
        /// VoiceVoxAPI[audio_query]に送信する<see cref="HttpRequestMessage"/>を作成します。
        /// </summary>
        /// <param name="setting"><see cref="CreateRequestSettingDic(string, string, int)"/>で生成した通信用共通設定辞書</param>
        /// <param name="text">読み上げメッセージ</param>
        /// <param name="speaker">VoiceVox話者ID</param>
        /// <returns></returns>
        public static HttpRequestMessage CreateVoiceVoxAudioQueryHttpRequest(IMultiDic setting, string text, string speaker)
        {
            NameValueCollection query = HttpUtility.ParseQueryString("");
            query.Add(Settings.AsString("VoiceVox.Request.AudioQuery.ParamName.Text"), text);
            query.Add(Settings.AsString("VoiceVox.Request.AudioQuery.ParamName.Speaker"), speaker);

            return new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new UriBuilder()
                {
                    Host = setting.GetAsString("host"),
                    Scheme = setting.GetAsString("schema"),
                    Port = setting.GetAsInteger("port"),
                    Path = Settings.AsString("VoiceVox.Request.AudioQuery.Path"),
                    Query = query.ToString()
                }.Uri
            };
        }

        /// <summary>
        /// VoiceVoxAPI[synthesis]に送信する<see cref="HttpRequestMessage"/>を作成します。
        /// </summary>
        /// <param name="setting"><see cref="CreateRequestSettingDic(string, string, int)"/>で生成した通信用共通設定辞書</param>
        /// <param name="audioQueryDic">VoiceVoxAPI[audio_query]で生成した音声合成パラメータ</param>
        /// <param name="speaker">VoiceVox話者ID</param>
        /// <returns></returns>
        public static HttpRequestMessage CreateVoiceVoxSynthesisHttpRequest(IMultiDic setting, IMultiDic audioQueryDic, string speaker)
        {
            NameValueCollection query = HttpUtility.ParseQueryString("");
            query.Add(Settings.AsString("VoiceVox.Request.Synthesis.ParamName.Speaker"), speaker);

            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                Content = new StringContent(SerializeUtil.SerializeJson(audioQueryDic), Encoding.UTF8, Settings.AsString("VoiceVox.Request.Synthesis.Content-Type")),
                RequestUri = new UriBuilder()
                {
                    Host = setting.GetAsString("host"),
                    Scheme = setting.GetAsString("schema"),
                    Port = setting.GetAsInteger("port"),
                    Path = Settings.AsString("VoiceVox.Request.Synthesis.Path"),
                    Query = query.ToString()
                }.Uri
            };

            foreach (KeyValuePair<string, object?> pair in Settings.AsMultiDic("VoiceVox.Request.Synthesis.Header"))
            {
                message.Headers.Add(pair.Key, CastUtil.ToString(pair.Value));
            }

            return message;
        }

        #endregion CreateHttpRequest

        /// <summary>
        /// VoiceVoxと通信する際に利用する共通設定辞書を作成します。
        /// </summary>
        /// <param name="schema">リクエストのスキーマ</param>
        /// <param name="host">リクエスト送信先のホスト名</param>
        /// <param name="port">リクエスト送信先のポート番号</param>
        /// <returns></returns>
        public static IMultiDic CreateRequestSettingDic(string schema, string host, int port)
        {
            return new ReadOnlyMultiDic(new MultiDic()
            {
                {"schema", schema},
                {"host", host},
                {"port", port},
            });
        }

        /// <summary>
        /// VoiceVoxで生成する音声生成クエリを調整します。
        /// </summary>
        /// <param name="audioQueryDic">VoiceVoxAPI[audio_query]で生成した音声合成パラメータ</param>
        /// <param name="speakerKey">メッセージ中にVOICEVOX話者を指定するキー</param>
        public static void ReplaceAudioQueryJson(IMultiDic audioQueryDic, string speakerKey)
        {
            IMultiDic paramDic;
            if (speakerKey.HasValue() && Settings.Get($"VoiceVox.Request.AudioQuery.ReplaceJsonParam.{speakerKey}").HasValue())
            {
                //話者固有設定
                paramDic = Settings.AsMultiDic($"VoiceVox.Request.AudioQuery.ReplaceJsonParam.{speakerKey}");
            }
            else
            {
                //グローバル設定
                paramDic = Settings.AsMultiDic("VoiceVox.Request.AudioQuery.ReplaceJsonParam");
            }

            foreach (var paramKey in paramDic.Keys)
            {
                var splits = paramDic.GetAsString(paramKey).Split("/").Select(param => param.Trim()).ToList();
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
    }
}
