using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

using net.boilingwater.Application.Common.Extentions;
using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;
using net.boilingwater.Application.Common.Utils;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Impl.Factory
{
    /// <summary>
    /// <see cref="Command"/>を文字列中から作成するファクトリ
    /// </summary>
    public class CommandFactory : ExecutableCommandFactory
    {
        /// <summary>
        /// ファクトリのシングルトンインスタンス
        /// </summary>
        public static CommandFactory Factory { get; protected set; } = new();

        private CommandFactory()
        {
        }

        /// <summary>
        /// <paramref name="input"/>からコマンドを検出します。
        /// </summary>
        /// <param name="input">コマンドを含む文字列<br/>検出したコマンドは文字列中から削除されます</param>
        /// <returns>文字列中から検出した<seealso cref="IEnumerable{ExecutableCommand}"/></returns>
        public override IEnumerable<ExecutableCommand> CreateExecutableCommands(ref string input)
        {
            var executableCommands = new List<Command>();
            var pattern = "(" + string.Join("|", Dic.Values.Select(c => ((Command)c).Regex)) + ")";
            foreach (Match match in Regex.Matches(input, pattern))
            {
                foreach (Command command1 in Dic.Values)
                {
                    if (Regex.IsMatch(match.Value, CastUtil.ToString(command1.Regex)))
                    {
                        var command2 = (Command)command1.Clone();
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
                var group1 = match.Groups.Values.Where(group => group.Name == replace).FirstOrDefault();
                if (group1 != null)
                {
                    for (var index = 0; index < command.RunCommand.Length; ++index)
                        command.RunCommand[index] = command.RunCommand[index].Replace("__" + replace + "__", group1.Value);
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

                    //読み込み
                    var options = new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.Default,
                        AllowTrailingCommas = true,
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    };
                    var dic = JsonSerializer.Deserialize<Dictionary<string, Command>>(
                        File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), Settings.AsString("CommandFile"))),
                        options);

                    if (dic != null)
                    {
                        foreach (var pair in dic)
                        {
                            pair.Value.CommandTitle = pair.Key;
                            Dic.Add(pair.Key, pair.Value);
                            Log.Logger.DebugFormat("コマンド登録：{0}", pair.Key);
                        }
                    }
                    Log.Logger.Info("コマンドファイルの読み込みが完了しました。");
                }
                catch (Exception ex)
                {
                    Log.Logger.Fatal($"コマンドファイルの読み込みに失敗しました。{Settings.AsString("CommandFile")}を確認してください。", ex);
                    throw;
                }
            }
        }
    }
}
