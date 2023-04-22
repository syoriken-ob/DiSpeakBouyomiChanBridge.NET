using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core.Logging;
using net.boilingwater.Framework.Core.Utils;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl.Factory
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
            MatchCollection matches = Regex.Matches(input, regex);

            foreach (Match match in matches)
            {
                var key = Regex.Escape(match.Value);
                if (Dic.ContainsKey(key))
                {
                    input = input.Replace(match.Value, "");
                    ExecutableCommand? instance = Dic[key];
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
                    var commandFilePath = Path.Combine(Directory.GetCurrentDirectory(), Settings.GetAppConfig("CommandFileFolder"), Settings.AsString("SystemCommandFile"));
                    Dictionary<string, string> dic = SerializeUtil.DeserializeYaml<Dictionary<string, string>>(File.ReadAllText(commandFilePath), false);
                    Log.Logger.Debug($"読み込み：{commandFilePath}");

                    if (dic != null)
                    {
                        IEnumerable<Type> SystemCommandTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(SystemCommand)));
                        foreach (KeyValuePair<string, string> pair in dic)
                        {
                            Type? type = SystemCommandTypes.Where(type => type.Name == pair.Value).FirstOrDefault();
                            if (type != null)
                            {
                                var instance = Activator.CreateInstance(type);
                                if (instance != null)
                                {
                                    Dic[pair.Key] = (SystemCommand)instance;
                                    Log.Logger.Debug($"システムコマンド登録：{pair.Value}");
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
