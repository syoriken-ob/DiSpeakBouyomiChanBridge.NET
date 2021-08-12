using System.Collections.Generic;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.External
{
    public abstract class ExecutableCommandFactory
    {
        public static ExecutableCommandFactory Factory { get; protected set; }

        public abstract void Reload();

        public abstract IEnumerable<ExecutableCommand> CreateExecutableCommands(ref string input);
    }
}