﻿using System;
using System.Collections.Generic;

namespace net.boilingwater.Utils.Extention
{
    public static class LinqExtention
    {
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (var item in sequence) action(item);
        }

        public static void ForEach<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var item in dictionary) action(item);
        }
    }
}