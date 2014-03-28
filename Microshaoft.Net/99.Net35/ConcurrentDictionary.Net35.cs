#if NET35
namespace System.Collections.Concurrent
{
    using System;
    using System.Collections.Generic;
    public class ConcurrentDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private object _locker = new object();
        public bool TryAdd(TKey key, TValue val)
        {
            var r = false;
            try
            {
                lock (_locker)
                {
                    base.Add(key, val);
                }
                
            }
            catch
            {
                r = false;
            }

            return r;
        }
    }

}
#endif