using System;
using System.IO;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Пакет regpack: w10/w11 AIO, folder network, REG_AIO.</summary>
    public sealed class RegpackTweak : TweakBase
    {
        public override string Id
        {
            get { return "main.regpack"; }
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
                RunReg(context, Path.Combine(root, "w10", "w10_AIO.reg"), false);
            }
            else
            {
                RunReg(context, Path.Combine(root, "w11", "w11_AIO.reg"), true);
            }

            RunReg(context, Path.Combine(root, "foldernetworkX.reg"), true);
            RunReg(context, Path.Combine(root, "REG_AIO.reg"), false);

            context.Registry.WriteValue(
                RegistryHive.CurrentUser,
                RegistryPaths.VideoSettings,
                RegistryPaths.EnableAutoEnhanceDuringPlayback,
                0,
                RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            bool win10 = context.WindowsVersion.Detect().IsWindows10;
            string root = context.Workspace.Regpack;

            if (win10)
            {
                RunReg(context, Path.Combine(root, "w10", "w10_AIO_RESTORE.reg"), false);
            }
            else
            {
                RunReg(context, Path.Combine(root, "restore", "win11widgets_restore.bat"), true);
                RunReg(context, Path.Combine(root, "w11", "w11_AIO_RESTORE.reg"), true);
            }

            RunReg(context, Path.Combine(root, "foldernetworkX_restore.reg"), true);
            RunReg(context, Path.Combine(root, "REG_AIO_RESTORE.reg"), false);

            context.Registry.WriteValue(
                RegistryHive.CurrentUser,
                RegistryPaths.VideoSettings,
                RegistryPaths.EnableAutoEnhanceDuringPlayback,
                1,
                RegistryValueKind.DWord);
        }

        private static void RunReg(TweakContext context, string path, bool elevated)
        {
            if (!File.Exists(path))
            {
                context.Log.Warn("Tweak:main.regpack", "файл не найден: " + path);
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
