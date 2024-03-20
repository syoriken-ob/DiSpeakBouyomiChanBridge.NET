using System.Data;

using Microsoft.Data.Sqlite;

using net.boilingwater.Framework.Common.SQLite;

namespace net.boilingwater.BusinessLogic.MessageReplacer.Dao;

/// <summary>
/// Discord絵文字置換情報用DAOクラス
/// </summary>
public class MessageReplacerDao : SQLiteDBDao
{
    /// <summary>
    /// メッセージ置換情報保存用データベーステーブルを作成します。
    /// </summary>
    public override void InitializeTable()
    {
        var sql = "";
        sql += " CREATE TABLE IF NOT EXISTS replace_setting ( ";
        sql += "    replace_key   TEXT  NOT NULL  PRIMARY KEY ";
        sql += "  , replace_value TEXT ";
        sql += "  , user          TEXT ";
        sql += "  , update_dt     TIMESTAMP DEFAULT (DATETIME('now','localtime')) ";
        sql += " ); ";

        _ = Execute(sql);
    }

    /// <summary>
    /// メッセージ置換情報を登録します。
    /// </summary>
    /// <param name="replaceKey"></param>
    /// <param name="replaceValue"></param>
    /// <param name="user"></param>
    /// <returns>影響行数</returns>
    public int UpdateOrRegisterReplaceSetting(string replaceKey, string replaceValue, string? user = null)
    {
        var sql = "";

        var sqlParams = new SQLiteParameterList
        {
            { "replace_key", SqliteType.Text, replaceKey },
            { "replace_value", SqliteType.Text, replaceValue },
            { "user", SqliteType.Text, user }
        };

        //更新
        sql += " UPDATE replace_setting SET ";
        sql += "   replace_value = @replace_value ";
        sql += " , user = @user ";
        sql += " , update_dt = (DATETIME('now','localtime')) ";
        sql += "  WHERE replace_key = @replace_key";

        var count = Execute(sql, sqlParams);
        if (count > 0)
        {
            return count;
        }

        //レコードがなかった場合登録
        sql = "";
        sql += " INSERT INTO replace_setting ";
        sql += "   (replace_key,  replace_value,  user,  update_dt) ";
        sql += " VALUES ";
        sql += "   (@replace_key, @replace_value, @user, (DATETIME('now','localtime'))) ";

        return Execute(sql, sqlParams);
    }

    /// <summary>
    /// メッセージ置換情報を削除します。
    /// </summary>
    /// <param name="replaceKey"></param>
    /// <returns></returns>
    public int DeleteReplaceSetting(string replaceKey)
    {
        var sql = "";
        var sqlParams = new SQLiteParameterList
        {
            { "replace_key", SqliteType.Text, replaceKey }
        };

        sql += " DELETE FROM replace_setting ";
        sql += "  WHERE replace_key = @replace_key";

        return Execute(sql, sqlParams);
    }

    /// <summary>
    /// メッセージ置換情報を取得します。
    /// </summary>
    /// <returns></returns>
    public DataTable SelectReplaceSetting()
    {
        var sql = "";

        sql += " SELECT ";
        sql += "   replace_key ";
        sql += " , replace_value ";
        sql += " , user ";
        sql += " , update_dt ";
        sql += " FROM replace_setting";
        sql += " ORDER BY LENGTH(replace_key) DESC ";
        sql += "        , replace_key         DESC ";

        return Select(sql);
    }
}
