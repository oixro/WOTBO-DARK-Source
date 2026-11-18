using System;
using System.Collections.Generic;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Каталог твиков. Пока пилот из трёх штук; сюда же добавляются
    /// следующие категории.
    /// </summary>
    public sealed class TweakCatalog
    {
        private readonly Dictionary<string, ITweak> _tweaks;

        public TweakCatalog(IEnumerable<ITweak> tweaks)
        {
            _tweaks = new Dictionary<string, ITweak>(StringComparer.OrdinalIgnoreCase);
            foreach (ITweak tweak in tweaks)
            {
                _tweaks.Add(tweak.Id, tweak);
            }
        }

        public IEnumerable<ITweak> All
        {
            get { return _tweaks.Values; }
        }

        public ITweak Get(string id)
        {
            ITweak tweak;
            if (!_tweaks.TryGetValue(id, out tweak))
            {
                throw new KeyNotFoundException("Твик не найден: " + id);
            }

            return tweak;
        }

        public static TweakCatalog CreateDefault()
        {
            var tweaks = new List<ITweak>
            {
                new JpegQualityTweak(),
                new IconCacheTweak(),
                new StickyKeysTweak(),
                new MpoTweak(),
                new MobileTrafficTweak(),
                new ScoobeTweak(),
                new DwmPriorityTweak(),
                new CsrssPriorityTweak(),
                new ContextMenuTweak(),
                new DwmInputTweak(),
                new AudioTweak(),
                new PowerLatencyTweak(),
                new UsbPowerTweak(),
                new MitigationsTweak(),
                new HibernateTweak(),
                new PagefileTweak(),
                new MouseFixTweak(),
                new PowerSchemeTweak(),
                new RegpackTweak(),
                new DefenderTweak(),
                new HdcpTweak(),
                new NvidiaTweaksTweak(),
                new MoveTempTweak(),
                new DirectPlayTweak(),
                new WindowsUpdateTweak(),
                new BufferbloatTweak(),
                new ButtonsTweak(),
                new ExplorerRestartTweak(),
                new Win11HeaderTweak(),
                new FfmpegTweak(),
                new AnselTweak(),
                new CompactOsTweak(),
                new WinSxsTweak(),
                new UpdateCleanTweak(),
                new TempCleanTweak(),
                new EdgeTweak(),
                new MicaTweak(),
                new CursorsTweak(),
                new StartupDelayTweak(),
                new MmcssTweak(),
                new KeyboardQueueTweak(),
                new DriverSearchTweak(),
                new BackgroundAppsTweak(),
                new SearchWebTweak(),
                new MaintenanceTweak(),
                new SysRestoreTweak(),
                new AppCompatTweak(),
                new KernelLatencyTweak(),
                new NagleTweak(),
                new CleanContextMenuTweak(),
                new GameBarTweak(),
                new ShortcutSuffixTweak(),
                new NvidiaEccTweak(),
                new NvidiaTelemetryTweak(),
                new NvidiaPowerTweak(),
                new NvidiaPowerHardTweak(),
                new IpiLatencyTweak(),
                new NdisWatchdogTweak(),
                new SleepStudyTweak(),
                new UwpCleanupTweak(),
                new VbsTweak(),
                new SearchIndexTweak(),
                new TelemetryServicesTweak(),
                new WidgetsTweak(),
                new WotboContextTweak(),
                new BlueFoldersTweak(),
                new ExplorerDetailsTweak(),
                new OneDriveTweak(),
                new HomeGalleryTweak(),
                new UnlockerTweak(),
                new AlwaysOnTopTweak(),
                new AnimationsTweak(),
                new Win32PriorityTweak(),
                new MmAgentTweak(),
                new ReservedStorageTweak(),
            };

            foreach (SimpleTweakDefinition definition in SimpleTweaks.All)
            {
                tweaks.Add(new SimpleTweak(definition));
            }

            return new TweakCatalog(tweaks);
        }
    }
}
