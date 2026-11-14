using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WOTBO.App;
using WOTBO.App.Logging;
using WOTBO.Services.Processes;

namespace WOTBO.Services.Defender
{
    /// <summary>
    /// Сервис-помощник для безопасного режима.
    ///
    /// Планировщик задач в safe mode задачи не запускает, поэтому движок
    /// отключателя должен стартовать как служба: сервисы из
    /// <c>SafeBoot\Minimal</c> в safe mode запускает сам SCM, под SYSTEM.
    /// Только SYSTEM может писать защищённые ключи служб Defender.
    ///
    /// Сервис одноразовый: выполняет движок и удаляет себя.
    /// </summary>
    internal sealed class DefenderServiceHost : System.ServiceProcess.ServiceBase
    {
        public const string ServiceId = "WotboDefender";
        public const string ServiceArgument = "--defender-service";

        public DefenderServiceHost()
        {
            ServiceName = ServiceId;
            CanStop = true;
            CanShutdown = false;
            AutoLog = false;
        }

        /// <summary>Точка входа, когда exe запущен как служба.</summary>
        public static void RunAsService()
        {
            System.ServiceProcess.ServiceBase.Run(new DefenderServiceHost());
        }

        protected override void OnStart(string[] args)
        {
            Task.Run(new Action(Execute));
        }

        private void Execute()
        {
            IAppLogger log = null;

            try
            {
                log = new AppLogger(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "Temp",
                    "wotbo-defender-service.log"));

                log.Info("DefenderService", "служба запущена");

                if (!DefenderStatus.IsSafeModeBoot())
                {
                    log.Warn("DefenderService", "обычная загрузка — служба удаляется без действий");
                    DefenderServiceControl.Remove(log);
                    Stop();
                    return;
                }

                string phase = DefenderServiceControl.ReadPhase();
                log.Info("DefenderService", "фаза: " + phase);

                var processes = new ProcessService(log);
                DefenderRunResult result = DefenderEngine.Run(
                    processes, log, phase, true, 30 * 60 * 1000);

                log.Info(
                    "DefenderService",
                    result == null
                        ? "движок не вернул результат"
                        : ("stage=" + result.Stage + " result=" + result.Result + " " + result.Message));
            }
            catch (Exception exception)
            {
                if (log != null)
                {
                    log.Error("DefenderService", "сбой службы", exception);
                }
            }
            finally
            {
                try
                {
                    DefenderServiceControl.Remove(null);
                }
                catch
                {
                }

                try
                {
                    Stop();
                }
                catch
                {
                }
            }
        }
    }

    /// <summary>Регистрация одноразовой службы безопасного режима.</summary>
    internal static class DefenderServiceControl
    {
        private const string LogCategory = "DefenderService";
        private const string SafeBootRoot = @"SYSTEM\CurrentControlSet\Control\SafeBoot";

        public static void Install(IAppLogger log, string phase)
        {
            try
            {
                Directory.CreateDirectory(DefenderPaths.Root);

                // Берём сборку WOTBO, а не EntryAssembly: твик может
                // вызываться из внешнего процесса (диагностика), и служба
                // должна запускать именно WOTBO.exe.
                string source = typeof(DefenderServiceControl).Assembly.Location;
                if (string.IsNullOrEmpty(source))
                {
                    source = Application_ExecutablePath();
                }

                if (!string.Equals(source, DefenderPaths.ServiceExe, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(source, DefenderPaths.ServiceExe, true);
                }

                File.WriteAllText(DefenderPaths.ServicePhaseFile, phase + Environment.NewLine, Encoding.ASCII);
                File.WriteAllText(DefenderPaths.AppPathFile, source, Encoding.Default);

                Run(
                    log,
                    "sc.exe",
                    "delete " + DefenderServiceHost.ServiceId);
                Run(
                    log,
                    "sc.exe",
                    "create " + DefenderServiceHost.ServiceId
                    + " binPath= \"\\\"" + DefenderPaths.ServiceExe + "\\\" "
                    + DefenderServiceHost.ServiceArgument + "\""
                    + " type= own start= auto obj= LocalSystem"
                    + " DisplayName= \"WOTBO Defender helper\"");

                AddSafeBootEntry("Minimal");
                AddSafeBootEntry("Network");

                log.Info(LogCategory, "служба безопасного режима установлена, фаза " + phase);
            }
            catch (Exception exception)
            {
                log.Error(LogCategory, "не удалось установить службу", exception);
            }
        }

        public static void Remove(IAppLogger log)
        {
            Run(log, "sc.exe", "delete " + DefenderServiceHost.ServiceId);

            RemoveSafeBootEntry("Minimal");
            RemoveSafeBootEntry("Network");

            try
            {
                if (File.Exists(DefenderPaths.ServicePhaseFile))
                {
                    File.Delete(DefenderPaths.ServicePhaseFile);
                }
            }
            catch
            {
            }
        }

        public static string ReadPhase()
        {
            try
            {
                if (File.Exists(DefenderPaths.ServicePhaseFile))
                {
                    string phase = File.ReadAllText(DefenderPaths.ServicePhaseFile).Trim();
                    if (phase.Length > 0)
                    {
                        return phase;
                    }
                }
            }
            catch
            {
            }

            return DefenderEngine.PhaseApply;
        }

        private static void AddSafeBootEntry(string group)
        {
            string path = SafeBootRoot + "\\" + group + "\\" + DefenderServiceHost.ServiceId;
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(path))
            {
                if (key != null)
                {
                    key.SetValue(string.Empty, DefenderServiceHost.ServiceId);
                }
            }
        }

        private static void RemoveSafeBootEntry(string group)
        {
            try
            {
                Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(
                    SafeBootRoot + "\\" + group + "\\" + DefenderServiceHost.ServiceId,
                    false);
            }
            catch
            {
            }
        }

        private static string Application_ExecutablePath()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        private static void Run(IAppLogger log, string fileName, string arguments)
        {
            try
            {
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

                    if (log != null)
                    {
                        log.Debug(LogCategory, "[cmd] " + fileName + " " + arguments);
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            log.Debug(LogCategory, "[out] " + output.Trim());
                        }
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            log.Warn(LogCategory, "[err] " + error.Trim());
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                if (log != null)
                {
                    log.Warn(LogCategory, "команда не выполнена: " + fileName + " :: " + exception.Message);
                }
            }
        }
    }
}
