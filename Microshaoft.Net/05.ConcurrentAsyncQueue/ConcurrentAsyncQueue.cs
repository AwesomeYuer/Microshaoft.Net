namespace Microshaoft
{
    using System;

    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;
    public class ConcurrentAsyncQueue<T>
    {
        public delegate void QueueEventHandler(T item);
        public event QueueEventHandler OnDequeue;
        public delegate void QueueLogEventHandler(string logMessage);
        public QueueLogEventHandler
                                OnQueueLog
                                , OnDequeueThreadStart
                                , OnDequeueThreadEnd;
        public delegate bool ExceptionEventHandler(ConcurrentAsyncQueue<T> sender, Exception exception);
        public event ExceptionEventHandler OnCaughtException;
        private ConcurrentQueue<Tuple<Stopwatch, T>> _queue =
                                    new ConcurrentQueue<Tuple<Stopwatch, T>>();
        private ConcurrentQueue<Action> _callbackProcessBreaksActions;
        private long _concurrentDequeueThreadsCount = 0; //Microshaoft 用于控制并发线程数
        private ConcurrentQueue<ThreadProcessor> _dequeueThreadsProcessorsPool;
        private int _dequeueIdleSleepSeconds = 10;
        public QueuePerformanceCountersContainer PerformanceCounters
        {
            get;
            private set;
        }
        public int DequeueIdleSleepSeconds
        {
            set
            {
                _dequeueIdleSleepSeconds = value;
            }
            get
            {
                return _dequeueIdleSleepSeconds;
            }
        }
        private bool _isAttachedPerformanceCounters = false;
        private class ThreadProcessor
        {
            public bool Break
            {
                set;
                get;
            }
            public EventWaitHandle Wait
            {
                private set;
                get;
            }
            public ConcurrentAsyncQueue<T> Sender
            {
                private set;
                get;
            }
            public void StopOne()
            {
                Break = true;
            }
            public ThreadProcessor
                            (
                                ConcurrentAsyncQueue<T> queue
                                , EventWaitHandle wait
                            )
            {
                Wait = wait;
                Sender = queue;
            }
            public void ThreadProcess()
            {
                long l = 0;
                Interlocked.Increment(ref Sender._concurrentDequeueThreadsCount);
                bool counterEnabled = Sender._isAttachedPerformanceCounters;
                QueuePerformanceCountersContainer qpcc = Sender.PerformanceCounters;
                var queue = Sender._queue;
                var reThrowException = false;
                PerformanceCountersHelper
                    .TryCountPerformance
                        (
                            counterEnabled
                            , reThrowException
                            , //IncrementCountersBeforeCountPerformance:
                                new PerformanceCounter[]
									{
										qpcc
											.DequeueThreadStartPerformanceCounter
										, qpcc
											.DequeueThreadsCountPerformanceCounter
									}
                            , //DecrementCountersBeforeCountPerformance:
                                null
                            , null
                            , () =>
                            {
#region Try Process
                                if (Sender.OnDequeueThreadStart != null)
                                {
                                    l = Interlocked.Read(ref Sender._concurrentDequeueThreadsCount);
                                    Sender
                                        .OnDequeueThreadStart
                                            (
                                                string
                                                    .Format
                                                        (
                                                            "{0} Threads Count {1},Queue Count {2},Current Thread: {3} at {4}"
                                                            , "Threads ++ !"
                                                            , l
                                                            , queue.Count
                                                            , Thread.CurrentThread.Name
                                                            , DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffff")
                                                        )
                                            );
                                }
                                while (true)
                                {
#region while true loop
                                    if (Break)
                                    {
                                        break;
                                    }
                                    while (!queue.IsEmpty)
                                    {
#region while queue.IsEmpty loop
                                        if (Break)
                                        {
                                            break;
                                        }
                                        Tuple<Stopwatch, T> item = null;
                                        if (queue.TryDequeue(out item))
                                        {
                                            PerformanceCountersHelper
                                                .TryCountPerformance
                                                    (
                                                        counterEnabled
                                                        , reThrowException
                                                        , new PerformanceCounter[]
																{
																	qpcc
																		.DequeuePerformanceCounter
																}
                                                        , new PerformanceCounter[]
																{
																	qpcc
																		.QueueLengthPerformanceCounter
																}
                                                        , new Tuple
                                                                <
                                                                    bool
                                                                    , Stopwatch
                                                                    , PerformanceCounter
                                                                    , PerformanceCounter
                                                                >[]
																{

                                                                    Tuple.Create
																			<
																				bool					//before 时是否需要启动		
																				, Stopwatch
																				, PerformanceCounter
																				, PerformanceCounter	//base
																			>

																		(
																			false
																			, item.Item1
																			, qpcc
																				.QueuedWaitAverageTimerPerformanceCounter
																			, qpcc
																				.QueuedWaitAverageBasePerformanceCounter
																		)
																	, Tuple.Create
																			<
																				bool
																				, Stopwatch
																				, PerformanceCounter
																				, PerformanceCounter
																			>
																		(
																			true
																			, new Stopwatch()
																			, qpcc
																				.DequeueProcessedAverageTimerPerformanceCounter
																			, qpcc
																				.DequeueProcessedAverageBasePerformanceCounter
																		)
																}
                                                        , () =>			//try
                                                        {
                                                            if (Sender.OnDequeue != null)
                                                            {
                                                                var element = item.Item2;
                                                                item = null;
                                                                Sender.OnDequeue(element);
                                                            }
                                                        }
                                                        , (x) =>		//catch
                                                        {
                                                            reThrowException = false;
                                                            return reThrowException;
                                                        }
                                                        , null			//finally
                                                        , null
                                                        , new PerformanceCounter[]
																{
																	qpcc
																		.DequeueProcessedPerformanceCounter
																	, qpcc
																		.DequeueProcessedRateOfCountsPerSecondPerformanceCounter
																}
                                                    );
                                        }
                                        #endregion while queue.IsEmpty loop
                                    }
#region wait
                                    Sender._dequeueThreadsProcessorsPool.Enqueue(this);
                                    if (Break)
                                    {
                                    }
                                    if (!Wait.WaitOne(Sender.DequeueIdleSleepSeconds * 1000))
                                    {
                                    }
                                    #endregion wait
                                    #endregion while true loop
                                }
                                #endregion
                            }
                            , (x) =>			//catch
                            {
#region Catch Process
                                if (Sender.OnCaughtException != null)
                                {
                                    reThrowException = Sender.OnCaughtException(Sender, x);
                                }
                                return reThrowException;
                                #endregion
                            }
                            , (x, y) =>		//finally
                            {
#region Finally Process
                                l = Interlocked.Decrement(ref Sender._concurrentDequeueThreadsCount);
                                if (l < 0)
                                {
                                    Interlocked.Exchange(ref Sender._concurrentDequeueThreadsCount, 0);
                                }
                                if (Sender.OnDequeueThreadEnd != null)
                                {
                                    Sender
                                        .OnDequeueThreadEnd
                                            (
                                                string.Format
                                                        (
                                                            "{0} Threads Count {1},Queue Count {2},Current Thread: {3} at {4}"
                                                            , "Threads--"
                                                            , l
                                                            , Sender._queue.Count
                                                            , Thread.CurrentThread.Name
                                                            , DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffff")
                                                        )
                                            );
                                }
                                if (!Break)
                                {
                                    Sender.StartIncreaseDequeueProcessThreads(1);
                                }
                                Break = false;
                                #endregion
                            }
                            , //DecrementCountersAfterCountPerformance:
                                new PerformanceCounter[]
									{
										qpcc.DequeueThreadsCountPerformanceCounter
									}
                            , //IncrementCountersAfterCountPerformance:
                                new PerformanceCounter[]
									{
										qpcc.DequeueThreadEndPerformanceCounter	
									}
                        );
            }
        }
        public void AttachPerformanceCounters
                            (
                                string instanceNamePrefix
                                , string categoryName
                                , QueuePerformanceCountersContainer performanceCounters
                            )
        {
            var process = Process.GetCurrentProcess();
            var processName = process.ProcessName;
            var instanceName = string.Format
                                    (
                                        "{0}-{1}"
                                        , instanceNamePrefix
                                        , processName
                                    );
            PerformanceCounters = performanceCounters;
            PerformanceCounters
                .AttachPerformanceCountersToProperties(instanceName, categoryName);
            _isAttachedPerformanceCounters = true;
        }
        public int Count
        {
            get
            {
                return _queue.Count;
            }
        }
        public long ConcurrentThreadsCount
        {
            get
            {
                return _concurrentDequeueThreadsCount;
            }
        }
        private void DecreaseDequeueProcessThreads(int count)
        {
            Action action;
            for (var i = 0; i < count; i++)
            {
                if (_callbackProcessBreaksActions.TryDequeue(out action))
                {
                    action();
                    action = null;
                }
            }
        }
        public void StartDecreaseDequeueProcessThreads(int count)
        {
            new Thread
                    (
                        new ThreadStart
                                (
                                    () =>
                                    {
                                        DecreaseDequeueProcessThreads(count);
                                    }
                                )
                    ).Start();
        }
        public void StartIncreaseDequeueProcessThreads(int count)
        {
            new Thread
                    (
                        new ThreadStart
                                (
                                    () =>
                                    {
                                        IncreaseDequeueProcessThreads(count);
                                    }
                                )
                    ).Start();
        }
        private void IncreaseDequeueProcessThreads(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Interlocked.Increment(ref _concurrentDequeueThreadsCount);
                if (_dequeueThreadsProcessorsPool == null)
                {
                    _dequeueThreadsProcessorsPool = new ConcurrentQueue<ThreadProcessor>();
                }
                var processor = new ThreadProcessor
                                                (
                                                    this
                                                    , new AutoResetEvent(false)
                                                );
                var thread = new Thread
                                    (
                                        new ThreadStart
                                                    (
                                                        processor.ThreadProcess
                                                    )
                                    );
                if (_callbackProcessBreaksActions == null)
                {
                    _callbackProcessBreaksActions = new ConcurrentQueue<Action>();
                }
                var callbackProcessBreakAction = new Action
                                                        (
                                                            processor.StopOne
                                                        );
                _callbackProcessBreaksActions.Enqueue(callbackProcessBreakAction);
                _dequeueThreadsProcessorsPool.Enqueue(processor);
                thread.Start();
            }
        }
        public bool Enqueue(T item)
        {
            var r = false;
            var reThrowException = false;
            var enableCount = _isAttachedPerformanceCounters;
            PerformanceCountersHelper
                .TryCountPerformance
                    (
                        enableCount
                        , reThrowException
                        , new PerformanceCounter[]
							{
								PerformanceCounters
									.EnqueuePerformanceCounter
								, PerformanceCounters
									.EnqueueRateOfCountsPerSecondPerformanceCounter
								, PerformanceCounters
									.QueueLengthPerformanceCounter
							}
                        , null
                        , null
                        , () =>
                        {
                            Stopwatch stopwatch = null;
                            if (_isAttachedPerformanceCounters)
                            {
                                stopwatch = Stopwatch.StartNew();
                            }
                            var element = Tuple.Create<Stopwatch, T>(stopwatch, item);
                            _queue.Enqueue(element);
                            r = true;
                        }
                        , (x) =>
                        {
                            if (OnCaughtException != null)
                            {
                                reThrowException = OnCaughtException(this, x);
                            }
                            return reThrowException;
                        }
                        , (x, y) =>
                        {
                            if
                                (
                                    _dequeueThreadsProcessorsPool != null
                                    && !_dequeueThreadsProcessorsPool.IsEmpty
                                )
                            {
                                ThreadProcessor processor;
                                if (_dequeueThreadsProcessorsPool.TryDequeue(out processor))
                                {
                                    processor.Wait.Set();
                                    processor = null;
                                    //Console.WriteLine("processor = null;");
                                }
                            }
                        }
                    );
            return r;
        }
    }
}
namespace Microshaoft
{
    using System;
    using System.Diagnostics;
    public class QueuePerformanceCountersContainer //: IPerformanceCountersContainer
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
        private PerformanceCounter _enqueuePerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.NumberOfItems64
                    , CounterName = "01.入队列累计总数(笔)"
                )
        ]
        public PerformanceCounter EnqueuePerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _enqueuePerformanceCounter, value, 2);
            }
            get
            {
                return _enqueuePerformanceCounter;
            }
        }
        private PerformanceCounter _enqueueRateOfCountsPerSecondPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond64
                    , CounterName = "02.每秒入队列笔数(笔/秒)"
                )
        ]
        public PerformanceCounter EnqueueRateOfCountsPerSecondPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _enqueueRateOfCountsPerSecondPerformanceCounter, value, 2);
            }
            get
            {
                return _enqueueRateOfCountsPerSecondPerformanceCounter;
            }
        }
        private PerformanceCounter _queueLengthPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.NumberOfItems64
                    , CounterName = "03.队列当前长度(笔)"
                )
        ]
        public PerformanceCounter QueueLengthPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _queueLengthPerformanceCounter, value, 2);
            }
            get
            {
                return _queueLengthPerformanceCounter;
            }
        }
        private PerformanceCounter _dequeuePerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.NumberOfItems64
                    , CounterName = "04.出队列累计总数(笔)"
                )
        ]
        public PerformanceCounter DequeuePerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _dequeuePerformanceCounter, value, 2);
            }
            get
            {
                return _dequeuePerformanceCounter;
            }
        }
        private PerformanceCounter _dequeueProcessedRateOfCountsPerSecondPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond64
                    , CounterName = "05.每秒出队列并完成处理笔数(笔/秒)"
                )
        ]
        public PerformanceCounter DequeueProcessedRateOfCountsPerSecondPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _dequeueProcessedRateOfCountsPerSecondPerformanceCounter, value, 2);
            }
            get
            {
                return _dequeueProcessedRateOfCountsPerSecondPerformanceCounter;
            }
        }
        private PerformanceCounter _dequeueProcessedPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.NumberOfItems64
                    , CounterName = "06.已出队列并完成处理累计总笔数(笔)"
                )
        ]
        public PerformanceCounter DequeueProcessedPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _dequeueProcessedPerformanceCounter, value, 2);
            }
            get
            {
                return _dequeueProcessedPerformanceCounter;
            }
        }
        private PerformanceCounter _dequeueProcessedAverageTimerPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.AverageTimer32
                    , CounterName = "07.每笔已出队列并完成处理平均耗时秒数(秒/笔)"
                )
        ]
        public PerformanceCounter DequeueProcessedAverageTimerPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _dequeueProcessedAverageTimerPerformanceCounter, value, 2);
            }
            get
            {
                return _dequeueProcessedAverageTimerPerformanceCounter;
            }
        }
        private PerformanceCounter _dequeueProcessedAverageBasePerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.AverageBase
                )
        ]
        public PerformanceCounter DequeueProcessedAverageBasePerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _dequeueProcessedAverageBasePerformanceCounter, value, 2);
            }
            get
            {
                return _dequeueProcessedAverageBasePerformanceCounter;
            }
        }
        private PerformanceCounter _queuedWaitAverageTimerPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.AverageTimer32
                    , CounterName = "08.每笔入出队列并完成处理平均耗时秒数(秒/笔)"
                )
        ]
        public PerformanceCounter QueuedWaitAverageTimerPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _queuedWaitAverageTimerPerformanceCounter, value, 2);
            }
            get
            {
                return _queuedWaitAverageTimerPerformanceCounter;
            }
        }
        private PerformanceCounter _queuedWaitAverageBasePerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.AverageBase
                )
        ]
        public PerformanceCounter QueuedWaitAverageBasePerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _queuedWaitAverageBasePerformanceCounter, value, 2);
            }
            get
            {
                return _queuedWaitAverageBasePerformanceCounter;
            }
        }
        private PerformanceCounter _dequeueThreadStartPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.NumberOfItems64
                    , CounterName = "09.新建出队列处理线程启动次数(次)"
                )
        ]
        public PerformanceCounter DequeueThreadStartPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _dequeueThreadStartPerformanceCounter, value, 2);
            }
            get
            {
                return _dequeueThreadStartPerformanceCounter;
            }
        }
        private PerformanceCounter _dequeueThreadsCountPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.NumberOfItems64
                    , CounterName = "10.当前出队列并发处理线程数(个)"
                )
        ]
        public PerformanceCounter DequeueThreadsCountPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _dequeueThreadsCountPerformanceCounter, value, 2);
            }
            get
            {
                return _dequeueThreadsCountPerformanceCounter;
            }
        }
        private PerformanceCounter _dequeueThreadEndPerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.NumberOfItems64
                    , CounterName = "11.出队列处理线程退出次数(次)"
                )
        ]
        public PerformanceCounter DequeueThreadEndPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (ref _dequeueThreadEndPerformanceCounter, value, 2);
            }
            get
            {
                return _dequeueThreadEndPerformanceCounter;
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
        private bool _isAttachedPerformanceCounters = false;
        public void AttachPerformanceCountersToProperties
                            (
                                string instanceName
                                , string categoryName
                            )
        {
            if (!_isAttachedPerformanceCounters)
            {
                var type = this.GetType();
                PerformanceCountersHelper
                    .AttachPerformanceCountersToProperties<QueuePerformanceCountersContainer>
                        (instanceName, categoryName, this);
            }
            _isAttachedPerformanceCounters = true;
        }
    }
}









