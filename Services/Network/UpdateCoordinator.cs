using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WOTBO.App.Logging;
using WOTBO.Services.Network;

namespace WOTBO.Services.Network
{
    /// <summary>Проверка и установка обновления WOTBO.</summary>
    public sealed class UpdateCoordinator
    {
        private const string Category = "Update";

        /// <summary>
        /// Откуда качать новый exe. Зеркала для бинарника нет: jsDelivr отдаёт
        /// на .exe 403, остальные CDN либо медленные, либо нестабильные.
        /// Если raw.githubusercontent.com заблокирован, показываем ссылку на
        /// ручную загрузку.
        /// </summary>
        private const string UpdateUrl = "https://raw.githubusercontent.com/oixro/WOTBO/main/WOTBO.exe";

        /// <summary>Страница, с которой можно скачать новую версию руками.</summary>
        public const string ManualDownloadUrl = "https://github.com/oixro/WOTBO";

        private readonly IGitHubUpdateService _updates;
        private readonly INetworkService _network;
        private readonly IAppLogger _log;

        public UpdateCoordinator(
            IGitHubUpdateService updates,
            INetworkService network,
            IAppLogger log)
        {
            _updates = updates ?? throw new ArgumentNullException(nameof(updates));
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task<UpdateInfo> CheckAsync(string localVersion)
        {
            return _updates.CheckAsync(localVersion);
        }

        /// <summary>
        /// Скачать и запустить установку. Загрузка идёт в фоне: раньше она
        /// выполнялась в UI-потоке и окно замирало на всё время скачивания.
        /// </summary>
        public async Task InstallAsync(UpdateInfo update, string exename, string exepath, string contexpath)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));

            await Task.Run(() => DownloadNewVersion());

            File.WriteAllText("lastchange.json", update.ChangeLog ?? string.Empty);

            string command =
                $"taskkill /f /im \"{exename}\" &&" +
                " timeout /t 1 &&" +
                $" del \"{exepath}\" /f /q &&" +
                $" del \"{contexpath}\" /f /q &&" +
                " del \"lastchange.json\" /f /q &&" +
                $" copy /y new.exe \"{contexpath}\" &&" +
                $" ren new.exe \"{exename}\" &&" +
                " del \"new.exe\" /f /q &" +
                $" \"{exepath}\"";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
            }
            catch (Exception exception)
            {
                _log.Warn(Category, "не удалось запустить обновление: " + exception.Message);
            }
        }

        /// <summary>
        /// Скачивает новый exe с первого рабочего источника.
        ///
        /// Без проверки вместо exe мог оказаться HTML от прокси или captive
        /// portal: программа заменила бы себя нерабочим файлом, поэтому файл
        /// проверяется (PE-заголовок и размер) и при неудаче берётся зеркало.
        /// </summary>
        private void DownloadNewVersion()
        {
            _log.Info(Category, "скачиваем новую: " + UpdateUrl);
            _network.DownloadFile(UpdateUrl, "new.exe");

            if (LooksLikeExecutable("new.exe"))
            {
                return;
            }

            TryDelete("new.exe");
            throw new InvalidDataException("скачанный файл не похож на WOTBO.exe");
        }

        /// <summary>PE-файл начинается с «MZ»; плюс отсекаем заведомо короткие ответы.</summary>
        private static bool LooksLikeExecutable(string path)
        {
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists || file.Length < 100 * 1024) return false;

                using (FileStream stream = File.OpenRead(path))
                {
                    return stream.ReadByte() == 'M' && stream.ReadByte() == 'Z';
                }
            }
            catch
            {
                return false;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // мусорный файл не должен мешать сообщить об ошибке
            }
        }
    }
}
