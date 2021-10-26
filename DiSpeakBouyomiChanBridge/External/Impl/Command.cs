using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using net.boilingwater.DiSpeakBouyomiChanBridge.Config;
using net.boilingwater.DiSpeakBouyomiChanBridge.Http;
using net.boilingwater.DiSpeakBouyomiChanBridge.Log;
using net.boilingwater.Utils.Extention;

namespace net.boilingwater.DiSpeakBouyomiChanBridge.External.Impl
{
    public class Command : ExecutableCommand, ICloneable
    {
        public static List<Process> ExecuteProcesses { get; private set; } = new();
        public string CommandTitle { get; set; }
        public bool Immediate { get; set; }
        public string Regex { get; set; }
        public string[] ReplacePattern { get; set; }
        public string[] RunCommand { get; set; }
        public Dictionary<string, string> Env { get; set; }
        public string Path { get; set; }
        public Dictionary<string, string> StdInOut { get; set; }
        public string ExecutionComment { get; set; }
        public string CompleteComment { get; set; }

        public object Clone() => new Command()
        {
            Immediate = Immediate,
            CommandTitle = CommandTitle,
            Regex = new(Regex),
            ReplacePattern = (string[])ReplacePattern.Clone(),
            RunCommand = (string[])RunCommand.Clone(),
            Env = new Dictionary<string, string>(Env),
            Path = new(Path),
            StdInOut = new Dictionary<string, string>(StdInOut),
            ExecutionComment = new(ExecutionComment),
            CompleteComment = new(CompleteComment)
        };

        public override void Execute()
        {
            ProcessStartInfo info = new()
            {
                WorkingDirectory = Path,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                //StandardOutputEncoding = Encoding.GetEncoding(Setting.Get("ShellEncoding")),
                //StandardInputEncoding = Encoding.GetEncoding(Setting.Get("ShellEncoding")),
                UseShellExecute = false,
                CreateNoWindow = false,
                FileName = Setting.Instance.AsString("ShellPath"),
                Arguments = $"{Setting.Instance.AsString("ShellOption")} \"{string.Join(" ", RunCommand)}\""
            };

            //環境変数設定
            if (Env.Any())
            {
                Env.ForEach(pair =>
                {
                    if (info.Environment.ContainsKey(pair.Key))
                    {
                        info.Environment.Remove(pair.Key);
                    }
                    info.Environment.Add(pair);
                });
            }

            try
            {
                using var p = Process.Start(info);

                lock (((ICollection)ExecuteProcesses).SyncRoot)
                {
                    ExecuteProcesses.Add(p);
                }

                LoggerPool.Logger.Info($"Run \"{CommandTitle}\" Command.");
                LoggerPool.Logger.Debug($"Env with({string.Join(",", info.Environment.Select(e => $"{e.Key}:{e.Value}"))})");

                if (!string.IsNullOrEmpty(ExecutionComment))
                {
                    LoggerPool.Logger.Info($"{CommandTitle} - {ExecutionComment}");
                    HttpClientForBouyomiChan.Instance.SendToBouyomiChan(ExecutionComment);
                }

                p.ErrorDataReceived += (sender, e) => LoggerPool.Logger.Debug($"{CommandTitle}({p.ProcessName}-{p.Id}) -> {e.Data}");
                p.BeginErrorReadLine();

                using var pOutput = p.StandardOutput;
                using var pInput = p.StandardInput;
                pInput.AutoFlush = true;

                string pOut = null;
                while ((pOut = pOutput.ReadLine()) != null)
                {
                    LoggerPool.Logger.Debug($"{CommandTitle}({p.ProcessName}-{p.Id}) -> {pOut}");

                    if (StdInOut != null && StdInOut.Any())
                    {
                        StdInOut.ForEach(regexPair =>
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(pOut, regexPair.Key))
                            {
                                pInput.WriteLine(regexPair.Value);
                                //pInput.Flush();
                                LoggerPool.Logger.Debug($"{CommandTitle}({p.ProcessName}-{p.Id}) <- {regexPair.Value}");
                            }
                        });
                    }
                }

                p.WaitForExit();
                lock (((ICollection)ExecuteProcesses).SyncRoot)
                {
                    ExecuteProcesses.Remove(p);
                }
                LoggerPool.Logger.Info($"Finish \"{CommandTitle}\" Command.");

                if (!string.IsNullOrEmpty(CompleteComment))
                {
                    HttpClientForBouyomiChan.Instance.SendToBouyomiChan(CompleteComment);
                }
            }
            catch (InvalidOperationException e)
            {
                LoggerPool.Logger.Info($"\"{CommandTitle}\" Command has been Killed.", e);
                HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.Instance.AsString("ErrorOccurrence"));
            }
            catch (Exception e)
            {
                LoggerPool.Logger.Error($"Couldn't run \"{CommandTitle}\" Command", e);
                HttpClientForBouyomiChan.Instance.SendToBouyomiChan(MessageSetting.Instance.AsString("ErrorOccurrence"));
            }
        }

        public static void KillProcess()
        {
            lock (((ICollection)ExecuteProcesses).SyncRoot)
            {
                ExecuteProcesses.ForEach(process =>
                {
                    try
                    {
                        process.Kill(true);
                    }
                    catch (Exception) { }
                    try
                    {
                        process.Close();
                    }
                    catch (Exception) { }
                    try
                    {
                        process.Dispose();
                    }
                    catch (Exception) { }
                });

            }
        }
    }
}