using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.DiSpeakBouyomiChanBridge.Log;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl.Factory
{
    public class SystemCommandFactory : ExecutableCommandFactory
    {
        public static new SystemCommandFactory Factory { get; protected set; }
        public Dictionary<string, string> Dic { get; private set; } = new();

        static SystemCommandFactory() => Factory = new SystemCommandFactory();

        private SystemCommandFactory()
        {
            //Reload();
        }

        public override IEnumerable<ExecutableCommand> CreateExecutableCommands(ref string input)
        {
            var SystemCommandTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(SystemCommand)));
            var list = new List<SystemCommand>();

            var regex = $"({string.Join("|", Dic.Keys)})";
            var matches = Regex.Matches(input, regex);

            foreach (Match match in matches)
            {
                var key = Regex.Escape(match.Value);
                if (Dic.ContainsKey(key))
                {
                    input = input.Replace(match.Value, "");
                    input = input.Replace(match.Value, "");
                    var className = Dic[key];
                    var type = SystemCommandTypes.Where(type => type.Name == className).FirstOrDefault();
                    if (type != null)
                    {
                        list.Add((SystemCommand)Activator.CreateInstance(type));
                    }
                }
            }
            return list;
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
                        WriteIndented = true
                    };
                    var dic = JsonSerializer.Deserialize<Dictionary<string, string>>(
                        File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), Setting.Instance.AsString("SystemCommandFile"))),
                        option);
                    foreach (var pair in dic)
                    {
                        Dic.Add(pair.Key, pair.Value);
                    }
                    LoggerPool.Logger.Info("Load successfully SystemCommandFile!");
                }
                catch (Exception e)
                {
                    LoggerPool.Logger.Fatal("Failed to load SystemCommandFile!\nReview [App.config] file!", e);
                    Environment.Exit(-1);
                }
            }
        }
    }
}