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
                                    , Stopwatch stopwatch
                                    , Action onCountPerformanceInnerProcessAction = null
                                    , Func<PerformanceCounter, Exception, bool> onCaughtExceptionProcessFunc = null
                                    , Action<PerformanceCounter, PerformanceCounter, bool, Exception> onFinallyProcessAction = null
                                )
        {
            //Stopwatch stopwatch = null;
            if (enabled)
            {
                stopwatch.Reset();
                stopwatch.Start();
            }
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
                                if (enabled && stopwatch != null && stopwatch.IsRunning)
                                {
                                    stopwatch.Stop();
                                    performanceCounter.IncrementBy(stopwatch.ElapsedTicks);
                                    //stopwatch = null;
                                    basePerformanceCounter.Increment();
                                }

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



