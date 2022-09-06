using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Linq;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.DiSpeakBouyomiChanBridge.external.AudioPlay;
using System.Collections.Generic;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Http.Impl
{
    public class HttpClientForVoiceVox : HttpClientForReadOut
    {
        /// <summary>
        /// メッセージを読み上げます
        /// </summary>
        /// <param name="text"></param>
        public override void ReadOut(string text)
        {
            var sendMessage = text.Trim();
            if (string.IsNullOrEmpty(sendMessage))
            {
                return;
            }

            var retryCount = 0L;
            var isValid = false;
            while (true)
            {
                try
                {
                    string? queryJson = null;
                    using (var audioQueryResponse = client_.Send(CreateVoiceVoxAudioQueryHttpRequest(sendMessage)))
                    {
                        if (audioQueryResponse.IsSuccessStatusCode)
                        {
                            Log.Logger.Debug($"Send AudioQuery :{audioQueryResponse.StatusCode}");
                            using (var reader = new StreamReader(audioQueryResponse.Content.ReadAsStream(), Encoding.UTF8))
                            {
                                queryJson = reader.ReadToEnd();
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(queryJson))
                    {
                        queryJson = queryJson.Replace("\"outputSamplingRate\":24000,\"outputStereo\":false", "\"outputSamplingRate\":48000,\"outputStereo\":true");
                        using (var synthesisResponse = client_.Send(CreateVoiceVoxSynthesisHttpRequest(queryJson)))
                        {
                            if (synthesisResponse.IsSuccessStatusCode)
                            {
                                Log.Logger.Debug($"Send Synthesis :{synthesisResponse.StatusCode}");

                                using (var receivedStream = synthesisResponse.Content.ReadAsStream())
                                {
                                    Log.Logger.Debug($"Play :{sendMessage}");
                                    AudioPlayer.Play(receivedStream);
                                    isValid = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        isValid = false;
                    }
                }
                catch (Exception)
                {
                    isValid = false;
                }

                if (isValid)
                {
                    return;
                }

                Log.Logger.Fatal($"Fail to Send Message to VoiceVox: {sendMessage}");
                if (string.IsNullOrEmpty(Settings.Get("RetryCount")) || retryCount++ < Settings.AsLong("RetryCount"))
                {
                    Log.Logger.DebugFormat("Retry Connect:{0}/{1}", retryCount, Settings.AsLong("RetryCount"));
                    Thread.Sleep(Settings.AsInteger("RetrySleepTime.Milliseconds"));
                }
                else
                {
                    return;
                }
            }
        }

        private static HttpRequestMessage CreateVoiceVoxAudioQueryHttpRequest(string text)
        {
            var query = System.Web.HttpUtility.ParseQueryString("");
            query.Add(Settings.AsString("VoiceVox.Request.AudioQuery.ParamName.Text"), text);
            query.Add(Settings.AsString("VoiceVox.Request.AudioQuery.ParamName.Speaker"), Settings.AsString("VoiceVox.Speaker"));

            var uriBuilder = new UriBuilder()
            {
                Host = Settings.AsString("VoiceVox.Application.Host"),
                Scheme = Settings.AsString("VoiceVox.Application.Scheme"),
                Port = Settings.AsInteger("VoiceVox.Application.Port"),
                Path = Settings.AsString("VoiceVox.Request.AudioQuery.Path"),
                Query = query.ToString()
            };

            return new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = uriBuilder.Uri
            };
        }

        private static HttpRequestMessage CreateVoiceVoxSynthesisHttpRequest(string queryJson)
        {
            var query = System.Web.HttpUtility.ParseQueryString("");
            query.Add(Settings.AsString("VoiceVox.Request.Synthesis.ParamName.Speaker"), Settings.AsString("VoiceVox.Speaker"));

            var uriBuilder = new UriBuilder()
            {
                Host = Settings.AsString("VoiceVox.Application.Host"),
                Scheme = Settings.AsString("VoiceVox.Application.Scheme"),
                Port = Settings.AsInteger("VoiceVox.Application.Port"),
                Path = Settings.AsString("VoiceVox.Request.Synthesis.Path"),
                Query = query.ToString()
            };

            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = uriBuilder.Uri,
                Content = new StringContent(queryJson, Encoding.UTF8, Settings.AsString("VoiceVox.Request.Synthesis.Content-Type"))
            };

            Settings.AsStringList("VoiceVox.Request.Synthesis.Header")
                .Select(e =>
                {
                    var split = e.Split(';');
                    return new KeyValuePair<string, string>(split[0], split[1]);
                })
                .ToList()
                .ForEach(pair => message.Headers.Add(pair.Key, pair.Value));

            return message;
        }
    }
}