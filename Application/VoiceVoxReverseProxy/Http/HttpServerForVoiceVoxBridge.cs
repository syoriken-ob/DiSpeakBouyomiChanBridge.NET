using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using net.boilingwater.Framework.Common.Http;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Application.VoiceVoxReverseProxy.Http
{
    /// <summary>
    /// VoiceVox互換の簡易HttpServerクラス
    /// </summary>
    public class HttpServerForVoiceVoxBridge : AbstractHttpServer
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static HttpServerForVoiceVoxBridge Instance { get; private set; } = new();

        /// <inheritdoc/>
        protected override void RegisterListeningUrlPrefix(HttpListenerPrefixCollection listenerPrefix)
        {
            var builder = new UriBuilder()
            {
                Scheme = "http",
                Host = Settings.AsString("ListeningHost"),
                Port = Settings.AsInteger("ListeningPort"),
                Path = "/"
            };
            listenerPrefix.Add(builder.Uri.ToString());
        }

        /// <inheritdoc/>
        protected override void OnRequestReceived(IAsyncResult result)
        {
            // Listening処理
            var context = GetContextAndResumeListening(result);
            if (context == null)
            {
                return;
            }
            try
            {
                var path = CastUtil.ToString(context.Request.Url?.AbsolutePath);

                if (path == Settings.AsString("VoiceVox.Request.Speakers.Path"))
                {
                    SetResponseFromSpeakersRequest(context);
                    return;
                }

                if (path == Settings.AsString("VoiceVox.Request.InitializeSpeaker.Path"))
                {
                    SetResponseFromInitializeSpeakerRequest(context);
                    return;
                }

                if (path == Settings.AsString("VoiceVox.Request.AudioQuery.Path"))
                {
                    SetResponseFromAudioQueryRequest(context);
                    return;
                }

                if (path == Settings.AsString("VoiceVox.Request.Synthesis.Path"))
                {
                    SetResponseFromSynthesisRequest(context);
                    return;
                }

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            finally
            {
                context.Response.Close();
            }
        }

        #region レスポンス生成処理

        /// <summary>
        /// VoiceVoxAPI[speakers]に該当するリクエストの処理を行います。
        /// </summary>
        /// <param name="context"><see cref="HttpListenerContext"/></param>
        private static void SetResponseFromSpeakersRequest(HttpListenerContext context)
        {
            if (!VoiceVoxHttpClientManager.FetchAllVoiceVoxSpeakers(out var speakers))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            var response = context.Response;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentEncoding = Encoding.UTF8;

            var str = SerializeUtil.SerializeJson(speakers);

            using var writer = new StreamWriter(response.OutputStream);
            writer.Write(str);
        }

        /// <summary>
        /// VoiceVoxAPI[initialize_speaker]に該当するリクエストの処理を行います。
        /// </summary>
        /// <param name="context"><see cref="HttpListenerContext"/></param>
        private static void SetResponseFromInitializeSpeakerRequest(HttpListenerContext context)
        {
            var query = context.Request.QueryString;
            var response = context.Response;

            if (!query.AllKeys.Contains(Settings.AsString("VoiceVox.Request.InitializeSpeaker.ParamName.Speaker")))
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            if (!VoiceVoxHttpClientManager.SendVoiceVoxInitializeSpeakerRequest(
                    CastUtil.ToString(
                        query.Get(Settings.AsString("VoiceVox.Request.InitializeSpeaker.ParamName.Speaker"))
                    )
            ))
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            response.StatusCode = (int)HttpStatusCode.NoContent;
        }

        /// <summary>
        /// VoiceVoxAPI[audio_query]に該当するリクエストの処理を行います。
        /// </summary>
        /// <param name="context"><see cref="HttpListenerContext"/></param>
        private static void SetResponseFromAudioQueryRequest(HttpListenerContext context)
        {
            var query = context.Request.QueryString;
            var response = context.Response;

            if (!query.AllKeys.Contains("speaker") || !query.AllKeys.Contains("text"))
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            response.ContentEncoding = Encoding.UTF8;

            try
            {
                if (!VoiceVoxHttpClientManager.SendVoiceVoxAudioQueryRequest(
                    CastUtil.ToString(query.Get("text")),
                    CastUtil.ToString(query.Get("speaker")),
                    out var audioQuery
                ))
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                }

                var str = SerializeUtil.SerializeJson(audioQuery);
                using var writer = new StreamWriter(response.OutputStream);
                writer.Write(str);
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            return;
        }

        /// <summary>
        /// VoiceVoxAPI[synthesis]に該当するリクエストの処理を行います。
        /// </summary>
        /// <param name="context"><see cref="HttpListenerContext"/></param>
        private static void SetResponseFromSynthesisRequest(HttpListenerContext context)
        {
            var query = context.Request.QueryString;
            var request = context.Request;
            var response = context.Response;

            if (!query.AllKeys.Contains("speaker"))
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            using var stream = new StreamReader(request.InputStream);
            var input = SerializeUtil.JsonToMultiDic(stream.ReadToEnd());

            if (!input.Any())
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            try
            {
                if (!VoiceVoxHttpClientManager.SendVoiceVoxSynthesisRequest(
                    input,
                    CastUtil.ToString(query.Get("speaker")),
                    out var voice
                ))
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                }

                response.ContentType = Settings.AsString("VoiceVox.Response.Synthesis.Content-Type");
                response.OutputStream.Write(voice, 0, voice.Length);
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            return;
        }

        #endregion レスポンス生成処理
    }
}
