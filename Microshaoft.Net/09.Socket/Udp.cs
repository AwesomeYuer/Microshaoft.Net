#if NET45
namespace Test
{
    using Microshaoft;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        static BufferManager _bufferManager;
        static SocketAsyncEventArgsPool _socketAsyncEventArgsPool;
        static void Main(string[] args)
        {
            args = new string[]
                        {
                            "127.0.0.1:10080"
                            , "127.0.0.1:10081"
                        };
            _bufferManager = new BufferManager
                                    (
                                        64 * 1024 * 1024
                                        , 64 * 1024
                                    );
            _bufferManager.InitBuffer();
            _socketAsyncEventArgsPool = new SocketAsyncEventArgsPool(100);
            var performanceCountersCategoryName = "Microshaoft EasyPerformanceCounters Category";
            EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                .AttachPerformanceCountersCategoryInstance
                                (
                                    performanceCountersCategoryName
                                    , "Hander1::Sended"
                                );
            EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                .AttachPerformanceCountersCategoryInstance
                                (
                                    performanceCountersCategoryName
                                    , "Hander2::Sended"
                                );
            EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                .AttachPerformanceCountersCategoryInstance
                                (
                                    performanceCountersCategoryName
                                    , "Hander1::Received"
                                );
            EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                .AttachPerformanceCountersCategoryInstance
                                (
                                    performanceCountersCategoryName
                                    , "Hander2::Received"
                                );
            Console.WriteLine(@"Press any key to Send! Press ""q"" to release resource");
            string s = string.Empty;
            while ((s = Console.ReadLine().ToLower()) != "q")
            {
                Run(args);
            }
        }
        static void Run(string[] args)
        {
            var performanceCountersCategoryName = "Microshaoft EasyPerformanceCounters Category";
            var enableCounters = MultiPerformanceCountersTypeFlags.ProcessCounter
                                    | MultiPerformanceCountersTypeFlags.ProcessedAverageTimerCounter
                                    | MultiPerformanceCountersTypeFlags.ProcessedCounter
                                    | MultiPerformanceCountersTypeFlags.ProcessedRateOfCountsPerSecondCounter
                                    | MultiPerformanceCountersTypeFlags.ProcessingCounter;
            var sendEncoding = Encoding.Default;
            var receiveEncoding = Encoding.Default;
            //byte[] data = new byte[1024];
            string[] a = args[0].Split(new char[] { ':' });
            string ip = a[0];
            int port = int.Parse(a[1]);
            IPEndPoint ipep1 = new IPEndPoint(IPAddress.Parse(ip), port);
            Console.WriteLine("ipep1 {0}", ipep1.ToString());
            a = args[1].Split(new char[] { ':' });
            ip = a[0];
            port = int.Parse(a[1]);
            IPEndPoint ipep2 = new IPEndPoint(IPAddress.Parse(ip), port);
            Console.WriteLine("ipep2 {0}", ipep2.ToString());
            var remoteAnyIPEP = new IPEndPoint(IPAddress.Any, 0);
            Socket socket1 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket1.Bind(ipep1);
            SocketAsyncDataHandler<string> handler1 = new SocketAsyncDataHandler<string>(socket1, 1);
            var receiveSocketAsyncEventArgs1 = _socketAsyncEventArgsPool.Pop();
            _bufferManager.SetBuffer(receiveSocketAsyncEventArgs1);
            handler1.StartReceiveDataFrom
                        (
                            remoteAnyIPEP
                            , receiveSocketAsyncEventArgs1
                            , (x, y, z, w) =>
                            {
                                Console.WriteLine("次数: {0}", x.ReceivedCount);
                                Console.WriteLine("字节: {0}", z.Length);
                                EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                                    .CountPerformance
                                        (
                                            enableCounters
                                            , performanceCountersCategoryName
                                            , "Hander1::Received"
                                            , null
                                            , () =>
                                            {
                                                var ss = receiveEncoding.GetString(z);
                                                //Console.Write(s);
                                                Console.WriteLine
                                                            (
                                                                "from {0} , to {1}, data {2}"
                                                                , x.WorkingSocket.LocalEndPoint
                                                                , w.RemoteEndPoint
                                                                , ss
                                                            );
                                            }
                                            , null
                                            , null
                                            , null
                                        );
                                return false;
                            }
                            , (x, y, z) =>
                            {
                                Console.WriteLine(z.ToString());
                                return true;
                            }
                        );
            Socket socket2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket2.Bind(ipep2);
            SocketAsyncDataHandler<string> handler2 = new SocketAsyncDataHandler<string>(socket2, 2);
            var receiveSocketAsyncEventArgs2 = _socketAsyncEventArgsPool.Pop();
            _bufferManager.SetBuffer(receiveSocketAsyncEventArgs2);
            handler2.StartReceiveDataFrom
                            (
                                remoteAnyIPEP
                                , receiveSocketAsyncEventArgs2
                                , (x, y, z, w) =>
                                {
                                    Console.WriteLine("次数: {0}", x.ReceivedCount);
                                    Console.WriteLine("字节: {0}", z.Length);
                                    EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                                        .CountPerformance
                                            (
                                                enableCounters
                                                , performanceCountersCategoryName
                                                , "Hander2::Received"
                                                , null
                                                , () =>
                                                {
                                                    var ss = receiveEncoding.GetString(z);
                                                    //Console.Write(s);
                                                    Console.WriteLine
                                                                (
                                                                    "from {0} , to {1}, data {2}"
                                                                    , x.WorkingSocket.LocalEndPoint
                                                                    , w.RemoteEndPoint
                                                                    , ss
                                                                );
                                                }
                                                , null
                                                , null
                                                , null
                                            );
                                    return false;
                                }
                                , (x, y, z) =>
                                {
                                    Console.WriteLine(z.ToString());
                                    return true;
                                }
                            );
            string s = string.Empty;
            Console.WriteLine("Send ...");
            while ((s = Console.ReadLine().ToLower()) != "q")
            {
                var buffer = sendEncoding.GetBytes(s);
                Parallel.For
                        (
                            0
                            , 1000
                            , new ParallelOptions()
                            {
                                MaxDegreeOfParallelism = 1// Environment.ProcessorCount
                                //, TaskScheduler = null
                            }
                            , i =>
                            {
                                Thread.Sleep(5);
                                EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                                    .CountPerformance
                                        (
                                            enableCounters
                                            , performanceCountersCategoryName
                                            , "Hander1::Sended"
                                            , null
                                            , () =>
                                            {
                                                handler1.SendDataToSync(buffer, ipep2);
                                            }
                                            , null
                                            , null
                                            , null
                                        );
                                EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                                    .CountPerformance
                                        (
                                            enableCounters
                                            , performanceCountersCategoryName
                                            , "Hander2::Sended"
                                            , null
                                            , () =>
                                            {
                                                handler2.SendDataToSync(buffer, ipep1);
                                            }
                                            , null
                                            , null
                                            , null
                                        );
                            }
                        );
            }
            var e = handler1.ReceiveSocketAsyncEventArgs;
            //_bufferManager.FreeBuffer(e);
            _socketAsyncEventArgsPool.Push(e);
            e = handler2.ReceiveSocketAsyncEventArgs;
            //_bufferManager.FreeBuffer(e);
            _socketAsyncEventArgsPool.Push(e);
            handler1.DestoryWorkingSocket();
            handler2.DestoryWorkingSocket();
            Console.WriteLine("Send quit");
        }
        //private static int _recieveCount = 0;
    }
}



namespace TestConsoleApplication
{
    using Microshaoft;
    using System;
    using System.Diagnostics;
#if NET45
    using System.Threading.Tasks;
#endif
    class Program
    {
        static void Main1(string[] args)
        {
            Console.WriteLine("Begin ...");
            Random r = new Random();
            int sleep = 2;
            int iterations = 10;
            int maxDegreeOfParallelism = 8; // Environment.ProcessorCount;
            var performanceCountersCategoryName = "Microshaoft EasyPerformanceCounters Category";
            var performanceCountersCategoryInstanceName
                    = string.Format
                            (
                                "{2}{0}{3}{1}{4}"
                                , ": "
                                , " @ "
                                , ""
                                , ""
                                , Process.GetCurrentProcess().ProcessName
                            );
            //EasyPerformanceCountersHelper 调用示例
            EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                .AttachPerformanceCountersCategoryInstance
                                (
                                    performanceCountersCategoryName
                                    , performanceCountersCategoryInstanceName + "-1"
                                );
            EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                .AttachPerformanceCountersCategoryInstance
                    (
                        performanceCountersCategoryName
                        , performanceCountersCategoryInstanceName + "-2"
                    );
            var enableCounters = MultiPerformanceCountersTypeFlags.ProcessCounter
                                    | MultiPerformanceCountersTypeFlags.ProcessedAverageTimerCounter
                                    | MultiPerformanceCountersTypeFlags.ProcessedCounter
                                    | MultiPerformanceCountersTypeFlags.ProcessedRateOfCountsPerSecondCounter
                                    | MultiPerformanceCountersTypeFlags.ProcessingCounter;
            Parallel.For
                        (
                            0
                            , iterations
                            , new ParallelOptions()
                            {
                                MaxDegreeOfParallelism = maxDegreeOfParallelism
                            }
                            , (x) =>
                            {
                                EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                                    .CountPerformance
                                        (
                                            enableCounters
                                            , performanceCountersCategoryName
                                            , performanceCountersCategoryInstanceName + "-1"
                                            , null
                                            , () =>
                                            {
                                                sleep = r.Next(0, 5) * 1000;
                                                //Thread.Sleep(sleep);
                                                throw new Exception("sadsad");
                                            }
                                            , null
                                            , (xx) =>
                                            {
                                                //Console.WriteLine("Exception {0}", xx.ToString());
                                                return false;
                                            }
                                            , null
                                        );
                            }
                        );
            Parallel.For
                (
                    0
                    , iterations
                    , new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = maxDegreeOfParallelism
                    }
                    , (x) =>
                    {
                        Stopwatch stopwatch = null;
                        try
                        {
                            stopwatch =
                                EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                                    .CountPerformanceBegin
                                        (
                                            enableCounters
                                            , performanceCountersCategoryName
                                            , performanceCountersCategoryInstanceName + "-2"
                                        );
                            sleep = r.Next(0, 5) * 1000;
                            //Thread.Sleep(sleep);
                            throw new Exception("Test");
                        }
                        catch
                        {
                        }
                        finally
                        {
                            EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                                .CountPerformanceEnd
                                    (
                                        enableCounters
                                        , performanceCountersCategoryName
                                        , performanceCountersCategoryInstanceName + "-2"
                                        , stopwatch
                                    );
                        }
                    }
                );
            Console.WriteLine("End ...");
            Console.ReadLine();
        }
    }
}


namespace Microshaoft
{
    using System;
    using System.Diagnostics;
    public class CommonPerformanceCountersContainer
                    : IPerformanceCountersContainer
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
                    , CounterName = "01.接收处理笔数(笔)"
                )
        ]
        public PerformanceCounter PrcocessPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (
                            ref _processPerformanceCounter
                            , value
                            , 2
                        );
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
                    , CounterName = "02.正在处理笔数(笔)"
                )
        ]
        public PerformanceCounter ProcessingPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (
                            ref _processingPerformanceCounter
                            , value
                            , 2
                        );
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
                    , CounterName = "03.完成处理笔数(笔)"
                )
        ]
        public PerformanceCounter ProcessedPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (
                            ref _processedPerformanceCounter
                            , value
                            , 2
                        );
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
                    , CounterName = "04.每秒完成处理笔数(笔/秒)"
                )
        ]
        public PerformanceCounter ProcessedRateOfCountsPerSecondPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (
                            ref _processedRateOfCountsPerSecondPerformanceCounter
                            , value
                            , 2
                        );
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
                    , CounterName = "05.平均每笔处理耗时秒数(秒/笔)"
                )
        ]
        public PerformanceCounter ProcessedAverageTimerPerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (
                            ref _ProcessedAverageTimerPerformanceCounter
                            , value
                            , 2
                        );
            }
            get
            {
                return _ProcessedAverageTimerPerformanceCounter;
            }
        }
        private PerformanceCounter _processedAverageBasePerformanceCounter;
        [
            PerformanceCounterDefinitionAttribute
                (
                    CounterType = PerformanceCounterType.AverageBase
                )
        ]
        public PerformanceCounter ProcessedAverageBasePerformanceCounter
        {
            private set
            {
                ReaderWriterLockSlimHelper
                    .TryEnterWriterLockSlimWrite<PerformanceCounter>
                        (
                            ref _processedAverageBasePerformanceCounter
                            , value
                            , 2
                        );
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
            PerformanceCountersHelper
                .AttachPerformanceCountersToProperties<CommonPerformanceCountersContainer>
                    (
                        instanceName
                        , categoryName
                        , this
                    );
        }
    }
}
//=========================================================================================
//=========================================================================================
#endif




