using System.Data;
using System.Text.RegularExpressions;

using net.boilingwater.BusinessLogic.Common.User.Const;
using net.boilingwater.BusinessLogic.Common.User.Dao;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core;
using net.boilingwater.Framework.Core.Extensions;

namespace net.boilingwater.BusinessLogic.Common.User.Service;

/// <summary>
/// ユーザー固有処理サービス
/// </summary>
public static class UserService
{
    /// <summary>
    /// ユーザー話者キー辞書
    /// </summary>
    private static readonly SimpleDic<string> _userSpeakerDic = [];

    /// <summary>
    /// 初期化を行います。
    /// </summary>
    public static void Initialize()
    {
        InitializeUserSpeaker();
    }

    /// <summary>
    /// ユーザー話者設定をDBから読み込み、初期化を行います。
    /// </summary>
    public static void InitializeUserSpeaker()
    {
        var dao = new UserDao();
        DataTable userSpeakerTable = dao.SelectUserSpeaker();

        _userSpeakerDic.Clear();

        foreach (DataRow row in userSpeakerTable.Rows)
        {
            _userSpeakerDic[row.GetAsString("user_id")] = row.GetAsString("speaker_key");
        }
    }

    /// <summary>
    /// ユーザー既定話者設定の登録・解除を行います。
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="messageUserId">メッセージのユーザーID</param>
    public static void ExecuteUserSpeakerProcess(ref string message, string messageUserId)
    {
        var isAffected = false;
        isAffected |= DetectAndDeleteUserSpeaker(ref message, messageUserId);
        isAffected |= DetectAndRegisterUserSpeaker(ref message, messageUserId);

        if (isAffected)
        {
            InitializeUserSpeaker();
        }
    }

    /// <summary>
    /// ユーザーの既定話者設定を取得します。
    /// </summary>
    /// <param name="messageUserId">メッセージのユーザーID</param>
    /// <returns></returns>
    public static string GetUserSpeaker(string messageUserId) => _userSpeakerDic[messageUserId] ?? "";

    #region private

    /// <summary>
    /// 話者登録コマンドを検出してユーザーの既定話者を登録します。<br/>
    /// 登録後、話者キーキャッシュ設定をDBから再読み込みします。
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="messageUserId">メッセージのユーザーID</param>
    /// <returns>既定話者を登録したかどうか</returns>
    private static bool DetectAndRegisterUserSpeaker(ref string message, string messageUserId)
    {
        Match match = RegexSet.RegisterUserSpeakerRegex().Match(message);
        if (!match.Success)
        {
            return false;
        }
        var registered = false;
        var dao = new UserDao();
        do
        {
            Group speakerKeyGroup = match.Groups["speaker_key"];
            Group userIdGroup = match.Groups["user_id"];

            var speakerKey = speakerKeyGroup.Value.Trim();
            if (!speakerKey.HasValue())
            {
                continue;
            }

            var userId = userIdGroup.Value.Trim();
            if (!userId.HasValue())
            {
                userId = messageUserId;
            }

            _ = dao.UpdateOrRegisterUserSpeaker(userId, speakerKey);
            registered = true;
            message = message.Replace(match.Value, Settings.AsMessage("Format.User.RegisterUserSpeakerSetting", userId, speakerKey));
        } while ((match = match.NextMatch()).Success);

        return registered;
    }

    /// <summary>
    /// 話者解除コマンドを検出してユーザーの既定話者設定を削除します。
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="messageUserId">メッセージのユーザーID</param>
    /// <returns>既定話者を削除したかどうか</returns>
    private static bool DetectAndDeleteUserSpeaker(ref string message, string messageUserId)
    {
        Match match = RegexSet.DeleteUserSpeakerRegex().Match(message);
        if (!match.Success)
        {
            return false;
        }

        var dao = new UserDao();
        var registered = false;
        do
        {
            Group userIdGroup = match.Groups["user_id"];

            var userId = userIdGroup.Value.Trim();
            if (!userId.HasValue())
            {
                userId = messageUserId;
            }

            if (_userSpeakerDic.ContainsKey(userId))
            {
                _ = dao.DeleteUserSpeaker(userId);
                _ = _userSpeakerDic.Remove(userId);
                registered = true;
                message = message.Replace(match.Value, Settings.AsMessage("Format.User.DeleteUserSpeakerSetting", userId));
            }
            else
            {
                message = message.Replace(match.Value, Settings.AsMessage("Format.User.NotRegisteredUserSpeakerSetting", userId));
            }
        } while ((match = match.NextMatch()).Success);

        return registered;
    }

    #endregion private
}
