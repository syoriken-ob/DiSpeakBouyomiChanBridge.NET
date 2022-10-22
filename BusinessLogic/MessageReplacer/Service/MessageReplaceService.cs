using System.Data;
using System.Text.RegularExpressions;

using net.boilingwater.Application.Common;
using net.boilingwater.Application.Common.Extensions;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.Application.Common.Utils;
using net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.MessageReplacer.Dao;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.BusinessLogic.MessageReplacer.Service
{
    /// <summary>
    /// メッセージ置換処理サービス
    /// </summary>
    public class MessageReplaceService
    {
        /// <summary>
        /// メッセージ置換辞書
        /// </summary>
        private static readonly SimpleDic<string> _replaceSetting = new();

        private static readonly Regex RegisterReplaceSettingRegex = new("(教育|学習)[(（](?<replace_key>.+?)=(?<replace_value>.+?)[)）]", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex DeleteReplaceSettingRegex = new("(忘却|消去)[(（](?<replace_key>.+?)[)）]", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// 置換設定をDBから読み込み、初期化を行います。
        /// </summary>
        public static void Initialize()
        {
            var dao = new MessageReplacerDao();
            var replaceTable = dao.SelectReplaceSetting();

            _replaceSetting.Clear();

            foreach (DataRow row in replaceTable.Rows)
            {
                _replaceSetting[CastUtil.ToString(row["replace_key"])] = CastUtil.ToString(row["replace_value"]);
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
                var deleteResult = DetectAndDeleteReplaceSetting(ref message);
                var registerResult = DetectAndRegisterReplaceSetting(ref message);
                ReplaceMessage(ref message);
                if (deleteResult || registerResult)
                {
                    Initialize();
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
                Settings.AsString("RegularExpression.URLShortener"),
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
            foreach (var replace in _replaceSetting)
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
            var match = RegisterReplaceSettingRegex.Match(message);
            if (!match.Success)
            {
                return false;
            }
            var registered = false;
            var dao = new MessageReplacerDao();
            do
            {
                var replaceKey = match.Groups["replace_key"];
                var replaceValue = match.Groups["replace_value"];
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
                message = message.Replace(match.Value, string.Format(Settings.AsString("Format.Replace.RegisterReplaceSetting"), key, replaceValue.Value));
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
            var match = DeleteReplaceSettingRegex.Match(message);
            if (!match.Success)
            {
                return false;
            }

            var dao = new MessageReplacerDao();
            var registered = false;
            do
            {
                var replaceKey = match.Groups["replace_key"];
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
                    message = message.Replace(match.Value, string.Format(Settings.AsString("Format.Replace.DeleteReplaceSetting"), key));
                }
                else
                {
                    message = message.Replace(match.Value, string.Format(Settings.AsString("Format.Replace.NotRegisteredReplaceSetting"), key));
                }
            } while ((match = match.NextMatch()).Success);
            return registered;
        }
    }
}
