﻿using System;
using System.Data;
using System.Linq;

using net.boilingwater.BusinessLogic.VoiceVoxSpeakerMapping.Dao;
using net.boilingwater.BusinessLogic.VoiceVoxSpeakerMapping.Dto;
using net.boilingwater.Framework.Core;
using net.boilingwater.Framework.Core.Utils;

namespace net.boilingwater.BusinessLogic.VoiceVoxSpeakerMapping.Service;

/// <summary>
/// VOICEVOX互換アプリケーション間のVoiceVox話者ID衝突回避マッピングサービスクラス
/// </summary>
public static class VoiceVoxSpeakerMappingService
{
    /// <summary>
    /// 衝突回避用VoiceVox話者IDマッピング設定を登録します。
    /// </summary>
    /// <param name="speakerUuid">VoiceVox話者UUID</param>
    /// <param name="speakerId">VoiceVox話者ID</param>
    /// <param name="newId">衝突解消後のVoiceVox話者ID</param>
    /// <returns>影響行数</returns>
    public static bool InsertMapping(Guid speakerUuid, string speakerId, string newId)
    {
        var dao = new VoiceVoxSpeakerMappingDao();
        return dao.InsertMapping(speakerUuid, speakerId, newId) == 1;
    }

    /// <summary>
    /// 衝突回避用VoiceVox話者IDマッピング設定を取得します。
    /// </summary>
    public static SimpleDic<SpeakerRemappingDto> GetMapping()
    {
        var dao = new VoiceVoxSpeakerMappingDao();
        DataTable table = dao.SelectMappingAll();
        var dic = new SimpleDic<SpeakerRemappingDto>();

        foreach (DataRow row in table.Rows.Cast<DataRow>())
        {
            var key = CastUtil.ToString(row["new_id"]);
            var value = new SpeakerRemappingDto
            {
                Guid = CastUtil.ToGuid(row["speaker_uuid"]),
                Id = CastUtil.ToString(row["speaker_id"])
            };
            dic[key] = value;
        }

        return dic;
    }
}
