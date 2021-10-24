using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.DiSpeakBouyomiChanBridge.External;
using net.boilingwater.DiSpeakBouyomiChanBridge.Log;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Http
{
    public class HttpServerForBouyomiChan
    {
        public static HttpServerForBouyomiChan Instance { get; private set; }

        static HttpServerForBouyomiChan()
        {
            Instance = new();
        }

        private HttpListener _listener;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="refreshHttpListener">HttpListenerを初期化するか</param>
        public void Init(bool refreshHttpListener = true)
        {

            if (refreshHttpListener)
            {
                long retryCount = 0L;
                bool isValid = false;
                do
                {
                    try
                    {
                        if (_listener != null)
                        {
                            _listener.Stop();
                        }
                        _listener = new HttpListener();
                        _listener.Prefixes.Add($"http://localhost:{Setting.Instance.AsString("ListeningPort")}/");

                        _listener.Start();
                        HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.Instance.AsString("Connecting"));
                        isValid = true;
                        _listener.Stop();
                        break;
                    }
                    catch (Exception)
                    {
                        LoggerPool.Logger.FatalFormat("Fail to Open ListeningPort:{0} !", Setting.Instance.AsString("ListeningPort"));
                        LoggerPool.Logger.DebugFormat("Retry Connect:{0}/{1} !", retryCount, Setting.Instance.AsLong("RetryCount"));
                        Thread.Sleep(Setting.Instance.AsInteger("RetrySleepTime.Milliseconds"));
                    }

                } while (string.IsNullOrEmpty(Setting.Instance.AsString("RetryCount")) || retryCount++ < Setting.Instance.AsInteger("RetryCount"));
                if (!isValid)
                {
                    LoggerPool.Logger.FatalFormat("Exit Program!");
                }
            }

        }

        public void Start()
        {
            _listener.Start();
            Handle();
        }

        private void Handle()
        {
            while (true)
            {
                // Listening処理
                HttpListenerContext context = _listener.GetContext();
                HttpListenerRequest request = context.Request;
                string message = "";
                using (HttpListenerResponse response = context.Response)
                {
                    if (request.HttpMethod != HttpMethod.Get.Method)
                    {
                        continue;
                    }
                    message = request.GetDiscordMessage();
                    response.StatusCode = 200;
                }

                LoggerPool.Logger.DebugFormat("Receive :{0}", message);

                CommandHandlingService.Handle(message);
            }
        }
    }

    internal static class RequestExtention
    {
        public static string GetDiscordMessage(this HttpListenerRequest request)
        {
            return request.QueryString["text"];
        }
    }
}