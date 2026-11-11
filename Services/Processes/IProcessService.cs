using System.Threading.Tasks;

namespace WOTBO.Services.Processes
{
    /// <summary>
    /// Запуск внешних процессов с прежней семантикой WOTBO.
    ///
    /// Важно: <see cref="StartCaptured"/> и <see cref="StartCapturedAsync"/>
    /// не ждут завершения — это бывшие <c>hcmd</c>/<c>hcmdAsync</c>.
    /// <see cref="RunSuper"/> ждёт и читает вывод, как бывший <c>supercmd</c>,
    /// потому что через него работают операции Defender.
    /// </summary>
    public interface IProcessService
    {
        /// <summary>
        /// Бывший <c>hcmd</c>: <c>cmd /c chcp 65001 &amp; command</c>,
        /// скрытое окно, stdout/stderr в лог, без ожидания.
        /// </summary>
        void StartCaptured(string command);

        /// <summary>Бывший <c>hcmdAsync</c>: то же самое через Task.Run.</summary>
        Task StartCapturedAsync(string command);

        /// <summary>
        /// Бывший <c>scmd</c>: <c>cmd /c command</c>. Если <paramref name="hidden"/>,
        /// окно скрывается (так делали Form2/Form4).
        /// </summary>
        void StartDetached(string command, bool hidden = false);

        /// <summary>Бывший <c>powershell</c>: <c>powershell /command command</c>, скрыто.</summary>
        void StartPowerShell(string command);

        /// <summary>
        /// Бывший <c>supercmd</c>: <c>{suPath} /wrs cmd.exe /c command</c>.
        /// Отдельный метод, не сливается с обычным запуском.
        /// </summary>
        void RunSuper(string command, string suPath);

        /// <summary>Открыть URL. Возвращает <c>false</c>, если браузер не открылся.</summary>
        bool OpenUrl(string url);
    }
}
