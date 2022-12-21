using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using log4net;
using log4net.Config;

using net.boilingwater.Framework.Common.Extensions;
using net.boilingwater.Framework.Common.Setting;

namespace net.boilingwater.Framework.Common.Logging
{
    /// <summary>
    /// ログ出力を行うクラス
    /// </summary>
    /// <remarks><see cref="log4net"/>のログ出力のラッパークラス</remarks>
    public static class Log
    {
        /// <summary>
        /// ロガーの初期化処理を行います。
        /// </summary>
        /// <remarks>出力されるログファイル名を実行するアプリケーション名に設定します</remarks>
        public static void Initialize()
        {
            var fileInfo = new FileInfo(Settings.GetAppConfig("LogConfigFile"));
            if (!fileInfo.Exists)
            {
                throw new ApplicationException("ロガー設定ファイルの読み込みに失敗しました。");
            }

            try
            {
                //ファイル読み出し
                var fileContent = File.ReadAllText(fileInfo.FullName, Encoding.UTF8);
                //パラメータ置換
                var replacedFile = ReplacePlaceholder(fileContent);
                //設定
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(replacedFile));
                _ = XmlConfigurator.Configure(stream);
            }
            catch
            {
                throw new ApplicationException("ロガーの初期化に失敗しました。");
            }
        }

        /// <summary>
        /// ファイル内に指定された動的パラメータを置換します。
        /// </summary>
        /// <param name="configFileContent"></param>
        /// <returns>置換後のファイル</returns>
        private static string ReplacePlaceholder(string? configFileContent)
        {
            if (configFileContent == null)
            {
                return "";
            }

            SimpleDic<string> settings = CreateReplacePlaceholderSetting();
            settings.ForEach(pair => configFileContent = configFileContent.Replace(pair.Key, pair.Value));
            return configFileContent;
        }

        /// <summary>
        /// log4net.config内の動的パラメータを置換する辞書を作成します。
        /// </summary>
        /// <returns>置換設定辞書</returns>
        private static SimpleDic<string> CreateReplacePlaceholderSetting()
        {
            return new SimpleDic<string>
            {
                {"{{==name==}}", Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location) ?? string.Empty}
            };
        }

        /// <summary>
        /// ロガー<see cref="ILog"/>を返します
        /// </summary>
        /// <remarks>自動的に呼び出し元の関数をログに記録します</remarks>
        public static ILog Logger
        {
            get
            {
                const int callerFrameIndex = 1;
                var callerFrame = new StackFrame(callerFrameIndex);
                MethodBase? callerMethod = callerFrame.GetMethod();

                return callerMethod != null ? LogManager.GetLogger(callerMethod.DeclaringType) : LogManager.GetLogger(typeof(object));
            }
        }
    }
}
