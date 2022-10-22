using System;
using System.Collections.Generic;

namespace net.boilingwater.Application.Common.Extensions
{
    /// <summary>
    /// LINQの拡張メソッドを管理するクラス
    /// </summary>
    public static class LinqExtension
    {
        /// <summary>
        /// <see cref="IEnumerable{T}"/>の<paramref name="sequence"/>のすべてに対して<paramref name="action"/>を適用します
        /// </summary>
        /// <typeparam name="T">パラメータの型</typeparam>
        /// <param name="sequence"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (var item in sequence)
            {
                action(item);
            }
        }

        /// <summary>
        /// <see cref="IEnumerable{T}"/>の<paramref name="dictionary"/>のすべてに対して<paramref name="action"/>を適用します
        /// </summary>
        /// <typeparam name="TKey">パラメータのキーの型</typeparam>
        /// <typeparam name="TValue">パラメータのバリューの型</typeparam>
        /// <param name="dictionary"></param>
        /// <param name="action"></param>
        public static void ForEach<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var item in dictionary)
            {
                action(item);
            }
        }
    }
}
