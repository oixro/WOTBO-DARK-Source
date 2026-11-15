namespace WOTBO.Services.Processes
{
    /// <summary>Результат запуска внешнего процесса: вывод и код возврата.</summary>
    public sealed class ProcessOutput
    {
        public ProcessOutput(string text, int exitCode)
        {
            Text = text ?? string.Empty;
            ExitCode = exitCode;
        }

        public string Text { get; private set; }

        public int ExitCode { get; private set; }

        public bool Ok
        {
            get { return ExitCode == 0; }
        }
    }
}
