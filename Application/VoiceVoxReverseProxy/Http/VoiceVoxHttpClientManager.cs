using net.boilingwater.Framework.Common;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Application.VoiceVoxReverseProxy.Http
{
    public static class VoiceVoxHttpClientManager
    {
        private static List<HttpClientForVoiceVoxBridge> HttpClientList { get; set; } = new();
        private static SimpleDic<HttpClientForVoiceVoxBridge> HttpClientDic { get; set; } = new();

        public static void Initialize()
        {
            HttpClientList.Clear();
            HttpClientDic.Clear();
            var d = Settings.AsStringList("Map.VoiceVox.Application.HostAndPort");

            foreach (var p in d)
            {
                var pair = p.Split(";");
                if (pair.Length <= 1)
                {
                    continue;
                }
                HttpClientList.Add(new HttpClientForVoiceVoxBridge(CastUtil.ToString(pair[0]), CastUtil.ToInteger(pair[1])));
            }
            var speakers = HttpClientList.SelectMany(c => c.Speakers.Select(s => new { SpeakerId = s, Client = c })).GroupBy(a => a.SpeakerId).ToDictionary(aGroup => aGroup.Key, aGroup => aGroup.Last().Client);
            HttpClientDic = new SimpleDic<HttpClientForVoiceVoxBridge>(speakers!);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="speakers"></param>
        /// <returns></returns>
        public static bool FetchAllVoiceVoxSpeakers(out MultiList speakers)
        {
            var successed = false;
            speakers = new MultiList();
            foreach (var client in HttpClientList)
            {
                var result = client.SendVoiceVoxSpeakersRequest();
                if (result.GetAsBoolean("valid"))
                {
                    successed = true;
                    speakers.AddRange(result.GetAsMultiList("speakers"));
                }
            }
            return successed;
        }
    }
}
