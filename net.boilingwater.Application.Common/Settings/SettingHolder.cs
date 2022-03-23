using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml;

using net.boilingwater.Application.Common.Logging;

namespace net.boilingwater.Application.Common.Settings
{
    /// <summary>
    /// アプリケーション設定値を保持するクラス
    /// </summary>
    public partial class SettingHolder
    {
        private static readonly string SEARCH_PATTERN = "*-Setting.xml";

        internal static SettingHolder Instance { get; private set; }

        private readonly SimpleDic<string> Settings;

        internal string this[string key] => Settings[key.ToLower()];

        private SettingHolder()
        {
            Settings = new();
            LoadSetting();
            LoadEnvironmentSetting();
        }

        private void LoadSetting()
        {
            try
            {
                foreach (var file in GetSettingFiles())
                {
                    var doc = new XmlDocument();
                    doc.Load(file);
                    Log.Logger.Debug($"読み込み：{file}");

                    var items = doc.SelectNodes("/Settings/Item");
                    foreach (XmlNode item in items)
                    {
                        var attr = item.Attributes;
                        Settings[attr["key"].Value.ToLower()] = attr["value"].Value;
                    }
                }

                Log.Logger.Info("設定ファイルの読み込みが完了しました。");
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal("設定ファイルの読み込みに失敗しました。", ex);
                throw;
            }
        }

        private void LoadEnvironmentSetting()
        {
            try
            {
                var file = GetEnvironmentSettingFile();
                if (string.IsNullOrEmpty(file))
                {
                    return;
                }

                var doc = new XmlDocument();
                doc.Load(file);
                Log.Logger.Debug($"読み込み：{file}");

                var items = doc.SelectNodes("/Settings/Item");
                foreach (XmlNode item in items)
                {
                    var attr = item.Attributes;
                    Settings[attr["key"].Value.ToLower()] = attr["value"].Value;
                }

                Log.Logger.Info("環境設定上書きファイルの読み込みが完了しました。");
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal("環境設定上書きファイルの読み込みに失敗しました。", ex);
            }
        }

        /// <summary>
        /// 設定ファイルの読み込みを行います。
        /// </summary>
        /// <remarks>こちらを呼び出さないと<see cref="net.boilingwater.Application.Common.Settings.Settings"/>を利用できません</remarks>
        public static void Initialize() => Instance = new SettingHolder();

        private static string[] GetSettingFiles()
        {
            return Directory.GetFiles(ConfigurationManager.AppSettings["SettingFileFolder"], SEARCH_PATTERN, SearchOption.AllDirectories)
                .Select(path => Path.GetFullPath(path))
                .ToArray();
        }

        private static string GetEnvironmentSettingFile()
        {
            return Directory.GetFiles(ConfigurationManager.AppSettings["OverrideFileFolder"], ConfigurationManager.AppSettings["EnvironmentSettingFile"], SearchOption.AllDirectories)
                .Select(path => Path.GetFullPath(path))
                .FirstOrDefault();
        }
    }
}
