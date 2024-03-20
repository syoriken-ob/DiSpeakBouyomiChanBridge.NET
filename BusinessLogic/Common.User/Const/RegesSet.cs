using System.Text.RegularExpressions;

namespace net.boilingwater.BusinessLogic.Common.User.Const;

/// <summary>
/// コンパイル済みの正規表現をまとめたクラス
/// </summary>
internal static partial class RegexSet
{
    [GeneratedRegex(@"(話者登録)[(（](?<speaker_key>\w{1,4})(,(?<user_id>\d+?))?[)）]", RegexOptions.Compiled | RegexOptions.Singleline)]
    internal static partial Regex RegisterUserSpeakerRegex();

    [GeneratedRegex(@"(話者解除)[(（](?<user_id>\d*?)[)）]", RegexOptions.Compiled | RegexOptions.Singleline)]
    internal static partial Regex DeleteUserSpeakerRegex();
}
