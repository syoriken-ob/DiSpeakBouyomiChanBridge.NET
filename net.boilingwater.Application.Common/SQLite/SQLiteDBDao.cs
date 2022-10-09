using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Utils;

namespace net.boilingwater.Application.Common.SQLite
{
    /// <summary>
    /// SQLite用Database Access Object
    /// </summary>
    public abstract class SQLiteDBDao
    {
        /// <summary>
        /// SQLite接続文字列
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SQLiteDBDao() => _connectionString = $"Data Source={Settings.Settings.AsString("SQLite3.DBFilePath")}";

        /// <summary>
        /// Daoクラスで利用するテーブルの作成処理などを定義します。
        /// </summary>
        public abstract void InitializeTable();

        /// <summary>
        /// SELECT SQLを実行し、データをDataTableで取得します。
        /// </summary>
        /// <param name="sql">実行するSQL文</param>
        /// <param name="parameters">パラメータリスト</param>
        /// <returns></returns>
        protected DataTable Select(string sql, SQLiteParameterList parameters = null)
        {
            var table = new DataTable();
            try
            {
                using var connection = CreateConnection();
                using var command = CreateSQLiteCommand(connection, sql, parameters);
                using var adapter = new SQLiteDataAdapter();
                adapter.SelectCommand = command;
                Log.Logger.Debug("[SQLite]" + command.CommandText.Trim());
                _ = adapter.Fill(table);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
                throw;
            }
            return table;
        }

        /// <summary>
        /// SELECT以外の更新系SQLを実行します。
        /// </summary>
        /// <param name="sql">実行するSQL文</param>
        /// <param name="parameters">パラメータリスト</param>
        /// <returns>影響行数</returns>
        protected int Execute(string sql, SQLiteParameterList parameters = null)
        {
            try
            {
                using var connection = CreateConnection();
                using var command = CreateSQLiteCommand(connection.OpenAndReturn(), sql, parameters);
                LoggingSQLCommand(command);
                return command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// SQLiteのDBファイルが存在しない場合、DBファイルを作成します。
        /// </summary>
        public static void CreateDataBase()
        {
            if (!File.Exists(Settings.Settings.AsString("SQLite3.DBFilePath")))
            {
                SQLiteConnection.CreateFile(Settings.Settings.AsString("SQLite3.DBFilePath"));
            }
        }

        #region private

        /// <summary>
        /// SQLiteとの接続を作成します。
        /// </summary>
        /// <returns></returns>
        private SQLiteConnection CreateConnection() => new(_connectionString);

        /// <summary>
        /// SQLiteCommandを生成します。
        /// </summary>
        /// <param name="connection">DB接続</param>
        /// <param name="sql">実行するSQL</param>
        /// <param name="parameters">SQLパラメータ</param>
        /// <returns></returns>
        private static SQLiteCommand CreateSQLiteCommand(SQLiteConnection connection, string sql, SQLiteParameterList parameters)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters.ToArray());
            }
            return command;
        }

        /// <summary>
        /// SQLをデバッグログに出力します。
        /// </summary>
        /// <param name="command"></param>
        private static void LoggingSQLCommand(SQLiteCommand command)
        {
            var builder = new StringBuilder();
            builder.Append("[SQLite]");
            if (command.Parameters.Count > 0)
            {
                builder.Append(" (");
                foreach (SQLiteParameter param in command.Parameters)
                {
                    builder.Append($"@{param.ParameterName} => {CastUtil.ToString(param.Value)}, ");
                }
                builder.Remove(builder.Length - 2, 2);
                builder.Append(')');
            }
            builder.Append(' ');
            builder.Append(command.CommandText);
            Log.Logger.Debug(builder.ToString());
        }

        #endregion private
    }
}
