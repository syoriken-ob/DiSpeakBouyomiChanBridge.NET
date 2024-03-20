using System.Text.RegularExpressions;

namespace net.boilingwater.BusinessLogic.VoiceReadOut.Const;

/// <summary>
/// コンパイル済みの正規表現をまとめたクラス
/// </summary>
internal static partial class RegexSet
{
    [GeneratedRegex(@"^(?<speaker_id>\w{1,4})\)", RegexOptions.Compiled)]
    internal static partial Regex SpeakerRegex();
}
