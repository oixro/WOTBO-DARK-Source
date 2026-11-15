using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WOTBO.App.Logging;

namespace WOTBO.Services.Processes
{
    /// <summary>
    /// Реализация <see cref="IProcessService"/>.
    ///
    /// Семантика запуска сохранена: <c>hcmd</c> по-прежнему не блокирует
    /// вызывающий поток, <c>supercmd</c> по-прежнему ждёт завершения и
    /// складывает stdout+stderr в лог.
    /// </summary>
    public sealed class ProcessService : IProcessService
    {
        private const string Category = "Process";

        private readonly IAppLogger _log;

        /// <summary>
        /// Держим ссылки на запущенные процессы, иначе GC может собрать
        /// объект до Exited и потерять stdout/stderr.
        /// </summary>
        private readonly object _sync = new object();
        private readonly System.Collections.Generic.HashSet<Process> _running =
            new System.Collections.Generic.HashSet<Process>();

        public ProcessService(IAppLogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void StartCaptured(string command)
        {
            try
            {
                _log.Info(Category, "[CMD] Command: " + command);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c chcp 65001 & {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                var process = new Process
                {
                    StartInfo = psi,
                    EnableRaisingEvents = true,
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _log.Debug(Category, "[CMD] Output: " + e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _log.Warn(Category, "[CMD] Error: " + e.Data);
                    }
                };

                process.Exited += (sender, e) =>
                {
                    try
                    {
                        // Дожидаемся окончания асинхронного чтения, чтобы
                        // последние строки попали в лог.
                        process.WaitForExit();
                    }
                    catch
                    {
                    }
                    finally
                    {
                        lock (_sync)
                        {
                            _running.Remove(process);
                        }

                        process.Dispose();
                    }
                };

                lock (_sync)
                {
                    _running.Add(process);
                }

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                _log.Error(Category, "[CMD] Error: " + ex.Message, ex);
            }
        }

        public Task StartCapturedAsync(string command)
        {
            return Task.Run(() => StartCaptured(command));
        }

        public void StartDetached(string command, bool hidden = false)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                };

                if (hidden)
                {
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                _log.Error(Category, "[CMD] Не удалось запустить: " + command, ex);
            }
        }

        public void StartPowerShell(string command)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"/command {command}",
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
            }
            catch (Exception ex)
            {
                _log.Error(Category, "[PowerShell] Не удалось запустить: " + command, ex);
            }
        }

        public void RunSuper(string command, string suPath)
        {
            try
            {
                _log.Info(Category, "[SUPER] Command: " + command);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $@"/c {suPath} /wrs cmd.exe /c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using (var process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    string result = output + error;
                    if (!string.IsNullOrEmpty(result))
                    {
                        _log.Debug(Category, "[SUPER] " + result);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(Category, "[CMD] Произошла ошибка: " + ex.Message, ex);
            }
        }

        public bool OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url));
                return true;
            }
            catch (Exception ex)
            {
                _log.Warn(Category, "не удалось открыть ссылку " + url + ": " + ex.Message);
                return false;
            }
        }

        public Task<string> ReadOutputAsync(string fileName, string arguments)
        {
            return ReadOutputAsync(fileName, arguments, 0, null);
        }

        /// <summary>
        /// То же самое, но с ограничением времени: чужой exe может зависнуть
        /// (например, NvCameraEnable.exe ждёт ввода), и без таймаута вызов
        /// висел бы вечно.
        /// </summary>
        public Task<string> ReadOutputAsync(string fileName, string arguments, int timeoutMs)
        {
            return ReadOutputAsync(fileName, arguments, timeoutMs, null);
        }

        /// <param name="encoding">
        /// Кодировка вывода, если процесс пишет не в системной ANSI
        /// (например, PowerShell с <c>[Console]::OutputEncoding = UTF8</c>).
        /// </param>
        public Task<string> ReadOutputAsync(string fileName, string arguments, int timeoutMs, System.Text.Encoding encoding)
        {
            return Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments ?? string.Empty,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    if (encoding != null)
                    {
                        psi.StandardOutputEncoding = encoding;
                        psi.StandardErrorEncoding = encoding;
                    }

                    using (var process = new Process())
                    {
                        process.StartInfo = psi;
                        process.Start();

                        // Читаем асинхронно: ReadToEnd() заблокировался бы навсегда,
                        // если процесс не закрывает stdout.
                        Task<string> output = process.StandardOutput.ReadToEndAsync();
                        Task<string> error = process.StandardError.ReadToEndAsync();

                        if (timeoutMs > 0 && !process.WaitForExit(timeoutMs))
                        {
                            _log.Warn(Category, "не ответил за " + timeoutMs + " мс, завершаем: " + fileName);

                            try
                            {
                                process.Kill();
                            }
                            catch
                            {
                                // процесс мог успеть выйти сам
                            }

                            process.WaitForExit(2000);
                        }
                        else
                        {
                            process.WaitForExit();
                        }

                        string text = Finish(output);
                        string err = Finish(error);

                        if (!string.IsNullOrEmpty(err))
                        {
                            _log.Debug(Category, "[OUTPUT] " + err.Trim());
                        }

                        return text + err;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(Category, "не удалось прочитать вывод: " + fileName + " " + arguments, ex);
                    return string.Empty;
                }
            });
        }

        public Task<ProcessOutput> RunAsync(string fileName, string arguments, int timeoutMs, System.Text.Encoding encoding)
        {
            return Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments ?? string.Empty,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };

                    if (encoding != null)
                    {
                        psi.StandardOutputEncoding = encoding;
                        psi.StandardErrorEncoding = encoding;
                    }

                    using (var process = new Process())
                    {
                        process.StartInfo = psi;
                        process.Start();

                        Task<string> output = process.StandardOutput.ReadToEndAsync();
                        Task<string> error = process.StandardError.ReadToEndAsync();

                        if (timeoutMs > 0 && !process.WaitForExit(timeoutMs))
                        {
                            _log.Warn(Category, "не ответил за " + timeoutMs + " мс, завершаем: " + fileName);

                            try
                            {
                                process.Kill();
                            }
                            catch
                            {
                            }

                            process.WaitForExit(2000);
                        }
                        else
                        {
                            process.WaitForExit();
                        }

                        string text = Finish(output);
                        string err = Finish(error);

                        if (!string.IsNullOrEmpty(err))
                        {
                            _log.Debug(Category, "[OUTPUT] " + err.Trim());
                        }

                        int code = -1;
                        try
                        {
                            code = process.ExitCode;
                        }
                        catch
                        {
                        }

                        return new ProcessOutput(text + err, code);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(Category, "не удалось выполнить: " + fileName + " " + arguments, ex);
                    return new ProcessOutput(string.Empty, -1);
                }
            });
        }

        private static string Finish(Task<string> read)
        {
            try
            {
                return read.Wait(2000) ? read.Result : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
