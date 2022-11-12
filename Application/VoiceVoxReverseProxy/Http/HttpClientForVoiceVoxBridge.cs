using net.boilingwater.BusinessLogic.VoiceReadOut.Httpclients.Service;
using net.boilingwater.Framework.Common;
using net.boilingwater.Framework.Common.Http;
using net.boilingwater.Framework.Common.Logging;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Application.VoiceVoxReverseProxy.Http
{
    /// <summary>
    /// VoiceVoxと各種通信を行うHttpClient
    /// </summary>
    public class HttpClientForVoiceVoxBridge : AbstractHttpClient
    {
        private MultiDic RequestSetting { get; set; }

        public List<string> Speakers { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public HttpClientForVoiceVoxBridge(string host, int port) : base()
        {
            RequestSetting = VoiceVoxRequestService.CreateRequestSettingDic(
                Settings.AsString("VoiceVox.Application.Scheme"),
                host,
                port
            );

            Speakers = FetchEnableVoiceVoxSpeakers();
            InitializeVoiceVoxSpeaker();
        }

        /// <summary>
        /// VoiceVoxAPI[speakers]から利用可能な話者のリストを取得します。
        /// </summary>
        /// <returns></returns>
        public MultiDic SendVoiceVoxSpeakersRequest() => VoiceVoxRequestService.SendVoiceVoxSpeakersRequst(Client, RequestSetting);

        /// <summary>
        /// VoiceVoxAPI[speakers]から利用可能な話者のリストを取得し、キャッシュします。
        /// </summary>
        /// <returns></returns>
        private List<string> FetchEnableVoiceVoxSpeakers()
        {
            var result = SendVoiceVoxSpeakersRequest();

            if (result.GetAsBoolean("valid"))
            {
                var speakers = result.GetAsMultiList("speakers")
                                 .Select(s => CastUtil.ToObject<MultiDic>(s))
                                 .Where(s => s != null)
                                 .SelectMany(s => s!.GetAsMultiList("styles"))
                                 .Select(s => CastUtil.ToObject<MultiDic>(s))
                                 .Where(s => s != null)
                                 .Select(s => s!.GetAsString("id"))
                                 .ToList();

                speakers.ForEach(pair => Log.Logger.Debug($"話者登録：{pair}"));
                return speakers;
            }
            return new List<string>(0);
        }

        /// <summary>
        /// VoiceVoxAPI[initialize_speaker]に通信し、話者の初期化を行います。
        /// </summary>
        private void InitializeVoiceVoxSpeaker()
        {
            foreach (var id in Speakers)
            {
                Log.Logger.Debug($"VoiceVox話者：{id}を初期化します。");
                var result = VoiceVoxRequestService.SendVoiceVoxInitializeSpeakerRequst(Client, RequestSetting, CastUtil.ToString(id));
                Log.Logger.Debug($"VoiceVox話者：{id}の初期化に{(result.GetAsBoolean("valid") ? "成功" : "失敗")}しました。");
            }
        }
    }
}
