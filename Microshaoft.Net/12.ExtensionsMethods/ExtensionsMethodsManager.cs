namespace Microshaoft
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    

    public static partial class ExtensionsMethodsManager
    {
        public static void ForEach<TKey, TValue>
                                (
                                    this ConcurrentDictionary<TKey, TValue> concurrentDictionary
                                    , Func<TKey, TValue, bool> processFunc
                                )
        {
            foreach (KeyValuePair<TKey, TValue> kvp in concurrentDictionary)
            {
                bool r = processFunc(kvp.Key, kvp.Value);
                if (r)
                {
                    break;
                }
            }
        }


        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where
                                (
                                    (x) =>
                                    {
                                        return x != null;
                                    }
                                );
            }
        }

       




    }
}
