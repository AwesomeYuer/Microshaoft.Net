namespace Microshaoft
{
    using System;
    using System.Diagnostics;
    public static class PerformanceCounterExtensionMethodsManager
    {
        public static void ChangeAverageTimerCounterValueWithTryCatchExceptionFinally
                                (
                                    this PerformanceCounter performanceCounter
                                    , bool enabled
                                    , PerformanceCounter basePerformanceCounter
                                    , Action onCountPerformanceInnerProcessAction //= null
                                    , Func<PerformanceCounter, Exception, bool> onCaughtExceptionProcessFunc //= null
                                    , Action<PerformanceCounter, PerformanceCounter, bool, Exception> onFinallyProcessAction //= null
                                )
        {
            if (enabled)
            {
                var stopwatch = Stopwatch.StartNew();
                if (onCountPerformanceInnerProcessAction != null)
                {
                    bool reThrowException = false;
                    TryCatchFinallyProcessHelper
                        .TryProcessCatchFinally
                            (
                                true
                                , () =>
                                {
                                    onCountPerformanceInnerProcessAction();
                                }
                                , reThrowException
                                , (x) =>
                                {
                                    var r = reThrowException;
                                    if (onCaughtExceptionProcessFunc != null)
                                    {
                                        r = onCaughtExceptionProcessFunc(performanceCounter, x);
                                    }
                                    return r;
                                }
                                , (x, y) =>
                                {
                                    stopwatch.Stop();
                                    performanceCounter.IncrementBy(stopwatch.ElapsedTicks);
                                    stopwatch = null;
                                    basePerformanceCounter.Increment();
                                    if (onFinallyProcessAction != null)
                                    {
                                        onFinallyProcessAction
                                            (
                                                performanceCounter
                                                , basePerformanceCounter
                                                , x
                                                , y
                                            );
                                    }
                                }
                            );
                }
            }
        }
    }
}
namespace Microshaoft
{
    using System;
    using System.Diagnostics;
    [FlagsAttribute]
    public enum MultiPerformanceCountersTypeFlags : ushort
    {
        None = 0,
        ProcessCounter = 1,
        ProcessingCounter = 2,
        ProcessedCounter = 4,
        ProcessedAverageTimerCounter = 8,
        ProcessedRateOfCountsPerSecondCounter = 16
    };
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class PerformanceCounterDefinitionAttribute : Attribute
    {
        public PerformanceCounterType CounterType;
        public string CounterName;
    }
}
//namespace Microsoft
//{
//    using System.Diagnostics;
//    using System.Linq;
//    public static class PerformanceCountersHelper
//    {
//        public static void AttachPerformanceCountersToProperties<T>
//                                    (
//                                        string performanceCounterInstanceName
//                                        , string category
//                                        , T target //= default(T)
//                                    )
//        {
//            var type = typeof(T);
//            var propertiesList = type.GetProperties().ToList();
//            propertiesList = propertiesList.Where
//                                                (
//                                                    (pi) =>
//                                                    {
//                                                        var parameters = pi.GetIndexParameters();
//                                                        return
//                                                            (
//                                                                pi.PropertyType == typeof(PerformanceCounter)
//                                                                && (parameters == null ? 0 : parameters.Length) <= 0
//                                                            );
//                                                    }
//                                                ).ToList();
//            if (PerformanceCounterCategory.Exists(category))
//            {
//                propertiesList.ForEach
//                                    (
//                                        (pi) =>
//                                        {
//                                            if (PerformanceCounterCategory.CounterExists(pi.Name, category))
//                                            {
//                                                if (PerformanceCounterCategory.InstanceExists(performanceCounterInstanceName, category))
//                                                {
//                                                    //var pc = new PerformanceCounter(category, pi.Name, instanceName, false);
//                                                    //pc.InstanceName = instanceName;
//                                                    //pc.RemoveInstance();
//                                                }
//                                            }
//                                        }
//                                    );
//                //PerformanceCounterCategory.Delete(category);
//            }
//            if (!PerformanceCounterCategory.Exists(category))
//            {
//                var ccdc = new CounterCreationDataCollection();
//                propertiesList.ForEach
//                                (
//                                    (pi) =>
//                                    {
//                                        var propertyName = pi.Name;
//                                        var performanceCounterType = PerformanceCounterType.NumberOfItems64;
//                                        var performanceCounterName = propertyName;
//                                        var attribute = pi.GetCustomAttributes(false).FirstOrDefault
//                                                                                    (
//                                                                                        (x) =>
//                                                                                        {
//                                                                                            return x as PerformanceCounterDefinitionAttribute != null;
//                                                                                        }
//                                                                                    ) as PerformanceCounterDefinitionAttribute;
//                                        if (attribute != null)
//                                        {
//                                            var counterName = attribute.CounterName;
//                                            if (!string.IsNullOrEmpty(counterName))
//                                            {
//                                                performanceCounterName = counterName;
//                                            }
//                                            var counterType = attribute.CounterType;
//                                            //if (counterType != null)
//                                            {
//                                                performanceCounterType = counterType;
//                                            }
//                                        }
//                                        var ccd = PerformanceCountersHelper
//                                                    .GetCounterCreationData
//                                                        (
//                                                            performanceCounterName
//                                                            , performanceCounterType
//                                                        );
//                                        ccdc.Add(ccd);
//                                    }
//                                );
//                PerformanceCounterCategory.Create
//                                (
//                                    category,
//                                    string.Format("{0} Category Help.", category),
//                                    PerformanceCounterCategoryType.MultiInstance,
//                                    ccdc
//                                );
//            }
//            propertiesList.ForEach
//                            (
//                                (pi) =>
//                                {
//                                    var propertyName = pi.Name;
//                                    var performanceCounterType = PerformanceCounterType.NumberOfItems64;
//                                    var performanceCounterName = propertyName;
//                                    var attribute =
//                                                    pi
//                                                        .GetCustomAttributes(false)
//                                                            .FirstOrDefault
//                                                                (
//                                                                    (x) =>
//                                                                    {
//                                                                        return
//                                                                            (
//                                                                                (x as PerformanceCounterDefinitionAttribute)
//                                                                                != null
//                                                                            );
//                                                                    }
//                                                                ) as PerformanceCounterDefinitionAttribute;
//                                    if (attribute != null)
//                                    {
//                                        var counterName = attribute.CounterName;
//                                        if (!string.IsNullOrEmpty(counterName))
//                                        {
//                                            performanceCounterName = counterName;
//                                        }
//                                        var counterType = attribute.CounterType;
//                                        //if (counterType != null)
//                                        {
//                                            performanceCounterType = counterType;
//                                        }
//                                    }
//                                    var pc = new PerformanceCounter()
//                                    {
//                                        CategoryName = category
//                                        ,
//                                        CounterName = performanceCounterName
//                                        ,
//                                        InstanceLifetime = PerformanceCounterInstanceLifetime.Process
//                                        ,
//                                        InstanceName = performanceCounterInstanceName
//                                        ,
//                                        ReadOnly = false
//                                        ,
//                                        RawValue = 0
//                                    };
//                                    if (pi.GetGetMethod().IsStatic)
//                                    {
//                                        var setter =
//                                                    DynamicPropertyAccessor
//                                                        .CreateSetStaticPropertyValueAction<PerformanceCounter>
//                                                            (
//                                                                type
//                                                                , propertyName
//                                                            );
//                                        setter(pc);
//                                    }
//                                    else
//                                    {
//                                        if (target != null)
//                                        {
//                                            var setter =
//                                                        DynamicPropertyAccessor
//                                                            .CreateSetPropertyValueAction<PerformanceCounter>
//                                                                (
//                                                                    type
//                                                                    , propertyName
//                                                                );
//                                            setter(target, pc);
//                                        }
//                                    }
//                                }
//                            );
//        }
//        public static CounterCreationData GetCounterCreationData
//                                            (
//                                                string counterName
//                                                , PerformanceCounterType performanceCounterType
//                                            )
//        {
//            return new CounterCreationData()
//            {
//                CounterName = counterName
//                ,
//                CounterHelp = string.Format("{0} Help", counterName)
//                ,
//                CounterType = performanceCounterType
//            };
//        }
//    }
//}




























//namespace Microsoft
//{
//    using System;
//    using System.Diagnostics;
//    using System.Threading;
//    using System.Linq;
//    public static class PerformanceCountersHelper
//    {
//        public static void TryCountPerformance
//                            (
//                                bool enableCount
//                                , bool reThrowException = false
//                                , PerformanceCounter[] IncrementCountersBeforeCountPerformance = null
//                                , PerformanceCounter[] DecrementCountersBeforeCountPerformance = null
//                                , Tuple
//                                        <
//                                            bool						//before时是否已经启动
//                                            , Stopwatch
//                                            , PerformanceCounter
//                                            , PerformanceCounter		//base计数器
//                                        >[] timerCounters = null
//                                , Action onTryCountPerformanceProcessAction = null
//                                , Func<Exception, bool> onCaughtExceptionCountPerformanceProcessFunc = null
//                                , Action<bool, Exception> onFinallyCountPerformanceProcessAction = null
//                                , PerformanceCounter[] DecrementCountersAfterCountPerformance = null
//                                , PerformanceCounter[] IncrementCountersAfterCountPerformance = null
//                            )
//        {
//            if (onTryCountPerformanceProcessAction != null)
//            {
//                if (enableCount)
//                {
//                    #region before
//                    if (IncrementCountersBeforeCountPerformance != null)
//                    {
//                        Array.ForEach
//                                (
//                                    IncrementCountersBeforeCountPerformance
//                                    , (x) =>
//                                    {
//                                        var l = x.Increment();
//                                    }
//                                );
//                    }
//                    if (DecrementCountersBeforeCountPerformance != null)
//                    {
//                        Array.ForEach
//                                (
//                                    DecrementCountersBeforeCountPerformance
//                                    , (x) =>
//                                    {
//                                        var l = x.Decrement();
//                                        if (l < 0)
//                                        {
//                                            x.RawValue = 0;
//                                        }
//                                    }
//                                );
//                    }
//                    if (timerCounters != null)
//                    {
//                        Array.ForEach
//                        (
//                            timerCounters
//                            , (x) =>
//                            {
//                                if
//                                    (
//                                        x.Item1
//                                        && x.Item2 != null
//                                    )
//                                {
//                                    x.Item2.Start();
//                                }
//                            }
//                        );
//                    }
//                    #endregion
//                }
//                var needTry = true;
//                TryCatchFinallyProcessHelper
//                    .TryProcessCatchFinally
//                        (
//                            needTry
//                            , () =>
//                            {
//                                onTryCountPerformanceProcessAction();
//                            }
//                            , reThrowException
//                            , (x) =>
//                            {
//                                if (onCaughtExceptionCountPerformanceProcessFunc != null)
//                                {
//                                    reThrowException = onCaughtExceptionCountPerformanceProcessFunc(x);
//                                }
//                                return reThrowException;
//                            }
//                            , (x, y) =>
//                            {
//                                if (enableCount)
//                                {
//                                    #region after
//                                    if (timerCounters != null)
//                                    {
//                                        Array.ForEach
//                                        (
//                                            timerCounters
//                                            , (xx) =>
//                                            {
//                                                if (xx.Item2 != null)
//                                                {
//                                                    Stopwatch stopwatch = xx.Item2;
//                                                    stopwatch.Stop();
//                                                    long elapsedTicks = stopwatch.ElapsedTicks;
//                                                    var counter = xx.Item3;
//                                                    counter.IncrementBy(elapsedTicks);
//                                                    stopwatch = null;
//                                                    counter = xx.Item4;  //base
//                                                    counter.Increment();
//                                                }
//                                            }
//                                        );
//                                    }
//                                    if (IncrementCountersAfterCountPerformance != null)
//                                    {
//                                        Array.ForEach
//                                                (
//                                                    IncrementCountersAfterCountPerformance
//                                                    , (xx) =>
//                                                    {
//                                                        var l = xx.Increment();
//                                                    }
//                                                );
//                                    }
//                                    if (DecrementCountersAfterCountPerformance != null)
//                                    {
//                                        Array.ForEach
//                                                (
//                                                    DecrementCountersAfterCountPerformance
//                                                    , (xx) =>
//                                                    {
//                                                        var l = xx.Decrement();
//                                                        if (l < 0)
//                                                        {
//                                                            xx.RawValue = 0;
//                                                        }
//                                                    }
//                                                );
//                                    }
//                                    #endregion
//                                }
//                                if (onFinallyCountPerformanceProcessAction != null)
//                                {
//                                    onFinallyCountPerformanceProcessAction(x, y);
//                                }
//                            }
//                        );
//            }
//        }
//        public static void AttachPerformanceCountersToProperties<T>
//                                    (
//                                        string performanceCounterInstanceName
//                                        , string category
//                                        , T target //= default(T)
//                                    )
//        {
//            var type = typeof(T);
//            var propertiesList = type.GetProperties().ToList();
//            propertiesList = propertiesList
//                                .Where
//                                    (
//                                        (pi) =>
//                                        {
//                                            var parameters = pi.GetIndexParameters();
//                                            return
//                                                (
//                                                    pi.PropertyType == typeof(PerformanceCounter)
//                                                    && (parameters == null ? 0 : parameters.Length) <= 0
//                                                );
//                                        }
//                                    ).ToList();
//            if (PerformanceCounterCategory.Exists(category))
//            {
//                propertiesList
//                    .ForEach
//                        (
//                            (pi) =>
//                            {
//                                if (PerformanceCounterCategory.CounterExists(pi.Name, category))
//                                {
//                                    if (PerformanceCounterCategory.InstanceExists(performanceCounterInstanceName, category))
//                                    {
//                                        //var pc = new PerformanceCounter(category, pi.Name, instanceName, false);
//                                        //pc.InstanceName = instanceName;
//                                        //pc.RemoveInstance();
//                                    }
//                                }
//                            }
//                        );
//                //PerformanceCounterCategory.Delete(category);
//            }
//            if (!PerformanceCounterCategory.Exists(category))
//            {
//                var ccdc = new CounterCreationDataCollection();
//                propertiesList
//                    .ForEach
//                        (
//                            (pi) =>
//                            {
//                                var propertyName = pi.Name;
//                                var performanceCounterType = PerformanceCounterType.NumberOfItems64;
//                                var performanceCounterName = propertyName;
//                                var attribute
//                                        = pi
//                                            .GetCustomAttributes(false)
//                                                .FirstOrDefault
//                                                    (
//                                                        (x) =>
//                                                        {
//                                                            return
//                                                                x as PerformanceCounterDefinitionAttribute
//                                                                != null;
//                                                        }
//                                                    ) as PerformanceCounterDefinitionAttribute;
//                                if (attribute != null)
//                                {
//                                    var counterName = attribute.CounterName;
//                                    if (!string.IsNullOrEmpty(counterName))
//                                    {
//                                        performanceCounterName = counterName;
//                                    }
//                                    var counterType = attribute.CounterType;
//                                    //if (counterType != null)
//                                    {
//                                        performanceCounterType = counterType;
//                                    }
//                                }
//                                var ccd = PerformanceCountersHelper
//                                            .GetCounterCreationData
//                                                (
//                                                    performanceCounterName
//                                                    , performanceCounterType
//                                                );
//                                ccdc.Add(ccd);
//                            }
//                        );
//                PerformanceCounterCategory
//                    .Create
//                        (
//                            category
//                            , string.Format("{0} Category Help.", category)
//                            , PerformanceCounterCategoryType.MultiInstance
//                            , ccdc
//                        );
//            }
//            propertiesList.ForEach
//                            (
//                                (pi) =>
//                                {
//                                    var propertyName = pi.Name;
//                                    var performanceCounterType = PerformanceCounterType.NumberOfItems64;
//                                    var performanceCounterName = propertyName;
//                                    var attribute
//                                            = pi
//                                                .GetCustomAttributes(false)
//                                                    .FirstOrDefault
//                                                        (
//                                                            (x) =>
//                                                            {
//                                                                return
//                                                                    x as PerformanceCounterDefinitionAttribute
//                                                                    != null;
//                                                            }
//                                                        ) as PerformanceCounterDefinitionAttribute;
//                                    if (attribute != null)
//                                    {
//                                        var counterName = attribute.CounterName;
//                                        if (!string.IsNullOrEmpty(counterName))
//                                        {
//                                            performanceCounterName = counterName;
//                                        }
//                                        var counterType = attribute.CounterType;
//                                        //if (counterType != null)
//                                        {
//                                            performanceCounterType = counterType;
//                                        }
//                                    }
//                                    var pc = new PerformanceCounter()
//                                    {
//                                        CategoryName = category
//                                        ,
//                                        CounterName = performanceCounterName
//                                        ,
//                                        InstanceLifetime = PerformanceCounterInstanceLifetime.Process
//                                        ,
//                                        InstanceName = performanceCounterInstanceName
//                                        ,
//                                        ReadOnly = false
//                                        ,
//                                        RawValue = 0
//                                    };
//                                    if (pi.GetGetMethod().IsStatic)
//                                    {
//                                        var setter = DynamicPropertyAccessor
//                                                        .CreateSetStaticPropertyValueAction<PerformanceCounter>
//                                                            (
//                                                                type
//                                                                , propertyName
//                                                            );
//                                        setter(pc);
//                                    }
//                                    else
//                                    {
//                                        if (target != null)
//                                        {
//                                            var setter = DynamicPropertyAccessor
//                                                            .CreateSetPropertyValueAction<PerformanceCounter>
//                                                                (
//                                                                    type
//                                                                    , propertyName
//                                                                );
//                                            setter(target, pc);
//                                        }
//                                    }
//                                }
//                            );
//        }
//        public static CounterCreationData GetCounterCreationData
//                    (
//                        string counterName
//                        , PerformanceCounterType performanceCounterType
//                    )
//        {
//            return
//                new CounterCreationData()
//                {
//                    CounterName = counterName
//                    ,
//                    CounterHelp = string.Format("{0} Help", counterName)
//                    ,
//                    CounterType = performanceCounterType
//                };
//        }
//    }
//}

//namespace Microsoft
//{
//    using System;
//    using System.Diagnostics;
//    public static class PerformanceCounterExtensionMethodsManager
//    {
//        public static T ChangeCounterValueWithTryCatchExceptionFinally<T>
//                                (
//                                    this PerformanceCounter performanceCounter
//                                    , bool enabled
//                                    , Func<PerformanceCounter, T> onCounterChangeProcessFunc = null
//                                    , Action<PerformanceCounter> onCounterChangedProcessAction = null
//                                    , Func<PerformanceCounter, Exception, bool> onCaughtExceptionProcessFunc = null
//                                    , Action<PerformanceCounter> onCaughtExceptionFinallyProcessAction = null
//                                )
//        {
//            T r = default(T);
//            if (enabled)
//            {
//                if (onCounterChangeProcessFunc != null)
//                {
//                    var caughtException = false;
//                    try
//                    {
//                        r = onCounterChangeProcessFunc(performanceCounter);
//                    }
//                    catch (Exception e)
//                    {
//                        caughtException = true;
//                        var reThrow = true;
//                        if (onCaughtExceptionProcessFunc != null)
//                        {
//                            reThrow = onCaughtExceptionProcessFunc(performanceCounter, e);
//                        }
//                        if (reThrow)
//                        {
//                            throw new Exception("ReThrow Exception On Caught Excepion", e);
//                        }
//                    }
//                    finally
//                    {
//                        if (caughtException)
//                        {
//                            if (onCaughtExceptionFinallyProcessAction != null)
//                            {
//                                onCaughtExceptionFinallyProcessAction(performanceCounter);
//                            }
//                        }
//                    }
//                }
//            }
//            if (onCounterChangedProcessAction != null)
//            {
//                var caughtException = false;
//                try
//                {
//                    onCounterChangedProcessAction(performanceCounter);
//                }
//                catch (Exception e)
//                {
//                    caughtException = true;
//                    var reThrow = true;
//                    if (onCaughtExceptionProcessFunc != null)
//                    {
//                        reThrow = onCaughtExceptionProcessFunc(performanceCounter, e);
//                    }
//                    if (reThrow)
//                    {
//                        throw new Exception("ReThrow Exception On Caught Excepion", e);
//                    }
//                }
//                finally
//                {
//                    if (caughtException)
//                    {
//                        if (onCaughtExceptionFinallyProcessAction != null)
//                        {
//                            onCaughtExceptionFinallyProcessAction(performanceCounter);
//                        }
//                    }
//                }
//            }
//            return r;
//        }
//        public static void ChangeAverageTimerCounterValueWithTryCatchExceptionFinally
//                                (
//                                    this PerformanceCounter performanceCounter
//                                    , bool enabled
//                                    , PerformanceCounter basePerformanceCounter
//                                    , Action onCountPerformanceInnerProcessAction = null
//                                    , Func<PerformanceCounter, Exception, bool> onCaughtExceptionProcessFunc = null
//                                    , Action<PerformanceCounter, PerformanceCounter> onCaughtExceptionFinallyProcessAction = null
//                                )
//        {
//            if (enabled)
//            {
//                var stopwatch = Stopwatch.StartNew();
//                if (onCountPerformanceInnerProcessAction != null)
//                {
//                    var caughtException = false;
//                    try
//                    {
//                        onCountPerformanceInnerProcessAction();
//                    }
//                    catch (Exception e)
//                    {
//                        caughtException = true;
//                        var reThrow = true;
//                        if (onCaughtExceptionProcessFunc != null)
//                        {
//                            reThrow = onCaughtExceptionProcessFunc(performanceCounter, e);
//                        }
//                        if (reThrow)
//                        {
//                            throw new Exception("ReThrow Exception On Caught Excepion", e);
//                        }
//                    }
//                    finally
//                    {
//                        stopwatch.Stop();
//                        performanceCounter.IncrementBy(stopwatch.ElapsedTicks);
//                        stopwatch = null;
//                        basePerformanceCounter.Increment();
//                        if (caughtException)
//                        {
//                            if (onCaughtExceptionFinallyProcessAction != null)
//                            {
//                                onCaughtExceptionFinallyProcessAction
//                                    (
//                                        performanceCounter
//                                        , basePerformanceCounter
//                                    );
//                            }
//                        }
//                    }
//                }
//            }
//        }
//    }
//}



//namespace Microsoft
//{
//    using System;
//    using System.Diagnostics;
//    [FlagsAttribute]
//    public enum MultiPerformanceCountersTypeFlags : ushort
//    {
//        None = 0,
//        ProcessCounter = 1,
//        ProcessingCounter = 2,
//        ProcessedCounter = 4,
//        ProcessedAverageTimerCounter = 8,
//        ProcessedRateOfCountsPerSecondCounter = 16
//    };
//    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
//    public class PerformanceCounterDefinitionAttribute : Attribute
//    {
//        public PerformanceCounterType CounterType;
//        public string CounterName;
//    }
//}


