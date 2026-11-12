using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Отключение Defender.
    ///
    /// Кроме прежних .reg-пакетов добавлены актуальные ветки политик и служб
    /// для 24H2/25H2/26H2. Tamper Protection реестром не отключается — об
    /// этом пишется предупреждение.
    /// </summary>
    public sealed class DefenderTweak : TweakBase
    {
        private static readonly string[] ModernServices =
        {
            "WinDefend",
            "WdNisSvc",
            "Sense",
            "SecurityHealthService",
        };

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
            return context.Store.IsApplied(Id)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            bool win10 = context.WindowsVersion.Detect().IsWindows10;
            string root = context.Workspace.Regpack;

            if (win10)
            {
                RunReg(context, Path.Combine(root, "w10", "win10defenderX.reg"), true);
            }
            else
            {
                RunReg(context, Path.Combine(root, "w11", "win11defenderX.reg"), true);
                SetWebThreatServices(context, true);
            }

            ApplyModernPolicy(context);
            RunReg(context, Path.Combine(root, "antimalwareserviceexecutable", "!AIO_OFF.reg"), true);

            context.Log.Warn(
                "Tweak:main.defender",
                "Tamper Protection реестром не отключается; если она включена, часть ключей может игнорироваться");
        }

        public override void Rollback(TweakContext context)
        {
            bool win10 = context.WindowsVersion.Detect().IsWindows10;
            string root = context.Workspace.Regpack;

            if (win10)
            {
                RunReg(context, Path.Combine(root, "w10", "win10defenderX_restore.reg"), true);
            }
            else
            {
                RunReg(context, Path.Combine(root, "w11", "win11defenderX_restore.reg"), true);
                SetWebThreatServices(context, false);
            }

            RollbackModernPolicy(context);
            RunReg(context, Path.Combine(root, "antimalwareserviceexecutable", "AIO_ON.reg"), true);
        }

        private static void ApplyModernPolicy(TweakContext context)
        {
            TryWrite(context, RegistryPaths.DefenderPolicy, "DisableAntiSpyware", 1);
            TryWrite(context, RegistryPaths.DefenderPolicy, "DisableAntiVirus", 1);
            TryWrite(context, RegistryPaths.DefenderRealtime, "DisableRealtimeMonitoring", 1);
            TryWrite(context, RegistryPaths.DefenderRealtime, "DisableBehaviorMonitoring", 1);
            TryWrite(context, RegistryPaths.DefenderRealtime, "DisableOnAccessProtection", 1);
            TryWrite(context, RegistryPaths.DefenderRealtime, "DisableScanOnRealtimeEnable", 1);
            TryWrite(context, RegistryPaths.DefenderSpynet, "SpynetReporting", 0);
            TryWrite(context, RegistryPaths.DefenderSpynet, "SubmitSamplesConsent", 2);

            foreach (string service in ModernServices)
            {
                TryWrite(context, RegistryPaths.Services + "\\" + service, "Start", 4);
            }
        }

        private static void RollbackModernPolicy(TweakContext context)
        {
            TryDelete(context, RegistryPaths.DefenderPolicy, "DisableAntiSpyware");
            TryDelete(context, RegistryPaths.DefenderPolicy, "DisableAntiVirus");
            TryDelete(context, RegistryPaths.DefenderRealtime, "DisableRealtimeMonitoring");
            TryDelete(context, RegistryPaths.DefenderRealtime, "DisableBehaviorMonitoring");
            TryDelete(context, RegistryPaths.DefenderRealtime, "DisableOnAccessProtection");
            TryDelete(context, RegistryPaths.DefenderRealtime, "DisableScanOnRealtimeEnable");
            TryDelete(context, RegistryPaths.DefenderSpynet, "SpynetReporting");
            TryDelete(context, RegistryPaths.DefenderSpynet, "SubmitSamplesConsent");

            foreach (string service in ModernServices)
            {
                TryWrite(context, RegistryPaths.Services + "\\" + service, "Start", 2);
            }
        }

        private static void SetWebThreatServices(TweakContext context, bool apply)
        {
            IReadOnlyList<string> services;
            try
            {
                services = context.Registry.GetSubKeyNames(
                    RegistryHive.LocalMachine,
                    RegistryPaths.Services);
            }
            catch (Exception ex)
            {
                context.Log.Warn("Tweak:main.defender", "не удалось перечислить службы: " + ex.Message);
                return;
            }

            foreach (string service in services)
            {
                if (!service.StartsWith("webthreatdefusersvc_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string path = RegistryPaths.Services + "\\" + service;
                if (apply)
                {
                    TryWrite(context, path, "LaunchProtected", 4);
                    TryWrite(context, path, "Start", 4);
                    TryWrite(context, path, "Start_old", 4);
                }
                else
                {
                    TryDelete(context, path, "LaunchProtected");
                    TryWrite(context, path, "Start", 2);
                    TryDelete(context, path, "Start_old");
                }
            }
        }

        private static void TryWrite(TweakContext context, string path, string name, int value)
        {
            try
            {
                context.Registry.WriteValue(RegistryHive.LocalMachine, path, name, value, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                context.Log.Warn("Tweak:main.defender", "не удалось записать " + path + "\\" + name + ": " + ex.Message);
            }
        }

        private static void TryDelete(TweakContext context, string path, string name)
        {
            try
            {
                context.Registry.DeleteValue(RegistryHive.LocalMachine, path, name);
            }
            catch (Exception ex)
            {
                context.Log.Warn("Tweak:main.defender", "не удалось удалить " + path + "\\" + name + ": " + ex.Message);
            }
        }

        private static void RunReg(TweakContext context, string path, bool elevated)
        {
            if (!File.Exists(path))
            {
                context.Log.Warn("Tweak:main.defender", "файл не найден: " + path);
                return;
            }

            context.BackupRegFile(path);
            string command = "regedit.exe /s \"" + path + "\"";
            if (elevated)
            {
                context.Processes.RunSuper(command, context.Workspace.SuExecutable);
            }
            else
            {
                context.Processes.StartCaptured(command);
            }
        }
    }
}
