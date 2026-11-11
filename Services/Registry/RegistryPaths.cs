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

        public const string HdcpPath =
            @"System\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000";

        public const string NvidiaTweaksPath =
            @"SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak";

        public const string TweakTooltips = "tweakTooltips";
        public const string JpegImportQuality = "JPEGImportQuality";
        public const string MaxCachedIcons = "Max Cached Icons";
        public const string StickyKeysFlags = "Flags";
    }
}
