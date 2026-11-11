using System;

namespace WOTBO.App.Logging
{
    /// <summary>
    /// Лог приложения. Реализация обязана быть потокобезопасной: логирование
    /// вызывается из обработчиков процессов, WMI-потоков и UI одновременно.
    /// </summary>
    public interface IAppLogger
    {
        /// <summary>Куда пишется лог. Используется F12-хвостом и диагностикой.</summary>
        string FilePath { get; }

        /// <summary>
        /// Минимальный уровень для записей с уровнем. На прежний
        /// <see cref="Write(string)"/> не влияет: он всегда пишется.
        /// </summary>
        LogLevel MinimumLevel { get; set; }

        /// <summary>
        /// Запись без уровня в прежнем формате <c>время - текст</c>.
        /// Оставлена для совместимости; в приложении используется
        /// <see cref="Write(LogLevel, string, string)"/>.
        /// </summary>
        void Write(string message);

        void Write(LogLevel level, string category, string message);

        void Trace(string category, string message);

        void Debug(string category, string message);

        void Info(string category, string message);

        void Warn(string category, string message);

        /// <summary>Ошибка с необязательным исключением: в лог попадёт полный стек.</summary>
        void Error(string category, string message, Exception exception = null);
    }
}
