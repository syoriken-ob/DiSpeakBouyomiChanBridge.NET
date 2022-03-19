using System;
using System.Collections.Generic;
using System.Linq;

using net.boilingwater.Application.Common.Utils;

namespace net.boilingwater.Application.Common.Settings
{
    /// <summary>
    /// アプリケーション設定値を取得するクラス
    /// </summary>
    public class Settings
    {
        private static readonly Dictionary<string, List<string>> _listCache = new();

        /// <summary>
        /// <paramref name="key"/>に紐づくアプリケーション設定値を取得します
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>アプリケーション設定値</returns>
        public static string Get(string key) => SettingHolder.Instance[key];

        /// <summary>
        /// <see cref="string"/>型で<paramref name="key"/>に紐づくアプリケーション設定値を取得します<br/>
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>アプリケーション設定値を<see cref="net.boilingwater.Application.Common.Utils.CastUtil.ToString(object)"/>で変換して取得</returns>
        public static string AsString(string key) => CastUtil.ToString(Get(key));

        /// <summary>
        /// <see cref="int"/>型で<paramref name="key"/>に紐づくアプリケーション設定値を取得します
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>アプリケーション設定値を<see cref="net.boilingwater.Application.Common.Utils.CastUtil.ToInteger(object)"/>で変換して取得</returns>
        public static int AsInteger(string key) => CastUtil.ToInteger(Get(key));

        /// <summary>
        /// <see cref="long"/>型で<paramref name="key"/>に紐づくアプリケーション設定値を取得します
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>アプリケーション設定値を<see cref="net.boilingwater.Application.Common.Utils.CastUtil.ToLong(object)"/>で変換して取得</returns>
        public static long AsLong(string key) => CastUtil.ToLong(Get(key));

        /// <summary>
        /// <see cref="bool"/>型で<paramref name="key"/>に紐づくアプリケーション設定値を取得します
        /// </summary>
        /// <param name="key"></param>
        /// <returns>アプリケーション設定値を<see cref="net.boilingwater.Application.Common.Utils.CastUtil.ToBoolean(object)"/>で変換して取得</returns>
        public static bool AsBoolean(string key) => CastUtil.ToBoolean(Get(key));

        /// <summary>
        /// <see cref="decimal"/>型で<paramref name="key"/>に紐づくアプリケーション設定値を取得します
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>アプリケーション設定値を<see cref="net.boilingwater.Application.Common.Utils.CastUtil.ToDecimal(object)"/>で変換して取得</returns>
        public static decimal AsDecimal(string key) => CastUtil.ToDecimal(Get(key));

        /// <summary>
        /// <paramref name="key"/>に紐づくアプリケーション設定値を<paramref name="splitKey"/>で分割した<see cref="List{String}"/>を取得します
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <param name="splitKey">分割するキー  ※初期値は,（カンマ）</param>
        /// <returns>アプリケーション設定値を<paramref name="splitKey"/>で分割して取得</returns>
        /// <remarks>一度分割した設定値は<paramref name="key"/>と<paramref name="splitKey"/>の組み合わせでキャッシュされます</remarks>
        public static List<string> AsStringList(string key, string splitKey = ",")
        {
            if (_listCache.ContainsKey($"{key}#{splitKey}"))
            {
                return _listCache[$"{key}#{splitKey}"];
            }

            var list = new List<string>();
            try
            {
                var original = Get(key).Split(splitKey).Select(str => str.Trim());
                list.AddRange(original.ToList());
            }
            catch (Exception) { }

            _listCache.Add($"{key}#{splitKey}", list);

            return list;
        }
    }
}
