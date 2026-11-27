using System;
using Microsoft.Win32;

namespace WOTBO.App
{
    /// <summary>
    /// Пользовательские настройки, не привязанные к конкретному твику.
    /// Хранятся в <c>HKCU\Software\oixro\wotbo</c>.
    /// </summary>
    public static class AppSettings
    {
        private const string RootPath = WOTBO.Services.Registry.RegistryPaths.AppRoot;

        /// <summary>
        /// Показывать подсказки при наведении на твики (описания).
        /// По умолчанию включено — так было всегда.
        /// </summary>
        public static bool TooltipsEnabled
        {
            get
            {
                object value = AppServices.Registry.GetValue(
                    RegistryHive.CurrentUser,
                    RootPath,
                    WOTBO.Services.Registry.RegistryPaths.Tooltips);

                return value == null || Convert.ToInt32(value) == 1;
            }
            set
            {
                AppServices.Registry.WriteValue(
                    RegistryHive.CurrentUser,
                    RootPath,
                    WOTBO.Services.Registry.RegistryPaths.Tooltips,
                    value ? 1 : 0);
            }
        }

        /// <summary>
        /// Высота окна в единицах 96 DPI. 0 — размер по умолчанию.
        /// Ширина фиксирована, поэтому храним только высоту.
        /// </summary>
        public static int WindowHeight
        {
            get
            {
                object value = AppServices.Registry.GetValue(
                    RegistryHive.CurrentUser,
                    RootPath,
                    WOTBO.Services.Registry.RegistryPaths.WindowHeight);

                int height;
                return value != null && int.TryParse(Convert.ToString(value), out height) && height > 0
                    ? height
                    : 0;
            }
            set
            {
                AppServices.Registry.WriteValue(
                    RegistryHive.CurrentUser,
                    RootPath,
                    WOTBO.Services.Registry.RegistryPaths.WindowHeight,
                    Math.Max(0, value));
            }
        }

        /// <summary>
        /// Показывать подсказки о применённых твиках. По умолчанию выключено.
        /// </summary>
        public static bool TweakTooltipsEnabled
        {
            get
            {
                object value = AppServices.Registry.GetValue(
                    RegistryHive.CurrentUser,
                    RootPath,
                    WOTBO.Services.Registry.RegistryPaths.TweakTooltips);

                return value != null && Convert.ToInt32(value) == 1;
            }
            set
            {
                AppServices.Registry.WriteValue(
                    RegistryHive.CurrentUser,
                    RootPath,
                    WOTBO.Services.Registry.RegistryPaths.TweakTooltips,
                    value ? 1 : 0);
            }
        }
    }
}
