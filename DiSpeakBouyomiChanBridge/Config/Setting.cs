using net.boilingwater.Utils;
using System.Collections.Specialized;
using System.Configuration;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Config
{
    public class Setting
    {
        public static string Get(string key) => ConfigurationManager.AppSettings[key];

        public static string AsString(string key) => Cast.ToString(Get(key));

        public static int AsInteger(string key) => Cast.ToInteger(Get(key));

        public static long AsLong(string key) => Cast.ToLong(Get(key));
    }

    public class MessageSetting
    {
        public static string Get(string key) => ((NameValueCollection)ConfigurationManager.GetSection("MessageSettings"))[key];

        public static string AsString(string key) => Cast.ToString(Get(key));
    }
}