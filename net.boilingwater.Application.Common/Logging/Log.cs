using System.Diagnostics;

using log4net;

namespace net.boilingwater.Application.Common.Logging
{
    /// <summary>
    /// ログ出力を行うクラス
    /// </summary>
    /// <remarks><see cref="log4net"/>のログ出力のラッパークラス</remarks>
    public static class Log
    {
        /// <summary>
        /// ロガー<see cref="log4net.ILog"/>を返します
        /// </summary>
        /// <remarks>自動的に呼び出し元の関数をログに記録します</remarks>
        public static ILog Logger
        {
            get
            {
                const int callerFrameIndex = 1;
                var callerFrame = new StackFrame(callerFrameIndex);
                var callerMethod = callerFrame.GetMethod();
                return LogManager.GetLogger(callerMethod.DeclaringType);
            }
        }
    }
}
