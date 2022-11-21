using System;
using System.Runtime.Caching;

using net.boilingwater.Framework.Common.Setting;

namespace net.boilingwater.Framework.Common.Utils
{
    /// <summary>
    /// メモリキャッシュのユーティリティクラス
    /// </summary>
    public static class MemoryCacheUtil
    {
        /// <summary>
        /// メモリキャッシュからデータを取得します。
        /// </summary>
        /// <param name="key">キャッシュキー</param>
        /// <param name="obj">キャッシュするデータ</param>
        /// <param name="expiration">キャッシュ有効期限</param>
        /// <param name="sholdNotRemovable">メモリキャッシュから削除されないようにするかどうか</param>
        public static void RegisterCache(string key, object obj, TimeSpan? expiration = null, bool sholdNotRemovable = false)
        {
            var item = CreateCacheItem(key, obj);
            var policy = CreateCacheItemPolicy(expiration, sholdNotRemovable);
            _ = MemoryCache.Default.Add(item, policy);
        }

        /// <summary>
        /// メモリキャッシュからデータを取得します。
        /// </summary>
        /// <typeparam name="T">取り出す型データ</typeparam>
        /// <param name="key">キャッシュキー</param>
        /// <returns>取得できない場合はその型の既定値を返却します。</returns>
        public static T? GetCache<T>(string key)
        {
            if (MemoryCache.Default.Contains(key))
            {
                try
                {
                    return (T)MemoryCache.Default.Get(key);
                }
                catch { }
            }
            return default;
        }

        /// <summary>
        /// メモリキャッシュからデータを取得できるか試します。
        /// </summary>
        /// <typeparam name="T">取り出す型データ</typeparam>
        /// <param name="key">キャッシュキー</param>
        /// <param name="obj">キャッシュから取り出したデータ</param>
        /// <returns>取得できたかどうか</returns>
        public static bool TryGetCache<T>(string key, out T? obj)
        {
            if (MemoryCache.Default.Contains(key))
            {
                try
                {
                    obj = (T)MemoryCache.Default.Get(key);
                    return true;
                }
                catch { }
            }

            obj = default;
            return false;
        }

        #region private

        /// <summary>
        /// 引数から<see cref="CacheItem"/>を生成します。
        /// </summary>
        /// <param name="key">キャッシュキー</param>
        /// <param name="obj">キャッシュするデータ</param>
        /// <returns></returns>
        private static CacheItem CreateCacheItem(string key, object obj) => new(key, obj);

        /// <summary>
        /// 引数から<see cref="CacheItemPolicy"/>を生成します。
        /// </summary>
        /// <param name="expiration">キャッシュ有効期限</param>
        /// <param name="sholdNotRemovable">メモリキャッシュから削除されないようにするかどうか</param>
        /// <returns></returns>
        private static CacheItemPolicy CreateCacheItemPolicy(TimeSpan? expiration, bool sholdNotRemovable)
        {
            var policy = new CacheItemPolicy
            {
                Priority = sholdNotRemovable ? CacheItemPriority.NotRemovable : CacheItemPriority.Default
            };

            if (expiration.HasValue)
            {
                policy.SlidingExpiration = expiration.Value;
            }
            else
            {
                policy.SlidingExpiration = TimeSpan.FromMinutes(Settings.AsDouble("MemoryCache.Expiration.Default.Minutes"));
            }
            return policy;
        }

        #endregion private
    }
}
