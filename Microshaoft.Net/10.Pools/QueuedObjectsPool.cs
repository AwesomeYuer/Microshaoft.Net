
namespace Microshaoft
{
    using System.Collections.Concurrent;
    public class QueuedObjectsPool<T> where T: new()
    {
        private ConcurrentQueue<T> _pool = new ConcurrentQueue<T>();
        public QueuedObjectsPool(int capacity)
        {
            _pool = new ConcurrentQueue<T>();
            for (int i = 0; i < capacity; i++)
            {
                PutNew();
            }
        }

        public void PutNew()
        {
            var e = default(T);
            e = new T();
            Put(e);
        }

        public bool Put(T target)
        {
            var r = false;
            if (target != null)
            {
               _pool.Enqueue(target);
               r = true;
            }
            return r;
            
        }

        public T Get()
        { 
            T r;
            while (!_pool.TryDequeue(out r))
            {
                PutNew();
            }
            return r;
        }

    }
}
