namespace Microshaoft
{
    using System;
    using System.Diagnostics;
    /// <summary>
    /// 关于Session数的性能计数器
    /// </summary>
    public class SessionsPerformanceCountersContainer : IPerformanceCountersContainer
    {
        #region PerformanceCounters
        private PerformanceCounter _caughtExceptionsPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.NumberOfItems64
                    , CounterName = "99.捕获异常次数(次)"
                )
        ]
        public PerformanceCounter CaughtExceptionsPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (
                            ref _caughtExceptionsPerformanceCounter
                            , value
                            , 2
                        );
            }
            get
            {
                return _caughtExceptionsPerformanceCounter;
            }
        }
        private PerformanceCounter _processPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.NumberOfItems64
                    , CounterName = "01.新建次数(次)"
                )
        ]
        public PerformanceCounter PrcocessPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper.TryEnterWriterLockSlimWrite<PerformanceCounter>(ref _processPerformanceCounter, value, 2);
            }
            get
            {
                return _processPerformanceCounter;
            }
        }
        private PerformanceCounter _processingPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.NumberOfItems64
                    , CounterName = "02.当前活动数(个)"
                )
        ]
        public PerformanceCounter ProcessingPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper.TryEnterWriterLockSlimWrite<PerformanceCounter>(ref _processingPerformanceCounter, value, 2);
            }
            get
            {
                return _processingPerformanceCounter;
            }
        }
        private PerformanceCounter _processedPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.NumberOfItems64
                    , CounterName = "03.删除次数(次)"
                )
        ]
        public PerformanceCounter ProcessedPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper.TryEnterWriterLockSlimWrite<PerformanceCounter>(ref _processedPerformanceCounter, value, 2);
            }
            get
            {
                return _processedPerformanceCounter;
            }
        }
        private PerformanceCounter _processedRateOfCountsPerSecondPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond64
                    , CounterName = "04.每秒完成处理笔数(笔/秒)[不适用]"
                )
        ]
        public PerformanceCounter ProcessedRateOfCountsPerSecondPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper.TryEnterWriterLockSlimWrite<PerformanceCounter>(ref _processedRateOfCountsPerSecondPerformanceCounter, value, 2);
            }
            get
            {
                return _processedRateOfCountsPerSecondPerformanceCounter;
            }
        }
        private PerformanceCounter _ProcessedAverageTimerPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.AverageTimer32
                    , CounterName = "05.平均每笔处理耗时秒数(秒/笔)[不适用]"
                )
        ]
        public PerformanceCounter ProcessedAverageTimerPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper.TryEnterWriterLockSlimWrite<PerformanceCounter>(ref _ProcessedAverageTimerPerformanceCounter, value, 2);
            }
            get
            {
                return _ProcessedAverageTimerPerformanceCounter;
            }
        }
        private PerformanceCounter _processedAverageBasePerformanceCounter;
        [PerformanceCounterDefinitionAttribute(CounterType = PerformanceCounterType.AverageBase)]
        public PerformanceCounter ProcessedAverageBasePerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper.TryEnterWriterLockSlimWrite<PerformanceCounter>(ref _processedAverageBasePerformanceCounter, value, 2);
            }
            get
            {
                return _processedAverageBasePerformanceCounter;
            }
        }
        #endregion
        // indexer declaration
        public PerformanceCounter this[string name]
        {
            get
            {
                throw new NotImplementedException();
                //return null;
            }
        }
        //private bool _isAttachedPerformanceCounters = false;
        public void AttachPerformanceCountersToProperties
                            (
                                string instanceName
                                , string categoryName
                            )
        {
            var type = this.GetType();
            PerformanceCountersHelper.AttachPerformanceCountersToProperties<SessionsPerformanceCountersContainer>(instanceName, categoryName, this);
        }
    }
}

