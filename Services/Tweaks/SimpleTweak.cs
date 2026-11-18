using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Описание простого твика: только значения реестра, без команд и файлов.
    /// Живёт в одном месте, чтобы каталог и строки вкладок не расходились.
    /// </summary>
    public sealed class SimpleTweakDefinition
    {
        public SimpleTweakDefinition(
            string id,
            string category,
            string panel,
            string text,
            string tooltip,
            params TweakValue[] values)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("id пуст", nameof(id));

            Id = id;
            Category = category;
            Panel = panel;
            Text = text;
            Tooltip = tooltip;
            Values = values ?? new TweakValue[0];
        }

        /// <summary>Идентификатор твика.</summary>
        public string Id { get; private set; }

        /// <summary>Категория ITweak (Main/UI/GPU/Extra).</summary>
        public string Category { get; private set; }

        /// <summary>Панель, на которой рисуется строка: main, ui, gpu, extra.</summary>
        public string Panel { get; private set; }

        public string Text { get; private set; }

        public string Tooltip { get; private set; }

        public TweakValue[] Values { get; private set; }

        /// <summary>Строка существует только на Windows 11.</summary>
        public bool Windows11Only { get; set; }
    }

    /// <summary>Твик, целиком описанный набором значений реестра.</summary>
    public sealed class SimpleTweak : TweakBase
    {
        private readonly SimpleTweakDefinition _definition;

        public SimpleTweak(SimpleTweakDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            _definition = definition;
        }

        public override string Id
        {
            get { return _definition.Id; }
        }

        public override string Category
        {
            get { return _definition.Category; }
        }

        public override TweakState Detect(TweakContext context)
        {
            if (_definition.Windows11Only && context.WindowsVersion.Detect().IsWindows10)
            {
                return TweakState.Unavailable;
            }

            TweakValue[] values = _definition.Values;
            if (values.Length == 0)
            {
                return TweakState.NotApplied;
            }

            foreach (TweakValue value in values)
            {
                if (!value.Matches(context))
                {
                    return TweakState.NotApplied;
                }
            }

            return TweakState.Applied;
        }

        public override void Apply(TweakContext context)
        {
            foreach (TweakValue value in _definition.Values)
            {
                context.Registry.WriteValue(
                    value.Hive,
                    value.Path,
                    value.Name,
                    value.Value,
                    value.Kind);
            }
        }

        public override void Rollback(TweakContext context)
        {
            // Прежние значения возвращает бэкап TweakScope.
        }
    }

    /// <summary>
    /// Новые реестровые твики. Держим их таблицей: одна запись даёт и класс
    /// для каталога, и строку на вкладке, поэтому добавить твик — это одна
    /// строка данных, а не правка четырёх файлов.
    /// </summary>
    public static class SimpleTweaks
    {
        private const string Gpu = "GPU";
        private const string Ui = "UI";
        private const string Extra = "Extra";

        private const string GameBar = @"Software\Microsoft\GameBar";
        private const string GameConfig = @"System\GameConfigStore";
        private const string GameDvr = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
        private const string GameDvrPolicy = @"SOFTWARE\Policies\Microsoft\Windows\GameDVR";
        private const string Advanced = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        private const string Policies = @"SOFTWARE\Policies\Microsoft\Windows";

        private static readonly SimpleTweakDefinition[] Definitions =
        {
            // ---- GPU и игры -------------------------------------------------
            new SimpleTweakDefinition(
                "gpu.hags",
                Gpu,
                "gpu",
                "Аппаратное планирование GPU (HAGS)",
                "HwSchMode=2: планирование очередей GPU силами драйвера. "
                    + "Нужна перезагрузка; на старых драйверах возможны артефакты.",
                Hklm(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2)),

            new SimpleTweakDefinition(
                "gpu.gamedvr",
                Gpu,
                "gpu",
                "Отключить запись Game DVR",
                "Убирает фоновую запись и историю игр (Game DVR). "
                    + "Кнопка Win+G и оверлей Xbox могут остаться.",
                Hkcu(GameConfig, "GameDVR_Enabled", 0),
                Hkcu(GameConfig, "GameDVR_HistoricalCaptureEnabled", 0),
                Hkcu(GameDvr, "AppCaptureEnabled", 0),
                Hkcu(GameDvr, "HistoricalCaptureEnabled", 0),
                Hklm(GameDvrPolicy, "AllowGameDVR", 0)),

            new SimpleTweakDefinition(
                "gpu.fso",
                Gpu,
                "gpu",
                "Отключить полноэкранные оптимизации",
                "Классический эксклюзивный полный экран вместо FSO. "
                    + "Помогает не всем играм; на некоторых даёт чёрный экран при Alt+Tab.",
                Hkcu(GameConfig, "GameDVR_FSEBehavior", 2),
                Hkcu(GameConfig, "GameDVR_FSEBehaviorMode", 2),
                Hkcu(GameConfig, "GameDVR_HonorUserFSEBehaviorMode", 1),
                Hkcu(GameConfig, "GameDVR_DXGIHonorFSEWindowsCompatible", 1)),

            // ---- Интерфейс ---------------------------------------------------
            new SimpleTweakDefinition(
                "ui.extensions",
                Ui,
                "ui",
                "Показывать расширения файлов",
                "HideFileExt=0. Полезно, чтобы не открыть .exe, замаскированный под картинку.",
                Hkcu(Advanced, "HideFileExt", 0)),

            new SimpleTweakDefinition(
                "ui.hiddenfiles",
                Ui,
                "ui",
                "Показывать скрытые файлы",
                "Hidden=1: скрытые файлы и папки становятся видимыми в проводнике.",
                Hkcu(Advanced, "Hidden", 1)),

            new SimpleTweakDefinition(
                "ui.syncnotifications",
                Ui,
                "ui",
                "Убрать уведомления синхронизации",
                "ShowSyncProviderNotifications=0: без баннеров OneDrive в проводнике.",
                Hkcu(Advanced, "ShowSyncProviderNotifications", 0)),

            new SimpleTweakDefinition(
                "ui.taskview",
                Ui,
                "ui",
                "Убрать кнопку «Представление задач»",
                "ShowTaskViewButton=0: освобождает место на панели задач.",
                Hkcu(Advanced, "ShowTaskViewButton", 0)),

            new SimpleTweakDefinition(
                "ui.taskbarend",
                Ui,
                "ui",
                "«Завершить задачу» в меню панели задач",
                "TaskbarEndTask=1: пункт завершения процесса по правому клику (Windows 11).",
                Hkcu(Advanced, "TaskbarEndTask", 1))
            {
                Windows11Only = true,
            },

            new SimpleTweakDefinition(
                "ui.seconds",
                Ui,
                "ui",
                "Секунды в часах панели задач",
                "ShowSecondsInSystemClock=1: часы показывают секунды.",
                Hkcu(Advanced, "ShowSecondsInSystemClock", 1)),

            new SimpleTweakDefinition(
                "ui.clipboard",
                Ui,
                "ui",
                "История буфера обмена",
                "EnableClipboardHistory=1: Win+V хранит последние скопированные элементы.",
                Hkcu(@"SOFTWARE\Microsoft\Clipboard", "EnableClipboardHistory", 1)),

            // ---- Дополнительно -----------------------------------------------
            new SimpleTweakDefinition(
                "priv.wer",
                Extra,
                "extra",
                "Отключить отчёты об ошибках",
                "Windows Error Reporting: без сбора и отправки дампов. "
                    + "Диагностика сбоев станет беднее.",
                Hklm(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting", "Disabled", 1),
                Hklm(@"SOFTWARE\Policies\Microsoft\Windows\HandwritingErrorReports", "PreventHandwritingErrorReports", 1)),

            new SimpleTweakDefinition(
                "priv.findmy",
                Extra,
                "extra",
                "Отключить «Найти моё устройство»",
                "AllowFindMyDevice=0: устройство нельзя найти через учётную запись Microsoft.",
                Hklm(@"SOFTWARE\Policies\Microsoft\FindMyDevice", "AllowFindMyDevice", 0)),

            new SimpleTweakDefinition(
                "priv.activity",
                Extra,
                "extra",
                "Отключить историю действий",
                "Не публиковать и не синхронизировать историю действий. "
                    + "Ломает «Хронологию» и продолжение задач на других устройствах.",
                Hklm(Policies + @"\System", "PublishUserActivities", 0),
                Hklm(Policies + @"\System", "EnableActivityFeed", 0),
                Hklm(Policies + @"\System", "UploadUserActivities", 0)),

            new SimpleTweakDefinition(
                "priv.deliveryopt",
                Extra,
                "extra",
                "Отключить P2P в обновлениях",
                "DODownloadMode=0: Delivery Optimization не раздаёт обновления другим ПК.",
                Hklm(Policies + @"\DeliveryOptimization", "DODownloadMode", 0)),

            new SimpleTweakDefinition(
                "sys.webdav",
                Extra,
                "extra",
                "Лимит WebDAV 4 ГБ",
                "FileSizeLimitInBytes: снимает ограничение 50 МБ для WebDAV-дисков.",
                Hklm(@"SYSTEM\CurrentControlSet\Services\WebClient\Parameters", "FileSizeLimitInBytes", unchecked((int)0xFFFFFFFF))),

            new SimpleTweakDefinition(
                "sys.longpaths",
                Extra,
                "extra",
                "Длинные пути (>260 символов)",
                "LongPathsEnabled=1. Приложения без манифеста longPathAware всё равно не увидят длинные пути.",
                Hklm(@"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 1)),

            new SimpleTweakDefinition(
                "sys.verbose",
                Extra,
                "extra",
                "Подробные сообщения при загрузке",
                "VerboseStatus=1: вместо «Подождите» видно, что именно делает Windows.",
                Hklm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus", 1)),
        };

        public static IEnumerable<SimpleTweakDefinition> All
        {
            get { return Definitions; }
        }

        public static IEnumerable<SimpleTweakDefinition> ForPanel(string panel)
        {
            foreach (SimpleTweakDefinition definition in Definitions)
            {
                if (string.Equals(definition.Panel, panel, StringComparison.OrdinalIgnoreCase))
                {
                    yield return definition;
                }
            }
        }

        private static TweakValue Hkcu(string path, string name, int value)
        {
            return new TweakValue(RegistryHive.CurrentUser, path, name, value);
        }

        private static TweakValue Hklm(string path, string name, int value)
        {
            return new TweakValue(RegistryHive.LocalMachine, path, name, value);
        }
    }
}
