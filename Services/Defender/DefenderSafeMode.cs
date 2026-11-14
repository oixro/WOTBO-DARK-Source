using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using WOTBO.App;
using WOTBO.App.Logging;

namespace WOTBO.Services.Defender
{
    /// <summary>
    /// Автоматический безопасный режим: задача SYSTEM на старте, метка
    /// <c>safeboot</c> в BCD и сторож, который снимает метку, если стадия
    /// не завершилась. Нужен там, где Tamper Protection не даёт остановить
    /// <c>WinDefend</c> из обычного режима.
    /// </summary>
    internal static class DefenderSafeMode
    {
        private const string LogCategory = "Defender";
        private const string StageTask = @"\WOTBO\DefenderStage";
        private const string WatchdogTask = @"\WOTBO\DefenderWatchdog";
        private const string FinalizeTask = @"\WOTBO\DefenderFinalize";

        public static bool Prepare(IAppLogger log)
        {
            return Prepare(log, DefenderEngine.PhaseApply);
        }

        public static bool Prepare(IAppLogger log, string phase)
        {
            if (!DefenderEngine.EnsurePayload(log))
            {
                return false;
            }

            // Основной триггер в safe mode: одноразовая служба из SafeBoot\Minimal.
            DefenderServiceControl.Install(log, phase);

            bool created = CreateTask(log, StageTask, DefenderPaths.StageRunner, null)
                && CreateTask(log, WatchdogTask, DefenderPaths.WatchdogRunner, "0005:00");

            if (!created)
            {
                Abort(log);
                return false;
            }

            WriteUndoBat(log);
            WriteDesktopHelpers(log);
            SetRunOnce(log);
            SetUserinit(log);

            if (!Run(log, "bcdedit.exe", "/set {current} safeboot minimal"))
            {
                log.Error(LogCategory, "не удалось включить безопасный режим (bcdedit)");
                Abort(log);
                return false;
            }

            log.Info(LogCategory, "безопасный режим подготовлен, задачи WOTBO\\DefenderStage созданы");
            return true;
        }

        /// <summary>Отменить подготовку безопасного режима без перезагрузки.</summary>
        public static void Abort(IAppLogger log)
        {
            DeleteTask(log, StageTask);
            DeleteTask(log, WatchdogTask);
            DeleteTask(log, FinalizeTask);
            DefenderServiceControl.Remove(log);
            RemoveDesktopHelpers(log);
            RemoveRunOnce(log);
            RemoveUserinit(log);
            Run(log, "bcdedit.exe", "/deletevalue {current} safeboot");
            WriteState("aborted", "ok", "safe mode cancelled");
            log.Info(LogCategory, "безопасный режим отменён");
        }

        public static void RebootNow(IAppLogger log, int seconds)
        {
            Run(
                log,
                "shutdown.exe",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/r /t {0} /f /c \"WOTBO: {1}\"",
                    seconds,
                    "restarting to continue Defender changes"));
        }

        public static bool IsArmed
        {
            get
            {
                return DefenderEngine.IsPending()
                    && File.Exists(DefenderPaths.StageRunner);
            }
        }

        private static bool CreateTask(
            IAppLogger log, string name, string runner, string delay)
        {
            string arguments = string.Format(
                CultureInfo.InvariantCulture,
                "/create /tn \"{0}\" /tr \"{1}\" /sc onstart /ru SYSTEM /f",
                name,
                runner);

            if (!string.IsNullOrEmpty(delay))
            {
                arguments += " /delay " + delay;
            }

            if (!Run(log, "schtasks.exe", arguments))
            {
                log.Error(LogCategory, "не удалось создать задачу " + name);
                return false;
            }

            return true;
        }

        private static void DeleteTask(IAppLogger log, string name)
        {
            Run(log, "schtasks.exe", "/delete /tn \"" + name + "\" /f");
        }

        private const string WinlogonKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
        private const string UserinitValue = "Userinit";
        private const string RunOnceKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
        private const string RunOnceValue = "WotboDefenderStage";

        private static void SetRunOnce(IAppLogger log)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    RunOnceKey, true))
                {
                    if (key == null)
                    {
                        log.Warn(LogCategory, "ветка RunOnce недоступна");
                        return;
                    }

                    key.SetValue(
                        RunOnceValue,
                        "cmd.exe /c \"" + DefenderPaths.StageVisibleRunner + "\"",
                        Microsoft.Win32.RegistryValueKind.String);
                }

                log.Info(LogCategory, "RunOnce-помощник добавлен");
            }
            catch (Exception exception)
            {
                log.Warn(LogCategory, "не удалось добавить RunOnce: " + exception.Message);
            }
        }

        private static void RemoveRunOnce(IAppLogger log)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    RunOnceKey, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(RunOnceValue, false);
                    }
                }
            }
            catch (Exception exception)
            {
                log.Warn(LogCategory, "не удалось убрать RunOnce: " + exception.Message);
            }
        }

        /// <summary>
        /// Добавить запуск в <c>Winlogon\Userinit</c>. Планировщик задач в
        /// безопасном режиме задачи не запускает, а Userinit работает всегда.
        /// </summary>
        private static void SetUserinit(IAppLogger log)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    WinlogonKey, true))
                {
                    if (key == null)
                    {
                        log.Warn(LogCategory, "ветка Winlogon недоступна");
                        return;
                    }

                    string current = key.GetValue(UserinitValue) as string ?? string.Empty;
                    string hook = DefenderPaths.LoginRunner;

                    if (current.IndexOf(hook, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return;
                    }

                    Directory.CreateDirectory(DefenderPaths.Root);
                    File.WriteAllText(
                        Path.Combine(DefenderPaths.Root, "userinit-original.txt"),
                        current,
                        Encoding.Default);

                    string updated = current.TrimEnd().TrimEnd(',') + "," + hook;
                    key.SetValue(UserinitValue, updated, Microsoft.Win32.RegistryValueKind.String);
                    log.Info(LogCategory, "Userinit-хук добавлен: " + updated);
                }
            }
            catch (Exception exception)
            {
                log.Warn(LogCategory, "не удалось добавить Userinit: " + exception.Message);
            }
        }

        private static void RemoveUserinit(IAppLogger log)
        {
            try
            {
                string originalFile = Path.Combine(DefenderPaths.Root, "userinit-original.txt");

                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    WinlogonKey, true))
                {
                    if (key == null)
                    {
                        return;
                    }

                    if (File.Exists(originalFile))
                    {
                        key.SetValue(
                            UserinitValue,
                            File.ReadAllText(originalFile),
                            Microsoft.Win32.RegistryValueKind.String);
                        File.Delete(originalFile);
                        return;
                    }

                    string current = key.GetValue(UserinitValue) as string;
                    string hook = DefenderPaths.LoginRunner;
                    if (current != null && current.IndexOf(hook, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string cleaned = current.Replace("," + hook, string.Empty).Replace(hook, string.Empty);
                        key.SetValue(UserinitValue, cleaned, Microsoft.Win32.RegistryValueKind.String);
                    }
                }
            }
            catch (Exception exception)
            {
                log.Warn(LogCategory, "не удалось убрать Userinit: " + exception.Message);
            }
        }

        private static void WriteDesktopHelpers(IAppLogger log)
        {
            try
            {
                WriteDesktopScript(
                    DefenderPaths.DesktopApplyScript,
                    DefenderPaths.StageVisibleRunner,
                    "WOTBO: disabling Windows Defender");
                WriteDesktopScript(
                    DefenderPaths.DesktopRestoreScript,
                    DefenderPaths.RestoreVisibleRunner,
                    "WOTBO: restoring Windows Defender");

                var undo = new StringBuilder();
                undo.AppendLine("@echo off");
                undo.AppendLine("title WOTBO: undo safe mode");
                undo.AppendLine("bcdedit /deletevalue {current} safeboot");
                undo.AppendLine("shutdown /r /t 5 /f");
                File.WriteAllText(DefenderPaths.DesktopUndoSafeMode, undo.ToString(), Encoding.Default);
                log.Info(LogCategory, "ярлыки безопасного режима созданы на рабочем столе");
            }
            catch (Exception exception)
            {
                log.Warn(LogCategory, "не удалось создать ярлыки: " + exception.Message);
            }
        }

        private static void WriteDesktopScript(string path, string runner, string title)
        {
            var text = new StringBuilder();
            text.AppendLine("@echo off");
            text.AppendLine("title " + title);
            text.AppendLine("echo " + title);
            text.AppendLine("call \"" + runner + "\"");
            text.AppendLine("echo.");
            text.AppendLine("echo Log: %WINDIR%\\Temp\\wotbo-defender.log");
            text.AppendLine("pause");
            File.WriteAllText(path, text.ToString(), Encoding.Default);
        }

        private static void RemoveDesktopHelpers(IAppLogger log)
        {
            foreach (string path in new[]
            {
                DefenderPaths.DesktopApplyScript,
                DefenderPaths.DesktopRestoreScript,
                DefenderPaths.DesktopUndoSafeMode,
            })
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception exception)
                {
                    log.Warn(LogCategory, "не удалось удалить " + path + ": " + exception.Message);
                }
            }
        }

        private static void WriteUndoBat(IAppLogger log)
        {
            try
            {
                var text = new StringBuilder();
                text.AppendLine("@echo off");
                text.AppendLine("bcdedit /deletevalue {current} safeboot");
                text.AppendLine("shutdown /r /t 5 /f");
                File.WriteAllText(DefenderPaths.UndoBat, text.ToString(), Encoding.Default);
            }
            catch (Exception exception)
            {
                log.Warn(LogCategory, "не удалось записать аварийный bat: " + exception.Message);
            }
        }

        private static void WriteState(string stage, string result, string message)
        {
            try
            {
                string[] lines =
                {
                    "stage=" + stage,
                    "result=" + result,
                    "message=" + message,
                    "mode=safemode",
                    "phase=abort",
                    "updated=" + DateTime.Now.ToString("o", CultureInfo.InvariantCulture),
                };
                File.WriteAllLines(DefenderPaths.StateFile, lines, Encoding.ASCII);
            }
            catch
            {
            }
        }

        private static bool Run(IAppLogger log, string fileName, string arguments)
        {
            try
            {
                log.Debug(LogCategory, "[cmd] " + fileName + " " + arguments);

                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit(30000);

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        log.Debug(LogCategory, "[out] " + output.Trim());
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        log.Warn(LogCategory, "[err] " + error.Trim());
                    }

                    return process.ExitCode == 0;
                }
            }
            catch (Exception exception)
            {
                log.Error(LogCategory, "команда не выполнена: " + fileName, exception);
                return false;
            }
        }
    }
}
