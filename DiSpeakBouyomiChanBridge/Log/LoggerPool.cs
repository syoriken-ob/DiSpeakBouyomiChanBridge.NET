using log4net;
using System.Diagnostics;
using System.Reflection;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.Log
{
    public static class LoggerPool
    {
        public static ILog Logger
        {
            get
            {
                const int callerFrameIndex = 1;
                StackFrame callerFrame = new(callerFrameIndex);
                MethodBase callerMethod = callerFrame.GetMethod();
                return LogManager.GetLogger(callerMethod.DeclaringType);
            }
        }
    }
}