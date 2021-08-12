using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.DiSpeakBouyomiChanBridge.External;
using net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl;
using net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl.Factory;
using net.boilingwater.DiSpeakBouyomiChanBridge.Log;
using net.boilingwater.Utils.Extention;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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

        private HttpServerForBouyomiChan()
        {
            Init();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="refreshHttpListener">HttpListenerを初期化するか</param>
        public void Init(bool refreshHttpListener = true)
        {
            LoggerPool.Logger.Info("Start Application Initialization!");
            CommandFactory.Factory.Reload();
            SystemCommandFactory.Factory.Reload();
            if (refreshHttpListener)
            {
                var retryCount = 0L;
                var isValid = false;
                do
                {
                    try
                    {
                        if (_listener != null)
                        {
                            _listener.Stop();
                        }
                        _listener = new HttpListener();
                        _listener.Prefixes.Add($"http://localhost:{Setting.AsString("ListeningPort")}/");

                        _listener.Start();
                        HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.AsString("Connecting"));
                        isValid = true;
                        _listener.Stop();
                        break;
                    }
                    catch (Exception)
                    {
                        LoggerPool.Logger.FatalFormat("Fail to Open ListeningPort:{0} !", Setting.AsString("ListeningPort"));
                        LoggerPool.Logger.DebugFormat("Retry Connect:{0}/{1} !", retryCount, Setting.AsLong("RetryCount"));
                        Thread.Sleep(Setting.AsInteger("RetrySleepTime.Milliseconds"));
                    }

                } while (string.IsNullOrEmpty(Setting.Get("RetryCount")) || retryCount++ < Setting.AsInteger("RetryCount"));
                if (!isValid)
                {
                    LoggerPool.Logger.FatalFormat("Exit Program!");
                }
            }
            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.AsString("FinishInitialize"));
            LoggerPool.Logger.Info("Finish Application Initialization!");
        }

        public void Start()
        {
            _listener.Start();
            HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.AsString("Welcome"));
            Handle();
        }

        private void Handle()
        {
            while (true)
            {
                // Listening処理
                var context = _listener.GetContext();
                var request = context.Request;
                string message = "";
                using (var response = context.Response)
                {
                    if (request.HttpMethod != HttpMethod.Get.Method)
                    {
                        continue;
                    }
                    message = request.GetDiscordMessage();
                    response.StatusCode = 200;
                }

                LoggerPool.Logger.DebugFormat("Receive :{0}", message);

                //システムコマンド検出
                var systemCmds = SystemCommandFactory.Factory.CreateExecutableCommands(ref message);
                //コマンド検出
                var cmds = CommandFactory.Factory.CreateExecutableCommands(ref message);
                //棒読みちゃんに送信
                HttpClientForBouyomiChan.Instance.SendToBouyomiChan(message);

                if (systemCmds != null)
                {
                    //システムコマンド実行
                    systemCmds.ForEach((cmd) => new Task(() => cmd.Execute()).RunSynchronously());
                }
                if (cmds != null)
                {
                    //コマンドを実行キューに追加
                    cmds.ForEach(cmd => CommandExecutor.AddCommand((Command)cmd));
                }
            }
        }
    }

    internal static class RequestExtention
    {
        public static string GetDiscordMessage(this HttpListenerRequest request) => request.QueryString["text"];
    }
}