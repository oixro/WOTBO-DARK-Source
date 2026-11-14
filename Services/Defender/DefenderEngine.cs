using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using WOTBO.App.Logging;
using WOTBO.Services.Processes;

namespace WOTBO.Services.Defender
{
    /// <summary>Результат одного запуска движка (<c>state.ini</c>).</summary>
    internal sealed class DefenderRunResult
    {
        public string Stage { get; set; }

        public string Result { get; set; }

        public string Message { get; set; }

        public string Mode { get; set; }

        public string Phase { get; set; }

        public bool Ok
        {
            get { return string.Equals(Result, "ok", StringComparison.OrdinalIgnoreCase); }
        }

        public bool Blocked
        {
            get { return string.Equals(Result, "blocked", StringComparison.OrdinalIgnoreCase); }
        }
    }

    /// <summary>
    /// Запуск PowerShell-движка отключения Defender и чтение его состояния.
    /// Весь привилегированный код живёт в <c>Resources\defender-engine.ps1</c>,
    /// чтобы одна и та же логика работала и из WOTBO, и из задачи в безопасном
    /// режиме.
    /// </summary>
    internal static class DefenderEngine
    {
        public const string PhasePrepare = "prepare";
        public const string PhaseApply = "apply";
        public const string PhaseRestore = "restore";
        public const string PhaseVerify = "verify";

        private const string LogCategory = "Defender";
        private const string ResourceSuffix = "defender-engine.ps1";

        /// <summary>Распаковать движок и раннеры задач в ProgramData.</summary>
        public static bool EnsurePayload(IAppLogger log)
        {
            try
            {
                Directory.CreateDirectory(DefenderPaths.Root);
                Directory.CreateDirectory(DefenderPaths.LogDirectory);

                string script = LoadScript();
                if (string.IsNullOrEmpty(script))
                {
                    log.Error(LogCategory, "встроенный скрипт движка Defender не найден");
                    return false;
                }

                File.WriteAllText(DefenderPaths.EngineScript, script, new UTF8Encoding(true));

                File.WriteAllText(
                    DefenderPaths.StageRunner,
                    BuildRunner(PhaseApply, true, false),
                    Encoding.Default);
                File.WriteAllText(
                    DefenderPaths.WatchdogRunner,
                    BuildRunner("watchdog", false, false),
                    Encoding.Default);
                File.WriteAllText(
                    DefenderPaths.StageVisibleRunner,
                    BuildRunner(PhaseApply, true, true),
                    Encoding.Default);
                File.WriteAllText(
                    DefenderPaths.RestoreVisibleRunner,
                    BuildRunner(PhaseRestore, true, true),
                    Encoding.Default);
                File.WriteAllText(
                    DefenderPaths.LoginRunner,
                    BuildLoginRunner(),
                    Encoding.Default);
                File.WriteAllText(
                    DefenderPaths.FinalizeRunner,
                    BuildRunner("finalize", false, false),
                    Encoding.Default);
                return true;
            }
            catch (Exception exception)
            {
                log.Error(LogCategory, "не удалось распаковать движок Defender", exception);
                return false;
            }
        }

        /// <summary>Запустить фазу движка и дождаться результата.</summary>
        public static DefenderRunResult Run(
            IProcessService processes,
            IAppLogger log,
            string phase,
            bool safeMode,
            int timeoutMs)
        {
            if (!EnsurePayload(log))
            {
                return null;
            }

            string powershell = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                @"System32\WindowsPowerShell\v1.0\powershell.exe");

            string arguments = string.Format(
                CultureInfo.InvariantCulture,
                "-NoProfile -ExecutionPolicy Bypass -File \"{0}\" -Phase {1}{2}",
                DefenderPaths.EngineScript,
                phase,
                safeMode ? " -SafeMode" : string.Empty);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = powershell,
                    Arguments = arguments,
                    WorkingDirectory = DefenderPaths.Root,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            log.Debug(LogCategory, "engine: " + e.Data);
                        }
                    };
                    process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            log.Warn(LogCategory, "engine: " + e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (!process.WaitForExit(timeoutMs))
                    {
                        log.Warn(LogCategory, "движок не завершился за отведённое время: " + phase);
                    }
                    else
                    {
                        process.WaitForExit();
                    }
                }
            }
            catch (Exception exception)
            {
                log.Error(LogCategory, "не удалось запустить движок Defender: " + phase, exception);
                return null;
            }

            return ReadState();
        }

        /// <summary>Прочитать последнее состояние без запуска движка.</summary>
        public static DefenderRunResult ReadState()
        {
            if (!File.Exists(DefenderPaths.StateFile))
            {
                return null;
            }

            try
            {
                Dictionary<string, string> values = ReadKeyValue(DefenderPaths.StateFile);
                var result = new DefenderRunResult();
                string value;

                if (values.TryGetValue("stage", out value)) result.Stage = value;
                if (values.TryGetValue("result", out value)) result.Result = value;
                if (values.TryGetValue("message", out value)) result.Message = value;
                if (values.TryGetValue("mode", out value)) result.Mode = value;
                if (values.TryGetValue("phase", out value)) result.Phase = value;
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Фаза уже подготовлена, но ещё не применена (ждёт перезагрузку).</summary>
        public static bool IsPending()
        {
            DefenderRunResult state = ReadState();
            if (state == null || string.IsNullOrEmpty(state.Stage))
            {
                return false;
            }

            return string.Equals(state.Stage, "prepared", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state.Stage, "applying", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state.Stage, "blocked", StringComparison.OrdinalIgnoreCase);
        }

        public static Dictionary<string, string> ReadVerify()
        {
            if (!File.Exists(DefenderPaths.VerifyFile))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return ReadKeyValue(DefenderPaths.VerifyFile);
        }

        /// <summary>Последние строки лога — для сообщений в UI.</summary>
        public static string ReadLastLog(int lines)
        {
            try
            {
                if (!Directory.Exists(DefenderPaths.LogDirectory))
                {
                    return string.Empty;
                }

                FileInfo file = new DirectoryInfo(DefenderPaths.LogDirectory)
                    .GetFiles("*.log")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (file == null)
                {
                    return string.Empty;
                }

                string[] all = File.ReadAllLines(file.FullName);
                int skip = Math.Max(0, all.Length - Math.Max(1, lines));
                return string.Join(Environment.NewLine, all.Skip(skip));
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>Скопировать журнал и экспорт в штатный каталог бэкапов твика.</summary>
        public static void MirrorBackup(IAppLogger log)
        {
            try
            {
                Directory.CreateDirectory(DefenderPaths.BackupMirror);

                if (File.Exists(DefenderPaths.JournalFile))
                {
                    File.Copy(
                        DefenderPaths.JournalFile,
                        Path.Combine(DefenderPaths.BackupMirror, "journal.json"),
                        true);
                }

                foreach (string file in Directory.GetFiles(Path.Combine(DefenderPaths.Root, "reg")))
                {
                    File.Copy(file, Path.Combine(DefenderPaths.BackupMirror, Path.GetFileName(file)), true);
                }
            }
            catch (Exception exception)
            {
                log.Warn(LogCategory, "не удалось скопировать бэкап Defender: " + exception.Message);
            }
        }

        private static string BuildRunner(string phase, bool safeMode, bool visible)
        {
            string powershell = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                @"System32\WindowsPowerShell\v1.0\powershell.exe");

            var builder = new StringBuilder();
            builder.AppendLine("@echo off");
            builder.AppendLine("set LOG=%WINDIR%\\Temp\\wotbo-defender-stage.log");
            builder.AppendLine("set WOTBO_PHASE=" + phase);
            builder.AppendLine("if exist \"" + DefenderPaths.ServicePhaseFile + "\" set /p WOTBO_PHASE=<\"" + DefenderPaths.ServicePhaseFile + "\"");
            builder.AppendLine("echo [%DATE% %TIME%] start " + phase + " >> \"%LOG%\"");
            builder.Append('"').Append(powershell).Append('"');
            builder.Append(" -NoProfile -ExecutionPolicy Bypass");
            if (!visible)
            {
                builder.Append(" -WindowStyle Hidden");
            }
            builder.Append(" -File \"").Append(DefenderPaths.EngineScript).Append('"');
            if (string.Equals(phase, "watchdog", StringComparison.OrdinalIgnoreCase))
            {
                builder.Append(" -Phase watchdog");
            }
            else
            {
                // Фаза берётся из файла, который пишет взвод safe mode:
                // иначе fallback-раннеры после отката снова применили бы твик.
                builder.Append(" -Phase %WOTBO_PHASE%");
            }
            if (safeMode)
            {
                builder.Append(" -SafeMode");
            }

            if (visible)
            {
                builder.AppendLine();
            }
            else
            {
                builder.AppendLine(" >> \"%LOG%\" 2>&1");
            }

            builder.AppendLine("echo [%DATE% %TIME%] exit %ERRORLEVEL% >> \"%LOG%\"");
            return builder.ToString();
        }

        /// <summary>
        /// Хук <c>Winlogon\Userinit</c>: планировщик в safe mode задачи не
        /// запускает, а Userinit выполняется при любом входе. Скрипт сразу
        /// отдаёт управление оболочке и запускает видимый раннер.
        /// </summary>
        private static string BuildLoginRunner()
        {
            var builder = new StringBuilder();
            builder.AppendLine("@echo off");
            builder.AppendLine("set LOG=%WINDIR%\\Temp\\wotbo-defender-stage.log");
            builder.AppendLine("echo [%DATE% %TIME%] userinit hook start >> \"%LOG%\"");
            builder.AppendLine("start \"\" \"" + DefenderPaths.StageVisibleRunner + "\"");
            builder.AppendLine("echo [%DATE% %TIME%] userinit hook launched >> \"%LOG%\"");
            builder.AppendLine("exit /b 0");
            return builder.ToString();
        }

        private static string LoadScript()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string name = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static Dictionary<string, string> ReadKeyValue(string path)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 1).Trim();
                if (key.Length > 0)
                {
                    values[key] = value;
                }
            }

            return values;
        }
    }
}
