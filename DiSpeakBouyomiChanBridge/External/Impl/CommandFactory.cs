using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.DiSpeakBouyomiChanBridge.Log;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl.Factory
{
    public class CommandFactory : ExecutableCommandFactory
    {
        public static new CommandFactory Factory { get; protected set; }

        public Dictionary<string, Command> Dic { get; private set; } = new();

        static CommandFactory()
        {
            Factory = new CommandFactory();
        }

        private CommandFactory()
        {
            //Reload();
        }

        public override IEnumerable<ExecutableCommand> CreateExecutableCommands(ref string input)
        {
            var list = new List<Command>();

            var regex = $"({string.Join("|", Dic.Values.Select(c => c.Regex))})";
            var matches = Regex.Matches(input, regex);

            foreach (Match match in matches)
            {
                foreach (var command in Dic.Values)
                {
                    if (Regex.IsMatch(match.Value, command.Regex))
                    {
                        var generateCommand = (Command)command.Clone();
                        input = input.Replace(match.Value, "");
                        ReplaceCommand(match, generateCommand);
                        list.Add(generateCommand);
                        continue;
                    }
                }
            }

            return list;
        }

        private static void ReplaceCommand(Match match, Command command)
        {
            foreach (string replace in command.ReplacePattern)
            {
                var group = match.Groups.Values.Where(group => group.Name == replace).FirstOrDefault();
                if (group != null)
                {
                    for (int i = 0; i < command.RunCommand.Length; i++)
                    {
                        command.RunCommand[i] = command.RunCommand[i].Replace($"__{replace}__", group.Value);
                    }
                }
            }
        }

        public override void Reload()
        {
            lock (((IDictionary)Dic).SyncRoot)
            {
                try
                {
                    //コマンド辞書初期化
                    Dic.Clear();

                    //読み込み
                    var option = new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.Default,
                        AllowTrailingCommas = true,
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true,
                        //DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    };
                    var dic = JsonSerializer.Deserialize<Dictionary<string, Command>>(
                        File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), Setting.AsString("CommandFile"))),
                        option
                        );
                    foreach (var pair in dic)
                    {
                        pair.Value.CommandTitle = pair.Key;
                        Dic.Add(pair.Key, pair.Value);
                    }
                    LoggerPool.Logger.Info("Load successfully CommandFile!");
                }
                catch (Exception e)
                {
                    LoggerPool.Logger.Fatal("Failed to load CommandFile!\nReview [App.config] file!", e);
                    Environment.Exit(-1);
                }
            }
        }
    }
}