using System.Collections.Generic;

using net.boilingwater.Framework.Common.Utils;

namespace net.boilingwater.Framework.Common
{
    /// <summary>
    /// 簡易辞書クラス
    /// </summary>
    public class SimpleDic<T> : Dictionary<string, T?>
    {
        /// <summary>
        /// 初期データを空とするコンストラクタ
        /// </summary>
        public SimpleDic()
        { }

        /// <summary>
        /// 初期データを<paramref name="dictionary"/>とするコンストラクタ
        /// </summary>
        public SimpleDic(Dictionary<string, T?> dictionary) : base(dictionary) { }

        /// <summary>
        /// 初期データを<paramref name="collection"/>とするコンストラクタ
        /// </summary>
        public SimpleDic(IEnumerable<KeyValuePair<string, T?>> collection) : base(collection) { }

        /// <summary>
        /// データを取得・設定します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns></returns>
        public new T? this[string key]
        {
            get => TryGetValue(key, out var value) ? value : default;
            set
            {
                if (ContainsKey(key))
                {
                    ((Dictionary<string, T?>)this)[key] = value;
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        #region debug

        /// <inheritdoc/>
        public override string ToString() => SerializeUtil.SerializeJson(this);

        #endregion debug
    }
}
