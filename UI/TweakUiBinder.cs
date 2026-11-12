using System;
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
        private readonly bool _disableOnApply;
        private readonly bool _showRollbackOnApply;
        private readonly bool _showRollbackOnDetect;
        private readonly bool _uncheckOnDetect;

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
            bool disableOnApply = true,
            bool showRollbackOnApply = true,
            bool showRollbackOnDetect = true,
            bool uncheckOnDetect = false)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id пуст", nameof(id));
            if (checkBox == null) throw new ArgumentNullException(nameof(checkBox));

            _id = id;
            _checkBox = checkBox;
            _rollback = rollback;
            _detectMessage = detectMessage;
            _onDetected = onDetected;
            _beforeApply = beforeApply;
            _inApply = inApply;
            _afterApply = afterApply;
            _inRollback = inRollback;
            _afterRollback = afterRollback;
            _confirmApply = confirmApply;
            _confirmRollback = confirmRollback;
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
            TweakState state = Tweak().Detect(AppServices.TweakContext);
            AppServices.Tweaks.Reconcile(_id, state == TweakState.Applied);

            if (state != TweakState.Applied)
            {
                return false;
            }

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

            return true;
        }

        /// <summary>Применить, если чекбокс отмечен.</summary>
        public void Apply()
        {
            if (!_checkBox.Checked)
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

            using (TweakScope.Apply(_id))
            {
                Tweak().Apply(AppServices.TweakContext);
                if (_inApply != null)
                {
                    _inApply();
                }
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

            using (TweakScope.Rollback(_id))
            {
                Tweak().Rollback(AppServices.TweakContext);
                if (_inRollback != null)
                {
                    _inRollback();
                }
            }

            _checkBox.Checked = false;
            _checkBox.Enabled = true;
            if (_rollback != null) _rollback.Visible = false;

            if (_afterRollback != null)
            {
                _afterRollback();
            }
        }

        private ITweak Tweak()
        {
            return AppServices.TweakCatalog.Get(_id);
        }
    }
}
