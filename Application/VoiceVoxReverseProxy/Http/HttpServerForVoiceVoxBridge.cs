using System.Net;
using System.Text;

using net.boilingwater.Framework.Common.Http;
using net.boilingwater.Framework.Common.Logging;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Application.VoiceVoxReverseProxy.Http
{
    public class HttpServerForVoiceVoxBridge : AbstractHttpServer
    {
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
                    ResponseSpeakersRequest(context);
                    return;
                }

                context.Response.StatusCode = 404;
            }
            finally
            {
                context.Response.Close();
            }
        }
        private void ResponseSpeakersRequest(HttpListenerContext context)
        {
            if (!VoiceVoxHttpClientManager.FetchAllVoiceVoxSpeakers(out var speakers))
            {
                context.Response.StatusCode = 400;
            }
            var response = context.Response;
            response.StatusCode = 200;
            response.ContentEncoding = Encoding.UTF8;

            var str = SerializeUtil.SerializeJson(speakers);

            using var writer = new StreamWriter(response.OutputStream);
            writer.Write(str);
        }
    }
}
