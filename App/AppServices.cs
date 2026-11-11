using System;
using WOTBO.App.Logging;
using WOTBO.Services.Registry;

namespace WOTBO.App
{
    /// <summary>
    /// Точка доступа к сервисам до появления DI-контейнера.
    ///
    /// Сервисы создаются лениво и один раз. По мере перевода форм на
    /// конструкторную инъекцию этот класс будет использоваться всё реже,
    /// но останется удобным местом для «одиночных» зависимостей.
    /// </summary>
    public static class AppServices
    {
        private static readonly Lazy<IRegistryService> RegistryLazy =
            new Lazy<IRegistryService>(() => new RegistryService(Log.Instance));

        /// <summary>Реестр x64 (<c>RegistryView.Registry64</c>).</summary>
        public static IRegistryService Registry
        {
            get { return RegistryLazy.Value; }
        }
    }
}
