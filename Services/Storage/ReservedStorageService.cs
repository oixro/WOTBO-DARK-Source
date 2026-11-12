using System;
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
    /// DISM /Online /Get-ReservedStorageState.
    ///
    /// Раньше запускался в инициализаторе статического поля MainForm, из-за чего
    /// DISM стартовал до Load и процесс не освобождался. Теперь — явный
    /// асинхронный вызов в Load.
    /// </summary>
    public sealed class ReservedStorageService : IReservedStorageService
    {
        private const string Category = "ReservedStorage";

        private readonly IProcessService _process;
        private readonly IAppLogger _log;

        public ReservedStorageService(IProcessService process, IAppLogger log)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task<string> GetStateAsync()
        {
            return _process.ReadOutputAsync(
                "cmd.exe",
                "/c chcp 65001 && DISM /Online /Get-ReservedStorageState");
        }

        public bool IsDisabled(string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                return false;
            }

            bool disabled = state.IndexOf("Reserved storage is disabled.", StringComparison.OrdinalIgnoreCase) >= 0
                || state.IndexOf("отключено", StringComparison.OrdinalIgnoreCase) >= 0
                || state.IndexOf("‡ аҐ§ҐаўЁа®ў ­­®Ґ еа ­Ё«ЁйҐ ®вЄ«озҐ­®.", StringComparison.Ordinal) >= 0;

            _log.Debug(Category, disabled ? "reserved storage disabled" : "reserved storage enabled/unknown");
            return disabled;
        }
    }
}
