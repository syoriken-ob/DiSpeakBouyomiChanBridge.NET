using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using net.boilingwater.BusinessLogic.MessageReplacer.Const;
using net.boilingwater.BusinessLogic.MessageReplacer.Dao;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core;
using net.boilingwater.Framework.Core.Extensions;

namespace net.boilingwater.BusinessLogic.MessageReplacer.Service;

/// <summary>
/// メッセージ置換処理サービス
/// </summary>
public static class MessageReplaceService
{
    /// <summary>
    /// メッセージ置換辞書
    /// </summary>
    private static readonly SimpleDic<string> _replaceSetting = [];

    /// <summary>
    /// メッセージ置換処理実行後初期化処理が必要かどうか
    /// </summary>
    private static bool _needInitialize = false;

    /// <summary>
    /// 置換設定をDBから読み込み、初期化を行います。
    /// </summary>
    public static void Initialize()
    {
        var dao = new MessageReplacerDao();
        DataTable replaceTable = dao.SelectReplaceSetting();

        _replaceSetting.Clear();

        foreach (DataRow row in replaceTable.Rows)
        {
            _replaceSetting[row.GetAsString("replace_key")] = row.GetAsString("replace_value");
        }
    }

    /// <summary>
    /// メッセージ読み込み処理後に初期化処理が必要な場合、初期化処理を行います。
    /// </summary>
    public static void InitializeAfterReadOutIfNeeded()
    {
        if (_needInitialize)
        {
            Initialize();
            _needInitialize = false;
        }
    }

    /// <summary>
    /// メッセージの置換処理を実行します。
    /// </summary>
    /// <param name="message"></param>
    public static void ExecuteReplace(ref string message)
    {
        if (Settings.AsBoolean("Use.ReadOutReplace.MessageReplacer"))
        {
            DetectAndDeleteReplaceSetting(ref message);
            var registerResult = DetectAndRegisterReplaceSetting(ref message);
            ReplaceMessage(ref message);
            if (registerResult)
            {
                _needInitialize = true;
            }
        }
    }

    /// <summary>
    /// URL省略処理
    /// </summary>
    /// <param name="message">メッセージ</param>
    public static void ReplaceMessageUrlShortener(ref string message)
    {
        if (!Settings.AsBoolean("Use.ReadOutReplace.URLShortener"))
        {
            return;
        }

        message = Regex.Replace(
            message,
            Settings.AsString("Regex.URLShortener"),
            Settings.AsString("Format.Replace.URLShortener"),
            RegexOptions.Singleline
        );
    }

    /// <summary>
    /// 置換設定に則って、メッセージを置換します。
    /// </summary>
    /// <param name="message">メッセージ</param>
    public static void ReplaceMessage(ref string message)
    {
        foreach (KeyValuePair<string, string?> replace in _replaceSetting)
        {
            if (message.Contains(replace.Key, StringComparison.Ordinal))
            {
                message = message.Replace(replace.Key, replace.Value);
            }
        }
    }

    /// <summary>
    /// [棒読みちゃん互換処理] 教育コマンドを検出してDBに登録します。<br/>
    /// 登録後、置換設定をDBから再読み込みします。
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <returns>置換設定を登録したかどうか</returns>
    private static bool DetectAndRegisterReplaceSetting(ref string message)
    {
        Match match = RegexSet.RegisterReplaceSettingRegex().Match(message);
        if (!match.Success)
        {
            return false;
        }
        var registered = false;
        var dao = new MessageReplacerDao();
        do
        {
            Group replaceKey = match.Groups["replace_key"];
            Group replaceValue = match.Groups["replace_value"];
            if (replaceKey == null || replaceValue == null)
            {
                continue;
            }

            var key = replaceKey.Value.Trim();
            if (!key.HasValue())
            {
                continue;
            }
            _ = dao.UpdateOrRegisterReplaceSetting(key, replaceValue.Value);
            registered = true;
            message = message.Replace(match.Value, Settings.AsMessage("Format.Replace.RegisterReplaceSetting", key, replaceValue.Value));
        } while ((match = match.NextMatch()).Success);

        return registered;
    }

    /// <summary>
    /// [棒読みちゃん互換処理] 忘却コマンドを検出して置換設定を削除します。
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <returns>置換設定を削除したかどうか</returns>
    private static bool DetectAndDeleteReplaceSetting(ref string message)
    {
        Match match = RegexSet.DeleteReplaceSettingRegex().Match(message);
        if (!match.Success)
        {
            return false;
        }

        var dao = new MessageReplacerDao();
        var registered = false;
        do
        {
            Group replaceKey = match.Groups["replace_key"];
            if (replaceKey == null)
            {
                continue;
            }

            var key = replaceKey.Value.Trim();
            if (!key.HasValue())
            {
                continue;
            }

            if (_replaceSetting.ContainsKey(key))
            {
                _ = dao.DeleteReplaceSetting(key);
                registered = true;
                message = message.Replace(match.Value, Settings.AsMessage("Format.Replace.DeleteReplaceSetting", key));
            }
            else
            {
                message = message.Replace(match.Value, Settings.AsMessage("Format.Replace.NotRegisteredReplaceSetting", key));
            }
        } while ((match = match.NextMatch()).Success);

        if (registered)
        {
            Initialize();
        }

        return registered;
    }
}
