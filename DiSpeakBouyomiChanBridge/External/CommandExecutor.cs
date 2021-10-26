using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl;
using net.boilingwater.DiSpeakBouyomiChanBridge.Log;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.External
{
    public sealed class CommandExecutor : TaskScheduler, IDisposable
    {
        private static TaskFactory EmergencyFactory { get; set; } = new TaskFactory(new CommandExecutor());
        private static TaskFactory CommonFactory { get; set; } = new TaskFactory(new CommandExecutor());

        //----------------------------------------------------------------

        private BlockingCollection<Task> _tasks = new();
        private Thread _thread;

        public CommandExecutor() => DoTask();

        private void DoTask()
        {
            _thread = new Thread(() =>
            {
                foreach (var Task in _tasks.GetConsumingEnumerable())
                {
                    try
                    {
                        TryExecuteTask(Task);
                    }
                    catch (Exception ex)
                    {
                        LoggerPool.Logger.Error(ex);
                    }
                }
            })
            {
                IsBackground = true
            };
            _thread.Start();
        }

        protected override void QueueTask(Task task) => _tasks.Add(task);

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (Thread.CurrentThread != _thread)
            {
                return false;
            }

            return TryExecuteTask(task);
        }

        public override int MaximumConcurrencyLevel => 1;

        protected override IEnumerable<Task> GetScheduledTasks() => _tasks.ToArray();

        public void Dispose()
        {
            if (_thread != null)
            {
                try
                {
                    _thread.Interrupt();
                }
                catch (Exception) { }
                _tasks.CompleteAdding();
                _thread.Join();
                _tasks.Dispose();
                _tasks = null;
                _thread = null;
            }
        }

        public void ClearTask()
        {
            if (_tasks == null)
            {
                return;
            }

            while (_tasks.Count > 0)
            {
                _tasks.TryTake(out _);
            }
        }

        public static void AddCommand(Command command)
        {
            if (command.Immediate)
            {
                EmergencyFactory.StartNew(() => command.Execute());
                LoggerPool.Logger.Info("Add Command Emergency Execute Que.");
            }
            else
            {
                CommonFactory.StartNew(() => command.Execute());
                LoggerPool.Logger.Info("Add Command Execute Que.");
            }
        }

        public static void ShutdownThreads()
        {
            Command.KillProcess();
            ((CommandExecutor)CommonFactory.Scheduler).ClearTask();
            ((CommandExecutor)EmergencyFactory.Scheduler).ClearTask();
        }
    }
}