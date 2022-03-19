using System;

using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Handle;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem
{
    internal class CommandHandlingService
    {
        private static AbstractCommandHandler? _handler;

        internal static void Initialize(AbstractCommandHandler handler) => _handler = handler;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="Initialize(AbstractCommandHandler)"/>を先に呼び出していない場合発生します
        /// </exception>
        internal static void Handle(string message)
        {
            if (_handler == null)
            {
                throw new InvalidOperationException("コマンドハンドラの初期化が行われていません。");
            }

            _handler.ExecutePreProcess(ref message);
            _handler.Handle(ref message);
            _handler.ExecutePostProcess(ref message);
        }
    }
}
