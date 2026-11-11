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
