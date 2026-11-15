using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Поиск без интернета и Copilot: только локальные файлы.</summary>
    public sealed class SearchWebTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Dsh", "IsPrelaunchEnabled", 0),
            new TweakValue(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1),
            new TweakValue(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0),
            // Классические значения Windows 10: без них поиск в меню «Пуск»
            // продолжает тянуть подсказки и результаты из интернета.
            new TweakValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0),
            new TweakValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", 0),
        };

        public override string Id
        {
            get { return "extra.searchweb"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        protected override TweakValue[] Values
        {
            get { return Items; }
        }
    }
}
