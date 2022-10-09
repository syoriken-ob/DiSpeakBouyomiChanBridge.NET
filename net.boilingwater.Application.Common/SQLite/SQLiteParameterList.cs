using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace net.boilingwater.Application.Common.SQLite
{
    /// <summary>
    /// SQLiteでパラメータ化クエリを実行する時に利用するパラメータを保持するリスト
    /// </summary>
    public class SQLiteParameterList : List<SQLiteParameter>
    {
        /// <summary>
        /// クエリに含まれるパラメータを追加します。
        /// </summary>
        /// <param name="name">パラメータ名（※@を付けないこと）</param>
        /// <param name="type">パラメータの型</param>
        /// <param name="value">値</param>
        public void Add(string name, DbType type, object value)
        {
            var parameter = new SQLiteParameter
            {
                ParameterName = name,
                DbType = type,
                Value = value
            };
            Add(parameter);
        }
    }
}
