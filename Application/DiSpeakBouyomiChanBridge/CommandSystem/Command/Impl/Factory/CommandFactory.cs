using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core;
using net.boilingwater.Framework.Core.Logging;
using net.boilingwater.Framework.Core.Utils;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl.Factory;

/// <summary>
/// <see cref="Command"/>を文字列中から作成するファクトリ
/// </summary>
public class CommandFactory : ExecutableCommandFactory
{
    /// <summary>
    /// ファクトリのシングルトンインスタンス
    /// </summary>
    public static CommandFactory Factory { get; protected set; } = new();

    private Regex CommandPattern { get; set; }

    private CommandFactory() => CommandPattern = new Regex("{dummy}", RegexOptions.Compiled);

    /// <summary>
    /// <paramref name="input"/>からコマンドを検出します。
    /// </summary>
    /// <param name="input">コマンドを含む文字列<br/>検出したコマンドは文字列中から削除されます</param>
    /// <returns>文字列中から検出した<seealso cref="IEnumerable{ExecutableCommand}"/></returns>
    public override IEnumerable<ExecutableCommand> CreateExecutableCommands(ref string input)
    {
        var executableCommands = new List<Command>();
        foreach (Match match in CommandPattern.Matches(input))
        {
            foreach (Command? command in Dic.Values)
            {
                if (command == null)
                {
                    continue;
                }

                if (Regex.IsMatch(match.Value, CastUtil.ToString(command.Regex)))
                {
                    var command2 = (Command)command.Clone();
                    input = input.Replace(match.Value, "");
                    CommandFactory.ReplaceCommand(match, command2);
                    executableCommands.Add(command2);
                }
            }
        }
        return executableCommands;
    }

    private static void ReplaceCommand(Match match, Command command)
    {
        foreach (var str in command.ReplacePattern)
        {
            var replace = str;
            Group? group = match.Groups.Values.Where(group => group.Name == replace).FirstOrDefault();
            if (group != null)
            {
                for (var index = 0; index < command.Arguments.Length; ++index)
                {
                    command.Arguments[index] = command.Arguments[index].Replace($"__{replace}__", group.Value);
                }
            }
        }
    }

    /// <summary>
    /// ファクトリの初期化を行います
    /// </summary>
    public override void Initialize()
    {
        lock (((IDictionary)Dic).SyncRoot)
        {
            try
            {
                //コマンド辞書初期化
                Dic.Clear();

                SimpleDic<Command> dic = LoadCommandDic();

                if (dic != null)
                {
                    foreach (KeyValuePair<string, Command?> pair in dic)
                    {
                        if (pair.Value == null)
                        {
                            continue;
                        }
                        pair.Value.CommandTitle = pair.Key;
                        Dic.Add(pair.Key, pair.Value);
                        Log.Logger.Debug($"コマンド登録：{pair.Key}");
                    }

                    CommandPattern = new($"({string.Join("|", Dic.Values.Select(c => ((Command)c!).Regex))})", RegexOptions.Compiled);

                    Log.Logger.Info("コマンドファイルの読み込みが完了しました。");
                }
                else
                {
                    Log.Logger.Info($"コマンドファイルが存在しないため読み込みをスキップしました。");
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal($"コマンドファイルの読み込みに失敗しました。{Settings.AsString("CommandFile")}を確認してください。", ex);
            }
        }
    }

    /// <summary>
    /// コマンドのYAMLファイルから辞書を取得します。
    /// </summary>
    /// <returns></returns>
    private SimpleDic<Command> LoadCommandDic()
    {
        var commandFilePath = Path.Combine(Directory.GetCurrentDirectory(), Settings.GetAppConfig("OverrideFileFolder"), Settings.AsString("CommandFile"));
        string? content = null;

        if (File.Exists(commandFilePath))
        {
            content = File.ReadAllText(commandFilePath);
        }
        else
        {
            commandFilePath = Path.Combine(Directory.GetCurrentDirectory(), Settings.GetAppConfig("CommandFileFolder"), Settings.AsString("CommandFile"));
            if (File.Exists(commandFilePath))
            {
                content = File.ReadAllText(commandFilePath);
            }
        }

        SimpleDic<Command>? commandDic = null;
        if (content != null)
        {
            commandDic = SerializeUtil.DeserializeYaml<SimpleDic<Command>>(content, false);
            Log.Logger.Debug($"読み込み：{commandFilePath}");
        }

        var dic = new SimpleDic<Command>();
        if (commandDic == null)
        {
            return dic;
        }

        foreach (KeyValuePair<string, Command?> item in commandDic)
        {
            dic[item.Key] = item.Value;
        }
        return dic;
    }
}
