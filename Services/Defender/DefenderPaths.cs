using System.IO;
using WOTBO.App;

namespace WOTBO.Services.Defender
{
    /// <summary>
    /// Пути движка отключения Defender.
    ///
    /// Всё состояние лежит в <c>%ProgramData%\oixro\wotbo\defender</c>, чтобы
    /// задача из безопасного режима (SYSTEM) могла читать журнал, а зеркало
    /// для UI — в штатном каталоге бэкапов твиков.
    /// </summary>
    internal static class DefenderPaths
    {
        public static string Root
        {
            get { return Path.Combine(AppPaths.ProgramData, "oixro", "wotbo", "defender"); }
        }

        public static string EngineScript
        {
            get { return Path.Combine(Root, "defender-engine.ps1"); }
        }

        public static string StageRunner
        {
            get { return Path.Combine(Root, "wotbo-stage.cmd"); }
        }

        public static string WatchdogRunner
        {
            get { return Path.Combine(Root, "wotbo-watchdog.cmd"); }
        }

        /// <summary>Видимый запуск применения из RunOnce или с рабочего стола.</summary>
        public static string StageVisibleRunner
        {
            get { return Path.Combine(Root, "wotbo-stage-visible.cmd"); }
        }

        /// <summary>Копия exe для одноразовой службы безопасного режима.</summary>
        public static string ServiceExe
        {
            get { return Path.Combine(Root, "WotboDefender.exe"); }
        }

        /// <summary>Путь к настоящему WOTBO.exe — чтобы открыть окно результата.</summary>
        public static string AppPathFile
        {
            get { return Path.Combine(Root, "app-path.txt"); }
        }

        /// <summary>Файл с фазой для службы: apply или restore.</summary>
        public static string ServicePhaseFile
        {
            get { return Path.Combine(Root, "service-phase.txt"); }
        }

        /// <summary>Одноразовая финализация после отката в обычном режиме.</summary>
        public static string FinalizeRunner
        {
            get { return Path.Combine(Root, "wotbo-finalize.cmd"); }
        }

        /// <summary>Хук Winlogon\Userinit: запускается при входе, работает и в safe mode.</summary>
        public static string LoginRunner
        {
            get { return Path.Combine(Root, "wotbo-login.cmd"); }
        }

        /// <summary>Видимый запуск отката из безопасного режима.</summary>
        public static string RestoreVisibleRunner
        {
            get { return Path.Combine(Root, "wotbo-restore-visible.cmd"); }
        }

        /// <summary>Ярлык на рабочем столе: применить в безопасном режиме.</summary>
        public static string DesktopApplyScript
        {
            get { return Path.Combine(AppPaths.PublicDesktop, "WOTBO - Defender off (safe mode).cmd"); }
        }

        /// <summary>Ярлык на рабочем столе: откатить в безопасном режиме.</summary>
        public static string DesktopRestoreScript
        {
            get { return Path.Combine(AppPaths.PublicDesktop, "WOTBO - Defender restore (safe mode).cmd"); }
        }

        /// <summary>Ярлык на рабочем столе: снять safeboot, если что-то пошло не так.</summary>
        public static string DesktopUndoSafeMode
        {
            get { return Path.Combine(AppPaths.PublicDesktop, "WOTBO - undo safe mode.cmd"); }
        }

        /// <summary>Общий лог, который виден и без входа в систему (safe mode).</summary>
        public static string PublicLog
        {
            get { return Path.Combine(AppPaths.Windows, "Temp", "wotbo-defender.log"); }
        }

        public static string StateFile
        {
            get { return Path.Combine(Root, "state.ini"); }
        }

        public static string VerifyFile
        {
            get { return Path.Combine(Root, "verify.ini"); }
        }

        public static string JournalFile
        {
            get { return Path.Combine(Root, "journal.json"); }
        }

        public static string LogDirectory
        {
            get { return Path.Combine(Root, "logs"); }
        }

        /// <summary>Аварийное снятие safeboot на случай проблем с загрузкой.</summary>
        public static string UndoBat
        {
            get { return Path.Combine(AppPaths.SystemDrive + "\\", "WOTBO-UNDO-SAFE-MODE.bat"); }
        }

        /// <summary>Зеркало журнала в штатном каталоге бэкапов твика.</summary>
        public static string BackupMirror
        {
            get
            {
                return Path.Combine(
                    AppPaths.LocalAppData, "oixro", "wotbo", "backups", "main.defender");
            }
        }
    }
}
