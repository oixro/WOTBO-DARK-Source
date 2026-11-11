using System;
using System.Threading;

namespace WOTBO.App
{
    /// <summary>
    /// Один экземпляр приложения. Заменяет прежний <c>InstanceChecker</c>.
    /// </summary>
    public static class InstanceGuard
    {
        private static readonly Mutex Mutex =
            new Mutex(false, AppDomain.CurrentDomain.FriendlyName.ToLowerInvariant());

        private static bool _taken;

        public static bool TryAcquire()
        {
            _taken = Mutex.WaitOne(0, true);
            return _taken;
        }

        public static void Release()
        {
            if (!_taken)
            {
                return;
            }

            try
            {
                Mutex.ReleaseMutex();
            }
            catch
            {
                // Повторное освобождение не должно ронять приложение.
            }
            finally
            {
                _taken = false;
            }
        }
    }
}
