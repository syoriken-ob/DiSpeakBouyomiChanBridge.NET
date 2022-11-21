using System;
using System.Collections.Generic;

using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Framework.Common
{
    /// <summary>
    /// 任意の型のデータを格納できる辞書オブジェクト
    /// </summary>
    public class MultiDic : Dictionary<string, object?>
    {
        /// <summary>
        /// 初期データを空とするコンストラクタ
        /// </summary>
        public MultiDic()
        { }

        /// <summary>
        /// 初期データを<paramref name="dictionary"/>とするコンストラクタ
        /// </summary>
        public MultiDic(Dictionary<string, object?> dictionary) : base(dictionary) { }

        /// <summary>
        /// 初期データを<paramref name="collection"/>とするコンストラクタ
        /// </summary>
        public MultiDic(IEnumerable<KeyValuePair<string, object?>> collection) : base(collection) { }

        /// <summary>
        /// データを取得・設定します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public new object? this[string key]
        {
            get => TryGetValue(key, out var value) ? value : default;
            set
            {
                if (ContainsKey(key))
                {
                    ((Dictionary<string, object?>)this)[key] = value;
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        /// <summary>
        /// <see cref="string"/>型としてデータを取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public string GetAsString(string key) => CastUtil.ToString(this[key]);

        /// <summary>
        /// <see cref="bool"/>型としてデータを取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public bool GetAsBoolean(string key) => CastUtil.ToBoolean(this[key]);

        /// <summary>
        /// <see cref="decimal"/>型としてデータを取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public decimal GetAsDecimal(string key) => CastUtil.ToDecimal(this[key]);

        /// <summary>
        /// <see cref="double"/>型としてデータを取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public double GetAsDouble(string key) => CastUtil.ToDouble(this[key]);

        /// <summary>
        /// <see cref="long"/>型としてデータを取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public long GetAsShort(string key) => CastUtil.ToLong(this[key]);

        /// <summary>
        /// <see cref="uint"/>型としてデータを取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public ulong GetAsUnsignedLong(string key) => CastUtil.ToUnsignedLong(this[key]);

        /// <summary>
        /// <see cref="int"/>型としてデータを取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public int GetAsInteger(string key) => CastUtil.ToInteger(this[key]);

        /// <summary>
        /// <see cref="uint"/>型としてデータを取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public uint GetAsUnsignedInteger(string key) => CastUtil.ToUnsignedInteger(this[key]);

        /// <summary>
        /// <see cref="Guid"/>型としてデータを取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public Guid GetAsGuid(string key) => CastUtil.ToGuid(this[key]);

        /// <summary>
        /// <typeparamref name="T"/>型としてデータを取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public T? GetAsObject<T>(string key) => CastUtil.ToObject<T>(this[key]);

        /// <summary>
        /// <see cref="MultiDic"/>型としてデータを取得します。
        /// キーに紐づくデータがない場合は、空の<see cref="MultiDic"/>を返します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public MultiDic GetAsMultiDic(string key)
        {
            if (ContainsKey(key))
            {
                try
                {
                    var d = CastUtil.ToObject<MultiDic>(this[key]);
                    return d ?? new MultiDic();
                }
                catch (Exception) { }
            }

            return new MultiDic();
        }

        /// <summary>
        /// <see cref="MultiList"/>型としてデータを取得します。
        /// キーに紐づくデータがない場合は、空の<see cref="MultiList"/>を返します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public MultiList GetAsMultiList(string key)
        {
            if (ContainsKey(key))
            {
                try
                {
                    var l = CastUtil.ToObject<MultiList>(this[key]);
                    return l ?? new MultiList();
                }
                catch (Exception) { }
            }

            return new MultiList();
        }

        #region debug

        /// <inheritdoc/>
        public override string ToString() => SerializeUtil.SerializeJson(this);

        #endregion debug
    }
}
