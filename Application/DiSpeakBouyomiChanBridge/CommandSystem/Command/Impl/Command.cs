using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RegularExpression = System.Text.RegularExpressions.Regex;

using net.boilingwater.BusinessLogic.VoiceReadout.HttpClients;
using net.boilingwater.Framework.Common.Setting;
using net.boilingwater.Framework.Core.Extensions;
using net.boilingwater.Framework.Core.Logging;
using net.boilingwater.Framework.Core.Utils;

namespace net.boilingwater.Application.DiSpeakBouyomiChanBridge.CommandSystem.Impl
{
    /// <summary>
    /// Discordの入力に応じた処理を定義したコマンド
    /// </summary>
    public class Command : ExecutableCommand, ICloneable
    {
        /// <summary>
        /// 実行中のプロセス
        /// </summary>
        internal static Process? ExecuteProcess { get; private set; }

        /// <summary>
        /// コマンドのタイトル
        /// </summary>
        public string CommandTitle { get; set; } = string.Empty;

        /// <summary>
        /// 即時実行コマンドかどうか
        /// </summary>
        public bool Immediate { get; set; }

        /// <summary>
        /// コマンドを検出する正規表現
        /// </summary>
        public string? Regex { get; set; } = string.Empty;

        /// <summary>
        /// 正規表現中の置換パターン
        /// </summary>
        public string[] ReplacePattern { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 実行コマンド
        /// </summary>
        public string[] RunCommand { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 環境変数
        /// </summary>
        public Dictionary<string, string?> Env { get; set; } = new();

        /// <summary>
        /// コマンド実行時のカレントディレクトリ
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// コマンド実行時の標準出力に応じた標準入力
        /// </summary>
        public Dictionary<string, string> StdInOut { get; set; } = new();

        /// <summary>
        /// コマンド実行時の開始メッセージ
        /// </summary>
        public string ExecutionComment { get; set; } = string.Empty;

        /// <summary>
        /// コマンド実行時の完了メッセージ
        /// </summary>
        public string CompleteComment { get; set; } = string.Empty;

        /// <summary>
        /// プロセス名
        /// </summary>
        /// <remarks>実行開始後のみ取得可能</remarks>
        private string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// プロセスID
        /// </summary>
        /// <remarks>実行開始後のみ取得可能</remarks>
        private int PID { get; set; } = -1;

        /// <summary>
        /// Commandオブジェクトを複製
        /// </summary>
        /// <returns><see cref="Command"/></returns>
        public object Clone() => new Command()
        {
            Immediate = Immediate,
            CommandTitle = CommandTitle,
            Regex = new(Regex),
            ReplacePattern = (string[])ReplacePattern.Clone(),
            RunCommand = (string[])RunCommand.Clone(),
            Env = new Dictionary<string, string?>(Env),
            Path = new(Path),
            StdInOut = new Dictionary<string, string>(StdInOut),
            ExecutionComment = new(ExecutionComment),
            CompleteComment = new(CompleteComment)
        };

        /// <summary>
        /// コマンドを実行します
        /// </summary>
        public override void Execute()
        {
            ProcessStartInfo info = new()
            {
                WorkingDirectory = Path,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = false,
                FileName = Settings.AsString("ShellPath"),
                Arguments = $"{Settings.AsString("ShellOption")} \"{string.Join(" ", RunCommand)}\""
            };

            //環境変数設定
            if (Env.Any())
            {
                Env.ForEach(pair =>
                {
                    if (info.Environment.ContainsKey(pair.Key))
                    {
                        _ = info.Environment.Remove(pair.Key);
                    }
                    info.Environment.Add(pair);
                });
            }

            try
            {
                using Process p = Process.Start(info) ?? throw new ArgumentException("コマンドが不正です");

                ExecuteProcess = p;

                Log.Logger.Info($"Run \"{CommandTitle}\" Command.");
                Log.Logger.Debug($"Env with({string.Join(",", info.Environment.Select(e => $"{e.Key}:{e.Value}"))})");

                if (!string.IsNullOrEmpty(ExecutionComment))
                {
                    Log.Logger.Info($"{CommandTitle} - {ExecutionComment}");
                    HttpClientForReadOut.Instance?.ReadOut(ExecutionComment);
                }

                p.ErrorDataReceived += ErrorDataReceivedHandler;
                p.BeginErrorReadLine();

                using StreamReader pOutput = p.StandardOutput;
                using StreamWriter pInput = p.StandardInput;
                pInput.AutoFlush = true;

                string? pOut = null;
                while ((pOut = pOutput.ReadLine()) != null)
                {
                    Log.Logger.Debug($"{CommandTitle}({p.ProcessName}-{p.Id}) -> {pOut}");

                    if (StdInOut != null && StdInOut.Any())
                    {
                        StdInOut.ForEach(regexPair =>
                        {
                            if (RegularExpression.IsMatch(pOut, regexPair.Key))
                            {
                                pInput.WriteLine(regexPair.Value);
                                Log.Logger.Debug($"{CommandTitle}({p.ProcessName}-{p.Id}) <- {regexPair.Value}");
                            }
                        });
                    }
                }

                p.WaitForExit();

                ExecuteProcess = p;

                Log.Logger.Info($"Finish \"{CommandTitle}\" Command.");

                if (!string.IsNullOrEmpty(CompleteComment))
                {
                    HttpClientForReadOut.Instance?.ReadOut(CompleteComment);
                }
                ExecuteProcess = null;
            }
            catch (InvalidOperationException e)
            {
                Log.Logger.Info($"\"{CommandTitle}\" Command has been Killed.", e);
                HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.ErrorOccurrence"));
            }
            catch (Exception e)
            {
                Log.Logger.Error($"Couldn't run \"{CommandTitle}\" Command", e);
                HttpClientForReadOut.Instance?.ReadOut(Settings.AsString("Message.ErrorOccurrence"));
            }
        }

        private void ErrorDataReceivedHandler(object sender, DataReceivedEventArgs e) => Log.Logger.Debug($"{CastUtil.ToString(CommandTitle)}({CastUtil.ToString(ProcessName)}-{CastUtil.ToString(PID)}) -> {e.Data}");

        /// <summary>
        /// 実行中のコマンドを強制終了します
        /// </summary>
        public static void KillProcess()
        {
            if (ExecuteProcess != null)
            {
                try
                {
                    ExecuteProcess.Kill(true);
                }
                catch (Exception) { }
                try
                {
                    ExecuteProcess.Close();
                }
                catch (Exception) { }
                try
                {
                    ExecuteProcess.Dispose();
                }
                catch (Exception) { }
            }
        }
    }
}
