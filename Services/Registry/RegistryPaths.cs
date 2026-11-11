namespace WOTBO.Services.Registry
{
    /// <summary>
    /// Часто используемые пути и имена значений реестра.
    /// Новый код не должен держать их строкой на месте.
    /// </summary>
    public static class RegistryPaths
    {
        public const string AppRoot = @"Software\oixro\wotbo";
        public const string BackupsRoot = AppRoot + @"\Backups";

        public const string WindowsVersion = @"Software\Microsoft\Windows NT\CurrentVersion";
        public const string Desktop = @"Control Panel\Desktop";
        public const string StickyKeys = @"Control Panel\Accessibility\StickyKeys";
        public const string Explorer = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer";
        public const string Dwm = @"SOFTWARE\Microsoft\Windows\Dwm";
        public const string MemoryManagement = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";
        public const string PriorityControl = @"SYSTEM\ControlSet001\Control\PriorityControl";
        public const string TcpipParameters = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
        public const string Tcpip6Parameters = @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters";
        public const string UserProfileEngagement = @"SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement";
        public const string ImageFileExecutionOptions = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";
        public const string SystemProfile = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        public const string ContextMenuClsid = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";
        public const string WindowsKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows";
        public const string Audio = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio";
        public const string BackgroundAudioPolicy = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\BackgroundModel\BackgroundAudioPolicy";
        public const string Power = @"SYSTEM\CurrentControlSet\Control\Power";
        public const string GraphicsPower = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power";
        public const string UsbHubHubg = @"SYSTEM\CurrentControlSet\Services\usbhub\hubg";
        public const string Xusb22Parameters = @"SYSTEM\CurrentControlSet\Services\xusb22\Parameters";
        public const string UsbXhciParameters = @"SYSTEM\CurrentControlSet\Services\USBXHCI\Parameters";
        public const string UsbHubParameters = @"SYSTEM\CurrentControlSet\Services\usbhub\Parameters";
        public const string EnumUsb = @"SYSTEM\CurrentControlSet\Enum\USB";
        public const string UsbXhciWdf = @"SYSTEM\CurrentControlSet\Services\USBXHCI\Parameters\Wdf";
        public const string UsbFlags = @"SYSTEM\CurrentControlSet\Control\usbflags";
        public const string EnhancedStorageDevices = @"SOFTWARE\Policies\Microsoft\Windows\EnhancedStorageDevices";
        public const string Kernel = @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel";
        public const string DeviceGuardHvci = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";
        public const string Services = @"SYSTEM\CurrentControlSet\Services";
        public const string VideoSettings = @"Software\Microsoft\Windows\CurrentVersion\VideoSettings";
        public const string DefenderPolicy = @"SOFTWARE\Policies\Microsoft\Windows Defender";
        public const string DefenderRealtime = @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection";
        public const string DefenderSpynet = @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet";
        public const string Environment = @"Environment";
        public const string NvidiaColor = @"Software\NVIDIA Corporation\Global\NVTweak\Devices\509901423-0\Color";
        public const string GraphicsDrivers = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
        public const string NvidiaFts = @"SYSTEM\CurrentControlSet\Services\nvlddmkm\FTS";

        public const string HdcpPath =
            @"System\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000";

        public const string NvidiaTweaksPath =
            @"SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak";

        public const string TweakTooltips = "tweakTooltips";
        public const string JpegImportQuality = "JPEGImportQuality";
        public const string MaxCachedIcons = "Max Cached Icons";
        public const string StickyKeysFlags = "Flags";
        public const string OverlayTestMode = "OverlayTestMode";
        public const string DefaultTtl = "DefaultTTL";
        public const string ScoobeSystemSettingEnabled = "ScoobeSystemSettingEnabled";
        public const string AllowHeadlessExecution = "AllowHeadlessExecution";
        public const string EnableAutoEnhanceDuringPlayback = "EnableAutoEnhanceDuringPlayback";
    }
}
