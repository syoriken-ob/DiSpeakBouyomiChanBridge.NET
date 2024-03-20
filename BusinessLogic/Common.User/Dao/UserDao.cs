using System.Data;

using Microsoft.Data.Sqlite;

using net.boilingwater.Framework.Common.SQLite;

namespace net.boilingwater.BusinessLogic.Common.User.Dao;

/// <summary>
/// ユーザー固有設定用DAOクラス
/// </summary>
public class UserDao : SQLiteDBDao
{
    /// <summary>
    /// ユーザー固有設定保存用データベーステーブルを作成します。
    /// </summary>
    public override void InitializeTable()
    {
        var sql = "";
        sql += " CREATE TABLE IF NOT EXISTS user_speaker ( ";
        sql += "    user_id         TEXT  NOT NULL  PRIMARY KEY ";
        sql += "  , speaker_key     TEXT ";
        sql += "  , update_dt       TIMESTAMP DEFAULT (DATETIME('now','localtime')) ";
        sql += " ); ";

        _ = Execute(sql);
    }

    /// <summary>
    /// ユーザーの既定話者キーを登録します。
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="speakerKey">話者キー</param>
    /// <returns>影響行数</returns>
    public int UpdateOrRegisterUserSpeaker(string userId, string speakerKey)
    {
        var sql = "";
        var sqlParams = new SQLiteParameterList
        {
            { "user_id", SqliteType.Text, userId },
            { "speaker_key", SqliteType.Text, speakerKey }
        };

        //UPSERT
        sql += " INSERT INTO user_speaker ( ";
        sql += "   user_id,  speaker_key,  update_dt ";
        sql += " ) VALUES ( ";
        sql += "   @user_id, @speaker_key, (DATETIME('now','localtime')) ";
        sql += " ) ";
        sql += " ON CONFLICT(user_id) ";
        sql += " DO UPDATE SET ";
        sql += "   speaker_key = @speaker_key ";
        sql += " , update_dt   = (DATETIME('now','localtime')) ";

        return Execute(sql, sqlParams);
    }

    /// <summary>
    /// ユーザーの既定話者キーを削除します。
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <returns></returns>
    public int DeleteUserSpeaker(string userId)
    {
        var sql = "";
        var sqlParams = new SQLiteParameterList
        {
            { "user_id", SqliteType.Text, userId }
        };

        sql += " DELETE FROM user_speaker ";
        sql += "  WHERE user_id = @user_id";

        return Execute(sql, sqlParams);
    }

    /// <summary>
    /// ユーザーの既定話者キーを取得します。
    /// </summary>
    /// <returns></returns>
    public DataTable SelectUserSpeaker()
    {
        var sql = "";
        sql += " SELECT ";
        sql += "   user_id ";
        sql += " , speaker_key ";
        sql += " , update_dt ";
        sql += " FROM user_speaker";
        sql += " ORDER BY user_id ASC ";

        return Select(sql);
    }
}
