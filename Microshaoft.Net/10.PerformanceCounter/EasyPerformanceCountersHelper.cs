namespace Microshaoft
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    //using System.Collections.Concurrent;
    public static class EasyPerformanceCountersHelper<TPerformanceCountersContainer>
                                        where TPerformanceCountersContainer : class
                                                , IPerformanceCountersContainer, new()
    {
        private static QueuedObjectsPool<Stopwatch> _stopwatchsPool = new QueuedObjectsPool<Stopwatch>(0);
        private static Dictionary<string, TPerformanceCountersContainer> _dictionary
                        = new Dictionary<string, TPerformanceCountersContainer>();
        public static void AttachPerformanceCountersCategoryInstance
                            (
                                string performanceCountersCategoryName
                                , string performanceCountersCategoryInstanceName
                            )
        {
            string key = string.Format
                                    (
                                        "{1}{0}{2}"
                                        , "-"
                                        , performanceCountersCategoryName
                                        , performanceCountersCategoryInstanceName
                                    );
            TPerformanceCountersContainer container = null;
            if (!_dictionary.TryGetValue(key, out container))
            {
                container = new TPerformanceCountersContainer(); //default(TPerformanceCountersContainer);
                _dictionary.Add
                            (
                                key
                                , container
                            );
                container
                    .AttachPerformanceCountersToProperties
                        (
                            performanceCountersCategoryInstanceName
                            , performanceCountersCategoryName
                        );
            }
        }
        private static object _lockerObject = new object();
        public static Stopwatch CountPerformanceBegin
                                    (
                                        MultiPerformanceCountersTypeFlags enabledPerformanceCounters
                                        , string performanceCountersCategoryName
                                        , string performanceCountersCategoryInstanceName
                                    )
        {
            Stopwatch r = null;
            if (enabledPerformanceCounters != MultiPerformanceCountersTypeFlags.None)
            {
                string key = string.Format
                                        (
                                            "{1}{0}{2}"
                                            , "-"
                                            , performanceCountersCategoryName
                                            , performanceCountersCategoryInstanceName
                                        );
                TPerformanceCountersContainer container = null;
                if (!_dictionary.TryGetValue(key, out container))
                {
                    lock (_lockerObject)
                    {
                        container = default(TPerformanceCountersContainer);
                        _dictionary.Add
                                    (
                                        key
                                        , container
                                    );
                        container.AttachPerformanceCountersToProperties
                                            (
                                                performanceCountersCategoryInstanceName
                                                , performanceCountersCategoryName
                                            );
                    }
                }
                var enableProcessCounter =
                                            (
                                                (
                                                    enabledPerformanceCounters
                                                    & MultiPerformanceCountersTypeFlags.ProcessCounter
                                                )
                                                != MultiPerformanceCountersTypeFlags.None
                                            );
                if (enableProcessCounter)
                {
                    container.PrcocessPerformanceCounter.Increment();
                }
                var enableProcessingCounter =
                                            (
                                                (
                                                    enabledPerformanceCounters
                                                    & MultiPerformanceCountersTypeFlags.ProcessingCounter
                                                )
                                                != MultiPerformanceCountersTypeFlags.None
                                            );
                if (enableProcessingCounter)
                {
                    container.ProcessingPerformanceCounter.Increment();
                }
                var enableProcessedAverageTimerCounter =
                                            (
                                                (
                                                    enabledPerformanceCounters
                                                    & MultiPerformanceCountersTypeFlags.ProcessedAverageTimerCounter
                                                )
                                                != MultiPerformanceCountersTypeFlags.None
                                            );
                if (enableProcessedAverageTimerCounter)
                {
                    r = _stopwatchsPool.Get(); //Stopwatch.StartNew();
                    r.Reset();
                    r.Start();
                }
            }
            return r;
        }
        public static void CountPerformanceEnd
                                    (
                                        MultiPerformanceCountersTypeFlags enabledPerformanceCounters
                                        , string performanceCountersCategoryName
                                        , string performanceCountersCategoryInstanceName
                                        , Stopwatch stopwatch
                                    )
        {
            string key = string.Format
                        (
                            "{1}{0}{2}"
                            , "-"
                            , performanceCountersCategoryName
                            , performanceCountersCategoryInstanceName
                        );
            TPerformanceCountersContainer container = null;
            if (!_dictionary.TryGetValue(key, out container))
            {
                return;
            }
            var enableProcessedAverageTimerCounter =
                                        (
                                            (
                                                enabledPerformanceCounters
                                                & MultiPerformanceCountersTypeFlags.ProcessedAverageTimerCounter
                                            )
                                            != MultiPerformanceCountersTypeFlags.None
                                        );
            if (enableProcessedAverageTimerCounter)
            {
                if (stopwatch != null)
                {
                    PerformanceCounter performanceCounter = container.ProcessedAverageTimerPerformanceCounter;
                    PerformanceCounter basePerformanceCounter = container.ProcessedAverageBasePerformanceCounter;

                    performanceCounter.IncrementBy(stopwatch.ElapsedTicks);
                    if (stopwatch.IsRunning)
                    {
                        stopwatch.Stop();
                        _stopwatchsPool.Put(stopwatch);
                    }
                    basePerformanceCounter.Increment();
                    //stopwatch = null;
                }
            }
            var enableProcessingCounter =
                                        (
                                            (
                                                enabledPerformanceCounters
                                                & MultiPerformanceCountersTypeFlags.ProcessingCounter
                                            )
                                            != MultiPerformanceCountersTypeFlags.None
                                        );
            if (enableProcessingCounter)
            {
                container.ProcessingPerformanceCounter.Decrement();
            }
            var enableProcessedPerformanceCounter =
                                    (
                                        (
                                            enabledPerformanceCounters
                                            & MultiPerformanceCountersTypeFlags.ProcessedCounter
                                        )
                                        != MultiPerformanceCountersTypeFlags.None
                                    );
            if (enableProcessedPerformanceCounter)
            {
                container.ProcessedPerformanceCounter.Increment();
            }
            var enableProcessedRateOfCountsPerSecondPerformanceCounter =
                        (
                            (
                                enabledPerformanceCounters
                                & MultiPerformanceCountersTypeFlags.ProcessedRateOfCountsPerSecondCounter
                            )
                            != MultiPerformanceCountersTypeFlags.None
                        );
            if (enableProcessedRateOfCountsPerSecondPerformanceCounter)
            {
                container.ProcessedRateOfCountsPerSecondPerformanceCounter.Increment();
            }
        }
        public static void CountPerformance
                                    (
                                        MultiPerformanceCountersTypeFlags enabledPerformanceCounters
                                        , string performanceCountersCategoryName
                                        , string performanceCountersCategoryInstanceName
                                        , Action onBeforeCountPerformanceInnerProcessAction = null
                                        , Action onCountPerformanceInnerProcessAction = null
                                        , Action onAfterCountPerformanceInnerProcessAction = null
                                        , Func<Exception, bool> onCaughtExceptionProcessFunc = null
                                        , Action<bool, Exception> onFinallyProcessAction = null
                                    )
        {
            if (enabledPerformanceCounters != MultiPerformanceCountersTypeFlags.None)
            {
                if (onCountPerformanceInnerProcessAction != null)
                {
                    string key = string.Format
                                            (
                                                "{1}{0}{2}"
                                                , "-"
                                                , performanceCountersCategoryName
                                                , performanceCountersCategoryInstanceName
                                            );
                    TPerformanceCountersContainer container = null;
                    if (!_dictionary.TryGetValue(key, out container))
                    {
                        lock (_lockerObject)
                        {
                            container = default(TPerformanceCountersContainer);
                            _dictionary.Add
                                        (
                                            key
                                            , container
                                        );
                            container.AttachPerformanceCountersToProperties
                                                (
                                                    performanceCountersCategoryInstanceName
                                                    , performanceCountersCategoryName
                                                );
                        }
                    }
                    var enableProcessCounter =
                                                (
                                                    (
                                                        enabledPerformanceCounters
                                                        & MultiPerformanceCountersTypeFlags.ProcessCounter
                                                    )
                                                    != MultiPerformanceCountersTypeFlags.None
                                                );
                    if (enableProcessCounter)
                    {
                        container.PrcocessPerformanceCounter.Increment();
                    }
                    var enableProcessingCounter =
                                                (
                                                    (
                                                        enabledPerformanceCounters
                                                        & MultiPerformanceCountersTypeFlags.ProcessingCounter
                                                    )
                                                    != MultiPerformanceCountersTypeFlags.None
                                                );
                    if (enableProcessingCounter)
                    {
                        container.ProcessingPerformanceCounter.Increment();
                    }
                    var enableProcessedAverageTimerCounter =
                                                (
                                                    (
                                                        enabledPerformanceCounters
                                                        & MultiPerformanceCountersTypeFlags.ProcessedAverageTimerCounter
                                                    )
                                                    != MultiPerformanceCountersTypeFlags.None
                                                );
                    var reThrowException = false;
                    var stopwatch = _stopwatchsPool.Get();
                    stopwatch.Reset();
                    container
                        .ProcessedAverageTimerPerformanceCounter
                            .ChangeAverageTimerCounterValueWithTryCatchExceptionFinally
                                        (
                                            enableProcessedAverageTimerCounter
                                            , container.ProcessedAverageBasePerformanceCounter
                                            , stopwatch
                                            , () =>
                                            {
                                                if (onCountPerformanceInnerProcessAction != null)
                                                {
                                                    if (onBeforeCountPerformanceInnerProcessAction != null)
                                                    {
                                                        onBeforeCountPerformanceInnerProcessAction();
                                                    }
                                                    onCountPerformanceInnerProcessAction();
                                                    if (onAfterCountPerformanceInnerProcessAction != null)
                                                    {
                                                        onAfterCountPerformanceInnerProcessAction();
                                                    }
                                                }
                                            }
                                            , (x, y) =>        //catch
                                            {
                                                container
                                                    .CaughtExceptionsPerformanceCounter
                                                        .Increment();
                                                var r = reThrowException;
                                                if (onCaughtExceptionProcessFunc != null)
                                                {
                                                    r = onCaughtExceptionProcessFunc(y);
                                                }
                                                return r;
                                            }
                                            , (x, y, z, w) =>        //Finally
                                            {
                                                if (enableProcessingCounter)
                                                {
                                                    container.ProcessingPerformanceCounter.Decrement();
                                                }
                                                var enableProcessedPerformanceCounter =
                                                                        (
                                                                            (
                                                                                enabledPerformanceCounters
                                                                                & MultiPerformanceCountersTypeFlags.ProcessedCounter
                                                                            )
                                                                            != MultiPerformanceCountersTypeFlags.None
                                                                        );
                                                if (enableProcessedPerformanceCounter)
                                                {
                                                    container.ProcessedPerformanceCounter.Increment();
                                                }
                                                var enableProcessedRateOfCountsPerSecondPerformanceCounter =
                                                                        (
                                                                            (
                                                                                enabledPerformanceCounters
                                                                                & MultiPerformanceCountersTypeFlags.ProcessedRateOfCountsPerSecondCounter
                                                                            )
                                                                            != MultiPerformanceCountersTypeFlags.None
                                                                        );
                                                if (enableProcessedRateOfCountsPerSecondPerformanceCounter)
                                                {
                                                    container
                                                        .ProcessedRateOfCountsPerSecondPerformanceCounter
                                                            .Increment();
                                                }
                                            }
                                        );
                    stopwatch.Reset();
                    _stopwatchsPool.Put(stopwatch);
                }

            }
            else
            {
                if (onCountPerformanceInnerProcessAction != null)
                {
                    onCountPerformanceInnerProcessAction();
                }
            }
        }
    }
}
