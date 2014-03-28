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
                            , (x, y, z) =>
                            {
                                Console.WriteLine("次数: {0}", x.ReceivedCount);
                                Console.WriteLine("字节: {0}", y.Length);
                                EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                                    .CountPerformance
                                        (
                                            enableCounters
                                            , performanceCountersCategoryName
                                            , "Hander1::Received"
                                            , null
                                            , () =>
                                            {
                                                var ss = receiveEncoding.GetString(y);
                                                //Console.Write(s);
                                                Console.WriteLine
                                                            (
                                                                "from {0} , to {1}, data {2}"
                                                                , x.WorkingSocket.LocalEndPoint
                                                                , z.RemoteEndPoint
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
                                , (x, y, z) =>
                                {
                                    Console.WriteLine("次数: {0}", x.ReceivedCount);
                                    Console.WriteLine("字节: {0}", y.Length);
                                    EasyPerformanceCountersHelper<CommonPerformanceCountersContainer>
                                        .CountPerformance
                                            (
                                                enableCounters
                                                , performanceCountersCategoryName
                                                , "Hander2::Received"
                                                , null
                                                , () =>
                                                {
                                                    var ss = receiveEncoding.GetString(y);
                                                    //Console.Write(s);
                                                    Console.WriteLine
                                                                (
                                                                    "from {0} , to {1}, data {2}"
                                                                    , x.WorkingSocket.LocalEndPoint
                                                                    , z.RemoteEndPoint
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
namespace Microshaoft
{
    using System.Collections.Concurrent;
    using System.Net.Sockets;
    // Represents a collection of reusable SocketAsyncEventArgs objects.
    public class SocketAsyncEventArgsPool
    {
        private ConcurrentStack<SocketAsyncEventArgs> _pool;
        public SocketAsyncEventArgsPool(int count)
        {
            _pool = new ConcurrentStack<SocketAsyncEventArgs>();
            for (var i = 0; i < count; i++)
            {
                _pool.Push(new SocketAsyncEventArgs());
            }
        }
        // Add a SocketAsyncEventArg instance to the pool
        //
        //The "item" parameter is the SocketAsyncEventArgs instance
        // to add to the pool
        public void Push(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            _pool.Push(socketAsyncEventArgs);
        }
        // Removes a SocketAsyncEventArgs instance from the pool
        // and returns the object removed from the pool
        public SocketAsyncEventArgs Pop()
        {
            SocketAsyncEventArgs socketAsyncEventArgs = null;
            if
                (
                    _pool.IsEmpty
                    || !_pool.TryPop(out socketAsyncEventArgs)
                )
            {
                socketAsyncEventArgs = new SocketAsyncEventArgs();
            }
            return socketAsyncEventArgs;
        }
        // The number of SocketAsyncEventArgs instances in the pool
        public int Count
        {
            get
            {
                return _pool.Count;
            }
        }
    }
}
namespace Microshaoft
{
    using System.Collections.Concurrent;
    using System.Net.Sockets;
    public class BufferManager
    {
        // This class creates a single large buffer which can be divided up
        // and assigned to SocketAsyncEventArgs objects for use with each
        // socket I/O operation.
        // This enables buffers to be easily reused and guards against
        // fragmenting heap memory.
        //
        //This buffer is a byte array which the Windows TCP buffer can copy its data to.
        // the total number of bytes controlled by the buffer pool
        int _totalBytesInBufferBlock;
        // Byte array maintained by the Buffer Manager.
        byte[] _bufferBlock;
        ConcurrentStack<int> _freeIndexPool;
        int _currentIndex;
        int _bufferBytesAllocatedForEachSaea;
        public BufferManager(int totalBytes, int totalBufferBytesInEachSaeaObject)
        {
            _totalBytesInBufferBlock = totalBytes;
            _currentIndex = 0;
            _bufferBytesAllocatedForEachSaea = totalBufferBytesInEachSaeaObject;
            _freeIndexPool = new ConcurrentStack<int>();
        }
        // Allocates buffer space used by the buffer pool
        public void InitBuffer()
        {
            // Create one large buffer block.
            _bufferBlock = new byte[_totalBytesInBufferBlock];
        }
        // Divide that one large buffer block out to each SocketAsyncEventArg object.
        // Assign a buffer space from the buffer block to the
        // specified SocketAsyncEventArgs object.
        //
        // returns true if the buffer was successfully set, else false
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            int index = -1;
            if (_freeIndexPool.TryPop(out index))
            {
                //This if-statement is only true if you have called the FreeBuffer
                //method previously, which would put an offset for a buffer space
                //back into this stack.
                args.SetBuffer(_bufferBlock, index, _bufferBytesAllocatedForEachSaea);
            }
            else
            {
                //Inside this else-statement is the code that is used to set the
                //buffer for each SAEA object when the pool of SAEA objects is built
                //in the Init method.
                if ((_totalBytesInBufferBlock - _bufferBytesAllocatedForEachSaea) < _currentIndex)
                {
                    return false;
                }
                args.SetBuffer(_bufferBlock, _currentIndex, _bufferBytesAllocatedForEachSaea);
                _currentIndex += _bufferBytesAllocatedForEachSaea;
            }
            return true;
        }
        // Removes the buffer from a SocketAsyncEventArg object.   This frees the
        // buffer back to the buffer pool. Try NOT to use the FreeBuffer method,
        // unless you need to destroy the SAEA object, or maybe in the case
        // of some exception handling. Instead, on the server
        // keep the same buffer space assigned to one SAEA object for the duration of
        // this app's running.
        private void FreeBuffer(SocketAsyncEventArgs args)
        {
            _freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
namespace Microshaoft
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    public class SocketAsyncDataHandler<T>
    {
        private Socket _socket;
        public Socket WorkingSocket
        {
            get
            {
                return _socket;
            }
        }
        //public int ReceiveDataBufferLength
        //{
        //    get;
        //    private set;
        //}
        public T ConnectionToken
        {
            get;
            set;
        }
        public IPAddress RemoteIPAddress
        {
            get
            {
                return ((IPEndPoint)_socket.RemoteEndPoint).Address;
            }
        }
        public IPAddress LocalIPAddress
        {
            get
            {
                return ((IPEndPoint)_socket.LocalEndPoint).Address;
            }
        }
        public int SocketID
        {
            get;
            private set;
        }
        public SocketAsyncDataHandler
                            (
                                Socket socket
                                , int socketID
                            )
        {
            _socket = socket;
            _isUdp = (_socket.ProtocolType == ProtocolType.Udp);
            _sendSocketAsyncEventArgs = new SocketAsyncEventArgs();
            SocketID = socketID;
        }
        private SocketAsyncEventArgs _sendSocketAsyncEventArgs;
        public int HeaderBytesLength
        {
            get;
            private set;
        }
        public int HeaderBytesOffset
        {
            get;
            private set;
        }
        private long _receivedCount = 0;
        public long ReceivedCount
        {
            get
            {
                return _receivedCount;
            }
        }
        public int HeaderBytesCount
        {
            get;
            private set;
        }
        public SocketAsyncEventArgs ReceiveSocketAsyncEventArgs
        {
            get;
            private set;
        }
        private bool _isStartedReceiveData = false;
        private bool _isHeader = true;
        public bool StartReceiveWholeDataPackets
                            (
                                int headerBytesLength
                                , int headerBytesOffset
                                , int headerBytesCount
                                , Func<SocketAsyncEventArgs> getReceiveSocketAsyncEventArgsProcessFunc
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , byte[]
                                        , SocketAsyncEventArgs
                                        , bool
                                    > onOneWholeDataPacketReceivedProcessFunc
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , byte[]
                                        , SocketAsyncEventArgs
                                        , bool
                                    > onReceivedDataPacketErrorProcessFunc = null
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , SocketAsyncEventArgs
                                        , Exception
                                        , bool
                                    > onCaughtExceptionProcessFunc = null
                            )
        {
            return
                StartReceiveWholeDataPackets
                    (
                        headerBytesLength
                        , headerBytesOffset
                        , headerBytesCount
                        , getReceiveSocketAsyncEventArgsProcessFunc()
                        , onOneWholeDataPacketReceivedProcessFunc
                        , onReceivedDataPacketErrorProcessFunc
                        , onCaughtExceptionProcessFunc
                    );
        }
        public bool StartReceiveWholeDataPackets
                            (
                                int headerBytesLength
                                , int headerBytesOffset
                                , int headerBytesCount
                                , SocketAsyncEventArgs receiveSocketAsyncEventArgs
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , byte[]
                                        , SocketAsyncEventArgs
                                        , bool
                                    > onOneWholeDataPacketReceivedProcessFunc
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , byte[]
                                        , SocketAsyncEventArgs
                                        , bool
                                    > onReceivedDataPacketErrorProcessFunc = null
                                , Func
                                    <
                                        SocketAsyncDataHandler<T>
                                        , SocketAsyncEventArgs
                                        , Exception
                                        , bool
                                    > onCaughtExceptionProcessFunc = null
                            )
        {
            if (!_isStartedReceiveData)
            {
                HeaderBytesLength = headerBytesLength;
                HeaderBytesOffset = headerBytesOffset;
                HeaderBytesCount = headerBytesCount;
                int bodyLength = 0;
                ReceiveSocketAsyncEventArgs = receiveSocketAsyncEventArgs;
                ReceiveSocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>
                                (
                                    (sender, e) =>
                                    {
                                        var socket = sender as Socket;
                                        if (e.BytesTransferred >= 0)
                                        {
                                            byte[] buffer = e.Buffer;
                                            int r = e.BytesTransferred;
                                            int p = e.Offset;
                                            int l = e.Count;
                                            if (r < l)
                                            {
                                                p += r;
                                                e.SetBuffer(p, l - r);
                                            }
                                            else if (r == l)
                                            {
                                                if (_isHeader)
                                                {
                                                    byte[] data = new byte[headerBytesCount];
                                                    Buffer.BlockCopy
                                                                (
                                                                    buffer
                                                                    , HeaderBytesOffset
                                                                    , data
                                                                    , 0
                                                                    , data.Length
                                                                );
                                                    byte[] intBytes = new byte[4];
                                                    l = (intBytes.Length < HeaderBytesCount ? intBytes.Length : HeaderBytesCount);
                                                    Buffer.BlockCopy
                                                                (
                                                                    data
                                                                    , 0
                                                                    , intBytes
                                                                    , 0
                                                                    , l
                                                                );
                                                    //Array.Reverse(intBytes);
                                                    bodyLength = BitConverter.ToInt32(intBytes, 0);
                                                    p += r;
                                                    e.SetBuffer(p, bodyLength);
                                                    Console.WriteLine(bodyLength);
                                                    _isHeader = false;
                                                }
                                                else
                                                {
                                                    byte[] data = new byte[bodyLength + HeaderBytesLength];
                                                    bodyLength = 0;
                                                    Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
                                                    _isHeader = true;
                                                    e.SetBuffer(0, HeaderBytesLength);
                                                    if (onOneWholeDataPacketReceivedProcessFunc != null)
                                                    {
                                                        onOneWholeDataPacketReceivedProcessFunc
                                                            (
                                                                this
                                                                , data
                                                                , e
                                                            );
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (onReceivedDataPacketErrorProcessFunc != null)
                                                {
                                                    byte[] data = new byte[p + r + HeaderBytesLength];
                                                    Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
                                                    bool b = onReceivedDataPacketErrorProcessFunc
                                                                (
                                                                    this
                                                                    , data
                                                                    , e
                                                                );
                                                    if (b)
                                                    {
                                                        bool i = DestoryWorkingSocket();
                                                    }
                                                    else
                                                    {
                                                        _isHeader = true;
                                                        e.SetBuffer(0, HeaderBytesLength);
                                                    }
                                                }
                                            }
                                        }
                                        if (!_isWorkingSocketDestoryed)
                                        {
                                            try
                                            {
                                                socket.ReceiveAsync(e);
                                            }
                                            catch (Exception exception)
                                            {
                                                var r = false;
                                                if (onCaughtExceptionProcessFunc != null)
                                                {
                                                    r = onCaughtExceptionProcessFunc
                                                                (
                                                                    this
                                                                    , e
                                                                    , exception
                                                                );
                                                }
                                                if (r)
                                                {
                                                    DestoryWorkingSocket();
                                                }
                                            }
                                        }
                                    }
                                );
                _socket.ReceiveAsync(ReceiveSocketAsyncEventArgs);
                _isStartedReceiveData = true;
            }
            return _isStartedReceiveData;
        }
        private bool _isUdp = false;
        public bool IsUdp
        {
            get
            {
                return _isUdp;
            }
        }
        private bool _isWorkingSocketDestoryed = false;
        public bool DestoryWorkingSocket()
        {
            //bool r = false;
            try
            {
                if (_socket.Connected)
                {
                    _socket.Disconnect(false);
                }
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                _socket.Dispose();
                _socket = null;
                _isWorkingSocketDestoryed = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                //r = false;
            }
            return _isWorkingSocketDestoryed;
        }
        public bool StartReceiveDataFrom
            (
                EndPoint remoteEndPoint
                , Func<SocketAsyncEventArgs> getReceiveSocketAsyncEventArgsProcessFunc
                , Func
                    <
                        SocketAsyncDataHandler<T>
                        , byte[]
                        , SocketAsyncEventArgs
                        , bool
                    > onDataReceivedProcessFunc
                , Func
                    <
                        SocketAsyncDataHandler<T>
                        , SocketAsyncEventArgs
                        , Exception
                        , bool
                    > onCaughtExceptionProcessFunc = null
            )
        {
            return
                StartReceiveDataFrom
                    (
                        remoteEndPoint
                        , getReceiveSocketAsyncEventArgsProcessFunc()
                        , onDataReceivedProcessFunc
                        , onCaughtExceptionProcessFunc
                    );
        }
        public bool StartReceiveDataFrom
                    (
                        EndPoint remoteEndPoint
                        , SocketAsyncEventArgs receiveSocketAsyncEventArgs
                        , Func
                            <
                                SocketAsyncDataHandler<T>
                                , byte[]
                                , SocketAsyncEventArgs
                                , bool
                            > onDataReceivedProcessFunc
                        , Func
                            <
                                SocketAsyncDataHandler<T>
                                , SocketAsyncEventArgs
                                , Exception
                                , bool
                            > onCaughtExceptionProcessFunc = null
                    )
        {
            if (!_isStartedReceiveData)
            {
                ReceiveSocketAsyncEventArgs = receiveSocketAsyncEventArgs;
                ReceiveSocketAsyncEventArgs.RemoteEndPoint = remoteEndPoint;
                ReceiveSocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>
                                                (
                                                    (sender, e) =>
                                                    {
                                                        Interlocked.Increment(ref _receivedCount);
                                                        var socket = sender as Socket;
                                                        int l = e.BytesTransferred;
                                                        //Console.WriteLine(l);
                                                        if (l > 0)
                                                        {
                                                            byte[] data = new byte[l];
                                                            var buffer = e.Buffer;
                                                            Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
                                                            if (onDataReceivedProcessFunc != null)
                                                            {
                                                                onDataReceivedProcessFunc(this, data, e);
                                                                //Console.WriteLine(_receivedCount);
                                                            }
                                                        }
                                                        if (!_isWorkingSocketDestoryed)
                                                        {
                                                            try
                                                            {
                                                                socket.ReceiveFromAsync(ReceiveSocketAsyncEventArgs);
                                                            }
                                                            catch (Exception exception)
                                                            {
                                                                //Console.WriteLine(exception.ToString());
                                                                var r = false;
                                                                if (onCaughtExceptionProcessFunc != null)
                                                                {
                                                                    r = onCaughtExceptionProcessFunc(this, ReceiveSocketAsyncEventArgs, exception);
                                                                }
                                                                if (r)
                                                                {
                                                                    DestoryWorkingSocket();
                                                                }
                                                            }
                                                        }
                                                    }
                                                );
                _socket.ReceiveFromAsync(ReceiveSocketAsyncEventArgs);
                _isStartedReceiveData = true;
            }
            return _isStartedReceiveData;
        }
        private object _sendSyncLockObject = new object();
        public int SendDataSync(byte[] data)
        {
            var r = -1;
            if (!_isUdp)
            {
                lock (_sendSyncLockObject)
                {
                    r = _socket.Send(data);
                }
            }
            return r;
        }
        public int SendDataSyncWithRetry
                (
                    byte[] data
                    , int retry = 3
                    , int sleepSeconds = 1
                )
        {
            //增加就地重试机制
            int r = -1;
            int i = 0;
            while (i < retry)
            {
                r = -1;
                //lock (_sendSyncLockObject)
                {
                    try
                    {
                        if (_socket != null)
                        {
                            r = SendDataSync(data);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                i++;
                if (r > 0 || i == retry)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(sleepSeconds * 1000);
                }
            }
            return r;
        }
        public int SendDataToSync(byte[] data, EndPoint remoteEndPoint)
        {
            var r = -1;
            if (_isUdp)
            {
                lock (_sendSyncLockObject)
                {
                    r = _socket.SendTo(data, remoteEndPoint);
                }
            }
            return r;
        }
        public int SendDataToSyncWithRetry
                        (
                            byte[] data
                            , EndPoint remoteEndPoint
                            , int retry = 3
                            , int sleepSeconds = 1
                        )
        {
            //增加就地重试机制
            int r = -1;
            int i = 0;
            while (i < retry)
            {
                r = -1;
                lock (_sendSyncLockObject)
                {
                    try
                    {
                        if (_socket != null)
                        {
                            r = _socket.SendTo(data, remoteEndPoint);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                i++;
                if (r > 0 || i == retry)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(sleepSeconds * 1000);
                }
            }
            return r;
        }
    }
}
namespace TestConsoleApplication
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microshaoft;
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





