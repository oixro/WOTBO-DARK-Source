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
        private const string UpdateUrl = "https://raw.githubusercontent.com/oixro/WOTBO/main/WOTBO.exe";

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

        public void Install(UpdateInfo update, string exename, string exepath, string contexpath)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));

            File.WriteAllText("lastchange.json", update.ChangeLog ?? string.Empty);
            _log.Info(Category, "скачиваем новую");
            _network.DownloadFile(UpdateUrl, "new.exe");

            string command =
                $"taskkill /f /im \"{exename}\" &&" +
                " timeout /t 1 &&" +
                $" del \"{exepath}\" /f /q &&" +
                $" del \"{contexpath}\" /f /q &&" +
                " del \"lastchange.json\" /f /q &&" +
                $" copy new.exe \"{contexpath}\" &&" +
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
    }
}
