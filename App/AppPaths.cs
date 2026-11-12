using System;
using System.IO;

namespace WOTBO.App
{
    /// <summary>
    /// Единая точка правды для путей приложения.
    ///
    /// Значения намеренно совпадают с тем, что раньше было разбросано по
    /// <c>MainForm</c>: <c>SysFolder</c> (папки Windows) и <c>tempfolder</c>
    /// (рабочая папка <c>c:\Windows\oixro</c>). Менять их без причины нельзя:
    /// от них зависят ресурсы, бэкапы и очистка temp.
    /// </summary>
    public static class AppPaths
    {
        /// <summary>Системный диск без обратного слэша в конце, например <c>C:</c>.</summary>
        public static string SystemDrive { get; } = Environment.ExpandEnvironmentVariables("%systemdrive%");

        /// <summary>Папка временных файлов текущего пользователя, например <c>C:\Users\wotbo\AppData\Local\Temp</c>.</summary>
        public static string Temp { get; } = Environment.ExpandEnvironmentVariables("%temp%");

        public static string UserProfile { get; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static string LocalAppData { get; } = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public static string RoamingAppData { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static string Desktop { get; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public static string Windows { get; } = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        /// <summary>
        /// <c>C:\Windows\System32</c>. Для x64-процесса на x64-системе совпадает с
        /// физической папкой; при появлении 32-битной сборки здесь нужен Sysnative.
        /// </summary>
        public static string System32 { get; } = Environment.GetFolderPath(Environment.SpecialFolder.System);

        public static string SysWOW64 { get; } = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);

        public static string ProgramFiles { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        public static string ProgramFilesX86 { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        public static string ProgramData { get; } = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        /// <summary>
        /// Основной лог. Путь сохранён прежним (<c>%TEMP%\wotbo.log</c>), чтобы
        /// F12, <c>build/run.ps1</c> и привычный сценарий диагностики работали как раньше.
        /// </summary>
        public static string LogFilePath { get; } = Path.Combine(Temp, "wotbo.log");

        /// <summary>
        /// Рабочая папка для распакованных ресурсов и временных утилит.
        /// При старте очищается (см. логику в <c>MainForm</c>), при неудаче
        /// заменяется папкой со случайным суффиксом.
        /// </summary>
        public static string WorkDirectory { get; } = @"c:\Windows\oixro";
    }
}
