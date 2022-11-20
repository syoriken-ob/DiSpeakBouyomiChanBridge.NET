using System;
using System.Collections.Generic;

using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Framework.Common
{
    /// <summary>
    /// 任意の型のデータを格納できるリストオブジェクト
    /// </summary>
    public class MultiList : List<object?>
    {
        /// <summary>
        /// 初期データを空とするコンストラクタ
        /// </summary>
        public MultiList()
        { }

        /// <summary>
        /// 初期データを<paramref name="list"/>とするコンストラクタ
        /// </summary>
        public MultiList(List<object> list) : base(list) { }

        /// <summary>
        /// 初期データを<paramref name="collection"/>とするコンストラクタ
        /// </summary>
        public MultiList(IEnumerable<object> collection) : base(collection) { }

        /// <summary>
        /// <see cref="string"/>型としてデータを取得します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public string GetAsString(int index) => CastUtil.ToString(this[index]);

        /// <summary>
        /// <see cref="bool"/>型としてデータを取得します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public bool GetAsBoolean(int index) => CastUtil.ToBoolean(this[index]);

        /// <summary>
        /// <see cref="decimal"/>型としてデータを取得します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public decimal GetAsDecimal(int index) => CastUtil.ToDecimal(this[index]);

        /// <summary>
        /// <see cref="double"/>型としてデータを取得します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public double GetAsDouble(int index) => CastUtil.ToDouble(this[index]);

        /// <summary>
        /// <see cref="long"/>型としてデータを取得します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public long GetAsShort(int index) => CastUtil.ToLong(this[index]);

        /// <summary>
        /// <see cref="uint"/>型としてデータを取得します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public ulong GetAsUnsignedLong(int index) => CastUtil.ToUnsignedLong(this[index]);

        /// <summary>
        /// <see cref="int"/>型としてデータを取得します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public int GetAsInteger(int index) => CastUtil.ToInteger(this[index]);

        /// <summary>
        /// <see cref="uint"/>型としてデータを取得します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public uint GetAsUnsignedInteger(int index) => CastUtil.ToUnsignedInteger(this[index]);

        /// <summary>
        /// <see cref="Guid"/>型としてデータを取得します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public Guid GetAsGuid(int index) => CastUtil.ToGuid(this[index]);

        /// <summary>
        /// <typeparamref name="T"/>型としてデータを取得します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public T? GetAsObject<T>(int index) => CastUtil.ToObject<T>(this[index]);

        /// <summary>
        /// <see cref="MultiDic"/>型としてデータを取得します。
        /// インデックスに紐づくデータがない場合は、空の<see cref="MultiDic"/>を返します。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        public MultiDic GetAsMultiDic(int index)
        {
            try
            {
                return CastUtil.ToObject<MultiDic>(this[index]) ?? new MultiDic();
            }
            catch (Exception) { }
            return new MultiDic();
        }

        #region debug

        /// <inheritdoc/>
        public override string ToString() => SerializeUtil.SerializeJson(this);

        #endregion debug
    }
}
