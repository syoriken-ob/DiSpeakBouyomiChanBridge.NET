using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using net.boilingwater.BusinessLogic.Common.User.Service;
using net.boilingwater.BusinessLogic.VoiceReadout.HttpClients;
using net.boilingwater.BusinessLogic.VoiceReadOut.Const;
using net.boilingwater.BusinessLogic.VoiceReadOut.Dto;
using net.boilingwater.Framework.Core.Extensions;

namespace net.boilingwater.BusinessLogic.VoiceReadOut.Service;

/// <summary>
/// メッセージを読み上げるサービスクラス
/// </summary>
public static class MessageReadOutService
{
    /// <summary>
    /// メッセージを分割する文字集合
    /// </summary>
    private static readonly char[] MessageDelimiter = ['\n', '。'];

    /// <summary>
    /// メッセージを読み上げます。
    /// </summary>
    /// <param name="message">読み上げ対象のメッセージ</param>
    /// <param name="userId">ユーザー</param>
    public static void ReadOutMessage(string message, string userId = "")
    {
        MessageDto messageDto = CreateMessageDto(message, userId);
        HttpClientForReadOut.Instance?.ReadOut(messageDto);
    }

    /// <summary>
    /// 読み上げメッセージDtoを生成します。
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="userId">ユーザーID</param>
    /// <returns>読み上げメッセージDto</returns>
    private static MessageDto CreateMessageDto(string message, string userId)
    {
        var userDefaultSpeakerKey = UserService.GetUserSpeaker(userId);
        var inlineMessages = new List<InlineMessageDto>();

        var speakerKeyInContext = userDefaultSpeakerKey;
        foreach (var messagePerLine in SplitMessage(message))
        {
            (var speakerKey, var extractedMessage) = ExtractSpeakerKey(messagePerLine, speakerKeyInContext);
            speakerKeyInContext = speakerKey;
            if (extractedMessage.HasValue())
            {
                inlineMessages.Add(new InlineMessageDto(extractedMessage, speakerKey));
            }
        }

        return new MessageDto(inlineMessages, userDefaultSpeakerKey);
    }

    /// <summary>
    /// メッセージを読み上げ単位に分割します。
    /// </summary>
    /// <returns></returns>
    private static List<string> SplitMessage(string message)
    {
        IEnumerable<string> messages = new List<string>() { message.Replace("\r", "") };

        foreach (var splitChar in MessageDelimiter)
        {
            messages = messages.SelectMany(m => m.Split(splitChar)).Where(m => m.HasValue());
        }

        return messages.ToList();
    }

    /// <summary>
    /// メッセージ文中からメッセージと話者キーを取得します。
    /// </summary>
    /// <param name="messagePerLine">改行や句点ごとに分割した文字列を指定してください。</param>
    /// <param name="speakerKeyInContext">文脈中の話者キー（メッセージ中から抽出できなかった場合はそのまま返却）</param>
    /// <returns></returns>
    private static (string speakerKey, string extractedMessage) ExtractSpeakerKey(string messagePerLine, string speakerKeyInContext = "")
    {
        Match match = RegexSet.SpeakerRegex().Match(messagePerLine);
        if (!match.Success)
        {
            return (speakerKeyInContext, messagePerLine);
        }

        Group speakerId = match.Groups["speaker_id"];
        if (speakerId == null)
        {
            return (speakerKeyInContext, messagePerLine);
        }

        return (speakerId.Value, messagePerLine.Replace(match.Value, ""));
    }
}
