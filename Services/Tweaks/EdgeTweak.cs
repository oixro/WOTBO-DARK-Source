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
            return context.Registry.KeyExists(RegistryHive.LocalMachine, RegistryPaths.Edge)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EdgeEnhanceImagesEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EdgeWorkspacesEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "TyposquattingCheckerEnabled", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "RemoveDesktopShortcutDefault", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "AlternateErrorPagesEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SpotlightExperiencesAndRecommendationsEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageSearchBox", "redirect");
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultSearchProviderEnabled", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BasicAuthOverHttpEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "AllowCrossOriginAuthPrompt", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DisableAuthNegotiateCnameLookup", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NativeMessagingUserLevelHosts", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "PasswordMonitorAllowed", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "StartupBoostEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "UseSystemPrintDialog", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SleepingTabsEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "RestoreOnStartup", 0x00000004, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "HomepageLocation", "www.google.com");
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageLocation", "www.google.com");
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPagePrerenderEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageHideDefaultTopSites", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "AdsSettingForIntrusiveAdsSites", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DownloadRestrictions", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "TabFreezingEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "InternetExplorerIntegrationTestingAllowed", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "InternetExplorerIntegrationLocalFileAllowed", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "PersonalizationReportingEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BrowserNetworkTimeQueriesEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "LocalProvidersEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "AudioSandboxEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "WebWidgetIsEnabledOnStartup", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "UserFeedbackAllowed", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "FamilySafetySettingsEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ClickOnceEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DirectInvokeEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SSLErrorOverrideAllowed", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BingAdsSuppression", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "TrackingPrevention", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BrowserSignin", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "InternetExplorerIntegrationEnhancedHangDetection", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "InternetExplorerIntegrationLevel", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ConfigureOnlineTextToSpeech", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "VerticalTabsAllowed", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ConfigureFriendlyURLFormat", 0x00000004, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ExperimentationAndConfigurationServiceControl", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "IntensiveWakeUpThrottlingEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DnsOverHttpsMode", "off");
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DNSInterceptionChecksEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "TargetBlankImpliesNoOpener", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ComponentUpdatesEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EnableDomainActionsDownload", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NetworkPredictionOptions", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EnableOnlineRevocationChecks", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "RendererCodeIntegrityEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ResolveNavigationErrorsUseWebService", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BackgroundTemplateListUpdatesEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SearchSuggestEnabled", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "CommandLineFlagSecurityWarningsEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SitePerProcess", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SpellcheckEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EdgeCollectionsEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "WebWidgetAllowed", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "MetricsReportingEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ForceEphemeralProfiles", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ForceGoogleSafeSearch", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "GoToIntranetSiteForSingleWordEntryInAddressBar", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "HideFirstRunExperience", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "HideInternetExplorerRedirectUXForIncompatibleSitesEnabled", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "RelaunchNotification", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "RedirectSitesFromInternetExplorerPreventBHOInstall", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "PromotionalTabsEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DiagnosticData", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SendSiteInfoToImproveServices", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "TotalMemoryLimitMb", 0x00008192, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "WPADQuickCheckEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EdgeShoppingAssistantEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "InternetExplorerIntegrationLocalFileShowContextMenu", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "AddressBarMicrosoftSearchInBingProviderEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ShowOfficeShortcutInFavoritesBar", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ShowMicrosoftRewards", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BuiltInDnsClientEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ClearCachedImagesAndFilesOnExit", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ConfigureDoNotTrack", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ApplicationGuardFavoritesSyncEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ApplicationGuardTrafficIdentificationEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ApplicationGuardUploadBlockingEnabled", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EnableMediaRouter", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "ShowCastIconInToolbar", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultCookiesSetting", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultFileSystemReadGuardSetting", 0x00000003, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultFileSystemWriteGuardSetting", 0x00000003, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultGeolocationSetting", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultImagesSetting", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultInsecureContentSetting", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultJavaScriptJitSetting", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultJavaScriptSetting", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultNotificationsSetting", 0x00000003, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultPluginsSetting", 0x00000003, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultPopupsSetting", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultWebBluetoothGuardSetting", 0x00000003, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultWebHidGuardSetting", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "DefaultWebUsbGuardSetting", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "LegacySameSiteCookieBehaviorEnabled", 0x00000002, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "PreventSmartScreenPromptOverrideForFiles", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "PreventSmartScreenPromptOverride", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SmartScreenForTrustedDownloadsEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewSmartScreenLibraryEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SmartScreenDnsRequestsEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SmartScreenPuaEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SmartScreenEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageContentEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageQuickLinksEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "NewTabPageAllowedBackgroundTypes", 0x00000003, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "TyposquattingCheckerEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "BackgroundModeEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EfficiencyMode", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SuppressUnsupportedOSWarning", 0x00000001, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "HubsSidebarEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "SiteSafetyServicesEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "EdgeFollowEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Edge, "LocalBrowserDataShareEnabled", 0x00000000, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.EdgeRestoreUrls, "1", "www.google.com");
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteKey(RegistryHive.LocalMachine, RegistryPaths.Edge);
        }
    }
}
