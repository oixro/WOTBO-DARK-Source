using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WOTBO.App;
using WOTBO.App.Logging;
using WOTBO.Services.Tweaks;

namespace WOTBO.UI
{
    /// <summary>
    /// Связка «чекбокс + стрелка отката + ITweak»: detect при старте,
    /// apply и rollback. Специфичные для UI шаги передаются колбэками,
    /// поэтому поведение остаётся прежним.
    /// </summary>
    public sealed class TweakUiBinder
    {
        private readonly string _id;
        private readonly CheckBox _checkBox;
        private readonly PictureBox _rollback;
        private readonly string _detectMessage;
        private readonly Action _onDetected;
        private readonly Action _beforeApply;
        private readonly Action _inApply;
        private readonly Action _afterApply;
        private readonly Action _inRollback;
        private readonly Action _afterRollback;
        private readonly Func<bool> _confirmApply;
        private readonly Func<bool> _confirmRollback;
        private readonly Func<Action, bool> _wrap;
        private readonly bool _disableOnApply;
        private readonly bool _showRollbackOnApply;
        private readonly bool _showRollbackOnDetect;
        private readonly bool _uncheckOnDetect;

        /// <summary>UI-контекст: detect идёт в фоне, а ShowApplied — в UI-потоке.</summary>
        private readonly SynchronizationContext _ui;

        public TweakUiBinder(
            string id,
            CheckBox checkBox,
            PictureBox rollback,
            string detectMessage = null,
            Action onDetected = null,
            Action beforeApply = null,
            Action inApply = null,
            Action afterApply = null,
            Action inRollback = null,
            Action afterRollback = null,
            Func<bool> confirmApply = null,
            Func<bool> confirmRollback = null,
            Func<Action, bool> wrap = null,
            bool disableOnApply = true,
            bool showRollbackOnApply = true,
            bool showRollbackOnDetect = true,
            bool uncheckOnDetect = false)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id пуст", nameof(id));
            if (checkBox == null) throw new ArgumentNullException(nameof(checkBox));

            _id = id;
            _checkBox = checkBox;
            _checkBox.ForeColor = System.Drawing.Color.White;
            _checkBox.BackColor = System.Drawing.Color.Transparent;
            _rollback = rollback;
            _ui = SynchronizationContext.Current;
            _detectMessage = detectMessage;
            _onDetected = onDetected;
            _beforeApply = beforeApply;
            _inApply = inApply;
            _afterApply = afterApply;
            _inRollback = inRollback;
            _afterRollback = afterRollback;
            _confirmApply = confirmApply;
            _confirmRollback = confirmRollback;
            _wrap = wrap;
            _disableOnApply = disableOnApply;
            _showRollbackOnApply = showRollbackOnApply;
            _showRollbackOnDetect = showRollbackOnDetect;
            _uncheckOnDetect = uncheckOnDetect;
        }

        public string Id
        {
            get { return _id; }
        }

        /// <summary>Detect при старте. Возвращает true, если твик применён.</summary>
        public bool Detect()
        {
            var watch = Stopwatch.StartNew();
            TweakState state = Tweak().Detect(AppServices.TweakContext);
            watch.Stop();

            if (watch.ElapsedMilliseconds >= 8)
            {
                Log.Debug("Tweak:" + _id, $"detect {watch.ElapsedMilliseconds} мс");
            }

            if (state == TweakState.Unavailable)
            {
                ShowUnavailable();
                return false;
            }

            AppServices.Tweaks.Reconcile(_id, state == TweakState.Applied);

            if (state != TweakState.Applied)
            {
                return false;
            }

            ShowApplied();
            return true;
        }

        /// <summary>
        /// Тяжёлый Detect (WMI, netsh, PowerShell) в фоне: окно не замирает,
        /// а состояние применяется уже в UI-потоке.
        /// </summary>
        public void DetectInBackground()
        {
            Task.Run(delegate
            {
                TweakState state;
                var watch = Stopwatch.StartNew();

                try
                {
                    state = Tweak().Detect(AppServices.TweakContext);
                }
                catch (Exception exception)
                {
                    Log.Error("Tweak:" + _id, "detect: " + exception.Message, exception);
                    return;
                }

                watch.Stop();

                if (watch.ElapsedMilliseconds >= 8)
                {
                    Log.Debug("Tweak:" + _id, $"detect {watch.ElapsedMilliseconds} мс");
                }

                if (state == TweakState.Unavailable)
                {
                    if (_ui != null)
                    {
                        _ui.Post(delegate { ShowUnavailable(); }, null);
                    }
                    else if (_checkBox.IsHandleCreated)
                    {
                        _checkBox.BeginInvoke(new Action(ShowUnavailable));
                    }

                    return;
                }

                AppServices.Tweaks.Reconcile(_id, state == TweakState.Applied);

                if (state != TweakState.Applied)
                {
                    return;
                }

                // Вкладка могла ещё ни разу не открываться: у контролов на
                // скрытой панели нет хендла, старый BeginInvoke молча не
                // выполнялся — и твик выглядел доступным, хотя уже применён.
                if (_ui != null)
                {
                    _ui.Post(delegate { ShowApplied(); }, null);
                    return;
                }

                if (_checkBox.IsHandleCreated)
                {
                    _checkBox.BeginInvoke(new Action(ShowApplied));
                }
            });
        }

        /// <summary>Твик не применим на этой системе: строку оставляем, но выключаем.</summary>
        private void ShowUnavailable()
        {
            _checkBox.Enabled = false;
            _checkBox.Checked = false;
            if (_rollback != null)
            {
                _rollback.Visible = false;
            }
        }

        /// <summary>Пометки «применён» в интерфейсе (всегда в UI-потоке).</summary>
        private void ShowApplied()
        {
            if (!string.IsNullOrEmpty(_detectMessage))
            {
                Log.Info("Tweak:" + _id, _detectMessage);
            }

            _checkBox.Enabled = false;
            if (_uncheckOnDetect)
            {
                _checkBox.Checked = false;
            }

            if (_showRollbackOnDetect)
            {
                if (_rollback != null) _rollback.Visible = true;
            }

            if (_onDetected != null)
            {
                _onDetected();
            }
        }

        /// <summary>Применить, если чекбокс отмечен.</summary>
        public void Apply()
        {
            if (!_checkBox.Checked || !_checkBox.Enabled)
            {
                return;
            }

            if (_confirmApply != null && !_confirmApply())
            {
                _checkBox.Checked = false;
                return;
            }

            if (_beforeApply != null)
            {
                _beforeApply();
            }

            try
            {
                RunWrapped(delegate
                {
                    using (TweakScope scope = TweakScope.Apply(_id))
                    {
                        try
                        {
                            Tweak().Apply(AppServices.TweakContext);

                            if (_inApply != null)
                            {
                                _inApply();
                            }
                        }
                        catch
                        {
                            scope.Abort();
                            throw;
                        }
                    }
                });
            }
            catch (Exception exception)
            {
                _checkBox.Checked = false;
                _checkBox.Enabled = true;
                _checkBox.Invalidate();

                Log.Error("Tweak:" + _id, "применение не удалось", exception);

                string message = exception is TweakApplyException
                    ? exception.Message
                    : "Не удалось применить твик.\n\n" + exception.Message;

                MessageBox.Show(message, "WOTBO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _checkBox.Checked = false;
            if (_disableOnApply)
            {
                _checkBox.Enabled = false;
            }

            if (_showRollbackOnApply)
            {
                if (_rollback != null) _rollback.Visible = true;
            }

            if (_afterApply != null)
            {
                _afterApply();
            }
        }

        /// <summary>Штатный откат из обработчика стрелки.</summary>
        public void Rollback()
        {
            if (_confirmRollback != null && !_confirmRollback())
            {
                return;
            }

            RunWrapped(delegate
            {
                using (TweakScope.Rollback(_id))
                {
                    Tweak().Rollback(AppServices.TweakContext);
                    if (_inRollback != null)
                    {
                        _inRollback();
                    }
                }
            });

            _checkBox.Checked = false;
            _checkBox.Enabled = true;
            if (_rollback != null) _rollback.Visible = false;

            if (_afterRollback != null)
            {
                _afterRollback();
            }
        }

        /// <summary>
        /// Выполнить блок в обёртке (например, в окне прогресса). Ошибку
        /// обёртки не глотаем: она уже записана в лог вызывающей стороны.
        /// </summary>
        private void RunWrapped(Action action)
        {
            if (_wrap == null)
            {
                action();
                return;
            }

            _wrap(action);
        }

        private ITweak Tweak()
        {
            return AppServices.TweakCatalog.Get(_id);
        }
    }
}
