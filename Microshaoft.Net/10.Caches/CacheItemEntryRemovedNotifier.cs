#define cs4 //C# 4.0+
//#define cs2 //C# 2.0+
// /r:C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Runtime.Caching.dll
namespace Test
{
    using System;
#if NET45
    using System.Runtime.Caching;
#endif
    using System.Web;
    using System.Web.Caching;
    using System.Threading;
    using Microshaoft;
    public class Class1
    {
        static void Main(string[] args)
        {
            CacheItemEntryRemovedNotifier x = new CacheItemEntryRemovedNotifier("key1", 5);
            x.CacheItemEntryRemoved += new CacheItemEntryRemovedNotifier.CacheItemEntryRemovedEventHandler(x_CacheItemEntryRemoved);
            CacheItemEntryRemovedNotifier y = new CacheItemEntryRemovedNotifier("key2", 5);
            y.CacheItemEntryRemoved += new CacheItemEntryRemovedNotifier.CacheItemEntryRemovedEventHandler(x_CacheItemEntryRemoved);
            Thread.Sleep(2 * 1000);
            x.Remove();
            Console.WriteLine("Hello World");
            Console.WriteLine(Environment.Version.ToString());
            Console.ReadLine();
        }
        static void x_CacheItemEntryRemoved(CacheItemEntryRemovedNotifier sender, Enum reason)
        {

            if (reason is CacheItemRemovedReason)
            {
                Console.WriteLine(Enum.GetName(typeof(CacheItemRemovedReason), (CacheItemRemovedReason)reason));
            }
#if NET45
            else if (reason is CacheEntryRemovedReason)
            {
                Console.WriteLine(Enum.GetName(typeof(CacheEntryRemovedReason), (CacheEntryRemovedReason)reason));
            }
#endif
            Console.WriteLine(sender.Key);
            sender.ExpireSeconds = 10;
        }
    }
}
namespace Microshaoft
{
    using System;
#if NET45
    using System.Runtime.Caching;
#elif NET35
	using System.Web;
	using System.Web.Caching;
#endif
    using System.Threading;
    public class CacheItemEntryRemovedNotifier
    {
        public delegate void CacheItemEntryRemovedEventHandler
                                        (
                                                CacheItemEntryRemovedNotifier sender
                                                ,
            ///#if cs4
            ///													CacheEntryRemovedReason
            ///#elif cs2
            ///													CacheItemRemovedReason
            ///#endif
                                                    Enum
                                                    removedReason
                                         );
        public event CacheItemEntryRemovedEventHandler CacheItemEntryRemoved;
        private
#if NET45
                    MemoryCache
#elif NET35
					Cache
#endif
                        _cache = null;
        private string _key;
        public string Key
        {
            get
            {
                return _key;
            }
        }
        private uint _expireSeconds = 0;
        public uint ExpireSeconds
        {
            get
            {
                return _expireSeconds;
            }
            set
            {
                _expireSeconds = value;
            }
        }
        public CacheItemEntryRemovedNotifier(string key, uint expireSeconds)
        {
            _key = key;
#if NET45
            _cache = MemoryCache.Default;
#elif NE35
			HttpContext context = HttpContext.Current;
			if (context != null)
			{
				_cache = context.Cache;
			}
			else
			{
				_cache = HttpRuntime.Cache;
			}
#endif
            Add(key, expireSeconds);
        }
        private void Add(string key, uint expireSeconds)
        {
#if NET45
            CacheItem item = null;
            CacheItemPolicy cip = null;
            CacheEntryRemovedCallback removedCallback = null;
            _expireSeconds = expireSeconds;
            if (!_cache.Contains(key))
            {
                //实例化一个CacheItem缓存项
                item = new CacheItem(key, new object());
                //实例化CacheItemPolicy 并关联缓存项的一组逐出和过期详细信息
                cip = new CacheItemPolicy();
                removedCallback = new CacheEntryRemovedCallback(CacheEntryRemovedCallbackProcess);
                cip.RemovedCallback = removedCallback;
                DateTime expire = DateTime.Now.AddSeconds(_expireSeconds);
                cip.AbsoluteExpiration = new DateTimeOffset(expire);
                //将缓存实例添加到系统缓存
                _cache.Add(item, cip);
            }
#elif NET35
			CacheItemRemovedCallback removedCallback = new CacheItemRemovedCallback(CacheItemRemovedCallbackProcess);
			_cache.Insert
						(
							key
							, new object()
							, null
							, Cache.NoAbsoluteExpiration
							, TimeSpan.FromSeconds(expireSeconds)
							, CacheItemPriority.Normal
							, removedCallback
						 );
#endif
        }
        public void Start(uint expireSeconds)
        {
            _expireSeconds = expireSeconds;
            Add(_key, _expireSeconds);
        }
        public void Remove()
        {
            _cache.Remove(_key);
            //_expireSeconds = 0;
        }
        public void Stop()
        {
            _cache.Remove(_key);
            _expireSeconds = 0;
        }
#if NET45
        private void CacheEntryRemovedCallbackProcess(CacheEntryRemovedArguments cera)
        {
            if (CacheItemEntryRemoved != null)
            {
                CacheItemEntryRemoved(this, cera.RemovedReason);
            }
            if (_expireSeconds > 0)
            {
                Add(_key, _expireSeconds);
            }
        }
#elif NET35
		private void CacheItemRemovedCallbackProcess(string key, object cacheItem, CacheItemRemovedReason removedReason)
		{
			if (CacheItemEntryRemoved != null)
			{
				CacheItemEntryRemoved(this, removedReason);
			}
			if (_expireSeconds > 0)
			{
				Add(_key, _expireSeconds);
			}
		}
#endif
    }
}
