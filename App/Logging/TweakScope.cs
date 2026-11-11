using System.Diagnostics;
using System.Globalization;

namespace WOTBO.App.Logging
{
    /// <summary>
    /// Область одного твика: пишет в лог начало, конец и длительность.
    ///
    /// <code>
    /// using (TweakScope.Apply("main.mpo"))
    /// {
    ///     // изменение реестра/файлов
    /// }
    /// // [INFO ] [Tweak:main.mpo] применение начато
    /// // [INFO ] [Tweak:main.mpo] применение завершено за 12 мс
    /// </code>
    ///
    /// Если внутри произошло исключение, оно уходит вызывающему коду и
    /// попадает в лог как <c>ERROR</c>; строка о завершении при этом тоже
    /// будет, поэтому ориентируйся на уровень: ошибка всегда важнее.
    /// </summary>
    public sealed class TweakScope : System.IDisposable
    {
        private readonly Stopwatch _watch;
        private readonly string _category;
        private readonly string _action;

        private TweakScope(string id, string action)
        {
            _category = "Tweak:" + id;
            _action = action;
            _watch = Stopwatch.StartNew();
            Log.Info(_category, _action + " начато");
        }

        public static TweakScope Apply(string id)
        {
            return new TweakScope(id, "применение");
        }

        public static TweakScope Rollback(string id)
        {
            return new TweakScope(id, "откат");
        }

        public string Category
        {
            get { return _category; }
        }

        public void Dispose()
        {
            Log.Info(_category, string.Format(
                CultureInfo.InvariantCulture,
                "{0} завершено за {1} мс",
                _action,
                _watch.ElapsedMilliseconds));
        }
    }
}
