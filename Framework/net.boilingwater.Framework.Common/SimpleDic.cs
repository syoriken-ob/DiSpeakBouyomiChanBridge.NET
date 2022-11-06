using System.Collections.Generic;

namespace net.boilingwater.Framework.Common
{
    /// <summary>
    /// 簡易辞書クラス
    /// </summary>
    public class SimpleDic<T> : Dictionary<string, T?>
    {
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
    }
}
