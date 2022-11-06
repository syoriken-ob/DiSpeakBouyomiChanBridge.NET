using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

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
            XmlConfigurator.Configure(fileInfo);

            var exeName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location) ?? string.Empty;
            LogManager.GetAllRepositories()
                      .SelectMany(r => r.GetAppenders())
                      .OfType<log4net.Appender.FileAppender>()
                      .ForEach(appender =>
                      {
                          var dirName = Path.GetDirectoryName(appender.File) ?? string.Empty;
                          appender.File = Path.Combine(dirName, exeName);//出力先ファイルを設定
                          appender.ActivateOptions();
                      });
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
                var callerMethod = callerFrame.GetMethod();

                return callerMethod != null ? LogManager.GetLogger(callerMethod.DeclaringType) : LogManager.GetLogger(typeof(object));
            }
        }
    }
}
