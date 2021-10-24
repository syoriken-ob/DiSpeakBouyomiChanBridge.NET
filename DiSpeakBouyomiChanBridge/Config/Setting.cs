using net.boilingwater.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Config
{
    public class Setting
    {
        public static SettingProvider Instance = new SettingProvider("AppSettings");
    }

    public class MessageSetting
    {
        public static SettingProvider Instance = new SettingProvider("MessageSettings");
    }

    public class DiscordSetting
    {
        public static SettingProvider Instance = new SettingProvider("DiscordSettings");
    }

    public class SettingProvider
    {
        private readonly Dictionary<string, List<string>> _listCache = new();

        private readonly NameValueCollection Setting;

        internal SettingProvider(string sectionName)
        {
            if (sectionName == "AppSettings")
            {
                Setting = ConfigurationManager.AppSettings;
            }
            else
            {
                Setting = (NameValueCollection)ConfigurationManager.GetSection(sectionName);
            }
        }
        public string Get(string key)
        {
            return Setting[key];
        }

        public string AsString(string key)
        {
            return Cast.ToString(Get(key));
        }

        public int AsInteger(string key)
        {
            return Cast.ToInteger(Get(key));
        }

        public long AsLong(string key)
        {
            return Cast.ToLong(Get(key));
        }

        public bool AsBoolean(string key)
        {
            return Cast.ToBoolean(Get(key));
        }

        public decimal AsDecimal(string key)
        {
            return Cast.ToDecimal(Get(key));
        }

        public List<string> AsStringList(string key, string splitKey = ",")
        {
            if (_listCache.ContainsKey($"{key}#{splitKey}"))
            {
                return _listCache[$"{key}#{splitKey}"];
            }

            List<string> list = new List<string>();
            try
            {
                IEnumerable<string> original = Get(key).Split(splitKey).Select(str => str.Trim());
                list.AddRange(original.ToList());
            }
            catch (Exception) { }

            _listCache.Add($"{key}#{splitKey}", list);

            return list;
        }
    }
}