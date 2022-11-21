using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml;

using net.boilingwater.Framework.Common.Extensions;
using net.boilingwater.Framework.Common.Logging;

namespace net.boilingwater.Framework.Common.Setting
{
    /// <summary>
    /// アプリケーション設定値を保持するクラス
    /// </summary>
    public partial class SettingHolder
    {
        private static readonly string SEARCH_PATTERN = "*-Setting.xml";

        private readonly SimpleDic<string> Settings;

        internal string this[string key] => Settings[key.ToLower()] ?? string.Empty;

        internal SettingHolder()
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
                    if (items == null)
                    {
                        return;
                    }
                    foreach (XmlNode item in items)
                    {
                        var attr = item.Attributes;
                        if (attr == null)
                        {
                            continue;
                        }
                        var key = attr["key"];
                        var value = attr["value"];
                        if (key == null || value == null)
                        {
                            continue;
                        }
                        Settings[key.Value.ToLower()] = value.Value;
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
                if (!file.HasValue())
                {
                    return;
                }

                var doc = new XmlDocument();
                doc.Load(file);
                Log.Logger.Debug($"読み込み：{file}");

                var items = doc.SelectNodes("/Settings/Item");
                if (items == null)
                {
                    return;
                }

                foreach (XmlNode item in items)
                {
                    var attr = item.Attributes;
                    if (attr == null)
                    {
                        continue;
                    }
                    var key = attr["key"];
                    var value = attr["value"];
                    if (key == null || value == null)
                    {
                        continue;
                    }
                    Settings[key.Value.ToLower()] = value.Value;
                }

                Log.Logger.Info("環境設定上書きファイルの読み込みが完了しました。");
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal("環境設定上書きファイルの読み込みに失敗しました。", ex);
            }
        }

        private static string[] GetSettingFiles()
        {
            var folderPath = ConfigurationManager.AppSettings["SettingFileFolder"];
            if (folderPath == null)
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(folderPath, SEARCH_PATTERN, SearchOption.AllDirectories)
                            .Select(path => Path.GetFullPath(path))
                            .ToArray();
        }

        private static string GetEnvironmentSettingFile()
        {
            var folderPath = ConfigurationManager.AppSettings["OverrideFileFolder"];
            var envFilePath = ConfigurationManager.AppSettings["EnvironmentSettingFile"];
            if (folderPath == null || envFilePath == null)
            {
                return string.Empty;
            }

            var envFile = Directory.GetFiles(folderPath, envFilePath, SearchOption.AllDirectories)
                        .Select(path => Path.GetFullPath(path))
                        .FirstOrDefault();

            return envFile ?? string.Empty;
        }
    }
}
