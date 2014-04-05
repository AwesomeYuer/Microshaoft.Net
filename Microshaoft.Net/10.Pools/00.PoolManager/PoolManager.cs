

namespace Microshaoft
{
    public static class PoolManager
    {
        private static AutoResetEventsPool _autoResetEventsWaitersPool = new AutoResetEventsPool(0);

        public static AutoResetEventsPool AutoResetEventsWaitersPool
        {
            get { return PoolManager._autoResetEventsWaitersPool; }
            set { PoolManager._autoResetEventsWaitersPool = value; }
        }


    }
}

namespace Microshaoft
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Collections.Concurrent;
    public class AutoResetEventsPool
    {
        private ConcurrentQueue<AutoResetEvent> _pool = new ConcurrentQueue<AutoResetEvent>();
        public AutoResetEventsPool(int capacity)
        {
            _pool = new ConcurrentQueue<AutoResetEvent>();
            for (int i = 0; i < capacity; i++)
            {
                PutNew();
            }
        }

        public void PutNew()
        {
            var e = new AutoResetEvent(false);
            Put(e);
        }

        public bool Put(AutoResetEvent target)
        {
            var r = false;
            if (target != null)
            {
                _pool.Enqueue(target);
                r = true;
            }
            return r;

        }

        public AutoResetEvent Get()
        {
            AutoResetEvent r;
            while (!_pool.TryDequeue(out r))
            {
                PutNew();
            }
            return r;
        }

    }
}
