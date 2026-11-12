using System;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>MouseFix: кривые мыши и параметры 1:1 без .reg-файла.</summary>
    public sealed class MouseFixTweak : TweakBase
    {
        private static readonly byte[] FixXCurve =
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xC0, 0xCC, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x80, 0x99, 0x19, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x40, 0x66, 0x26, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x33, 0x33, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        private static readonly byte[] FixYCurve =
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x38, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x70, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0xA8, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0xE0, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        private static readonly byte[] DefaultXCurve =
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x15, 0x6E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x40, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x29, 0xDC, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        private static readonly byte[] DefaultYCurve =
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xFD, 0x11, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x24, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0xFC, 0x12, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0xC0, 0xBB, 0x01, 0x00, 0x00, 0x00, 0x00,
        };

        private const string MousePath = @"Control Panel\Mouse";
        private const string DefaultUserMousePath = @".DEFAULT\Control Panel\Mouse";

        public override string Id
        {
            get { return "main.mousefix"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object sensitivity = context.Registry.GetValue(RegistryHive.CurrentUser, MousePath, "MouseSensitivity");
            object speed = context.Registry.GetValue(RegistryHive.CurrentUser, MousePath, "MouseSpeed");
            object xCurve = context.Registry.GetValue(RegistryHive.CurrentUser, MousePath, "SmoothMouseXCurve");
            object yCurve = context.Registry.GetValue(RegistryHive.CurrentUser, MousePath, "SmoothMouseYCurve");

            return Convert.ToString(sensitivity) == "10"
                && Convert.ToString(speed) == "0"
                && BytesEqual(xCurve as byte[], FixXCurve)
                && BytesEqual(yCurve as byte[], FixYCurve)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }

            return true;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.CurrentUser, MousePath, "MouseSensitivity", "10", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.CurrentUser, MousePath, "MouseSpeed", "0", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.CurrentUser, MousePath, "SmoothMouseXCurve", FixXCurve, RegistryValueKind.Binary);
            context.Registry.WriteValue(RegistryHive.CurrentUser, MousePath, "SmoothMouseYCurve", FixYCurve, RegistryValueKind.Binary);
            context.Registry.WriteValue(RegistryHive.Users, DefaultUserMousePath, "MouseSpeed", "0", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.Users, DefaultUserMousePath, "MouseThreshold1", "0", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.Users, DefaultUserMousePath, "MouseThreshold2", "0", RegistryValueKind.String);
        }

        public override void Rollback(TweakContext context)
        {
            // Прежнее unmousefix.reg возвращал только стандартные кривые;
            // остальные значения восстановит TweakScope из бэкапа.
            context.Registry.WriteValue(RegistryHive.CurrentUser, MousePath, "SmoothMouseXCurve", DefaultXCurve, RegistryValueKind.Binary);
            context.Registry.WriteValue(RegistryHive.CurrentUser, MousePath, "SmoothMouseYCurve", DefaultYCurve, RegistryValueKind.Binary);
        }
    }
}
