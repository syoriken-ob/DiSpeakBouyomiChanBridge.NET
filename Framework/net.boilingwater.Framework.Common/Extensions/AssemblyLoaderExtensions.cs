using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace net.boilingwater.Framework.Common.Extensions
{
    /// <summary>
    /// <see cref="Assembly"/>型の拡張メソッドを管理するクラス
    /// </summary>
    /// <remarks>
    /// こちらを利用しています。<br/>
    /// https://qiita.com/jun1s/items/4cf712d92151658f6250
    /// </remarks>
    public static class AssemblyLoaderExtensions
    {
        /// <summary>
        /// <paramref name="assembly"/>から参照されるアセンブリを一覧で取得します。
        /// </summary>
        /// <param name="assembly">アセンブリ情報</param>
        /// <param name="assemblyNameCheckCondition">対象とするアセンブリ探索条件</param>
        /// <returns>アセンブリの一覧</returns>
        /// <remarks><paramref name="assemblyNameCheckCondition"/>で絞り込み条件を設定します。</remarks>
        public static IEnumerable<Assembly> CollectReferencedAssemblies(this Assembly assembly, Func<AssemblyName, bool> assemblyNameCheckCondition)
        {
            return assembly.GetReferencedAssemblies()
                           .Where(assemblyNameCheckCondition)
                           .SelectMany(a => Assembly.Load(a).CollectReferencedAssemblies(assemblyNameCheckCondition))
                           .Append(assembly)
                           .ToHashSet();
        }
    }
}
