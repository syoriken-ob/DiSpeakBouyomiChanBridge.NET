using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

using net.boilingwater.Framework.Common.Logging;
using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Framework.Common.Setting
{
    /// <summary>
    /// アプリケーション設定値を取得するクラス
    /// </summary>
    public class Settings
    {
        private static readonly Dictionary<string, List<string>> _listCache = new();
        private static readonly SimpleDic<MultiDic> _dicCache = new();
        private static SettingHolder? SettingHolderInstance { get; set; }

        /// <summary>
        /// アプリケーション設定値を読み込みます。
        /// </summary>
        public static void Initialize()
        {
            SettingHolderInstance = new SettingHolder();
            _dicCache.Clear();
            _listCache.Clear();
        }

        /// <summary>
        /// <paramref name="key"/>に紐づくアプリケーション設定値を取得します
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>アプリケーション設定値</returns>
        public static string? Get(string key) => SettingHolderInstance?[key];

        /// <summary>
        /// <see cref="string"/>型で<paramref name="key"/>に紐づくアプリケーション設定値を取得します<br/>
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>アプリケーション設定値を<see cref="net.boilingwater.Framework.Common.Utils.CastUtil.ToString(object)"/>で変換して取得</returns>
        public static string AsString(string key) => CastUtil.ToString(Get(key));

        /// <summary>
        /// <see cref="int"/>型で<paramref name="key"/>に紐づくアプリケーション設定値を取得します
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>アプリケーション設定値を<see cref="net.boilingwater.Framework.Common.Utils.CastUtil.ToInteger(object?)"/>で変換して取得</returns>
        public static int AsInteger(string key) => CastUtil.ToInteger(Get(key));

        /// <summary>
        /// <see cref="long"/>型で<paramref name="key"/>に紐づくアプリケーション設定値を取得します
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>アプリケーション設定値を<see cref="net.boilingwater.Framework.Common.Utils.CastUtil.ToLong(object?)"/>で変換して取得</returns>
        public static long AsLong(string key) => CastUtil.ToLong(Get(key));

        /// <summary>
        /// <see cref="bool"/>型で<paramref name="key"/>に紐づくアプリケーション設定値を取得します
        /// </summary>
        /// <param name="key"></param>
        /// <returns>アプリケーション設定値を<see cref="net.boilingwater.Framework.Common.Utils.CastUtil.ToBoolean(object?)"/>で変換して取得</returns>
        public static bool AsBoolean(string key) => CastUtil.ToBoolean(Get(key));

        /// <summary>
        /// <see cref="bool"/>型で<paramref name="key"/>に紐づくアプリケーション設定値を取得します
        /// </summary>
        /// <param name="key"></param>
        /// <returns>アプリケーション設定値を<see cref="net.boilingwater.Framework.Common.Utils.CastUtil.ToDouble(object?)"/>で変換して取得</returns>
        public static double AsDouble(string key) => CastUtil.ToDouble(Get(key));

        /// <summary>
        /// <see cref="decimal"/>型で<paramref name="key"/>に紐づくアプリケーション設定値を取得します
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>アプリケーション設定値を<see cref="net.boilingwater.Framework.Common.Utils.CastUtil.ToDecimal(object?)"/>で変換して取得</returns>
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
                var original = AsString(key).Split(splitKey).Select(str => str.Trim());
                list.AddRange(original.ToList());
            }
            catch (Exception) { }

            _listCache.Add($"{key}#{splitKey}", list);

            return list;
        }

        /// <summary>
        /// <paramref name="key"/>に紐づくアプリケーション設定値を<paramref name="listSplitKey"/>と<paramref name="pairSplitKey"/>で分割した<see cref="MultiDic"/>を取得します
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <param name="listSplitKey">分割するキー  ※初期値は,（カンマ）</param>
        /// <param name="pairSplitKey">さらに分割するキー  ※初期値は;（セミコロン）</param>
        /// <returns>アプリケーション設定値を<paramref name="listSplitKey"/>と<paramref name="pairSplitKey"/>で分割して取得</returns>
        /// <remarks>一度分割した設定値は<paramref name="key"/>と<paramref name="listSplitKey"/>と<paramref name="pairSplitKey"/>の組み合わせでキャッシュされます</remarks>
        public static MultiDic AsMultiDic(string key, string listSplitKey = ",", string pairSplitKey = ";")
        {
            if (_dicCache.ContainsKey($"{key}#{listSplitKey}#{pairSplitKey}"))
            {
                return _dicCache[$"{key}#{listSplitKey}#{pairSplitKey}"] ?? new MultiDic();
            }

            var dic = new MultiDic();
            try
            {
                foreach (var keyValue in AsString(key).Split(listSplitKey))
                {
                    var split = keyValue.Split(pairSplitKey);
                    if (!split.Any() || split[0] == null || dic.ContainsKey(split[0]))
                    {
                        continue;
                    }
                    dic[split[0].Trim()] = split.Length > 1 ? split[1].Trim() : (object)"";
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Warn(ex);
            }

            _dicCache.Add($"{key}#{listSplitKey}#{pairSplitKey}", dic);

            return dic;
        }

        /// <summary>
        /// <paramref name="key"/>に紐づくアプリケーション設定値をメッセージとして取得します。
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>メッセージ</returns>
        /// <remarks><see cref="AsString(string)"/>と同じ</remarks>
        public static string AsMessage(string key) => AsString(key);

        /// <summary>
        /// <paramref name="key"/>に紐づくアプリケーション設定値をメッセージとして取得します。
        /// 取得時に{0}などのプレースホルダを<paramref name="replaceValues"/>で置換します。
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <param name="replaceValues"></param>
        /// <remarks><see cref="AsString(string)"/>と同じ</remarks>
        public static string AsMessage(string key, params string[] replaceValues) => string.Format(AsMessage(key), replaceValues);

        /// <summary>
        /// AppSettingsの設定値を取得します<br/>
        /// 存在しない場合は、空文字を返却します。
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <returns>アプリケーション設定値</returns>
        public static string GetAppConfig(string key)
        {
            try
            {
                return CastUtil.ToString(ConfigurationManager.AppSettings[key]);
            }
            catch
            {
                return "";
            }
        }
    }
}
