namespace WOTBO.App.Logging
{
    /// <summary>Уровень записи. Порядок важен: больше значение — важнее сообщение.</summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
    }
}
