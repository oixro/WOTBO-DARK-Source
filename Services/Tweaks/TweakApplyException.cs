using System;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Твик не смог примениться по понятной пользователю причине
    /// (нет сети, не скачался ресурс, нет прав). Текст уходит в окно.
    /// </summary>
    public sealed class TweakApplyException : Exception
    {
        public TweakApplyException(string message) : base(message)
        {
        }

        public TweakApplyException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
