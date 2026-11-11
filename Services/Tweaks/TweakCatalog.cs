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
            return new TweakCatalog(new ITweak[]
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
            });
        }
    }
}
