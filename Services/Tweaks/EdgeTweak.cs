using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Политики Microsoft Edge (большой набор значений).</summary>
    public sealed class EdgeTweak : TweakBase
    {
        public override string Id { get { return "extra.edge"; } }
        public override string Category { get { return "Extra"; } }

        public override TweakState Detect(TweakContext context)
        {
            // Ключ Edge может существовать и без нашего твика (чужие политики),
            // поэтому проверяем конкретное значение, которое пишем всегда.
            object boost = context.Registry.GetValue(
                RegistryHive.LocalMachine, RegistryPaths.Edge, "StartupBoostEnabled");

            if (boost == null)
            {
                return TweakState.NotApplied;
            }

            return System.Convert.ToInt32(boost) == 0
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        /// <summary>
        /// Политики, которые писал старый Edge-твик и которые больше не нужны:
        /// устаревшие, дефолтные и выключающие защиту. При повторном
        /// применении удаляем их, иначе они остаются висеть в реестре.
        /// </summary>
        private static readonly string[] RemovedPolicies =
        {
            "EdgeEnhanceImagesEnabled",
            "TyposquattingCheckerEnabled",
            "RemoveDesktopShortcutDefault",
            "DefaultSearchProviderEnabled",
            "PasswordMonitorAllowed",
            "UseSystemPrintDialog",
            "DownloadRestrictions",
            "TabFreezingEnabled",
            "InternetExplorerIntegrationTestingAllowed",
            "InternetExplorerIntegrationLocalFileAllowed",
            "AudioSandboxEnabled",
            "WebWidgetIsEnabledOnStartup",
            "FamilySafetySettingsEnabled",
            "ClickOnceEnabled",
            "SSLErrorOverrideAllowed",
            "TrackingPrevention",
            "BrowserSignin",
            "InternetExplorerIntegrationEnhancedHangDetection",
            "InternetExplorerIntegrationLevel",
            "VerticalTabsAllowed",
            "ConfigureFriendlyURLFormat",
            "DnsOverHttpsMode",
            "TargetBlankImpliesNoOpener",
            "ComponentUpdatesEnabled",
            "EnableDomainActionsDownload",
            "RendererCodeIntegrityEnabled",
            "BackgroundTemplateListUpdatesEnabled",
            "CommandLineFlagSecurityWarningsEnabled",
            "SitePerProcess",
            "SpellcheckEnabled",
            "EdgeCollectionsEnabled",
            "WebWidgetAllowed",
            "MetricsReportingEnabled",
            "ForceEphemeralProfiles",
            "ForceGoogleSafeSearch",
            "GoToIntranetSiteForSingleWordEntryInAddressBar",
            "RelaunchNotification",
            "RedirectSitesFromInternetExplorerPreventBHOInstall",
            "PromotionalTabsEnabled",
            "SendSiteInfoToImproveServices",
            "TotalMemoryLimitMb",
            "WPADQuickCheckEnabled",
            "InternetExplorerIntegrationLocalFileShowContextMenu",
            "AddressBarMicrosoftSearchInBingProviderEnabled",
            "ShowOfficeShortcutInFavoritesBar",
            "ConfigureDoNotTrack",
            "ApplicationGuardFavoritesSyncEnabled",
            "ApplicationGuardTrafficIdentificationEnabled",
            "ApplicationGuardUploadBlockingEnabled",
            "EnableMediaRouter",
            "ShowCastIconInToolbar",
            "DefaultCookiesSetting",
            "DefaultImagesSetting",
            "DefaultJavaScriptJitSetting",
            "DefaultJavaScriptSetting",
            "DefaultPluginsSetting",
            "LegacySameSiteCookieBehaviorEnabled",
            "PreventSmartScreenPromptOverrideForFiles",
            "PreventSmartScreenPromptOverride",
            "SmartScreenForTrustedDownloadsEnabled",
            "NewSmartScreenLibraryEnabled",
            "SmartScreenDnsRequestsEnabled",
            "SmartScreenPuaEnabled",
            "SmartScreenEnabled",
            "SiteSafetyServicesEnabled",
            "EdgeFollowEnabled",
        };

        public override void Apply(TweakContext context)
        {
            context.BackupRegistryKey(RegistryHive.LocalMachine, RegistryPaths.Edge);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EdgeWorkspacesEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "AlternateErrorPagesEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SpotlightExperiencesAndRecommendationsEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageSearchBox", "redirect", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BasicAuthOverHttpEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "AllowCrossOriginAuthPrompt", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DisableAuthNegotiateCnameLookup", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NativeMessagingUserLevelHosts", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "StartupBoostEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SleepingTabsEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "RestoreOnStartup", 0x00000004, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "HomepageLocation", "https://www.google.com/", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageLocation", "https://www.google.com/", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPagePrerenderEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageHideDefaultTopSites", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "AdsSettingForIntrusiveAdsSites", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "PersonalizationReportingEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BrowserNetworkTimeQueriesEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "LocalProvidersEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "UserFeedbackAllowed", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DirectInvokeEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BingAdsSuppression", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ConfigureOnlineTextToSpeech", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ExperimentationAndConfigurationServiceControl", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "IntensiveWakeUpThrottlingEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DNSInterceptionChecksEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NetworkPredictionOptions", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EnableOnlineRevocationChecks", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ResolveNavigationErrorsUseWebService", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SearchSuggestEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DiagnosticData", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EdgeShoppingAssistantEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ShowMicrosoftRewards", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BuiltInDnsClientEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ClearCachedImagesAndFilesOnExit", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultFileSystemReadGuardSetting", 0x00000003, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultFileSystemWriteGuardSetting", 0x00000003, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultGeolocationSetting", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultInsecureContentSetting", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultNotificationsSetting", 0x00000003, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultPopupsSetting", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultWebBluetoothGuardSetting", 0x00000003, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultWebHidGuardSetting", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultWebUsbGuardSetting", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageContentEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageQuickLinksEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageAllowedBackgroundTypes", 0x00000003, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BackgroundModeEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EfficiencyMode", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SuppressUnsupportedOSWarning", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "HubsSidebarEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "LocalBrowserDataShareEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "HideFirstRunExperience", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "HideInternetExplorerRedirectUXForIncompatibleSitesEnabled", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.EdgeRestoreUrls, "1", "https://www.google.com/");

            foreach (string name in RemovedPolicies)
            {
                context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, name);
            }
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteKey(RegistryHive.LocalMachine, RegistryPaths.Edge);
        }
    }
}
