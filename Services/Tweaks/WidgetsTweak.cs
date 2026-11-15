using System;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Отключает виджеты Windows: политика новостей и кнопка на панели задач.
    ///
    /// Политика — основное: с ней компонент виджетов не запускается.
    /// Кнопку прячем best-effort: значение <c>TaskbarDa</c> на некоторых
    /// сборках защищено от записи, и падать из-за этого нельзя.
    /// </summary>
    public sealed class WidgetsTweak : TweakBase
    {
        private const string Policy = @"SOFTWARE\Policies\Microsoft\Dsh";
        private const string Advanced = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        private const string PolicyName = "AllowNewsAndInterests";
        private const string ButtonName = "TaskbarDa";

        public override string Id
        {
            get { return "extra.widgets"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object value = context.Registry.GetValue(RegistryHive.LocalMachine, Policy, PolicyName);
            return value != null && Convert.ToInt32(value) == 0
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.LocalMachine, Policy, PolicyName, 0, RegistryValueKind.DWord);

            try
            {
                context.Registry.WriteValue(
                    RegistryHive.CurrentUser, Advanced, ButtonName, 0, RegistryValueKind.DWord);
            }
            catch (Exception exception)
            {
                context.Log.Debug(Id, "кнопку виджетов скрыть не удалось: " + exception.Message);
            }
        }

        public override void Rollback(TweakContext context)
        {
            // Политику и (если записалась) кнопку вернёт бэкап TweakScope.
        }
    }
}
