namespace Microshaoft
{
    using System;
    using System.Threading;
    public static class ReaderWriterLockSlimHelper
    {
        public static bool TryEnterWriterLockSlimWrite<T>
                                                 (
                                                     ref T target
                                                    , T newTarget
                                                    , int enterTimeOutSeconds
                                                 )
                                                    where T : class
        {
            bool r = false;
            var rwls = new ReaderWriterLockSlim();
            int timeOut = Timeout.Infinite;
            if (enterTimeOutSeconds >= 0)
            {
                timeOut = enterTimeOutSeconds * 1000;
            }
            try
            {
                r = (rwls.TryEnterWriteLock(timeOut));
                if (r)
                {
                    Interlocked.Exchange<T>(ref target, newTarget);
                    r = true;
                }
            }
            finally
            {
                if (r)
                {
                    rwls.ExitWriteLock();
                }
            }
            return r;
        }
        public static bool TryEnterWriterLockSlimWrite
                                (
                                    Action action
                                    , int enterTimeOutSeconds
                                )
        {
            bool r = false;
            if (action != null)
            {
                var rwls = new ReaderWriterLockSlim();
                int timeOut = Timeout.Infinite;
                if (enterTimeOutSeconds >= 0)
                {
                    timeOut = enterTimeOutSeconds * 1000;
                }
                try
                {
                    r = (rwls.TryEnterWriteLock(timeOut));
                    if (r)
                    {
                        action();
                        r = true;
                    }
                }
                finally
                {
                    if (r)
                    {
                        rwls.ExitWriteLock();
                    }
                }
            }
            return r;
        }
    }
}