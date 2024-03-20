using System.Text.RegularExpressions;

namespace net.boilingwater.BusinessLogic.MessageReplacer.Const;

/// <summary>
/// コンパイル済みの正規表現をまとめたクラス
/// </summary>
internal static partial class RegexSet
{
    [GeneratedRegex("(教育|学習)[(（](?<replace_key>.+?)=(?<replace_value>.+?)[)）]", RegexOptions.Compiled | RegexOptions.Singleline)]
    internal static partial Regex RegisterReplaceSettingRegex();

    [GeneratedRegex("(忘却|消去)[(（](?<replace_key>.+?)[)）]", RegexOptions.Compiled | RegexOptions.Singleline)]
    internal static partial Regex DeleteReplaceSettingRegex();
}
