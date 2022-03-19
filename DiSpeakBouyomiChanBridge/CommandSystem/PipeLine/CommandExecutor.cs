using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using net.boilingwater.Application.Common.Logging;
using net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.Impl;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.CommandSystem.PipeLine
{
    /// <summary>
    /// コマンドを順次実行する
    /// </summary>
    public sealed class CommandExecutor : IDisposable
    {
        private Thread? _thread;
        private BlockingCollection<Command>? _commands = new();
        private Command? _active;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CommandExecutor()
        {
            _thread = new Thread(() =>
            {
                foreach (var command in _commands.GetConsumingEnumerable())
                {
                    try
                    {
                        command.Execute();
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex);
                    }
                    finally
                    {
                        if (_active != null)
                        {
                            lock (_active)
                            {
                                _active = null;
                            }
                        }
                    }
                }
            })
            {
                IsBackground = true
            };
            _thread.Start();
        }

        /// <summary>
        /// コマンドを実行キューに追加します
        /// </summary>
        /// <param name="command">追加するコマンド</param>
        /// <exception cref="InvalidOperationException"></exception>
        internal void Add(Command command)
        {
            if (_commands == null)
            {
                throw new InvalidOperationException();
            }
            _commands.Add(command);
        }

        /// <summary>
        /// リソースを解放します
        /// </summary>
        void IDisposable.Dispose()
        {
            if (_thread != null)
            {
                try
                {
                    _thread.Interrupt();
                }
                catch (Exception) { }
                if (_commands != null)
                {
                    _commands.CompleteAdding();
                    _commands.Dispose();
                }
                _thread.Join();
                _commands = null;
                _thread = null;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// キュー中のコマンドの複製を取得します
        /// </summary>
        /// <returns></returns>
        internal List<Command> GetCommandsInQueue()
        {
            var commands = new List<Command>();

            if (_active != null)
            {
                commands.Add((Command)_active.Clone());
            }
            if (_commands != null)
            {
                commands.AddRange(_commands.Select(command => (Command)command.Clone()));
            }

            return commands;
        }

        /// <summary>
        /// すべてのコマンドを終了する
        /// </summary>
        internal void ClearTask()
        {
            if (_commands == null)
            {
                return;
            }
            while (_commands.Count > 0)
            {
                _ = _commands.TryTake(out _);
            }
        }
    }
}
