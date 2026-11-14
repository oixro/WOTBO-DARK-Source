using System;
using Microsoft.Win32;
using WOTBO.Services.Defender;
using WOTBO.Services.Windows;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Полное отключение Windows Defender.
    ///
    /// Логика вынесена в PowerShell-движок <c>Resources\defender-engine.ps1</c>:
    /// он делает бэкап (экспорт реестра, пары переименований, XML задач),
    /// отключает службы, драйверы, задачи и политики, а также брандмауэр,
    /// SmartScreen, Smart App Control и Windows Security.
    ///
    /// Если Tamper Protection не даёт остановить <c>WinDefend</c>, WOTBO
    /// готовит автоматический безопасный режим (задача SYSTEM + safeboot),
    /// перезагружается, применяет изменения и возвращается в обычный режим.
    ///
    /// Файлы не удаляются, а переименовываются в <c>*.wotbo_bak</c>, поэтому
    /// откат возвращает систему в исходное состояние.
    /// </summary>
    public sealed class DefenderTweak : TweakBase
    {
        private const string LogCategory = "Tweak:main.defender";
        private const int PrepareTimeoutMs = 5 * 60 * 1000;
        private const int ApplyTimeoutMs = 20 * 60 * 1000;
        private const int VerifyTimeoutMs = 2 * 60 * 1000;

        public enum DefenderOutcome
        {
            None,
            Applied,
            AppliedNeedsReboot,
            SafeModeApply,
            SafeModeRestore,
            Restored,
            Failed,
        }

        /// <summary>Результат последнего применения — для сообщений в UI.</summary>
        public static DefenderOutcome LastOutcome { get; private set; }

        public static string LastDetails { get; private set; }

        public override string Id
        {
            get { return "main.defender"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            if (DefenderStatus.IsFullyDisabled())
            {
                return TweakState.Applied;
            }

            // Подготовленный безопасный режим считается применённым: твик уже
            // нельзя прерывать, он завершится после перезагрузки.
            if (DefenderEngine.IsPending())
            {
                return TweakState.Applied;
            }

            return TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            LastOutcome = DefenderOutcome.None;
            LastDetails = null;

            // Проверяем до любых изменений: если TP/RTP включены, Defender
            // всё равно не даст себя выключить, и состояние не должно
            // выглядеть как «подготовлено».
            DefenderProtectionState protection = DefenderStatus.GetProtection();
            if (protection.Available && (protection.TamperProtection || protection.RealTimeProtection))
            {
                context.Log.Warn(
                    LogCategory,
                    "защита от подделки или защита в реальном времени включена — отключение остановлено");
                Fail(
                    context,
                    "Перед отключением выключите в «Безопасности Windows» защиту от подделки " +
                    "и защиту в реальном времени.");
                return;
            }

            if (!DefenderEngine.EnsurePayload(context.Log))
            {
                Fail(context, "не удалось распаковать движок Defender");
                return;
            }

            DefenderRunResult prepared = DefenderEngine.Run(
                context.Processes,
                context.Log,
                DefenderEngine.PhasePrepare,
                false,
                PrepareTimeoutMs);

            if (prepared == null || !prepared.Ok)
            {
                Fail(context, "не удалось создать бэкап Defender");
                return;
            }

            DefenderEngine.MirrorBackup(context.Log);
            context.BackupRegistryKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender");

            // На Windows 11 защитник защищает свои службы даже после остановки,
            // поэтому всегда идём через безопасный режим. На Windows 10 хватает
            // обычного применения.
            WindowsVersionInfo version = context.WindowsVersion.Detect();
            if (version.IsWindows11)
            {
                context.Log.Info(LogCategory, "Windows 11 — применяем через безопасный режим");
                StartSafeMode(context, DefenderOutcome.SafeModeApply);
                return;
            }

            DefenderRunResult applied = DefenderEngine.Run(
                context.Processes,
                context.Log,
                DefenderEngine.PhaseApply,
                false,
                ApplyTimeoutMs);

            DefenderVerification verification = DefenderStatus.Verify(
                context.Processes, context.Log, VerifyTimeoutMs);

            if (applied != null && applied.Blocked)
            {
                context.Log.Warn(LogCategory, "быстрое отключение заблокировано — безопасный режим");
                StartSafeMode(context, DefenderOutcome.SafeModeApply);
                return;
            }

            bool stillOn = verification == null
                || !verification.DefenderOff
                || string.Equals(verification.Antivirus, "1", StringComparison.OrdinalIgnoreCase);

            if (stillOn)
            {
                context.Log.Warn(LogCategory, "Defender всё ещё активен — безопасный режим");
                StartSafeMode(context, DefenderOutcome.SafeModeApply);
                return;
            }

            LastOutcome = verification.NeedsReboot
                ? DefenderOutcome.AppliedNeedsReboot
                : DefenderOutcome.Applied;
            LastDetails = verification.Details;
            context.Log.Info(LogCategory, "Defender отключён: " + verification.Details);
        }

        public override void Rollback(TweakContext context)
        {
            LastOutcome = DefenderOutcome.None;
            LastDetails = null;

            if (!DefenderEngine.EnsurePayload(context.Log))
            {
                Fail(context, "не удалось распаковать движок Defender");
                return;
            }

            DefenderEngine.Run(
                context.Processes,
                context.Log,
                DefenderEngine.PhaseRestore,
                false,
                ApplyTimeoutMs);

            DefenderVerification verification = DefenderStatus.Verify(
                context.Processes, context.Log, VerifyTimeoutMs);

            if (verification != null && verification.Restored)
            {
                LastOutcome = DefenderOutcome.Restored;
                LastDetails = verification.Details;
                context.Log.Info(LogCategory, "Defender восстановлен: " + verification.Details);
                return;
            }

            context.Log.Warn(LogCategory, "откат из обычного режима не удался — безопасный режим");
            if (DefenderSafeMode.Prepare(context.Log, DefenderEngine.PhaseRestore))
            {
                LastOutcome = DefenderOutcome.SafeModeRestore;
                LastDetails = "откат продолжится в безопасном режиме";
            }
            else
            {
                Fail(context, "не удалось подготовить безопасный режим для отката");
            }
        }

        /// <summary>Отменить подготовленный безопасный режим (пользователь отказался от перезагрузки).</summary>
        public static void AbortPrepared(TweakContext context)
        {
            DefenderSafeMode.Abort(context.Log);
            LastOutcome = DefenderOutcome.None;
        }

        /// <summary>Перезагрузить ПК, чтобы применить или откатить изменения.</summary>
        public static void Reboot(TweakContext context)
        {
            DefenderSafeMode.RebootNow(context.Log, 0);
        }

        private static void StartSafeMode(TweakContext context, DefenderOutcome outcome)
        {
            if (DefenderSafeMode.Prepare(context.Log))
            {
                LastOutcome = outcome;
                LastDetails = "для завершения нужна перезагрузка в безопасный режим";
                return;
            }

            Fail(context, "не удалось подготовить безопасный режим");
        }

        private static void Fail(TweakContext context, string message)
        {
            LastOutcome = DefenderOutcome.Failed;
            LastDetails = message;
            context.Log.Error(LogCategory, message);
        }
    }
}
