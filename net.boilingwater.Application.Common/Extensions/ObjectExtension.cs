using System.Linq;
using System.Reflection;

using net.boilingwater.Application.Common.Utils;

namespace net.boilingwater.Application.Common.Extentions
{
    /// <summary>
    /// <see cref="object"/>型の拡張メソッドを管理するクラス<br/>
    /// こちらを利用しています。<br/>
    /// https://baba-s.hatenablog.com/entry/2014/02/27/000000
    /// </summary>
    public static class ObjectExtension
    {
        private const string SEPARATOR = ",";       // 区切り記号として使用する文字列
        private const string FORMAT = "{0}:{1}";    // 複合書式指定文字列

        /// <summary>
        /// すべての公開フィールドの情報を文字列にして返します
        /// </summary>
        public static string ToStringFields<T>(this T obj) => string.Join(SEPARATOR, obj
                .GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(c => string.Format(FORMAT, c.Name, CastUtil.ToString(c.GetValue(obj)))));

        /// <summary>
        /// すべての公開プロパティの情報を文字列にして返します
        /// </summary>
        public static string ToStringProperties<T>(this T obj) => string.Join(SEPARATOR, obj
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(c => c.CanRead)
                .Select(c => string.Format(FORMAT, c.Name, CastUtil.ToString(c.GetValue(obj, null)))));

        /// <summary>
        /// すべての公開フィールドと公開プロパティの情報を文字列にして返します
        /// </summary>
        public static string ToStringReflection<T>(this T obj) => string.Join(SEPARATOR,
                obj.ToStringFields(),
                obj.ToStringProperties());
    }
}
