using System;
using System.Threading;

namespace WOTBO
{
    internal class InstanceChecker
    {
        static readonly Mutex mutex = new Mutex(false, AppDomain.CurrentDomain.FriendlyName.ToLower());
        static bool taken;
        public static bool TakeMemory() => taken = mutex.WaitOne(0, true);

        public static void ReleaseMemory()
        {
            if (taken)
                try { mutex.ReleaseMutex(); } catch { }
        }
    }
}
