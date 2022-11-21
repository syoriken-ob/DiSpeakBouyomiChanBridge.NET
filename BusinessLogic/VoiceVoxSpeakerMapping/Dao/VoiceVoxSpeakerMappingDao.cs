using System.Data;

using net.boilingwater.Framework.Common.SQLite;

namespace net.boilingwater.BusinessLogic.VoiceVoxSpeakerCache.Dao
{
    /// <summary>
    /// VOICEVOX互換アプリケーション間のVoiceVox話者ID衝突回避マッピングDAOクラス
    /// </summary>
    public class VoiceVoxSpeakerMappingDao : SQLiteDBDao
    {
        /// <inheritdoc/>
        public override void InitializeTable()
        {
            var sql = "";
            sql += " CREATE TABLE IF NOT EXISTS voicevox_speaker_mapping ( ";
            sql += "    speaker_uuid  TEXT  NOT NULL ";
            sql += "  , speaker_id    TEXT  NOT NULL ";
            sql += "  , new_id        TEXT  NOT NULL ";
            sql += "  , update_dt     TIMESTAMP DEFAULT (DATETIME('now','localtime')) ";
            sql += "  , PRIMARY KEY (speaker_uuid, speaker_id) ";
            sql += " ); ";

            _ = Execute(sql);
        }

        /// <summary>
        /// 衝突回避用VoiceVox話者IDマッピング設定を登録します。
        /// </summary>
        /// <param name="speakerUuid">VoiceVox話者UUID</param>
        /// <param name="speakerId">VoiceVox話者ID</param>
        /// <param name="newId">衝突解消後のVoiceVox話者ID</param>
        /// <returns>影響行数</returns>
        public int InsertMapping(Guid speakerUuid, string speakerId, string newId)
        {
            var sql = "";
            var param = new SQLiteParameterList()
            {
                {"speaker_uuid", DbType.String, speakerUuid.ToString() },
                {"speaker_id", DbType.String, speakerId },
                {"new_id", DbType.String, newId }
            };

            sql += " INSERT INTO voicevox_speaker_mapping ( ";
            sql += "   speaker_uuid ";
            sql += " , speaker_id ";
            sql += " , new_id ";
            sql += " ) values ( ";
            sql += "   @speaker_uuid ";
            sql += " , @speaker_id ";
            sql += " , @new_id ";
            sql += " )  ";

            return Execute(sql, param);
        }

        /// <summary>
        /// 衝突回避用VoiceVox話者IDマッピング設定を取得します。
        /// </summary>
        /// <returns></returns>
        public DataTable SelectMappingAll()
        {
            var sql = "";

            sql += " SELECT ";
            sql += "   speaker_uuid ";
            sql += " , speaker_id ";
            sql += " , new_id ";
            sql += " FROM voicevox_speaker_mapping ";

            return Select(sql);
        }
    }
}
