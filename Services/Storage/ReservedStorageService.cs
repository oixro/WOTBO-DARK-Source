using System;
using System.Text;
using System.Threading.Tasks;
using WOTBO.App.Logging;
using WOTBO.Services.Processes;

namespace WOTBO.Services.Storage
{
    public interface IReservedStorageService
    {
        Task<string> GetStateAsync();

        bool IsDisabled(string state);
    }

    /// <summary>
    /// Состояние зарезервированного хранилища Windows.
    ///
    /// Раньше брали только DISM и искали английское «Reserved storage is
    /// disabled.» или русское «отключено». На русской Windows DISM пишет в
    /// кодировке консоли, поэтому строка приходила мусором, разбор не срабатывал
    /// и твик всегда выглядел неприменённым. Теперь спрашиваем
    /// <c>Get-WindowsReservedStorageState</c> с английской культурой и UTF-8,
    /// а DISM остаётся запасным вариантом.
    /// </summary>
    public sealed class ReservedStorageService : IReservedStorageService
    {
        private const string Category = "ReservedStorage";

        private const string StateCommand =
            "-NoProfile -Command \"[Console]::OutputEncoding=[Text.Encoding]::UTF8; " +
            "[Threading.Thread]::CurrentThread.CurrentUICulture='en-US'; " +
            "(Get-WindowsReservedStorageState).ReservedStorageState\"";

        private readonly IProcessService _process;
        private readonly IAppLogger _log;

        public ReservedStorageService(IProcessService process, IAppLogger log)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<string> GetStateAsync()
        {
            try
            {
                string state = await _process
                    .ReadOutputAsync("powershell.exe", StateCommand, 15000, Encoding.UTF8)
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(state))
                {
                    return state.Trim();
                }
            }
            catch (Exception exception)
            {
                _log.Debug(Category, "Get-WindowsReservedStorageState: " + exception.Message);
            }

            // Запасной путь для систем без командлета (Win10 до 2004 и т.п.).
            return await _process
                .ReadOutputAsync("cmd.exe", "/c chcp 65001 && DISM /Online /Get-ReservedStorageState", 20000, Encoding.UTF8)
                .ConfigureAwait(false);
        }

        public bool IsDisabled(string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                return false;
            }

            string text = state.ToLowerInvariant();

            if (text.Contains("disabled") || text.Contains("отключ") || text.Contains("выключ"))
            {
                _log.Debug(Category, "reserved storage disabled");
                return true;
            }

            if (text.Contains("enabled") || text.Contains("включ"))
            {
                _log.Debug(Category, "reserved storage enabled");
                return false;
            }

            _log.Debug(Category, "reserved storage unknown: " + Short(state));
            return false;
        }

        private static string Short(string text)
        {
            string line = (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            return line.Length <= 120 ? line : line.Substring(0, 120) + "…";
        }
    }
}
