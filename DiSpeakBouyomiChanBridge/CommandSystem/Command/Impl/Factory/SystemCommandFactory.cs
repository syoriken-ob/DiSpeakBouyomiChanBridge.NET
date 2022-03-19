using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.Application.Common.Settings;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Impl.Factory
{
    /// <summary>
    /// <see cref="SystemCommand"/>を文字列中から作成するファクトリ
    /// </summary>
    public class SystemCommandFactory : ExecutableCommandFactory
    {
        /// <summary>
        /// ファクトリのシングルトンインスタンス
        /// </summary>
        public static SystemCommandFactory Factory { get; protected set; } = new();

        private SystemCommandFactory()
        {
        }

        /// <summary>
        /// 入力文字列からコマンドを生成します
        /// </summary>
        /// <param name="input">入力文字列</param>
        /// <returns></returns>
        public override IEnumerable<ExecutableCommand> CreateExecutableCommands(ref string input)
        {

            var list = new List<SystemCommand>();

            var regex = $"({string.Join("|", Dic.Keys)})";
            var matches = Regex.Matches(input, regex);

            foreach (Match match in matches)
            {
                var key = Regex.Escape(match.Value);
                if (Dic.ContainsKey(key))
                {
                    input = input.Replace(match.Value, "");
                    var instance = Dic[key];
                    if (instance != null)
                    {
                        list.Add((SystemCommand)instance);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 設定ファイルを読み込んでコマンド初期化処理を行います
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
                    var option = new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.Default,
                        AllowTrailingCommas = true,
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    };
                    var dic = JsonSerializer.Deserialize<Dictionary<string, string>>(
                        File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), Settings.AsString("SystemCommandFile"))),
                        option);
                    if (dic != null)
                    {
                        var SystemCommandTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(SystemCommand)));
                        foreach (var pair in dic)
                        {
                            var type = SystemCommandTypes.Where(type => type.Name == pair.Value).FirstOrDefault();
                            if (type != null)
                            {
                                var instance = Activator.CreateInstance(type);
                                if (instance != null)
                                {
                                    Dic.Add(pair.Key, (SystemCommand)instance);
                                    Log.Logger.DebugFormat("システムコマンド登録：{0}", pair.Value);
                                }
                            }
                        }
                    }
                    Log.Logger.Info("システムコマンドファイルの読み込みが完了しました。");
                }
                catch (Exception e)
                {
                    Log.Logger.Fatal($"システムコマンドファイルの読み込みに失敗しました。{Settings.AsString("SystemCommandFile")}を確認してください。", e);
                    throw;
                }
            }
        }
    }
}
