using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.DiSpeakBouyomiChanBridge.Log;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl.Factory
{
    public class SystemCommandFactory : ExecutableCommandFactory
    {
        public static new SystemCommandFactory Factory { get; protected set; }
        public Dictionary<string, string> Dic { get; private set; } = new();

        static SystemCommandFactory()
        {
            Factory = new SystemCommandFactory();
        }

        private SystemCommandFactory()
        {
            //Reload();
        }

        public override IEnumerable<ExecutableCommand> CreateExecutableCommands(ref string input)
        {
            IEnumerable<Type> SystemCommandTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(SystemCommand)));
            List<SystemCommand> list = new List<SystemCommand>();

            string regex = $"({string.Join("|", Dic.Keys)})";
            MatchCollection matches = Regex.Matches(input, regex);

            foreach (Match match in matches)
            {
                string key = Regex.Escape(match.Value);
                if (Dic.ContainsKey(key))
                {
                    input = input.Replace(match.Value, "");
                    input = input.Replace(match.Value, "");
                    string className = Dic[key];
                    Type type = SystemCommandTypes.Where(type => type.Name == className).FirstOrDefault();
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
                    JsonSerializerOptions option = new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.Default,
                        AllowTrailingCommas = true,
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    };
                    Dictionary<string, string> dic = JsonSerializer.Deserialize<Dictionary<string, string>>(
                        File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), Setting.Instance.AsString("SystemCommandFile"))),
                        option);
                    foreach (KeyValuePair<string, string> pair in dic)
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